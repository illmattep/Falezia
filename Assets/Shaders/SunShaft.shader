// Velo additivo per i fasci di luce: si spegne verso il basso e ai bordi.
Shader "Falezia/SunShaft"
{
    Properties
    {
        _Color ("Colore", Color) = (1, 0.94, 0.78, 0.16)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // si spegne in basso (uv.y=0 è la base) e sfuma sui lati
                half fadeBottom = smoothstep(0.0, 0.55, i.uv.y);
                half fadeSides  = 1.0 - abs(i.uv.x - 0.5) * 2.0;
                fadeSides = fadeSides * fadeSides;
                half a = _Color.a * fadeBottom * fadeSides;
                return half4(_Color.rgb * a, a);
            }
            ENDHLSL
        }
    }
}
