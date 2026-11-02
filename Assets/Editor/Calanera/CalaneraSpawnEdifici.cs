using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Le facciate dello spawn Nord: magazzini sul mare, case del piazzale, Capitaneria, Dogana.
    /// Ogni facciata è un "vestito" sulla scatola del blockout: il muro sta 2 cm fuori dalla
    /// faccia, e la joinery (cornici, vetri, persiane, davanzali) è applicata a filo muro, mai
    /// incassata: il corpo dell'edificio è una scatola chiusa e nasconderebbe il vano. I rilievi
    /// sotto i 3 m non superano i 12 cm (doc 30, regola 2).
    /// </summary>
    public static partial class CalaneraSpawn
    {
        // ------------------------------------------------------------------ facciata
        private const float Fuori = 0.02f;     // quanto il muro sporge dalla scatola
        private static BlockoutBox[] Boxes = System.Array.Empty<BlockoutBox>();

        /// <summary>
        /// Profondità del corpo dietro una facciata: la scatola dell'edificio, non la strada.
        /// Deve essere un edificio vero, con la facciata appoggiata a una sua faccia.
        /// </summary>
        private static float ProfonditaDietro(Vector3 origine, Vector3 outDir, Vector3 right, float width)
        {
            float best = 8f, bestDist = float.MaxValue;
            foreach (var b in Boxes)
            {
                if (!BuildingKinds.Contains(b.kind)) continue;
                var mn = b.importedMin;
                var mx = mn + b.importedSize;
                var size = mx - mn;
                if (size.x < 2.2f || size.z < 2.2f || size.y < 2.4f) continue;

                // La facciata deve stare su una faccia E dentro la scatola sull'altro asse:
                // altrimenti si prende l'edificio accanto e il corpo finisce nel piazzale.
                float a0, a1, b0, b1;
                if (Mathf.Abs(outDir.x) > 0.5f)
                { a0 = Mathf.Min(origine.z, origine.z + right.z * width); a1 = Mathf.Max(origine.z, origine.z + right.z * width); b0 = mn.z; b1 = mx.z; }
                else
                { a0 = Mathf.Min(origine.x, origine.x + right.x * width); a1 = Mathf.Max(origine.x, origine.x + right.x * width); b0 = mn.x; b1 = mx.x; }
                if (a0 < b0 - 0.3f || a1 > b1 + 0.3f) continue;

                float dist = Mathf.Abs(outDir.x) > 0.5f
                    ? Mathf.Abs((outDir.x > 0f ? mx.x : mn.x) - origine.x)
                    : Mathf.Abs((outDir.z > 0f ? mx.z : mn.z) - origine.z);
                if (dist > 0.75f) continue;

                float depth = Mathf.Abs(Vector3.Dot(size, outDir));
                if (depth > 2f && dist < bestDist) { bestDist = dist; best = depth; }
            }
            return best;
        }

        private sealed class Facciata
        {
            public readonly Vector3 O, R, Out;
            public readonly float W, H;
            public readonly string Muro, Persiana;
            private readonly List<Rect> finestre = new();

            public Facciata(Vector3 origine, Vector3 right, float width, float height, string muro, string persiana)
            {
                R = right.normalized;
                Out = Vector3.Cross(R, Vector3.up).normalized;
                O = origine + Out * Fuori;
                W = width; H = height; Muro = muro; Persiana = persiana;
            }

            public Vector3 P(float x, float y, float d = 0f) => O + R * x + Vector3.up * y + Out * d;

            /// <summary>Pannello pieno in coordinate facciata (d0..d1 = sporgenza).</summary>
            public void BoxF(Geo g, float x0, float x1, float y0, float y1, float d0, float d1)
            {
                if (x1 < x0) (x0, x1) = (x1, x0);
                if (y1 < y0) (y0, y1) = (y1, y0);
                if (d1 < d0) (d0, d1) = (d1, d0);
                bool q = g.Quiet; g.Quiet = true;
                g.Quad(P(x0, y1, d1), P(x1, y1, d1), P(x1, y0, d1), P(x0, y0, d1), Out);
                g.Quad(P(x1, y1, d0), P(x0, y1, d0), P(x0, y0, d0), P(x1, y0, d0), -Out);
                g.Quad(P(x1, y1, d1), P(x1, y1, d0), P(x1, y0, d0), P(x1, y0, d1), R);
                g.Quad(P(x0, y1, d0), P(x0, y1, d1), P(x0, y0, d1), P(x0, y0, d0), -R);
                g.Quad(P(x0, y1, d0), P(x1, y1, d0), P(x1, y1, d1), P(x0, y1, d1), Vector3.up);
                g.Quad(P(x0, y0, d1), P(x1, y0, d1), P(x1, y0, d0), P(x0, y0, d0), Vector3.down);
                g.Quiet = q;
                Vector3 lo = Vector3.Min(Vector3.Min(Vector3.Min(P(x0,y0,d0), P(x1,y0,d0)), Vector3.Min(P(x0,y1,d0), P(x1,y1,d0))),
                                         Vector3.Min(Vector3.Min(P(x0,y0,d1), P(x1,y0,d1)), Vector3.Min(P(x0,y1,d1), P(x1,y1,d1))));
                Vector3 hi = Vector3.Max(Vector3.Max(Vector3.Max(P(x0,y0,d0), P(x1,y0,d0)), Vector3.Max(P(x0,y1,d0), P(x1,y1,d0))),
                                         Vector3.Max(Vector3.Max(P(x0,y0,d1), P(x1,y0,d1)), Vector3.Max(P(x0,y1,d1), P(x1,y1,d1))));
                g.Solid(lo, hi);
            }

            public void AggiungiFinestra(float cx, float y, float w = 1.10f, float h = 1.45f)
            {
                float x0 = cx - w * 0.5f;
                finestre.Add(new Rect(x0, y, w, h));
            }

            /// <summary>
            /// Chiude l'edificio dietro la facciata: parete di fondo, lati, tetto e pavimento.
            /// Senza questo, una facciata è un piano e da dietro si vede l'interno e la joinery
            /// sospesa. La profondità arriva dalla scatola del blockout.
            /// </summary>
            public void Corpo(float depth = -1f)
            {
                if (depth <= 0f) depth = ProfonditaDietro(O - Out * Fuori, Out, R, W);
                if (depth <= 0f)
                {
                    Debug.LogWarning("Corpo: nessuna scatola per la facciata in " + O.ToString("F1") + " — corpo saltato");
                    return;
                }
                float d = Mathf.Max(2f, depth);
                var g = B(Muro);
                g.Quad(P(W, H, -d), P(0, H, -d), P(0, 0, -d), P(W, 0, -d), -Out);
                g.Quad(P(0, H, 0), P(0, H, -d), P(0, 0, -d), P(0, 0, 0), -R);
                g.Quad(P(W, H, -d), P(W, H, 0), P(W, 0, 0), P(W, 0, -d), R);
                g.Quad(P(0, H, -d), P(W, H, -d), P(W, H, 0), P(0, H, 0), Vector3.up);
                g.Quad(P(0, 0, 0), P(W, 0, 0), P(W, 0, -d), P(0, 0, -d), Vector3.down);
            }

            /// <summary>
            /// Muro pieno e joinery APPLICATA. Il corpo dell'edificio è una scatola chiusa,
            /// quindi un vano incassato sarebbe invisibile (era il difetto delle cornici
            /// sospese). Cornici, vetri e persiane stanno a filo muro, entro 12 cm, e non
            /// escono mai dall'angolo della facciata.
            /// </summary>
            public void Muri()
            {
                var muro = B(Muro);
                var pietra = B("M_Porto_Pietra");
                var legno = B("M_Porto_BiancoRotto");
                var ferro = B("M_Porto_Ferro");
                var persiana = B(Persiana);
                var ombra = B("M_Porto_NeroFumo");

                // doppia faccia: le case del porto non hanno un corpo chiuso, e da dietro
                // si vedevano le cornici sospese nel vuoto (il muro spariva per il culling).
                muro.Quad(P(0, H), P(W, H), P(W, 0), P(0, 0), Out);
                muro.Quad(P(0, 0), P(W, 0), P(W, H), P(0, H), -Out);

                foreach (var f in finestre)
                {
                    float x0 = f.xMin, x1 = f.xMax, y0 = f.yMin, y1 = f.yMax;

                    // filo scuro appena più grande del vetro: dà profondità senza bucare
                    BoxF(ombra, x0 - 0.015f, x1 + 0.015f, y0 - 0.015f, y1 + 0.015f, 0.004f, 0.006f);

                    var v = B("M_Porto_VetroScuro");
                    v.Quad(P(x0 + 0.01f, y1 - 0.01f, 0.008f), P(x1 - 0.01f, y1 - 0.01f, 0.008f),
                           P(x1 - 0.01f, y0 + 0.01f, 0.008f), P(x0 + 0.01f, y0 + 0.01f, 0.008f), Out);

                    BoxTelaio(legno, x0, x1, y0, y1, 0.055f);

                    // davanzale in pietra con gocciolatoio, e architrave
                    BoxF(pietra, x0 - 0.10f, x1 + 0.10f, y0 - 0.08f, y0 - 0.01f, 0f, 0.12f);
                    BoxF(pietra, x0 - 0.08f, x1 + 0.08f, y0 - 0.10f, y0 - 0.08f, 0.02f, 0.09f);
                    BoxF(pietra, x0 - 0.07f, x1 + 0.07f, y1 + 0.01f, y1 + 0.10f, 0f, 0.055f);

                    float sw = Mathf.Clamp((x1 - x0) * 0.5f - 0.06f, 0.18f, 0.55f);
                    bool fits = x0 - sw - 0.035f >= 0.02f && x1 + sw + 0.035f <= W - 0.02f;
                    bool chiusa = !fits || ((int)(Mathf.Abs(x0) * 13f + y0 * 7f) % 10) < 6;
                    if (chiusa) PersianaChiusa(persiana, ferro, x0, x1, y0, y1);
                    else PersianaAperta(persiana, ferro, x0, x1, y0, y1, sw);
                }

                Corpo();
            }

            private void BoxTelaio(Geo g, float x0, float x1, float y0, float y1, float w)
            {
                float d0 = 0.010f, d1 = 0.055f;
                BoxF(g, x0, x0 + w, y0, y1, d0, d1);
                BoxF(g, x1 - w, x1, y0, y1, d0, d1);
                BoxF(g, x0 + w, x1 - w, y0, y0 + w, d0, d1);
                BoxF(g, x0 + w, x1 - w, y1 - w, y1, d0, d1);
            }

            private void PersianaChiusa(Geo g, Geo ferro, float x0, float x1, float y0, float y1)
            {
                float mid = (x0 + x1) * 0.5f;
                float d0 = 0.058f, d1 = 0.086f;
                for (int k = 0; k < 2; k++)
                {
                    float a = k == 0 ? x0 + 0.015f : mid + 0.004f;
                    float b = k == 0 ? mid - 0.004f : x1 - 0.015f;
                    BoxF(g, a, b, y0 + 0.02f, y1 - 0.02f, d0, d1);
                    for (int i = 1; i < 6; i++)
                    {
                        float y = Mathf.Lerp(y0 + 0.10f, y1 - 0.10f, i / 6f);
                        BoxF(g, a + 0.025f, b - 0.025f, y - 0.018f, y + 0.018f, d1, d1 + 0.012f);
                    }
                }
                BoxF(ferro, mid - 0.025f, mid + 0.025f, y0 + 0.05f, y1 - 0.05f, d1, d1 + 0.014f);
            }

            private void PersianaAperta(Geo g, Geo ferro, float x0, float x1, float y0, float y1, float w)
            {
                float d0 = 0.058f, d1 = 0.086f;
                for (int k = 0; k < 2; k++)
                {
                    // l'anta aperta sta di fianco, sempre dentro la facciata
                    float a = k == 0 ? x0 - w - 0.035f : x1 + 0.035f;
                    a = Mathf.Clamp(a, 0.02f, W - w - 0.02f);
                    float b = a + w;
                    BoxF(g, a, b, y0 + 0.02f, y1 - 0.02f, d0, d1);
                    for (int i = 1; i < 6; i++)
                    {
                        float y = Mathf.Lerp(y0 + 0.10f, y1 - 0.10f, i / 6f);
                        BoxF(g, a + 0.025f, b - 0.025f, y - 0.018f, y + 0.018f, d1, d1 + 0.012f);
                    }
                    float hx = k == 0 ? b - 0.05f : a + 0.05f;
                    foreach (float y in new[] { y0 + 0.15f, y1 - 0.15f })
                        BoxF(ferro, hx - 0.03f, hx + 0.03f, y - 0.04f, y + 0.04f, d1, d1 + 0.014f);
                }
            }

            /// <summary>Porta in legno, cornice in pietra, gradino: tutto applicato a filo muro.</summary>
            public void Porta(float cx, float y, float w, float h, string legnoKey = "M_Porto_LegnoScuro")
            {
                float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f;
                var pietra = B("M_Porto_Pietra");
                var legno = B(legnoKey);
                var ferro = B("M_Porto_Ferro");
                BoxF(pietra, x0 - 0.13f, x0, y, y + h + 0.13f, 0f, 0.055f);
                BoxF(pietra, x1, x1 + 0.13f, y, y + h + 0.13f, 0f, 0.055f);
                BoxF(pietra, x0 - 0.13f, x1 + 0.13f, y + h, y + h + 0.13f, 0f, 0.05f);
                BoxF(pietra, x0 - 0.10f, x1 + 0.10f, y - 0.02f, y + 0.035f, 0f, 0.20f);
                BoxF(legno, x0 + 0.02f, x1 - 0.02f, y + 0.02f, y + h - 0.01f, 0.02f, 0.065f);
                // specchiature
                float ix0 = x0 + 0.14f, ix1 = x1 - 0.14f;
                foreach (var (ya, yb) in new[] { (y + 0.20f, y + h * 0.46f), (y + h * 0.54f, y + h - 0.20f) })
                    BoxF(legno, ix0, ix1, ya, yb, 0.065f, 0.078f);
                ferro.Cylinder(P(x1 - 0.20f, y + 1.05f, 0.07f), P(x1 - 0.20f, y + 1.05f, 0.16f), 0.022f, 8);
                // numero civico
                var ottone = B("M_Porto_Ottone");
                BoxF(ottone, x1 + 0.15f, x1 + 0.29f, y + 1.55f, y + 1.75f, 0.02f, 0.035f);
            }

            /// <summary>Vetrina con zoccolo, telaio e vetro: applicata, non incassata.</summary>
            public void Vetrina(float cx, float y, float w, float h, float zoccolo = 0.55f)
            {
                float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f;
                var legno = B("M_Porto_LegnoVerniciato");
                var vetro = B("M_Porto_Vetro");
                BoxF(legno, x0 - 0.08f, x1 + 0.08f, y, y + zoccolo, 0f, 0.07f);
                BoxF(legno, x0 - 0.08f, x0, y, y + h, 0f, 0.07f);
                BoxF(legno, x1, x1 + 0.08f, y, y + h, 0f, 0.07f);
                BoxF(legno, x0 - 0.08f, x1 + 0.08f, y + h, y + h + 0.10f, 0f, 0.07f);
                BoxF(legno, x0, x1, y + zoccolo, y + zoccolo + 0.06f, 0f, 0.06f);
                vetro.Quad(P(x0 + 0.03f, y + h - 0.02f, 0.045f), P(x1 - 0.03f, y + h - 0.02f, 0.045f),
                           P(x1 - 0.03f, y + zoccolo + 0.07f, 0.045f), P(x0 + 0.03f, y + zoccolo + 0.07f, 0.045f), Out);
                // traversa
                BoxF(legno, x0, x1, y + zoccolo + 0.55f, y + zoccolo + 0.60f, 0f, 0.075f);
            }

            /// <summary>Saracinesca in lamiera, appoggiata alla facciata.</summary>
            public void Serranda(float cx, float y, float w, float h)
            {
                float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f;
                var lamiera = B("M_Porto_LamieraGrigio");
                var ferro = B("M_Porto_Ferro");
                BoxF(lamiera, x0, x1, y, y + h, 0.02f, 0.07f);
                for (int i = 0; i < Mathf.RoundToInt(h / 0.18f); i++)
                {
                    float y0 = y + i * 0.18f;
                    BoxF(lamiera, x0 + 0.02f, x1 - 0.02f, y0, y0 + 0.10f, 0.07f, 0.095f);
                }
                BoxF(ferro, x0 - 0.10f, x0, y, y + h + 0.10f, 0.02f, 0.09f);
                BoxF(ferro, x1, x1 + 0.10f, y, y + h + 0.10f, 0.02f, 0.09f);
                BoxF(ferro, x0 - 0.10f, x1 + 0.10f, y + h, y + h + 0.10f, 0.02f, 0.10f);
            }

            /// <summary>Tenda a strisce con mantovana, appoggiata alla facciata.</summary>
            public void Tenda(float cx, float w, float y, float sporgenza = 1.35f)
            {
                int strisce = Mathf.Max(4, Mathf.RoundToInt(w / 0.30f));
                float passo = w / strisce;
                var a = B("M_Porto_TendaOcra");
                var b = B("M_Porto_TendaRuggine");
                for (int i = 0; i < strisce; i++)
                {
                    var g = i % 2 == 0 ? a : b;
                    float x0 = cx - w * 0.5f + i * passo, x1 = x0 + passo;
                    float yWall = y, yMid = y - 0.10f, yOut = y - 0.26f;
                    // due falde (parete→metà, metà→bordo) per la curva
                    Vector3 nTop = (Vector3.up + Out * 0.22f).normalized;
                    g.Quad(P(x0, yWall, 0.03f), P(x1, yWall, 0.03f), P(x1, yMid, sporgenza * 0.5f), P(x0, yMid, sporgenza * 0.5f), nTop);
                    g.Quad(P(x0, yMid, sporgenza * 0.5f), P(x1, yMid, sporgenza * 0.5f), P(x1, yOut, sporgenza), P(x0, yOut, sporgenza), nTop);
                    // sotto, appena più in basso, così si vede anche da terra
                    Vector3 t = Vector3.down * 0.012f;
                    g.Quad(P(x0, yWall, 0.03f) + t, P(x1, yWall, 0.03f) + t, P(x1, yMid, sporgenza * 0.5f) + t, P(x0, yMid, sporgenza * 0.5f) + t, -nTop);
                    g.Quad(P(x0, yMid, sporgenza * 0.5f) + t, P(x1, yMid, sporgenza * 0.5f) + t, P(x1, yOut, sporgenza) + t, P(x0, yOut, sporgenza) + t, -nTop);
                    // mantovana
                    g.Quad(P(x0, yOut, sporgenza), P(x1, yOut, sporgenza), P(x1, yOut - 0.20f, sporgenza), P(x0, yOut - 0.20f, sporgenza), Out);
                }
                var ferro = B("M_Porto_Ferro");
                foreach (float x in new[] { cx - w * 0.5f + 0.06f, cx + w * 0.5f - 0.06f })
                    ferro.Cylinder(P(x, y + 0.06f, 0.02f), P(x, y - 0.24f, sporgenza - 0.02f), 0.014f, 6);
            }

            /// <summary>Insegna dipinta direttamente sul muro (lettere ritagliate).</summary>
            public void Insegna(string texKey, float cx, float y, float h, bool tavola)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexSegni}/{Decal[texKey]}.png");
                if (tex == null) return;
                float w = h * tex.width / tex.height;
                float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f;
                var ferro = B("M_Porto_Ferro");
                if (tavola)
                {
                    var legno = B("M_Porto_LegnoScuro");
                    BoxF(legno, x0 - 0.10f, x1 + 0.10f, y - 0.09f, y + h + 0.09f, 0.02f, 0.06f);
                    BoxF(legno, x0 - 0.10f, x1 + 0.10f, y - 0.09f, y + h + 0.09f, 0.06f, 0.062f);
                    foreach (float x in new[] { x0 - 0.06f, x1 + 0.06f })
                        ferro.Cylinder(P(x, y + 0.02f, 0.05f), P(x, y - 0.28f, 0.05f), 0.012f, 5);
                }
                var g = B(texKey);
                float dd = tavola ? 0.066f : 0.028f;
                Vector3 vr = Vector3.Cross(Vector3.up, -Out);
                bool flip = Vector3.Dot(R, vr) < 0f;
                Vector3 A = flip ? P(x1, y, dd) : P(x0, y, dd);
                Vector3 Bp = flip ? P(x0, y, dd) : P(x1, y, dd);
                g.QuadUv(A, Bp, Bp + Vector3.up * h, A + Vector3.up * h, Out);
            }

            public void Balcone(float cx, float y, float w)
            {
                var pietra = B("M_Porto_Pietra");
                var ferro = B("M_Porto_Ferro");
                // il balcone non tocca mai gli angoli e non copre le finestre di bordo
                float bw = Mathf.Clamp(w - 0.9f, 1.6f, W - 0.9f);
                float x0 = Mathf.Clamp(cx - bw * 0.5f, 0.45f, W - bw - 0.45f);
                float x1 = x0 + bw;
                cx = (x0 + x1) * 0.5f;
                BoxF(pietra, x0, x1, y, y + 0.13f, 0f, 0.55f);
                foreach (float t in new[] { -0.35f, 0f, 0.35f })
                    BoxF(pietra, cx + t * bw - 0.07f, cx + t * bw + 0.07f, y - 0.26f, y, 0f, 0.42f);
                float hi = y + 1.06f;
                for (float x = x0 + 0.06f; x <= x1 - 0.05f; x += 0.13f)
                    BoxF(ferro, x, x + 0.018f, y + 0.13f, hi, 0.50f, 0.53f);
                BoxF(ferro, x0, x1, hi, hi + 0.05f, 0.47f, 0.56f);
                foreach (float x in new[] { x0 + 0.02f, x1 - 0.09f })
                    for (float d = 0.06f; d <= 0.50f; d += 0.13f)
                        BoxF(ferro, x, x + 0.018f, y + 0.13f, hi, d, d + 0.03f);
            }

            public void Pluviale(float x)
            {
                var ferro = B("M_Porto_Ferro");
                ferro.Cylinder(P(x, 0.15f, 0.10f), P(x, H - 0.35f, 0.10f), 0.045f, 8);
                for (float y = 1.2f; y < H - 0.6f; y += 1.8f)
                    BoxF(ferro, x - 0.09f, x + 0.09f, y - 0.05f, y + 0.05f, 0.02f, 0.10f);
            }

            /// <summary>Cornicione a due risalti.</summary>
            public void Cornicione()
            {
                var pietra = B("M_Porto_Pietra");
                BoxF(pietra, -0.10f, W + 0.10f, H - 0.40f, H - 0.24f, 0f, 0.16f);
                BoxF(pietra, -0.16f, W + 0.16f, H - 0.24f, H - 0.10f, 0f, 0.26f);
            }

            /// <summary>Tetto a due falde sul lotto (profondità nota dal blockout), con comignolo.</summary>
            public void Tetto(float profondita, bool camino = true)
            {
                float dFront = 0.50f, dBack = -profondita - 0.35f;
                Vector3 a0 = P(-0.35f, H, dFront), a1 = P(W + 0.35f, H, dFront);
                Vector3 b0 = P(-0.35f, H, dBack), b1 = P(W + 0.35f, H, dBack);
                float dMid = (dFront + dBack) * 0.5f;
                float half = (dFront - dBack) * 0.5f;
                float rise = Mathf.Tan(22f * Mathf.Deg2Rad) * half;
                Vector3 r0 = P(-0.35f, H + rise, dMid), r1 = P(W + 0.35f, H + rise, dMid);
                var coppi = B("M_Coppi");
                Vector3 nf = (Vector3.up + Out * Mathf.Tan(22f * Mathf.Deg2Rad)).normalized;
                coppi.Quad(a0, a1, r1, r0, nf);
                coppi.Quad(r0, r1, b1, b0, (Vector3.up - Out * Mathf.Tan(22f * Mathf.Deg2Rad)).normalized);
                // fasce di gronda
                var legno = B("M_Porto_LegnoScuro");
                BoxF(legno, -0.36f, W + 0.36f, H - 0.14f, H + 0.02f, dFront - 0.06f, dFront + 0.02f);
                // soffitto di gronda: collega la falda al corpo, niente tetto sospeso
                BoxF(legno, -0.30f, W + 0.30f, H - 0.16f, H - 0.02f, dFront - 0.55f, dFront + 0.01f);
                B(Muro).Quad(P(0, H, dFront), P(W, H, dFront), P(W, H, dBack), P(0, H, dBack), Vector3.up);
                // colmo
                coppi.Box(new Vector3(r0.x - 0.12f, r0.y - 0.02f, r0.z - 0.12f), new Vector3(r1.x + 0.12f, r1.y + 0.12f, r1.z + 0.12f));
                // timpani
                var muro = B(Muro);
                muro.Tri(P(0, H, dFront - 0.02f), P(0, H, dBack + 0.02f), P(0, H + rise, dMid), -R);
                muro.Tri(P(W, H, dBack + 0.02f), P(W, H, dFront - 0.02f), P(W, H + rise, dMid), R);
                // timpani semplici in muratura: niente oculo/travi decorative sospese
                if (!camino) return;
                // comignolo sul colmo
                float cx = Mathf.Lerp(1.0f, W - 1.0f, Mathf.Clamp((W * 0.37f) / Mathf.Max(W, 1f), 0f, 1f));
                Vector3 c = P(cx, H + rise - 0.25f, dMid);
                var pietra = B("M_Porto_Pietra");
                pietra.Box(new Vector3(c.x - 0.28f, c.y, c.z - 0.28f), new Vector3(c.x + 0.28f, c.y + 1.15f, c.z + 0.28f));
                pietra.Box(new Vector3(c.x - 0.36f, c.y + 1.15f, c.z - 0.36f), new Vector3(c.x + 0.36f, c.y + 1.26f, c.z + 0.36f));
                pietra.Box(new Vector3(c.x - 0.12f, c.y + 1.26f, c.z - 0.12f), new Vector3(c.x + 0.12f, c.y + 1.44f, c.z + 0.12f));
            }
        }

        // ------------------------------------------------------------------ i magazzini sul mare
        private static void Magazzini()
        {
            Magazzino(-20f, 10f, 6.2f, "M_Porto_LamieraVerde", "M_Porto_LamieraGrigio", 3.0f, new[] { 4.55f }, 4);
            Magazzino(-10f, 10f, 11.8f, "M_Porto_LamieraRuggine", "M_Porto_LamieraVerde", 2.9f, new[] { 4.55f, 7.15f, 9.75f }, 4);
            Magazzino(0f, 10f, 11.8f, "M_Porto_LamieraGrigio", "M_Porto_LamieraRuggine", 3.0f, new[] { 4.55f, 7.15f, 9.75f }, 4);
            Magazzino(10f, 10f, 6.2f, "M_Porto_LamieraVerde", "M_Porto_LamieraGrigio", 3.0f, new[] { 4.55f }, 4);

            // insegna FALEZIA dipinta sulle lettere di ferro, sopra i due magazzini alti
            var insegna = new Facciata(new Vector3(-10, 0, -99), Vector3.right, 20f, 11.8f,
                                        "M_Porto_MuroCementizio", "M_Porto_PersianaVerde");
            InsegnaNeon(insegna, 10f, 11.86f);
        }

        private static void Magazzino(float x0, float W, float H, string lamiera, string portaMat, float portaX,
                                      float[] righe, int finestrePerRiga)
        {
            var f = new Facciata(new Vector3(x0, 0, -99f), Vector3.right, W, H, "M_Porto_MuroCementizio", "M_Porto_PersianaVerde");
            var g = B(lamiera);
            var porta = B(portaMat);
            var acciaio = B("M_Porto_Acciaio");
            var pietra = B("M_Porto_PietraScura");
            var ferro = B("M_Porto_Ferro");

            // zoccolo di cemento
            f.BoxF(pietra, 0f, W, 0f, 0.60f, 0f, 0.045f);
            // lamiera grecata: pannello pieno, il rilievo lo fa la normal map (niente raggiungere il vuoto)
            f.BoxF(g, 0.0f, W, 0.60f, H - 0.40f, -0.06f, 0.0f);

            // pilastri a I
            foreach (float x in new[] { 0.08f, W * 0.5f, W - 0.28f })
            {
                f.BoxF(ferro, x, x + 0.24f, 0.60f, H - 0.30f, 0f, 0.20f);
                f.BoxF(ferro, x - 0.05f, x + 0.29f, 0.60f, H - 0.30f, 0.20f, 0.26f);
                f.BoxF(ferro, x - 0.05f, x + 0.29f, 0.60f, 0.85f, 0f, 0.26f);
            }

            // porta scorrevole, appesa fuori dalla lamiera
            float px0 = portaX - 2.1f, px1 = portaX + 2.1f;
            f.BoxF(porta, px0, px1, 0.22f, 3.55f, 0.04f, 0.10f);
            for (float x = px0 + 0.02f; x < px1 - 0.05f; x += 0.30f)
                f.BoxF(porta, x, x + 0.16f, 0.25f, 3.52f, 0.10f, 0.135f);
            // telaio scuro del portone
            f.BoxF(acciaio, px0 - 0.18f, px0, 0f, 3.75f, 0.04f, 0.16f);
            f.BoxF(acciaio, px1, px1 + 0.18f, 0f, 3.75f, 0.04f, 0.16f);
            f.BoxF(acciaio, px0 - 0.18f, px1 + 0.18f, 3.55f, 3.75f, 0.04f, 0.20f);
            f.BoxF(acciaio, px0 - 0.10f, px1 + 0.10f, 3.55f, 3.62f, 0.20f, 0.30f);
            foreach (float x in new[] { px0 + 0.3f, px1 - 0.3f })
                ferro.Cylinder(f.P(x, 3.58f, 0.16f), f.P(x, 3.05f, 0.16f), 0.028f, 6);
            // fascia ocra consumata alla base del portone
            f.BoxF(B("M_Porto_GialloSpento"), px0 + 0.02f, px1 - 0.02f, 0.22f, 0.50f, 0.10f, 0.137f);
            // maniglia e serratura
            f.BoxF(ferro, px1 - 0.55f, px1 - 0.45f, 1.10f, 1.45f, 0.14f, 0.19f);

            // portoncino pedonale accanto
            float dx = portaX > W * 0.5f ? 0.9f : W - 1.9f;
            f.BoxF(porta, dx, dx + 0.95f, 0.02f, 2.20f, 0.04f, 0.10f);
            f.BoxF(ferro, dx - 0.06f, dx, 0f, 2.26f, 0.04f, 0.12f);
            f.BoxF(ferro, dx + 0.95f, dx + 1.01f, 0f, 2.26f, 0.04f, 0.12f);

            // finestre alte con inferriate
            for (int r = 0; r < righe.Length; r++)
                for (int i = 0; i < finestrePerRiga; i++)
                {
                    float cx = Mathf.Lerp(1.6f, W - 1.6f, finestrePerRiga == 1 ? 0.5f : i / (float)(finestrePerRiga - 1));
                    float y = righe[r];
                    float w = 1.15f, h = 1.05f;
                    f.BoxF(pietra, cx - w * 0.5f, cx + w * 0.5f, y - 0.07f, y, 0f, 0.06f);
                    f.BoxF(B("M_Porto_Vetro"), cx - w * 0.5f, cx + w * 0.5f, y, y + h, 0.01f, 0.025f);
                    for (float x = cx - w * 0.5f; x <= cx + w * 0.5f + 0.01f; x += 0.20f)
                        f.BoxF(ferro, x - 0.014f, x + 0.014f, y, y + h, 0.025f, 0.06f);
                    f.BoxF(ferro, cx - w * 0.5f, cx + w * 0.5f, y, y + 0.03f, 0.02f, 0.06f);
                    f.BoxF(ferro, cx - w * 0.5f, cx + w * 0.5f, y + h - 0.03f, y + h, 0.02f, 0.06f);
                    f.BoxF(ferro, cx - w * 0.5f - 0.05f, cx - w * 0.5f, y - 0.02f, y + h + 0.02f, 0.02f, 0.06f);
                    f.BoxF(ferro, cx + w * 0.5f, cx + w * 0.5f + 0.05f, y - 0.02f, y + h + 0.02f, 0.02f, 0.06f);
                }

            // lampada di servizio sopra il portone
            f.BoxF(ferro, portaX - 0.05f, portaX + 0.05f, 4.05f, 4.15f, 0f, 0.55f);
            var lampada = B("M_Porto_Ferro");
            lampada.Cylinder(f.P(portaX, 4.02f, 0.50f), f.P(portaX, 3.86f, 0.50f), 0.12f, 8);
            B("M_Porto_BiancoRotto").Disc(f.P(portaX, 3.85f, 0.50f), Vector3.down, 0.10f, 10);

            // cornicione e tetto basso
            f.BoxF(pietra, -0.10f, W + 0.10f, H - 0.40f, H - 0.16f, 0f, 0.16f);
            f.BoxF(g, -0.10f, W + 0.10f, H - 0.16f, H, 0f, 0.04f);
            B("M_Porto_LamieraGrigio").Quad(f.P(-0.2f, H + 0.02f, 0.2f), f.P(W + 0.2f, H + 0.02f, 0.2f),
                                             f.P(W + 0.2f, H + 0.02f, -1.2f), f.P(-0.2f, H + 0.02f, -1.2f), Vector3.up);
            f.Corpo();
            // ruggine sotto il cornicione
            for (int i = 0; i < 5; i++)
                Ruggine(f.P(1.2f + i * 1.9f, H - 0.65f, 0.008f), f.Out, 0.34f, 0.55f);
        }

        // ------------------------------------------------------------------ le case del piazzale
        private static void Case()
        {
            // PV-01 · fabbrica del ghiaccio (fronte est, 7,5 m)
            {
                var f = new Facciata(new Vector3(-20, 0, -81.5f), Vector3.back, 7.5f, 9f, "M_Intonaco_Mattone", "M_Porto_PersianaVerde");
                for (int k = 0; k < 2; k++)
                {
                    f.AggiungiFinestra(2.5f, 4.35f + k * 2.8f);
                    f.AggiungiFinestra(5.0f, 4.35f + k * 2.8f);
                }
                f.Porta(2.05f, 0f, 2.0f, 2.8f);
                f.Vetrina(5.0f, 0f, 1.6f, 2.4f, 0.5f);
                f.Insegna("D_Gelo", 2.05f, 3.15f, 0.62f, false);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(0.3f);
                f.Pluviale(7.2f);
                // cassette di polistirolo sui davanzali
                CassettePolistirolo(new Vector3(-20.6f, 4.05f, -84.0f), f.R * 1f, 3);
                CassettePolistirolo(new Vector3(-20.6f, 6.85f, -86.5f), f.R * 1f, 2);
            }

            // PV-03 · bar del porto (fronte nord, 8 m, 4 piani)
            {
                var f = new Facciata(new Vector3(-12, 0, -82), Vector3.left, 8f, 11.8f, "M_Porto_MuroGrigio", "M_Porto_PersianaVerde");
                for (int k = 0; k < 3; k++)
                {
                    bordi(f, 2.67f, 4.35f + k * 2.8f);
                    f.AggiungiFinestra(5.33f, 4.35f + k * 2.8f);
                }
                f.Porta(1.45f, 0f, 1.2f, 2.5f);
                f.Vetrina(4.4f, 0f, 2.5f, 2.35f, 0.55f);
                f.Tenda(3.2f, 4.7f, 3.10f, 1.30f);
                f.Insegna("D_Bar", 3.2f, 3.42f, 0.42f, true);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(0.35f);
                f.Pluviale(7.65f);
                Tavolini(new Vector3(-10.7f, 0, -81.2f), 2);
                Cavo(f, 1.6f, 6.4f, 5.2f);
            }

            // PV-04 · magazzino del pesce (fronte nord, 8 m, 2 piani)
            {
                var f = new Facciata(new Vector3(-4, 0, -82), Vector3.left, 8f, 6.2f, "M_Porto_MuroAvorio", "M_Porto_PersianaArdesia");
                for (int i = 0; i < 2; i++) f.AggiungiFinestra(2.67f + i * 2.66f, 4.35f);
                f.Serranda(2.6f, 0f, 3.0f, 2.9f);
                f.Porta(6.3f, 0f, 1.05f, 2.25f, "M_Porto_LegnoChiaro");
                f.Insegna("D_Pescheria", 2.6f, 3.22f, 0.44f, true);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(0.25f);
                // casse fuori
                Cassa(new Vector3(-3.1f, 0, -81.1f), 0.8f, 0.3f, 0.5f, 6f);
                Cassa(new Vector3(-3.1f, 0.3f, -81.05f), 0.8f, 0.3f, 0.5f, -4f);
                Cassa(new Vector3(-2.2f, 0, -81.15f), 0.8f, 0.3f, 0.5f, 20f);
            }

            // lato ovest della trattoria: il P2 in piazza lo vede, non deve restare cieco
            {
                var s = new Facciata(new Vector3(20, 0, -88), Vector3.forward, 6f, 11.8f,
                                     "M_Porto_MuroGrigio", "M_Porto_PersianaLegno");
                for (int k = 0; k < 3; k++)
                {
                    s.AggiungiFinestra(1.8f, 4.35f + k * 2.8f);
                    s.AggiungiFinestra(4.2f, 4.35f + k * 2.8f);
                }
                s.Muri();
                s.Cornicione();
                s.Pluviale(0.3f);
                s.Pluviale(5.7f);
            }

            // lato est della pescheria, verso il piazzale
            {
                var s = new Facciata(new Vector3(4, 0, -74), Vector3.back, 8f, 6.2f,
                                     "M_Porto_MuroAvorio", "M_Porto_PersianaArdesia");
                for (int i = 0; i < 2; i++) s.AggiungiFinestra(1.8f + i * 2.4f, 4.35f);
                s.Porta(3.0f, 0f, 1.05f, 2.25f, "M_Porto_LegnoChiaro");
                s.Vetrina(1.35f, 0f, 1.5f, 2.15f, 0.12f);
                s.Tenda(4.55f, 2.55f, 1.9f, 0.75f);
                s.Insegna("D_Pescheria", 4.55f, 2.95f, 0.34f, true);
                s.Muri();
                s.Cornicione();
                s.Pluviale(0.3f);
                s.Pluviale(4.9f);
            }

            // PV-07 · officina nautica (fronte nord, 8 m, 2 piani)
            {
                var f = new Facciata(new Vector3(12, 0, -82), Vector3.left, 8f, 6.2f, "M_Porto_MuroCrema", "M_Porto_PersianaSalvia");
                for (int i = 0; i < 2; i++) f.AggiungiFinestra(2.67f + i * 2.66f, 4.35f);
                f.Serranda(2.9f, 0f, 3.2f, 3.05f);
                f.Porta(6.6f, 0f, 1.05f, 2.25f, "M_Porto_LegnoScuro");
                f.Insegna("D_Officina", 2.9f, 3.30f, 0.40f, true);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(7.7f);
                CassettePolistirolo(new Vector3(11.6f, 4.05f, -83.2f), f.R, 3);
                Barile(new Vector3(12.7f, 0, -81.0f));
                Barile(new Vector3(13.4f, 0, -81.15f));
            }

            // PV-09 · trattoria (fronte nord, 8 m, 4 piani)
            {
                var f = new Facciata(new Vector3(20, 0, -82), Vector3.left, 8f, 11.8f, "M_Porto_MuroGrigio", "M_Porto_PersianaLegno");
                for (int k = 0; k < 3; k++)
                {
                    f.AggiungiFinestra(2.67f, 4.35f + k * 2.8f);
                    f.AggiungiFinestra(5.33f, 4.35f + k * 2.8f);
                }
                f.Porta(1.45f, 0f, 1.2f, 2.5f);
                f.Vetrina(4.4f, 0f, 2.5f, 2.35f, 0.55f);
                f.Tenda(3.2f, 4.7f, 3.10f, 1.30f);
                f.Insegna("D_Trattoria", 3.2f, 3.42f, 0.44f, true);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(0.35f);
                f.Pluviale(7.65f);
                Tavolini(new Vector3(17.9f, 0, -81.2f), 2);
                Cavo(f, 1.6f, 6.4f, 5.2f);
            }

            // PV-10 · Capitaneria di Porto (fronte ovest, 7,67 m, 4 piani)
            {
                var f = new Facciata(new Vector3(20, 0, -87), Vector3.forward, 7.667f, 11.8f,
                                     "M_Porto_MuroCementizio", "M_Porto_PersianaVerde");
                for (int k = 0; k < 3; k++) f.AggiungiFinestra(6.15f, 4.35f + k * 2.8f);
                f.Porta(3.0f, 0f, 1.6f, 2.6f);
                f.Muri();
                f.Balcone(3.0f, 3.55f, 2.4f);
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(0.3f);
                f.Insegna("D_Capitania", 3.0f, 2.95f, 0.34f, false);
                Orologio(f, 3.0f, 5.75f);
                Salvagente(f, 1.15f, 2.1f);
                Bandiera(f, 5.6f, 11.6f);
                // bandierine e cavi sulla facciata
                Cavo(f, 0.8f, 6.8f, 6.9f);
            }

            // PV-04 · fianco est sulla Salita del Porto (6 m)
            {
                var e = new Facciata(new Vector3(-4, 0, -76), Vector3.back, 6f, 6.2f,
                                     "M_Porto_MuroAvorio", "M_Porto_PersianaArdesia");
                e.AggiungiFinestra(1.9f, 4.35f);
                e.AggiungiFinestra(4.1f, 4.35f);
                e.Porta(4.7f, 0f, 1.05f, 2.25f, "M_Porto_LegnoChiaro");
                e.Muri();
                e.Cornicione();
                e.Pluviale(0.3f);
                Cavo(e, 1.2f, 4.9f, 5.4f);
            }

            // PV-07 · fianco ovest sulla Salita del Porto (6 m)
            {
                var e = new Facciata(new Vector3(4, 0, -82), Vector3.forward, 6f, 6.2f,
                                     "M_Porto_MuroCrema", "M_Porto_PersianaSalvia");
                e.AggiungiFinestra(1.9f, 4.35f);
                e.AggiungiFinestra(4.1f, 4.35f);
                e.Porta(1.35f, 0f, 1.05f, 2.25f, "M_Porto_LegnoScuro");
                e.Serranda(4.5f, 0f, 1.8f, 2.6f);
                e.Muri();
                e.Cornicione();
                e.Pluviale(5.7f);
                CassettePolistirolo(new Vector3(3.72f, 4.05f, -78.2f), Vector3.forward, 2);
            }

            // PVE-01 · magazzino doganale (fronte ovest e sud, 4 m, 4 piani)
            {
                var f = new Facciata(new Vector3(20, 0, -99), Vector3.forward, 4f, 11.8f,
                                     "M_Porto_MuroPerla", "M_Porto_PersianaLegno");
                for (int k = 0; k < 3; k++) f.AggiungiFinestra(2.0f, 4.35f + k * 2.8f);
                f.Porta(0.95f, 0f, 1.5f, 2.6f);
                f.Insegna("D_Dogana", 2.95f, 3.0f, 0.40f, true);
                f.Muri();
                f.Cornicione();
                // tetto piano: niente falde in coppi sospese
                f.Pluviale(3.75f);

                var s = new Facciata(new Vector3(24, 0, -95), Vector3.left, 4f, 11.8f,
                                     "M_Porto_MuroPerla", "M_Porto_PersianaLegno");
                for (int k = 0; k < 3; k++) s.AggiungiFinestra(2.0f, 4.35f + k * 2.8f);
                s.Muri();
                s.Cornicione();
                // tetto piano: niente falde in coppi sospese
            }
        }

        private static void bordi(Facciata f, float x, float y) => f.AggiungiFinestra(x, y);

        private static void CassettePolistirolo(Vector3 pos, Vector3 dir, int n)
        {
            var g = B("M_Porto_BiancoRotto");
            Vector3 d = dir.normalized;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = pos + d * (i * 0.5f);
                g.Box(new Vector3(p.x - 0.26f, p.y, p.z - 0.20f), new Vector3(p.x + 0.26f, p.y + 0.26f, p.z + 0.20f));
                g.Box(new Vector3(p.x - 0.24f, p.y + 0.26f, p.z - 0.18f), new Vector3(p.x + 0.24f, p.y + 0.29f, p.z + 0.18f));
            }
        }

        private static void Tavolini(Vector3 o, int n)
        {
            var legno = B("M_Porto_LegnoScuro");
            var ferro = B("M_Porto_Ferro");
            for (int i = 0; i < n; i++)
            {
                Vector3 c = o + new Vector3(i * 1.5f, 0, -i * 0.35f);
                legno.Cylinder(new Vector3(c.x, 0.72f, c.z), new Vector3(c.x, 0.76f, c.z), 0.36f, 14);
                ferro.Cylinder(new Vector3(c.x, 0f, c.z), new Vector3(c.x, 0.72f, c.z), 0.035f, 6);
                ferro.Disc(new Vector3(c.x, 0.01f, c.z), Vector3.up, 0.28f, 10);
                for (int k = 0; k < 2; k++)
                {
                    float a = (k * 180 + i * 35) * Mathf.Deg2Rad;
                    Vector3 s = c + new Vector3(Mathf.Sin(a) * 0.62f, 0, Mathf.Cos(a) * 0.62f);
                    Sedia(s, legno, ferro, a * Mathf.Rad2Deg + 180f);
                }
            }
        }

        private static void Sedia(Vector3 p, Geo legno, Geo ferro, float rot)
        {
            var q = Quaternion.Euler(0, rot, 0);
            legno.BoxR(p + new Vector3(0, 0.45f, 0), new Vector3(0.42f, 0.04f, 0.42f), q);
            legno.BoxR(p + q * new Vector3(0, 0.72f, -0.18f), new Vector3(0.42f, 0.46f, 0.04f), q);
            foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                ferro.Cylinder(p + q * new Vector3(sx * 0.16f, 0f, sz * 0.16f), p + q * new Vector3(sx * 0.16f, 0.43f, sz * 0.16f), 0.015f, 5);
        }

        private static void Barile(Vector3 p)
        {
            var legno = B("M_Porto_LegnoScuro");
            var ferro = B("M_Porto_Ferro");
            legno.Cylinder(p, p + Vector3.up * 0.92f, 0.30f, 14, true, 0.26f);
            foreach (float y in new[] { 0.12f, 0.46f, 0.80f })
                ferro.Torus(p + Vector3.up * y, Vector3.up, 0.295f, 0.022f, 16, 5);
        }

        /// <summary>Cavo elettrico che attraversa la facciata con la sua freccia.</summary>
        private static void Cavo(Facciata f, float x0, float x1, float y)
        {
            var ferro = B("M_Porto_Ferro");
            ferro.Catenary(f.P(x0, y, 0.14f), f.P(x1, y, 0.14f), 0.28f, 0.012f, 10);
        }

        private static void Orologio(Facciata f, float cx, float cy)
        {
            var bianco = B("M_Porto_BiancoRotto");
            var ferro = B("M_Porto_Ferro");
            Vector3 c = f.P(cx, cy, 0.04f);
            bianco.Disc(c, f.Out, 0.56f, 28);
            ferro.Torus(c + f.Out * 0.02f, f.Out, 0.56f, 0.035f, 28, 6);
            ferro.Disc(c + f.Out * 0.03f, f.Out, 0.035f, 10);
            // lancette: ore 7:10
            Vector3 Hand(float deg, float len)
            {
                float a = deg * Mathf.Deg2Rad;
                return c + (f.R * Mathf.Sin(a) + Vector3.up * Mathf.Cos(a)) * len + f.Out * 0.05f;
            }
            ferro.Beam(c + f.Out * 0.05f, Hand(215f, 0.30f), 0.05f, 0.02f, f.Out);
            ferro.Beam(c + f.Out * 0.05f, Hand(60f, 0.44f), 0.032f, 0.02f, f.Out);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Vector3 d = f.R * Mathf.Sin(a) + Vector3.up * Mathf.Cos(a);
                f.BoxF(ferro, cx + d.x * 0.485f - 0.012f, cx + d.x * 0.485f + 0.012f, cy + d.y * 0.485f - 0.012f, cy + d.y * 0.485f + 0.012f, 0.05f, 0.062f);
            }
        }

        private static void Salvagente(Facciata f, float cx, float cy)
        {
            var bianco = B("M_Porto_BiancoRotto");
            var rosso = B("M_Porto_RossoSpento");
            Vector3 c = f.P(cx, cy, 0.12f);
            bianco.Torus(c, f.Out, 0.30f, 0.075f, 24, 8);
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                Vector3 d = f.R * Mathf.Sin(a) + Vector3.up * Mathf.Cos(a);
                rosso.Beam(c + d * 0.375f - f.R * 0.04f, c + d * 0.375f + f.R * 0.04f, 0.14f, 0.155f, f.Out);
            }
        }

        private static void Bandiera(Facciata f, float cx, float y)
        {
            var ferro = B("M_Porto_Ferro");
            var tessuto = B("M_Porto_RossoSpento");
            Vector3 baseP = f.P(cx, y, 0.25f);
            ferro.Cylinder(baseP, baseP + Vector3.up * 3.0f, 0.035f, 8);
            Vector3 top = baseP + Vector3.up * 2.95f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = top + f.R * (i * 0.30f) - Vector3.up * (i * 0.03f);
                Vector3 b = top + f.R * ((i + 1) * 0.30f) - Vector3.up * ((i + 1) * 0.03f);
                tessuto.Quad(a, b, b - Vector3.up * 0.95f, a - Vector3.up * 0.95f, f.Out);
                tessuto.Quad(b, a, a - Vector3.up * 0.95f, b - Vector3.up * 0.95f, -f.Out);
            }
        }
    }
}
