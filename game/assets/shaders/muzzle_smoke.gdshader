// Fumo di canna — Calanera
//
// Sbuffo billboard, non illuminato, che si apre e svanisce. Il colore lo dà il tint,
// la forma la dà la texture (smoke_puff.png) più un secondo strato che scorre al
// contrario: il fumo "si apre" invece di gonfiarsi come una palla.
//
// Parametri per istanza (il client ne tiene una copia per ogni sbuffo del pool):
//   age      0 = appena nato, 1 = morto
//   seed     sfasamento del rumore, così due sbuffi non sono identici
//   density  quanto è denso (la Hush ne fa molto meno)
//   tint     colore del fumo
shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_disabled, unshaded, shadows_disabled, fog_disabled;

uniform sampler2D smoke_tex : filter_linear_mipmap, repeat_enable;
uniform vec4 tint : source_color = vec4(0.84, 0.85, 0.87, 1.0);
uniform float age : hint_range(0.0, 1.0) = 0.0;
uniform float seed = 0.0;
uniform float density : hint_range(0.0, 1.0) = 0.6;
uniform float roll : hint_range(-180.0, 180.0) = 0.0;
uniform float scroll_speed : hint_range(0.0, 2.0) = 0.45;
// 1 = tondo, >1 = allungato in verticale: il fumo sale, non è una palla
uniform float stretch : hint_range(0.5, 3.0) = 1.0;
// >1 mangia l'interno lasciando solo i ciuffi: meno "palla", più vapore
uniform float erosion : hint_range(0.5, 3.0) = 1.0;
// ritaglio della texture: cambiando offset e scala la STESSA immagine sembra un'altra
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform float uv_scale : hint_range(0.5, 2.0) = 1.0;
// quanto la parte alta se ne va prima del resto mentre lo sbuffo sale
uniform float rise : hint_range(0.0, 2.0) = 1.0;


void vertex() {
    // Billboard: la faccia guarda sempre la camera. La scala viene dal nodo
    // (lo sbuffo cresce), la rotazione dal parametro roll: due assi indipendenti.
    float sx = length(MODEL_MATRIX[0].xyz);
    float sy = length(MODEL_MATRIX[1].xyz);
    float c = cos(radians(roll));
    float sn = sin(radians(roll));
    vec2 q = mat2(vec2(c, -sn), vec2(sn, c)) * VERTEX.xy;
    VERTEX.x = q.x * sx;
    VERTEX.y = q.y * sy * stretch;

    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        vec4(INV_VIEW_MATRIX[0].xyz, 0.0),
        vec4(INV_VIEW_MATRIX[1].xyz, 0.0),
        vec4(INV_VIEW_MATRIX[2].xyz, 0.0),
        vec4(MODEL_MATRIX[3].xyz, 1.0));

}

void fragment() {
    float t = TIME * scroll_speed;

    // primo strato: la texture base, che sale lentamente
    vec2 uv = (UV - 0.5) * uv_scale + 0.5 + uv_offset;

    vec2 uv1 = uv + vec2(seed * 0.37, -t * 0.35 + age * 0.10);
    vec4 a = texture(smoke_tex, uv1);

    // secondo strato: più piccolo, ruotato e in controluce — serve a rompere la forma
    vec2 uv2 = uv * 1.6 + vec2(seed * 0.11 + 0.43, t * 0.55);
    vec4 b = texture(smoke_tex, uv2);

    float shape = mix(a.a, b.a, 0.42);

    // bordo del quad: si spegne prima di arrivare allo spigolo
    float d = length(UV - 0.5) * 2.0;
    shape *= 1.0 - smoothstep(0.72, 1.0, d);

    // erosione: mangia l'interno del ciuffo, non l'opacità. Prima si dà la FORMA
    // alla texture, poi la densità decide quanto si vede.
    shape = pow(clamp(shape, 0.0, 1.0), erosion);

    // vita: entra in fretta, esce lenta. La parte iniziale è più densa.
    float born = smoothstep(0.0, 0.12, age);
    float die = 1.0 - smoothstep(0.40, 1.0, age);

    // la parte alta se ne va per prima: lo sbuffo si sfilaccia verso il cielo
    // invece di spegnersi tutto insieme
    float top = 1.0 - smoothstep(0.05, 0.95, UV.y);
    float rising = mix(1.0, top, clamp(age * rise, 0.0, 1.0));

    float alpha = shape * born * die * density * rising;

    // un filo di controluce: il fumo non è una macchia piatta
    float rim = smoothstep(0.60, 1.0, d) * 0.10 * born;

    ALBEDO = tint.rgb * (1.0 + rim);
    ALPHA = clamp(alpha, 0.0, 1.0);
}
