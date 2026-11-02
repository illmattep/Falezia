using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LE MANOPOLE GRAFICHE CHE CONTANO, legate al preset (BASSO/MEDIO/ALTO/ULTRA).
///
/// Il preset non tocca la geometria: scala ombre (risoluzione, distanza, cascate, morbidezza),
/// SSAO (acceso/spento e qualità fissa alta), bloom e grading, antialiasing. I valori di
/// partenza stanno negli asset (URP, renderer, volume); qui si applicano a runtime, così
/// cambiando preset nel menu cambia tutto senza riavviare.
///
/// L'SSAO è tarato una volta per la qualità: 16 campioni, blur alto, normali geometriche.
/// Su BASSO si spegne (è il costo per-pixel più caro), sopra resta acceso.
/// </summary>
public static class GraphicsQuality
{
    public static int Level { get; private set; } = 2;

    public static void Apply(int preset)
    {
        Level = Mathf.Clamp(preset, 0, 3);

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = Level switch { 0 => 0.72f, 1 => 0.85f, _ => 1f };
            urp.shadowDistance = Level switch { 0 => 45f, 1 => 70f, 2 => 110f, _ => 130f };
            urp.shadowCascadeCount = Level == 0 ? 2 : 4;
            urp.mainLightShadowmapResolution = Level switch { 0 => 2048, 1 => 2048, 2 => 4096, _ => 8192 };
            SetField(urp, "m_SoftShadowsSupported", true);
            SetField(urp, "m_SoftShadowQuality", Level >= 2 ? 3 : Level == 1 ? 2 : 1);
            Ssao(urp, Level > 0);
        }

        QualitySettings.shadowDistance = Level switch { 0 => 45f, 1 => 70f, 2 => 110f, _ => 130f };
        QualitySettings.shadowCascades = Level == 0 ? 2 : 4;
        QualitySettings.shadowResolution = Level >= 2 ? UnityEngine.ShadowResolution.VeryHigh : UnityEngine.ShadowResolution.High;
        QualitySettings.shadowCascade4Split = new Vector3(0.05f, 0.15f, 0.35f);
        QualitySettings.antiAliasing = Level == 0 ? 0 : Level == 1 ? 2 : 4;

        Post(Level);
    }

    private static void Ssao(UniversalRenderPipelineAsset urp, bool on)
    {
        var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field?.GetValue(urp) is not ScriptableRendererData[] list) return;
        foreach (var data in list)
        {
            if (data == null) continue;
            foreach (var f in data.rendererFeatures)
                if (f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                    f.SetActive(on);
        }
    }

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return;
        f.SetValue(target, f.FieldType.IsEnum ? Enum.ToObject(f.FieldType, value) : value);
    }

    /// <summary>Bloom basso, colore meno saturo e più neutro: il giallo non deve venire dal grading.</summary>
    private static void Post(int level)
    {
        var profile = Resources.Load<VolumeProfile>("Falezia-Volume");
        if (profile == null) return;

        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.intensity.Override(level switch { 0 => 0.03f, 1 => 0.05f, 2 => 0.06f, _ => 0.08f });
            bloom.threshold.Override(level switch { 0 => 1.45f, 1 => 1.40f, 2 => 1.35f, _ => 1.30f });
            bloom.highQualityFiltering.Override(level >= 2);
        }
        if (profile.TryGet<ColorAdjustments>(out var ca))
        {
            ca.saturation.Override(level switch { 0 => 3f, 1 => 4f, 2 => 4f, _ => 5f });
            ca.contrast.Override(level switch { 0 => 8f, 1 => 9f, 2 => 10f, _ => 11f });
            ca.postExposure.Override(level switch { 0 => -0.05f, 1 => -0.12f, 2 => -0.15f, _ => -0.18f });
        }
        if (profile.TryGet<WhiteBalance>(out var wb))
            wb.temperature.Override(level switch { 0 => -8f, 1 => -10f, 2 => -12f, _ => -12f });
    }
}
