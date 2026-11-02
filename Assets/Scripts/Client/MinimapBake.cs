using System;
using System.Collections.Generic;
using Falezia.Sim;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// IL DISEGNO DELLA MINIMAPPA, calcolato dalle scatole della mappa.
///
/// Si vede solo dove si cammina davvero (come in Valorant): niente tetti, niente muri,
/// niente coperture. Per saperlo non si guarda la grafica ma la collisione:
///
/// 1. Ogni colonna di 25 cm raccoglie gli intervalli verticali delle scatole che la
///    toccano. Un piano è calpestabile se sopra ha almeno l'altezza da accovacciati.
/// 2. Si riempie a macchia d'olio dagli spawn con la regola vera del movimento: si sale
///    solo di <see cref="Constants.StepHeight"/>, si scende quanto si vuole. Quello che
///    non si raggiunge (tetti, cime dei muri, coperture da 1 m) resta trasparente.
/// 3. Il colore schiarisce con la quota; un bordo pieno contorna l'area calpestabile e
///    una linea più tenue segna i dislivelli sopra il mezzo metro (i rialzi).
///
/// C# puro, senza UnityEngine: si prova anche fuori dal motore. La texture la crea
/// <see cref="Minimap"/>. Il raster ha la riga 0 a Z minima, come vuole Texture2D.
/// </summary>
public static class MinimapBake
{
    public sealed class Raster
    {
        public int Width, Height;
        /// <summary>Angolo (X, Z) minimo del raster, in metri.</summary>
        public float MinX, MinZ;
        /// <summary>Lato di un pixel, in metri.</summary>
        public float Cell;
        /// <summary>RGBA, riga 0 = Z minima.</summary>
        public byte[] Rgba;
        /// <summary>Riquadro dell'area calpestabile (X/Z min e max), per inquadrare la mappa.</summary>
        public float ReachMinX, ReachMinZ, ReachMaxX, ReachMaxZ;
        public int WalkableCells;
    }

    public struct Style
    {
        public float Cell;            // metri per pixel
        public float OutlineMeters;   // spessore del contorno
        public float LedgeMeters;     // spessore della linea dei rialzi
        public float LedgeMin;        // dislivello minimo per tracciare un rialzo
        public float FullBrightAt;    // quota (sopra il piano più basso) a cui il riempimento è al massimo
        public Rgba8 Fill, FillHigh, Outline, Ledge;

        public static Style Default => new Style
        {
            Cell = 0.25f,
            OutlineMeters = 0.75f,
            LedgeMeters = 0.5f,
            LedgeMin = 0.5f,
            FullBrightAt = 6f,
            // la mappa sta su un cerchio scuro (shader MinimapDisc): il calpestabile è grigio
            // chiaro, i rialzi più chiari ancora, i bordi bianchi. Quello che non si calpesta
            // resta trasparente e lascia vedere il fondo scuro
            Fill = new Rgba8(104, 116, 130, 200),
            FillHigh = new Rgba8(160, 172, 186, 220),
            Outline = new Rgba8(236, 240, 244, 235),
            Ledge = new Rgba8(214, 222, 230, 200),
        };
    }

    public readonly struct Rgba8
    {
        public readonly byte R, G, B, A;
        public Rgba8(byte r, byte g, byte b, byte a) { R = r; G = g; B = b; A = a; }
    }

    private const int Layers = 6;            // piani calpestabili per colonna, al massimo
    private const float Overlap = 0.002f;    // una scatola che sfiora la cella non la tocca

    public static Raster Bake(WorldState world) => Bake(world, Style.Default);

    public static Raster Bake(WorldState world, Style st)
    {
        float cell = st.Cell > 0.01f ? st.Cell : 0.25f;

        // ---- estensione: tutte le scatole, più un margine per il contorno
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var b in world.Boxes)
        {
            minX = Math.Min(minX, b.Min.X); minZ = Math.Min(minZ, b.Min.Z);
            maxX = Math.Max(maxX, b.Max.X); maxZ = Math.Max(maxZ, b.Max.Z);
        }
        if (world.Boxes.Count == 0) { minX = minZ = -1f; maxX = maxZ = 1f; }
        float pad = st.OutlineMeters + cell * 2f;
        minX -= pad; minZ -= pad; maxX += pad; maxZ += pad;

