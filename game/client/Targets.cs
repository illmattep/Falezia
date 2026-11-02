using System.Collections.Generic;
using Falezia.Sim;
using Godot;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// I bersagli della stanza debug: omini fatti di scatole (le stesse che colpisce
/// <see cref="Combat"/>), barra della vita sopra la testa, numeri del danno che
/// salgono e svaniscono, e la caduta quando muoiono.
///
/// La logica di chi prende cosa sta nella simulazione: qui c'è solo quello che si vede.
/// </summary>
public partial class Targets : Node3D
{
    private sealed class View
    {
        public TargetState State;
        public Node3D Root;
        public Node3D Body;
        public MeshInstance3D Torso;
        public MeshInstance3D Head;
        public MeshInstance3D BarBack;
        public MeshInstance3D BarFill;
        public StandardMaterial3D BarFillMat;
        public float Flash;
        public float Fall;
    }

    private readonly List<View> _views = new();
    private readonly List<TargetState> _targets = new();
    private readonly List<(Label3D Label, float Age)> _numbers = new();

    private StandardMaterial3D _matBody;
    private StandardMaterial3D _matHead;
    private StandardMaterial3D _matDead;
    private StandardMaterial3D _matBarBack;
    private StandardMaterial3D _matBarFill;

    public int Kills { get; private set; }
    public int Score { get; private set; }
    public int AliveCount { get; private set; }
    public string LastEvent { get; private set; } = "";

    public IReadOnlyList<TargetState> All => _targets;

    public override void _Ready()
    {
        _matBody = Mat(new Color("#E8632E"), 0.55f, 0.02f);
        _matHead = Mat(new Color("#F2A65A"), 0.50f, 0.02f);
        _matDead = Mat(new Color("#3A3F46"), 0.85f, 0f);
        _matBarBack = Mat(new Color(0.04f, 0.045f, 0.055f, 0.8f), 0.9f, 0f);
        _matBarFill = Mat(new Color("#8AE05A"), 0.6f, 0f);
        _matBarBack.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
        _matBarFill.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
        _matBarBack.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _matBarFill.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
    }

    private static StandardMaterial3D Mat(Color c, float rough, float metal) => new()
    {
        AlbedoColor = c,
        Roughness = rough,
        Metallic = metal,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel
    };

    // ------------------------------------------------------------------ creazione

