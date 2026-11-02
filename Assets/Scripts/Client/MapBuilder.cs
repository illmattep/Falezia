using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// COSTRUZIONE DELLA MAPPA.
///
/// Ogni volume della simulazione diventa un cubo texturizzato. Tre scelte importanti:
///
/// 1. **UV ancorate al mondo**: la coordinata texture si calcola dalla posizione, non
///    dal singolo cubo. Due volumi affiancati che formano lo stesso muro hanno la
///    texture che continua, senza scatti. La scala è quella vera della foto
///    (<see cref="MapMaterials"/>: un sampietrino è 10 cm).
/// 2. **Un mesh per tipo**: i cubi vengono fusi per materiale, quindi la mappa intera
///    (671 volumi + 1200 pezzi di decorazione) costa una draw call per tipo.
/// 3. **Materiali PBR** (URP/Lit): colore, normal map, occlusione e levigatezza da foto
///    CC0 (`tools/map/make_map_pbr.py`), più una variazione larga (`macro`) che rompe la
///    ripetizione sui muri lunghi. Obiettivo e colori di squadra restano piatti: sono
///    informazione di gioco (doc 07).
///
/// Sopra la mappa ci sono gli **abbellimenti** (<see cref="MapDressing"/>): zoccoli,
/// cornicioni, finestre, persiane, pluviali, lampioni, erba. Solo aspetto, nessuna
/// collisione: la forma della mappa resta quella della simulazione. Si spengono con
/// Esc → GRAFICA → DETTAGLI MAPPA.
///
/// Le piazzole di spawn (lastre da 2 cm) restano nella simulazione ma non si disegnano.
/// </summary>
public static class MapBuilder
{
    /// <summary>Colori di ripiego se una texture manca.</summary>
    private static readonly Dictionary<string, Color> Palette = new()
    {
        { "street", new Color32(132, 126, 114, 255) },
        { "plaster", new Color32(216, 169, 122, 255) },
        { "plaster_ochre", new Color32(201, 138, 75, 255) },
        { "stone", new Color32(201, 191, 168, 255) },
        { "rock", new Color32(125, 119, 110, 255) },
        { "concrete", new Color32(140, 136, 128, 255) },
        { "steel", new Color32(85, 88, 92, 255) },
        { "wood", new Color32(138, 107, 74, 255) },
        { "cover", new Color32(181, 172, 152, 255) },
        { "stairs", new Color32(189, 178, 156, 255) },
        { "roof", new Color32(156, 91, 63, 255) },
        { "water", new Color32(79, 102, 114, 255) },
        { "objective", new Color32(242, 194, 48, 255) },
        { "team_north", new Color32(46, 155, 230, 255) },
        { "team_south", new Color32(232, 99, 46, 255) },
        { "trim", new Color32(214, 204, 182, 255) },
        { "shutter", new Color32(92, 104, 86, 255) },
        { "metal", new Color32(70, 72, 74, 255) },
    };

    /// <summary>Le due varianti di intonaco condividono normal e maschera (stessa foto, colore diverso).</summary>
    private static readonly Dictionary<string, string> SharedMaps = new() { { "plaster_ochre", "plaster" } };

    /// <summary>Metri coperti dalla texture di variazione larga (macchie di umido, sole, sporco).</summary>
    private const float MacroMeters = 14f;

    private static GameObject _dressing;

    /// <summary>Accende o spegne gli abbellimenti (erba, finestre, cornici…) senza ricostruire niente.</summary>
    /// <summary>Con la pelle costruita in Blender i volumi degli edifici non si disegnano più.</summary>
    public static bool HideBuildings = true;

    /// <summary>Vero quando la città arriva dal kit (Blender): spegne ogni geometria procedurale.</summary>
    public static bool UseKit = true;   // la mappa definitiva arriverà da Blender in un colpo solo

    /// <summary>Tipi che la pelle sostituisce: muri e tetti degli edifici.</summary>
    private static bool SkinCovers(string kind) => true;   // il blockout non si disegna: la città è il kit

    public static void SetDetail(bool on)
    {
        if (_dressing != null) _dressing.SetActive(on);
    }

