// Falezia/Superficie — lo shader master di Calanera (doc 29), semplificato.
//
// Due strati (base + sotto-strato) fusi per altezza, UV in metri, variazione larga e
// sporco al piede. Niente hex-tiling, niente UV3, niente pittura dei vertici, niente
// decal/lightmap/APV: quello che serve alla mappa, con un quarto delle varianti.
// Illuminazione URP standard (UniversalFragmentPBR): ombre morbide, SSAO e nebbia
// arrivano dal motore, non da codice duplicato.
Shader "Falezia/Superficie"
{
    Properties
    {
        [Header(Base L0)]
        [NoScaleOffset] _L0_Albedo ("Albedo", 2D) = "white" {}
        [NoScaleOffset][Normal] _L0_Normal ("Normal", 2D) = "bump" {}
        [NoScaleOffset] _L0_Mask ("Maschera (R met, G occl, B altezza, A lev)", 2D) = "grey" {}
        _L0_Tint ("Tinta", Color) = (1,1,1,1)
        _L0_Tile ("Metri per tessera", Float) = 2.0
        _L0_NormalStrength ("Forza normal", Range(0,2)) = 1.0
        _L0_Smoothness ("Levigatezza ×", Range(0,2)) = 1.0
        _L0_Occlusion ("Occlusione", Range(0,1)) = 1.0

        [Header(Sotto strato L1)]
        [Toggle] _UseLayer1 ("Usa sotto-strato", Float) = 0
        [NoScaleOffset] _L1_Albedo ("Albedo", 2D) = "white" {}
        [NoScaleOffset][Normal] _L1_Normal ("Normal", 2D) = "bump" {}
        [NoScaleOffset] _L1_Mask ("Maschera", 2D) = "grey" {}
        _L1_Tint ("Tinta", Color) = (1,1,1,1)
        _L1_Tile ("Metri per tessera", Float) = 1.2
        _L1_NormalStrength ("Forza normal", Range(0,2)) = 1.0
        _L1_Smoothness ("Levigatezza ×", Range(0,2)) = 1.0
        _L1_Occlusion ("Occlusione", Range(0,1)) = 1.0

        [Header(Danni che scoprono L1)]
        _DamageAmount ("Quantità", Range(0,1)) = 0.25
        _DamageContrast ("Contrasto", Range(1,12)) = 4.0
        _DamageBottom ("Extra al piede", Range(0,1)) = 0.3
        _DamageBottomHeight ("Altezza del piede (m)", Float) = 1.4
        _BlendDepth ("Nitidezza fusione", Range(0.01,0.5)) = 0.1

        [Header(Sporco L2)]
        _GrimeColor ("Colore (moltiplica)", Color) = (0.62,0.6,0.55,1)
        _GrimeAmount ("Quantità", Range(0,1)) = 0.8
        _GrimeHeight ("Altezza (m)", Float) = 0.7
        _GrimeFade ("Sfumatura (m)", Float) = 0.45
        _GrimeNoise ("Irregolarità", Range(0,3)) = 1.2
        _GrimeSmoothness ("Levigatezza dello sporco", Range(0,1)) = 0.1

        [Header(Variazione larga)]
        [NoScaleOffset] _MacroTex ("Rumore (R danni, G luce, B sporco, A tinta)", 2D) = "grey" {}
        _MacroScale ("Scala (m)", Float) = 14.0
        _MacroStrength ("Forza", Range(0,1)) = 0.35
        _MacroTint ("Tinta delle macchie", Color) = (0.95,0.9,0.82,1)

        [Header(Suolo)]
        _GroundY ("Quota del suolo (m)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" }
        LOD 300

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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "SuperficieCore.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
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
                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);
                o.positionWS = vp.positionWS;
                o.positionCS = vp.positionCS;
                o.normalWS = vn.normalWS;
                o.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            void Frag(Varyings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                FaleziaSurface fs = EvaluateSurface(input.positionWS, input.normalWS);

                SurfaceData sd = (SurfaceData)0;
                sd.albedo = fs.albedo;
                sd.metallic = fs.metallic;
                sd.specular = half3(0, 0, 0);
                sd.smoothness = fs.smoothness;
                sd.occlusion = fs.occlusion;
                sd.normalTS = half3(0, 0, 1);
                sd.emission = half3(0, 0, 0);
                sd.alpha = 1.0h;

                InputData id = (InputData)0;
                id.positionWS = input.positionWS;
                id.positionCS = input.positionCS;
                id.normalWS = NormalizeNormalPerPixel(fs.normalWS);
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                id.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                id.shadowCoord = float4(0, 0, 0, 0);
            #endif
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                id.bakedGI = SampleSH(id.normalWS);
                id.shadowMask = half4(1, 1, 1, 1);
                id.vertexLighting = half3(0, 0, 0);
                id.fogCoord = input.fogFactor;

                half4 color = UniversalFragmentPBR(id, sd);
                color.rgb = MixFog(color.rgb, id.fogCoord);
                color.a = 1.0h;
                outColor = color;
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

            #include "SuperficieCore.hlsl"
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
            #include "SuperficieCore.hlsl"

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

        // ------------------------------------------------------------------ normali per SSAO
        // Normale GEOMETRICA (non quella della normal map): l'SSAO lavora sulla forma
        // (angoli, gradini, sotto i cornicioni) e resta pulito, senza rumore di dettaglio.
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
            #include "SuperficieCore.hlsl"
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
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(normalWS);
                outNormalWS = half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
            #else
                outNormalWS = half4(normalWS, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
