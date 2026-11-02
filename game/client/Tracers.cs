using Godot;

/// <summary>
/// Gli effetti dello sparo: la scia del proiettile e il fumo che esce dalla canna.
///
/// Non è fisica e non è autorità — il punto d'impatto lo decide <see cref="Falezia.Sim.Trace"/>
/// sul raggio della simulazione. Qui si disegna soltanto, con due pool fissi di nodi
/// (niente allocazioni a ogni colpo):
///
/// * **scia** — un cilindro sottile con <c>tracer.gdshader</c>: rumore che scorre, centro
///   luminoso, punta che evapora. 6 cm di vita, larga 7 mm, poco opaca;
/// * **fumo** — quad billboard con <c>muzzle_smoke.gdshader</c>: sbuffi che si allontanano,
///   salgono, si aprono e svaniscono. La Hush, che è silenziata, ne fa quasi niente.
/// </summary>
public partial class Tracers : Node3D
{
    private const int BeamCount = 24;   // una pompa spara 12 palle: servono più scie
    private const int PuffCount = 64;

    private const float BeamLife = 0.06f;

    private sealed class Beam
    {
        public MeshInstance3D Node;
        public ShaderMaterial Shader;
        public StandardMaterial3D Fallback;
        public float Left;
        public bool Alive;
    }

    private sealed class Puff
    {
        public MeshInstance3D Node;
        public ShaderMaterial Shader;
        public StandardMaterial3D Fallback;
        public float Left;
        public float Life;
        public float Size0;
        public float Size1;
        public float Age;
        public float Delay;
        public bool Alive;
        public Vector3 Vel;
        public float Roll;
        public float RollSpeed;
    }

    private readonly Beam[] _beams = new Beam[BeamCount];
    private readonly Puff[] _puffs = new Puff[PuffCount];

    private CylinderMesh _beamMesh;
    private QuadMesh _quadMesh;
    private Shader _tracerShader;
    private Shader _smokeShader;
    private readonly System.Collections.Generic.List<Texture2D> _wispTex = new();
    private readonly System.Collections.Generic.List<Texture2D> _puffTex = new();

    // più varianti: stessa composizione, forma diversa a ogni colpo
    private static readonly string[] PuffFiles =
    {
        "res://assets/textures/fx/smoke_puff_1.png",
        "res://assets/textures/fx/smoke_puff_2.png",
        "res://assets/textures/fx/smoke_puff_3.png",
        "res://assets/textures/fx/smoke_puff_4.png"
    };
    private static readonly string[] WispFiles =
    {
        "res://assets/textures/fx/smoke_wisp_1.png",
        "res://assets/textures/fx/smoke_wisp_2.png",
        "res://assets/textures/fx/smoke_wisp_3.png"
    };
    private Material _fallbackBeam;
    private Material _fallbackPuff;
    private int _nextBeam;
    private int _nextPuff;

    private static readonly Color BeamCore = new(1.00f, 0.90f, 0.66f);
    private static readonly Color BeamSmoke = new(0.90f, 0.92f, 0.95f);
    private static readonly Color BeamCoreHush = new(0.92f, 0.86f, 0.70f);

