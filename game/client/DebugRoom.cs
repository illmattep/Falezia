using Falezia.Sim;
using Godot;
using Vec3 = System.Numerics.Vector3;
using Vec2 = System.Numerics.Vector2;

/// <summary>
/// STANZA DEBUG. Tutti i dati tecnici in un posto solo, e gli scenari per provarli.
/// UI costruita con i container (niente sovrapposizioni) e area controlli scrollabile.
/// </summary>
public partial class DebugRoom : Node3D
{
    private const int DockWidth = 400;

    private Camera3D _cam;
    private Label _state;
    private Label _perfLabel;
    private Label _stopwatchLabel;
    private RichTextLabel _log;
    private Crosshair _crosshair;
    private Node3D _botRoot;
    private Node3D _worldRoot;
    private GridContainer _teleports;
    private Atmosphere _atm;
    private LatencyGovernor _governor;
    private WeaponView _weapon;
    private Tracers _tracers;
    private ShellCasings _casings;
    private BulletHoles _holes;
    private ScopeOverlay _scope;
    private LoadoutState _loadout;
    private Label _moneyLabel;
    private Label _modeLabel;
    private Label _ammoLabel;
    private Label _spreadLabel;
    private int _seenShots;
    private Label _weaponLabel;
    private Label _killsLabel;
    private Label _feedLabel;
    private HitMarker _hitmark;
    private Targets _targets;
    private float _feedLife;
    private int _lastKills;
    private float _kickPitch;      // rinculo della camera (radianti), torna da solo
    private float _kickYaw;
    private int _recoilShots;      // colpi nella raffica: decide il pattern
    private float _recoilHold;     // secondi in cui la mira resta su prima di rientrare
    private OmniLight3D _flash;
    private float _flashLife;
    private bool _fireHeld;
    private bool _pendingFire;
    private Control _dock;            // la colonna di debug: si apre con F8
    private bool _debugOpen;

    private WorldState _world;
    private SimState _sim;
    private float _yaw, _pitch;
    private double _acc;
    private double _perfTimer;
    private int _botId = 100;
    private float _simLatencyMs;
    private float _simLossPct;
    private string _profile = "rifle";

    // cronometro dei percorsi (doc 07: "un cronometro in-game, non a occhio")
    private bool _swRunning;
    private double _swTime;
    private float _swDist;
    private Vec3 _swLast;
    private Vec3 _swStart;

    public override void _Ready()
    {
        // nella stanza debug i soldi non finiscono: lo shop si prova senza economia
        _loadout = new LoadoutState { Unlimited = true };

        GraphicsSettings.ApplyGlobal();

        _atm = Atmosphere.Create(this);
        _cam = new Camera3D { Fov = 85f, Near = 0.05f, Far = 440f, Current = true };
        AddChild(_cam);
        _botRoot = new Node3D { Name = "Bot" };
        AddChild(_botRoot);

        _weapon = new WeaponView { Name = "Viewmodel" };
        _cam.AddChild(_weapon);
        _weapon.Setup(_cam);

        _tracers = new Tracers { Name = "Tracce" };
        AddChild(_tracers);

        _targets = new Targets { Name = "Bersagli" };
        AddChild(_targets);

        // lampo di volata: una luce vera, non un cartellino. Dura 45 ms.
        _flash = new OmniLight3D
        {
            Name = "LampoDiVolata",
            LightColor = new Color(1f, 0.86f, 0.62f),
            OmniRange = 7f,
            ShadowEnabled = false,
            Visible = false
        };
        AddChild(_flash);

        _casings = new ShellCasings { Name = "Bossoli" };
        AddChild(_casings);

        _holes = new BulletHoles { Name = "Fori" };
        AddChild(_holes);

        BuildUi();
        LoadMap("calanera");
        InputConfig.FpsDefaults();

        _governor = new LatencyGovernor { Name = "BassaLatenza" };
        AddChild(_governor);
        GraphicsSettings.Changed += ApplyGraphics;
        ApplyGraphics();

        Log("[color=#7fd0ff]Stanza debug pronta.[/color]");
        Log($"[color=#7fd0ff]ARMI:[/color] tutto l'arsenale è in mano ({Arsenal.Count} slot: 5 pistole, 3 SMG, 2 pompe, 3 AR, 2 cecchini, LMG, coltello).");
        Log("[color=#7fd0ff]ARMI:[/color] 1 primaria · 2 secondaria · 3 coltello · rotella per girare quello che hai.");
        Log("[color=#ffcf70]IN COSTRUZIONE:[/color] economia, round, porte, anti-cheat.");
        Log("ESC libera il mouse · clicca nella vista per ricatturarlo · F10 impostazioni.");
    }

    public override void _ExitTree()
    {
        GraphicsSettings.Changed -= ApplyGraphics;
    }

    private void ApplyGraphics()
    {
        GraphicsSettings.ApplyViewport(GetViewport());
        GraphicsSettings.ApplyScene(_atm, _cam);
    }

    // ================================================================== MONDO

    /// <summary>Carica una mappa: "calanera" (dal file, come client e server) o "test".</summary>
    private void LoadMap(string which)
    {
        _worldRoot?.QueueFree();
        _world = which == "test" ? WorldState.MakeTestWorld() : MapFiles.Load();
        _worldRoot = new Node3D { Name = "Mondo" };
        AddChild(_worldRoot);
        MapView.Build(_worldRoot, _world);

        foreach (var c in _botRoot.GetChildren()) c.QueueFree();
        _targets?.Clear();
        _sim = new SimState();
        var start = _world.SpawnsNorth.Count > 0 ? _world.SpawnsNorth[0] : _world.FallbackFeet;
        Simulation.Spawn(_sim, 1, start);
        _yaw = MathF.Atan2(start.X, start.Z);   // guarda verso il centro della mappa
        _pitch = 0f;
        StopStopwatch(false);
        _seenShots = 0;   // mappa nuova: la simulazione riparte da zero, i contatori anche
        _casings?.Clear();
        _holes?.Clear();
        RebuildTeleports();
        EquipWeapon(_loadout.DeployWeapon);   // si spawna con la secondaria e il coltello
        Log($"mappa: [color=#7fd0ff]{_world.Name}[/color] · {_world.Boxes.Count} volumi · {_world.Decor.Count} decorazioni · {_world.Points.Count} punti");
    }

