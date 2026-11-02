using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Vestizione dell'INTERA mappa oltre al porto hand-made: ogni scatola del blockout
    /// diventa un edificio vero (corpo + facciate su tutti i lati esposti + tetto), una
    /// scala, una roccia o un arredo. Il blockout viene spento e la collisione passa alle
    /// mesh nuove (CalaneraCollision esporta i solidi e mette i MeshCollider per i fori
    /// dei proiettili).
    /// </summary>
    public static partial class CalaneraSpawn
    {
        private static readonly string[] BuildingKinds = { "plaster", "plaster_ochre", "stone", "concrete" };

        private static void Citta()
        {
            var all = Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude)
                            .OrderBy(b => b.index).ToList();
            if (all.Count == 0) return;

            foreach (var b in all)
            {
                var min = b.importedMin;
                var max = min + b.importedSize;
                var c = (min + max) * 0.5f;
                var size = max - min;

                // il porto è già vestito a mano: lascia fare a NascondiBlockout
                if (b.kind == "street") { Hide(b); continue; }
                // il porto è già vestito a mano (piazzole comprese)
                // il porto hand-made resta a nord (magazzini e piazzale); la fila delle case
                // a sud (z >= -82.5) la costruisce il generatore, così il vicolo centrale resta aperto
                if (c.x > -27f && c.x < 25.5f && c.z > -102f && c.z < -82.5f) continue;
                if (b.kind.StartsWith("team_")) { CittaPiazzola(min, max, b.kind); Hide(b); continue; }

                Hide(b);

                // il solettone basso del porticato del mercato (3,15 m) va tolto:
                // lasciava il banco in una scatola cieca. Il mercato resta aperto.
                if (b.kind == "stone" && size.y < 0.6f && size.z > 10f)
                {
                    Hide(b);
                    continue;
                }
                if (BuildingKinds.Contains(b.kind) && size.x > 2.2f && size.z > 2.2f && size.y > 2.4f)
                    CittaEdificio(b, min, max, all);
                else if (b.kind == "stairs") CittaScala(min, max);
                else if (b.kind == "rock") CittaRoccia(min, max);
                else CittaArredo(b, min, max);
            }

            CittaDettagli(all);
        }

        private static void Hide(BlockoutBox b)
        {
            var r = b.GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false;
            var col = b.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
        }

        private static bool Exposed(Vector3 p, List<BlockoutBox> all, int skip)
        {
            foreach (var o in all)
            {
                if (o.index == skip) continue;
                var mn = o.importedMin;
                var mx = mn + o.importedSize;
                if (p.x > mn.x - 0.18f && p.x < mx.x + 0.18f &&
                    p.y > mn.y - 0.18f && p.y < mx.y + 0.18f &&
                    p.z > mn.z - 0.18f && p.z < mx.z + 0.18f) return false;
            }
            return true;
        }

        private static void CittaEdificio(BlockoutBox b, Vector3 min, Vector3 max, List<BlockoutBox> all)
        {
            var size = max - min;
            string wall = WallFor(b.kind);
            string shutter = ShutterFor(b.kind);
            var wg = B(wall);

            // corpo: stesso volume del blockout, ora è mesh nuova e collisione
            wg.Box(min, max, bottom: false);

            // lati esposti
            var sides = new List<(int s, float w, Vector3 o, Vector3 r, Vector3 n, Vector3 center)>();
            for (int s = 0; s < 4; s++)
            {
                var n = s == 0 ? Vector3.right : s == 1 ? Vector3.left : s == 2 ? Vector3.forward : Vector3.back;
                // La "destra" va presa guardando la facciata DA FUORI: R x su = normale.
                // Con (-n.z, 0, n.x) la normale usciva opposta e le cornici finivano
                // 14 cm fuori dal muro, sospese sull'angolo.
                var r = new Vector3(n.z, 0, -n.x);
                float w = s < 2 ? size.z : size.x;
                var center = s == 0 ? new Vector3(max.x, 0, (min.z + max.z) * 0.5f)
                           : s == 1 ? new Vector3(min.x, 0, (min.z + max.z) * 0.5f)
                           : s == 2 ? new Vector3((min.x + max.x) * 0.5f, 0, max.z)
                           : new Vector3((min.x + max.x) * 0.5f, 0, min.z);
                var o = center - r * (w * 0.5f);
                o.y = min.y;
                if (Exposed(center + n * 0.25f + Vector3.up * 1.5f, all, b.index))
                    sides.Add((s, w, o, r, n, center));
            }
            if (sides.Count == 0) return;

            int front = sides.OrderByDescending(s => s.w).First().s;
            foreach (var side in sides)
            {
                float H = size.y;
                float W = side.w;
                var f = new Facciata(side.o, side.r, W, H, wall, shutter);

                bool isFront = side.s == front;
                bool shop = isFront && (b.kind == "plaster" || b.kind == "plaster_ochre") && W > 5.5f;

                if (shop)
                {
                    f.Vetrina(W * 0.5f, 0.0f, Mathf.Min(2.6f, W * 0.45f), 2.6f);
                    f.Insegna("D_Bar", W * 0.5f, 3.05f, 0.42f, true);
                }
                else if (isFront && W > 4.2f)
                {
                    f.Porta(W * 0.5f, 0.0f, 1.22f, 2.35f, DoorFor(b.kind));
                }

                int rows = 0;
                for (float y = 2.55f; y + 1.55f < H - 0.55f; y += 3.05f)
                {
                    rows++;
                    int cols = Mathf.Clamp(Mathf.FloorToInt((W - 1.2f) / 2.35f), 1, 6);
                    for (int i = 0; i < cols; i++)
                    {
                        float cx = Mathf.Lerp(1.0f, W - 1.0f, cols == 1 ? 0.5f : i / (float)(cols - 1));
                        if (shop && y < 2.6f) continue;
                        if (isFront && Mathf.Abs(cx - W * 0.5f) < 1.25f && y < 3.2f) continue;
                        f.AggiungiFinestra(cx, y, 1.05f, 1.42f);
                        if ((i + rows + b.index) % 4 == 0) CittaFioriera(f, cx, y - 0.05f);
                    }
                }

                f.Muri();
                f.Cornicione();
                f.Pluviale(0.32f);
                f.Pluviale(W - 0.32f);

                // dettagli: lampada al portone, balconi, tenda, poster, crepe
                if (isFront)
                {
                    CittaLampada(f, W * 0.5f, 3.1f);
                    if (shop) CittaTenda(f, W * 0.5f, 2.55f, Mathf.Min(2.4f, W * 0.4f));
                    if (b.index % 2 == 0) CittaPanni(f, W * 0.5f, 3.15f);
                    if (b.index % 3 == 0) CittaACWall(f, W * 0.28f, 4.5f);
                }
                if (rows >= 2 && (isFront || W > 4.4f)) CittaBalcone(f, W * 0.5f, 3.0f, W);
                if (b.index % 3 == 0) CittaPoster(f, W, H, b.index);
                CittaCrepaMuro(f, W, H, b.index);

                // tetto a due falde sul lato frontale; gli altri lati hanno il cornicione
                if (isFront)
                {
                    float depth = Mathf.Abs(Vector3.Dot(size, side.n));
                    if (b.kind == "concrete")
                    {
                        var coppi = B("M_Coppi");
                        coppi.Box(new Vector3(min.x - 0.15f, max.y, min.z - 0.15f),
                                  new Vector3(max.x + 0.15f, max.y + 0.22f, max.z + 0.15f));
                        CittaAC(min, max, b.index);
                    }
                    else
                    {
                        // tetto piano con parapetto: niente falde in coppi sospese nel costruito fitto
                        f.Cornicione();
                    }
                }
            }

            // dettagli di copertura
            if (b.kind != "concrete") CittaAC(min, max, b.index);
            if (b.index % 4 == 0) CittaCiminiere(min, max, b.index);
            if (b.index % 5 == 1) CittaAntenna(min, max, b.index);
        }

        private static void CittaFioriera(Facciata f, float x, float y)
        {
            var pot = B("M_Porto_LegnoScuro");
            pot.Box(f.P(x - 0.45f, y, 0.10f), f.P(x + 0.45f, y + 0.16f, 0.30f));
            var green = B("M_Porto_VerdeLanterna");
            for (int i = 0; i < 5; i++)
            {
                float bx = x - 0.34f + i * 0.17f;
                green.Tri(f.P(bx, y + 0.16f, 0.20f), f.P(bx + 0.06f, y + 0.42f, 0.24f),
                          f.P(bx - 0.05f, y + 0.38f, 0.30f), (f.Out + Vector3.up).normalized);
            }
        }

        private static void CittaPanni(Facciata f, float x, float y)
        {
            var ferro = B("M_Porto_Ferro");
            ferro.Cylinder(f.P(x - 1.5f, y, 0.52f), f.P(x + 1.5f, y, 0.52f), 0.012f, 5);
            var colors = new[] { "M_Porto_BiancoRotto", "M_Porto_Ciano", "M_Porto_RossoSpento" };
            for (int i = 0; i < 4; i++)
            {
                var g = B(colors[(i + 1) % colors.Length]);
                float bx = x - 1.1f + i * 0.62f;
                g.Quad(f.P(bx, y, 0.52f), f.P(bx + 0.42f, y, 0.52f),
                       f.P(bx + 0.42f, y - 0.55f, 0.52f), f.P(bx, y - 0.55f, 0.52f), f.Out);
            }
        }

        private static void CittaACWall(Facciata f, float x, float y)
        {
            var g = B("M_Porto_LamieraGrigio");
            g.Box(f.P(x - 0.45f, y, 0.18f), f.P(x + 0.45f, y + 0.55f, 0.45f));
            var ferro = B("M_Porto_Ferro");
            for (int i = 0; i < 4; i++)
                ferro.Box(f.P(x - 0.40f, y + 0.06f + i * 0.11f, 0.45f), f.P(x + 0.40f, y + 0.09f + i * 0.11f, 0.47f));
        }

        private static void CittaAntenna(Vector3 min, Vector3 max, int seed)
        {
            var g = B("M_Porto_Ferro");
            float x = Mathf.Lerp(min.x, max.x, 0.2f + (seed % 5) * 0.15f);
            float z = Mathf.Lerp(min.z, max.z, 0.25f + (seed % 3) * 0.2f);
            g.Cylinder(new Vector3(x, max.y, z), new Vector3(x, max.y + 2.6f, z), 0.018f, 5);
            for (int i = 0; i < 3; i++)
            {
                float yy = max.y + 1.2f + i * 0.55f;
                g.Cylinder(new Vector3(x - 0.45f, yy, z), new Vector3(x + 0.45f, yy, z), 0.012f, 4);
            }
        }

        private static void CittaScala(Vector3 min, Vector3 max)
        {
            // gradini veri, non un blocco unico: la scala non entra più nel muro
            var g = B("M_Porto_Pietra");
            var n = B("M_Pietra_Conci");
            int steps = Mathf.Clamp(Mathf.RoundToInt((max.y - min.y) / 0.17f), 4, 20);
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps, t1 = (i + 1) / (float)steps;
                float x0 = Mathf.Lerp(min.x, max.x, t0), x1 = Mathf.Lerp(min.x, max.x, t1);
                float y1 = Mathf.Lerp(min.y, max.y, t1);
                g.Box(new Vector3(x0, min.y, min.z), new Vector3(x1, y1, max.z), bottom: false);
                n.Box(new Vector3(x0, y1 - 0.02f, min.z), new Vector3(x1, y1 + 0.012f, max.z));
            }
        }

        private static void CittaRoccia(Vector3 min, Vector3 max)
        {
            var g = B("M_Porto_PietraScura");
            g.Box(min, max);
            // strati e massi
            var s = B("M_Porto_Pietra");
            float y = min.y + 0.7f;
            for (int i = 0; i < 3 && y < max.y; i++, y += 1.2f)
                s.Box(new Vector3(min.x - 0.12f + i * 0.2f, y, min.z - 0.12f),
                      new Vector3(max.x + 0.12f - i * 0.2f, y + 0.22f, max.z + 0.12f));
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(min.x, max.x, (i + 0.5f) / 4f);
                float z = min.z - 0.6f - i * 0.3f;
                s.BoxR(new Vector3(x, min.y + 0.25f, z), new Vector3(0.8f + i * 0.2f, 0.5f, 0.7f), Quaternion.Euler(0, i * 33f, 0));
            }
        }

        private static void CittaArredo(BlockoutBox b, Vector3 min, Vector3 max)
        {
            string mat = b.kind switch
            {
                "wood" => "M_Porto_LegnoScuro",
                "steel" => "M_Porto_LamieraGrigio",
                "cover" => "M_Porto_MuroGrigio",
                "roof" => "M_Coppi",
                "objective" => "M_Porto_Pad",
                _ => "M_Porto_MuroGrigio"
            };
            var g = B(mat);
            g.Box(min, max);
            // bordo/rinforzo superiore
            var trim = B(b.kind == "wood" ? "M_Porto_LegnoChiaro" : "M_Porto_Acciaio");
            trim.Box(new Vector3(min.x - 0.02f, max.y - 0.06f, min.z - 0.02f),
                     new Vector3(max.x + 0.02f, max.y + 0.015f, max.z + 0.02f));
            if (b.kind == "wood")
            {
                for (float y = min.y + 0.25f; y < max.y - 0.1f; y += 0.35f)
                    trim.Box(new Vector3(min.x - 0.03f, y, min.z - 0.03f), new Vector3(max.x + 0.03f, y + 0.03f, min.z + 0.02f));
            }
        }

        private static void CittaPiazzola(Vector3 min, Vector3 max, string kind)
        {
            var pad = B("M_Porto_Pad");
            pad.Box(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y + 0.021f, max.z));
            var line = B(kind == "team_north" ? "M_Porto_Ciano" : "M_Porto_GialloSpento");
            float w = 0.055f;
            Vector3 a = new(min.x + 0.04f, min.y + 0.023f, min.z + 0.04f);
            Vector3 b = new(max.x - 0.04f, min.y + 0.023f, max.z - 0.04f);
            line.Box(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y + 0.004f, a.z + w));
            line.Box(new Vector3(a.x, a.y, b.z - w), new Vector3(b.x, a.y + 0.004f, b.z));
            line.Box(new Vector3(a.x, a.y, a.z), new Vector3(a.x + w, a.y + 0.004f, b.z));
            line.Box(new Vector3(b.x - w, a.y, a.z), new Vector3(b.x, a.y + 0.004f, b.z));
        }

        private static void CittaLampada(Facciata f, float x, float y)
        {
            var ferro = B("M_Porto_Ferro");
            ferro.Cylinder(f.P(x, y, 0.18f), f.P(x, y, 0.52f), 0.028f, 6);
            B("M_Porto_BiancoRotto").Box(f.P(x - 0.10f, y - 0.20f, 0.50f), f.P(x + 0.10f, y - 0.14f, 0.62f));
        }

        private static void CittaBalcone(Facciata f, float x, float y, float W)
        {
            var pietra = B("M_Porto_Pietra");
            float x0 = Mathf.Clamp(x - 1.2f, 0.4f, W - 2.4f);
            pietra.Box(f.P(x0, y, 0.02f), f.P(x0 + 2.2f, y + 0.14f, 0.55f));
            var ferro = B("M_Porto_Ferro");
            for (int i = 0; i <= 8; i++)
            {
                float bx = x0 + 0.12f + i * 0.25f;
                ferro.Cylinder(f.P(bx, y + 0.14f, 0.52f), f.P(bx, y + 0.95f, 0.52f), 0.018f, 5);
            }
            ferro.Cylinder(f.P(x0 + 0.10f, y + 0.95f, 0.52f), f.P(x0 + 2.1f, y + 0.95f, 0.52f), 0.022f, 5);
        }

        private static void CittaTenda(Facciata f, float x, float y, float w)
        {
            var t = B("M_Porto_TendaOcra");
            t.Quad(f.P(x - w * 0.5f, y + 0.2f, 0.05f), f.P(x + w * 0.5f, y + 0.2f, 0.05f),
                   f.P(x + w * 0.5f, y - 0.35f, 0.85f), f.P(x - w * 0.5f, y - 0.35f, 0.85f),
                   (f.Out + Vector3.down * 0.4f).normalized);
            var ferro = B("M_Porto_Ferro");
            ferro.Cylinder(f.P(x - w * 0.5f, y - 0.36f, 0.84f), f.P(x + w * 0.5f, y - 0.36f, 0.84f), 0.016f, 5);
        }

        private static void CittaAC(Vector3 min, Vector3 max, int seed)
        {
            var g = B("M_Porto_LamieraGrigio");
            float x = Mathf.Lerp(min.x, max.x, 0.25f + (seed % 3) * 0.2f);
            float z = Mathf.Lerp(min.z, max.z, 0.3f + (seed % 4) * 0.15f);
            g.Box(new Vector3(x, max.y + 0.25f, z), new Vector3(x + 1.25f, max.y + 1.05f, z + 0.85f));
            var ferro = B("M_Porto_Ferro");
            ferro.Box(new Vector3(x - 0.05f, max.y + 0.20f, z - 0.05f), new Vector3(x + 1.30f, max.y + 0.30f, z + 0.90f));
        }

        private static void CittaCiminiere(Vector3 min, Vector3 max, int seed)
        {
            var g = B("M_Porto_Pietra");
            float x = Mathf.Lerp(min.x, max.x, 0.25f + (seed % 3) * 0.2f);
            float z = Mathf.Lerp(min.z, max.z, 0.3f + (seed % 4) * 0.15f);
            g.Box(new Vector3(x, max.y, z), new Vector3(x + 0.55f, max.y + 1.1f, z + 0.55f));
            g.Box(new Vector3(x - 0.08f, max.y + 1.1f, z - 0.08f), new Vector3(x + 0.63f, max.y + 1.22f, z + 0.63f));
        }

        private static void CittaPoster(Facciata f, float W, float H, int seed)
        {
            var g = B("D_Stencil");
            float x = Mathf.Lerp(0.6f, W - 1.4f, (seed % 5) / 5f);
            float y = Mathf.Lerp(1.2f, Mathf.Max(1.4f, H - 2f), ((seed / 5) % 4) / 4f);
            g.QuadUv(f.P(x, y, 0.03f), f.P(x + 0.55f, y, 0.03f), f.P(x + 0.55f, y + 0.75f, 0.03f), f.P(x, y + 0.75f, 0.03f), f.Out);
        }

        private static void CittaCrepaMuro(Facciata f, float W, float H, int seed)
        {
            if (seed % 2 != 0) return;
            var g = B("D_Crepe");
            float x = 0.4f + (seed % 7) * 0.37f;
            x = Mathf.Clamp(x, 0.2f, Mathf.Max(0.3f, W - 0.8f));
            float y = 0.5f;
            g.QuadUv(f.P(x, y, 0.025f), f.P(x + 0.7f, y, 0.025f), f.P(x + 0.7f, y + 0.7f, 0.025f), f.P(x, y + 0.7f, 0.025f), f.Out);
        }

        private static string WallFor(string kind) => kind switch
        {
            "plaster" => "M_Porto_MuroAvorio",
            "plaster_ochre" => "M_Porto_MuroCrema",
            "stone" => "M_Porto_Pietra",
            "concrete" => "M_Porto_MuroCementizio",
            _ => "M_Porto_MuroGrigio"
        };

        private static string ShutterFor(string kind) => kind switch
        {
            "stone" or "concrete" => "M_Porto_PersianaArdesia",
            "plaster_ochre" => "M_Porto_PersianaSalvia",
            _ => "M_Porto_PersianaVerde"
        };

        private static string DoorFor(string kind) => kind == "stone" ? "M_Porto_LegnoVerniciato" : "M_Porto_LegnoScuro";

        private static void CittaDettagli(List<BlockoutBox> all)
        {
            // tombini con crepe su tutta la mappa (lontani dagli edifici)
            var spots = new List<Vector3>();
            for (int i = 0; i < 34; i++)
            {
                float x = Mathf.Lerp(-78f, 78f, ((i * 37) % 101) / 100f);
                float z = Mathf.Lerp(-95f, 95f, ((i * 61 + 13) % 103) / 102f);
                var p = new Vector3(x, 0, z);
                bool inside = all.Any(o => !o.kind.StartsWith("street") &&
                    p.x > o.importedMin.x - 0.8f && p.x < o.importedMin.x + o.importedSize.x + 0.8f &&
                    p.z > o.importedMin.z - 0.8f && p.z < o.importedMin.z + o.importedSize.z + 0.8f);
                if (inside) continue;
                spots.Add(p);
            }
            for (int i = 0; i < spots.Count; i++)
            {
                Tombino(spots[i], i * 47f);
                if (i % 3 == 0) Segno("D_Olio", spots[i] + new Vector3(2.1f, 0.010f, 0.7f), Vector3.forward, 1.6f, 1.2f, i * 13f);
                if (i % 4 == 0) Segno("D_Pozza", spots[i] + new Vector3(-2.4f, 0.012f, -0.9f), Vector3.forward, 2.1f, 1.5f, i * 31f);
            }

            // erbacce ai bordi dei marciapiedi
            var erba = B("M_Porto_VerdeLanterna");
            for (int i = 0; i < 70; i++)
            {
                float x = Mathf.Lerp(-80f, 80f, ((i * 29) % 97) / 96f);
                float z = Mathf.Lerp(-96f, 96f, ((i * 43 + 7) % 89) / 88f);
                var p = new Vector3(x, 0, z);
                if (all.Any(o => p.x > o.importedMin.x - 0.3f && p.x < o.importedMin.x + o.importedSize.x + 0.3f &&
                                 p.z > o.importedMin.z - 0.3f && p.z < o.importedMin.z + o.importedSize.z + 0.3f &&
                                 o.importedSize.y > 1.2f)) continue;
                for (int k = 0; k < 4; k++)
                {
                    var q = p + new Vector3((k - 1.5f) * 0.11f, 0, (i % 3 - 1) * 0.09f);
                    erba.Tri(q, q + new Vector3(0.05f, 0.24f, 0.02f), q + new Vector3(-0.04f, 0.20f, 0.05f), Vector3.up);
                }
            }

            // porto: tombini con crepe anche qui
            Tombino(new Vector3(6.2f, 0, -89.8f), 17f);
            Tombino(new Vector3(-8.4f, 0, -94.6f), 71f);
            Tombino(new Vector3(13.6f, 0, -95.6f), 133f);
        }

        private static void Tombino(Vector3 p, float rot)
        {
            var pietra = B("M_Porto_Pietra");
            var ferro = B("M_Porto_Ferro");
            var acciaio = B("M_Porto_Acciaio");
            var up = Vector3.up;
            pietra.Disc(p + up * 0.004f, up, 0.50f, 20);
            ferro.Disc(p + up * 0.006f, up, 0.42f, 24);
            acciaio.Cylinder(p + up * 0.004f, p + up * 0.040f, 0.35f, 24, true);
            ferro.Disc(p + up * 0.041f, up, 0.35f, 24, 0.30f);
            // feritoie radiali
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var t = new Vector3(-d.z, 0, d.x);
                acciaio.BoxR(p + up * 0.045f + d * 0.16f, new Vector3(0.05f, 0.012f, 0.16f), Quaternion.LookRotation(t, up));
            }
            acciaio.Cylinder(p + up * 0.041f, p + up * 0.048f, 0.07f, 12, true);
            Segno("D_Crepe", p + up * 0.003f, Vector3.right, 1.85f, 1.85f, rot);
        }
    }
}