    public override void _Ready()
    {
        _tracerShader = GD.Load<Shader>("res://assets/shaders/tracer.gdshader");
        _smokeShader = GD.Load<Shader>("res://assets/shaders/muzzle_smoke.gdshader");
        foreach (string f in PuffFiles)
            if (ResourceLoader.Exists(f)) _puffTex.Add(GD.Load<Texture2D>(f));
        foreach (string f in WispFiles)
            if (ResourceLoader.Exists(f)) _wispTex.Add(GD.Load<Texture2D>(f));

        if (_tracerShader == null || _smokeShader == null)
            GD.PushWarning("[fx] shader di scia/fumo non trovati: si usa il ripiego opaco");
        if (_puffTex.Count == 0 || _wispTex.Count == 0)
            GD.PushWarning("[fx] texture del fumo non importate: apri l'editor una volta");

        _beamMesh = new CylinderMesh
        {
            TopRadius = 1f,
            BottomRadius = 1f,
            Height = 1f,
            RadialSegments = 6,
            Rings = 1
        };
        _quadMesh = new QuadMesh { Size = Vector2.One };

        _fallbackBeam = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(1f, 0.92f, 0.7f, 0.5f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DisableReceiveShadows = true
        };
        _fallbackPuff = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(0.82f, 0.84f, 0.86f, 0.35f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DisableReceiveShadows = true
        };

        for (int i = 0; i < BeamCount; i++)
        {
            var shader = _tracerShader != null ? new ShaderMaterial { Shader = _tracerShader } : null;
            if (shader != null)
            {
                if (_wispTex.Count > 0) shader.SetShaderParameter("noise_tex", _wispTex[0]);
                shader.SetShaderParameter("core", BeamCore);
                shader.SetShaderParameter("smoke", BeamSmoke);
                shader.SetShaderParameter("opacity", 0.42f);
                shader.SetShaderParameter("age", 0f);
                shader.SetShaderParameter("seed", 0f);
            }
            var mi = new MeshInstance3D
            {
                Mesh = _beamMesh,
                MaterialOverride = shader ?? _fallbackBeam,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false
            };
            AddChild(mi);
            _beams[i] = new Beam { Node = mi, Shader = shader, Fallback = (StandardMaterial3D)_fallbackBeam };
        }

        for (int i = 0; i < PuffCount; i++)
        {
            var shader = _smokeShader != null ? new ShaderMaterial { Shader = _smokeShader } : null;
            if (shader != null)
            {
                if (_puffTex.Count > 0) shader.SetShaderParameter("smoke_tex", _puffTex[0]);
                shader.SetShaderParameter("tint", new Color(0.84f, 0.85f, 0.87f));
                shader.SetShaderParameter("age", 0f);
                shader.SetShaderParameter("seed", 0f);
                shader.SetShaderParameter("density", 0.55f);
                shader.SetShaderParameter("roll", 0f);
            }
            var mi = new MeshInstance3D
            {
                Mesh = _quadMesh,
                MaterialOverride = shader ?? _fallbackPuff,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                ExtraCullMargin = 3f,
                Visible = false
            };
            AddChild(mi);
            _puffs[i] = new Puff { Node = mi, Shader = shader, Fallback = (StandardMaterial3D)_fallbackPuff };
        }
    }

    // =================================================================== SPARO

    /// <summary>
    /// Un colpo: scia dal vivo di canna all'impatto, fumo alla bocca e polvere sul muro.
    /// </summary>
    public void Spawn(Vector3 from, Vector3 to, bool silenced = false, bool impact = true, bool smoke = true)
    {
        if (_beams[0] == null) return;      // _Ready non è ancora passato

        Vector3 d = to - from;
        float len = d.Length();
        if (len < 0.10f) return;

        Vector3 dir = d / len;
        SpawnBeam(from, d, len, silenced);
        if (smoke) SpawnMuzzleSmoke(from, dir, silenced);   // una pompa: fumo una volta sola

        if (impact) SpawnImpactSmoke(to, -dir, silenced);
    }

    private void SpawnBeam(Vector3 from, Vector3 d, float len, bool silenced)
    {
        Vector3 y = d / len;
        Vector3 helper = Mathf.Abs(y.Y) > 0.95f ? Vector3.Right : Vector3.Up;
        Vector3 x = helper.Cross(y).Normalized();
        Vector3 z = x.Cross(y);

        // Il cilindro ha l'asse su Y: le colonne della base sono gli assi già scalati.
        // NON usare Basis.Scaled(): quello scala in spazio globale (le righe) e con una
        // base ruotata diventa un taglio obliquo, cioè una riga verticale a schermo.
        const float width = 0.0075f;        // 7,5 mm: una scia, non un tubo
        var basis = new Basis(x * width, y * len, z * width);

        var b = TakeBeam();
        b.Node.Transform = new Transform3D(basis, from + d * 0.5f);
        b.Node.Visible = true;
        b.Left = BeamLife;
        b.Alive = true;

        if (b.Shader != null)
        {
            if (_wispTex.Count > 0)
                b.Shader.SetShaderParameter("noise_tex", _wispTex[(int)GD.RandRange(0, _wispTex.Count - 1)]);
            b.Shader.SetShaderParameter("seed", (float)GD.RandRange(0.0, 10.0));
            b.Shader.SetShaderParameter("uv_offset",
                new Vector2((float)GD.RandRange(0.0, 1.0), (float)GD.RandRange(0.0, 1.0)));
            b.Shader.SetShaderParameter("age", 0f);
            b.Shader.SetShaderParameter("opacity", silenced ? 0.16f : 0.42f);
            b.Shader.SetShaderParameter("core", silenced ? BeamCoreHush : BeamCore);
        }
    }