        int w = Math.Max(1, (int)Math.Ceiling((maxX - minX) / cell));
        int h = Math.Max(1, (int)Math.Ceiling((maxZ - minZ) / cell));
        int n = w * h;

        // ---- 1) intervalli verticali per colonna (lista concatenata, niente allocazioni per cella)
        var head = new int[n];
        for (int i = 0; i < n; i++) head[i] = -1;
        var next = new List<int>(1 << 20);
        var lo = new List<float>(1 << 20);
        var hi = new List<float>(1 << 20);

        foreach (var b in world.Boxes)
        {
            // conservativo: basta toccare la cella. Così anche un muro da 30 cm chiude il passaggio,
            // e il corridoio si stringe di poco ai lati (= il raggio del giocatore)
            int x0 = Clamp((int)Math.Floor((b.Min.X + Overlap - minX) / cell), 0, w - 1);
            int x1 = Clamp((int)Math.Ceiling((b.Max.X - Overlap - minX) / cell) - 1, 0, w - 1);
            int z0 = Clamp((int)Math.Floor((b.Min.Z + Overlap - minZ) / cell), 0, h - 1);
            int z1 = Clamp((int)Math.Ceiling((b.Max.Z - Overlap - minZ) / cell) - 1, 0, h - 1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    int c = z * w + x;
                    next.Add(head[c]);
                    lo.Add(b.Min.Y);
                    hi.Add(b.Max.Y);
                    head[c] = next.Count - 1;
                }
        }

        // ---- piani calpestabili: cima di un blocco pieno con almeno l'altezza da accovacciati sopra
        var count = new byte[n];
        var surf = new float[n * Layers];
        var ceil = new float[n * Layers];
        var tmp = new List<(float lo, float hi)>(16);

        for (int c = 0; c < n; c++)
        {
            if (head[c] < 0) continue;
            tmp.Clear();
            for (int k = head[c]; k >= 0; k = next[k]) tmp.Add((lo[k], hi[k]));
            tmp.Sort((a, b) => a.lo.CompareTo(b.lo));

            // fonde gli intervalli che si toccano: una scala di gradini diventa un blocco solo
            int merged = 0;
            for (int i = 0; i < tmp.Count; i++)
            {
                if (merged > 0 && tmp[i].lo <= tmp[merged - 1].hi + 0.01f)
                {
                    var m = tmp[merged - 1];
                    tmp[merged - 1] = (m.lo, Math.Max(m.hi, tmp[i].hi));
                }
                else tmp[merged++] = tmp[i];
            }

            int layers = 0;
            for (int i = 0; i < merged && layers < Layers; i++)
            {
                float top = tmp[i].hi;
                float roof = i + 1 < merged ? tmp[i + 1].lo : float.PositiveInfinity;
                if (roof - top < Constants.CrouchHeight) continue;
                surf[c * Layers + layers] = top;
                ceil[c * Layers + layers] = roof;
                layers++;
            }
            count[c] = (byte)layers;
        }

        // ---- 2) macchia d'olio dagli spawn, con la regola del gradino
        var seen = new bool[n * Layers];
        var queue = new Queue<int>(1 << 16);

        void Seed(Vec3 feet)
        {
            int x = (int)Math.Floor((feet.X - minX) / cell), z = (int)Math.Floor((feet.Z - minZ) / cell);
            if (x < 0 || z < 0 || x >= w || z >= h) return;
            int c = z * w + x, best = -1;
            float bestD = float.MaxValue;
            for (int k = 0; k < count[c]; k++)
            {
                float d = Math.Abs(surf[c * Layers + k] - feet.Y);
                if (d < bestD) { bestD = d; best = k; }
            }
            if (best < 0 || bestD > 1f) return;
            int node = c * Layers + best;
            if (seen[node]) return;
            seen[node] = true;
            queue.Enqueue(node);
        }

        foreach (var s in world.SpawnsNorth) Seed(s);
        foreach (var s in world.SpawnsSouth) Seed(s);
        foreach (var p in world.Points) Seed(p.Pos);   // i punti con nome sono luoghi raggiungibili per costruzione

