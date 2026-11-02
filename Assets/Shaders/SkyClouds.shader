// CIELO CON NUVOLE IN MOVIMENTO — skybox procedurale, nessuna texture.
//
// Gradiente zenith→orizzonte, disco solare con alone, due strati di nuvole che scorrono
// lentamente su un piano proiettato (proiezione planare: le nuvole si allontanano verso
// l'orizzonte e si comprimono, come in un cielo vero).
//
// Costa nulla in memoria e anima la scena senza un solo oggetto in più: è il cielo che
// cambia mentre giochi. La copertura e la velocità sono parametri del materiale.
Shader "Skybox/Falezia Nuvole"
{
    Properties
    {
        _Zenith ("Zenit", Color) = (0.16, 0.34, 0.62, 1)
        _Horizon ("Orizzonte", Color) = (0.80, 0.83, 0.84, 1)
        _Ground ("Sotto l'orizzonte", Color) = (0.30, 0.29, 0.27, 1)
        _SunColor ("Sole", Color) = (1.0, 0.93, 0.80, 1)
        _SunDir ("Direzione del sole", Vector) = (0.6, 0.45, 0.0, 0)
        _CloudLight ("Nuvole chiare", Color) = (1.0, 0.99, 0.96, 1)
        _CloudDark ("Nuvole in ombra", Color) = (0.55, 0.58, 0.63, 1)
        _Cover ("Copertura", Range(0, 1)) = 0.52
        _Speed ("Velocità", Float) = 0.010
        _Scale ("Scala", Float) = 2.4
        _Softness ("Morbidezza", Range(0.02, 0.6)) = 0.22
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Zenith, _Horizon, _Ground, _SunColor, _CloudLight, _CloudDark;
            float4 _SunDir;
            float _Cover, _Speed, _Scale, _Softness;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            // ---- rumore 3D sulla sfera: nuvole tonde, senza lo stiramento della proiezione piana
            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float vnoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i + float3(0, 0, 0));
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            float fbm3(float3 p)
            {
                float v = 0.0, amp = 0.55;
                for (int i = 0; i < 3; i++)
                {
                    v += amp * vnoise3(p);
                    p = p * 2.02 + 11.7;
                    amp *= 0.5;
                }
                return v;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float up = dir.y;

                // gradiente del cielo
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, saturate(pow(saturate(up), 0.55)));
                float below = saturate(-up * 3.0);
                sky = lerp(sky, _Ground.rgb, below);

                // sole: disco + alone
                float3 sd = normalize(_SunDir.xyz);
                float d = saturate(dot(dir, sd));
                // disco contenuto: il bianco pieno lo fa il bloom, non la texture
                sky += _SunColor.rgb * pow(d, 1400.0) * 0.95;
                sky += _SunColor.rgb * pow(d, 24.0) * 0.30;
                sky += _SunColor.rgb * pow(d, 4.0) * 0.05;

                // nuvole: rumore 3D sulla direzione, niente proiezione piana
                if (up > 0.015)
                {
                    float3 drift = float3(_Time.y * _Speed * 0.8, 0.0, _Time.y * _Speed * 0.3);
                    float clouds = fbm3(dir * (_Scale * 2.2) + drift);

                    // soglia netta: copertura bassa = poche nuvole, grandi
                    float mask = smoothstep(_Cover - _Softness, _Cover + _Softness, clouds);

                    // si dissolvono all'orizzonte
                    float horizonFade = smoothstep(0.02, 0.26, up);
                    float thickness = smoothstep(0.16, 0.92, saturate((clouds - _Cover) * 2.5 + 0.45));

                    float3 cloudCol = lerp(_CloudDark.rgb, _CloudLight.rgb, thickness);
                    // nucleo chiaro, bordo in ombra: dà volume senza scolpire troppo
                    cloudCol *= 0.80 + 0.20 * thickness;
                    float sunSide = saturate(dot(dir, sd) * 0.5 + 0.5);
                    cloudCol = lerp(cloudCol * 0.90, cloudCol, sunSide);
                    cloudCol += _SunColor.rgb * pow(saturate(dot(dir, sd)), 9.0) * 0.10;   // controluce

                    sky = lerp(sky, cloudCol, mask * horizonFade * 0.96);

                    // banda chiara appena sopra l'orizzonte: stacco grafico, non foschia
                    float band = smoothstep(0.0, 0.12, up) * (1.0 - smoothstep(0.12, 0.30, up));
                    sky = lerp(sky, _Horizon.rgb * 1.04, band * 0.45);
                }

                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
