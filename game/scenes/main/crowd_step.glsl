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

// GpuCrowd.cs の PushConstants と同じ並び（48 バイト）
layout(push_constant, std430) uniform Params {
    uint count;
    uint seed;
    uint step;
    uint mode; // 0 = 散策、1 = 狂乱（CrowdMode の値）
    uint turn_steps;
    uint columns;
    uint rows;
    float step_seconds;
    float walk_speed;
    float field_scale;
    float frenzy_speed;
    float padding;
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

void wander(uint i) {
    float width = float(p.columns);
    float height = float(p.rows);
    float h = heading[i];
    if ((p.step + i) % p.turn_steps == 0u) {
        h = next01(p.seed, i, p.step) * TAU;
    }
    int column = clamp(int(pos_x[i]), 0, int(p.columns) - 1);
    int row = clamp(int(pos_y[i]), 0, int(p.rows) - 1);
    uint cell = uint(row) * p.columns + uint(column);
    float x = pos_x[i] + (cos(h) * p.walk_speed + force_x[cell] * p.field_scale) * p.step_seconds;
    float y = pos_y[i] + (sin(h) * p.walk_speed + force_y[cell] * p.field_scale) * p.step_seconds;

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
    if (i >= p.count) {
        return;
    }
    if (p.mode == 0u) {
        wander(i);
    } else {
        frenzy(i);
    }
}
