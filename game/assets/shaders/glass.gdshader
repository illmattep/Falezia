shader_type canvas_item;
render_mode blend_mix, unshaded;

// FALEZIA · "vetro" dell'interfaccia.
//
// Un solo shader per due modalità, decise dalle impostazioni grafiche:
//   · ALTO  → blur: campiona lo schermo dietro al pannello (mip) e lo tinge;
//   · BASSO → trasparenza semplice: niente campionamento, solo tinta e alfa.
//
// Il pannello è un rettangolo con gli angoli arrotondati (SDF), un bordo interno
// appena più chiaro e una velatura verticale: è la base di TUTTA la UI (negozio,
// HUD, menu), così i pannelli hanno lo stesso carattere ovunque.

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear_mipmap;

uniform vec2 size = vec2(400.0, 240.0);        // in pixel: lo aggiorna il nodo
uniform float radius = 12.0;
uniform float blur = 2.2;                      // 0 = trasparenza semplice
uniform vec4 tint : source_color = vec4(0.035, 0.055, 0.075, 0.86);
uniform vec4 border_color : source_color = vec4(1.0, 1.0, 1.0, 0.07);
uniform float border_width = 1.0;
uniform float sheen = 0.05;                    // velatura verticale

float sd_rrect(vec2 p, vec2 b, float r) {
	vec2 q = abs(p) - b + vec2(r);
	return min(max(q.x, q.y), 0.0) + length(max(q, vec2(0.0))) - r;
}

void fragment() {
	vec2 p = (UV - 0.5) * size;
	float d = sd_rrect(p, size * 0.5, radius);
	float inside = 1.0 - smoothstep(-1.0, 0.0, d);

	vec3 col = tint.rgb;
	float a = tint.a;

	if (blur > 0.01) {
		// lo schermo dietro, sfocato: il mip level è il raggio del blur
		vec3 back = textureLod(screen_tex, SCREEN_UV, blur).rgb;
		col = mix(back, tint.rgb, tint.a);
		a = 1.0;
	}

	// velatura: in alto un filo più chiaro, in basso più scuro
	float g = mix(-sheen, sheen, UV.y);
	col = clamp(col + g, 0.0, 1.0);

	// bordo interno: dà lo spessore del vetro
	float rim = inside * (1.0 - smoothstep(-border_width - 1.0, -border_width, d));
	col = mix(col, border_color.rgb, rim * border_color.a);

	COLOR = vec4(col, a * inside);
}
