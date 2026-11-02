// Scia del proiettile — Calanera
//
// Non è un tubo che si illumina: è gas. Il cilindro resta (costa niente), ma dentro
// c'è rumore che scorre, il centro è più luminoso dei bordi e la punta evapora.
// Sottile e poco opaca: deve leggersi come una scia, non come un laser.
//
// Parametri per istanza:
//   age     0 = appena partita, 1 = finita (vita ~55 ms)
//   seed    sfasamento del rumore, diverso a ogni colpo
//   opacity quanto è visibile (la Hush quasi niente)
//   core    colore caldo del nucleo · smoke colore del gas
shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_disabled, unshaded, shadows_disabled, fog_disabled;

uniform sampler2D noise_tex : filter_linear_mipmap, repeat_enable;
uniform vec4 core : source_color = vec4(1.0, 0.90, 0.66, 1.0);
uniform vec4 smoke : source_color = vec4(0.88, 0.90, 0.94, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.42;
uniform float age : hint_range(0.0, 1.0) = 0.0;
uniform float seed = 0.0;
uniform float scroll : hint_range(0.0, 8.0) = 3.0;
// ritaglio del rumore: cambia la forma delle vene a ogni colpo
uniform vec2 uv_offset = vec2(0.0, 0.0);

varying float v_along;

void vertex() {
    // 0 alla bocca della canna, 1 al punto d'impatto: il cilindro ha altezza 1
    v_along = clamp(VERTEX.y + 0.5, 0.0, 1.0);
}

void fragment() {
    float along = v_along;

    // rumore che scorre lungo la scia (due strati, il secondo più fine)
    float n1 = texture(noise_tex, vec2(along * 0.9 + seed, TIME * scroll + seed * 3.1) + uv_offset).r;
    float n2 = texture(noise_tex, vec2(along * 2.7 - seed * 0.7, TIME * scroll * 1.7) - uv_offset * 0.7).r;
    float wisps = mix(n1, n2, 0.45);

    // il vetro del cilindro: centro luminoso, silouette evanescente
    float facing = clamp(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.0, 1.0);
    float body = pow(facing, 2.2);

    // si spegne verso la punta e non nasce "tagliata" alla bocca
    float fade = smoothstep(0.0, 0.04, along) * (1.0 - pow(along, 1.35));
    fade *= 1.0 - smoothstep(0.55, 1.0, along) * 0.65;

    // crepe: dove il rumore è basso la scia manca, come un filo di fumo
    float breakup = smoothstep(0.20, 0.62, wisps);

    float a = body * fade * breakup * opacity * (1.0 - age) * 1.35;

    vec3 col = mix(smoke.rgb, core.rgb, body * 0.85);
    ALBEDO = col;
    ALPHA = clamp(a, 0.0, 1.0);
    EMISSION = core.rgb * (a * 0.55);
}
