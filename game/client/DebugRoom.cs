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
    private RichTextLabel _log;
    private Crosshair _crosshair;
    private Node3D _botRoot;

    private WorldState _world;
    private SimState _sim;
    private float _yaw, _pitch;
    private double _acc;
    private double _perfTimer;
    private int _botId = 100;
    private float _simLatencyMs;
    private float _simLossPct;
    private string _profile = "rifle";

    private readonly (string Name, Vector3 Pos)[] _pads =
    {
        ("PIAZZA", new Vector3(0f, 1.0f, 0f)),
        ("CASSA", new Vector3(3.25f, 1.4f, 6.25f)),
        ("TORRE", new Vector3(8f, 5.4f, -4f)),
        ("MURO", new Vector3(-6f, 1.0f, 0f)),
        ("GRADINO", new Vector3(-1f, 0.6f, 9f)),
        ("RESET", Vector3.Zero),
    };

    public override void _Ready()
    {
        _world = WorldState.MakeTestWorld();
        _sim = new SimState();
        Simulation.Spawn(_sim, 1, new Vec3(0f, 0.2f, 3f));

        BuildWorld();
        BuildUi();
        InputConfig.FpsDefaults();

        Log("[color=#7fd0ff]Stanza debug pronta.[/color] Mondo: " + _world.Boxes.Count + " volumi.");
        Log("[color=#ffcf70]IN COSTRUZIONE:[/color] armi, economia, round, anti-cheat.");
        Log("ESC libera il mouse · clicca nella vista per ricatturarlo.");
    }

    // ================================================================== MONDO

    private static StandardMaterial3D Mat(string hex, float rough)
        => new() { AlbedoColor = Color.FromHtml(hex), Roughness = rough };

    private void BuildWorld()
    {
        // Palette del doc 07: pietra calcarea, cemento, legno, acciaio.
        var matFloor = Mat("#6E6A62", 0.92f);   // cemento consumato
        var matWall = Mat("#C9BFA8", 0.88f);    // pietra calcarea
        var matWood = Mat("#8A6B4A", 0.85f);    // legno / cassa
        var matSteel = Mat("#55585C", 0.55f);   // acciaio

        foreach (var b in _world.Boxes)
        {
            bool isFloor = b.Size.X > 50f || b.Size.Z > 50f;
            bool isTall = b.Size.Y > 3.0f;
            bool isLow = b.Size.Y <= 1.3f;

            var m = isFloor ? matFloor
                  : isTall ? matSteel
                  : isLow ? matWood
                  : matWall;

            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(b.Size.X, b.Size.Y, b.Size.Z) },
                Position = new Vector3(b.Center.X, b.Center.Y, b.Center.Z),
                MaterialOverride = m
            });
        }

        // pad di teleport: emissivi, così si trovano a colpo d'occhio
        var padMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 0.30f, 0.48f),
            EmissionEnabled = true,
            Emission = new Color(0.12f, 0.55f, 0.85f),
            EmissionEnergyMultiplier = 1.1f,
            Roughness = 0.5f
        };
        foreach (var pad in _pads)
        {
            if (pad.Name == "RESET") continue;
            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.6f, 0.05f, 1.6f) },
                Position = pad.Pos,
                MaterialOverride = padMat
            });
        }

        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-42f, -34f, 0f),
            LightColor = new Color(1f, 0.96f, 0.88f),
            LightEnergy = 1.35f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 90f
        };
        AddChild(sun);

        var skyMat = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.22f, 0.44f, 0.72f),
            SkyHorizonColor = new Color(0.80f, 0.84f, 0.88f),
            GroundHorizonColor = new Color(0.62f, 0.64f, 0.66f),
            GroundBottomColor = new Color(0.24f, 0.25f, 0.27f)
        };

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = skyMat },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 0.95f,
            FogEnabled = true,
            FogLightColor = new Color(0.76f, 0.81f, 0.87f),
            FogDensity = 0.004f
        };
        AddChild(new WorldEnvironment { Environment = env });

        _cam = new Camera3D { Fov = 85f, Near = 0.05f, Far = 400f, Current = true };
        AddChild(_cam);

        _botRoot = new Node3D();
        AddChild(_botRoot);
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
        inner.AddChild(Section("TELEPORT"));
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        foreach (var pad in _pads)
        {
            var target = pad.Pos;
            var name = pad.Name;
            grid.AddChild(Btn(name, () =>
            {
                if (name == "RESET") { ResetScenario(); return; }
                Teleport(target);
            }));
        }
        inner.AddChild(grid);

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
        scen.AddChild(Btn("SPAWN BOT", SpawnBot));
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
        _crosshair = new Crosshair();
        layer.AddChild(_crosshair);
        _crosshair.SetProfile(_profile);

        // ---------- hint
        var hint = new Label
        {
            Text = "WASD muoviti · mouse guarda · SPACE salta · SHIFT sprint · CTRL accovacciati · ESC mouse",
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

    private void Teleport(Vector3 pos)
    {
        var p = _sim.Get(1);
        if (p == null) return;
        p.Pos = new Vec3(pos.X, pos.Y + Constants.Height * 0.5f, pos.Z);
        p.Vel = Vec3.Zero;
        Log($"teleport → {pos.X:F1}, {pos.Z:F1}");
    }

    private void ResetScenario()
    {
        _sim = new SimState();
        Simulation.Spawn(_sim, 1, new Vec3(0f, 0.2f, 3f));
        foreach (var c in _botRoot.GetChildren()) c.QueueFree();
        Log("scenario azzerato");
    }

    private void SpawnBot()
    {
        int id = _botId++;
        var p = _sim.GetOrCreate(id);
        p.Pos = new Vec3(2f, Constants.Height * 0.5f, 0f);
        p.Vel = Vec3.Zero;

        _botRoot.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = 0.32f, Height = 1.8f },
            Position = new Vector3(p.Pos.X, p.Pos.Y, p.Pos.Z),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.33f, 0.18f) }
        });
        Log($"bot {id} spawnato");
    }

    private void DumpWeapons()
    {
        Log("[color=#7fd0ff]--- ARSENALE (doc 11) ---[/color]");
        Log("Standard  25  4/5/6  0.20s 17  gratis");
        Log("Hush      22  5/6/7  0.22s 15  $400  silenziata");
        Log("Snub      50  2/3/3  0.25s  6  $500  ADS 0.16");
        Log("Hammer    70  2/2/2  0.55s  7  $700");
        Log("Chatter   18  6/7/8  1000rpm 20 $600");
        Log("Cadence   20  5/6/7  800rpm  30 $900");
        Log("Blitz     16  7/8/9  1150rpm 30 $1000");
        Log("Scrap     17  6/8/9  900rpm  20 $600  ECO");
        Log("Choke    130  1/1/2  0.90s  6  $1100");
        Log("Sweeper  100  1/2/2  0.35s  7  $1400");
        Log("Anvil     30  4/4/5  600rpm 30 $1400");
        Log("Service   25  4/5/6  700rpm 30 $1400");
        Log("Vantage   60  2/2/3  0.40s 20  $1800");
        Log("Spotter   75  2/2/2  0.35s 10  $1800");
        Log("Longshot 150  1/1/1  1.40s  5  $2400");
    }

    // ================================================================ CICLO

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm && InputConfig.Captured)
        {
            _yaw -= mm.Relative.X * 0.0025f;
            _pitch = Mathf.Clamp(_pitch - mm.Relative.Y * 0.0025f, -1.55f, 1.55f);
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed && !InputConfig.Captured)
        {
            InputConfig.Capture();
        }
        else if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape)
        {
            if (InputConfig.Captured) InputConfig.Release();
            else GetTree().ChangeSceneToFile("res://client/main_menu.tscn");
        }
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

    public override void _Process(double delta)
    {
        var cmd = new InputCmd
        {
            Yaw = _yaw,
            Pitch = _pitch,
            Move = new Vec2(
                (Godot.Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
                (Godot.Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0)),
            Jump = Godot.Input.IsPhysicalKeyPressed(Key.Space),
            Sprint = ShiftDown(),
            Crouch = CtrlDown()
        };

        _acc += delta;
        int guard = 0;
        while (_acc >= Constants.Dt && guard++ < 8)
        {
            _acc -= Constants.Dt;
            Simulation.ApplyInput(_sim, 1, cmd, _world);
        }

        var p = _sim.Get(1);
        if (p != null)
        {
            float halfY = (p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
            _cam.Position = new Vector3(p.Pos.X, p.Pos.Y + halfY - 0.25f, p.Pos.Z);
            _cam.Rotation = new Vector3(p.Pitch, p.Yaw, 0);

            float hspeed = Mathf.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Z * p.Vel.Z);
            _crosshair.SetSpread(Mathf.Clamp(hspeed * 1.1f, 0f, 14f) + (p.OnFloor ? 0f : 3f));

            _state.Text =
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
            _perfLabel.Text = Perf.Line();
        }
    }
}