    private void SpawnMuzzleSmoke(Vector3 from, Vector3 dir, bool silenced)
    {
        Vector3 side = new Vector3(dir.Z, 0f, -dir.X).Normalized();

        if (silenced)
        {
            // la Hush: due sbuffi piccoli e subito spenti. Va bene così, non si tocca.
            for (int i = 0; i < 2; i++)
            {
                Vector3 start = from + dir * (0.02f + 0.04f * i) + side * (float)GD.RandRange(-0.02, 0.02);
                Vector3 vel = dir * (float)GD.RandRange(0.5, 1.1)
                            + Vector3.Up * (float)GD.RandRange(0.12, 0.35)
                            + side * (float)GD.RandRange(-0.25, 0.25);
                SpawnPuff(start, vel,
                    size0: 0.06f, size1: 0.20f, life: 0.32f,
                    tint: new Color(0.80f, 0.83f, 0.88f), density: 0.18f,
                    delay: 0.012f * i, stretch: 1.0f, erosion: 1.0f);
            }
            return;
        }

        // La Standard: poco fumo, largo e trasparente, emesso in tre tempi invece che
        // in una palla sola. Ogni sbuffo è allungato e "mangiato" dal rumore, così si
        // legge come vapore che si apre, non come una sfera che si gonfia.
        float[] delays = { 0f, 0.055f, 0.115f };
        float[] density = { 0.22f, 0.17f, 0.12f };
        float[] size1 = { 0.22f, 0.19f, 0.16f };
        int count = (int)GD.RandRange(2, 3);              // due o tre: mai lo stesso schema

        for (int i = 0; i < count; i++)
        {
            Vector3 start = from
                          + dir * (0.06f + 0.04f * i)
                          + side * (float)GD.RandRange(-0.04, 0.04)
                          + Vector3.Up * (float)GD.RandRange(-0.02, 0.03);
            Vector3 vel = dir * (float)GD.RandRange(0.85, 1.60)
                        + Vector3.Up * (float)GD.RandRange(0.20, 0.50)
                        + side * (float)GD.RandRange(-0.45, 0.45);

            SpawnPuff(start, vel,
                size0: 0.05f, size1: size1[i],
                life: (float)GD.RandRange(0.50, 0.80),
                tint: new Color(0.85f, 0.86f, 0.88f),
                density: density[i] * (float)GD.RandRange(0.85, 1.15),
                delay: delays[i],
                stretch: (float)GD.RandRange(1.25, 1.55),
                erosion: (float)GD.RandRange(1.45, 1.75));
        }
    }

