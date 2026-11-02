using Falezia.Sim;
using Godot;
using NVec3 = System.Numerics.Vector3;

/// <summary>
/// I bossoli. Quando spari, l'otturatore ne estrae uno dal finestrino: vola a destra,
/// cade, rimbalza e resta a terra per qualche secondo, poi sparisce.
///
/// Niente fisica del motore: il mondo è fatto di scatole e la collisione vera è nella
/// simulazione (<see cref="Trace"/>). Quindi il bossolo è un piccolo corpo rigido
/// simulato a mano, con un raggio verso il basso per sapere dov'è il pavimento.
/// </summary>
public partial class ShellCasings : Node3D
{
    private const int Pool = 14;
    private const float Life = 4.5f;      // quanto resta a terra prima di sparire
    private const float Fade = 0.8f;      // quanto ci mette a sparire
    private const float Gravity = 12f;    // un filo più della realtà: cade meglio a schermo

    private sealed class Shell
    {
        public MeshInstance3D Node;
        public StandardMaterial3D Mat;
        public Vector3 Vel;
        public Vector3 Spin;
        public float GroundY;
        public float Age;
        public float TraceCd;
        public int Bounces;
        public bool Resting;
        public bool Alive;
    }

    private readonly Shell[] _shells = new Shell[Pool];
    private CylinderMesh _mesh;
    private WorldState _world;
    private int _next;

    private static readonly Color Brass = new(0.78f, 0.62f, 0.28f);

    public override void _Ready()
    {
        // bossolo 9×19: 19 mm di lunghezza, 9,6 mm di diametro
        _mesh = new CylinderMesh
        {
            TopRadius = 0.0044f,
            BottomRadius = 0.0048f,
            Height = 0.019f,
            RadialSegments = 8,
            Rings = 1
        };

        for (int i = 0; i < Pool; i++)
        {
            var mat = new StandardMaterial3D
            {
                AlbedoColor = Brass,
                Metallic = 0.85f,
                Roughness = 0.38f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
            var node = new MeshInstance3D
            {
                Mesh = _mesh,
                MaterialOverride = mat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false
            };
            AddChild(node);
            _shells[i] = new Shell { Node = node, Mat = mat };
        }
    }

    /// <summary>Espelle un bossolo dal finestrino. La velocità è in spazio mondo.</summary>
    public void Eject(WorldState world, Vector3 at, Vector3 vel)
    {
        if (_shells[0] == null) return;
        _world = world;

        var s = Take();
        s.Alive = true;
        s.Resting = false;
        s.Bounces = 0;
        s.Age = 0f;
        s.Vel = vel;
        s.Spin = new Vector3(
            (float)GD.RandRange(-14.0, 14.0),
            (float)GD.RandRange(-14.0, 14.0),
            (float)GD.RandRange(-14.0, 14.0));
        s.GroundY = at.Y - 6f;              // prudente, finché il raggio non trova il pavimento
        s.TraceCd = 0f;

        s.Node.Position = at;
        s.Node.Rotation = new Vector3(
            (float)GD.RandRange(0.0, 6.28),
            (float)GD.RandRange(0.0, 6.28),
            (float)GD.RandRange(0.0, 6.28));
        s.Node.Visible = true;
        s.Mat.AlbedoColor = Brass;

        FindGround(s);                       // subito: il pavimento sotto il finestrino
    }

    private void FindGround(Shell s)
    {
        if (_world == null) return;
        var p = s.Node.Position;
        var hit = Trace.Ray(_world, new NVec3(p.X, p.Y, p.Z), new NVec3(0f, -1f, 0f), 8f);
        if (hit.Hit) s.GroundY = p.Y - hit.Distance;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_shells[0] == null) return;

        foreach (var s in _shells)
        {
            if (!s.Alive) continue;

            s.Age += dt;
            if (s.Age >= Life)
            {
                s.Alive = false;
                s.Node.Visible = false;
                continue;
            }

            if (!s.Resting)
            {
                s.Vel.Y -= Gravity * dt;
                s.Node.Position += s.Vel * dt;
                s.Node.Rotation += s.Spin * dt;

                // il pavimento si ricontrolla mentre cade: se scavalca un gradino, lo trova
                s.TraceCd -= dt;
                if (s.TraceCd <= 0f)
                {
                    s.TraceCd = 0.15f;
                    FindGround(s);
                }

                if (s.Node.Position.Y <= s.GroundY + 0.006f && s.Vel.Y < 0f)
                {
                    s.Node.Position = new Vector3(s.Node.Position.X, s.GroundY + 0.006f, s.Node.Position.Z);
                    s.Vel = new Vector3(s.Vel.X * 0.62f, -s.Vel.Y * 0.34f, s.Vel.Z * 0.62f);
                    s.Spin *= 0.45f;
                    s.Bounces++;

                    if (s.Bounces >= 3 || s.Vel.Length() < 0.35f)
                    {
                        s.Resting = true;
                        // sdraiato: un bossolo non resta in piedi
                        s.Node.Rotation = new Vector3(Mathf.Pi * 0.5f, s.Node.Rotation.Y, 0f);
                    }
                }
            }

            float a = Mathf.Clamp((Life - s.Age) / Fade, 0f, 1f);
            s.Mat.AlbedoColor = new Color(Brass.R, Brass.G, Brass.B, a);
        }
    }

    /// <summary>Pulisce i bossoli (cambio mappa).</summary>
    public void Clear()
    {
        foreach (var s in _shells)
        {
            s.Alive = false;
            s.Node.Visible = false;
        }
    }

    private Shell Take()
    {
        for (int i = 0; i < Pool; i++)
        {
            int k = (_next + i) % Pool;
            if (!_shells[k].Alive)
            {
                _next = (k + 1) % Pool;
                return _shells[k];
            }
        }
        var oldest = _shells[_next];
        _next = (_next + 1) % Pool;
        return oldest;
    }
}
