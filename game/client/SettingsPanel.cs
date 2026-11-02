using Godot;

/// <summary>
/// IMPOSTAZIONI GRAFICHE — un pannello solo, che si apre dal menu e in partita (F10).
/// Ogni modifica si applica subito e si salva. Toccare una voce di qualità sposta il
/// preset su PERSONALIZZATO: il preset dice sempre la verità su cosa stai usando.
/// </summary>
public partial class SettingsPanel : Control
{
    private static SettingsPanel _open;

    private readonly LatencyGovernor _governor;
    private bool _refreshing;

    private OptionButton _preset, _window, _vsync, _fps, _aa, _shadows, _view;
    private CheckButton _lowLatency, _ssao, _bloom;
    private HSlider _scale, _fov;
    private Label _scaleLabel, _fovLabel, _latencyLabel, _info;
    private double _infoTimer;

    public event System.Action Closed;

    public static bool IsOpen => _open != null && IsInstanceValid(_open);

    /// <summary>Apre il pannello (uno solo alla volta) sopra tutto il resto.</summary>
    public static SettingsPanel Open(Node parent, LatencyGovernor governor = null)
    {
        if (IsOpen) return _open;
        var layer = new CanvasLayer { Layer = 50, Name = "Impostazioni" };
        parent.AddChild(layer);
        var panel = new SettingsPanel(governor);
        layer.AddChild(panel);
        _open = panel;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        return panel;
    }

    public SettingsPanel() : this(null) { }

    public SettingsPanel(LatencyGovernor governor)
    {
        _governor = governor;
    }

    public void Close()
    {
        if (_open == this) _open = null;
        Closed?.Invoke();
        var layer = GetParent();
        if (layer is CanvasLayer) layer.QueueFree();
        else QueueFree();
    }

    public override void _ExitTree()
    {
        if (_open == this) _open = null;
    }