    public static Transform Build(WorldState world, Transform parent)
    {
        // ---- livello dal kit: Unity non costruisce più nulla ----
        if (UseKit) return LevelKit.Build(parent);

        var root = new GameObject("Mappa").transform;
        root.SetParent(parent, false);

        var cubeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube = cubeGo.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(cubeGo);
        var cv = cube.vertices;
        var cn = cube.normals;
        var ct = cube.triangles;

        var groups = new Dictionary<string, (List<Vector3> p, List<Vector3> n, List<Vector2> uv)>();
        int index = 0;
        void Add(string kind, Vector3 min, Vector3 max, int seed)
        {
            if (kind == null) kind = "";
            int fam = TintFamily(kind);
            if (fam >= 0) kind = kind + "|" + VariantOf(seed + (int)min.x * 7 + (int)min.z * 13);
            // le piazzole di spawn (2 cm) sono dati di regia, non segnaletica: non si disegnano
            if (kind == "team_north" || kind == "team_south") return;
            float tile = MapMaterials.TileOf(kind);
            if (!groups.TryGetValue(kind, out var g))
                groups[kind] = g = (new List<Vector3>(), new List<Vector3>(), new List<Vector2>());

            for (int i = 0; i < cv.Length; i++)
            {
                var p = new Vector3(
                    Mathf.LerpUnclamped(min.x, max.x, cv[i].x + 0.5f),
                    Mathf.LerpUnclamped(min.y, max.y, cv[i].y + 0.5f),
                    Mathf.LerpUnclamped(min.z, max.z, cv[i].z + 0.5f));
                var n = cn[i];
                var uv = MapMaterials.WorldUv(new System.Numerics.Vector3(p.x, p.y, p.z),
                                              new System.Numerics.Vector3(n.x, n.y, n.z), tile);
                g.p.Add(p);
                g.n.Add(n);
                g.uv.Add(new Vector2(uv.X, uv.Y));
            }
        }

        var notHidden = new Dictionary<string, int>();
        foreach (var b in world.Boxes)
        {
            string kind = b.Kind < world.Kinds.Length ? world.Kinds[b.Kind] : "";
            if (HideBuildings && SkinCovers(kind)) continue;
            notHidden[kind] = notHidden.TryGetValue(kind, out var c0) ? c0 + 1 : 1;
            Add(kind, new Vector3(b.Min.X, b.Min.Y, b.Min.Z),
                       new Vector3(b.Min.X + b.Size.X, b.Min.Y + b.Size.Y, b.Min.Z + b.Size.Z), index++);
        }

        foreach (var b in world.Decor)
        {
            string kind = b.Kind < world.Kinds.Length ? world.Kinds[b.Kind] : "";
            if (HideBuildings && SkinCovers(kind)) continue;    // anche i decor: la pelle li sostituisce
            notHidden["decor:" + kind] = notHidden.TryGetValue("decor:" + kind, out var c1) ? c1 + 1 : 1;
            Add(kind, new Vector3(b.Min.X, b.Min.Y, b.Min.Z),
                       new Vector3(b.Min.X + b.Size.X, b.Min.Y + b.Size.Y, b.Min.Z + b.Size.Z), index++);
        }

        foreach (var pair in groups)
        {
            var g = pair.Value;
            if (g.p.Count == 0) continue;

            // le facce del cubo di Unity: 24 vertici, 36 indici per cubo
            int cubes = g.p.Count / 24;
            var tris = new int[cubes * 36];
            for (int c = 0; c < cubes; c++)
                for (int k = 0; k < 36; k++)
                    tris[c * 36 + k] = c * 24 + ct[k];

            var mesh = MakeMesh("Mappa_" + (pair.Key.Length == 0 ? "box" : pair.Key), g.p, g.n, g.uv, tris);
            var mr = AddRenderer(root, string.IsNullOrEmpty(pair.Key) ? "box" : pair.Key, mesh, Mat(pair.Key));
            if (pair.Key == "water") mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        {
            var sb = new System.Text.StringBuilder("MapBuilder: volumi NON nascosti = ");
            foreach (var kv in notHidden) sb.Append(kv.Key).Append('×').Append(kv.Value).Append(' ');
            Debug.Log(sb.ToString());
        }

        // la città è il KIT assemblato da Blender: niente abbellimenti procedurali,
        // che disegnerebbero ringhiere e tubi sospesi sopra i modelli nuovi
        if (!UseKit) BuildDressing(world, root);
        return root;
    }

    // ------------------------------------------------------------------ abbellimenti

    private static void BuildDressing(WorldState world, Transform root)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = MapDressing.Build(world);

            _dressing = new GameObject("Abbellimenti");
            _dressing.transform.SetParent(root, false);

            foreach (var pair in res.Meshes)
            {
                var d = pair.Value;
                if (d.Positions.Count == 0) continue;
                var p = new List<Vector3>(d.Positions.Count);
                var n = new List<Vector3>(d.Normals.Count);
                var uv = new List<Vector2>(d.Uvs.Count);
                foreach (var v in d.Positions) p.Add(new Vector3(v.X, v.Y, v.Z));
                foreach (var v in d.Normals) n.Add(new Vector3(v.X, v.Y, v.Z));
                foreach (var v in d.Uvs) uv.Add(new Vector2(v.X, v.Y));

                var mesh = MakeMesh("Abbellimenti_" + pair.Key, p, n, uv, d.Triangles.ToArray());
                var mr = AddRenderer(_dressing.transform, pair.Key, mesh, Mat(pair.Key));
                // erba e vetri non proiettano ombre: costano e non aggiungono niente
                if (pair.Key == "grass" || pair.Key == "glass")
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _dressing.SetActive(GameSettings.MapDetail);
            Debug.Log($"MapBuilder: abbellimenti in {sw.ElapsedMilliseconds} ms · finestre {res.Windows} · ciuffi {res.Tufts} · " +
                      $"pluviali {res.Pipes} · lampioni {res.Lamps} · {res.TriangleCount} triangoli · {res.Meshes.Count} draw call");
        }
        catch (System.Exception e)
        {
            // gli abbellimenti non devono mai impedire di giocare: senza, resta la mappa nuda
            Debug.LogError("MapBuilder: abbellimenti disattivati, " + e);
            if (_dressing != null) Object.Destroy(_dressing);
            _dressing = null;
        }
    }

