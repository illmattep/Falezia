using System.Collections.Generic;
using UnityEngine;
#if FALEZIA_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// IMPOSTAZIONI GRAFICHE E DI CONTROLLO.
///
/// Tutto quello che si può cambiare sta qui, in un posto solo: risoluzione, modalità
/// finestra, qualità, v-sync, anti-aliasing, ombre, scala di rendering, campo visivo e
/// sensibilità. I valori si salvano in PlayerPrefs e si riapplicano all'avvio.
///
/// Regola: la risoluzione minima è **1280×720**. Sotto non ha senso per un tattico 5v5,
/// e le voci più piccole vengono filtrate anche quando il monitor le dichiara.
/// </summary>
public static class GameSettings
{
    public const int MinWidth = 1280;
    public const int MinHeight = 720;

    public static readonly List<Vector2Int> Modes = new();
    public static int ModeIndex;
    public static bool Fullscreen = true;
    public static bool VSync = true;
    public static int Quality;          // indice in QualitySettings.names
    public static int AntiAliasing = 4; // 0, 2, 4, 8
    public static int Shadows = 3;      // 0 off · 1 basse · 2 medie · 3 alte
    public static float RenderScale = 1f;
    public static int Fov = 85;
    public static int Sensitivity = 100; // percentuale
    public static int MasterVolume = 100; // percentuale, 0 = muto

    /// <summary>Preset di qualità: 0 BASSO · 1 MEDIO · 2 ALTO · 3 ULTRA. Vedi QualityPresets.</summary>
    public static int Preset = 2;

    /// <summary>Zoom della minimappa sul giocatore: le tre scelte, in percentuale.</summary>
    public static readonly int[] MinimapZoomSteps = { 20, 40, 60 };
    public static int MinimapZoom = 1;    // indice in MinimapZoomSteps (40 %)
    public static int MinimapZoomPercent => MinimapZoomSteps[Mathf.Clamp(MinimapZoom, 0, MinimapZoomSteps.Length - 1)];
    /// <summary>1,2 · 1,4 · 1,6: quanto si ingrandisce la mappa rispetto a "tutta nel riquadro".</summary>
    public static float MinimapZoomFactor => 1f + MinimapZoomPercent / 100f;

    /// <summary>Abbellimenti della mappa (erba, finestre, cornici…). Solo aspetto: spegnerli non cambia niente nel gioco.</summary>
    public static bool MapDetail = true;

    private static bool _loaded;

    public static string ShadowName => Shadows switch
    {
        0 => "OFF",
        1 => "BASSE",
        2 => "MEDIE",
        _ => "ALTE",
    };

    public static string AaName => AntiAliasing == 0 ? "OFF" : AntiAliasing + "×";

    public static Vector2Int Mode => Modes.Count > 0 ? Modes[Mathf.Clamp(ModeIndex, 0, Modes.Count - 1)] : new Vector2Int(1600, 900);

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        BuildModes();

        Fullscreen = PlayerPrefs.GetInt("fx.fullscreen", 1) == 1;
        VSync = PlayerPrefs.GetInt("fx.vsync", 1) == 1;
        Quality = PlayerPrefs.GetInt("fx.quality", Mathf.Max(0, QualitySettings.names.Length - 2));
        AntiAliasing = PlayerPrefs.GetInt("fx.aa", 4);
        Shadows = PlayerPrefs.GetInt("fx.shadows", 3);
        RenderScale = PlayerPrefs.GetFloat("fx.scale", 1f);
        Fov = PlayerPrefs.GetInt("fx.fov", 85);
        Sensitivity = PlayerPrefs.GetInt("fx.sens", 100);
        MasterVolume = PlayerPrefs.GetInt("fx.volume", 100);
        Preset = Mathf.Clamp(PlayerPrefs.GetInt("fx.preset", 2), 0, 3);
        MinimapZoom = Mathf.Clamp(PlayerPrefs.GetInt("hud.minimapZoom", 1), 0, MinimapZoomSteps.Length - 1);
        MapDetail = PlayerPrefs.GetInt("fx.mapDetail", 1) == 1;

