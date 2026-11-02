// Scia del proiettile — versione URP del tracer del progetto Godot.
//
// Non è un tubo che si illumina: è gas. Il cilindro resta (costa niente), ma dentro
// c'è rumore che scorre, il centro è più luminoso dei bordi e la punta evapora.
// Sottile e poco opaca: deve leggersi come una scia, non come un laser.
//
// La LineRenderer fornisce uv.x lungo la scia (0 = volata, 1 = impatto) e uv.y di traverso.
Shader "Falezia/Tracer"
{
    Properties
    {
        _NoiseTex ("Rumore", 2D) = "gray" {}
        _CoreColor ("Nucleo", Color) = (1.0, 0.90, 0.66, 1)
        _SmokeColor ("Fumo", Color) = (0.88, 0.90, 0.94, 1)
        _Opacity ("Opacità", Range(0, 1)) = 0.55
        _Speed ("Velocità", Range(0, 8)) = 3
        _Age ("Età", Range(0, 1)) = 0
        _Seed ("Seme", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "Tracer"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                half4 _CoreColor;
                half4 _SmokeColor;
                float _Opacity;
                float _Speed;
                float _Age;
                float _Seed;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float along = saturate(i.uv.x);
                float across = i.uv.y;
                float t = _Time.y * _Speed;

                // due strati di rumore che scorrono: vene larghe + grana fine
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                            float2(along * 0.9 + _Seed, t + _Seed * 3.1)).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                            float2(along * 2.7 - _Seed * 0.7, t * 1.7)).r;
                float wisps = lerp(n1, n2, 0.45);

                // il "vetro" della scia: centro luminoso, bordi evanescenti
                float body = pow(saturate(1.0 - abs(across * 2.0 - 1.0)), 1.8);

                // si spegne verso la punta e non nasce tagliata alla volata
                float fade = smoothstep(0.0, 0.04, along) * (1.0 - pow(along, 1.25));
                fade *= 1.0 - smoothstep(0.55, 1.0, along) * 0.6;

                // dove il rumore è basso la scia manca: filo di fumo, non laser
                float breakup = smoothstep(0.20, 0.62, wisps);

                half a = body * fade * breakup * _Opacity * (1.0 - _Age);
                half3 col = lerp(_SmokeColor.rgb, _CoreColor.rgb, body * 0.85);

                return half4(col, saturate(a)) * i.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
