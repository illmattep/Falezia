Shader "Falezia/Acqua"
{
    Properties
    {
        _DeepColor ("Deep", Color) = (0.010, 0.055, 0.070, 1)
        _ShallowColor ("Shallow", Color) = (0.045, 0.165, 0.185, 1)
        _NormalA ("Normal A", 2D) = "bump" {}
        _NormalB ("Normal B", 2D) = "bump" {}
        _ScrollA ("Scroll A", Vector) = (0.020, 0.011, 0, 0)
        _ScrollB ("Scroll B", Vector) = (-0.014, 0.016, 0, 0)
        _NormalStrength ("Normal Strength", Range(0, 1.5)) = 0.55
        _Smoothness ("Smoothness", Range(0, 1)) = 0.55
        _FresnelPower ("Fresnel Power", Range(1, 8)) = 5.0
        _FoamColor ("Foam", Color) = (0.92, 0.96, 0.96, 1)
        _FoamDepth ("Foam Depth", Float) = 2.2
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.8
        _SunGlint ("Riflesso del sole", Range(0, 2)) = 0.9
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _NormalA_ST;
                float4 _NormalB_ST;
                float4 _ScrollA;
                float4 _ScrollB;
                float _NormalStrength;
                float _Smoothness;
                float _FresnelPower;
                float4 _FoamColor;
                float _FoamDepth;
                float _FoamStrength;
                float _SunGlint;
            CBUFFER_END

            TEXTURE2D(_NormalA); SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB); SAMPLER(sampler_NormalB);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                float3 wp = p.positionWS;
                // onde 3D: due seni incrociati, ampiezza piccola (mare di porto)
                float w = sin(wp.x * 0.42 + _Time.y * 1.1) * 0.055
                        + sin(wp.z * 0.55 - _Time.y * 0.9) * 0.045
                        + sin((wp.x + wp.z) * 0.21 + _Time.y * 0.6) * 0.035;
                wp.y += w;
                p.positionCS = TransformWorldToHClip(wp);
                o.positionCS = p.positionCS;
                o.positionWS = wp;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.screenPos = ComputeScreenPos(p.positionCS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvA = input.uv * _NormalA_ST.xy + _NormalA_ST.zw + _Time.y * _ScrollA.xy;
                float2 uvB = input.uv * _NormalB_ST.xy + _NormalB_ST.zw + _Time.y * _ScrollB.xy;
                float3 nA = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uvA), _NormalStrength);
                float3 nB = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uvB), _NormalStrength);
                float3 normalTS = normalize(float3(nA.xy + nB.xy, nA.z * nB.z));

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                // normale perturbata dalle due mappe: il sole ci fa un lustrino vero
                float3 nWS = normalize(input.normalWS + float3(normalTS.x, 0.0, normalTS.y) * _NormalStrength);
                float fresnel = pow(saturate(1.0 - dot(nWS, viewDir)), _FresnelPower);
                float3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(viewDir.y * 1.4 + 0.35));

                // schiuma dove l'acqua incontra la geometria (banchina, molo)
                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUv), _ZBufferParams);
                float waterDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDiff = saturate((sceneDepth - waterDepth) / max(0.001, _FoamDepth));
                float foamMask = (1.0 - depthDiff) * _FoamStrength;
                foamMask *= saturate(0.55 + 0.45 * sin(input.positionWS.x * 2.1 + _Time.y * 1.6) * sin(input.positionWS.z * 1.7 - _Time.y * 1.2));
                color = lerp(color, _FoamColor.rgb, saturate(foamMask) * 0.7);

                // lustrino del sole: la firma dell'acqua di Valorant, e costa due istruzioni
                Light sun = GetMainLight();
                float3 h = normalize(sun.direction + viewDir);
                float glint = pow(saturate(dot(nWS, h)), 140.0) * _SunGlint;
                color += sun.color * glint;

                // acqua stilizzata: colore + fresnel verso il cielo, niente PBR/sonda
                float3 skyTint = float3(0.52, 0.70, 0.85);
                float3 finalColor = lerp(color, skyTint, saturate(fresnel) * 0.60);
                finalColor = lerp(finalColor, _FoamColor.rgb, saturate(foamMask) * 0.7);
                finalColor = MixFog(finalColor, ComputeFogFactor(input.positionCS.z) * 0.55);
                return half4(finalColor, 1.0);

            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
