// Falezia/PitturaSchermo — prova dello stile "dipinto" applicato in gioco (doc 29).
//
// Filtro di Kuwahara a quattro settori sullo schermo: in ogni pixel si guarda in quattro quadranti
// e si tiene la media di quello più uniforme. Le superfici diventano pennellate piatte, i bordi
// restano netti (niente sfocatura). È un passo a schermo intero: costa uguale ovunque, non dipende
// dalla scena. Si accende e si spegne dal renderer (Falezia → Calanera → Pittura a schermo).
Shader "Hidden/Falezia/PitturaSchermo"
{
    Properties
    {
        _Radius ("Raggio (px)", Range(1, 6)) = 3
        _Strength ("Forza", Range(0, 1)) = 1
        _EdgeKeep ("Tieni i dettagli forti", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "Pittura"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Radius;
            float _Strength;
            float _EdgeKeep;

            half3 Fetch(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb; }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 px = 1.0 / _ScreenParams.xy;
                int R = (int)clamp(_Radius, 1, 6);

                float3 m[4] = { (float3)0, (float3)0, (float3)0, (float3)0 };
                float  s[4] = { 0, 0, 0, 0 };
                float  n = (R + 1) * (R + 1);

                [loop] for (int j = -R; j <= R; j++)
                {
                    [loop] for (int i = -R; i <= R; i++)
                    {
                        float3 c = Fetch(uv + float2(i, j) * px);
                        float l = dot(c, float3(0.299, 0.587, 0.114));
                        // ogni campione va nei quadranti che lo contengono (gli assi sono in comune)
                        if (i <= 0 && j <= 0) { m[0] += c; s[0] += l * l; }
                        if (i >= 0 && j <= 0) { m[1] += c; s[1] += l * l; }
                        if (i <= 0 && j >= 0) { m[2] += c; s[2] += l * l; }
                        if (i >= 0 && j >= 0) { m[3] += c; s[3] += l * l; }
                    }
                }

                float best = 1e9; float3 col = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float3 mean = m[k] / n;
                    float lm = dot(mean, float3(0.299, 0.587, 0.114));
                    float var = s[k] / n - lm * lm;
                    if (var < best) { best = var; col = mean; }
                }

                float3 orig = Fetch(uv);
                // dove il contrasto è fortissimo (bordi di sagome, testo) si tiene un po' di originale
                float keep = saturate(best * 40.0) * _EdgeKeep;
                float3 painted = lerp(col, orig, keep);
                return half4(lerp(orig, painted, _Strength), 1);
            }
            ENDHLSL
        }
    }
}
