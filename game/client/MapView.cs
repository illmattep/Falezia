using Falezia.Sim;
using Godot;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// Luce e cielo di Calanera: sole caldo di metà pomeriggio, cielo mediterraneo,
/// foschia di mare (che è anche il culling, doc 07). I valori di qualità li mette
/// <see cref="GraphicsSettings.ApplyScene"/>.
/// </summary>
public sealed class Atmosphere
{
    public DirectionalLight3D Sun;
    public Godot.Environment Env;
    public WorldEnvironment WorldEnv;

    /// <summary>
    /// Rotazione del cielo attorno all'asse verticale, in gradi. Serve quando la
    /// panorama ha un sole suo: si ruota finché non combacia col <see cref="Sun"/>.
    /// </summary>
    public const float SkyYawDeg = 0f;

    /// <summary>Ordine di ricerca della panorama. Il primo file che esiste vince.</summary>
    private static readonly string[] SkyFiles =
    {
        "res://assets/sky/sky_panorama.exr",
        "res://assets/sky/sky_panorama.hdr",
        "res://assets/sky/sky_panorama.png",
        "res://assets/sky/sky_panorama.jpg",
        "res://assets/sky/sky_panorama.webp"
    };

    /// <summary>La panorama in assets/sky, se c'è. Null = si usa il cielo procedurale.</summary>
    private static Material LoadPanorama()
    {
        foreach (string path in SkyFiles)
        {
            if (!ResourceLoader.Exists(path))
            {
                // il file c'è ma Godot non l'ha importato: succede se lo si copia
                // a gioco chiuso. Meglio dirlo che sembrare ciechi.
                if (Godot.FileAccess.FileExists(path))
                    GD.PushWarning($"[cielo] {path} esiste ma non è importato: apri l'editor una volta");
                continue;
            }
            var tex = GD.Load<Texture2D>(path);
            if (tex == null) continue;

            GD.Print($"[cielo] panorama: {path} ({tex.GetWidth()}x{tex.GetHeight()})");
            return new PanoramaSkyMaterial
            {
                Panorama = tex,
                Filter = true,          // il filtro evita le cuciture ai poli
                EnergyMultiplier = 1.0f
            };
        }
        return null;
    }

    /// <summary>Cielo di Calanera fatto in casa: cielo mediterraneo, foschia chiara all'orizzonte.</summary>
    private static Material ProceduralSky()
    {
        return new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.26f, 0.46f, 0.72f),
            SkyHorizonColor = new Color(0.76f, 0.82f, 0.88f),
            SkyCurve = 0.12f,
            GroundHorizonColor = new Color(0.70f, 0.75f, 0.80f),
            GroundBottomColor = new Color(0.22f, 0.30f, 0.36f),   // il mare visto dall'alto
            SunAngleMax = 30f
        };
    }

    public static Atmosphere Create(Node3D parent)
    {
        var a = new Atmosphere();

        a.Sun = new DirectionalLight3D
        {
            Name = "Sole",
            // Sole da OVEST, perpendicolare all'asse Nord–Sud: nessuna squadra gioca
            // controluce. In un tattico simmetrico anche la luce deve esserlo.
            RotationDegrees = new Vector3(-42f, -90f, 0f),
            LightColor = new Color(1.00f, 0.94f, 0.84f),   // ~5000 K: luce calda, non gialla
            LightEnergy = 1.0f,
            ShadowEnabled = true,
            ShadowBias = 0.04f,
            ShadowNormalBias = 1.2f,
            DirectionalShadowMaxDistance = 100f,
            DirectionalShadowBlendSplits = true
        };
        parent.AddChild(a.Sun);

        // Cielo: se in assets/sky c'è una panorama (Sketchfab o HDRI) si usa quella,
        // altrimenti resta il cielo procedurale, che non ha bisogno di nessun file.
        Material skyMat = LoadPanorama() ?? ProceduralSky();

        a.Env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky
            {
                SkyMaterial = skyMat,
                // 2048: i riflessi sul metallo e sul vetro delle finestre leggono il cielo
                RadianceSize = Sky.RadianceSizeEnum.Size2048
            },
            // ruota il cielo per far coincidere il sole della panorama col Direzionale
            SkyRotation = new Vector3(0f, Mathf.DegToRad(SkyYawDeg), 0f),
            // ombre leggibili: metà cielo, metà luce rimbalzata calda dall'intonaco
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightSkyContribution = 0.45f,
            AmbientLightColor = new Color(0.66f, 0.64f, 0.60f),
            AmbientLightEnergy = 0.8f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,

            // Filmic: alte luci morbide sull'intonaco chiaro senza bruciarle
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            TonemapWhite = 6.0f,

            // foschia di mare: profondità, colore dell'orizzonte
            FogEnabled = true,
            FogMode = Godot.Environment.FogModeEnum.Depth,
            FogLightColor = new Color(0.74f, 0.80f, 0.87f),
            FogLightEnergy = 1.0f,
            FogDepthBegin = 120f,
            FogDepthEnd = 400f,
            FogDepthCurve = 1.6f,
            FogSkyAffect = 0.0f,
            FogAerialPerspective = 0.35f,

            // qualità: la decidono i preset
            SsaoEnabled = false,
            SsaoRadius = 1.4f,
            SsaoIntensity = 1.8f,
            SsaoPower = 1.5f,
            GlowEnabled = false,
            GlowIntensity = 0.35f,
            GlowBloom = 0.03f,
            GlowHdrThreshold = 1.1f
        };
        a.WorldEnv = new WorldEnvironment { Name = "Ambiente", Environment = a.Env };
        parent.AddChild(a.WorldEnv);
        return a;
    }
}