    private void SpawnImpactSmoke(Vector3 at, Vector3 normal, bool silenced)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 vel = normal * (float)GD.RandRange(0.5, 1.3)
                        + new Vector3((float)GD.RandRange(-0.5, 0.5), (float)GD.RandRange(0.1, 0.6), (float)GD.RandRange(-0.5, 0.5));
            SpawnPuff(at + normal * 0.03f, vel,
                size0: silenced ? 0.05f : 0.07f,
                size1: silenced ? 0.16f : 0.26f,
                life: (float)GD.RandRange(0.22, 0.38),
                tint: new Color(0.72f, 0.70f, 0.66f),
                density: silenced ? 0.22f : 0.38f,
                delay: 0.02f * i, stretch: 1.0f, erosion: 1.2f);
        }
    }

    private void SpawnPuff(Vector3 at, Vector3 vel, float size0, float size1, float life,
                           Color tint, float density, float delay = 0f,
                           float stretch = 1.0f, float erosion = 1.0f)
    {
        var p = TakePuff();
        p.Alive = true;
        p.Left = life;
        p.Life = life;
        p.Size0 = size0;
        p.Size1 = size1;
        p.Age = 0f;
        p.Delay = Mathf.Max(0f, delay);
        p.Vel = vel;
        p.Roll = (float)GD.RandRange(0.0, 360.0);
        p.RollSpeed = (float)GD.RandRange(-40.0, 40.0);

        p.Node.Position = at;
        p.Node.Scale = Vector3.One * size0;
        p.Node.Visible = p.Delay <= 0f;

        if (p.Shader != null)
        {
            // ogni sbuffo pesca una texture, un ritaglio e una velocità di scorrimento
            // diversi: la stessa composizione non si vede mai due volte identica
            if (_puffTex.Count > 0)
                p.Shader.SetShaderParameter("smoke_tex", _puffTex[(int)GD.RandRange(0, _puffTex.Count - 1)]);
            // ritaglio PICCOLO: la texture non è tileabile, spostarla troppo
            // taglierebbe lo sbuffo contro il bordo del quad
            p.Shader.SetShaderParameter("uv_offset",
                new Vector2((float)GD.RandRange(-0.07, 0.07), (float)GD.RandRange(-0.07, 0.07)));
            p.Shader.SetShaderParameter("uv_scale", (float)GD.RandRange(0.88, 1.10));
            p.Shader.SetShaderParameter("scroll_speed", (float)GD.RandRange(0.28, 0.62));
            p.Shader.SetShaderParameter("rise", (float)GD.RandRange(0.8, 1.3));
            p.Shader.SetShaderParameter("tint", tint);
            p.Shader.SetShaderParameter("density", density);
            p.Shader.SetShaderParameter("seed", (float)GD.RandRange(0.0, 10.0));
            p.Shader.SetShaderParameter("roll", p.Roll);
            p.Shader.SetShaderParameter("stretch", stretch);
            p.Shader.SetShaderParameter("erosion", erosion);
            p.Shader.SetShaderParameter("age", 0f);
        }
    }

    // =================================================================== CICLO

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_beams[0] == null) return;

        foreach (var b in _beams)
        {
            if (!b.Alive) continue;
            b.Left -= dt;
            if (b.Left <= 0f)
            {
                b.Alive = false;
                b.Node.Visible = false;
                continue;
            }
            if (b.Shader != null)
                b.Shader.SetShaderParameter("age", 1f - Mathf.Clamp(b.Left / BeamLife, 0f, 1f));
            else
            {
                float t = Mathf.Clamp(b.Left / BeamLife, 0f, 1f);
                b.Fallback.AlbedoColor = new Color(1f, 0.92f, 0.7f, 0.5f * t);
            }
        }

        foreach (var p in _puffs)
        {
            if (!p.Alive) continue;

            if (p.Delay > 0f)
            {
                p.Delay -= dt;
                if (p.Delay > 0f) continue;
                p.Node.Visible = true;
            }

            p.Left -= dt;
            if (p.Left <= 0f)
            {
                p.Alive = false;
                p.Node.Visible = false;
                continue;
            }

            p.Age = 1f - Mathf.Clamp(p.Left / p.Life, 0f, 1f);

            // il fumo rallenta in avanti, ma continua a salire: è la salita che lo
            // porta fuori dalla linea di tiro mentre si dissolve
            p.Vel *= Mathf.Max(0f, 1f - 2.2f * dt);
            p.Vel += Vector3.Up * (0.70f * dt);
            p.Node.Position += p.Vel * dt;

            // si ALLARGA più di quanto cresca in altezza: l'ultima parte è una
            // velatura larga e bassa, non una palla più grande
            float grow = Mathf.Sqrt(p.Age);
            float sx = Mathf.Lerp(p.Size0 * 0.85f, p.Size1 * 1.45f, grow);
            float sy = Mathf.Lerp(p.Size0, p.Size1 * 0.95f, grow);
            p.Node.Scale = new Vector3(sx, sy, 1f);
            p.Roll += p.RollSpeed * dt;

            if (p.Shader != null)
            {
                p.Shader.SetShaderParameter("age", p.Age);
                p.Shader.SetShaderParameter("roll", p.Roll);
            }
            else
            {
                float t = 1f - p.Age;
                p.Fallback.AlbedoColor = new Color(0.82f, 0.84f, 0.86f, 0.35f * t);
            }
        }
    }

    private Beam TakeBeam()
    {
        for (int i = 0; i < BeamCount; i++)
        {
            int k = (_nextBeam + i) % BeamCount;
            if (!_beams[k].Alive)
            {
                _nextBeam = (k + 1) % BeamCount;
                return _beams[k];
            }
        }
        var oldest = _beams[_nextBeam];
        _nextBeam = (_nextBeam + 1) % BeamCount;
        return oldest;
    }

    private Puff TakePuff()
    {
        for (int i = 0; i < PuffCount; i++)
        {
            int k = (_nextPuff + i) % PuffCount;
            if (!_puffs[k].Alive)
            {
                _nextPuff = (k + 1) % PuffCount;
                return _puffs[k];
            }
        }
        var oldest = _puffs[_nextPuff];
        _nextPuff = (_nextPuff + 1) % PuffCount;
        return oldest;
    }
}