    public TargetState Spawn(Vec3 feet, int id)
    {
        var t = new TargetState { Id = id, Feet = feet };
        _targets.Add(t);

        var root = new Node3D { Name = $"Bersaglio{id}", Position = new Vector3(feet.X, feet.Y, feet.Z) };
        AddChild(root);

        // il corpo ruota quando cade: le scatole della simulazione restano in piedi
        var body = new Node3D();
        root.AddChild(body);

        var torso = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(TargetState.HalfWidth * 2f, 1.10f, TargetState.HalfDepth * 2f) },
            Position = new Vector3(0f, 0.97f, 0f),
            MaterialOverride = _matBody
        };
        body.AddChild(torso);

        var hips = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(TargetState.HalfWidth * 1.85f, 0.84f, TargetState.HalfDepth * 1.85f) },
            Position = new Vector3(0f, 0.42f, 0f),
            MaterialOverride = _matBody
        };
        body.AddChild(hips);

        var head = new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(TargetState.HeadHalf * 2f, TargetState.HeadHalf * 2f, TargetState.HeadHalf * 2f)
            },
            Position = new Vector3(0f, TargetState.Height - TargetState.HeadHalf, 0f),
            MaterialOverride = _matHead
        };
        body.AddChild(head);

        // barra della vita: due quad che guardano sempre la camera
        var barBack = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(0.46f, 0.055f), Material = _matBarBack },
            Position = new Vector3(0f, TargetState.Height + 0.13f, 0f)
        };
        root.AddChild(barBack);

        var fillMat = (StandardMaterial3D)_matBarFill.Duplicate();
        var barFill = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(0.42f, 0.035f), Material = fillMat },
            Position = new Vector3(0f, TargetState.Height + 0.13f, 0f)
        };
        root.AddChild(barFill);

        _views.Add(new View
        {
            State = t,
            Root = root,
            Body = body,
            Torso = torso,
            Head = head,
            BarBack = barBack,
            BarFill = barFill,
            BarFillMat = fillMat
        });
        return t;
    }

    public void Clear()
    {
        foreach (var v in _views) v.Root.QueueFree();
        _views.Clear();
        _targets.Clear();
        foreach (var n in _numbers) n.Label.QueueFree();
        _numbers.Clear();
        Kills = 0;
        Score = 0;
        AliveCount = 0;
        LastEvent = "";
    }

    /// <summary>Una fila di bersagli davanti al giocatore, a distanze diverse.</summary>
    public void SpawnLine(Vec3 eye, float yaw, float floorY, int count = 5)
    {
        var fwd = new Vec3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        var right = new Vec3(fwd.Z, 0f, -fwd.X);
        for (int i = 0; i < count; i++)
        {
            float side = (i - (count - 1) * 0.5f) * 1.7f;
            float ahead = 11f + i * 3.0f;
            var p = eye + fwd * ahead + right * side;
            Spawn(new Vec3(p.X, floorY, p.Z), 200 + i);
        }
    }

    // ------------------------------------------------------------------ colpi

    /// <summary>Un colpo andato a segno: scheggia, numerino del danno, morte.</summary>
    public void RegisterHit(ShotHit h)
    {
        if (!h.Hit || h.Target < 0) return;
        var view = _views.Find(v => v.State.Id == h.Target);
        if (view == null) return;

        view.Flash = 0.14f;

        var label = new Label3D
        {
            Text = h.Zone == HitZone.Head ? $"{h.Damage}!" : h.Damage.ToString(),
            Position = new Vector3(h.Point.X, h.Point.Y, h.Point.Z),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = h.Zone == HitZone.Head ? 78 : 56,
            OutlineSize = 14,
            Modulate = h.Zone == HitZone.Head ? new Color("#FFD166") : new Color("#FFFFFF"),
            OutlineModulate = new Color(0f, 0f, 0f, 0.85f),
            NoDepthTest = true
        };
        AddChild(label);
        _numbers.Add((label, 0f));

        if (h.Killed)
        {
            Kills++;
            Score += h.Zone == HitZone.Head ? 150 : 100;
            LastEvent = h.Zone == HitZone.Head
                ? $"bersaglio {h.Target} · tiro alla testa · +150"
                : $"bersaglio {h.Target} · abbattuto · +100";
        }
    }

    public void Tick(float dt)
    {
        int alive = 0;
        foreach (var v in _views)
        {
            var s = v.State;
            if (s.Alive) alive++;

            if (!s.Alive)
            {
                v.Fall = Mathf.Min(1f, v.Fall + dt * 2.4f);
                float k = Ease(v.Fall);
                v.Body.RotationDegrees = new Vector3(-88f * k, 0f, 0f);
                v.Body.Position = new Vector3(0f, -0.12f * k, 0f);
                if (v.Torso.MaterialOverride != _matDead) v.Torso.MaterialOverride = _matDead;
                if (v.Head.MaterialOverride != _matDead) v.Head.MaterialOverride = _matDead;
            }
            else if (v.Fall > 0f)
            {
                v.Fall = 0f;
                v.Body.RotationDegrees = Vector3.Zero;
                v.Body.Position = Vector3.Zero;
                v.Torso.MaterialOverride = _matBody;
                v.Head.MaterialOverride = _matHead;
            }

            float frac = Mathf.Clamp(s.Health / TargetState.MaxHealth, 0f, 1f);
            v.BarFill.Visible = s.Alive;
            v.BarBack.Visible = s.Alive;
            if (s.Alive)
            {
                var mesh = (QuadMesh)v.BarFill.Mesh;
                mesh.Size = new Vector2(0.42f * Mathf.Max(0.02f, frac), 0.035f);
                v.BarFill.Position = new Vector3(-0.21f * (1f - frac), TargetState.Height + 0.13f, 0.002f);
                v.BarFillMat.AlbedoColor = frac > 0.5f
                    ? new Color("#8AE05A").Lerp(new Color("#FFD166"), (1f - frac) * 2f)
                    : new Color("#FFD166").Lerp(new Color("#E0452E"), (0.5f - frac) * 2f);
            }

            if (v.Flash > 0f)
            {
                v.Flash -= dt;
                float k = Mathf.Max(0f, v.Flash / 0.14f);
                v.Head.Scale = Vector3.One * (1f + 0.18f * k);
            }

            s.TryRespawn(dt);
        }
        AliveCount = alive;

        // numeri del danno: salgono e svaniscono in 0,9 s
        for (int i = _numbers.Count - 1; i >= 0; i--)
        {
            var (label, age) = _numbers[i];
            age += dt;
            if (age > 0.9f)
            {
                label.QueueFree();
                _numbers.RemoveAt(i);
                continue;
            }
            label.Position += new Vector3(0f, dt * 0.85f, 0f);
            var m = label.Modulate;
            m.A = 1f - age / 0.9f;
            label.Modulate = m;
            label.Scale = Vector3.One * (1f + age * 0.35f);
            _numbers[i] = (label, age);
        }
    }

    private static float Ease(float t) => t * t * (3f - 2f * t);
}