    // ------------------------------------------------------------------ mesh

    private static Mesh MakeMesh(string name, List<Vector3> p, List<Vector3> n, List<Vector2> uv, int[] tris)
    {
        var mesh = new Mesh { name = name };
        mesh.indexFormat = p.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(p);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();   // senza tangenti la normal map illumina storto
        return mesh;
    }

    private static MeshRenderer AddRenderer(Transform parent, string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return mr;
    }

    // ------------------------------------------------------------------ materiali

    private static readonly Dictionary<string, Material> Mats = new();

    /// <summary>
    /// TINTE PER EDIFICIO. La mappa non deve avere due palazzi dello stesso colore attaccati:
    /// ogni volume prende una variante deterministica dalla sua posizione (il gioco è
    /// deterministico ovunque, quindi due partite vedono la stessa città).
    /// Valori vicini a 1: moltiplicano la texture, non la sostituiscono.
    /// </summary>
    private static readonly Color[][] Tints =
    {
        // muratura chiara / intonaco
        new[] { new Color(1.00f, 1.00f, 1.00f), new Color(1.00f, 0.92f, 0.80f),
                new Color(0.94f, 0.86f, 0.78f), new Color(1.00f, 0.84f, 0.72f),
                new Color(0.90f, 0.93f, 0.88f), new Color(0.86f, 0.90f, 0.96f),
                new Color(1.00f, 0.97f, 0.90f), new Color(0.88f, 0.84f, 0.80f) },
        // pietra
        new[] { new Color(1.00f, 1.00f, 1.00f), new Color(0.97f, 0.94f, 0.88f),
                new Color(0.92f, 0.90f, 0.88f), new Color(1.00f, 0.95f, 0.86f),
                new Color(0.90f, 0.88f, 0.84f), new Color(0.95f, 0.95f, 0.97f),
                new Color(1.02f, 0.98f, 0.92f), new Color(0.86f, 0.84f, 0.82f) },
        // persiane dipinte: verde bosco, verde salvia, blu polvere, grigio, rosso scuro…
        new[] { new Color(0.42f, 0.58f, 0.44f), new Color(0.55f, 0.66f, 0.55f),
                new Color(0.42f, 0.52f, 0.64f), new Color(0.66f, 0.66f, 0.64f),
                new Color(0.58f, 0.40f, 0.36f), new Color(0.50f, 0.56f, 0.60f),
                new Color(0.62f, 0.62f, 0.46f), new Color(0.46f, 0.46f, 0.48f) },
        // legno
        new[] { new Color(1.00f, 0.96f, 0.90f), new Color(0.92f, 0.84f, 0.74f),
                new Color(0.84f, 0.78f, 0.70f), new Color(0.96f, 0.90f, 0.80f),
                new Color(0.88f, 0.82f, 0.76f), new Color(0.80f, 0.76f, 0.72f),
                new Color(1.00f, 0.94f, 0.86f), new Color(0.90f, 0.88f, 0.84f) },
    };