        int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
        float stepUp = Constants.StepHeight + 0.02f;
        while (queue.Count > 0)
        {
            int node = queue.Dequeue();
            int c = node / Layers;
            int cx = c % w, cz = c / w;
            float y = surf[node], yCeil = ceil[node];

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dx[d], nz = cz + dz[d];
                if (nx < 0 || nz < 0 || nx >= w || nz >= h) continue;
                int nc = nz * w + nx;
                for (int k = 0; k < count[nc]; k++)
                {
                    int nn = nc * Layers + k;
                    if (seen[nn]) continue;
                    float ny = surf[nn];
                    if (ny - y > stepUp) continue;                       // troppo alto: serve il salto
                    float top = Math.Max(y, ny) + Constants.CrouchHeight;
                    if (top > ceil[nn] || top > yCeil) continue;          // si sbatte la testa
                    seen[nn] = true;
                    queue.Enqueue(nn);
                }
            }
        }

        // ---- quota mostrata: il piano raggiunto più alto della colonna (come Valorant: il ponte copre la strada)
        var shown = new float[n];
        float floorY = float.MaxValue;
        var r = new Raster
        {
            Width = w, Height = h, MinX = minX, MinZ = minZ, Cell = cell,
            ReachMinX = float.MaxValue, ReachMinZ = float.MaxValue,
            ReachMaxX = float.MinValue, ReachMaxZ = float.MinValue
        };

        for (int c = 0; c < n; c++)
        {
            shown[c] = float.NaN;
            for (int k = count[c] - 1; k >= 0; k--)
                if (seen[c * Layers + k]) { shown[c] = surf[c * Layers + k]; break; }
            if (float.IsNaN(shown[c])) continue;

            floorY = Math.Min(floorY, shown[c]);
            r.WalkableCells++;
            float x = minX + (c % w + 0.5f) * cell, z = minZ + (c / w + 0.5f) * cell;
            r.ReachMinX = Math.Min(r.ReachMinX, x); r.ReachMaxX = Math.Max(r.ReachMaxX, x);
            r.ReachMinZ = Math.Min(r.ReachMinZ, z); r.ReachMaxZ = Math.Max(r.ReachMaxZ, z);
        }
        if (r.WalkableCells == 0)
        {
            r.ReachMinX = minX; r.ReachMaxX = maxX; r.ReachMinZ = minZ; r.ReachMaxZ = maxZ;
            floorY = 0f;
        }

        // ---- 3) colori: contorno, rialzi, riempimento per quota
        int ro = Math.Max(1, (int)Math.Round(st.OutlineMeters / cell));
        int rl = Math.Max(1, (int)Math.Round(st.LedgeMeters / cell));
        var rgba = new byte[n * 4];

        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                int c = z * w + x;
                float y = shown[c];
                if (float.IsNaN(y)) continue;

                bool outline = false, ledge = false;
                for (int oz = -ro; oz <= ro && !outline; oz++)
                    for (int ox = -ro; ox <= ro; ox++)
                    {
                        int d2 = ox * ox + oz * oz;
                        if (d2 > ro * ro) continue;
                        int qx = x + ox, qz = z + oz;
                        float q = qx < 0 || qz < 0 || qx >= w || qz >= h ? float.NaN : shown[qz * w + qx];
                        if (float.IsNaN(q)) { outline = true; break; }
                        // la linea del rialzo sta sul lato ALTO del dislivello, come un bordo di marciapiede
                        if (d2 <= rl * rl && y - q > st.LedgeMin) ledge = true;
                    }

                Rgba8 col;
                if (outline) col = st.Outline;
                else if (ledge) col = st.Ledge;
                else col = Lerp(st.Fill, st.FillHigh, Clamp01((y - floorY) / Math.Max(0.1f, st.FullBrightAt)));

                rgba[c * 4] = col.R; rgba[c * 4 + 1] = col.G; rgba[c * 4 + 2] = col.B; rgba[c * 4 + 3] = col.A;
            }

        r.Rgba = rgba;
        return r;
    }

    private static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static Rgba8 Lerp(Rgba8 a, Rgba8 b, float t) => new Rgba8(
        (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t), (byte)(a.A + (b.A - a.A) * t));
}
