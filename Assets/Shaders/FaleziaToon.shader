// Falezia/Toon — shader da personaggio per un competitivo (stile Valorant).
//
// Cosa fa, e nient'altro:
//   1) TOON A DUE TONALITÀ: la luce principale viene campionata con N·L, resa netta con
//      smoothstep (bordo quasi duro) e moltiplicata per l'attenuazione d'ombra. Due toni:
//      luce (colore della luce) e ombra (tinta configurabile), più ambiente SH.
//   2) RIM LIGHT: Fresnel con potenza, soglia e intensità configurabili; il colore è libero.
//      In ombra il rim si può attenuare (_RimInShadow) per non "incollare" il personaggio.
//   3) FORWARD + MSAA: una sola passata opaca, ZWrite On, nessun clip/alpha test, quindi
//      MSAA funziona; niente deferred, niente decal, niente luci aggiuntive.
//
// Ottimizzato per GTX 970: una texture (albedo), una luce direzionale, nessuna normal map,
// half precision, SRP Batcher (tutte le proprietà nel CBUFFER), GPU instancing.
//
// Uso: assegnare il materiale al corpo/alle braccia dell'operatore; le texture del GLB
// restano la _BaseMap. Le proprietà principali sono esposte in italiano nell'Inspector.
Shader "Falezia/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Colore", Color) = (1,1,1,1)

        [Header(Toon a due tonalita)]
        _ShadowColor ("Tinta dell'ombra", Color) = (0.34, 0.39, 0.52, 1)
        _ShadeStep ("Soglia luce/ombra", Range(-1, 1)) = 0.0
        _ShadeSoftness ("Morbidezza del bordo", Range(0.001, 0.4)) = 0.02
        _AmbientStrength ("Ambiente", Range(0, 2)) = 0.55

        [Header(Rim light)]
        _RimColor ("Colore del rim", Color) = (0.65, 0.85, 1.0, 1)
        _RimIntensity ("Intensità", Range(0, 20)) = 2.0
        _RimPower ("Potenza", Range(1, 16)) = 3.5
        _RimThreshold ("Soglia (dove parte)", Range(0, 1)) = 0.55
        _RimInShadow ("Rim in ombra", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "Queue" = "Geometry"
        }
        LOD 200

        // ------------------------------------------------------------------ luce
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half  _ShadeStep;
                half  _ShadeSoftness;
                half  _AmbientStrength;
                half4 _RimColor;
                half  _RimIntensity;
                half  _RimPower;
                half  _RimThreshold;
                half  _RimInShadow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;

                float3 N = normalize(input.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // --- 1) due tonalità ---
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half ndl = dot(N, light.direction);
                half litMask = smoothstep(_ShadeStep - _ShadeSoftness, _ShadeStep + _ShadeSoftness, ndl);
                half shadow = light.shadowAttenuation * light.distanceAttenuation;
                half lit = litMask * shadow;

                half3 lightTone = light.color * lit;
                half3 shadowTone = _ShadowColor.rgb * (1.0h - lit);
                half3 ambient = SampleSH(N) * _AmbientStrength;
                half3 color = albedo * (lightTone + shadowTone + ambient);

                // --- 2) rim light ---
                half fresnel = 1.0h - saturate(dot(N, V));
                half rim = smoothstep(_RimThreshold, 1.0h, pow(fresnel, _RimPower));
                half rimMask = lerp(_RimInShadow, 1.0h, lit);
                color += _RimColor.rgb * (_RimIntensity * rim * rimMask);

                // --- 3) nebbia; niente alpha, quindi MSAA pulito ---
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ ombre
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V ShadowVert(A input)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                o.positionCS = ApplyShadowClamping(positionCS);
                return o;
            }

            half4 ShadowFrag(V input) : SV_TARGET { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------------ profondità
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            V DepthVert(A input)
            {
                V o = (V)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half DepthFrag(V input) : SV_TARGET { return input.positionCS.z; }
            ENDHLSL
        }

        // ------------------------------------------------------------------ normali (SSAO)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            V DNVert(A input)
            {
                V o = (V)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            void DNFrag(V input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 N = NormalizeNormalPerPixel(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(N);
                outNormalWS = half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
            #else
                outNormalWS = half4(N, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
