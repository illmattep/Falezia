using System;
using System.Collections.Generic;
using Falezia.Sim;
using Vec2 = System.Numerics.Vector2;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// I MATERIALI DELLA MAPPA: per ogni tipo, quanti metri copre una ripetizione della
/// texture (la foto ha una scala vera: un sampietrino è 10 cm, un coppo 18).
/// Una sola tabella per la mappa e per gli abbellimenti.
/// </summary>
public static class MapMaterials
{
    private static readonly Dictionary<string, float> Tile = new()
    {
        { "street", 2.2f },        // PavingStones070: ~22 sampietrini per lato
        { "plaster", 3.2f },
        { "plaster_ochre", 3.2f },
        { "stone", 2.6f },         // muratura di pietrame
        { "rock", 7.0f },          // scogliera: la foto è una parete intera
        { "concrete", 3.0f },
        { "steel", 2.4f },         // lamiera ondulata dei container
        { "wood", 2.0f },
        { "cover", 2.2f },
        { "stairs", 2.4f },        // lastre da 48 cm
        { "roof", 1.6f },          // coppi da ~18 cm
        { "trim", 1.8f },
        { "shutter", 1.0f },
        { "metal", 1.0f },
        { "glass", 1.0f },
        { "water", 24f },
        { "objective", 2f },
        { "team_north", 3f },
        { "team_south", 3f },
    };

    public static float TileOf(string kind) => kind != null && Tile.TryGetValue(kind, out var t) ? t : 4f;

    /// <summary>
    /// Coordinate texture ancorate al mondo: la stessa formula per i volumi e per gli
    /// abbellimenti, così uno zoccolo continua l'intonaco senza scatti.
    /// </summary>
    public static Vec2 WorldUv(Vec3 p, Vec3 n, float tile)
    {
        if (MathF.Abs(n.X) > 0.5f) return new Vec2(p.Z / tile, p.Y / tile);
        if (MathF.Abs(n.Y) > 0.5f) return new Vec2(p.X / tile, p.Z / tile);
        return new Vec2(p.X / tile, p.Y / tile);
    }
}

/// <summary>
/// GLI ABBELLIMENTI DELLA MAPPA: solo aspetto, nessuna collisione, nessuna forma cambiata.
///
/// Si calcolano dai 671 volumi della simulazione, all'avvio, in modo deterministico
/// (stesse scelte su ogni client: niente casuale vero, solo hash della posizione).
///
/// Regole, perché il gioco resti onesto (la simulazione vede solo le scatole):
/// - **sotto i 3 m niente sporge più di 4 cm** (zoccolo, cornici delle casse, cappe
///   dei muretti): non nasce copertura finta, e nessuna sagoma si stacca dalla scatola;
/// - **niente finestre o porte all'altezza di chi cammina**: dal primo piano in su, e
///   mai sopra un terrazzo o un riparo (sembrerebbero aperture o porte usabili);
/// - **l'erba non nasconde nessuno**: ciuffi da 8 a 30 cm, a terra, contro i muri
///   (fili larghi 2–5 cm alla base: è l'unica eccezione alla regola dei 3 cm, voluta);
/// - le cappe dei muretti stanno DENTRO il volume (non alzano la linea di tiro).
///
/// C# puro, senza UnityEngine: lo si prova e lo si guarda anche fuori dal motore.
/// <see cref="MapBuilder"/> lo trasforma in mesh, una per materiale.
/// </summary>
public static class MapDressing
{
    public sealed class Mesh
    {
        public readonly List<Vec3> Positions = new();
        public readonly List<Vec3> Normals = new();
        public readonly List<Vec2> Uvs = new();
        public readonly List<int> Triangles = new();
        public int Quads;
    }

    public sealed class Result
    {
        public readonly Dictionary<string, Mesh> Meshes = new();
        public int Windows, Tufts, Pipes, Lamps, Crates;

        public Mesh Get(string material)
        {
            if (!Meshes.TryGetValue(material, out var m)) Meshes[material] = m = new Mesh();
            return m;
        }

        public int TriangleCount
        {
            get { int t = 0; foreach (var m in Meshes.Values) t += m.Triangles.Count / 3; return t; }
        }
    }

    private static readonly HashSet<string> BuildingKinds = new() { "plaster", "plaster_ochre", "stone", "concrete" };

