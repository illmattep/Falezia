using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// I PRESET DI QUALITÀ. Un numero solo che muove tutte le manopole che contano davvero,
/// in ordine di costo: risoluzione di rendering, ombre, antialiasing, LOD, texture, cielo.
///
/// Il principio: **la geometria e le texture non si toccano** (l'utente ha chiesto qualità
/// alta prima di tutto). Si abbassano invece le cose che si pagano ogni fotogramma —
/// risoluzione, ombre, AA, distanza degli oggetti — così un portatile regge la stessa mappa.
/// </summary>
public static class QualityPresets
{
    public static readonly string[] Names = { "BASSO", "MEDIO", "ALTO", "ULTRA" };

    public static int Current { get; private set; } = 2;

    /// <summary>Oltre questa distanza i prop non vengono creati (0 = tutti).</summary>
    public static float PropDistance { get; private set; } = 0f;

    /// <summary>Moltiplicatore di LOD: più alto = mesh dettagliate più lontano.</summary>
    public static float LodBias { get; private set; } = 1.5f;

    public static void Apply(int tier, bool verbose = false)
    {
        Current = Mathf.Clamp(tier, 0, 3);
        var urp = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;

        switch (Current)
        {
            case 0:   // BASSO — portatili, GPU integrata
                Set(urp, 0.72f, 45f, ShadowResolution.Medium, 0, 0.8f);
                PropDistance = 70f;
                break;

            case 1:   // MEDIO
                Set(urp, 0.85f, 65f, ShadowResolution.High, 2, 1.0f);
                PropDistance = 110f;
                break;

            case 2:   // ALTO (predefinito)
                Set(urp, 1.0f, 90f, ShadowResolution.VeryHigh, 4, 1.5f);
                PropDistance = 0f;
                break;

            default:  // ULTRA
                Set(urp, 1.0f, 130f, ShadowResolution.VeryHigh, 8, 2.2f);
                PropDistance = 0f;
                break;
        }

        GraphicsQuality.Apply(Current);

        if (verbose) Debug.Log($"Qualità: {Names[Current]} · ombre {QualitySettings.shadowDistance} m · " +
                               $"AA {QualitySettings.antiAliasing}× · LOD {LodBias} · " +
                               $"prop entro {(PropDistance <= 0 ? "tutta la mappa" : PropDistance + " m")}");
    }

    private static void Set(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp,
                            float renderScale, float shadowDistance, ShadowResolution shadowRes,
                            int aa, float lodBias)
    {
        if (urp != null) urp.renderScale = renderScale;

        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.shadowResolution = shadowRes;
        QualitySettings.shadowCascades = shadowDistance > 100f ? 4 : 2;
        QualitySettings.antiAliasing = aa;
        QualitySettings.lodBias = lodBias;
        LodBias = lodBias;

        // La nebbia NON si tocca: è atmosfera (GameRoot.BuildLight, 70→330 m).
        // Il vecchio preset passava 0 come "tutta la mappa" e la applicava alla nebbia:
        // con fogEnd = 0 tutto diventava bianco al primo cambio di impostazioni.
    }
}
