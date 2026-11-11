shader_type canvas_item;

// 視点を切り替えた直後の「ピクセル・フォーカス」。地図を block 画素ごとのモザイクにして、段階的に元に戻す。
// block が 1 のときは元の絵と同じ。
uniform float block : hint_range(1.0, 16.0) = 1.0;

void fragment() {
	vec2 size = 1.0 / TEXTURE_PIXEL_SIZE;
	vec2 uv = (floor(UV * size / block) + 0.5) * block / size;
	COLOR = texture(TEXTURE, clamp(uv, vec2(0.0), vec2(1.0)));
}