    /// <summary>Famiglie che prendono la tinta per edificio; le altre restano com'erano.</summary>
    /// <summary>
    /// Le superfici STILIZZATE (cottura Blender, colore piatto e grana appena accennata) che
    /// sostituiscono le foto per le famiglie di muratura. Il resto resta com'era: sampietrini,
    /// coppi e lamiera hanno già il dettaglio giusto.
    /// </summary>
    private static readonly Dictionary<string, string> Styl = new()
    {
        { "plaster", "styl_plaster" },
        { "plaster_ochre", "styl_plaster_ochre" },
        { "concrete", "styl_plaster" },
        { "stone", "styl_stone" },
        { "shutter", "styl_shutter" },
        { "trim", "styl_trim" },
        { "metal", "styl_metal" },
        { "wood", "styl_wood" },
        { "roof", "styl_roof" },
    };

    private static int TintFamily(string kind) =>
        kind switch
        {
            "plaster" or "plaster_ochre" or "concrete" => 0,
            "stone" => 1,
            "shutter" => 2,
            "wood" => 3,
            _ => -1,
        };

    private static int VariantOf(int seed) => Mathf.Abs(seed * 73856093 ^ 19349663) % 8;
    private static Texture2D _macro;

    private static Material Mat(string kind)
    {
        kind ??= "";
        if (Mats.TryGetValue(kind, out var m)) return m;

        int variant = 0;
        int bar = kind.IndexOf('|');
        if (bar >= 0)
        {
            int.TryParse(kind.Substring(bar + 1), out variant);
            kind = kind.Substring(0, bar);
        }

        var color = Palette.TryGetValue(kind, out var c) ? c : new Color(0.62f, 0.62f, 0.62f);

        if (kind == "glass")
        {
            // vetro scuro e liscio: riflette il cielo
            m = Fx.Make(new Color(0.20f, 0.25f, 0.30f), 0.90f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.25f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
        }
        else if (kind == "grass")
        {
            m = Grass();
        }
        else
        {
            string stylName = Styl.TryGetValue(kind, out var sn) ? sn : null;
            var albedo = stylName != null ? Resources.Load<Texture2D>("MapTex/" + stylName + "_c") : null;
            if (albedo == null && !string.IsNullOrEmpty(kind))
                albedo = Resources.Load<Texture2D>("MapTex/" + kind + "_c");

            if (albedo != null)
            {
                string maps = stylName ?? (SharedMaps.TryGetValue(kind, out var s) ? s : kind);
                if (stylName != null && Resources.Load<Texture2D>("MapTex/" + maps + "_m") == null)
                    maps = "styl_plaster";   // le stilizzate hanno la maschera solo per la famiglia base
                m = Pbr(albedo, Resources.Load<Texture2D>("MapTex/" + maps + "_n"),
                        Resources.Load<Texture2D>("MapTex/" + maps + "_m"), MapMaterials.TileOf(kind));
            }
            else
            {
                // obiettivo, squadre, acqua: piatti (informazione di gioco, doc 07)
                var tex = string.IsNullOrEmpty(kind) ? null : Resources.Load<Texture2D>("MapTex/" + kind);
                m = Fx.Make(color, 0.10f, tex);
            }
        }

        // sulle stilizzate il dettaglio largo resta un soffio: la superficie deve leggersi piatta
        if (Styl.ContainsKey(kind))
        {
            // sulle superfici dipinte il dettaglio largo non serve: era lui a fare la
            // fascia calda in basso ai muri
            if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
            if (m.HasProperty("_DetailAlbedoMapScale")) m.SetFloat("_DetailAlbedoMapScale", 0f);
            // senza maschera la levigatezza resterebbe a 1: i muri diventavano specchi del cielo
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        }

        // tinta dell'edificio: moltiplica la texture, così due palazzi vicini non sono uguali
        int fam = TintFamily(kind);
        if (fam >= 0 && m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", Tints[fam][Mathf.Clamp(variant, 0, 7)]);

        m.name = "Mappa_" + (kind.Length == 0 ? "box" : kind) + (variant > 0 ? "_v" + variant : "");
        Mats[kind] = m;
        return m;
    }

    private static Shader _mapLit;

    /// <summary>
    /// Materiale PBR della mappa. Prima scelta lo shader <c>Falezia/MapLit</c>
    /// (<c>Resources/Shaders</c>, generato da <c>tools/map/make_map_shaders.py</c>): è URP/Lit
    /// con normal map, maschera (occlusione, levigatezza) e variazione larga ACCESE nel
    /// codice. Su URP/Lit quelle funzioni dipendono da keyword che sui materiali creati a
    /// runtime non arrivano nella build: senza maschera la levigatezza saliva a 1 e le
    /// strade diventavano specchi. Ripiego, se lo shader manca: URP/Lit con le keyword.
    /// </summary>
    private static Material Pbr(Texture2D albedo, Texture2D normal, Texture2D mask, float tile)
    {
        if (_macro == null) _macro = Resources.Load<Texture2D>("MapTex/macro");
        if (_mapLit == null)
        {
            _mapLit = Resources.Load<Shader>("Shaders/MapLit");
            if (_mapLit == null) _mapLit = Shader.Find("Falezia/MapLit");
            if (_mapLit != null && !_mapLit.isSupported) _mapLit = null;
            if (_mapLit == null) Debug.LogWarning("MapBuilder: shader Falezia/MapLit assente, ripiego su URP/Lit con keyword");
        }
        if (_mapLit != null)
        {
            var d = new Material(_mapLit);
            d.SetTexture("_BaseMap", albedo);
            d.SetColor("_BaseColor", Color.white);
            if (normal != null) d.SetTexture("_BumpMap", normal);
            d.SetFloat("_BumpScale", 1f);
            if (mask != null)
            {
                d.SetTexture("_MetallicGlossMap", mask);
                d.SetTexture("_OcclusionMap", mask);
            }
            d.SetFloat("_OcclusionStrength", 1f);
            d.SetFloat("_Smoothness", 1f);   // moltiplica l'alfa della maschera: il valore vero sta nella texture
            if (_macro != null)
            {
                d.SetTexture("_DetailAlbedoMap", _macro);
                d.SetTextureScale("_DetailAlbedoMap", Vector2.one * (tile / MacroMeters));
            }
            d.SetFloat("_DetailAlbedoMapScale", 1f);
            return d;
        }

        var m = Fx.Make(Color.white, 0.25f, albedo);

        if (normal != null && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
        }

        if (mask != null)
        {
            // ripiego: levigatezza tenuta bassa (0,25 × maschera), così anche se la keyword della
            // maschera non arriva nella build la strada non diventa uno specchio
            // maschera: R metallo · G occlusione · A levigatezza
            if (m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", mask);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (m.HasProperty("_OcclusionMap"))
            {
                m.SetTexture("_OcclusionMap", mask);
                m.SetFloat("_OcclusionStrength", 1f);
                m.EnableKeyword("_OCCLUSIONMAP");
            }
        }
        else if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness", 0.25f);
        }

        // variazione larga: grigio 0,5 = neutro nel "detail ×2". La UV della mappa è in
        // ripetizioni di texture, quindi la scala del dettaglio è tile / metri voluti
        if (_macro != null && m.HasProperty("_DetailAlbedoMap"))
        {
            m.SetTexture("_DetailAlbedoMap", _macro);
            m.SetTextureScale("_DetailAlbedoMap", Vector2.one * (tile / MacroMeters));
            m.SetFloat("_DetailAlbedoMapScale", 1f);
            m.EnableKeyword("_DETAIL_MULX2");
        }
        return m;
    }

    /// <summary>
    /// Erba: fili in geometria, materiale OPACO a due facce (nessuna trasparenza).
    /// Il colore viene da una piccola sfumatura generata qui: u = tinta (verde → secco),
    /// v = altezza sul filo (base scura, punta chiara).
    /// </summary>
    private static Material Grass()
    {
        const int W = 16, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true)
        {
            name = "Erba",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var green = new Color(0.33f, 0.40f, 0.20f);    // verde oliva spento (doc 07: ambiente desaturato)
        var dry = new Color(0.58f, 0.53f, 0.33f);      // erba bruciata dal sole
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float t = x / (W - 1f), v = y / (H - 1f);
                var c = Color.Lerp(green, dry, t);
                c *= Mathf.Lerp(0.45f, 1.08f, Mathf.Pow(v, 0.7f));    // radice in ombra, punta al sole
                c.a = 1f;
                px[y * W + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply(true, true);

        var m = Fx.Make(Color.white, 0.08f, tex);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);    // due facce: è un render state, non una keyword
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }
}
