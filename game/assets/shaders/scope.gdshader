shader_type canvas_item;
render_mode blend_mix, unshaded;

// FALEZIA · ottiche. Un solo shader per due cose diverse:
//   reticle 0/1/2 → cannocchiale: tubo nero, vetro, reticolo (1 = croce, 2 = mildot)
//   reticle 3     → punto rosso: nessuna maschera, solo il puntino e l'anello
// Il raggio è in PIXEL, così il cerchio resta tondo a qualunque risoluzione.
//
// Il vetro non è solo un buco nel nero: ha la vignettatura del tubo (gli angoli
// si scuriscono), una tinta da trattamento antiriflesso, un riflesso a mezzaluna
// in alto a sinistra e il bordo scuro dell'oculare. Il reticolo ha i pali grossi
// fuori e la croce sottile dentro, con i punti di profondità sul 6×.

uniform vec2 viewport = vec2(1920.0, 1080.0);
uniform float radius = 420.0;
uniform float softness = 1.5;
uniform float alpha = 1.0;
uniform float reticle = 0.0;
uniform float spread_px = 0.0;
uniform vec2 sway = vec2(0.0);
uniform vec4 mask_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 ink : source_color = vec4(0.025, 0.025, 0.03, 1.0);
uniform vec4 glass_tint : source_color = vec4(0.045, 0.075, 0.085, 1.0);
uniform vec4 dot_color : source_color = vec4(1.0, 0.12, 0.09, 1.0);

void fragment() {
	vec2 p = (UV - 0.5) * viewport;
	// il mirino "respira": il reticolo si sposta di un pixel o due, il tubo no
	vec2 q = p - sway;
	float d = length(q);
	float d_all = length(p);

	vec3 col = vec3(0.0);
	float a = 0.0;

	if (reticle < 2.5) {
		// ---------------------------------------------------- cannocchiale
		// fuori dal cerchio: nero pieno. Dentro: vetro quasi trasparente.
		float outside = smoothstep(radius - softness, radius + softness, d_all);
		float vig = smoothstep(radius * 0.48, radius * 1.02, d_all);
		float tube = smoothstep(radius * 0.86, radius * 1.0, d_all);   // spessore dell'oculare

		float glass_a = (0.045 + 0.30 * vig * vig + 0.34 * tube) * alpha;
		vec3 glass = glass_tint.rgb;

		// riflesso del cielo sulla lente: una mezzaluna in alto a sinistra
		vec2 g = (p - vec2(-radius * 0.30, -radius * 0.34)) / (radius * 0.92);
		float glare = (1.0 - smoothstep(0.0, 1.0, length(g))) * 0.055;
		glare += (1.0 - smoothstep(0.0, 1.0, length((p - vec2(radius * 0.55, -radius * 0.72)) / (radius * 0.55)))) * 0.03;

		col = mix(glass, mask_color.rgb, outside);
		a = mix(glass_a, 1.0, outside);
		col += vec3(0.55, 0.66, 0.78) * glare * (1.0 - outside);

		if (reticle > 0.5) {
			float th = max(0.9, radius * 0.0030);
			float gap = 9.0 + spread_px;              // il vuoto al centro segue il cono vero
			float post = radius * 0.52;               // dove finiscono i pali grossi

			// pali grossi (dal bordo verso il centro) e croce sottile (dal palo al vuoto)
			float thick = max(th * 2.4, 2.4);
			float v_post = step(abs(q.x), thick) * step(post, abs(q.y));
			float h_post = step(abs(q.y), thick) * step(post, abs(q.x));
			float v = step(abs(q.x), th) * step(gap, abs(q.y)) * step(abs(q.y), post);
			float h = step(abs(q.y), th) * step(gap, abs(q.x)) * step(abs(q.x), post);
			float ret = max(max(v_post, h_post), max(v, h));

			if (reticle > 1.5) {
				// tacche di profondità (mildot): punti ogni 0,1 raggio, sotto e ai lati
				float stepy = radius * 0.105;
				for (int i = 1; i <= 5; i++) {
					float yy = -float(i) * stepy;
					ret = max(ret, 1.0 - smoothstep(1.0, 2.2, length(q - vec2(0.0, yy))));
					float xx = float(i) * stepy;
					ret = max(ret, 1.0 - smoothstep(1.0, 2.2, length(q - vec2(xx, 0.0))));
					ret = max(ret, 1.0 - smoothstep(1.0, 2.2, length(q - vec2(-xx, 0.0))));
				}
				// tacche corte fra i punti, solo sulla verticale bassa
				float tick = step(abs(fract(q.y / (stepy * 0.5)) - 0.5), 0.04)
				           * step(abs(q.x), th * 0.7) * step(stepy * 0.5, abs(q.y));
				ret = max(ret, tick * 0.75);
			}

			// il reticolo si spegne sul bordo del vetro, non ci arriva mai
			ret *= 1.0 - smoothstep(radius * 0.94, radius, d_all);
			col = mix(col, ink.rgb, ret);
			a = max(a, ret * alpha);

			// puntino centrale, più fine dei pali
			float dot = 1.0 - smoothstep(1.1, 2.3, d);
			col = mix(col, ink.rgb, dot);
			a = max(a, dot * alpha * 0.92);
		}
	} else {
		// ---------------------------------------------------- punto rosso
		float dot = 1.0 - smoothstep(2.0, 3.4, d);
		float core = 1.0 - smoothstep(0.0, 1.2, d);
		float glow = (1.0 - smoothstep(0.0, 13.0, d)) * 0.34;
		float ring = 1.0 - smoothstep(1.1, 2.3, abs(d - 86.0));
		// alone appena percettibile: il vetro del dot non è perfettamente pulito
		float smear = (1.0 - smoothstep(0.0, 26.0, d)) * 0.05;

		float m = max(max(dot, glow), max(ring * 0.15, smear));
		col = mix(dot_color.rgb, vec3(1.0, 0.55, 0.45), core * 0.6);
		a = m * alpha;
	}

	COLOR = vec4(col, a);
}
