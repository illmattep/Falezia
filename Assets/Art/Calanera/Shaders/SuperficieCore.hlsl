// Falezia/Superficie — il cuore condiviso da tutte le passate.
//
// Due strati, una regola:
//   L0  base         (intonaco, lastre, coppi, lamiera...)
//   L1  sotto-strato (il mattone sotto l'intonaco, la pietra fra le fughe)
//
// L0 e L1 si fondono per ALTEZZA (maschera B) e per il rumore del mondo: l'intonaco
// cade dove il sotto-strato è più alto, quindi il bordo segue le forme e non è una
// sfumatura piatta. Sopra, sporco al piede dei muri e variazione larga dal mondo.
//
// UV dal mondo (metri): la texture non si stira mai e due volumi affiancati continuano.
// Niente hex-tiling, niente pittura dei vertici, niente UV3: la stessa resa con un
// quarto del lavoro per pixel. Le normali per l'SSAO sono geometriche (forma pulita).
//
// Texture per strato (convenzione della libreria, doc 29):
//   _Lx_Albedo  sRGB
//   _Lx_Normal  normal map (OpenGL, verde = +V)
//   _Lx_Mask    lineare: R metallo · G occlusione · B altezza · A levigatezza

#ifndef FALEZIA_SUPERFICIE_CORE
#define FALEZIA_SUPERFICIE_CORE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_L0_Albedo);  SAMPLER(sampler_L0_Albedo);
TEXTURE2D(_L0_Normal);
TEXTURE2D(_L0_Mask);
TEXTURE2D(_L1_Albedo);  SAMPLER(sampler_L1_Albedo);
TEXTURE2D(_L1_Normal);
TEXTURE2D(_L1_Mask);
TEXTURE2D(_MacroTex);   SAMPLER(sampler_MacroTex);

CBUFFER_START(UnityPerMaterial)
    float4 _L0_Tint;
    float  _L0_Tile;
    float  _L0_NormalStrength;
    float  _L0_Smoothness;
    float  _L0_Occlusion;

    float4 _L1_Tint;
    float  _L1_Tile;
    float  _L1_NormalStrength;
    float  _L1_Smoothness;
    float  _L1_Occlusion;

    float  _UseLayer1;
    float  _DamageAmount;
    float  _DamageContrast;
    float  _DamageBottom;
    float  _DamageBottomHeight;
    float  _BlendDepth;

    float4 _GrimeColor;
    float  _GrimeAmount;
    float  _GrimeHeight;
    float  _GrimeFade;
    float  _GrimeNoise;
    float  _GrimeSmoothness;

    float  _MacroScale;
    float  _MacroStrength;
    float4 _MacroTint;

    float  _GroundY;
CBUFFER_END

// ------------------------------------------------------------------ UV dal mondo
// (T, B, N) destrorso per ogni faccia: la normal map illumina giusta su tutti i lati.
void WorldPlanarUV(float3 positionWS, float3 normalGeoWS, out float2 uv, out float3 T, out float3 B)
{
    float3 a = abs(normalGeoWS);
    if (a.y >= a.x && a.y >= a.z)
    {
        float s = normalGeoWS.y >= 0 ? 1.0 : -1.0;
        uv = float2(positionWS.x, -s * positionWS.z);
        T = float3(1, 0, 0); B = float3(0, 0, -s);
    }
    else if (a.x >= a.z)
    {
        float s = normalGeoWS.x >= 0 ? 1.0 : -1.0;
        uv = float2(-s * positionWS.z, positionWS.y);
        T = float3(0, 0, -s); B = float3(0, 1, 0);
    }
    else
    {
        float s = normalGeoWS.z >= 0 ? 1.0 : -1.0;
        uv = float2(s * positionWS.x, positionWS.y);
        T = float3(s, 0, 0); B = float3(0, 1, 0);
    }
}

// Rumore del mondo: pareti (x+z, y), suolo (x, z).
float2 MacroUV(float3 positionWS, float3 normalGeoWS)
{
    return abs(normalGeoWS.y) > 0.6 ? positionWS.xz : float2(positionWS.x + positionWS.z, positionWS.y);
}

half3 UnpackN(half4 packed, half strength)
{
    half3 n;
    n.xy = packed.xy * 2.0h - 1.0h;
    n.xy *= strength;
    n.z = sqrt(saturate(1.0h - dot(n.xy, n.xy)));
    return n;
}

struct Layer
{
    half3 albedo;
    half3 normalTS;
    half4 mask;
};

