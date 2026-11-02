using Godot;

/// <summary>
/// Impostazioni grafiche e di visualizzazione (doc 06: "limite di FPS + vsync + modalità
/// a schermo" sono non negoziabili; doc 07: i preset PRO, BASSO, ALTO).
///
/// Un solo posto: i valori, il salvataggio su user://settings.cfg, e l'applicazione.
/// Le scene si iscrivono a <see cref="Changed"/> e riapplicano ciò che le riguarda
/// (luce, ombre, foschia, camera). Lo stato globale (finestra, vsync, FPS) lo applica
/// questa classe da sola.
/// </summary>
public static class GraphicsSettings
{
    public enum Preset { Pro, Low, High, Custom }

    // ------------------------------------------------------------------ valori

    public static Preset CurrentPreset = Preset.High;

    /// <summary>0 finestra · 1 schermo intero (senza bordi) · 2 schermo intero esclusivo</summary>
    public static int WindowMode = 0;

    /// <summary>0 disattivato · 1 attivo · 2 adattivo · 3 mailbox</summary>
    public static int VSync = 0;

    /// <summary>Limite FPS. 0 = illimitato.</summary>
    public static int MaxFps = 0;

    /// <summary>
    /// Bassa latenza. Godot non integra NVIDIA Reflex (serve l'SDK NVIDIA dentro il motore):
    /// questa è l'alternativa che si può fare da qui. Vedi <see cref="LatencyGovernor"/>.
    /// </summary>
    public static bool LowLatency = true;

    /// <summary>0 nessuno · 1 FXAA · 2 SMAA · 3 MSAA 2× · 4 MSAA 4× · 5 TAA</summary>
    public static int AntiAliasing = 1;

    /// <summary>0 spente · 1 basse · 2 alte</summary>
    public static int Shadows = 2;

    public static bool Ssao = true;
    public static bool Bloom = true;

    /// <summary>Scala di rendering 3D, 0,5–1,0. Sotto 1 usa FSR 1 dove il renderer lo permette.</summary>
    public static float RenderScale = 1.0f;

    /// <summary>Distanza di visibilità (foschia + piano lontano), metri: 150 · 250 · 400.</summary>
    public static int ViewDistance = 400;

    public static float Fov = 85f;

    public static event System.Action Changed;

    public static readonly int[] FpsChoices = { 0, 30, 60, 75, 120, 144, 165, 240, 360 };
    public static readonly int[] ViewChoices = { 150, 250, 400 };

    private const string SettingsPath = "user://settings.cfg";
    private const string Section = "grafica";
    private static bool _loaded;

    // ------------------------------------------------------------------ preset

    /// <summary>
    /// I preset del doc 07. PRO "disattiva tutto": è ufficiale, così nessuno lo deve
    /// ricostruire a mano. Il preset NON tocca finestra, vsync, FPS e FOV: sono
    /// preferenze personali, non qualità.
    /// </summary>
    public static void ApplyPreset(Preset p)
    {
        switch (p)
        {
            case Preset.Pro:
                AntiAliasing = 0; Shadows = 0; Ssao = false; Bloom = false;
                RenderScale = 1f; ViewDistance = 150;
                break;
            case Preset.Low:
                AntiAliasing = 1; Shadows = 0; Ssao = false; Bloom = false;
                RenderScale = 1f; ViewDistance = 150;
                break;
            case Preset.High:
                AntiAliasing = 1; Shadows = 2; Ssao = true; Bloom = true;
                RenderScale = 1f; ViewDistance = 400;
                break;
        }
        CurrentPreset = p;
    }

    /// <summary>
    /// True se la UI può usare il blur dietro i pannelli. Con i preset "competitivi"
    /// (PRO, BASSO) la UI resta trasparente: costa una copia dello schermo per pannello.
    /// </summary>
    public static bool UiBlur => CurrentPreset is Preset.High;

    public static string PresetName(Preset p) => p switch
    {
        Preset.Pro => "PRO",
        Preset.Low => "BASSO",
        Preset.High => "ALTO",
        _ => "PERSONALIZZATO"
    };