    // ===================================================================== UI

    private static StyleBoxFlat Box(Color bg, int radius = 0, int pad = 12)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = pad,
            ContentMarginRight = pad,
            ContentMarginTop = pad,
            ContentMarginBottom = pad
        };
    }

    private static Label Section(string text)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", 10);
        l.AddThemeColorOverride("font_color", new Color(0.42f, 0.72f, 0.94f));
        return l;
    }

    private static Label Note(string text, int size = 11)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", new Color(0.52f, 0.57f, 0.64f));
        return l;
    }

    private static HSeparator Sep() => new HSeparator();

    private Button Btn(string text, System.Action onPress)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, 27) };
        b.AddThemeFontSizeOverride("font_size", 11);
        b.Pressed += onPress;
        return b;
    }

    private HBoxContainer Row((string Label, float Value)[] items, System.Action<float> onPick)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        foreach (var it in items)
        {
            var v = it.Value;
            row.AddChild(Btn(it.Label, () => onPick(v)));
        }
        return row;
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        // ---------- dock sinistro
        var dock = new PanelContainer();
        dock.AnchorLeft = 0; dock.AnchorTop = 0; dock.AnchorRight = 0; dock.AnchorBottom = 1;
        dock.OffsetLeft = 0; dock.OffsetTop = 0; dock.OffsetRight = DockWidth; dock.OffsetBottom = 0;
        dock.AddThemeStyleboxOverride("panel", Box(new Color(0.052f, 0.058f, 0.070f, 0.97f)));
        layer.AddChild(dock);
        _dock = dock;
        _dock.Visible = false;      // la schermata di debug si apre con F8

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 13);
        margin.AddThemeConstantOverride("margin_right", 13);
        margin.AddThemeConstantOverride("margin_top", 11);
        margin.AddThemeConstantOverride("margin_bottom", 11);
        dock.AddChild(margin);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vb);

        var title = new Label { Text = "FALEZIA" };
        title.AddThemeFontSizeOverride("font_size", 21);
        title.AddThemeColorOverride("font_color", new Color(0.91f, 0.94f, 0.98f));
        vb.AddChild(title);
        vb.AddChild(Note("stanza debug · M0 · economia, armi e round: IN COSTRUZIONE", 10));

        // ---------- controlli scrollabili (non escono mai dal dock)
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        vb.AddChild(scroll);

        var inner = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(inner);

        _state = new Label();
        _state.AddThemeFontSizeOverride("font_size", 12);
        _state.AddThemeColorOverride("font_color", new Color(0.80f, 0.86f, 0.92f));
        inner.AddChild(_state);

        inner.AddChild(Sep());
        inner.AddChild(Section("MAPPA"));
        var maps = new HBoxContainer();
        maps.AddThemeConstantOverride("separation", 4);
        maps.AddChild(Btn("CALANERA", () => LoadMap("calanera")));
        maps.AddChild(Btn("STANZA TEST", () => LoadMap("test")));
        inner.AddChild(maps);

        inner.AddChild(Sep());
        inner.AddChild(Section("TELEPORT"));
        _teleports = new GridContainer { Columns = 2 };
        _teleports.AddThemeConstantOverride("h_separation", 4);
        _teleports.AddThemeConstantOverride("v_separation", 4);
        inner.AddChild(_teleports);

        inner.AddChild(Sep());
        inner.AddChild(Section("CRONOMETRO — tempi di percorrenza (doc 02)"));
        var sw = new HBoxContainer();
        sw.AddThemeConstantOverride("separation", 4);
        sw.AddChild(Btn("AVVIA", StartStopwatch));
        sw.AddChild(Btn("FERMA", () => StopStopwatch(true)));
        inner.AddChild(sw);
        _stopwatchLabel = new Label { Text = "fermo" };
        _stopwatchLabel.AddThemeFontSizeOverride("font_size", 12);
        _stopwatchLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.45f));
        inner.AddChild(_stopwatchLabel);

        inner.AddChild(Sep());
        inner.AddChild(Section("GRAFICA"));
        var gfx = new HBoxContainer();
        gfx.AddThemeConstantOverride("separation", 4);
        foreach (var pr in new[] { GraphicsSettings.Preset.Pro, GraphicsSettings.Preset.Low, GraphicsSettings.Preset.High })
        {
            var preset = pr;
            gfx.AddChild(Btn(GraphicsSettings.PresetName(preset), () =>
            {
                GraphicsSettings.ApplyPreset(preset);
                GraphicsSettings.Commit();
                Log($"preset grafico: {GraphicsSettings.PresetName(preset)}");
            }));
        }
        inner.AddChild(gfx);
        inner.AddChild(Btn("TUTTE LE IMPOSTAZIONI  (F10)", () => SettingsPanel.Open(this, _governor)));

        inner.AddChild(Sep());
        inner.AddChild(Section("ARSENALE — click o rotella del mouse"));
        var guns = new GridContainer { Columns = 2 };
        guns.AddThemeConstantOverride("h_separation", 4);
        guns.AddThemeConstantOverride("v_separation", 4);
        foreach (var def in Arsenal.All)
        {
            var slot = (WeaponSlot)System.Array.IndexOf(Arsenal.All, def);
            var label = def.Price > 0 ? $"{def.Name.ToUpperInvariant()}  ${def.Price}" : def.Name.ToUpperInvariant();
            guns.AddChild(Btn(label, () => BuyWeapon(slot)));
        }
        inner.AddChild(guns);

        inner.AddChild(Sep());
        inner.AddChild(Section("MIRINO"));
        var prof = new GridContainer { Columns = 3 };
        prof.AddThemeConstantOverride("h_separation", 4);
        prof.AddThemeConstantOverride("v_separation", 4);
        foreach (var p in new[] { "pistol", "smg", "shotgun", "rifle", "sniper", "knife" })
        {
            var key = p;
            prof.AddChild(Btn(p.ToUpperInvariant(), () =>
            {
                _profile = key;
                _crosshair?.SetProfile(key);
                Log($"mirino: {key}");
            }));
        }
        inner.AddChild(prof);

        inner.AddChild(Sep());
        inner.AddChild(Section("RETE — simulata, non ancora sul socket"));
        inner.AddChild(Row(new[] { ("LAN", 0f), ("50", 50f), ("100", 100f), ("200", 200f) },
            v => { _simLatencyMs = v; Log($"latenza simulata: {v:F0} ms"); }));
        inner.AddChild(Row(new[] { ("loss 0%", 0f), ("loss 2%", 2f), ("loss 10%", 10f) },
            v => { _simLossPct = v; Log($"perdita simulata: {v:F0}%"); }));

        inner.AddChild(Sep());
        inner.AddChild(Section("SCENARI"));
        var scen = new HBoxContainer();
        scen.AddThemeConstantOverride("separation", 4);
        scen.AddChild(Btn("BERSAGLIO (6 m)", SpawnBot));
        scen.AddChild(Btn("CINQUE BERSAGLI", SpawnTargets));
        scen.AddChild(Btn("DUMP ARMI", DumpWeapons));
        inner.AddChild(scen);

        inner.AddChild(Sep());
        inner.AddChild(Section("PERFORMANCE"));
        _perfLabel = new Label();
        _perfLabel.AddThemeFontSizeOverride("font_size", 11);
        _perfLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.88f, 0.78f));
        inner.AddChild(_perfLabel);

        // ---------- log: altezza fissa, sempre visibile
        vb.AddChild(Sep());
        vb.AddChild(Section("LOG"));
        var logBox = new PanelContainer { CustomMinimumSize = new Vector2(0, 128) };
        logBox.AddThemeStyleboxOverride("panel", Box(new Color(0.03f, 0.035f, 0.045f, 1f), 4, 8));
        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _log.AddThemeFontSizeOverride("normal_font_size", 11);
        logBox.AddChild(_log);
        vb.AddChild(logBox);

        // ---------- mirino: sempre al centro, sopra tutto
        // l'ottica sta SOPRA il mirino: quando è in faccia, il mirino si nasconde
        _scope = new ScopeOverlay { Name = "Ottica" };
        layer.AddChild(_scope);

        _crosshair = new Crosshair();
        layer.AddChild(_crosshair);
        _crosshair.SetProfile(_profile);

        // segno del colpo: sotto il mirino, sopra tutto il resto
        _hitmark = new HitMarker { Name = "Segno" };
        layer.AddChild(_hitmark);

        // punteggio: in alto a sinistra, sotto la barra dei dati
        _killsLabel = new Label { Text = "0 abbattuti   ·   0 punti" };
        _killsLabel.AnchorLeft = 0; _killsLabel.AnchorTop = 0;
        _killsLabel.AnchorRight = 0; _killsLabel.AnchorBottom = 0;
        _killsLabel.OffsetLeft = 18; _killsLabel.OffsetTop = 14;
        _killsLabel.OffsetRight = 320; _killsLabel.OffsetBottom = 38;
        _killsLabel.AddThemeFontSizeOverride("font_size", 15);
        _killsLabel.AddThemeColorOverride("font_color", new Color(0.90f, 0.92f, 0.95f));
        layer.AddChild(_killsLabel);

        // kill feed: una riga che entra e svanisce
        _feedLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        _feedLabel.AnchorLeft = 1; _feedLabel.AnchorTop = 0;
        _feedLabel.AnchorRight = 1; _feedLabel.AnchorBottom = 0;
        _feedLabel.OffsetLeft = -460; _feedLabel.OffsetTop = 16;
        _feedLabel.OffsetRight = -18; _feedLabel.OffsetBottom = 40;
        _feedLabel.AddThemeFontSizeOverride("font_size", 14);
        _feedLabel.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.42f));
        layer.AddChild(_feedLabel);

        // ---------- hint
        // ---------- arma e munizioni, basso a destra
        var ammoBox = new VBoxContainer();
        ammoBox.AnchorLeft = 1; ammoBox.AnchorTop = 1; ammoBox.AnchorRight = 1; ammoBox.AnchorBottom = 1;
        ammoBox.OffsetLeft = -300; ammoBox.OffsetTop = -150; ammoBox.OffsetRight = -18; ammoBox.OffsetBottom = -34;
        ammoBox.Alignment = BoxContainer.AlignmentMode.End;
        layer.AddChild(ammoBox);

        _weaponLabel = new Label { Text = "STANDARD", HorizontalAlignment = HorizontalAlignment.Right };
        _weaponLabel.AddThemeFontSizeOverride("font_size", 13);
        _weaponLabel.AddThemeColorOverride("font_color", new Color(0.62f, 0.68f, 0.76f));
        ammoBox.AddChild(_weaponLabel);

        _ammoLabel = new Label { Text = "17 / 51", HorizontalAlignment = HorizontalAlignment.Right };
        _ammoLabel.AddThemeFontSizeOverride("font_size", 30);
        _ammoLabel.AddThemeColorOverride("font_color", new Color(0.93f, 0.94f, 0.96f));
        ammoBox.AddChild(_ammoLabel);

        // cono di dispersione vero (gradi) e cadenza: così si vede cosa cambia
        // fra fermo, in movimento e in mira
        _modeLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        _modeLabel.AddThemeFontSizeOverride("font_size", 12);
        _modeLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.45f));
        ammoBox.AddChild(_modeLabel);

        _moneyLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        _moneyLabel.AddThemeFontSizeOverride("font_size", 13);
        _moneyLabel.AddThemeColorOverride("font_color", new Color(0.62f, 0.68f, 0.76f));
        ammoBox.AddChild(_moneyLabel);

        _spreadLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        _spreadLabel.AddThemeFontSizeOverride("font_size", 12);
        _spreadLabel.AddThemeColorOverride("font_color", new Color(0.66f, 0.72f, 0.80f));
        ammoBox.AddChild(_spreadLabel);

        var hint = new Label
        {
            Text = "WASD · LMB spara · RMB mira · R ricarica · B shop · 1/2/3 armi · rotella · F8 debug · F10 impostazioni · ESC",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        hint.AnchorLeft = 0; hint.AnchorTop = 1; hint.AnchorRight = 1; hint.AnchorBottom = 1;
        hint.OffsetLeft = 8; hint.OffsetTop = -25; hint.OffsetRight = -12; hint.OffsetBottom = -6;
        hint.AddThemeFontSizeOverride("font_size", 11);
        hint.AddThemeColorOverride("font_color", new Color(0.58f, 0.63f, 0.70f));
        layer.AddChild(hint);
    }

    private void Log(string s)
    {
        _log?.AppendText(s + "\n");
        GD.Print(s.StripEdges());
    }

    // =============================================================== AZIONI

    private void RebuildTeleports()
    {
        if (_teleports == null) return;
        foreach (var c in _teleports.GetChildren()) c.QueueFree();
        foreach (var pt in _world.Points)
        {
            var feet = pt.Pos;
            var name = pt.Name;
            _teleports.AddChild(Btn(name.ToUpperInvariant(), () => Teleport(feet, name)));
        }
        _teleports.AddChild(Btn("RESET", ResetScenario));
    }

    private void Teleport(Vec3 feet, string name)
    {
        var p = _sim.Get(1);
        if (p == null) return;
        p.Pos = feet + new Vec3(0f, Constants.Height * 0.5f + 0.05f, 0f);
        p.Vel = Vec3.Zero;
        p.Crouching = false;
        Log($"teleport → {name} ({feet.X:F1}, {feet.Y:F1}, {feet.Z:F1})");
    }

    private void ResetScenario()
    {
        _sim = new SimState();
        var start = _world.SpawnsNorth.Count > 0 ? _world.SpawnsNorth[0] : _world.FallbackFeet;
        Simulation.Spawn(_sim, 1, start);
        foreach (var c in _botRoot.GetChildren()) c.QueueFree();
        _targets?.Clear();
        StopStopwatch(false);
        EquipWeapon(_loadout.DeployWeapon);
        Log("scenario azzerato");
    }

    private void StartStopwatch()
    {
        var p = _sim.Get(1);
        if (p == null) return;
        _swRunning = true;
        _swTime = 0;
        _swDist = 0;
        _swLast = p.Pos;
        _swStart = p.Pos;
        Log("cronometro avviato");
    }

    private void StopStopwatch(bool log)
    {
        if (_swRunning && log)
        {
            float straight = MathF.Sqrt((_swLast.X - _swStart.X) * (_swLast.X - _swStart.X) + (_swLast.Z - _swStart.Z) * (_swLast.Z - _swStart.Z));
            Log($"[color=#ffcf70]cronometro:[/color] {_swTime:F1} s · percorsi {_swDist:F1} m · in linea d'aria {straight:F1} m");
        }
        _swRunning = false;
        if (_stopwatchLabel != null && !log) _stopwatchLabel.Text = "fermo";
    }

    /// <summary>
    /// Un bersaglio davanti al giocatore, a sei metri, sul pavimento: serve a provare
    /// danno, zona colpita e rinascita senza uscire dalla stanza.
    /// </summary>
    private void SpawnBot()
    {
        var me = _sim?.Get(1);
        if (me == null || _targets == null) return;
        float floorY = me.Pos.Y - (me.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
        var fwd = new Vec3(MathF.Sin(_yaw), 0f, MathF.Cos(_yaw));
        var p = me.Pos + fwd * 6f;
        var t = _targets.Spawn(new Vec3(p.X, floorY, p.Z), _botId++);
        Log($"bersaglio {t.Id} a 6 m · {TargetState.MaxHealth:F0} punti vita · testa ×{Combat.HeadshotMultiplier:F1}");
    }

    /// <summary>Cinque bersagli a distanze crescenti: la prova del falloff.</summary>
    private void SpawnTargets()
    {
        var me = _sim?.Get(1);
        if (me == null || _targets == null) return;
        float floorY = me.Pos.Y - (me.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
        _targets.SpawnLine(me.Pos, _yaw, floorY, 5);
        Log("[color=#7fd0ff]BERSAGLI:[/color] cinque sagome da 11 a 23 m · danno pieno vicino, ridotto lontano");
    }

    private void DumpWeapons()
    {
        Log("[color=#7fd0ff]--- ARSENALE (doc 11) ---[/color]");
        foreach (var d in Arsenal.All)
        {
            Log($"{d.Name,-10} {d.ShotDamage,4} danni  {d.RoundsPerMinute,5:F0} c/min  {d.Magazine,2}/{d.Reserve,-3} " +
                $"{d.ReloadTactical:F1}/{d.ReloadEmpty:F1}s  cono {d.SpreadAds:F3}°/{d.SpreadStill:F2}°" +
                (d.Pellets > 1 ? $"  {d.Pellets} palle" : "") +
                (d.Silenced ? "  silenziosa" : "") +
                (d.Melee ? "  mischia" : "") +
                (d.Price > 0 ? $"  ${d.Price}" : "  gratis"));
        }
        Log($"{Arsenal.Count} armi · {Arsenal.All.Length} slot");
    }

    // ================================================================ CICLO

    public override void _UnhandledInput(InputEvent @event)
    {
        bool shopOpen = ShopPanel.IsOpen;

        if (@event is InputEventMouseMotion mm && InputConfig.Captured && !shopOpen)
        {
            // la sensibilità segue il campo visivo: col 6× del Longshot un pixel di mouse
            // vale sei volte meno, altrimenti il cannocchiale è impossibile da usare
            float sens = 0.0025f * (_cam != null ? _cam.Fov / 85f : 1f);
            _yaw -= mm.Relative.X * sens;
            _pitch = Mathf.Clamp(_pitch - mm.Relative.Y * sens, -1.55f, 1.55f);
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed && !InputConfig.Captured
                 && !SettingsPanel.IsOpen && !shopOpen)
        {
            InputConfig.Capture();
        }
        else if (@event is InputEventMouseButton lmb && lmb.Pressed
                 && lmb.ButtonIndex == MouseButton.Left && InputConfig.Captured && !shopOpen)
        {
            // il polling in _Process guarda lo stato del mouse una volta per frame:
            // un click più rapido di un frame si perderebbe. Qui si contano gli eventi.
            _pendingFire = true;
        }
        else if (@event is InputEventKey b && b.Pressed && !b.Echo && b.Keycode == Key.B && !SettingsPanel.IsOpen)
        {
            ToggleShop();
        }
        else if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape && !shopOpen)
        {
            if (InputConfig.Captured) InputConfig.Release();
            else GetTree().ChangeSceneToFile("res://client/main_menu.tscn");
        }
        else if (@event is InputEventKey f && f.Pressed && !f.Echo && f.Keycode == Key.F10)
        {
            SettingsPanel.Open(this, _governor);
        }
        else if (@event is InputEventKey d8 && d8.Pressed && !d8.Echo && d8.Keycode == Key.F8)
        {
            ToggleDebug();
        }
        else if (@event is InputEventKey w && w.Pressed && !w.Echo && SlotKey(w.Keycode) is int key && !shopOpen)
        {
            var slot = _loadout.ForSlotKey(key);
            if (slot.HasValue) EquipWeapon(slot.Value);
            else Log("[color=#ffcf70]non hai una primaria: comprala con B[/color]");
        }
        else if (@event is InputEventMouseButton wheel && InputConfig.Captured && !shopOpen
                 && (wheel.ButtonIndex == MouseButton.WheelUp || wheel.ButtonIndex == MouseButton.WheelDown))
        {
            // la rotella gira solo fra gli slot che hai: primaria, secondaria, coltello
            var next = _loadout.NextHeld(_weapon.Slot, wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
            EquipWeapon(next);
        }
    }

    /// <summary>
    /// La colonna di debug (dati, teleport, cronometro, log) si apre e si chiude con F8.
    /// Quando è chiusa, anche il punteggio torna a filo del bordo: niente spazio sprecato.
    /// </summary>
    private void ToggleDebug()
    {
        _debugOpen = !_debugOpen;
        if (_dock != null) _dock.Visible = _debugOpen;
        if (_killsLabel != null)
        {
            _killsLabel.OffsetLeft = _debugOpen ? DockWidth + 18 : 18;
            _killsLabel.OffsetRight = (_debugOpen ? DockWidth : 0) + 320;
        }
        Log(_debugOpen ? "debug aperto (F8)" : "debug chiuso (F8)");
    }

    /// <summary>Apre o chiude lo shop (B). Chiudendolo il mouse torna al gioco.</summary>
    private void ToggleShop()
    {
        if (ShopPanel.IsOpen)
        {
            ShopPanel.CloseIfOpen();
            return;
        }
        ShopPanel.Open(this, _loadout, _weapon.Slot, OnShopEquip, () =>
        {
            Log("shop chiuso");
            if (!SettingsPanel.IsOpen) InputConfig.Capture();
        });
        Log($"[color=#7fd0ff]SHOP[/color] aperto · {(_loadout.Unlimited ? "soldi illimitati (debug)" : "$" + _loadout.Money)}");
    }

    private void OnShopEquip(WeaponSlot slot)
    {
        EquipWeapon(slot);
        var def = Arsenal.Get(slot);
        Log($"in mano: [color=#7fd0ff]{def.Name}[/color]" +
            (def.IsPrimary ? " · primaria" : " · secondaria") +
            $" · {(_loadout.Unlimited ? "∞" : "$" + _loadout.Money)}");
    }

    /// <summary>Compra dall'elenco del dock (in debug i soldi sono illimitati).</summary>
    private void BuyWeapon(WeaponSlot slot)
    {
        var def = Arsenal.Get(slot);
        var result = _loadout.Buy(slot);
        if (result is BuyResult.Bought or BuyResult.Equipped)
        {
            EquipWeapon(slot);
            Log($"[color=#7fd0ff]{def.Name}[/color] " +
                (result == BuyResult.Bought ? $"comprata (${def.Price})" : "già tua, equipaggiata"));
        }
        else
        {
            Log($"[color=#ffcf70]{def.Name}: non comprabile ({result})[/color]");
        }
    }

    /// <summary>I tasti 1, 2 e 3 sono i tre posti dell'arsenale, non dieci armi.</summary>
    private static int? SlotKey(Key k) => k switch
    {
        Key.Key1 => 1,
        Key.Key2 => 2,
        Key.Key3 => 3,
        _ => null
    };

    /// <summary>Mette in mano un'arma: simulazione (munizioni e contatori) e viewmodel.</summary>
    private void EquipWeapon(WeaponSlot slot)
    {
        if (_sim == null) return;
        Simulation.Equip(_sim, 1, slot);
        _weapon.Equip(slot);
        // i contatori degli eventi vivono NELL'ARMA e non ripartono da zero quando la
        // riprendi in mano: azzerarli qui faceva credere al client che fosse partito
        // un colpo, e al primo frame dopo l'1/2 si sentiva uno sparo fantasma.
        _seenShots = _sim.Get(1)?.Weapon.Shots ?? 0;
        _recoilShots = 0;
        _kickPitch = _kickYaw = 0f;
        var def = Arsenal.Get(slot);
        _crosshair?.SetProfile(def.Profile);
        Log($"arma: [color=#7fd0ff]{def.Name}[/color] · {def.Class} · " +
            (def.Melee ? "mischia" : $"{def.Magazine}/{def.Reserve} · {def.RoundsPerMinute:F0} c/min") +
            (def.Pellets > 1 ? $" · {def.Pellets} palle" : ""));
    }

    /// <summary>
    /// I modificatori NON si leggono con IsPhysicalKeyPressed: quella funzione
    /// cerca una POSIZIONE sulla tastiera, e Ctrl/Shift non ne hanno una.
    /// Si legge lo stato del tasto, con il fallback fisico per sicurezza.
    /// </summary>
    private static bool ShiftDown()
        => Godot.Input.IsKeyPressed(Key.Shift) || Godot.Input.IsPhysicalKeyPressed(Key.Shift);

    private static bool CtrlDown()
        => Godot.Input.IsKeyPressed(Key.Ctrl) || Godot.Input.IsPhysicalKeyPressed(Key.Ctrl);

    /// <summary>
    /// Un colpo appena partito: la simulazione ha già fissato cono e seme, quindi la
    /// traiettoria è ricalcolabile da chiunque (un domani anche dal server). Si traccia
    /// il raggio contro le scatole del mondo e si disegna la traccia dal vivo di canna.
    /// </summary>
    private void FireTracer(PlayerState p)
    {
        var def = Arsenal.Get(p.Weapon.Slot);
        var eye = new Vec3(_cam.GlobalPosition.X, _cam.GlobalPosition.Y, _cam.GlobalPosition.Z);

        // il coltello non spara: niente traccia e niente bossolo, ma TAGLIA
        if (def.Melee)
        {
            float my = _yaw + _kickYaw, mp = _pitch + _kickPitch;
            var fwd = new Vec3(MathF.Sin(my) * MathF.Cos(mp), MathF.Sin(mp), MathF.Cos(my) * MathF.Cos(mp));
            var dirv = Vec3.Normalize(fwd);
            var cut = Combat.Melee(_world, _targets?.All, eye, dirv, def);
            if (cut.Target >= 0)
            {
                _targets?.RegisterHit(cut);
                _hitmark?.Flash(cut.Killed);
                if (cut.Killed) _feedLife = 3.2f;
            }
            return;
        }

        // la dispersione è nel seme del colpo: due client ottengono la stessa traiettoria.
        // Le pompe sparano PELLETS palle: stesso seme, semi derivati, stesso ordine.
        var muzzle = _weapon.Muzzle();

        // la canna punta dove guarda la camera, rinculo compreso: si spara dove si vede
        float aimYaw = _yaw + _kickYaw;
        float aimPitch = _pitch + _kickPitch;

        bool anyKill = false;
        int totalDamage = 0;
        bool anyHit = false;
        for (int i = 0; i < def.Pellets; i++)
        {
            // la palla parte lungo la camera VERA, rinculo compreso: è quello che
            // rende onesto il pattern (prima si sparava sempre al punto pre-rinculo)
            Vec3 dir = Arsenal.PelletDirection(aimYaw, aimPitch, p.Weapon.ShotSpread, p.Weapon.ShotSeed, i, def.Pellets);
            // la stessa funzione che userà il server: muri, testa o corpo, danno con la distanza
            var shot = Combat.Shoot(_world, _targets?.All, eye, dir, def);
            Vec3 end = shot.Hit || shot.Target >= 0 ? shot.Point : eye + dir * 250f;

            // muro colpito: un foro sulla faccia, che poi si dissolve
            if (shot.Target == -1)
            {
                string kind = shot.Surface < _world.Kinds.Length ? _world.Kinds[shot.Surface] : "";
                _holes?.Spawn(new Vector3(shot.Point.X, shot.Point.Y, shot.Point.Z),
                              new Vector3(shot.Normal.X, shot.Normal.Y, shot.Normal.Z), kind);
            }
            _tracers.Spawn(muzzle, new Vector3(end.X, end.Y, end.Z), def.Silenced, shot.Hit, smoke: i == 0);

            if (shot.Target >= 0)
            {
                anyHit = true;
                totalDamage += shot.Damage;
                anyKill |= shot.Killed;
                _targets?.RegisterHit(shot);
            }
        }
        if (anyHit)
        {
            _hitmark?.Flash(anyKill);
            if (anyKill) _feedLife = 3.2f;
        }

        // RINCULO alla Valorant: la canna sale molto nei primi colpi, poi meno, e
        // deriva a zig-zag; quando smetti di sparare la mira RIENTRA esattamente
        // dove era prima della raffica.
        float adsK = p.Weapon.AdsT > 0.5f ? 0.72f : 1f;
        _recoilShots++;
        float ramp = 1f - Mathf.Min(0.65f, (_recoilShots - 1) * 0.055f);   // i primi colpi alzano di più
        float upKick = def.RecoilUp * ramp * adsK;
        float sideKick = def.RecoilSide * adsK
                       * Mathf.Sin(_recoilShots * 1.9f)
                       * Mathf.Min(1f, 0.25f + _recoilShots / 10f);
        _kickPitch += Mathf.DegToRad(upKick);
        _kickYaw += Mathf.DegToRad(sideKick);
        _recoilHold = 0.10f;      // per un attimo la mira resta su: poi rientra

        // lampo di volata attaccato alla bocca della canna
        if (_flash != null)
        {
            _flash.Position = muzzle;
            _flash.LightEnergy = def.Silenced ? 0.6f : 2.6f;
            _flash.Visible = true;
            _flashLife = def.Silenced ? 0.02f : 0.045f;
        }

        // il bossolo esce dal finestrino, verso destra e in alto, e cade a terra
        var right = _cam.GlobalBasis.X;
        var up = _cam.GlobalBasis.Y;
        var back = _cam.GlobalBasis.Z;
        float oomph = def.Pellets > 1 ? 1.4f : 1.0f;
        var kick = right * ((float)GD.RandRange(1.1, 1.8) * oomph)
                 + up * ((float)GD.RandRange(0.7, 1.3) * oomph)
                 + back * (float)GD.RandRange(0.10, 0.35);
        _casings.Eject(_world, _weapon.EjectPort(), kick);
    }

    /// <summary>Quanti pixel sullo schermo vale mezzo cono di dispersione, in gradi.</summary>
    private float ConeToPixels(float spreadDeg)
    {
        float half = GetViewport().GetVisibleRect().Size.Y * 0.5f;
        float tanHalfFov = Mathf.Tan(Mathf.DegToRad(_cam.Fov * 0.5f));
        if (tanHalfFov < 1e-4f) return 0f;
        return Mathf.Tan(Mathf.DegToRad(Mathf.Max(0f, spreadDeg))) / tanHalfFov * half;
    }

    public override void _Process(double delta)
    {
        // con lo shop aperto il giocatore è fermo: il mouse serve al pannello
        bool playing = !ShopPanel.IsOpen;

        var cmd = new InputCmd
        {
            Yaw = _yaw,
            Pitch = _pitch,
            Move = playing ? new Vec2(
                (Godot.Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
                (Godot.Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0)) : Vec2.Zero,
            Jump = playing && Godot.Input.IsPhysicalKeyPressed(Key.Space),
            Sprint = playing && ShiftDown(),
            Crouch = playing && CtrlDown(),
            // il grilletto è uno stato, non un fronte: la simulazione ricava il click
            // (FireLatch) e così tenere premuto funziona anche per raffica e automatiche
            Fire = playing && (_pendingFire || _fireHeld),
            Reload = playing && Godot.Input.IsPhysicalKeyPressed(Key.R),
            Ads = playing && InputConfig.Captured && Godot.Input.IsMouseButtonPressed(MouseButton.Right)
        };

        bool fireDown = InputConfig.Captured && Godot.Input.IsMouseButtonPressed(MouseButton.Left);
        if (fireDown && !_fireHeld) _pendingFire = true;
        _fireHeld = fireDown;
        _acc += delta;
        int guard = 0;
        while (_acc >= Constants.Dt && guard++ < 8)
        {
            _acc -= Constants.Dt;
            Simulation.ApplyInput(_sim, 1, cmd, _world);
            Simulation.ApplyWeapon(_sim, 1, cmd.Fire, cmd.Reload, cmd.Ads);
            _pendingFire = false;
        }

        _targets?.Tick((float)delta);
        if (_killsLabel != null && _targets != null)
        {
            _killsLabel.Text = $"{_targets.Kills} abbattuti   ·   {_targets.Score} punti   ·   {_targets.AliveCount} in piedi";
            if (_targets.Kills != _lastKills) _lastKills = _targets.Kills;
            if (_feedLife > 0f)
            {
                _feedLife -= (float)delta;
                var c = _feedLabel.GetThemeColor("font_color");
                c.A = Mathf.Clamp(_feedLife, 0f, 1f);
                _feedLabel.AddThemeColorOverride("font_color", c);
                _feedLabel.Text = _targets.LastEvent;
            }
            else if (_feedLabel.Text != "")
            {
                _feedLabel.Text = "";
            }
        }

        var p = _sim.Get(1);
        if (p != null)
        {
            float halfY = (p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
            _cam.Position = new Vector3(p.Pos.X, p.Pos.Y + halfY - 0.25f, p.Pos.Z);
            _cam.Rotation = new Vector3(p.Pitch + _kickPitch, p.Yaw + _kickYaw, 0);

            // il rinculo rientra: prima veloce, poi sempre più lento (non è un elastico).
            // Dopo 0,10 s dall'ultimo colpo la mira torna ESATTAMENTE dove stavi mirando.
            float dtf = (float)delta;
            if (_recoilHold > 0f) _recoilHold -= dtf;
            // rate in RADIANTI al secondo: 0,35 rad/s ≈ 20°/s, che riporta a posto una
            // raffica di 5° in circa 0,25 s (prima era 3,2 rad/s: rientro istantaneo)
            float rate = _recoilHold > 0f ? 0f : Mathf.Max(0.35f, Mathf.Abs(_kickPitch) * 6f);
            _kickPitch = Mathf.MoveToward(_kickPitch, 0f, dtf * rate);
            _kickYaw = Mathf.MoveToward(_kickYaw, 0f, dtf * Mathf.Max(0.35f, Mathf.Abs(_kickYaw) * 6f));
            if (Mathf.Abs(_kickPitch) < 0.0005f && Mathf.Abs(_kickYaw) < 0.0005f) _recoilShots = 0;
            if (_flash != null && _flashLife > 0f)
            {
                _flashLife -= dtf;
                if (_flashLife <= 0f) _flash.Visible = false;
            }

            _weapon.Sync(p.Weapon, (float)delta);
            _cam.Fov = Mathf.Lerp(85f, _weapon.AdsFov, p.Weapon.AdsT);   // l'ottica del cecchino chiude di più

            // un colpo appena partito: la simulazione ha già deciso cono e munizioni,
            // qui si disegna solo la traccia
            if (p.Weapon.Shots != _seenShots)
            {
                _seenShots = p.Weapon.Shots;
                FireTracer(p);
            }

            if (_ammoLabel != null)
            {
                var def = Arsenal.Get(p.Weapon.Slot);
                _weaponLabel.Text = $"{def.Name.ToUpperInvariant()} · {def.Class.ToUpperInvariant()} · {def.OpticName.ToUpperInvariant()}";

                // modo di fuoco: ogni arma ne ha uno solo, niente selettore
                _modeLabel.Text = def.Melee ? "MISCHIA" : WeaponDef.ModeName(p.Weapon.Mode);

                _moneyLabel.Text = _loadout.Unlimited
                    ? "∞ soldi (debug)   ·   B shop"
                    : $"${_loadout.Money:N0}   ·   B shop";
                _moneyLabel.Text += _loadout.Primary.HasValue
                    ? $"\nP {Arsenal.Get(_loadout.Primary.Value).Name}  ·  S {Arsenal.Get(_loadout.Secondary).Name}"
                    : $"\nP —  ·  S {Arsenal.Get(_loadout.Secondary).Name}";
                _ammoLabel.Text = def.Melee
                    ? "MISCHIA"
                    : p.Weapon.Reloading ? "RICARICA…" : $"{p.Weapon.Ammo} / {p.Weapon.Reserve}";
                _ammoLabel.AddThemeColorOverride("font_color",
                    !def.Melee && p.Weapon.Ammo == 0 ? new Color(0.95f, 0.45f, 0.35f) : new Color(0.93f, 0.94f, 0.96f));
            }

            // il mirino è la proiezione del cono VERO sullo schermo: quello che vedi
            // è quello che la simulazione spara (fermo, in movimento, in mira)
            // l'ottica: maschera e reticolo per i cannocchiali, puntino per i dot.
            // Quando è in faccia il mirino normale si nasconde (due mirini = confusione).
            var optic = Arsenal.Get(p.Weapon.Slot);
            _scope?.Set(p.Weapon.AdsT, optic, ConeToPixels(p.Weapon.AdsT > 0f ? p.Weapon.Spread : 0f));
            // col cannocchiale in faccia si nasconde anche l'arma: col FOV a 11° il
            // viewmodel coprirebbe tutto il cerchio (come fanno i giochi veri)
            bool scopedIn = optic.Scoped && p.Weapon.AdsT > 0.55f;
            // col punto rosso o col cannocchiale in faccia il mirino normale si nasconde:
            // il puntino lo copriva (e due riferimenti sovrapposti non aiutano nessuno)
            bool opticIn = scopedIn || (optic.DotSight && p.Weapon.AdsT > 0.5f);
            if (_crosshair != null) _crosshair.Visible = !opticIn;
            if (_weapon != null) _weapon.Visible = !scopedIn;

            _crosshair.SetSpread(ConeToPixels(p.Weapon.Spread));
            if (_spreadLabel != null)
            {
                var def = Arsenal.Get(p.Weapon.Slot);
                _spreadLabel.Text = def.Melee
                    ? $"mischia · {def.Damage} danni · {def.RoundsPerMinute:F1} colpi/s"
                    : $"cono {p.Weapon.Spread:F2}°   ·   {def.RoundsPerMinute:F0} c/min" +
                      (def.Pellets > 1 ? $"   ·   {def.Pellets} palle" : "") +
                      (p.Weapon.Bloom > 0.01f ? $"   (bloom +{p.Weapon.Bloom:F2}°)" : "");
            }

            if (_swRunning)
            {
                _swTime += delta;
                float dx = p.Pos.X - _swLast.X, dz = p.Pos.Z - _swLast.Z;
                _swDist += MathF.Sqrt(dx * dx + dz * dz);
                _swLast = p.Pos;
                _stopwatchLabel.Text = $"{_swTime:F1} s · {_swDist:F1} m percorsi · media {(_swTime > 0 ? _swDist / _swTime : 0):F2} m/s";
            }

            _state.Text =
                $"mappa    {_world.Name}\n" +
                $"tick     {_sim.Tick}\n" +
                $"pos      {p.Pos.X,7:F2} {p.Pos.Y,6:F2} {p.Pos.Z,7:F2}\n" +
                $"vel      {p.Vel.X,7:F2} {p.Vel.Y,6:F2} {p.Vel.Z,7:F2}\n" +
                $"suolo    {p.OnFloor}      accovacciato {p.Crouching}\n" +
                $"stamina  {p.Stamina,4:F1}     rete {_simLatencyMs:F0} ms · loss {_simLossPct:F0}%\n" +
                $"mouse    {(InputConfig.Captured ? "catturato (raw)" : "libero")}";
        }

        _perfTimer += delta;
        if (_perfTimer >= 0.2)
        {
            _perfTimer = 0;
            _perfLabel.Text = Perf.Line() +
                $"\nrender  {GraphicsSettings.Renderer} · {GraphicsSettings.PresetName(GraphicsSettings.CurrentPreset)}" +
                $"\nlatenza {(GraphicsSettings.LowLatency ? _governor?.Mode ?? "—" : "bassa latenza spenta")}";
        }
    }
}