    // misure vere (doc 07): piano 2,80 m, finestra 1,1 × 1,5
    private const float Storey = 2.8f;
    private const float WinW = 1.1f, WinH = 1.5f, SillOverFloor = 0.95f;
    private const float Surround = 0.12f;
    private const float LowProtrude = 0.04f;     // tetto alle sporgenze sotto i 3 m

    public static Result Build(WorldState world)
    {
        var ctx = new Ctx(world);
        var res = new Result();

        for (int i = 0; i < world.Boxes.Count; i++)
        {
            var b = world.Boxes[i];
            string kind = KindOf(world, b);
            if (BuildingKinds.Contains(kind) && b.Size.Y >= 3.0f) Facades(ctx, res, b, kind, i);
            else if (kind == "wood" && b.Size.Y <= 1.6f && b.Size.X <= 3f && b.Size.Z <= 3f) Crate(res, b, "wood", 0.07f, 0.018f);
            else if (kind == "steel" && b.Size.Y >= 2.3f && b.Size.Y <= 3.2f) Crate(res, b, "metal", 0.14f, 0.025f);
            else if (kind == "cover" && b.Size.Y <= 1.6f) Capstone(ctx, res, b);

            if (kind == "rock" || kind == "cover" || kind == "stairs" || (kind == "stone" && b.Size.Y < 3f))
                BaseTufts(ctx, res, b, i, kind == "rock" ? 0.55f : 0.3f);
            if (kind == "rock" && b.Max.Y >= 8f) TopTufts(ctx, res, b, i);
        }

        StreetTufts(ctx, res);
        return res;
    }

    private static string KindOf(WorldState w, Box b) => b.Kind < w.Kinds.Length ? w.Kinds[b.Kind] : "";

    // ------------------------------------------------------------------ interrogare il mondo

    private sealed class Ctx
    {
        public readonly WorldState World;
        public Ctx(WorldState w) { World = w; }

        /// <summary>Il punto è dentro una scatola (collisione vera)?</summary>
        public bool Solid(Vec3 p)
        {
            foreach (int i in World.Query(p, p))
            {
                var b = World.Boxes[i];
                const float e = 0.002f;
                if (p.X > b.Min.X + e && p.X < b.Max.X - e &&
                    p.Y > b.Min.Y + e && p.Y < b.Max.Y - e &&
                    p.Z > b.Min.Z + e && p.Z < b.Max.Z - e) return true;
            }
            return false;
        }

        /// <summary>A terra in quel punto: pavimento subito sotto, aria sopra (fino a 1,8 m).</summary>
        public bool Floor(Vec3 p)
        {
            if (!Solid(p - new Vec3(0f, 0.05f, 0f))) return false;
            for (float h = 0.08f; h < 1.8f; h += 0.4f)
                if (Solid(p + new Vec3(0f, h, 0f))) return false;
            return true;
        }
    }

