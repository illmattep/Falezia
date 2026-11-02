// Falezia/Neon — insegna al neon (testo o tubi). Unlit, HDR, alpha dal font.
//
// Il TextMesh disegna i glifi con l'atlante del font: qui si campiona quell'atlante,
// si moltiplica per un colore HDR e si lascia che il bloom faccia il resto. Nessuna luce,
// nessuna ombra: l'insegna È la luce. `_Intensity` viene pilotata per lettera da NeonSign
// (MaterialPropertyBlock), così qualcuna può sfarfallare senza toccare le altre.
Shader "Falezia/Neon"
{
    Properties
    {
        [MainTexture] _MainTex ("Atlante del font", 2D) = "white" {}
        [HDR] _Color ("Colore neon (HDR)", Color) = (3.5, 1.1, 0.35, 1)
        _Intensity ("Intensità", Float) = 1
        _Alpha ("Opacità", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Neon"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
                half _Alpha;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            V Vert(A i)
            {
                V o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color;
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = tex.a * _Color.a * _Alpha * i.color.a;
                clip(a - 0.01h);
                // il glifo è nell'alpha dell'atlante: il colore è tutto HDR
                half3 neon = _Color.rgb * _Intensity * i.color.rgb * a;
                return half4(neon, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
