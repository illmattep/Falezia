using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Geometria procedurale della mappa nuova (doc 29): un sacco di triangoli con normali, UV in
    /// metri e colori dei vertici, da buttare in una mesh per materiale. Niente GameObject per
    /// mattone: si costruisce tutto e alla fine si crea un oggetto per materiale (draw call = materiali).
    ///
    /// Le UV servono solo ai decal (0..1); i materiali Falezia/Superficie usano il mondo.
    /// </summary>
    public sealed class Geo
    {
        public readonly List<Vector3> Pos = new(), Nrm = new();
        public readonly List<Vector2> Uv = new();
        public readonly List<Color> Col = new();
        public readonly List<int> Idx = new();

        public int Triangles => Idx.Count / 3;
        public bool Empty => Idx.Count == 0;

        /// <summary>Scatole candidate alla collisione (una per primitiva solida, non per triangolo).</summary>
        public readonly List<(Vector3 min, Vector3 max)> Solids = new();
        /// <summary>Mentre è true le facce non registrano una scatola (le registra il chiamante).</summary>
        public bool Quiet;

        public void Solid(Vector3 min, Vector3 max)
        {
            if (min.x > max.x) (min.x, max.x) = (max.x, min.x);
            if (min.y > max.y) (min.y, max.y) = (max.y, min.y);
            if (min.z > max.z) (min.z, max.z) = (max.z, min.z);
            Solids.Add((min, max));
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Vector2 ua, Vector2 ub, Vector2 uc, Color? tint = null)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f) (b, c) = (c, b);
            int i = Pos.Count;
            Pos.Add(a); Pos.Add(b); Pos.Add(c);
            Nrm.Add(n); Nrm.Add(n); Nrm.Add(n);
            Uv.Add(ua); Uv.Add(ub); Uv.Add(uc);
            Color k = tint ?? Color.white;
            Col.Add(k); Col.Add(k); Col.Add(k);
            Idx.Add(i); Idx.Add(i + 1); Idx.Add(i + 2);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n) =>
            Tri(a, b, c, n, Planar(a, n), Planar(b, n), Planar(c, n));

        /// <summary>Quad p0..p3 in giro (p0-p1-p2-p3), normale n. UV proiettate dal mondo.</summary>
        public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
        {
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0f) (p1, p3) = (p3, p1);
            if (!Quiet) Solid(Vector3.Min(Vector3.Min(p0, p1), Vector3.Min(p2, p3)),
                              Vector3.Max(Vector3.Max(p0, p1), Vector3.Max(p2, p3)));
            Tri(p0, p1, p2, n, Planar(p0, n), Planar(p1, n), Planar(p2, n));
            Tri(p0, p2, p3, n, Planar(p0, n), Planar(p2, n), Planar(p3, n));
        }

        /// <summary>Quad con UV esplicite (decal): p0 in basso a sinistra, poi antiorario.</summary>
        public void QuadUv(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
        {
            var u0 = new Vector2(0, 0); var u1 = new Vector2(1, 0);
            var u2 = new Vector2(1, 1); var u3 = new Vector2(0, 1);
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0f)
            {
                (p1, p3) = (p3, p1);
                (u1, u3) = (u3, u1);
            }
            if (!Quiet) Solid(Vector3.Min(Vector3.Min(p0, p1), Vector3.Min(p2, p3)),
                              Vector3.Max(Vector3.Max(p0, p1), Vector3.Max(p2, p3)));
            Tri(p0, p1, p2, n, u0, u1, u2);
            Tri(p0, p2, p3, n, u0, u2, u3);
        }

        public void Box(Vector3 min, Vector3 max, bool bottom = true, bool top = true)
        {
            bool q = Quiet; Quiet = true;
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
            Quad(new(x0, y1, z1), new(x1, y1, z1), new(x1, y0, z1), new(x0, y0, z1), Vector3.forward);
            Quad(new(x1, y1, z0), new(x0, y1, z0), new(x0, y0, z0), new(x1, y0, z0), Vector3.back);
            Quad(new(x1, y1, z1), new(x1, y1, z0), new(x1, y0, z0), new(x1, y0, z1), Vector3.right);
            Quad(new(x0, y1, z0), new(x0, y1, z1), new(x0, y0, z1), new(x0, y0, z0), Vector3.left);
            if (top) Quad(new(x0, y1, z0), new(x1, y1, z0), new(x1, y1, z1), new(x0, y1, z1), Vector3.up);
            if (bottom) Quad(new(x0, y0, z1), new(x1, y0, z1), new(x1, y0, z0), new(x0, y0, z0), Vector3.down);
            Quiet = q;
            Solid(min, max);
        }

        public void BoxC(Vector3 center, Vector3 size) =>
            Box(center - size * 0.5f, center + size * 0.5f);

        /// <summary>Scatola orientata: utile per sedie, assi, pezzi ruotati.</summary>
        public void BoxR(Vector3 center, Vector3 size, Quaternion rot)
        {
            bool q = Quiet; Quiet = true;
            Vector3 h = size * 0.5f;
            Vector3 P(float sx, float sy, float sz) => center + rot * new Vector3(sx * h.x, sy * h.y, sz * h.z);
            Vector3 F(float x, float y, float z) => rot * new Vector3(x, y, z);
            Quad(P(-1, 1, 1), P(1, 1, 1), P(1, -1, 1), P(-1, -1, 1), F(0, 0, 1));
            Quad(P(1, 1, -1), P(-1, 1, -1), P(-1, -1, -1), P(1, -1, -1), F(0, 0, -1));
            Quad(P(1, 1, 1), P(1, 1, -1), P(1, -1, -1), P(1, -1, 1), F(1, 0, 0));
            Quad(P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1), P(-1, -1, -1), F(-1, 0, 0));
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), F(0, 1, 0));
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), F(0, -1, 0));
            Quiet = q;
            Vector3 lo = Vector3.Min(Vector3.Min(Vector3.Min(P(-1,-1,-1), P(1,-1,-1)), Vector3.Min(P(-1,1,-1), P(1,1,-1))),
                                     Vector3.Min(Vector3.Min(P(-1,-1,1), P(1,-1,1)), Vector3.Min(P(-1,1,1), P(1,1,1))));
            Vector3 hi = Vector3.Max(Vector3.Max(Vector3.Max(P(-1,-1,-1), P(1,-1,-1)), Vector3.Max(P(-1,1,-1), P(1,1,-1))),
                                     Vector3.Max(Vector3.Max(P(-1,-1,1), P(1,-1,1)), Vector3.Max(P(-1,1,1), P(1,1,1))));
            Solid(lo, hi);
        }

        /// <summary>Trave (o asta) fra due punti, sezione w × h. upHint decide l'orientamento della sezione.</summary>
        public void Beam(Vector3 a, Vector3 b, float w, float h, Vector3 upHint)
        {
            Vector3 axis = b - a;
            float len = axis.magnitude;
            if (len < 1e-4f) return;
            axis /= len;
            Vector3 up = upHint - axis * Vector3.Dot(upHint, axis);
            if (up.sqrMagnitude < 1e-6f) up = Vector3.up - axis * axis.y;
            if (up.sqrMagnitude < 1e-6f) up = Vector3.right - axis * axis.x;
            up.Normalize();
            Vector3 right = Vector3.Cross(axis, up);
            Vector3 A(float sx, float sy) => a + right * (sx * w * 0.5f) + up * (sy * h * 0.5f);
            Vector3 B(float sx, float sy) => b + right * (sx * w * 0.5f) + up * (sy * h * 0.5f);
            Quad(A(-1, 1), A(1, 1), B(1, 1), B(-1, 1), up);
            Quad(A(1, -1), A(-1, -1), B(-1, -1), B(1, -1), -up);
            Quad(A(1, 1), A(1, -1), B(1, -1), B(1, 1), right);
            Quad(A(-1, -1), A(-1, 1), B(-1, 1), B(-1, -1), -right);
            Quad(A(-1, 1), A(1, 1), A(1, -1), A(-1, -1), -axis);
            Quad(B(1, 1), B(-1, 1), B(-1, -1), B(1, -1), axis);
        }

        /// <summary>Cilindro (o tubo) fra a e b, con tappi.</summary>
        public void Cylinder(Vector3 a, Vector3 b, float radius, int sides = 10, bool caps = true, float radiusTop = -1)
        {
            Vector3 axis = b - a;
            if (axis.sqrMagnitude < 1e-8f) return;
            axis.Normalize();
            Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 u = Vector3.Cross(axis, up).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            float rt = radiusTop < 0 ? radius : radiusTop;
            if (!Quiet) Solid(Vector3.Min(a, b) - Vector3.one * Mathf.Max(radius, rt),
                              Vector3.Max(a, b) + Vector3.one * Mathf.Max(radius, rt));
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                Vector3 d1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                Vector3 n0 = d0, n1 = d1;
                Quad(a + d0 * radius, a + d1 * radius, b + d1 * rt, b + d0 * rt, (n0 + n1).normalized);
                if (caps)
                {
                    Tri(a, a + d1 * radius, a + d0 * radius, -axis, Planar(a, -axis), Planar(a + d1 * radius, -axis), Planar(a + d0 * radius, -axis));
                    Tri(b, b + d0 * rt, b + d1 * rt, axis, Planar(b, axis), Planar(b + d0 * rt, axis), Planar(b + d1 * rt, axis));
                }
            }
        }

        /// <summary>Cerchio pieno (o anello se inner > 0) appoggiato sul piano di normale n.</summary>
        public void Disc(Vector3 center, Vector3 n, float radius, int sides = 24, float inner = 0f)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 u = Vector3.Cross(n, up).normalized;
            Vector3 v = Vector3.Cross(n, u);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                Vector3 d1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                if (inner <= 0f)
                    Tri(center, center + d1 * radius, center + d0 * radius, n, Planar(center, n), Planar(center + d1 * radius, n), Planar(center + d0 * radius, n));
                else
                {
                    Quad(center + d0 * inner, center + d0 * radius, center + d1 * radius, center + d1 * inner, n);
                    Quad(center + d0 * radius, center + d0 * inner, center + d1 * inner, center + d1 * radius, -n);
                }
            }
        }

        /// <summary>Toro (salvagente, cerchi, corde).</summary>
        public void Torus(Vector3 center, Vector3 axis, float ring, float tube, int seg = 24, int sides = 8)
        {
            axis.Normalize();
            Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 u = Vector3.Cross(axis, up).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 r0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                Vector3 r1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                for (int j = 0; j < sides; j++)
                {
                    float b0 = j * Mathf.PI * 2f / sides, b1 = (j + 1) * Mathf.PI * 2f / sides;
                    Vector3 p00 = center + r0 * ring + (r0 * Mathf.Cos(b0) + axis * Mathf.Sin(b0)) * tube;
                    Vector3 p01 = center + r0 * ring + (r0 * Mathf.Cos(b1) + axis * Mathf.Sin(b1)) * tube;
                    Vector3 p10 = center + r1 * ring + (r1 * Mathf.Cos(b0) + axis * Mathf.Sin(b0)) * tube;
                    Vector3 p11 = center + r1 * ring + (r1 * Mathf.Cos(b1) + axis * Mathf.Sin(b1)) * tube;
                    Vector3 n = (p00 - center - r0 * ring).normalized;
                    Quad(p00, p10, p11, p01, (n + (p01 - center - r1 * ring).normalized).normalized);
                }
            }
        }

        /// <summary>Falda a due piode sopra un rettangolo (per i tetti). min/max in pianta, y base.</summary>
        public void Tetto(Vector3 min, Vector3 max, float y, float pitchDeg, bool ridgeAlongX)
        {
            float t = Mathf.Tan(pitchDeg * Mathf.Deg2Rad);
            if (ridgeAlongX)
            {
                float half = (max.z - min.z) * 0.5f, rise = t * half, zc = min.z + half;
                var nf = new Vector3(0, Mathf.Cos(pitchDeg * Mathf.Deg2Rad), Mathf.Sin(pitchDeg * Mathf.Deg2Rad));
                var nb = new Vector3(0, nf.y, -nf.z);
                Quad(new(min.x, y + rise, zc), new(max.x, y + rise, zc), new(max.x, y, max.z), new(min.x, y, max.z), nf);
                Quad(new(max.x, y + rise, zc), new(min.x, y + rise, zc), new(min.x, y, min.z), new(max.x, y, min.z), nb);
            }
            else
            {
                float half = (max.x - min.x) * 0.5f, rise = t * half, xc = min.x + half;
                var nr = new Vector3(Mathf.Sin(pitchDeg * Mathf.Deg2Rad), Mathf.Cos(pitchDeg * Mathf.Deg2Rad), 0);
                var nl = new Vector3(-nr.x, nr.y, 0);
                Quad(new(xc, y + rise, max.z), new(xc, y + rise, min.z), new(max.x, y, min.z), new(max.x, y, max.z), nr);
                Quad(new(xc, y + rise, min.z), new(xc, y + rise, max.z), new(min.x, y, max.z), new(min.x, y, min.z), nl);
            }
        }

        /// <summary>Catena (cavo, corda) fra due punti con freccia: una serie di cilindri sottili.</summary>
        public void Catenary(Vector3 a, Vector3 b, float sag, float radius, int steps = 10)
        {
            Vector3 prev = a;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (sag * Mathf.Sin(t * Mathf.PI));
                Cylinder(prev, p, radius, 5, false);
                prev = p;
            }
        }

        public void Merge(Geo other, Vector3 offset)
        {
            int baseIndex = Pos.Count;
            for (int i = 0; i < other.Pos.Count; i++)
            {
                Pos.Add(other.Pos[i] + offset);
                Nrm.Add(other.Nrm[i]);
                Uv.Add(other.Uv[i]);
                Col.Add(other.Col[i]);
            }
            foreach (int i in other.Idx) Idx.Add(i + baseIndex);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(Pos);
            mesh.SetNormals(Nrm);
            mesh.SetUVs(0, Uv);
            mesh.SetColors(Col);
            mesh.SetTriangles(Idx, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>La stessa UV planare dello shader "Mondo": la mesh si vede bene anche con materiali UV0.</summary>
        public static Vector2 Planar(Vector3 p, Vector3 n)
        {
            var a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (a.y >= a.x && a.y >= a.z) { float s = Mathf.Sign(n.y); return new Vector2(p.x, -s * p.z); }
            if (a.x >= a.z) { float s = Mathf.Sign(n.x); return new Vector2(-s * p.z, p.y); }
            { float s = Mathf.Sign(n.z); return new Vector2(s * p.x, p.y); }
        }
    }

    /// <summary>Scrittura delle mesh come asset e creazione degli oggetti in scena.</summary>
    public static class CalaneraMesh
    {
        public static Mesh Save(Geo geo, string dir, string name)
        {
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var mesh = geo.ToMesh(name);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static GameObject Spawn(string name, Geo geo, Material mat, Transform parent, string dir, bool staticFlags = true)
        {
            var mesh = Save(geo, dir, name);
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (staticFlags)
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                                                           StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }
    }
}
