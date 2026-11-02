using Falezia.Net;
using Falezia.Sim;
using Godot;
using Vec3 = System.Numerics.Vector3;
using Vec2 = System.Numerics.Vector2;

/// <summary>
/// CLIENT. Predice il proprio movimento, riconcilia con gli snapshot del server,
/// mostra HUD e netgraph. Il server ha sempre l'ultima parola.
/// </summary>
public partial class ClientMain : Node3D
{
    private static readonly Color Ink = new(0.93f, 0.95f, 0.98f);
    private static readonly Color Dim = new(0.60f, 0.66f, 0.74f);
    private static readonly Color Accent = new(0.42f, 0.78f, 0.96f);
    private static readonly Color Warn = new(0.95f, 0.74f, 0.36f);

    private readonly PacketPeerUdp _peer = new();
    private readonly List<InputCmd> _pending = new();
    private readonly Dictionary<int, Vec3> _history = new();
    private readonly Dictionary<int, ulong> _sendTimes = new();
    private readonly Dictionary<int, MeshInstance3D> _remoteMeshes = new();

    private WorldState _world;
    private SimState _state;
    private int _myId;
    private int _localTick;
    private double _acc;
    private double _rttMs;
    private float _lastError;
    private int _corrections;
    private long _snapshots;
    private long _tx;

    private float _yaw, _pitch;
    private Camera3D _cam;
    private Atmosphere _atm;
    private LatencyGovernor _governor;
    private Crosshair _crosshair;
    private Label _tl, _tr, _tc, _bl, _br, _hint;

    public override void _Ready()
    {
        GraphicsSettings.ApplyGlobal();
        _world = MapFiles.Load();
        BuildWorld();
        BuildHud();
        InputConfig.FpsDefaults();

        _governor = new LatencyGovernor { Name = "BassaLatenza" };
        AddChild(_governor);
        GraphicsSettings.Changed += ApplyGraphics;
        ApplyGraphics();

        var err = _peer.ConnectToHost(Constants.ServerHost, Constants.ServerPort);
        GD.Print(err == Error.Ok
            ? $"[client] connesso a {Constants.ServerHost}:{Constants.ServerPort}"
            : $"[client] errore connessione: {err}");
    }

    // ================================================================= MONDO

    private void BuildWorld()
    {
        // la STESSA mappa del server: stesse scatole, stesso ordine, stessa simulazione
        MapView.Build(this, _world);
        _atm = Atmosphere.Create(this);

        _cam = new Camera3D { Fov = 85f, Near = 0.05f, Far = 440f, Current = true };
        AddChild(_cam);
    }

    private void ApplyGraphics()
    {
        GraphicsSettings.ApplyViewport(GetViewport());
        GraphicsSettings.ApplyScene(_atm, _cam);
    }

    public override void _ExitTree()
    {
        GraphicsSettings.Changed -= ApplyGraphics;
    }

    // ==================================================================== HUD

    private Label Hud(string text, int size, Color color, Vector2 anchor,
                      Vector2 min, Vector2 max, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var l = new Label { Text = text, HorizontalAlignment = align };
        l.AnchorLeft = anchor.X; l.AnchorTop = anchor.Y;
        l.AnchorRight = anchor.X; l.AnchorBottom = anchor.Y;
        l.OffsetLeft = min.X; l.OffsetTop = min.Y;
        l.OffsetRight = max.X; l.OffsetBottom = max.Y;
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        l.AddThemeConstantOverride("shadow_offset_x", 1);
        l.AddThemeConstantOverride("shadow_offset_y", 1);
        return l;
    }

    private void BuildHud()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        _tl = Hud("FALEZIA\nin attesa del server…", 13, Ink, new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(560f, 90f));
        layer.AddChild(_tl);

        _tc = Hud("ROUND —", 15, Dim, new Vector2(0.5f, 0f), new Vector2(-200f, 14f), new Vector2(200f, 42f), HorizontalAlignment.Center);
        layer.AddChild(_tc);

        _tr = Hud("", 12, Accent, new Vector2(1f, 0f), new Vector2(-620f, 14f), new Vector2(-18f, 76f), HorizontalAlignment.Right);
        layer.AddChild(_tr);