/// <summary>
/// Disegna un <see cref="WorldState"/> in graybox (fase A0 del doc 07): niente texture,
/// solo la palette e una griglia proiettata nel mondo (1 m e 4 m, il passo dei kit),
/// che serve a leggere distanze e proporzioni a colpo d'occhio.
///
/// Budget di sviluppo ≤ 200 draw call: le scatole NON sono un nodo l'una. Vengono fuse
/// in poche mesh per settore da 48 m e per materiale. Il colore e la rugosità viaggiano
/// negli UV dei vertici, quindi un materiale solo copre tutta l'architettura.
/// </summary>
public static class MapView
{
    private const float Chunk = 48f;

    private const string WorldShader = @"
shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform float grid_strength : hint_range(0.0, 1.0) = 0.07;
uniform float roof_mode : hint_range(0.0, 1.0) = 0.0;   // 1 = tetto in coppi
uniform float windows_mode : hint_range(0.0, 1.0) = 1.0; // 1 = facciata con finestre

varying vec3 world_pos;
varying vec3 world_normal;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

vec3 srgb_to_linear(vec3 c) {
    return mix(pow((c + vec3(0.055)) * (1.0 / 1.055), vec3(2.4)), c * (1.0 / 12.92), lessThan(c, vec3(0.04045)));
}

// linea di griglia antialiasata: larga `width` pixel a qualsiasi distanza
float grid_line(vec2 p, float width) {
    vec2 d = abs(fract(p - 0.5) - 0.5) / max(fwidth(p), vec2(1e-4));
    return 1.0 - clamp(min(d.x, d.y) / width, 0.0, 1.0);
}

// hash 2D stabile: file di coppi e macchie del lastricato
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void fragment() {
    vec3 n = abs(world_normal);
    vec2 p = n.y > 0.5 ? world_pos.xz : (n.x > 0.5 ? world_pos.zy : world_pos.xy);
    float minor = grid_line(p, 1.0) * 0.45;          // 1 m
    float major = grid_line(p * 0.25, 1.4);          // 4 m: la griglia dei kit
    float g = max(minor, major) * grid_strength;
    // Colore (sRGB) e rugosità arrivano negli UV: gli UV non subiscono conversioni.
    // Forward+ e Mobile illuminano in spazio lineare, Compatibility in sRGB: la
    // conversione la fa lo shader, una volta sola, secondo il renderer in uso.
    vec3 srgb = vec3(UV.x, UV.y, UV2.x);
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
    vec3 albedo = srgb;
#else
    vec3 albedo = srgb_to_linear(srgb);
#endif
    float rough = UV2.y;
    float spec = 0.35;

    // 1 = parete verticale, 0 = pavimento o tetto
    float vertical = smoothstep(0.55, 0.80, 1.0 - n.y);
    float h = world_pos.y;

    if (roof_mode > 0.5) {
        // --------------------------------------------------------------- coppi
        // File da 34 × 42 cm, sfalsate di mezza fila. Sulla faccia orizzontale si
        // legge il tetto; sulla fiancata del rialzo resta la terracotta piena.
        float top = smoothstep(0.55, 0.85, n.y);
        vec2 q = world_pos.xz / vec2(0.34, 0.42);
        q.x += mod(floor(q.y), 2.0) * 0.5;
        vec2 dq = abs(fract(q) - 0.5) / max(fwidth(q), vec2(1e-4));
        float seam = 1.0 - clamp(min(dq.x, dq.y) / 1.3, 0.0, 1.0);
        float tile = hash21(floor(q));
        albedo *= mix(0.90, 0.88 + tile * 0.24, top);
        albedo *= 1.0 - seam * 0.34 * top;
        rough = mix(0.90, 0.95, top);
        spec = 0.22;
        g *= 1.0 - top * 0.75;                        // la griglia non sporca i tetti
    } else {
        // zoccolo scuro alla base: stacca l'edificio dalla strada
        float plinth = vertical * (1.0 - smoothstep(0.30, 0.85, h));
        albedo *= mix(1.0, 0.62, plinth);
        rough = mix(rough, 0.98, plinth);
        spec = mix(spec, 0.30, plinth);

        if (windows_mode > 0.5) {
            // finestre: il materiale decide se il volume è un edificio (non la
            // luminosità: così casse, container e auto restano lisci).
            // Non sono aperture: sono colore, la simulazione non le conosce.
            float building = vertical * smoothstep(0.10, 0.22, dot(albedo, vec3(0.299, 0.587, 0.114)));
            float u = abs(n.x) > 0.5 ? world_pos.z : world_pos.x;
            // l'altezza del volume arriva nel colore del vertice: i piani si
            // distribuiscono nell'altezza reale e l'ultimo non finisce sul cornicione
            float topY = COLOR.r * 64.0;
            float usable = max(2.40, topY - 3.35);
            float rowsN = max(1.0, floor(usable / 3.2 + 0.5));
            float pitch = max(2.2, usable / rowsN);
            vec2 cell = vec2(u, h - 2.9) / vec2(3.4, pitch);
            vec2 f = fract(cell);
            vec2 id = floor(cell);
            float rows = step(0.0, id.y) * (1.0 - step(rowsN - 0.5, id.y));
            float win = step(0.34, f.x) * step(f.x, 0.66) * step(0.30, f.y) * step(f.y, 0.80);
            win *= rows * building;

            // vetro: riflesso del cielo in alto, ombra in basso. Senza gradiente
            // le finestre sono buchi neri; con il gradiente sono vetro.
            vec3 glass = mix(vec3(0.018, 0.024, 0.032), vec3(0.105, 0.145, 0.185),
                             smoothstep(0.34, 0.80, f.y) * 0.85);
            // una finestra su quattro ha la persiana abbassata: legno caldo
            float shutter = step(0.72, hash21(id + 3.7));
            vec3 wood = mix(vec3(0.150, 0.092, 0.055), vec3(0.205, 0.125, 0.070),
                            step(0.5, fract(f.x * 7.0)));
            vec3 fill = mix(glass, wood, shutter);

            albedo = mix(albedo, fill, win * 0.94);
            rough = mix(rough, mix(0.16, 0.74, shutter), win);
            spec = mix(spec, 0.55 * (1.0 - shutter), win);

            // cornice della finestra: una fascia appena più scura della parete,
            // che dà spessore al buco. Il davanzale ha un'ombra sotto.
            float frame = step(0.29, f.x) * step(f.x, 0.71) * step(0.24, f.y) * step(f.y, 0.86);
            frame *= (1.0 - win) * rows * building;
            albedo *= mix(1.0, 0.90, frame);
            float sill = smoothstep(0.24, 0.30, f.y) * (1.0 - smoothstep(0.30, 0.37, f.y));
            sill *= step(0.29, f.x) * step(f.x, 0.71) * rows * building;
            albedo *= mix(1.0, 0.74, sill);

            // terrazze, porticati, piattaforme: lastroni da 80 cm. Sono le superfici
            // orizzontali che senza un disegno restano lastre bianche.
            float terrace = (1.0 - vertical) * smoothstep(2.20, 2.60, h);
            vec2 tq = world_pos.xz / 0.80;
            vec2 td = abs(fract(tq) - 0.5) / max(fwidth(tq), vec2(1e-4));
            float joint = 1.0 - clamp(min(td.x, td.y) / 1.2, 0.0, 1.0);
            albedo *= mix(1.0, 0.90 + hash21(floor(tq)) * 0.14, terrace);
            albedo *= 1.0 - joint * 0.26 * terrace;
            rough = mix(rough, 0.96, terrace);
        }

        // lastricato: macchie larghe, solo a terra. Rompe il piano unico della strada
        // senza costare una draw call.
        float ground = (1.0 - vertical) * (1.0 - smoothstep(0.30, 1.10, h));
        float mottle = hash21(floor(world_pos.xz * 0.65)) * 0.55
                     + hash21(floor(world_pos.xz * 0.21)) * 0.45;
        albedo *= mix(1.0, 0.90 + mottle * 0.16, ground);
        rough = mix(rough, 0.97, ground);
    }

    ALBEDO = albedo * (1.0 - g);
    ROUGHNESS = rough;
    SPECULAR = spec;
}
";

// colori sRGB del doc 07 (e rugosità). Il saturo è SOLO informazione di gioco.
    private static (Color Albedo, float Rough) Look(string kind) => kind switch
    {
        "street" => (new Color("#847E72"), 0.95f),
        "plaster" => (new Color("#D8A97A"), 0.92f),
        "plaster_ochre" => (new Color("#C98A4B"), 0.92f),
        "stone" => (new Color("#C9BFA8"), 0.88f),
        "rock" => (new Color("#7D776E"), 0.95f),
        "concrete" => (new Color("#8C8880"), 0.90f),
        "steel" => (new Color("#55585C"), 0.55f),
        "wood" => (new Color("#8A6B4A"), 0.85f),
        "cover" => (new Color("#B5AC98"), 0.90f),
        "stairs" => (new Color("#BDB29C"), 0.88f),
        "roof" => (new Color("#9C5B3F"), 0.85f),
        "water" => (new Color("#4F6672"), 0.20f),
        _ => (new Color("#A0A0A0"), 0.9f)
    };

