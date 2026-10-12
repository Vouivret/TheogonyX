#[compute]
#version 450

// 群衆の 1 歩（docs/domain/crowd.md）。src/TheogonyX.Core/Crowd.cs の Wander・Frenzy と同じ計算をする。
// 変えたら Crowd.cs と GpuCrowd.cs（バッファ・push constant の並び）も同じ変更で直す（.agent/rules/godot.md）。

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) restrict buffer PositionX { float pos_x[]; };
layout(set = 0, binding = 1, std430) restrict buffer PositionY { float pos_y[]; };
layout(set = 0, binding = 2, std430) restrict buffer Heading { float heading[]; };
layout(set = 0, binding = 3, std430) restrict readonly buffer TargetX { float target_x[]; };
layout(set = 0, binding = 4, std430) restrict readonly buffer TargetY { float target_y[]; };
layout(set = 0, binding = 5, std430) restrict readonly buffer ForceX { float force_x[]; };
layout(set = 0, binding = 6, std430) restrict readonly buffer ForceY { float force_y[]; };
// 群れの集計（FlockGrid.cs）。マスの数を n として、[人数 n 個, x の和 n 個, y の和 n 個, 向き x の和 n 個, 向き y の和 n 個]
layout(set = 0, binding = 7, std430) restrict buffer Flock { int flock[]; };

// GpuCrowd.cs の PushConstants と同じ並び（80 バイト）
layout(push_constant, std430) uniform Params {
    uint count;
    uint seed;
    uint step;
    uint mode; // 0 = 散策、1 = 狂乱（CrowdMode の値）、2 = 群れの集計を 0 にする、3 = 群れを集計する
    uint turn_steps;
    uint columns;
    uint rows;
    float step_seconds;
    float walk_speed;
    float field_scale;
    float frenzy_speed;
    uint flock_enabled;
    uint grid_columns;
    uint grid_rows;
    float cell_size;
    float align_weight;
    float crowd_weight;
    float target_neighbors;
    float padding0;
    float padding1;
} p;

// C# の MathF.PI・MathF.Tau と同じ float の値
const float PI = 3.14159274;
const float TAU = 6.28318548;