    public override void _Ready()
    {
        GraphicsSettings.EnsureLoaded();
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        BuildUi();
        Refresh();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.Escape || k.Keycode == Key.F10))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    public override void _Process(double delta)
    {
        _infoTimer += delta;
        if (_infoTimer < 0.5) return;
        _infoTimer = 0;
        string cap = Engine.MaxFps == 0 ? "nessuno" : Engine.MaxFps.ToString();
        _latencyLabel.Text = _governor != null && GraphicsSettings.LowLatency
            ? $"   stato: {_governor.Mode} · GPU {_governor.GpuMs:F1} ms · CPU {_governor.CpuMs:F1} ms · limite attuale {cap}"
            : $"   limite attuale: {cap}";
        _info.Text = $"renderer: {GraphicsSettings.Renderer} · {Engine.GetFramesPerSecond():F0} fps · ESC o F10 per chiudere";
    }

    // ================================================================== UI

    private static StyleBoxFlat Box(Color bg, int pad)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = pad, ContentMarginRight = pad,
            ContentMarginTop = pad, ContentMarginBottom = pad
        };
    }

    private static Label Text(string s, int size, Color c)
    {
        var l = new Label { Text = s };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", c);
        return l;
    }

    private static readonly Color Ink = new(0.91f, 0.93f, 0.96f);
    private static readonly Color Dim = new(0.55f, 0.60f, 0.67f);
    private static readonly Color Accent = new(0.42f, 0.72f, 0.94f);

    private GridContainer _grid;

    private void Section(string title)
    {
        _grid.AddChild(Text(title, 11, Accent));
        _grid.AddChild(new Control());
    }

    private void Row(string label, Control control, string note = null)
    {
        var l = Text(label, 14, Ink);
        l.CustomMinimumSize = new Vector2(230, 0);
        if (note != null) l.TooltipText = note;
        _grid.AddChild(l);
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (note != null) control.TooltipText = note;
        _grid.AddChild(control);
    }

    private OptionButton Options(string[] items, System.Action<int> onPick)
    {
        var o = new OptionButton { CustomMinimumSize = new Vector2(0, 30) };
        for (int i = 0; i < items.Length; i++) o.AddItem(items[i], i);
        o.ItemSelected += idx => { if (!_refreshing) onPick((int)idx); };
        return o;
    }

    private CheckButton Toggle(System.Action<bool> onToggle)
    {
        var c = new CheckButton();
        c.Toggled += on => { if (!_refreshing) onToggle(on); };
        return c;
    }

    private HBoxContainer Slider(double min, double max, double step, out HSlider slider, out Label value, System.Action<double> onChange)
    {
        var row = new HBoxContainer();
        var s = new HSlider { MinValue = min, MaxValue = max, Step = step, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        s.CustomMinimumSize = new Vector2(220, 24);
        var v = Text("", 14, Ink);
        v.CustomMinimumSize = new Vector2(60, 0);
        s.ValueChanged += x => { if (!_refreshing) onChange(x); };
        row.AddChild(s);
        row.AddChild(v);
        slider = s;
        value = v;
        return row;
    }

    private void BuildUi()
    {
        UiKit.UpdateScale(GetViewport());
        var vpSize = GetViewportRect().Size;
        CustomMinimumSize = vpSize;
        Size = vpSize;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var backdrop = new ColorRect
        {
            Color = new Color(0.030f, 0.042f, 0.055f, 0.94f),
            MouseFilter = MouseFilterEnum.Stop
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(UiKit.Px(760), 0) };
        panel.AddThemeStyleboxOverride("panel", UiKit.PanelStyle());
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", UiKit.Px(22));
        margin.AddThemeConstantOverride("margin_right", UiKit.Px(22));
        margin.AddThemeConstantOverride("margin_top", UiKit.Px(20));
        margin.AddThemeConstantOverride("margin_bottom", UiKit.Px(20));
        panel.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", UiKit.Px(10));
        margin.AddChild(col);

        col.AddChild(Text("IMPOSTAZIONI GRAFICHE", 24, Ink));
        col.AddChild(Text("Si applicano subito e si salvano. Il preset PRO è quello competitivo ufficiale (doc 07).", 12, Dim));

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, UiKit.Px(520)),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        col.AddChild(scroll);

        _grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _grid.AddThemeConstantOverride("h_separation", 18);
        _grid.AddThemeConstantOverride("v_separation", 9);
        scroll.AddChild(_grid);

        // ---------------------------------------------------------- qualità
        Section("QUALITÀ");
        _preset = Options(new[] { "PRO — tutto spento", "BASSO — GTX 970, 60 fps", "ALTO — GTX 1070 Ti, 120 fps", "PERSONALIZZATO" }, i =>
        {
            if (i == 3) return;
            GraphicsSettings.ApplyPreset((GraphicsSettings.Preset)i);
            Refresh();
            GraphicsSettings.Commit();
        });
        _preset.SetItemDisabled(3, true);
        Row("Preset", _preset);

        _aa = Options(new[] { "Nessuno", "FXAA", "SMAA", "MSAA 2×", "MSAA 4×", "TAA" }, i =>
        { GraphicsSettings.AntiAliasing = i; Custom(); });
        Row("Antialiasing", _aa, "FXAA costa quasi zero. TAA ammorbidisce l'immagine: molti competitivi lo evitano.");

        _shadows = Options(new[] { "Spente", "Basse (2048, 60 m)", "Alte (4096, 100 m)" }, i =>
        { GraphicsSettings.Shadows = i; Custom(); });
        Row("Ombre del sole", _shadows);

        _ssao = Toggle(on => { GraphicsSettings.Ssao = on; Custom(); });
        Row("Occlusione ambientale (SSAO)", _ssao, "Solo con il renderer Forward+.");

        _bloom = Toggle(on => { GraphicsSettings.Bloom = on; Custom(); });
        Row("Bagliore (bloom)", _bloom);

        Row("Scala di rendering 3D", Slider(50, 100, 5, out _scale, out _scaleLabel, v =>
        {
            GraphicsSettings.RenderScale = (float)(v / 100.0);
            _scaleLabel.Text = $"{v:F0}%";
            Custom();
        }), "Sotto il 100% usa FSR 1 per ricostruire l'immagine.");

        _view = Options(new[] { "150 m", "250 m", "400 m" }, i =>
        { GraphicsSettings.ViewDistance = GraphicsSettings.ViewChoices[i]; Custom(); });
        Row("Distanza di visibilità", _view, "La foschia del mare: è anche il culling (doc 07).");

        // ---------------------------------------------------------- schermo
        Section("SCHERMO E LATENZA");
        _window = Options(new[] { "Finestra", "Schermo intero (senza bordi)", "Schermo intero esclusivo" }, i =>
        { GraphicsSettings.WindowMode = i; GraphicsSettings.Commit(); });
        // nell'editor incassato la finestra non può cambiare modalità: si dice, non si nasconde
        _window.Disabled = GraphicsSettings.IsEmbedded;
        Row("Modalità", _window, GraphicsSettings.IsEmbedded
            ? "Stai giocando dentro l'editor: la finestra resta finestrata. La scelta vale appena avvii il gioco fuori dall'editor."
            : "L'esclusivo dà la latenza più bassa sulla maggior parte dei sistemi.");

        _vsync = Options(new[] { "Disattivato", "Attivo", "Adattivo", "Mailbox" }, i =>
        { GraphicsSettings.VSync = i; GraphicsSettings.Commit(); });
        Row("V-Sync", _vsync, "Disattivato = latenza minima, possibile tearing. Mailbox: niente tearing, latenza bassa (dove supportato).");

        var fpsNames = new string[GraphicsSettings.FpsChoices.Length];
        for (int i = 0; i < fpsNames.Length; i++)
            fpsNames[i] = GraphicsSettings.FpsChoices[i] == 0 ? "Illimitato" : GraphicsSettings.FpsChoices[i] + " fps";
        _fps = Options(fpsNames, i => { GraphicsSettings.MaxFps = GraphicsSettings.FpsChoices[i]; GraphicsSettings.Commit(); });
        Row("Limite FPS", _fps);

        _lowLatency = Toggle(on => { GraphicsSettings.LowLatency = on; GraphicsSettings.Commit(); });
        Row("Bassa latenza (stile Reflex)", _lowLatency,
            "Godot non integra NVIDIA Reflex. Questa opzione ne riproduce l'effetto principale: " +
            "con V-Sync limita a refresh − 3, senza V-Sync tiene la GPU sotto la saturazione, così non si accodano fotogrammi.");
        _latencyLabel = Text("", 11, Dim);
        _grid.AddChild(new Control());
        _grid.AddChild(_latencyLabel);

        // ---------------------------------------------------------- vista
        Section("VISTA");
        Row("Campo visivo (FOV)", Slider(70, 110, 1, out _fov, out _fovLabel, v =>
        {
            GraphicsSettings.Fov = (float)v;
            _fovLabel.Text = $"{v:F0}°";
            GraphicsSettings.Commit();
        }), "Verticale, in gradi.");

        // ---------------------------------------------------------- piede
        _info = Text("", 11, Dim);
        col.AddChild(_info);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        var close = new Button { Text = "  CHIUDI  ", CustomMinimumSize = new Vector2(140, 36) };
        close.Pressed += Close;
        buttons.AddChild(close);
        col.AddChild(buttons);
    }

    /// <summary>Una voce di qualità è cambiata: il preset non è più quello di prima.</summary>
    private void Custom()
    {
        GraphicsSettings.CurrentPreset = GraphicsSettings.Preset.Custom;
        _refreshing = true;
        _preset.Select(3);
        _refreshing = false;
        GraphicsSettings.Commit();
    }

    private void Refresh()
    {
        _refreshing = true;
        _preset.Select((int)GraphicsSettings.CurrentPreset);
        _window.Select(GraphicsSettings.WindowMode);
        _vsync.Select(GraphicsSettings.VSync);
        int fpsIdx = System.Array.IndexOf(GraphicsSettings.FpsChoices, GraphicsSettings.MaxFps);
        _fps.Select(fpsIdx >= 0 ? fpsIdx : 0);
        _lowLatency.ButtonPressed = GraphicsSettings.LowLatency;
        _aa.Select(GraphicsSettings.AntiAliasing);
        _shadows.Select(GraphicsSettings.Shadows);
        _ssao.ButtonPressed = GraphicsSettings.Ssao;
        _ssao.Disabled = !GraphicsSettings.SupportsSsao;
        _bloom.ButtonPressed = GraphicsSettings.Bloom;
        _scale.Value = Mathf.Round(GraphicsSettings.RenderScale * 100f);
        _scaleLabel.Text = $"{_scale.Value:F0}%";
        int viewIdx = System.Array.IndexOf(GraphicsSettings.ViewChoices, GraphicsSettings.ViewDistance);
        _view.Select(viewIdx >= 0 ? viewIdx : 2);
        _fov.Value = GraphicsSettings.Fov;
        _fovLabel.Text = $"{GraphicsSettings.Fov:F0}°";
        _refreshing = false;
    }
}