    // ------------------------------------------------------------------ file

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok) return;   // primo avvio: restano i valori di default

        CurrentPreset = (Preset)Mathf.Clamp(cfg.GetValue(Section, "preset", (int)CurrentPreset).AsInt32(), 0, 3);
        WindowMode = Mathf.Clamp(cfg.GetValue(Section, "finestra", WindowMode).AsInt32(), 0, 2);
        VSync = Mathf.Clamp(cfg.GetValue(Section, "vsync", VSync).AsInt32(), 0, 3);
        MaxFps = Mathf.Clamp(cfg.GetValue(Section, "max_fps", MaxFps).AsInt32(), 0, 1000);
        LowLatency = cfg.GetValue(Section, "bassa_latenza", LowLatency).AsBool();
        AntiAliasing = Mathf.Clamp(cfg.GetValue(Section, "antialiasing", AntiAliasing).AsInt32(), 0, 5);
        Shadows = Mathf.Clamp(cfg.GetValue(Section, "ombre", Shadows).AsInt32(), 0, 2);
        Ssao = cfg.GetValue(Section, "ssao", Ssao).AsBool();
        Bloom = cfg.GetValue(Section, "bloom", Bloom).AsBool();
        RenderScale = Mathf.Clamp(cfg.GetValue(Section, "scala_3d", RenderScale).AsSingle(), 0.5f, 1f);
        ViewDistance = Mathf.Clamp(cfg.GetValue(Section, "visibilita", ViewDistance).AsInt32(), 100, 600);
        Fov = Mathf.Clamp(cfg.GetValue(Section, "fov", Fov).AsSingle(), 70f, 110f);
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath);   // conserva eventuali altre sezioni
        cfg.SetValue(Section, "preset", (int)CurrentPreset);
        cfg.SetValue(Section, "finestra", WindowMode);
        cfg.SetValue(Section, "vsync", VSync);
        cfg.SetValue(Section, "max_fps", MaxFps);
        cfg.SetValue(Section, "bassa_latenza", LowLatency);
        cfg.SetValue(Section, "antialiasing", AntiAliasing);
        cfg.SetValue(Section, "ombre", Shadows);
        cfg.SetValue(Section, "ssao", Ssao);
        cfg.SetValue(Section, "bloom", Bloom);
        cfg.SetValue(Section, "scala_3d", RenderScale);
        cfg.SetValue(Section, "visibilita", ViewDistance);
        cfg.SetValue(Section, "fov", Fov);
        var err = cfg.Save(SettingsPath);
        if (err != Error.Ok) GD.PushWarning($"[impostazioni] salvataggio fallito: {err}");
    }

    /// <summary>Da chiamare dopo ogni modifica: salva, applica lo stato globale, avvisa le scene.</summary>
    public static void Commit()
    {
        Save();
        ApplyGlobal();
        Changed?.Invoke();
    }

    // ------------------------------------------------------------------ applicazione

    public static bool IsHeadless => DisplayServer.GetName() == "headless";

    /// <summary>
    /// true se il gioco gira DENTRO la finestra dell'editor (Godot 4.6+ "Embed Game").
    /// Lì la finestra può essere solo finestrata: chiedere il pieno schermo stampa
    /// "Embedded window only supports Windowed mode." e non cambia niente.
    /// </summary>
    public static bool IsEmbedded => Engine.IsEmbeddedInEditor();

    private static bool _embeddedWarned;

    /// <summary>"forward_plus", "mobile" o "gl_compatibility".</summary>
    public static string Renderer => RenderingServer.GetCurrentRenderingMethod();

    public static bool SupportsSsao => Renderer == "forward_plus";

    /// <summary>Finestra, vsync, limite FPS. Non tocca la scena.</summary>
    public static void ApplyGlobal()
    {
        EnsureLoaded();
        if (IsHeadless) return;   // il server ha il suo limite e nessuna finestra

        // Dentro l'editor incassato la finestra è sempre finestrata: qualunque altra
        // modalità viene rifiutata dall'engine con un errore. La preferenza resta
        // salvata e vale alla prima esecuzione normale.
        if (IsEmbedded && !_embeddedWarned)
        {
            _embeddedWarned = true;
            GD.Print("[finestra] stai giocando dentro l'editor: modalità finestrata obbligatoria " +
                     "(avvia il gioco fuori dall'editor per il pieno schermo).");
        }

        if (!IsEmbedded)
        {
            var mode = WindowMode switch
            {
                1 => DisplayServer.WindowMode.Fullscreen,
                2 => DisplayServer.WindowMode.ExclusiveFullscreen,
                _ => DisplayServer.WindowMode.Windowed
            };
            if (DisplayServer.WindowGetMode() != mode) DisplayServer.WindowSetMode(mode);
        }

        var vs = VSync switch
        {
            1 => DisplayServer.VSyncMode.Enabled,
            2 => DisplayServer.VSyncMode.Adaptive,
            3 => DisplayServer.VSyncMode.Mailbox,
            _ => DisplayServer.VSyncMode.Disabled
        };
        DisplayServer.WindowSetVsyncMode(vs);

        // il limite reale lo decide il LatencyGovernor, se attivo; altrimenti è questo
        Engine.MaxFps = MaxFps;
    }

    /// <summary>Qualità della vista 3D: antialiasing e scala di rendering.</summary>
    public static void ApplyViewport(Viewport vp)
    {
        EnsureLoaded();
        vp.Msaa3D = AntiAliasing switch
        {
            3 => Viewport.Msaa.Msaa2X,
            4 => Viewport.Msaa.Msaa4X,
            _ => Viewport.Msaa.Disabled
        };
        vp.ScreenSpaceAA = AntiAliasing switch
        {
            1 => Viewport.ScreenSpaceAAEnum.Fxaa,
            2 => Viewport.ScreenSpaceAAEnum.Smaa,
            _ => Viewport.ScreenSpaceAAEnum.Disabled
        };
        vp.UseTaa = AntiAliasing == 5;

        vp.Scaling3DScale = RenderScale;
        vp.Scaling3DMode = RenderScale < 0.99f && Renderer != "gl_compatibility"
            ? Viewport.Scaling3DModeEnum.Fsr
            : Viewport.Scaling3DModeEnum.Bilinear;
    }

    /// <summary>Luce, ombre, foschia, post-processing e camera di una scena.</summary>
    public static void ApplyScene(Atmosphere atm, Camera3D cam)
    {
        EnsureLoaded();
        if (atm != null)
        {
            var sun = atm.Sun;
            sun.ShadowEnabled = Shadows > 0;

            // Ombre DEFINITE: atlas grande, quattro split con densità concentrata
            // vicino alla camera, filtro morbido di qualità alta (SoftLow produceva
            // il puntinato che si vedeva sul muro) e penombra stretta.
            if (Shadows == 1)
            {
                RenderingServer.DirectionalShadowAtlasSetSize(4096, true);
                sun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel2Splits;
                sun.DirectionalShadowSplit1 = 0.22f;
                sun.DirectionalShadowMaxDistance = 70f;
                sun.ShadowBlur = 0.85f;
                sun.LightAngularDistance = 0.6f;
                sun.ShadowBias = 0.03f;
                sun.ShadowNormalBias = 1.0f;
                sun.DirectionalShadowBlendSplits = true;
                RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftMedium);
            }
            else if (Shadows >= 2)
            {
                RenderingServer.DirectionalShadowAtlasSetSize(8192, true);
                sun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
                // split ravvicinati: i primi metri hanno 4-5 volte i texel di prima
                sun.DirectionalShadowSplit1 = 0.08f;
                sun.DirectionalShadowSplit2 = 0.22f;
                sun.DirectionalShadowSplit3 = 0.48f;
                sun.DirectionalShadowMaxDistance = 90f;
                sun.DirectionalShadowFadeStart = 0.92f;
                sun.DirectionalShadowBlendSplits = true;
                // penombra stretta (0 = filo di rasoio): ombre nette ma non finte
                sun.ShadowBlur = 0.35f;
                sun.LightAngularDistance = 0.35f;
                sun.ShadowBias = 0.022f;
                sun.ShadowNormalBias = 0.8f;
                RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftHigh);
            }
            else
            {
                RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.Hard);
            }

            var env = atm.Env;
            env.SsaoEnabled = Ssao && SupportsSsao;
            env.GlowEnabled = Bloom;

            // La foschia del doc 07 è anche il culling: oltre la distanza di visibilità
            // non si disegna niente, e il mare la giustifica.
            env.FogDepthBegin = ViewDistance * 0.30f;
            env.FogDepthEnd = ViewDistance;
        }
        if (cam != null)
        {
            cam.Fov = Fov;
            cam.Far = ViewDistance + 40f;
        }
    }
}