// CrowdHash.cs と同じ（docs/adr/0004-crowd-counter-based-hash-random.md）
uint pcg(uint value) {
    uint state = value * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

float next01(uint seed, uint index, uint step) {
    return float(pcg(seed ^ pcg(index ^ pcg(step))) >> 8u) * (1.0 / 16777216.0);
}

// FlockGrid.ToFixed と同じ
int to_fixed(float value) {
    return int(floor(value * 1024.0 + 0.5));
}

uint grid_cells() {
    return p.grid_columns * p.grid_rows;
}

uvec2 flock_cell_of(float x, float y) {
    return uvec2(clamp(int(x / p.cell_size), 0, int(p.grid_columns) - 1), clamp(int(y / p.cell_size), 0, int(p.grid_rows) - 1));
}

void flock_clear(uint i) {
    if (i < grid_cells()) {
        for (uint k = 0u; k < 5u; k++) {
            flock[k * grid_cells() + i] = 0;
        }
    }
}

void flock_accumulate(uint i) {
    uvec2 c = flock_cell_of(pos_x[i], pos_y[i]);
    uint cell = c.y * p.grid_columns + c.x;
    uint n = grid_cells();
    atomicAdd(flock[cell], 1);
    atomicAdd(flock[n + cell], to_fixed(pos_x[i]));
    atomicAdd(flock[2u * n + cell], to_fixed(pos_y[i]));
    atomicAdd(flock[3u * n + cell], to_fixed(cos(heading[i])));
    atomicAdd(flock[4u * n + cell], to_fixed(sin(heading[i])));
}

// Crowd.FlockVelocity と同じ
vec2 flock_velocity(uint i) {
    if (p.flock_enabled == 0u) {
        return vec2(0.0);
    }
    uvec2 c = flock_cell_of(pos_x[i], pos_y[i]);
    uint n = grid_cells();
    int count = 0;
    int sum_x = 0;
    int sum_y = 0;
    int sum_dx = 0;
    int sum_dy = 0;
    for (int r = max(int(c.y) - 1, 0); r <= min(int(c.y) + 1, int(p.grid_rows) - 1); r++) {
        for (int col = max(int(c.x) - 1, 0); col <= min(int(c.x) + 1, int(p.grid_columns) - 1); col++) {
            uint cell = uint(r) * p.grid_columns + uint(col);
            count += flock[cell];
            sum_x += flock[n + cell];
            sum_y += flock[2u * n + cell];
            sum_dx += flock[3u * n + cell];
            sum_dy += flock[4u * n + cell];
        }
    }
    if (count <= 1) {
        return vec2(0.0);
    }
    float scale = float(count) * 1024.0;
    vec2 velocity = vec2(float(sum_dx) / scale, float(sum_dy) / scale) * p.walk_speed * p.align_weight;
    float to_center_x = float(sum_x) / scale - pos_x[i];
    float to_center_y = float(sum_y) / scale - pos_y[i];
    float distance = sqrt(to_center_x * to_center_x + to_center_y * to_center_y);
    if (distance > 1e-6) {
        float crowding = clamp(1.0 - float(count) / p.target_neighbors, -1.0, 1.0);
        float speed = crowding * p.walk_speed * p.crowd_weight / distance;
        velocity += vec2(to_center_x, to_center_y) * speed;
    }
    return velocity;
}

// PotentialField.At と同じ（周りの 4 マスの中心の力を双線形補間する）
float blend(float top_left, float top_right, float bottom_left, float bottom_right, float tx, float ty) {
    float top = top_left + (top_right - top_left) * tx;
    float bottom = bottom_left + (bottom_right - bottom_left) * tx;
    return top + (bottom - top) * ty;
}

vec2 field_at(float x, float y) {
    float grid_x = clamp(x - 0.5, 0.0, float(p.columns) - 1.0);
    float grid_y = clamp(y - 0.5, 0.0, float(p.rows) - 1.0);
    uint column0 = uint(grid_x);
    uint row0 = uint(grid_y);
    uint column1 = min(column0 + 1u, p.columns - 1u);
    uint row1 = min(row0 + 1u, p.rows - 1u);
    float tx = grid_x - float(column0);
    float ty = grid_y - float(row0);
    uint a = row0 * p.columns + column0;
    uint b = row0 * p.columns + column1;
    uint c = row1 * p.columns + column0;
    uint d = row1 * p.columns + column1;
    return vec2(
        blend(force_x[a], force_x[b], force_x[c], force_x[d], tx, ty),
        blend(force_y[a], force_y[b], force_y[c], force_y[d], tx, ty));
}

void wander(uint i) {
    float width = float(p.columns);
    float height = float(p.rows);
    float h = heading[i];
    if ((p.step + i) % p.turn_steps == 0u) {
        h = next01(p.seed, i, p.step) * TAU;
    }
    vec2 force = field_at(pos_x[i], pos_y[i]);
    vec2 flock_v = flock_velocity(i);
    float x = pos_x[i] + (cos(h) * p.walk_speed + force.x * p.field_scale + flock_v.x) * p.step_seconds;
    float y = pos_y[i] + (sin(h) * p.walk_speed + force.y * p.field_scale + flock_v.y) * p.step_seconds;

    if (x < 0.0 || x > width) {
        x = x < 0.0 ? -x : 2.0 * width - x;
        h = PI - h;
    }
    if (y < 0.0 || y > height) {
        y = y < 0.0 ? -y : 2.0 * height - y;
        h = -h;
    }
    heading[i] = h;
    pos_x[i] = clamp(x, 0.0, width);
    pos_y[i] = clamp(y, 0.0, height);
}

void frenzy(uint i) {
    float reach = p.frenzy_speed * p.step_seconds;
    float dx = target_x[i] - pos_x[i];
    float dy = target_y[i] - pos_y[i];
    float distance = sqrt(dx * dx + dy * dy);
    if (distance <= reach) {
        pos_x[i] = target_x[i];
        pos_y[i] = target_y[i];
    } else {
        pos_x[i] += dx / distance * reach;
        pos_y[i] += dy / distance * reach;
    }
}

void main() {
    uint i = gl_GlobalInvocationID.x;
    if (p.mode == 2u) {
        flock_clear(i);
        return;
    }
    if (i >= p.count) {
        return;
    }
    if (p.mode == 3u) {
        flock_accumulate(i);
    } else if (p.mode == 0u) {
        wander(i);
    } else {
        frenzy(i);
    }
}