    /// <summary>Hash deterministico 0..1 di una posizione e di un sale.</summary>
    private static float Hash(float x, float y, float z, int salt)
    {
        unchecked
        {
            uint h = (uint)(MathF.Round(x * 100f)) * 73856093u ^ (uint)(MathF.Round(y * 100f)) * 19349663u
                   ^ (uint)(MathF.Round(z * 100f)) * 83492791u ^ (uint)salt * 2654435761u;
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    // ------------------------------------------------------------------ facciate

    /// <summary>Una faccia verticale: piano, normale uscente, asse lungo la faccia.</summary>
    private struct Face
    {
        public Vec3 N, T;          // normale uscente e tangente (lungo la faccia)
        public float Plane;        // coordinata del piano sull'asse della normale
        public float A0, A1;       // estensione lungo la tangente
        public float Y0, Y1;

        public Vec3 At(float u, float y, float out_)
        {
            // u sulla tangente, y in quota, out_ fuori dal muro
            return Abs(N) * Plane + T * u + new Vec3(0f, y, 0f) + N * out_;
        }

        private static Vec3 Abs(Vec3 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
    }

    private static IEnumerable<Face> FacesOf(Box b)
    {
        yield return new Face { N = new Vec3(1, 0, 0), T = new Vec3(0, 0, 1), Plane = b.Max.X, A0 = b.Min.Z, A1 = b.Max.Z, Y0 = b.Min.Y, Y1 = b.Max.Y };
        yield return new Face { N = new Vec3(-1, 0, 0), T = new Vec3(0, 0, 1), Plane = b.Min.X, A0 = b.Min.Z, A1 = b.Max.Z, Y0 = b.Min.Y, Y1 = b.Max.Y };
        yield return new Face { N = new Vec3(0, 0, 1), T = new Vec3(1, 0, 0), Plane = b.Max.Z, A0 = b.Min.X, A1 = b.Max.X, Y0 = b.Min.Y, Y1 = b.Max.Y };
        yield return new Face { N = new Vec3(0, 0, -1), T = new Vec3(1, 0, 0), Plane = b.Min.Z, A0 = b.Min.X, A1 = b.Max.X, Y0 = b.Min.Y, Y1 = b.Max.Y };
    }

    /// <summary>
    /// Tratti della faccia che si vedono davvero a quota y: fuori dal muro c'è aria
    /// (non un altro edificio addossato). Passo di 25 cm.
    /// </summary>
    private static List<(float a, float b)> Exposed(Ctx ctx, Face f, float y, float probe = 0.15f)
    {
        const float step = 0.25f;
        var runs = new List<(float, float)>();
        float start = float.NaN;
        for (float u = f.A0 + step * 0.5f; u < f.A1; u += step)
        {
            bool open = !ctx.Solid(f.At(u, y, probe));
            if (open && float.IsNaN(start)) start = u - step * 0.5f;
            if (!open && !float.IsNaN(start)) { runs.Add((start, u - step * 0.5f)); start = float.NaN; }
        }
        if (!float.IsNaN(start)) runs.Add((start, f.A1));
        return runs;
    }

    private static void Facades(Ctx ctx, Result res, Box b, string kind, int index)
    {
        bool plaster = kind == "plaster" || kind == "plaster_ochre";
        bool concrete = kind == "concrete";

        foreach (var f in FacesOf(b))
        {
            // ---- a terra? (pavimento appena fuori, alla base della faccia)
            var groundRuns = Exposed(ctx, f, f.Y0 + 0.3f);
            bool onGround = false;
            foreach (var r in groundRuns)
                if (ctx.Solid(f.At((r.a + r.b) * 0.5f, f.Y0 - 0.05f, 0.2f))) { onGround = true; break; }

            // ---- zoccolo in pietra (intonaco e cemento), 60 cm, 4 cm fuori
            if (onGround && !(kind == "stone"))
                foreach (var r in groundRuns)
                {
                    if (r.b - r.a < 0.4f) continue;
                    float a = r.a <= f.A0 + 0.01f ? r.a - LowProtrude : r.a;   // all'angolo si chiude con l'altra faccia
                    float e = r.b >= f.A1 - 0.01f ? r.b + LowProtrude : r.b;
                    SlabOnFace(res.Get(concrete ? "concrete" : "stone"), f, a, e, f.Y0, f.Y0 + 0.6f, 0f, LowProtrude, concrete ? "concrete" : "stone");
                    // erba contro lo zoccolo
                    WallTufts(ctx, res, f, a, e, f.Y0, index);
                }

            // ---- cornicione in cima (solo se sopra c'è cielo)
            bool roofFree = !ctx.Solid(new Vec3(b.Center.X, b.Max.Y + 0.1f, b.Center.Z));
            if (roofFree && b.Size.Y >= 3.5f)
                foreach (var r in Exposed(ctx, f, f.Y1 - 0.2f))
                {
                    if (r.b - r.a < 0.6f) continue;
                    float a = r.a <= f.A0 + 0.01f ? r.a - 0.16f : r.a;
                    float e = r.b >= f.A1 - 0.01f ? r.b + 0.16f : r.b;
                    SlabOnFace(res.Get("trim"), f, a, e, f.Y1 - 0.34f, f.Y1 - 0.14f, 0f, 0.07f, "trim");
                    SlabOnFace(res.Get("trim"), f, a, e, f.Y1 - 0.14f, f.Y1, 0f, 0.16f, "trim");
                }

            if (!onGround) continue;

            // ---- finestre dal primo piano in su
            for (int k = 1; ; k++)
            {
                float sill = f.Y0 + Storey * k + SillOverFloor;
                float top = sill + WinH;
                if (top > f.Y1 - 0.55f) break;

                foreach (var r in Exposed(ctx, f, sill + WinH * 0.5f))
                {
                    float len = r.b - r.a;
                    int n = (int)MathF.Floor((len - 0.6f) / 3.3f);
                    if (n <= 0 && len >= 2.0f) n = 1;
                    if (n <= 0) continue;
                    float pitch = len / n;
                    for (int w = 0; w < n; w++)
                    {
                        float u = r.a + pitch * (w + 0.5f);
                        if (!WindowFits(ctx, f, u, sill, top)) continue;
                        float roomL = u - r.a - (WinW * 0.5f + Surround), roomR = r.b - u - (WinW * 0.5f + Surround);
                        Window(res, f, u, sill, plaster || kind == "stone" ? Shutters(f, u, sill, roomL, roomR) : -1, concrete);
                        res.Windows++;
                    }
                }
            }

            // ---- pluviale all'angolo (intonaco), uno su tre
            if (plaster && b.Size.Y >= 4f)
                foreach (var r in groundRuns)
                {
                    if (r.b < f.A1 - 0.01f) continue;             // solo dove la faccia gira l'angolo
                    if (Hash(f.At(r.b, 0, 0).X, f.Y0, f.At(r.b, 0, 0).Z, 11) > 0.34f) continue;
                    var p = f.At(r.b - 0.14f, 0f, 0.07f);
                    Pipe(res.Get("metal"), new Vec3(p.X, f.Y0, p.Z), f.Y1 - 0.36f - f.Y0, 0.055f);
                    res.Pipes++;
                }

            // ---- lampione a muro ogni ~14 m di strada (3 m d'altezza: sopra le teste)
            if ((plaster || kind == "stone") && b.Size.Y >= 4.5f)
                foreach (var r in groundRuns)
                {
                    float len = r.b - r.a;
                    if (len < 5f) continue;
                    int n = Math.Max(1, (int)(len / 14f));
                    for (int l = 0; l < n; l++)
                    {
                        float u = r.a + len * (l + 0.5f) / n;
                        if (Hash(u, f.Y0, f.Plane, 23) > 0.6f) continue;
                        if (ctx.Solid(f.At(u, f.Y0 + 3.0f, 0.5f))) continue;
                        if (!LampClear(ctx, f, u)) continue;
                        Lamp(res, f, u, f.Y0 + 3.0f);
                        res.Lamps++;
                    }
                }
        }
    }

    /// <summary>
    /// La finestra sta tutta su muro libero e SOTTO non c'è niente fino a terra
    /// (un terrazzo, un riparo addossato): altrimenti sembrerebbe un'apertura raggiungibile.
    /// </summary>
    private static bool WindowFits(Ctx ctx, Face f, float u, float sill, float top)
    {
        float h = WinW * 0.5f + Surround;
        if (u - h < f.A0 + 0.15f || u + h > f.A1 - 0.15f) return false;
        foreach (float du in new[] { -h, 0f, h })
        {
            if (ctx.Solid(f.At(u + du, sill - 0.1f, 0.15f))) return false;
            if (ctx.Solid(f.At(u + du, top + 0.15f, 0.15f))) return false;
            for (float y = f.Y0 + 0.3f; y < sill; y += 0.3f)
                if (ctx.Solid(f.At(u + du, y, 0.6f))) return false;
        }
        return true;
    }

    private static bool LampClear(Ctx ctx, Face f, float u)
    {
        for (float y = f.Y0 + 0.3f; y < f.Y0 + 3.0f; y += 0.4f)
            if (ctx.Solid(f.At(u, y, 0.6f))) return false;
        // e sopra: niente lanterne schiacciate sotto un portico o un ponte
        for (float y = f.Y0 + 3.2f; y < f.Y0 + 4.2f; y += 0.25f)
            if (ctx.Solid(f.At(u, y, 0.4f))) return false;
        return true;
    }

    /// <summary>-1 nessuna persiana, 0 chiuse, 1 aperte (se c'è posto ai lati).</summary>
    private static int Shutters(Face f, float u, float sill, float roomL, float roomR)
    {
        var p = f.At(u, sill, 0f);
        float h = Hash(p.X, p.Y, p.Z, 5);
        if (h < 0.22f) return -1;
        if (h < 0.55f) return 0;
        return roomL >= 0.6f && roomR >= 0.6f ? 1 : 0;
    }

    private static void Window(Result res, Face f, float u, float sill, int shutters, bool concrete)
    {
        float top = sill + WinH, hw = WinW * 0.5f;

        // vetro scuro, appena davanti al muro
        SlabOnFace(res.Get("glass"), f, u - hw, u + hw, sill, top, 0.003f, 0.01f, "glass");

        if (concrete)
        {
            // telaio sottile in metallo, nessuna persiana
            var m = res.Get("metal");
            SlabOnFace(m, f, u - hw - 0.05f, u + hw + 0.05f, top, top + 0.05f, 0f, 0.03f, "metal");
            SlabOnFace(m, f, u - hw - 0.05f, u + hw + 0.05f, sill - 0.05f, sill, 0f, 0.05f, "metal");
            SlabOnFace(m, f, u - hw - 0.05f, u - hw, sill, top, 0f, 0.03f, "metal");
            SlabOnFace(m, f, u + hw, u + hw + 0.05f, sill, top, 0f, 0.03f, "metal");
            SlabOnFace(m, f, u - 0.02f, u + 0.02f, sill, top, 0f, 0.02f, "metal");   // montante
            return;
        }

        // cornice in travertino e davanzale
        var t = res.Get("trim");
        // telaio chiaro dentro la luce: montante e traverso, così il vetro non è un buco nero
        SlabOnFace(t, f, u - hw, u + hw, top - 0.05f, top, 0.003f, 0.025f, "trim");
        SlabOnFace(t, f, u - hw, u + hw, sill, sill + 0.05f, 0.003f, 0.025f, "trim");
        SlabOnFace(t, f, u - hw, u - hw + 0.05f, sill, top, 0.003f, 0.025f, "trim");
        SlabOnFace(t, f, u + hw - 0.05f, u + hw, sill, top, 0.003f, 0.025f, "trim");
        SlabOnFace(t, f, u - 0.025f, u + 0.025f, sill, top, 0.003f, 0.022f, "trim");
        SlabOnFace(t, f, u - hw, u + hw, top - WinH * 0.3f - 0.02f, top - WinH * 0.3f + 0.02f, 0.003f, 0.022f, "trim");
        SlabOnFace(t, f, u - hw - Surround, u + hw + Surround, top, top + Surround, 0f, 0.03f, "trim");
        SlabOnFace(t, f, u - hw - Surround, u - hw, sill, top, 0f, 0.03f, "trim");
        SlabOnFace(t, f, u + hw, u + hw + Surround, sill, top, 0f, 0.03f, "trim");
        SlabOnFace(t, f, u - hw - 0.2f, u + hw + 0.2f, sill - 0.07f, sill, 0f, 0.08f, "trim");

        var s = res.Get("shutter");
        if (shutters == 0)
        {
            // chiuse: due ante accostate sopra il vetro, con la fessura in mezzo
            SlabOnFace(s, f, u - hw, u - 0.008f, sill, top, 0.012f, 0.035f, "shutter");
            SlabOnFace(s, f, u + 0.008f, u + hw, sill, top, 0.012f, 0.035f, "shutter");
        }
        else if (shutters == 1)
        {
            // aperte: le ante stanno piatte contro il muro ai lati della cornice
            float l0 = u - hw - Surround - 0.56f, r0 = u + hw + Surround;
            SlabOnFace(s, f, l0, l0 + 0.55f, sill, top, 0f, 0.03f, "shutter");
            SlabOnFace(s, f, r0 + 0.01f, r0 + 0.56f, sill, top, 0f, 0.03f, "shutter");
        }
    }

    private static void Lamp(Result res, Face f, float u, float y)
    {
        var m = res.Get("metal");
        // staffa a L e lanterna: 40 cm fuori dal muro, a 3 m (sopra le teste)
        SlabOnFace(m, f, u - 0.06f, u + 0.06f, y - 0.1f, y + 0.12f, 0f, 0.03f, "metal");
        SlabOnFace(m, f, u - 0.02f, u + 0.02f, y + 0.08f, y + 0.12f, 0f, 0.40f, "metal");
        SlabOnFace(m, f, u - 0.09f, u + 0.09f, y - 0.22f, y + 0.08f, 0.31f, 0.49f, "metal");
        SlabOnFace(res.Get("trim"), f, u - 0.07f, u + 0.07f, y - 0.2f, y + 0.04f, 0.315f, 0.485f, "trim");   // vetri smerigliati
    }

    // ------------------------------------------------------------------ oggetti

    /// <summary>Casse e container: listelli o profili sui dodici spigoli (sporgenza minima).</summary>
    private static void Crate(Result res, Box b, string material, float width, float out_)
    {
        var m = res.Get(material);
        var mn = b.Min - new Vec3(out_, 0f, out_);
        var mx = b.Max + new Vec3(out_, 0f, out_);
        float w = width;
        // verticali
        foreach (var (x, z) in new[] { (mn.X, mn.Z), (mx.X - w, mn.Z), (mn.X, mx.Z - w), (mx.X - w, mx.Z - w) })
            AddBox(m, new Vec3(x, b.Min.Y, z), new Vec3(x + w, b.Max.Y, z + w), material);
        // orizzontali, sopra e sotto
        foreach (float y in new[] { b.Min.Y, b.Max.Y - w })
        {
            AddBox(m, new Vec3(mn.X, y, mn.Z), new Vec3(mx.X, y + w, mn.Z + w), material);
            AddBox(m, new Vec3(mn.X, y, mx.Z - w), new Vec3(mx.X, y + w, mx.Z), material);
            AddBox(m, new Vec3(mn.X, y, mn.Z), new Vec3(mn.X + w, y + w, mx.Z), material);
            AddBox(m, new Vec3(mx.X - w, y, mn.Z), new Vec3(mx.X, y + w, mx.Z), material);
        }
        res.Crates++;
    }

    /// <summary>Muretti e fioriere: la cappa in travertino sta DENTRO l'altezza del volume.</summary>
    private static void Capstone(Ctx ctx, Result res, Box b)
    {
        if (ctx.Solid(new Vec3(b.Center.X, b.Max.Y + 0.05f, b.Center.Z))) return;
        AddBox(res.Get("trim"), new Vec3(b.Min.X - 0.03f, b.Max.Y - 0.08f, b.Min.Z - 0.03f),
                                new Vec3(b.Max.X + 0.03f, b.Max.Y + 0.001f, b.Max.Z + 0.03f), "trim");
    }

    // ------------------------------------------------------------------ erba

    private static void WallTufts(Ctx ctx, Result res, Face f, float a, float e, float y, int salt)
    {
        for (float u = a + 0.3f; u < e - 0.3f; u += 0.9f)
        {
            float h = Hash(u, y, f.Plane, salt * 7 + 3);
            if (h > 0.55f) continue;
            float off = 0.06f + LowProtrude + h * 0.25f;
            var p = f.At(u + (h - 0.2f) * 0.8f, y, off);
            if (!ctx.Floor(p)) continue;
            Tuft(res, p, 0.14f + h * 0.3f, h * 6.283f * 3f, (int)(h * 97f));
            if (h < 0.22f) Tuft(res, p + f.T * 0.22f + f.N * 0.08f, 0.10f + h, h * 31f, (int)(h * 53f) + 1);
        }
    }

    private static void BaseTufts(Ctx ctx, Result res, Box b, int salt, float density)
    {
        foreach (var f in FacesOf(b))
        {
            if (!ctx.Solid(f.At((f.A0 + f.A1) * 0.5f, f.Y0 - 0.05f, 0.2f))) continue;
            for (float u = f.A0 + 0.2f; u < f.A1 - 0.2f; u += 0.7f)
            {
                float h = Hash(u, f.Y0, f.Plane, salt * 13 + 1);
                if (h > density) continue;
                var p = f.At(u, f.Y0, 0.05f + h * 0.2f);
                if (!ctx.Floor(p)) continue;
                Tuft(res, p, 0.12f + h * 0.3f, h * 40f, (int)(h * 71f));
            }
        }
    }

    private static void TopTufts(Ctx ctx, Result res, Box b, int salt)
    {
        for (float x = b.Min.X + 0.5f; x < b.Max.X - 0.5f; x += 1.6f)
            for (float z = b.Min.Z + 0.5f; z < b.Max.Z - 0.5f; z += 1.6f)
            {
                float h = Hash(x, b.Max.Y, z, salt * 5 + 9);
                if (h > 0.3f) continue;
                var p = new Vec3(x + h * 1.2f, b.Max.Y, z - h * 0.9f);
                if (ctx.Solid(p + new Vec3(0f, 0.1f, 0f))) continue;
                Tuft(res, p, 0.25f + h, h * 50f, (int)(h * 41f));
            }
    }

    /// <summary>Fili d'erba tra i sampietrini, sparsi: 8–15 cm, non coprono niente.</summary>
    private static void StreetTufts(Ctx ctx, Result res)
    {
        var w = ctx.World;
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var b in w.Boxes)
        {
            minX = MathF.Min(minX, b.Min.X); minZ = MathF.Min(minZ, b.Min.Z);
            maxX = MathF.Max(maxX, b.Max.X); maxZ = MathF.Max(maxZ, b.Max.Z);
        }
        for (float x = minX; x < maxX; x += 2.1f)
            for (float z = minZ; z < maxZ; z += 2.1f)
            {
                float h = Hash(x, 0f, z, 3);
                if (h > 0.08f) continue;
                var p = new Vec3(x + h * 9f, 0f, z - h * 7f);
                // il primo piano calpestabile dall'alto in giù, entro 7 m
                for (float y = 7f; y >= -0.5f; y -= 0.05f)
                {
                    var q = new Vec3(p.X, y, p.Z);
                    if (!ctx.Solid(q)) continue;
                    var top = new Vec3(p.X, y + 0.05f, p.Z);
                    if (ctx.Floor(top)) Tuft(res, top - new Vec3(0f, 0.04f, 0f), 0.08f + h * 0.9f, h * 90f, (int)(h * 1000f));
                    break;
                }
            }
    }

    /// <summary>
    /// Un ciuffo: fili d'erba VERI, in geometria (niente alfa: nessuna trasparenza da
    /// far funzionare, nessun quadrato verde se la keyword dell'alpha-test non c'è).
    /// Ogni ciuffo è diverso: numero di fili, altezze, piega e colore (verde, oliva,
    /// secco) vengono da un hash della posizione, uguale su ogni client.
    /// La UV porta il colore: u = tinta (0 verde → 1 secco), v = altezza sul filo.
    /// </summary>
    private static void Tuft(Result res, Vec3 p, float height, float yaw, int variant)
    {
        height = MathF.Min(height, 0.3f);
        var m = res.Get("grass");
        float seed = Hash(p.X, p.Y, p.Z, variant * 31 + 7);
        int blades = 6 + (int)(seed * 9f);                      // 6–14 fili
        float dryness = Hash(p.X, p.Z, p.Y, 97);                 // ciuffo verde, oliva o bruciato dal sole
        float spread = height * (0.25f + seed * 0.35f);

        for (int k = 0; k < blades; k++)
        {
            float hk = Hash(p.X + k * 0.37f, p.Y, p.Z - k * 0.21f, variant + k * 13);
            float hk2 = Hash(p.Z + k * 0.11f, p.X, p.Y + k, variant * 3 + k);
            float a = yaw + k * 2.39996f;                         // angolo aureo: fili sparsi in cerchio
            var dir = new Vec3(MathF.Cos(a), 0f, MathF.Sin(a));
            var side = new Vec3(-dir.Z, 0f, dir.X);
            var root = p + dir * (spread * (0.15f + 0.85f * hk));
            float h = height * (0.55f + 0.45f * hk2);
            float lean = h * (0.15f + 0.55f * hk);                 // i fili esterni si piegano in fuori
            float w = 0.012f + 0.014f * hk2;                       // mezza larghezza alla base
            float tint = Math.Clamp(dryness * 0.8f + (hk - 0.5f) * 0.4f, 0f, 1f);

            var mid = root + new Vec3(0f, h * 0.55f, 0f) + dir * (lean * 0.3f);
            var tip = root + new Vec3(0f, h, 0f) + dir * lean;
            // normale inclinata verso l'alto: luce morbida, niente fili neri controsole
            var n = Vec3.Normalize(new Vec3(0f, 1f, 0f) * 0.7f + dir * 0.3f);

            int i0 = m.Positions.Count;
            m.Positions.Add(root - side * w); m.Positions.Add(root + side * w);
            m.Positions.Add(mid - side * (w * 0.6f)); m.Positions.Add(mid + side * (w * 0.6f));
            m.Positions.Add(tip);
            for (int q = 0; q < 5; q++) m.Normals.Add(n);
            m.Uvs.Add(new Vec2(tint, 0.02f)); m.Uvs.Add(new Vec2(tint, 0.02f));
            m.Uvs.Add(new Vec2(tint, 0.55f)); m.Uvs.Add(new Vec2(tint, 0.55f));
            m.Uvs.Add(new Vec2(tint, 0.98f));
            m.Triangles.Add(i0); m.Triangles.Add(i0 + 2); m.Triangles.Add(i0 + 1);
            m.Triangles.Add(i0 + 1); m.Triangles.Add(i0 + 2); m.Triangles.Add(i0 + 3);
            m.Triangles.Add(i0 + 2); m.Triangles.Add(i0 + 4); m.Triangles.Add(i0 + 3);
            m.Quads++;
        }
        res.Tufts++;
    }

    // ------------------------------------------------------------------ geometria

    /// <summary>Lastra appoggiata alla faccia: da out0 a out1 fuori dal muro, [a,e] × [y0,y1].</summary>
    private static void SlabOnFace(Mesh m, Face f, float a, float e, float y0, float y1, float out0, float out1, string material)
    {
        var p0 = f.At(a, y0, out0);
        var p1 = f.At(e, y1, out1);
        AddBox(m, Vec3.Min(p0, p1), Vec3.Max(p0, p1), material);
    }

    private static readonly Vec3[] FaceNormals =
    {
        new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1)
    };

    /// <summary>Scatola con UV ancorate al mondo (stessa formula dei volumi della mappa).</summary>
    public static void AddBox(Mesh m, Vec3 mn, Vec3 mx, string material)
    {
        float tile = MapMaterials.TileOf(material);
        foreach (var n in FaceNormals)
        {
            // quattro angoli della faccia, in ordine antiorario visto da fuori
            Vec3 c = (mn + mx) * 0.5f, h = (mx - mn) * 0.5f;
            Vec3 t1, t2;
            if (MathF.Abs(n.X) > 0.5f) { t1 = new Vec3(0, 0, 1); t2 = new Vec3(0, 1, 0); }
            else if (MathF.Abs(n.Y) > 0.5f) { t1 = new Vec3(1, 0, 0); t2 = new Vec3(0, 0, 1); }
            else { t1 = new Vec3(1, 0, 0); t2 = new Vec3(0, 1, 0); }
            var fc = c + n * Vec3.Dot(h, Abs(n));
            var e1 = t1 * Vec3.Dot(h, t1);
            var e2 = t2 * Vec3.Dot(h, t2);
            int i0 = m.Positions.Count;
            var q = new[] { fc - e1 - e2, fc + e1 - e2, fc + e1 + e2, fc - e1 + e2 };
            foreach (var p in q)
            {
                m.Positions.Add(p);
                m.Normals.Add(n);
                m.Uvs.Add(MapMaterials.WorldUv(p, n, tile));
            }
            // faccia davanti in Unity: Cross(b - a, c - a) verso fuori (orario visto da fuori)
            bool outward = Vec3.Dot(Vec3.Cross(q[1] - q[0], q[2] - q[0]), n) > 0f;
            if (outward)
            {
                m.Triangles.Add(i0); m.Triangles.Add(i0 + 1); m.Triangles.Add(i0 + 2);
                m.Triangles.Add(i0); m.Triangles.Add(i0 + 2); m.Triangles.Add(i0 + 3);
            }
            else
            {
                m.Triangles.Add(i0); m.Triangles.Add(i0 + 2); m.Triangles.Add(i0 + 1);
                m.Triangles.Add(i0); m.Triangles.Add(i0 + 3); m.Triangles.Add(i0 + 2);
            }
            m.Quads++;
        }
    }

    /// <summary>Tubo verticale ottagonale (pluviale), con i collari ogni 1,5 m.</summary>
    private static void Pipe(Mesh m, Vec3 foot, float height, float r)
    {
        const int sides = 8;
        float tile = MapMaterials.TileOf("metal");
        int i0 = m.Positions.Count;
        for (int s = 0; s <= sides; s++)
        {
            float a = s * MathF.PI * 2f / sides;
            var n = new Vec3(MathF.Cos(a), 0f, MathF.Sin(a));
            var p = foot + n * r;
            m.Positions.Add(p); m.Normals.Add(n); m.Uvs.Add(new Vec2(s / (float)sides, 0f));
            m.Positions.Add(p + new Vec3(0f, height, 0f)); m.Normals.Add(n); m.Uvs.Add(new Vec2(s / (float)sides, height / tile));
        }
        for (int s = 0; s < sides; s++)
        {
            int a = i0 + s * 2, b = a + 2;
            // Unity: triangoli in senso orario visti da fuori
            m.Triangles.Add(a); m.Triangles.Add(a + 1); m.Triangles.Add(b);
            m.Triangles.Add(b); m.Triangles.Add(a + 1); m.Triangles.Add(b + 1);
        }
        for (float y = 1.2f; y < height - 0.2f; y += 1.5f)
            AddBox(m, foot + new Vec3(-r - 0.012f, y, -r - 0.012f), foot + new Vec3(r + 0.012f, y + 0.04f, r + 0.012f), "metal");
    }

    private static Vec3 Abs(Vec3 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
}
