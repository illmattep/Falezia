using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// BANCO DI PROVA DEI MATERIALI (doc 29, fase F1).
    ///
    /// Una scena piccola e sempre uguale dove si giudicano i materiali prima di metterli nella mappa:
    /// due case (una di fronte, una di fianco, così si vedono tutti gli assi), finestre incassate,
    /// zoccolo e cornicione in pietra, tetto di coppi, gradini, piazza. Sole da Ovest, cielo, post.
    ///
    /// Menu: Falezia → Calanera → Banco di prova. Le inquadrature fisse stanno in "Viste":
    /// <see cref="CalaneraShots"/> le rende in PNG.
    /// </summary>
    public static class CalaneraBanco
    {
        public const string ScenePath = "Assets/Scenes/Calanera_Banco.unity";
        public const string MeshDir = "Assets/Art/Calanera/Banco";

        [MenuItem("Falezia/Calanera/Banco di prova")]
        public static void Crea()
        {
            CalaneraLibreria.Crea(false);
            Directory.CreateDirectory(MeshDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/{n}.mat");
            var vetro = VetroMat();

            // ---- suolo: piazza 48 × 48 m
            var suolo = new Builder();
            suolo.Quad(new Vector3(-24, 0, 24), new Vector3(24, 0, 24), new Vector3(24, 0, -24), new Vector3(-24, 0, -24), Vector3.up, 24);
            Spawn("Suolo", suolo, M("M_Piazza"));

            // ---- casa A: facciata verso +Z, 12 × 8 m, due piani di finestre, porta al centro
            var muroA = new Builder(); var vetriA = new Builder();
            var buchi = new List<Rect>();
            foreach (float x in new[] { -4.2f, -1.4f, 1.4f, 4.2f })
            {
                buchi.Add(new Rect(x - 0.55f, 4.65f, 1.1f, 1.55f));       // primo piano
                if (Mathf.Abs(x) > 2f) buchi.Add(new Rect(x - 0.55f, 1.35f, 1.1f, 1.55f)); // terra
            }
            buchi.Add(new Rect(-0.6f, 0.45f, 1.2f, 2.35f));                // porta (sopra i gradini)
            Facciata(muroA, vetriA, new Vector3(-6, 0, 0), Vector3.right, Vector3.forward, 12f, 8f, buchi, 0.18f, offset: -6f);
            // il resto del volume (lati, retro) come scatola chiusa dietro la facciata
            Box(muroA, new Vector3(-6, 0, -10), new Vector3(6, 8, 0f), skipFace: Vector3.forward);
            {
                float t = Mathf.Tan(22f * Mathf.Deg2Rad), apexY = 8f + t * 5.5f, eave = 8f + t * 0.5f;
                Timpano(muroA, new Vector3(6, 8, 0), new Vector3(6, 8, -10), eave, new Vector3(6, apexY, -5), Vector3.right);
                Timpano(muroA, new Vector3(-6, 8, 0), new Vector3(-6, 8, -10), eave, new Vector3(-6, apexY, -5), Vector3.left);
            }
            Spawn("CasaA_Muri", muroA, M("M_Intonaco_Mattone"));
            Spawn("CasaA_Vetri", vetriA, vetro);

            var pietraA = new Builder();
            Box(pietraA, new Vector3(-6.06f, 0, -10.06f), new Vector3(-0.6f, 0.95f, 0.06f));          // zoccolo (sinistra)
            Box(pietraA, new Vector3(0.6f, 0, -10.06f), new Vector3(6.06f, 0.95f, 0.06f));            // zoccolo (destra)
            Box(pietraA, new Vector3(-6.25f, 7.6f, -10.25f), new Vector3(6.25f, 8.0f, 0.25f));        // cornicione
            for (int i = 0; i < 3; i++)                                                                // gradini
                Box(pietraA, new Vector3(-1.0f, 0, 0.06f + 0.32f * i), new Vector3(1.0f, 0.45f - 0.15f * i, 0.38f + 0.32f * i));
            Spawn("CasaA_Pietra", pietraA, M("M_Pietra_Conci"));

            var tettoA = new Builder();
            Tetto(tettoA, new Vector3(-6.5f, 8.0f, -10.5f), new Vector3(6.5f, 8.0f, 0.5f), 22f);
            Spawn("CasaA_Tetto", tettoA, M("M_Coppi"));

            // ---- casa B: di fianco, facciata verso -X (verso la casa A e il sole), tre piani
            var muroB = new Builder(); var vetriB = new Builder();
            var buchiB = new List<Rect>();
            foreach (float z in new[] { -3.0f, -0.5f, 2.0f })
                for (int p = 0; p < 3; p++) buchiB.Add(new Rect(z - 0.55f, 1.4f + p * 3.2f, 1.1f, 1.55f));
            Facciata(muroB, vetriB, new Vector3(10, 0, -5.5f), Vector3.forward, Vector3.left, 10f, 10.6f, buchiB, 0.18f, offset: -5.5f);
            Box(muroB, new Vector3(10f, 0, -5.5f), new Vector3(19f, 10.6f, 4.5f), skipFace: Vector3.left);
            {
                float t = Mathf.Tan(22f * Mathf.Deg2Rad), apexY = 10.6f + t * 5f, eave = 10.6f + t * 0.5f;
                Timpano(muroB, new Vector3(10, 10.6f, 4.5f), new Vector3(19, 10.6f, 4.5f), eave, new Vector3(14.5f, apexY, 4.5f), Vector3.forward);
                Timpano(muroB, new Vector3(10, 10.6f, -5.5f), new Vector3(19, 10.6f, -5.5f), eave, new Vector3(14.5f, apexY, -5.5f), Vector3.back);
            }
            Spawn("CasaB_Muri", muroB, M("M_Venez_Pietra"));
            Spawn("CasaB_Vetri", vetriB, vetro);
            var pietraB = new Builder();
            Box(pietraB, new Vector3(9.94f, 0, -5.56f), new Vector3(19.06f, 0.95f, 4.56f));
            Box(pietraB, new Vector3(9.75f, 10.2f, -5.75f), new Vector3(19.25f, 10.6f, 4.75f));
            Spawn("CasaB_Pietra", pietraB, M("M_Pietra_Conci"));
            var tettoB = new Builder();
            Tetto(tettoB, new Vector3(9.5f, 10.6f, -6.0f), new Vector3(19.5f, 10.6f, 5.0f), 22f, alongZ: false);
            Spawn("CasaB_Tetto", tettoB, M("M_Coppi"));

            // ---- muretto lungo (prova di ripetizione: 24 m dello stesso materiale)
            var muretto = new Builder();
            Box(muretto, new Vector3(-24, 0, 8), new Vector3(0, 1.1f, 8.45f));
            Spawn("Muretto", muretto, M("M_Grezzo_Mattone"));

            Luce();
            Viste();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Calanera: banco di prova creato ({ScenePath})");
        }

        // ------------------------------------------------------------------ geometria
        /// <summary>Raccoglie triangoli con normali e UV in metri (proiezione del mondo).</summary>
        private class Builder
        {
            public readonly List<Vector3> v = new();
            public readonly List<Vector3> n = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> t = new();

            public void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f) { (p1, p2) = (p2, p1); }
                int b = v.Count;
                foreach (var p in new[] { p0, p1, p2 }) { v.Add(p); n.Add(normal); uv.Add(PlanarUV(p, normal)); }
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }

            /// <summary>Quad p0..p3 (orario visto dalla normale). Suddiviso in div×div per la pittura dei vertici.</summary>
            public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, int div = 1)
            {
                // Unity disegna il lato da cui i vertici girano in senso orario: cross(p1-p0, p2-p0) deve
                // guardare come la normale. Se no, si inverte il giro (così nessuna faccia sparisce).
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f) { (p1, p3) = (p3, p1); }
                int b = v.Count;
                for (int j = 0; j <= div; j++)
                for (int i = 0; i <= div; i++)
                {
                    float u = i / (float)div, w = j / (float)div;
                    var p = Vector3.Lerp(Vector3.Lerp(p0, p1, u), Vector3.Lerp(p3, p2, u), w);
                    v.Add(p); n.Add(normal); uv.Add(PlanarUV(p, normal));
                }
                int row = div + 1;
                for (int j = 0; j < div; j++)
                for (int i = 0; i < div; i++)
                {
                    int a = b + j * row + i;
                    t.Add(a); t.Add(a + 1); t.Add(a + row + 1);
                    t.Add(a); t.Add(a + row + 1); t.Add(a + row);
                }
            }
        }

        /// <summary>
        /// Timpano: il muro sotto le falde. a e b sono gli angoli in cima al muro, yGronda l'altezza del
        /// tetto sopra il filo del muro, apex il colmo. Ventaglio di triangoli dal colmo.
        /// </summary>
        private static void Timpano(Builder bl, Vector3 a, Vector3 b, float yGronda, Vector3 apex, Vector3 normal)
        {
            var a2 = new Vector3(a.x, yGronda, a.z); var b2 = new Vector3(b.x, yGronda, b.z);
            bl.Tri(apex, a2, b2, normal);
            bl.Tri(a2, a, b, normal);
            bl.Tri(a2, b, b2, normal);
        }

        /// <summary>Le stesse UV dello shader in modalità "Mondo": la mesh funziona anche in modalità UV0.</summary>
        private static Vector2 PlanarUV(Vector3 p, Vector3 nrm)
        {
            var a = new Vector3(Mathf.Abs(nrm.x), Mathf.Abs(nrm.y), Mathf.Abs(nrm.z));
            if (a.y >= a.x && a.y >= a.z) { float s = Mathf.Sign(nrm.y); return new Vector2(p.x, -s * p.z); }
            if (a.x >= a.z) { float s = Mathf.Sign(nrm.x); return new Vector2(-s * p.z, p.y); }
            { float s = Mathf.Sign(nrm.z); return new Vector2(s * p.x, p.y); }
        }

        /// <summary>Scatola allineata agli assi. skipFace: la faccia da non emettere (coperta da altro).</summary>
        private static void Box(Builder b, Vector3 min, Vector3 max, Vector3? skipFace = null)
        {
            Vector3 c(float x, float y, float z) => new(x, y, z);
            void F(Vector3 nrm, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                if (skipFace.HasValue && skipFace.Value == nrm) return;
                b.Quad(p0, p1, p2, p3, nrm);
            }
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
            F(Vector3.forward, c(x0, y1, z1), c(x1, y1, z1), c(x1, y0, z1), c(x0, y0, z1));
            F(Vector3.back, c(x1, y1, z0), c(x0, y1, z0), c(x0, y0, z0), c(x1, y0, z0));
            F(Vector3.right, c(x1, y1, z1), c(x1, y1, z0), c(x1, y0, z0), c(x1, y0, z1));
            F(Vector3.left, c(x0, y1, z0), c(x0, y1, z1), c(x0, y0, z1), c(x0, y0, z0));
            F(Vector3.up, c(x0, y1, z0), c(x1, y1, z0), c(x1, y1, z1), c(x0, y1, z1));
            F(Vector3.down, c(x0, y0, z1), c(x1, y0, z1), c(x1, y0, z0), c(x0, y0, z0));
        }

        /// <summary>
        /// Facciata piana con aperture incassate. origin = angolo in basso a sinistra visto da fuori,
        /// right = direzione lungo la facciata, outward = normale. Le aperture sono rettangoli in
        /// coordinate locali (x lungo la facciata, y in alto). depth = profondità dell'incasso.
        /// </summary>
        private static void Facciata(Builder muro, Builder vetri, Vector3 origin, Vector3 right, Vector3 outward,
                                     float width, float height, List<Rect> holes, float depth, float offset = 0f)
        {
            // coordinate x lungo la facciata partono da "offset" (comodo per le facciate ruotate)
            var xs = new SortedSet<float> { offset, offset + width };
            var ys = new SortedSet<float> { 0, height };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            var X = new List<float>(xs); var Y = new List<float>(ys);
            Vector3 P(float x, float y, float d = 0f) => origin + right * (x - offset) + Vector3.up * y - outward * d;
            for (int j = 0; j < Y.Count - 1; j++)
            for (int i = 0; i < X.Count - 1; i++)
            {
                var cell = new Rect(X[i], Y[j], X[i + 1] - X[i], Y[j + 1] - Y[j]);
                bool foro = holes.Exists(h => h.Contains(cell.center));
                if (!foro) muro.Quad(P(cell.xMin, cell.yMax), P(cell.xMax, cell.yMax), P(cell.xMax, cell.yMin), P(cell.xMin, cell.yMin), outward);
            }
            foreach (var h in holes)
            {
                // spallette, architrave e davanzale interni (stesso materiale del muro)
                muro.Quad(P(h.xMin, h.yMax), P(h.xMin, h.yMax, depth), P(h.xMin, h.yMin, depth), P(h.xMin, h.yMin), right);
                muro.Quad(P(h.xMax, h.yMax, depth), P(h.xMax, h.yMax), P(h.xMax, h.yMin), P(h.xMax, h.yMin, depth), -right);
                muro.Quad(P(h.xMin, h.yMax, depth), P(h.xMin, h.yMax), P(h.xMax, h.yMax), P(h.xMax, h.yMax, depth), Vector3.down);
                if (h.yMin > 0.01f)
                    muro.Quad(P(h.xMin, h.yMin), P(h.xMin, h.yMin, depth), P(h.xMax, h.yMin, depth), P(h.xMax, h.yMin), Vector3.up);
                vetri.Quad(P(h.xMin, h.yMax, depth), P(h.xMax, h.yMax, depth), P(h.xMax, h.yMin, depth), P(h.xMin, h.yMin, depth), outward);
            }
        }

        /// <summary>Tetto a due falde sopra il rettangolo min-max (colmo lungo X, o lungo Z se alongZ è falso).</summary>
        private static void Tetto(Builder b, Vector3 min, Vector3 max, float pitchDeg, bool alongZ = true)
        {
            float y = min.y;
            if (alongZ)
            {
                float half = (max.z - min.z) * 0.5f, rise = Mathf.Tan(pitchDeg * Mathf.Deg2Rad) * half, zc = min.z + half;
                var nf = new Vector3(0, Mathf.Cos(pitchDeg * Mathf.Deg2Rad), Mathf.Sin(pitchDeg * Mathf.Deg2Rad));
                var nb = new Vector3(0, nf.y, -nf.z);
                b.Quad(new(min.x, y + rise, zc), new(max.x, y + rise, zc), new(max.x, y, max.z), new(min.x, y, max.z), nf);
                b.Quad(new(max.x, y + rise, zc), new(min.x, y + rise, zc), new(min.x, y, min.z), new(max.x, y, min.z), nb);
            }
            else
            {
                float half = (max.x - min.x) * 0.5f, rise = Mathf.Tan(pitchDeg * Mathf.Deg2Rad) * half, xc = min.x + half;
                var nr = new Vector3(Mathf.Sin(pitchDeg * Mathf.Deg2Rad), Mathf.Cos(pitchDeg * Mathf.Deg2Rad), 0);
                var nl = new Vector3(-nr.x, nr.y, 0);
                b.Quad(new(xc, y + rise, max.z), new(xc, y + rise, min.z), new(max.x, y, min.z), new(max.x, y, max.z), nr);
                b.Quad(new(xc, y + rise, min.z), new(xc, y + rise, max.z), new(min.x, y, max.z), new(min.x, y, min.z), nl);
            }
        }

        private static GameObject Spawn(string name, Builder b, Material mat)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(b.v); mesh.SetNormals(b.n); mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.t, 0);
            var black = new Color[b.v.Count];
            for (int i = 0; i < black.Length; i++) black[i] = new Color(0, 0, 0, 1);
            mesh.colors = black;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            string path = $"{MeshDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        private static Material VetroMat()
        {
            string path = $"{CalaneraLibreria.MatRoot}/M_Vetro.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.09f, 0.12f, 0.13f));
            m.SetFloat("_Smoothness", 0.82f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ luce e viste
        public static void Luce()
        {
            var sunGo = new GameObject("Sole");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.91f, 0.77f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.35f;
            sunGo.transform.rotation = Quaternion.Euler(34f, 125f, 0f);
            RenderSettings.sun = sun;

            var sky = Shader.Find("Skybox/Falezia Nuvole");
            if (sky != null)
            {
                var mat = new Material(sky) { name = "Cielo Banco" };
                mat.SetVector("_SunDir", -sunGo.transform.forward);
                mat.SetColor("_SunColor", sun.color);
                mat.SetColor("_Horizon", new Color(0.80f, 0.82f, 0.82f));
                mat.SetColor("_Zenith", new Color(0.15f, 0.33f, 0.62f));
                mat.SetFloat("_Cover", 0.45f);
                string p = $"{CalaneraLibreria.MatRoot}/M_CieloBanco.mat";
                AssetDatabase.DeleteAsset(p);
                AssetDatabase.CreateAsset(mat, p);
                RenderSettings.skybox = mat;
            }
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.85f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.80f, 0.80f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 330f;
            DynamicGI.UpdateEnvironment();

            var profile = Resources.Load<VolumeProfile>("Falezia-Volume");
            if (profile != null)
            {
                var v = new GameObject("Post").AddComponent<Volume>();
                v.isGlobal = true;
                v.sharedProfile = profile;
            }
        }

        private static void Viste()
        {
            var root = new GameObject("Viste").transform;
            void V(string n, Vector3 pos, Vector3 look)
            {
                var t = new GameObject(n).transform;
                t.SetParent(root);
                t.position = pos;
                t.rotation = Quaternion.LookRotation(look - pos);
            }
            V("1_Vicino", new Vector3(-2.6f, 1.7f, 3.2f), new Vector3(0.8f, 1.3f, 0f));
            V("2_Medio", new Vector3(-9f, 1.7f, 10f), new Vector3(2f, 3.2f, -2f));
            V("3_Lontano", new Vector3(-21f, 4.5f, 22f), new Vector3(3f, 4f, -4f));
            V("4_Angolo", new Vector3(4.5f, 1.7f, 7.5f), new Vector3(10f, 3.2f, -1.5f));
            V("5_Muretto", new Vector3(-3f, 1.6f, 13f), new Vector3(-16f, 0.6f, 8.2f));
            V("6_Tetti", new Vector3(-9f, 15f, 15f), new Vector3(5f, 8.5f, -4f));
        }
    }
}
