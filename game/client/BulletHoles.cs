using Godot;

/// <summary>
/// I FORI DEI PROIETTILI.
///
/// Ogni palla che prende una superficie lascia un buco: creta scura, orlo scheggiato,
/// polvere attorno e crepe raggiate. La texture è **generata in codice** all'avvio
/// (sei varianti da 192²), quindi non c'è niente da importare e nessun foro è uguale
/// a un altro.
///
/// Il foro è un **quad che guarda sempre la camera**: così non può mai essere visto
/// di taglio (era il difetto delle strisce stirate sui muri) e resta tondo da
/// qualunque angolo. La posizione è quella dell'impatto, appoggiata sulla faccia
/// (2 mm fuori), con la normale usata solo per la polvere e per il verso.
/// Resta 12 s e svanisce in 1,8 s; pool fisso di 96.
/// </summary>
public partial class BulletHoles : Node3D
{
    private const int Pool = 96;
    private const int Variants = 6;
    private const int TexPx = 160;
    private const float Life = 12f;
    private const float Fade = 1.8f;

    private sealed class Hole
    {
        public MeshInstance3D Node;
        public StandardMaterial3D Mat;
        public float Age;
        public float Roll;
        public Vector3 Size;
        public bool Alive;
    }

    private readonly Hole[] _holes = new Hole[Pool];
    private QuadMesh _mesh;
    private ImageTexture[] _textures;
    private int _next;

    private static readonly Color RefDust = new(0.74f, 0.74f, 0.74f);
    private static readonly Color DustStone = new(0.74f, 0.71f, 0.66f);
    private static readonly Color DustConcrete = new(0.70f, 0.69f, 0.67f);
    private static readonly Color DustMetal = new(0.42f, 0.43f, 0.45f);
    private static readonly Color DustWood = new(0.56f, 0.45f, 0.33f);

    public override void _Ready()
    {
        _mesh = new QuadMesh { Size = new Vector2(1f, 1f) };

        try { _textures = MakeVariants(); }
        catch (System.Exception e)
        {
            GD.PushWarning($"BulletHoles: texture procedurali non generate ({e.Message}), uso il ripiego");
            _textures = new[] { FallbackTexture() };
        }

        for (int i = 0; i < Pool; i++)
        {
            var mat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
                AlbedoTexture = _textures[i % _textures.Length],
                AlbedoColor = new Color(1, 1, 1, 1),
                DisableReceiveShadows = true
            };

            var node = new MeshInstance3D
            {
                Mesh = _mesh,
                MaterialOverride = mat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false
            };
            AddChild(node);
            _holes[i] = new Hole { Node = node, Mat = mat };
        }