    /// <summary>Tipi che portano informazione di gioco: colore pieno e un filo di emissione.</summary>
    private static Color? InfoColor(string kind) => kind switch
    {
        "objective" => new Color("#F2C230"),
        "team_north" => new Color("#2E9BE6"),
        "team_south" => new Color("#E8632E"),
        _ => null
    };

    public static Node3D Build(Node3D parent, WorldState world)
    {
        var root = new Node3D { Name = "Mappa_" + world.Name };
        parent.AddChild(root);

        // Un solo shader, due materiali: world_mode normale e roof_mode per i coppi.
        var worldShader = new Shader { Code = WorldShader };
        var worldMat = new ShaderMaterial { Shader = worldShader };
        worldMat.SetShaderParameter("windows_mode", 0.0f);
        var facadeMat = new ShaderMaterial { Shader = worldShader };   // finestre accese
        facadeMat.SetShaderParameter("windows_mode", 1.0f);
        var roofMat = new ShaderMaterial { Shader = worldShader };
        roofMat.SetShaderParameter("roof_mode", 1.0f);
        var waterMat = new StandardMaterial3D
        {
            AlbedoColor = new Color("#4F6672"),
            Roughness = 0.18f,
            Metallic = 0.0f,
            MetallicSpecular = 0.6f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var infoMats = new Dictionary<string, StandardMaterial3D>();

        // (materiale, settore) → geometria
        var buckets = new Dictionary<(string Mat, int Cx, int Cz), MeshData>();

        void Add(Box b, bool castShadow)
        {
            string kind = b.Kind < world.Kinds.Length ? world.Kinds[b.Kind] : "";
            string mat;
            Color col;
            if (InfoColor(kind) is Color info)
            {
                mat = "info:" + kind;
                col = info;
                if (!infoMats.ContainsKey(mat))
                    infoMats[mat] = new StandardMaterial3D
                    {
                        AlbedoColor = info,
                        Roughness = 0.6f,
                        EmissionEnabled = true,
                        Emission = info,
                        EmissionEnergyMultiplier = 0.15f,
                        CullMode = BaseMaterial3D.CullModeEnum.Disabled
                    };
            }
            else if (kind == "water")
            {
                mat = "water";
                col = new Color(1, 1, 1);
            }
            else
            {
                var look = Look(kind);
                // ±5% di luminosità per scatola: i lotti vicini si distinguono (doc 07: vertex color per le varianti)
                // i tetti si vedono tutti insieme dall alto: lì la varietà serve doppia
                float spread = kind == "roof" ? 0.26f : 0.10f;
                float jitter = 1f + (Hash(b.Min.X, b.Min.Z) - 0.5f) * spread;
                var a = look.Albedo;   // sRGB: la conversione la fa lo shader
                col = new Color(Mathf.Min(a.R * jitter, 1f), Mathf.Min(a.G * jitter, 1f), Mathf.Min(a.B * jitter, 1f), look.Rough);
                // le finestre esistono solo sui volumi che sono edifici
                bool facade = kind is "plaster" or "plaster_ochre" or "stone" or "concrete";
                mat = kind == "roof" ? "roof"
                    : facade ? "facade"
                    : castShadow ? "world" : "world_noshadow";
            }

            var c = b.Center;
            bool huge = b.Size.X > Chunk * 2 || b.Size.Z > Chunk * 2;   // suolo e mare: un settore a sé
            var key = huge ? (mat, int.MinValue, int.MinValue)
                           : (mat, (int)Mathf.Floor(c.X / Chunk), (int)Mathf.Floor(c.Z / Chunk));
            if (!buckets.TryGetValue(key, out var md)) buckets[key] = md = new MeshData();
            md.AddBox(b, col, skipBottom: b.Min.Y <= 0.01f);
        }

        foreach (var b in world.Boxes) Add(b, true);
        foreach (var b in world.Decor) Add(b, world.Kinds.Length > b.Kind && world.Kinds[b.Kind] != "water");

        foreach (var kv in buckets)
        {
            Material material = kv.Key.Mat switch
            {
                "water" => waterMat,
                "roof" => roofMat,
                "facade" => facadeMat,
                var m when m.StartsWith("info:") => infoMats[m],
                _ => worldMat
            };
            var mi = new MeshInstance3D
            {
                Mesh = kv.Value.ToMesh(),
                MaterialOverride = material,
                CastShadow = kv.Key.Mat is "water" or "world_noshadow"
                    ? GeometryInstance3D.ShadowCastingSetting.Off
                    : GeometryInstance3D.ShadowCastingSetting.On
            };
            root.AddChild(mi);
        }
        return root;
    }

    private static float Hash(float x, float z)
    {
        int ix = (int)MathF.Floor(x * 4f), iz = (int)MathF.Floor(z * 4f);
        uint h = unchecked((uint)(ix * 73856093) ^ (uint)(iz * 19349663));
        h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
        return (h & 0xFFFF) / 65535f;
    }

    /// <summary>Accumula triangoli di scatole e li trasforma in una ArrayMesh.</summary>
    private sealed class MeshData
    {
        private readonly List<Vector3> _v = new();
        private readonly List<Vector3> _n = new();
        private readonly List<Vector2> _uv = new();    // R, G (sRGB)
        private readonly List<Vector2> _uv2 = new();   // B (sRGB), rugosità
        private readonly List<Color> _c = new();       // R = altezza del volume / 64

        private float _height;

        public void AddBox(Box b, Color col, bool skipBottom)
        {
            var mn = new Vector3(b.Min.X, b.Min.Y, b.Min.Z);
            var mx = new Vector3(b.Max.X, b.Max.Y, b.Max.Z);
            _height = Mathf.Min(b.Max.Y / 64f, 1f);

            Quad(new Vector3(mn.X, mx.Y, mn.Z), new Vector3(mx.X, mx.Y, mn.Z), new Vector3(mx.X, mx.Y, mx.Z), new Vector3(mn.X, mx.Y, mx.Z), Vector3.Up, col);
            if (!skipBottom)
                Quad(new Vector3(mn.X, mn.Y, mn.Z), new Vector3(mx.X, mn.Y, mn.Z), new Vector3(mx.X, mn.Y, mx.Z), new Vector3(mn.X, mn.Y, mx.Z), Vector3.Down, col);
            Quad(new Vector3(mx.X, mn.Y, mn.Z), new Vector3(mx.X, mx.Y, mn.Z), new Vector3(mx.X, mx.Y, mx.Z), new Vector3(mx.X, mn.Y, mx.Z), Vector3.Right, col);
            Quad(new Vector3(mn.X, mn.Y, mn.Z), new Vector3(mn.X, mx.Y, mn.Z), new Vector3(mn.X, mx.Y, mx.Z), new Vector3(mn.X, mn.Y, mx.Z), Vector3.Left, col);
            Quad(new Vector3(mn.X, mn.Y, mx.Z), new Vector3(mx.X, mn.Y, mx.Z), new Vector3(mx.X, mx.Y, mx.Z), new Vector3(mn.X, mx.Y, mx.Z), Vector3.Back, col);
            Quad(new Vector3(mn.X, mn.Y, mn.Z), new Vector3(mx.X, mn.Y, mn.Z), new Vector3(mx.X, mx.Y, mn.Z), new Vector3(mn.X, mx.Y, mn.Z), Vector3.Forward, col);
        }

        /// <summary>
        /// Un quadrilatero (vertici in giro, ordine qualsiasi) con la normale data.
        /// Godot considera "davanti" i triangoli in senso ORARIO visti dalla normale:
        /// l'ordine si corregge qui. I materiali usano comunque cull_disabled: in graybox
        /// una faccia invisibile per un errore di ordine costa più di un po' di overdraw.
        /// </summary>
        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color col)
        {
            if ((b - a).Cross(c - a).Dot(n) > 0f)
            {
                (b, d) = (d, b);
            }
            Tri(a, b, c, n, col);
            Tri(a, c, d, n, col);
        }

        private void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Color col)
        {
            _v.Add(a); _v.Add(b); _v.Add(c);
            _n.Add(n); _n.Add(n); _n.Add(n);
            var hc = new Color(_height, 0f, 0f, 1f);
            _c.Add(hc); _c.Add(hc); _c.Add(hc);
            var uv = new Vector2(col.R, col.G);
            var uv2 = new Vector2(col.B, col.A);
            _uv.Add(uv); _uv.Add(uv); _uv.Add(uv);
            _uv2.Add(uv2); _uv2.Add(uv2); _uv2.Add(uv2);
        }

        public ArrayMesh ToMesh()
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = _v.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = _n.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = _uv.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV2] = _uv2.ToArray();
            arrays[(int)Mesh.ArrayType.Color] = _c.ToArray();
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            return mesh;
        }
    }
}