        _bl = Hud("VITA  —      PIASTRE  —", 15, Ink, new Vector2(0f, 1f), new Vector2(18f, -52f), new Vector2(420f, -20f));
        layer.AddChild(_bl);

        _br = Hud("MUNIZIONI  — / —", 15, Ink, new Vector2(1f, 1f), new Vector2(-420f, -52f), new Vector2(-18f, -20f), HorizontalAlignment.Right);
        layer.AddChild(_br);

        _hint = Hud("ESC libera il mouse · clicca per ricatturare · F10 impostazioni", 11, Dim,
            new Vector2(0.5f, 1f), new Vector2(-300f, -26f), new Vector2(300f, -6f), HorizontalAlignment.Center);
        layer.AddChild(_hint);

        _crosshair = new Crosshair();
        layer.AddChild(_crosshair);
        _crosshair.SetProfile("rifle");
    }

    // ================================================================== INPUT

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm && InputConfig.Captured)
        {
            _yaw -= mm.Relative.X * 0.0025f;
            _pitch = Mathf.Clamp(_pitch - mm.Relative.Y * 0.0025f, -1.55f, 1.55f);
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed && !InputConfig.Captured && !SettingsPanel.IsOpen)
        {
            InputConfig.Capture();
        }
        else if (@event is InputEventKey k && k.Pressed)
        {
            if (k.Keycode == Key.Escape)
            {
                if (InputConfig.Captured) InputConfig.Release();
                else GetTree().ChangeSceneToFile("res://client/main_menu.tscn");
            }
            else if (k.Keycode == Key.F1)
            {
                InputConfig.Capture();
            }
            else if (k.Keycode == Key.F10)
            {
                SettingsPanel.Open(this, _governor);
            }
        }
    }

    private InputCmd SampleInput()
    {
        return new InputCmd
        {
            Tick = ++_localTick,
            Yaw = _yaw,
            Pitch = _pitch,
            Move = new Vec2(
                (Godot.Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
                (Godot.Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0) - (Godot.Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0)),
            Jump = Godot.Input.IsPhysicalKeyPressed(Key.Space),
            Sprint = Godot.Input.IsKeyPressed(Key.Shift) || Godot.Input.IsPhysicalKeyPressed(Key.Shift),
            Crouch = Godot.Input.IsKeyPressed(Key.Ctrl) || Godot.Input.IsPhysicalKeyPressed(Key.Ctrl),
            Ads = Godot.Input.IsMouseButtonPressed(MouseButton.Right)
        };
    }

    // ================================================================= CICLO

    public override void _Process(double delta)
    {
        _acc += delta;
        int guard = 0;
        while (_acc >= Constants.Dt && guard++ < 8)
        {
            _acc -= Constants.Dt;
            Tick();
        }

        Receive();
        UpdateCamera();
        UpdateRemoteMeshes();
        UpdateHud();
    }

    private void Tick()
    {
        var cmd = SampleInput();

        if (_myId != 0 && _state != null)
        {
            Simulation.ApplyInput(_state, _myId, cmd, _world);
            var p = _state.Get(_myId);
            if (p != null) _history[cmd.Tick] = p.Pos;
        }

        _pending.Add(cmd);
        while (_pending.Count > Constants.InputBuffer) _pending.RemoveAt(0);

        _sendTimes[cmd.Tick] = Time.GetTicksMsec();
        _peer.PutPacket(Protocol.EncodeInput(cmd));
        _tx++;
    }

    private void Receive()
    {
        while (_peer.GetAvailablePacketCount() > 0)
        {
            byte[] data = _peer.GetPacket();
            if (!Protocol.TryDecodeSnapshot(data, out var snap)) continue;
            _snapshots++;
            OnSnapshot(snap);
        }
    }

    private void OnSnapshot(Snapshot snap)
    {
        if (_myId == 0)
        {
            _myId = snap.YourId;
            _state = snap.State;
            var me = _state.Get(_myId);
            if (me != null) _yaw = MathF.Atan2(me.Pos.X, me.Pos.Z);   // si nasce guardando il centro
            GD.Print($"[client] id assegnato: {_myId}");
            return;
        }

        foreach (var kv in snap.State.Players)
        {
            if (kv.Key == _myId) continue;
            if (_state.Players.TryGetValue(kv.Key, out var existing)) existing.CopyFrom(kv.Value);
            else _state.Players[kv.Key] = kv.Value;
        }

        var serverPlayer = snap.State.Get(_myId);
        if (serverPlayer == null) return;

        Vec3 predicted = _history.TryGetValue(snap.Ack, out var h) ? h : serverPlayer.Pos;
        _lastError = Vec3.Distance(predicted, serverPlayer.Pos);

        if (_lastError > Constants.CorrectionThreshold)
        {
            _corrections++;
            var me = _state.Get(_myId);
            if (me != null) me.CopyFrom(serverPlayer);

            _pending.RemoveAll(c => c.Tick <= snap.Ack);
            foreach (var cmd in _pending)
            {
                Simulation.ApplyInput(_state, _myId, cmd, _world);
                var p = _state.Get(_myId);
                if (p != null) _history[cmd.Tick] = p.Pos;
            }
        }

        PruneKeys(_history, snap.Ack);
        if (_sendTimes.TryGetValue(snap.Ack, out ulong sent))
            _rttMs = Time.GetTicksMsec() - sent;
        PruneKeys(_sendTimes, snap.Ack);
    }

    private static void PruneKeys<T>(Dictionary<int, T> d, int ack)
    {
        List<int> old = null;
        foreach (var k in d.Keys)
            if (k <= ack) (old ??= new List<int>()).Add(k);
        if (old != null) foreach (var k in old) d.Remove(k);
    }

    // ================================================================ VISTA

    private void UpdateCamera()
    {
        if (_state == null || _myId == 0) return;
        var p = _state.Get(_myId);
        if (p == null) return;
        float halfY = (p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
        _cam.Position = new Vector3(p.Pos.X, p.Pos.Y + halfY - 0.25f, p.Pos.Z);
        _cam.Rotation = new Vector3(p.Pitch, p.Yaw, 0);
    }

    private void UpdateRemoteMeshes()
    {
        if (_state == null) return;
        foreach (var kv in _state.Players)
        {
            if (kv.Key == _myId) continue;
            if (!_remoteMeshes.TryGetValue(kv.Key, out var mesh))
            {
                // colore di squadra (doc 07): ciano Nord, arancio Sud. È informazione, quindi è saturo.
                var team = MapFiles.IsNorth(kv.Key) ? new Color("#2E9BE6") : new Color("#E8632E");
                mesh = new MeshInstance3D
                {
                    Mesh = new CapsuleMesh { Radius = 0.32f, Height = 1.8f },
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = team, Roughness = 0.7f }
                };
                AddChild(mesh);
                _remoteMeshes[kv.Key] = mesh;
            }
            mesh.Position = new Vector3(kv.Value.Pos.X, kv.Value.Pos.Y, kv.Value.Pos.Z);
        }
    }

    private void UpdateHud()
    {
        var p = _state?.Get(_myId);

        _tl.Text = _myId == 0
            ? "FALEZIA\nconnessione al server…"
            : $"FALEZIA · {_world.Name}      id {_myId} · {(MapFiles.IsNorth(_myId) ? "Nord, Porto Vecchio" : "Sud, Forte San Verano")}      tick {_localTick}";

        _tr.Text =
            $"fps  {Engine.GetFramesPerSecond(),4:F0}\nrtt  {_rttMs,4:F0} ms\n" +
            $"err  {_lastError * 100f,5:F1} cm\n" +
            $"corr {_corrections}\n" +
            $"pend {_pending.Count}";

        if (p != null)
        {
            float hspeed = Mathf.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Z * p.Vel.Z);
            float spread = Mathf.Clamp(hspeed * 1.1f, 0f, 14f) + (p.OnFloor ? 0f : 3f);
            _crosshair.SetSpread(spread);
            _hint.Text = InputConfig.Captured
                ? "ESC mouse   ·   F1 ricattura   ·   F10 impostazioni"
                : "mouse libero · clicca per ricatturare · F10 impostazioni";
        }
    }
}
