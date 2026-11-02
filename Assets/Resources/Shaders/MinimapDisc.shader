// Minimappa dentro un cerchio.
//
// IMGUI sa ritagliare solo a rettangoli: il cerchio lo fa questo shader. Disegna il
// quadrato che contiene il cerchio e, pixel per pixel:
//   1. fuori dal cerchio: niente;
//   2. dentro: fondo scuro velato, sopra la mappa (ruotata e centrata sul giocatore);
//   3. sul bordo: l'anello chiaro, con un filo scuro all'interno per staccarlo dalla mappa.
//
// La mappa non si ruota con la matrice della GUI: ogni pixel calcola il suo punto del
// mondo da centro + assi (destra/su sullo schermo) e da lì la coordinata texture.
//
// Sta in Resources perché si carica a runtime (Resources.Load): così entra sempre nella
// build, senza liste "Always Included" da tenere aggiornate.
Shader "Falezia/MinimapDisc"
{
    Properties
    {
        _MainTex ("Mappa", 2D) = "black" {}
        _Bg ("Fondo", Color) = (0.03, 0.04, 0.055, 0.62)
        _Ring ("Bordo", Color) = (0.93, 0.95, 0.97, 0.92)
        _RingShade ("Filo interno", Color) = (0, 0, 0, 0.45)
        _RingWidth ("Spessore bordo (frazione del raggio)", Float) = 0.017
        _Radius ("Raggio (metri)", Float) = 100
        _Center ("Centro (X, Z mondo)", Vector) = (0, 0, 0, 0)
        _Axes ("Destra XZ, Su XZ", Vector) = (1, 0, 0, 1)
        _Raster ("Min XZ, dimensioni XZ della texture", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Bg, _Ring, _RingShade;
            float _RingWidth, _Radius;
            float4 _Center, _Axes, _Raster;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;              // -1..1, y in su
                float d = length(p);
                float aa = max(fwidth(d), 1e-4);

                // punto del mondo e coordinata nella texture della mappa
                float2 world = _Center.xy + (p.x * _Axes.xy + p.y * _Axes.zw) * _Radius;
                float2 muv = (world - _Raster.xy) / _Raster.zw;
                float4 m = tex2D(_MainTex, muv);

                // fondo + mappa sopra
                float4 c = _Bg;
                c.rgb = lerp(c.rgb, m.rgb, m.a);
                c.a = m.a + c.a * (1.0 - m.a);

                // filo scuro appena dentro l'anello, poi l'anello
                float inner = 1.0 - _RingWidth;
                float shade = smoothstep(inner - _RingWidth - aa, inner - _RingWidth, d) * _RingShade.a;
                c.rgb = lerp(c.rgb, _RingShade.rgb, shade);
                c.a = c.a + shade * (1.0 - c.a);

                float ring = smoothstep(inner - aa, inner, d) * _Ring.a;
                c.rgb = lerp(c.rgb, _Ring.rgb, ring);
                c.a = c.a + ring * (1.0 - c.a);

                // fuori dal cerchio: niente (bordo antialias)
                c.a *= 1.0 - smoothstep(1.0 - aa, 1.0, d);
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