        GD.Print($"BulletHoles: {Pool} fori pronti · {_textures.Length} texture");
    }

    // ------------------------------------------------------------------ texture

    private static ImageTexture[] MakeVariants()
    {
        var list = new ImageTexture[Variants];
        for (int v = 0; v < Variants; v++) list[v] = MakeTexture(v * 19.7f + 3.1f);
        return list;
    }

    private static ImageTexture MakeTexture(float seed)
    {
        var img = Image.CreateEmpty(TexPx, TexPx, false, Image.Format.Rgba8);

        for (int y = 0; y < TexPx; y++)
        {
            for (int x = 0; x < TexPx; x++)
            {
                float u = (x + 0.5f) / TexPx * 2f - 1f;
                float w = (y + 0.5f) / TexPx * 2f - 1f;
                float r = Mathf.Sqrt(u * u + w * w) * 1.2f;
                float ang = Mathf.Atan2(w, u);

                float wob = Fbm(Mathf.Cos(ang) * 2.4f + seed, Mathf.Sin(ang) * 2.4f + seed);
                float edge = 0.32f + 0.13f * wob;

                float crater = 1f - Smooth(edge * 0.30f, edge, r);
                float rim = Smooth(edge * 0.86f, edge * 1.02f, r)
                          * (1f - Smooth(edge * 1.02f, edge * 1.42f, r));
                float dust = Smooth(edge * 0.95f, edge * 1.25f, r)
                           * (1f - Smooth(edge * 1.55f, edge * 2.05f, r));
                dust *= 0.45f + 0.55f * Fbm(u * 5f + seed, w * 5f + seed);

                float spikes = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(ang * 9f + seed * 23f + wob * 3f)), 26f);
                float crack = spikes * Smooth(edge * 0.85f, edge, r)
                            * (1f - Smooth(edge * 1.05f, edge * 1.9f, r));

                Color col = RefDust;
                col = col.Lerp(new Color(0.02f, 0.018f, 0.016f), crater);
                col = col.Lerp(new Color(0.90f, 0.89f, 0.86f), rim * 0.55f);
                col = col.Lerp(new Color(0.05f, 0.04f, 0.035f), crack * 0.7f);

                float alpha = Mathf.Max(Mathf.Max(crater, rim * 0.75f),
                                        Mathf.Max(dust * 0.42f, crack * 0.55f));
                alpha *= 1f - Smooth(0.94f, 1.06f, r);

                img.SetPixel(x, y, new Color(col.R, col.G, col.B, Mathf.Clamp(alpha, 0f, 1f)));
            }
        }

        img.GenerateMipmaps();
        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>Ripiego: un disco scuro sfumato, se la generazione non riesce.</summary>
    private static ImageTexture FallbackTexture()
    {
        var img = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float u = (x + 0.5f) / 32f * 2f - 1f;
                float v = (y + 0.5f) / 32f * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = 1f - Smooth(0.35f, 0.85f, r);
                var c = new Color(0.03f, 0.028f, 0.026f, a);
                img.SetPixel(x, y, c);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static float Hash(float x, float y)
    {
        float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    private static float ValueNoise(float x, float y)
    {
        float xi = Mathf.Floor(x), yi = Mathf.Floor(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3f - 2f * xf);
        float v = yf * yf * (3f - 2f * yf);
        float a = Hash(xi, yi), b = Hash(xi + 1f, yi);
        float c = Hash(xi, yi + 1f), d = Hash(xi + 1f, yi + 1f);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            sum += ValueNoise(x, y) * amp;
            x *= 2.03f;
            y *= 2.03f;
            amp *= 0.5f;
        }
        return sum;
    }

    private static float Smooth(float a, float b, float x)
    {
        if (Mathf.IsEqualApprox(a, b)) return x < a ? 0f : 1f;
        float t = Mathf.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    // ------------------------------------------------------------------ spawn

    /// <summary>
    /// Un foro sulla superficie colpita. <paramref name="point"/> è il punto d'impatto,
    /// <paramref name="normal"/> la normale della faccia, <paramref name="surface"/>
    /// il materiale del volume (per il colore della polvere).
    /// </summary>
    public void Spawn(Vector3 point, Vector3 normal, string surface = "")
    {
        if (_holes[0] == null) return;

        var h = Take();
        h.Alive = true;
        h.Age = 0f;
        h.Roll = (float)GD.RandRange(0.0, Mathf.Tau);

        var n = normal.LengthSquared() > 1e-6f ? normal.Normalized() : Vector3.Up;

        float size = (float)GD.RandRange(0.062, 0.094);
        float squash = (float)GD.RandRange(0.94, 1.06);

        h.Node.Position = point + n * 0.006f;
        h.Size = new Vector3(size * squash, size / squash, 1f);
        h.Node.MaterialOverride = h.Mat;
        h.Mat.AlbedoTexture = _textures[(int)(GD.Randi() % (uint)_textures.Length)];

        var dust = DustFor(surface);
        h.Mat.AlbedoColor = new Color(
            Mathf.Clamp(dust.R / RefDust.R, 0f, 1f),
            Mathf.Clamp(dust.G / RefDust.G, 0f, 1f),
            Mathf.Clamp(dust.B / RefDust.B, 0f, 1f), 1f);

        h.Node.Visible = true;
        FaceCamera(h);
    }

    /// <summary>
    /// Il quad guarda la camera: il foro resta tondo anche guardandolo di sbieco.
    /// (Con il quad appoggiato sulla normale, un muro visto di taglio lo riduceva
    /// a una striscia.)
    /// </summary>
    private void FaceCamera(Hole h)
    {
        var cam = GetViewport()?.GetCamera3D();
        if (cam == null) return;

        var dir = cam.GlobalPosition - h.Node.GlobalPosition;
        if (dir.LengthSquared() < 1e-8f) return;
        dir = dir.Normalized();

        // +Z del quad verso la camera; il roll è attorno a quell'asse.
        // Se guardi il pavimento da sopra, "up" verticale è parallelo alla direzione
        // e la base degenera: in quel caso si usa Forward come appoggio.
        var up = Mathf.Abs(dir.Y) > 0.98f ? Vector3.Forward : Vector3.Up;
        var basis = Basis.LookingAt(-dir, up);
        basis *= new Basis(Vector3.Back, h.Roll);
        // la scala va DENTRO la base: scrivere GlobalTransform la azzererebbe
        h.Node.GlobalTransform = new Transform3D(basis.Scaled(h.Size), h.Node.GlobalPosition);
    }

    private static Color DustFor(string surface) => surface switch
    {
        "metal" or "steel" or "sheet" or "container" => DustMetal,
        "wood" or "plank" => DustWood,
        "concrete" or "stone" => DustConcrete,
        "" => DustConcrete,
        _ => DustStone
    };

    private Hole Take()
    {
        for (int i = 0; i < Pool; i++)
        {
            int k = (_next + i) % Pool;
            if (!_holes[k].Alive) { _next = (k + 1) % Pool; return _holes[k]; }
        }
        var old = _holes[_next];
        _next = (_next + 1) % Pool;
        return old;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < Pool; i++)
        {
            var h = _holes[i];
            if (h == null || !h.Alive) continue;

            h.Age += dt;
            FaceCamera(h);

            if (h.Age < Life) continue;

            float k = 1f - Mathf.Clamp((h.Age - Life) / Fade, 0f, 1f);
            var c = h.Mat.AlbedoColor;
            h.Mat.AlbedoColor = new Color(c.R, c.G, c.B, k);

            if (k <= 0f)
            {
                h.Alive = false;
                h.Node.Visible = false;
            }
        }
    }

    /// <summary>Mappa nuova o ritorno in scena: i buchi vecchi non servono più.</summary>
    public void Clear()
    {
        for (int i = 0; i < Pool; i++)
        {
            var h = _holes[i];
            if (h == null) continue;
            h.Alive = false;
            h.Node.Visible = false;
        }
    }
}