        int w = PlayerPrefs.GetInt("fx.w", 1600);
        int h = PlayerPrefs.GetInt("fx.h", 900);
        ModeIndex = Mathf.Max(0, Modes.FindIndex(m => m.x == w && m.y == h));
    }

    public static void Save()
    {
        var m = Mode;
        PlayerPrefs.SetInt("fx.fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("fx.vsync", VSync ? 1 : 0);
        PlayerPrefs.SetInt("fx.quality", Quality);
        PlayerPrefs.SetInt("fx.aa", AntiAliasing);
        PlayerPrefs.SetInt("fx.shadows", Shadows);
        PlayerPrefs.SetFloat("fx.scale", RenderScale);
        PlayerPrefs.SetInt("fx.fov", Fov);
        PlayerPrefs.SetInt("fx.sens", Sensitivity);
        PlayerPrefs.SetInt("fx.volume", MasterVolume);
        PlayerPrefs.SetInt("fx.preset", Preset);
        PlayerPrefs.SetInt("hud.minimapZoom", MinimapZoom);
        PlayerPrefs.SetInt("fx.mapDetail", MapDetail ? 1 : 0);
        PlayerPrefs.SetInt("fx.w", m.x);
        PlayerPrefs.SetInt("fx.h", m.y);
        PlayerPrefs.Save();
    }

    private static Vector2Int _lastScreen;
    private static FullScreenMode _lastMode;
    private static bool _hasScreen;

    public static void Apply()
    {
        var m = Mode;
        m.x = Mathf.Max(MinWidth, m.x);
        m.y = Mathf.Max(MinHeight, m.y);

        // La risoluzione si tocca SOLO quando cambia davvero: ogni Apply (AA, ombre,
        // preset, volume...) la reimpostava, e su Linux il cambio di mode a metà partita
        // poteva lasciare il backbuffer bianco. Stesso discorso per il livello qualità.
        var target = Fullscreen ? NativeSize : m;
        var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (!_hasScreen || _lastScreen != target || _lastMode != mode)
        {
            Screen.SetResolution(target.x, target.y, mode);
            _lastScreen = target;
            _lastMode = mode;
            _hasScreen = true;
        }
        QualitySettings.vSyncCount = VSync ? 1 : 0;

        if (QualitySettings.names.Length > 0)
        {
            int q = Mathf.Clamp(Quality, 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != q)
                QualitySettings.SetQualityLevel(q, true);
        }

        // il volume è globale: l'audio delle armi non ha un mixer suo (sarebbe un asset
        // in più da mantenere per un gioco che ha un solo tipo di suono)
        AudioListener.volume = Mathf.Clamp01(MasterVolume / 100f);

        QualitySettings.antiAliasing = AntiAliasing;
        QualitySettings.shadows = Shadows == 0 ? UnityEngine.ShadowQuality.Disable : UnityEngine.ShadowQuality.All;
        QualitySettings.shadowDistance = Shadows switch { 1 => 45f, 2 => 70f, _ => 90f };
        QualitySettings.shadowResolution = Shadows >= 3 ? UnityEngine.ShadowResolution.VeryHigh : UnityEngine.ShadowResolution.Medium;

#if FALEZIA_URP
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            urp.renderScale = Mathf.Clamp(RenderScale, 0.5f, 1f);
#endif
        QualityPresets.Apply(Preset);
    }

    /// <summary>Risoluzione nativa del monitor (quella del desktop).</summary>
    public static Vector2Int NativeSize
    {
        get
        {
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (w <= 0 || h <= 0) { w = Screen.currentResolution.width; h = Screen.currentResolution.height; }
            return new Vector2Int(Mathf.Max(MinWidth, w), Mathf.Max(MinHeight, h));
        }
    }

    public static void StepResolution(int dir)
    {
        if (Fullscreen) return;            // a schermo intero la risoluzione è quella nativa
        if (Modes.Count == 0) return;
        ModeIndex = Mathf.Clamp(ModeIndex + dir, 0, Modes.Count - 1);
        Apply();
        Save();
    }

    /// <summary>Risoluzioni del monitor, senza doppioni e mai sotto 1280×720.</summary>
    private static void BuildModes()
    {
        Modes.Clear();
        var seen = new HashSet<long>();

        foreach (var r in Screen.resolutions)
        {
            if (r.width < MinWidth || r.height < MinHeight) continue;
            if (!seen.Add((long)r.width * 100000 + r.height)) continue;
            Modes.Add(new Vector2Int(r.width, r.height));
        }

        if (Modes.Count == 0)
            foreach (var m in new[]
            {
                new Vector2Int(1280, 720), new Vector2Int(1366, 768), new Vector2Int(1600, 900),
                new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160),
            })
                Modes.Add(m);

        Modes.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
    }
}