Layer SampleLayer(TEXTURE2D_PARAM(albedoTex, samp), TEXTURE2D_PARAM(normalTex, sampN), TEXTURE2D_PARAM(maskTex, sampM),
                  float2 uvMeters, float tile, float normalStrength)
{
    Layer o;
    float2 uv = uvMeters / max(tile, 0.01);
    o.albedo = SAMPLE_TEXTURE2D(albedoTex, samp, uv).rgb;
    o.normalTS = UnpackN(SAMPLE_TEXTURE2D(normalTex, sampN, uv), normalStrength);
    o.mask = SAMPLE_TEXTURE2D(maskTex, sampM, uv);
    return o;
}

struct FaleziaSurface
{
    half3 albedo;
    half3 normalWS;
    half  metallic;
    half  smoothness;
    half  occlusion;
};

FaleziaSurface EvaluateSurface(float3 positionWS, float3 normalGeoWS)
{
    FaleziaSurface s;
    normalGeoWS = normalize(normalGeoWS);

    float2 uvM; float3 T, B;
    WorldPlanarUV(positionWS, normalGeoWS, uvM, T, B);

    // rumore del mondo: R danni · G luminosità · B sporco · A tinta
    float2 mUV = MacroUV(positionWS, normalGeoWS);
    half4 macro = SAMPLE_TEXTURE2D(_MacroTex, sampler_MacroTex, mUV / max(_MacroScale, 0.1));

    Layer l0 = SampleLayer(TEXTURE2D_ARGS(_L0_Albedo, sampler_L0_Albedo), TEXTURE2D_ARGS(_L0_Normal, sampler_L0_Albedo),
                           TEXTURE2D_ARGS(_L0_Mask, sampler_L0_Albedo), uvM, _L0_Tile, _L0_NormalStrength);
    l0.albedo *= _L0_Tint.rgb;
    l0.mask.a *= _L0_Smoothness;
    l0.mask.g = lerp(1.0h, l0.mask.g, _L0_Occlusion);

    half3 albedo = l0.albedo;
    half3 nTS = l0.normalTS;
    half4 mask = l0.mask;
    float heightAbove = positionWS.y - _GroundY;

    if (_UseLayer1 > 0.5)
    {
        Layer l1 = SampleLayer(TEXTURE2D_ARGS(_L1_Albedo, sampler_L1_Albedo), TEXTURE2D_ARGS(_L1_Normal, sampler_L1_Albedo),
                               TEXTURE2D_ARGS(_L1_Mask, sampler_L1_Albedo), uvM, _L1_Tile, _L1_NormalStrength);
        l1.albedo *= _L1_Tint.rgb;
        l1.mask.a *= _L1_Smoothness;
        l1.mask.g = lerp(1.0h, l1.mask.g, _L1_Occlusion);

        // il sotto-strato si vede per rumore del mondo e per il piede del muro
        float damage = saturate((macro.r - (1.0 - _DamageAmount)) * _DamageContrast);
        damage += _DamageBottom * saturate(1.0 - heightAbove / max(_DamageBottomHeight, 0.01));
        damage = saturate(damage);

        float a = l0.mask.b * 0.6 + (1.0 - damage);
        float b = l1.mask.b * 0.6 + damage;
        float ma = max(a, b) - max(_BlendDepth, 0.001);
        float wa = max(a - ma, 0.0), wb = max(b - ma, 0.0);
        half w0 = (half)(wa / max(wa + wb, 1e-5));

        albedo = lerp(l1.albedo, l0.albedo, w0);
        nTS = normalize(lerp(l1.normalTS, l0.normalTS, w0));
        mask = lerp(l1.mask, l0.mask, w0);
    }

    // variazione larga: mai una superficie identica per metri
    albedo *= 1.0h + _MacroStrength * (macro.g - 0.5h) * 0.5h;
    albedo = lerp(albedo, albedo * _MacroTint.rgb, saturate(macro.a * 1.4h - 0.2h) * _MacroStrength);

    // sporco al piede dei muri
    float grime = saturate((_GrimeHeight - heightAbove) / max(_GrimeFade, 0.01) + (macro.b - 0.5) * _GrimeNoise);
    grime *= _GrimeAmount;
    albedo = lerp(albedo, albedo * _GrimeColor.rgb, grime);
    mask.a = lerp(mask.a, _GrimeSmoothness, grime * 0.6);

    float3x3 tbn = float3x3(T, B, normalGeoWS);
    s.normalWS = normalize(mul(nTS, tbn));
    s.albedo = saturate(albedo);
    s.metallic = mask.r;
    s.smoothness = mask.a;
    s.occlusion = mask.g;
    return s;
}

#endif
