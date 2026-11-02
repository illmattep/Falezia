using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// SPAWN NORD — PORTO VECCHIO (doc 29 fase F2, doc 30 area 1).
    ///
    /// Riempie il piazzale del blockout con l'arte vera: suolo di lastre, i due container,
    /// le pile di casse, le cinque piazzole di spawn col bordo ciano, le facciate dei magazzini
    /// sul mare con l'insegna FALEZIA, le case sul piazzale e la gru a traliccio.
    ///
    /// Tutto è rigenerabile: il comando cancella il gruppo "Spawn Nord" e lo rifà. Ciò che si
    /// ritocca a mano nella scena resta solo se non sta sotto quel gruppo. Le scatole del
    /// blockout restano i collider: qui si nascondono soltanto i renderer nella zona dello spawn.
    ///
    /// Menu: Falezia → Calanera → Spawn Nord: costruisci.
    /// Foto: Falezia → Calanera → Foto delle viste (le inquadrature P_* sono dello spawn).
    /// </summary>
    public static partial class CalaneraSpawn
    {
        public const string RootName = "Spawn Nord";
        private const string ScenePath = CalaneraBlockout.ScenePath;
        private const string MeshDir = "Assets/Art/Calanera/PortoVecchio";
        private const string TexSegni = "Assets/Art/Calanera/Textures/segni";

        private static readonly Dictionary<string, Geo> Geos = new();
        private static readonly Dictionary<string, Material> Mats = new();

        // ------------------------------------------------------------------ comandi
        [MenuItem("Falezia/Calanera/Spawn Nord: costruisci")]
        public static void MenuBuild() => Debug.Log(Build());

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "Fermare il Play mode prima di costruire lo spawn.";
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // via TUTTE le copie: ogni ricostruzione ne lasciava una e le copie si sovrapponevano
            foreach (var oldRoot in scene.GetRootGameObjects())
                if (oldRoot != null && oldRoot.name == RootName) Object.DestroyImmediate(oldRoot);
            Geos.Clear(); Mats.Clear();
            // le scatole del blockout servono anche a chiudere i corpi delle facciate
            Boxes = Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude);

            CalaneraLibreria.Crea(false);
            Tinte();
            Vetri();
            DecalMaterial();

            var root = new GameObject(RootName).transform;

            Suolo();
            SuoloCitta();
            Piazzole();
            MoloEMare();
            Container();
            Cassette();
            Magazzini();
            // Case() hand-made non corrisponde al blockout (metteva una pescheria nel vicolo
            // centrale): la fila delle case la genera Citta(), chiusa e con tutte le facciate.
            Gru();
            Dettagli();
            RetePorto(new Vector3(13.5f, 0f, -94.2f));
            PiazzaSet();
            MercatoSet();
            SalitaSegni();
            MacchieESegni();
            Citta();
            Forte();
            Eroi();

            int tris = 0, oggetti = 0;
            foreach (var kv in Geos.OrderBy(k => k.Key))
            {
                if (kv.Value.Empty) continue;
                var mat = Mats.TryGetValue(kv.Key, out var m) ? m : CalaneraLibreriaMat(kv.Key);
                if (mat == null) { Debug.LogWarning($"Spawn Nord: materiale mancante {kv.Key}"); continue; }
                string meshName = kv.Key.StartsWith("D_") ? kv.Key : kv.Key;
                var go = CalaneraMesh.Spawn(meshName, kv.Value, mat, root, MeshDir);
                if (kv.Key.StartsWith("D_") || kv.Key == "M_Porto_Mare" || kv.Key == "M_Porto_Pozza" || kv.Key == "M_Sampietrini")
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) mr.shadowCastingMode = ShadowCastingMode.Off;
                }
                tris += kv.Value.Triangles;
                oggetti++;
            }
            CasseMeshy(root.transform);

            NascondiBlockout();
            CalaneraCollision.Export(Geos);
            Luce();
            Riflessi();
            Viste();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            string msg = $"Spawn Nord: {oggetti} oggetti, {tris} triangoli in {ScenePath}";
            Debug.Log(msg);
            return msg;
        }

        private static Material CalaneraLibreriaMat(string key)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/{key}.mat");
            if (m != null) Mats[key] = m;
            return m;
        }

        private static Geo B(string material)
        {
            if (!Geos.TryGetValue(material, out var g)) Geos[material] = g = new Geo();
            return g;
        }

        // ------------------------------------------------------------------ materiali
        private static void Tinte()
        {
            Tinta("M_Porto_Ciano", new Color(0.16f, 0.52f, 0.64f), 0.35f, 0f, new Color(0.03f, 0.14f, 0.18f));
            Tinta("M_Porto_GialloSpento", new Color(0.60f, 0.47f, 0.26f), 0.22f);
            Tinta("M_Porto_BiancoRotto", new Color(0.86f, 0.84f, 0.77f), 0.28f);
            Tinta("M_Porto_NeroFumo", new Color(0.09f, 0.09f, 0.10f), 0.30f);
            Tinta("M_Porto_RossoSpento", new Color(0.46f, 0.21f, 0.17f), 0.30f);
            Tinta("M_Porto_VerdeLanterna", new Color(0.20f, 0.34f, 0.24f), 0.45f, 0f, new Color(0.10f, 0.22f, 0.13f));
            Tinta("M_Porto_TendaOcra", new Color(0.62f, 0.47f, 0.29f), 0.16f);
            Tinta("M_Porto_TendaRuggine", new Color(0.43f, 0.25f, 0.18f), 0.16f);
            Tinta("M_Porto_Vetro", new Color(0.200f, 0.240f, 0.265f), 0.90f, 0.35f);
            Tinta("M_Porto_VetroScuro", new Color(0.055f, 0.070f, 0.080f), 0.55f, 0.10f);
            Tinta("M_Porto_Cotto", new Color(0.520f, 0.270f, 0.170f), 0.12f, 0.18f);
            Tinta("M_Porto_Terra", new Color(0.180f, 0.120f, 0.085f), 0.08f, 0.04f);
            Tinta("M_Porto_Ottone", new Color(0.55f, 0.44f, 0.24f), 0.45f, 0.65f);
            Tinta("M_Porto_Acciaio", new Color(0.38f, 0.40f, 0.41f), 0.55f, 0.85f);
            Acqua();
        }

        private static void Tinta(string name, Color c, float smoothness, float metal = 0f, Color? emission = null)
        {
            string path = $"{CalaneraLibreria.MatRoot}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            Mats[name] = m;
        }

        private static void Acqua()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexSegni}/acqua_n.png");
            // mare: opaco, specchio del cielo, riflesso largo
            var sea = new Material(Shader.Find("Falezia/Acqua")) { name = "M_Porto_Mare" };
            sea.SetColor("_DeepColor", new Color(0.012f, 0.085f, 0.105f));
            sea.SetColor("_ShallowColor", new Color(0.055f, 0.245f, 0.285f));
            sea.SetTexture("_NormalA", tex);
            sea.SetTexture("_NormalB", tex);
            sea.SetFloat("_NormalStrength", 0.80f);
            sea.SetFloat("_Smoothness", 0.70f);
            sea.SetTextureScale("_NormalA", new Vector2(0.16f, 0.16f));
            sea.SetTextureScale("_NormalB", new Vector2(0.11f, 0.13f));
            sea.SetVector("_ScrollA", new Vector4(0.020f, 0.011f, 0f, 0f));
            sea.SetVector("_ScrollB", new Vector4(-0.014f, 0.016f, 0f, 0f));
            sea.SetFloat("_SunGlint", 1.1f);
            sea.enableInstancing = true;
            string seaPath = $"{CalaneraLibreria.MatRoot}/M_Porto_Mare.mat";
            var oldSea = AssetDatabase.LoadAssetAtPath<Material>(seaPath);
            if (oldSea != null) AssetDatabase.DeleteAsset(seaPath);
            AssetDatabase.CreateAsset(sea, seaPath);
            Mats["M_Porto_Mare"] = sea;

            // pozzanghere: velo scuro trasparente
            var puddle = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Porto_Pozza" };
            puddle.SetColor("_BaseColor", new Color(0.055f, 0.075f, 0.085f, 0.72f));
            puddle.SetFloat("_Smoothness", 0.95f);
            puddle.SetFloat("_Metallic", 0.05f);
            puddle.SetTexture("_BumpMap", tex);
            puddle.SetFloat("_BumpScale", 0.35f);
            puddle.SetTextureScale("_BumpMap", new Vector2(0.7f, 0.7f));
            puddle.SetFloat("_Surface", 1f);
            puddle.SetFloat("_Blend", 0f);
            puddle.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            puddle.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            puddle.SetFloat("_ZWrite", 0f);
            puddle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            puddle.SetOverrideTag("RenderType", "Transparent");
            puddle.SetShaderPassEnabled("ShadowCaster", false);
            puddle.renderQueue = 3000;
            string pPath = $"{CalaneraLibreria.MatRoot}/M_Porto_Pozza.mat";
            var oldP = AssetDatabase.LoadAssetAtPath<Material>(pPath);
            if (oldP != null) AssetDatabase.DeleteAsset(pPath);
            AssetDatabase.CreateAsset(puddle, pPath);
            Mats["M_Porto_Pozza"] = puddle;
        }

        private static readonly Dictionary<string, string> Decal = new()
        {
            ["D_Olio"] = "olio_1",
            ["D_Olio2"] = "olio_2",
            ["D_Pozza"] = "pozzanghera_1",
            ["D_Pozza2"] = "pozzanghera_2",
            ["D_Linea"] = "linea_corsia",
            ["D_Griglia"] = "griglia",
            ["D_Crepe"] = "crepe_tombino",
            ["D_Ruggine"] = "ruggine",
            ["D_Schiuma"] = "schiuma",
            ["D_Bar"] = "insegna_bar",
            ["D_Gelo"] = "insegna_gelo",
            ["D_Pescheria"] = "insegna_pescheria",
            ["D_Capitania"] = "insegna_capitania",
            ["D_Officina"] = "insegna_officina",
            ["D_Dogana"] = "insegna_dogana",
            ["D_Trattoria"] = "insegna_trattoria",
            ["D_Targa"] = "targa_porto",
            ["D_Falezia"] = "insegna_falezia",
            ["D_Stencil"] = "stencil_calanera",
        };

        private static void DecalMaterial()
        {
            foreach (var kv in Decal)
            {
                string tex = $"{TexSegni}/{kv.Value}.png";
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
                if (t == null) { Debug.LogWarning($"Spawn Nord: manca la texture {tex} (esegui spawn_segni.py)"); continue; }
                bool painted = kv.Key.StartsWith("D_Bar") || kv.Key.StartsWith("D_Gelo") || kv.Key.StartsWith("D_Pescheria") ||
                               kv.Key.StartsWith("D_Capitania") || kv.Key.StartsWith("D_Officina") || kv.Key.StartsWith("D_Dogana") ||
                               kv.Key.StartsWith("D_Trattoria") || kv.Key.StartsWith("D_Targa") || kv.Key.StartsWith("D_Falezia");
                Material m;
                if (painted)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        name = kv.Key, renderQueue = 2450
                    };
                    m.SetTexture("_BaseMap", t);
                    m.SetColor("_BaseColor", Color.white);
                    m.SetFloat("_Smoothness", 0.22f);
                    m.SetFloat("_Metallic", 0f);
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", 0.32f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    m = new Material(Shader.Find("Falezia/FxUnlit")) { name = kv.Key };
                    m.SetTexture("_MainTex", t);
                    m.SetColor("_Color", kv.Key switch
                    {
                        "D_Linea" => new Color(0.66f, 0.60f, 0.46f, 0.55f),
                        "D_Olio" or "D_Olio2" => new Color(1f, 1f, 1f, 0.85f),
                        "D_Pozza" or "D_Pozza2" => new Color(1f, 1f, 1f, 0.9f),
                        _ => Color.white
                    });
                }
                string path = $"{CalaneraLibreria.MatRoot}/{kv.Key}.mat";
                var old = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (old != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m, path);
                Mats[kv.Key] = m;
            }
        }

        // ------------------------------------------------------------------ suolo, piazzole, mare
        private static void Suolo()
        {
            // piazzale di banchina 40 × 17 m, tutto a quota 0
            B("M_Porto_Suolo").Box(new Vector3(-20, -0.35f, -99), new Vector3(20, 0, -82), bottom: false);
            // varco est verso la Banchina (4 m), molo, via Stretta e Salita del Porto:
            // il suolo continua sotto le case e oltre i varchi, così non restano "isole" sospese
            B("M_Porto_Suolo").Box(new Vector3(20, -0.35f, -95), new Vector3(30, 0, -87), bottom: false);
            B("M_Porto_Suolo").Box(new Vector3(-6, -0.35f, -100), new Vector3(6, 0, -99), bottom: false);
            B("M_Porto_Suolo").Box(new Vector3(-27, -0.35f, -99), new Vector3(-20, 0, -76), bottom: false);
            B("M_Porto_Suolo").Box(new Vector3(-4, -0.35f, -82), new Vector3(4, 0, -70), bottom: false);

            // fascia di cordolo in pietra all'imbocco dei magazzini (piatta, non sporge)
            var trim = B("M_Porto_Pietra");
            trim.Quad(new Vector3(-20, 0.006f, -99.9f), new Vector3(20, 0.006f, -99.9f),
                      new Vector3(20, 0.006f, -98.9f), new Vector3(-20, 0.006f, -98.9f), Vector3.up);

            // binari incassati a filo: due coppie da est a ovest
            var acciaio = B("M_Porto_Acciaio");
            foreach (float z in new[] { -90.10f, -91.50f })
                acciaio.Box(new Vector3(-20, -0.02f, z - 0.022f), new Vector3(20, 0.003f, z + 0.022f));

            // giunto del piazzale verso la salita: lastra di bordo
            B("M_Porto_Pietra").Box(new Vector3(-4.4f, -0.05f, -82.35f), new Vector3(4.4f, 0.015f, -82.0f));
        }

        /// <summary>
        /// SUOLO DELLA CITTÀ. Il blockout ha un unico volume "street" 180 × 200 × 1 m: al posto suo
        /// un piano di sampietrini 2 cm più basso del piazzale, così non z-fighta con lo spawn e
        /// non lascia buchi fuori dai varchi. Bordo di banchina in pietra sul mare a nord.
        /// </summary>
        private static void SuoloCitta()
        {
            var g = B("M_Sampietrini");
            g.Quad(new Vector3(-90, -0.02f, 100), new Vector3(90, -0.02f, 100),
                   new Vector3(90, -0.02f, -100), new Vector3(-90, -0.02f, -100), Vector3.up);
            // banchina in pietra sul mare (il piano copre tutta la mappa)
            B("M_Porto_Pietra").Box(new Vector3(-90, -2.2f, -100.4f), new Vector3(90, -0.02f, -100f));
        }

        private static void Piazzole()
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -8 + i * 4;
                var g = B("M_Porto_Pad");
                g.Box(new Vector3(x - 0.60f, 0f, -91.6f), new Vector3(x + 0.60f, 0.021f, -90.4f));
                var c = B("M_Porto_Ciano");
                float y = 0.0225f, w = 0.055f;
                // cornice ciano dipinta a filo: quattro lati
                c.Quad(new Vector3(x - 0.58f, y, -91.58f), new Vector3(x + 0.58f, y, -91.58f),
                       new Vector3(x + 0.58f, y, -91.58f + w), new Vector3(x - 0.58f, y, -91.58f + w), Vector3.up);
                c.Quad(new Vector3(x - 0.58f, y, -90.42f - w), new Vector3(x + 0.58f, y, -90.42f - w),
                       new Vector3(x + 0.58f, y, -90.42f), new Vector3(x - 0.58f, y, -90.42f), Vector3.up);
                c.Quad(new Vector3(x - 0.58f, y, -91.58f + w), new Vector3(x - 0.58f + w, y, -91.58f + w),
                       new Vector3(x - 0.58f + w, y, -90.42f - w), new Vector3(x - 0.58f, y, -90.42f - w), Vector3.up);
                c.Quad(new Vector3(x + 0.58f - w, y, -91.58f + w), new Vector3(x + 0.58f, y, -91.58f + w),
                       new Vector3(x + 0.58f, y, -90.42f - w), new Vector3(x + 0.58f - w, y, -90.42f - w), Vector3.up);
            }
        }

        private static void MoloEMare()
        {
            // molo in punta al piazzale (solo vista: sta dietro i magazzini)
            B("M_Porto_Suolo").Box(new Vector3(-6, -0.5f, -112), new Vector3(6, 0, -100), bottom: false);
            B("M_Porto_Pietra").Box(new Vector3(-6.3f, -2.6f, -112.3f), new Vector3(-6f, 0, -100));
            B("M_Porto_Pietra").Box(new Vector3(6f, -2.6f, -112.3f), new Vector3(6.3f, 0, -100));
            B("M_Porto_Pietra").Box(new Vector3(-6.3f, -2.6f, -112.3f), new Vector3(6.3f, 0, -112f));

            // mare a -2 m, griglia 72x28 per le onde verticali
            {
                var go = new GameObject("Mare");
                go.transform.SetParent(GameObject.Find("Spawn Nord")?.transform, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Mats["M_Porto_Mare"];
                const int nx = 72, nz = 28;
                var verts = new Vector3[(nx + 1) * (nz + 1)];
                var uvs = new Vector2[verts.Length];
                var tris = new int[nx * nz * 6];
                for (int iz = 0; iz <= nz; iz++)
                    for (int ix = 0; ix <= nx; ix++)
                    {
                        int i = iz * (nx + 1) + ix;
                        verts[i] = new Vector3(Mathf.Lerp(-180f, 180f, ix / (float)nx), -2f, Mathf.Lerp(-100f, -260f, iz / (float)nz));
                        uvs[i] = new Vector2(ix / (float)nx, iz / (float)nz);
                    }
                for (int iz = 0; iz < nz; iz++)
                    for (int ix = 0; ix < nx; ix++)
                    {
                        int i = iz * (nx + 1) + ix, k = (iz + 1) * (nx + 1) + ix;
                        int t = (iz * nx + ix) * 6;
                        tris[t] = i; tris[t + 1] = k; tris[t + 2] = i + 1;
                        tris[t + 3] = i + 1; tris[t + 4] = k; tris[t + 5] = k + 1;
                    }
                var mesh = new Mesh { name = "MareGriglia" };
                mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
                mesh.RecalculateNormals();
                mf.sharedMesh = mesh;
            }
            // schiuma contro la banchina
            var schiuma = B("D_Schiuma");
            schiuma.QuadUv(new Vector3(-180, -1.995f, -100.4f), new Vector3(180, -1.995f, -100.4f),
                           new Vector3(180, -1.995f, -103.4f), new Vector3(-180, -1.995f, -103.4f), Vector3.up);
        }

        // ------------------------------------------------------------------ i due container
        private static void Container()
        {
            ContainerUno(-16, -97, -94.6f, "M_Porto_LamieraBlu", 0.35f, "PV 047");
            ContainerUno(10, -97, -97 + 2.4f, "M_Porto_LamieraRuggine", -0.2f, "CL 12");
        }

        /// <summary>Container 6 × 2,4 × 2,6: corpo pieno, castelli agli angoli, porte con le barre.</summary>
        private static void ContainerUno(float x0, float zNord, float zSud, string lamiera, float stencilShift, string sigla)
        {
            float x1 = x0 + 6f, y1 = 2.6f, z0 = zNord, z1 = zSud;
            var g = B(lamiera);
            var ferro = B("M_Porto_Ferro");

            // corpo d'acciaio pieno: la grecatura la disegna la normal map della lamiera
            g.Box(new Vector3(x0, 0, z0), new Vector3(x1, y1, z1));
            // longheroni e montanti, più spessi del corpo
            g.Box(new Vector3(x0 - 0.02f, 0f, z0 - 0.02f), new Vector3(x1 + 0.02f, 0.16f, z1 + 0.02f));
            g.Box(new Vector3(x0 - 0.02f, y1 - 0.15f, z0 - 0.02f), new Vector3(x1 + 0.02f, y1, z1 + 0.02f));
            g.Box(new Vector3(x0 - 0.03f, 0.15f, z0 - 0.03f), new Vector3(x0 + 0.20f, y1 - 0.14f, z1 + 0.03f));
            g.Box(new Vector3(x1 - 0.20f, 0.15f, z0 - 0.03f), new Vector3(x1 + 0.03f, y1 - 0.14f, z1 + 0.03f));

            // castelli agli angoli
            foreach (float x in new[] { x0 - 0.03f, x1 - 0.23f })
                foreach (float y in new[] { 0f, y1 - 0.26f })
                    foreach (float z in new[] { z0 - 0.03f, z1 - 0.23f })
                        g.Box(new Vector3(x, y, z), new Vector3(x + 0.26f, y + 0.26f, z + 0.26f));

            // porte sul lato nord, appena fuori dal corpo
            g.Box(new Vector3(x0 + 0.24f, 0.18f, z0 - 0.07f), new Vector3(x0 + 2.86f, y1 - 0.18f, z0 - 0.03f));
            g.Box(new Vector3(x0 + 3.14f, 0.18f, z0 - 0.07f), new Vector3(x1 - 0.24f, y1 - 0.18f, z0 - 0.03f));
            // quattro barre di chiusura e le cerniere
            for (int i = 0; i < 4; i++)
            {
                float x = x0 + 0.62f + i * 1.58f;
                ferro.Cylinder(new Vector3(x, 0.32f, z0 - 0.12f), new Vector3(x, y1 - 0.30f, z0 - 0.12f), 0.028f, 8);
                ferro.Box(new Vector3(x - 0.10f, 1.02f, z0 - 0.16f), new Vector3(x + 0.10f, 1.26f, z0 - 0.08f));
            }
            foreach (float x in new[] { x0 + 0.30f, x1 - 0.36f })
                ferro.Box(new Vector3(x, 0.25f, z0 - 0.13f), new Vector3(x + 0.10f, y1 - 0.25f, z0 - 0.09f));

            // danza di ruggine sotto i bordi (decal)
            for (int i = 0; i < 3; i++)
            {
                float x = x0 + 1.1f + i * 1.9f;
                Ruggine(new Vector3(x, 2.1f, z1 + 0.012f), Vector3.forward, 0.42f, 0.9f);
            }
            // sigla della compagnia sulla testata est (piatta), leggibile da fuori
            var d = B("D_Stencil");
            float sx = x1 + 0.012f;
            d.QuadUv(new Vector3(sx, 1.55f, z1 - 1.72f), new Vector3(sx, 1.55f, z1 - 0.32f),
                     new Vector3(sx, 1.95f, z1 - 0.32f), new Vector3(sx, 1.95f, z1 - 1.72f), Vector3.right);
        }

        /// <summary>Lamiera grecata fra due punti (anche in verticale), normale verso "out".</summary>
        private static void Grecato(Geo g, Vector3 a, Vector3 b, Vector3 outN, float passo, float profondita)
        {
            Vector3 dir = b - a;
            dir.y = 0f;
            float len = dir.magnitude;
            if (len < 1e-3f) return;
            dir /= len;
            Vector3 nOut = outN.normalized;
            float ya = a.y, yb = b.y;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / passo));
            float p = len / n;
            Vector3 P(float u, float d, float y) => a + dir * u + nOut * d + Vector3.up * (y - ya);
            for (int i = 0; i < n; i++)
            {
                float u0 = i * p, u1 = u0 + 0.42f * p, u2 = u1 + 0.08f * p, u3 = u2 + 0.42f * p, u4 = (i + 1) * p;
                g.Quad(P(u0, 0, ya), P(u1, 0, ya), P(u1, 0, yb), P(u0, 0, yb), nOut);
                g.Quad(P(u1, 0, ya), P(u2, profondita, ya), P(u2, profondita, yb), P(u1, 0, yb), dir);
                g.Quad(P(u2, profondita, ya), P(u3, profondita, ya), P(u3, profondita, yb), P(u2, profondita, yb), nOut);
                g.Quad(P(u3, profondita, ya), P(u4, 0, ya), P(u4, 0, yb), P(u3, profondita, yb), -dir);
            }
        }

        private static void Ruggine(Vector3 center, Vector3 normal, float w, float h)
        {
            var g = B("D_Ruggine");
            Vector3 right = Vector3.Cross(Vector3.up, normal).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            Vector3 up = Vector3.Cross(normal, right).normalized;
            g.QuadUv(center - right * w * 0.5f - up * h, center + right * w * 0.5f - up * h,
                     center + right * w * 0.5f, center - right * w * 0.5f, normal);
        }

        // ------------------------------------------------------------------ le pile di casse
        private static void Cassette()
        {
            CassaPila(new Vector3(-11.9f, 0, -86.4f), new Vector3(2.4f, 1.0f, 2.4f), 3);
            CassaPila(new Vector3(9.5f, 0, -86.4f), new Vector3(2.4f, 1.0f, 2.4f), 8);
            // cassette sparse ai piedi dei magazzini
            Cassa(new Vector3(-7.3f, 0, -97.9f), 0.8f, 0.28f, 0.5f, -12f);
            Cassa(new Vector3(-6.4f, 0, -98.0f), 0.8f, 0.28f, 0.5f, 4f);
            Cassa(new Vector3(7.6f, 0, -97.8f), 0.8f, 0.28f, 0.5f, 18f);
        }

        private static void CassaPila(Vector3 o, Vector3 size, int seed)
        {
            var rnd = new System.Random(seed);
            var legno = B("M_Porto_LegnoChiaro");
            var ferro = B("M_Porto_Ferro");
            // base: quattro casse separate (con il vuoto in mezzo, così si leggono una per una)
            for (int i = 0; i < 4; i++)
            {
                float x = o.x + (i % 2) * 1.18f + (float)rnd.NextDouble() * 0.05f;
                float z = o.z + (i / 2) * 1.18f + (float)rnd.NextDouble() * 0.05f;
                float rot = (float)(rnd.NextDouble() * 10 - 5);
                Cassa(new Vector3(x, o.y, z), 0.94f, 0.42f, 0.94f, rot, legno, ferro);
            }
            // regge di ferro fra le casse
            ferro.Box(new Vector3(o.x + 0.35f, o.y + 0.40f, o.z + 0.55f), new Vector3(o.x + 1.55f, o.y + 0.43f, o.z + 0.60f));
        }

        /// <summary>Le casse da pesce di Meshy (12k triangoli): due per pila, appoggiate sopra la base.</summary>
        private static void CasseMeshy(Transform root)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Calanera/Props/casse_pesce.glb");
            if (prefab == null) { Debug.LogWarning("Spawn Nord: manca il GLB delle casse Meshy"); return; }
            var posti = new (Vector3 pos, float rot, float scale)[]
            {
                (new Vector3(-11.15f, 0.42f, -85.85f), 8f, 0.92f),
                (new Vector3(-10.15f, 0.42f, -86.2f), -24f, 0.86f),
                (new Vector3(10.35f, 0.42f, -85.9f), -12f, 0.92f),
                (new Vector3(11.25f, 0.42f, -86.25f), 32f, 0.86f),
                (new Vector3(-3.25f, 0f, -81.02f), 18f, 0.95f),
                (new Vector3(13.65f, 0f, -81.15f), -35f, 0.88f),
            };
            foreach (var p in posti)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "Cassa da pesce (Meshy)";
                go.transform.SetParent(root, false);
                go.transform.localPosition = p.pos;
                go.transform.localRotation = Quaternion.Euler(0, p.rot, 0);
                go.transform.localScale = Vector3.one * p.scale;
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    mr.gameObject.isStatic = true;
                }
            }
        }

        private static void Cassa(Vector3 o, float w, float h, float d, float rotDeg, Geo legno = null, Geo ferro = null)
        {
            legno ??= B("M_Porto_LegnoChiaro");
            ferro ??= B("M_Porto_Ferro");
            var rot = Quaternion.Euler(0, rotDeg, 0);
            Vector3 c = o + new Vector3(w * 0.5f, h * 0.5f, d * 0.5f);
            // cassetta a doghe: quattro lati con listelli
            legno.BoxR(c, new Vector3(w, h, d), rot);
            // regge di ferro a metà e sotto il bordo
            foreach (float t in new[] { 0.18f, 0.82f })
            {
                Vector3 p = o + rot * new Vector3(w * 0.5f, h * t, d * 0.5f);
                legno.BoxR(p, new Vector3(w + 0.02f, 0.035f, d + 0.02f), rot);
            }
            // coperchio appena staccato su alcune
            legno.BoxR(o + rot * new Vector3(w * 0.5f, h + 0.015f, d * 0.5f), new Vector3(w * 0.98f, 0.03f, d * 0.98f), rot);
        }

        // ------------------------------------------------------------------ la gru
        private static void Gru()
        {
            var g = B("M_Porto_Gru");
            var ferro = B("M_Porto_Ferro");
            Vector3 baseC = new Vector3(21.5f, 0, -101.2f);
            float h = 30f, half = 1.15f;
            // basamento
            B("M_Porto_Pietra").Box(new Vector3(baseC.x - 2.2f, 0, baseC.z - 2.2f), new Vector3(baseC.x + 2.2f, 0.5f, baseC.z + 2.2f));
            B("M_Porto_Pietra").Box(new Vector3(baseC.x - 1.6f, 0.5f, baseC.z - 1.6f), new Vector3(baseC.x + 1.6f, 0.75f, baseC.z + 1.6f));
            // quattro montanti
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                {
                    Vector3 p = baseC + new Vector3(sx * half, 0f, sz * half);
                    g.Box(new Vector3(p.x - 0.11f, 0.7f, p.z - 0.11f), new Vector3(p.x + 0.11f, h, p.z + 0.11f));
                }
            // anelli e croci di controvento
            for (float y = 3.2f; y < h; y += 3.0f)
            {
                Vector3 a = new(baseC.x - half, y, baseC.z - half), b = new(baseC.x + half, y, baseC.z + half);
                g.Beam(new Vector3(a.x, y, a.z), new Vector3(b.x, y, a.z), 0.10f, 0.10f, Vector3.up);
                g.Beam(new Vector3(a.x, y, b.z), new Vector3(b.x, y, b.z), 0.10f, 0.10f, Vector3.up);
                g.Beam(new Vector3(a.x, y, a.z), new Vector3(a.x, y, b.z), 0.10f, 0.10f, Vector3.up);
                g.Beam(new Vector3(b.x, y, a.z), new Vector3(b.x, y, b.z), 0.10f, 0.10f, Vector3.up);
                float y2 = y - 3.0f;
                foreach (var (x, z) in new[] { (a.x, a.z), (b.x, a.z), (b.x, b.z), (a.x, b.z) })
                {
                    Vector3 p0 = new(x, y2, z), p1 = new(x, y, z);
                    Vector3 altro = new(x + (Mathf.Abs(x - baseC.x) > 0.1f ? -2 * (x - baseC.x) : 0), y2, z + (z > baseC.z ? -2 : 2) * half);
                    g.Beam(p1, altro, 0.08f, 0.08f, Vector3.up);
                }
            }
            // braccio: due correnti inferiori e uno superiore, triangolo
            float yJib = h, xTip = -6.5f, xRoot = baseC.x + 0.4f;
            g.Beam(new Vector3(xRoot, yJib, baseC.z - 0.72f), new Vector3(xTip, yJib, baseC.z - 0.72f), 0.12f, 0.12f, Vector3.up);
            g.Beam(new Vector3(xRoot, yJib, baseC.z + 0.72f), new Vector3(xTip, yJib, baseC.z + 0.72f), 0.12f, 0.12f, Vector3.up);
            g.Beam(new Vector3(xRoot, yJib + 1.5f, baseC.z), new Vector3(xTip, yJib + 0.6f, baseC.z), 0.11f, 0.11f, Vector3.up);
            for (int i = 0; i <= 18; i++)
            {
                float t = i / 18f, x = Mathf.Lerp(xRoot, xTip, t), y = Mathf.Lerp(yJib + 1.5f, yJib + 0.6f, t);
                g.Beam(new Vector3(x, y, baseC.z), new Vector3(x, yJib, baseC.z - 0.72f), 0.07f, 0.07f, Vector3.up);
                g.Beam(new Vector3(x, y, baseC.z), new Vector3(x, yJib, baseC.z + 0.72f), 0.07f, 0.07f, Vector3.up);
                if (i < 18)
                {
                    float t2 = (i + 1) / 18f, x2 = Mathf.Lerp(xRoot, xTip, t2);
                    g.Beam(new Vector3(x, yJib, baseC.z - 0.72f), new Vector3(x2, yJib, baseC.z + 0.72f), 0.055f, 0.055f, Vector3.up);
                }
            }
            // contrappeso e argano
            g.Beam(new Vector3(xRoot, yJib, baseC.z), new Vector3(xRoot + 8f, yJib, baseC.z), 0.16f, 0.16f, Vector3.up);
            g.Box(new Vector3(xRoot + 5.0f, yJib - 2.0f, baseC.z - 1.1f), new Vector3(xRoot + 7.6f, yJib - 0.4f, baseC.z + 1.1f));
            // cabina
            g.Box(new Vector3(baseC.x - 2.2f, 27.4f, baseC.z - 0.9f), new Vector3(baseC.x - 0.5f, 29.6f, baseC.z + 0.9f));
            B("M_Porto_Vetro").Box(new Vector3(baseC.x - 2.22f, 28.3f, baseC.z - 0.55f), new Vector3(baseC.x - 2.10f, 29.2f, baseC.z + 0.55f));

            // torretta di punta e tiranti
            g.Beam(new Vector3(baseC.x, h, baseC.z), new Vector3(baseC.x, h + 3.4f, baseC.z), 0.16f, 0.16f, Vector3.up);
            ferro.Catenary(new Vector3(baseC.x, h + 3.4f, baseC.z), new Vector3(xTip + 0.8f, yJib + 0.6f, baseC.z), 0.35f, 0.028f, 12);
            ferro.Catenary(new Vector3(baseC.x, h + 3.4f, baseC.z), new Vector3(xRoot + 7.4f, yJib + 0.1f, baseC.z), 0.3f, 0.028f, 10);

            // carrello e gancio sopra il piazzale
            float trolleyX = 1.6f;
            ferro.Box(new Vector3(trolleyX - 0.35f, yJib - 0.35f, baseC.z - 0.35f), new Vector3(trolleyX + 0.35f, yJib + 0.15f, baseC.z + 0.35f));
            ferro.Cylinder(new Vector3(trolleyX, yJib - 0.35f, baseC.z), new Vector3(trolleyX, 16.5f, baseC.z), 0.022f, 6);
            ferro.Box(new Vector3(trolleyX - 0.30f, 15.9f, baseC.z - 0.30f), new Vector3(trolleyX + 0.30f, 16.5f, baseC.z + 0.30f));
            ferro.Torus(new Vector3(trolleyX, 15.72f, baseC.z), Vector3.up, 0.22f, 0.045f, 12, 6);
        }

        // ------------------------------------------------------------------ luce
        private static void Luce()
        {
            var sole = GameObject.Find("Sole");
            if (sole != null)
            {
                // sole da ovest, un filo più alto: il piazzale resta illuminato fra i palazzi
                sole.transform.rotation = Quaternion.Euler(38f, 96f, 0f);
                var l = sole.GetComponent<Light>();
                if (l != null) { l.intensity = 1.00f; l.color = new Color(1.00f, 0.97f, 0.94f); }
                if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_SunDir"))
                    RenderSettings.skybox.SetVector("_SunDir", -sole.transform.forward);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.64f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.70f, 0.67f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.36f, 0.33f);
            RenderSettings.ambientIntensity = 0.85f;
            DynamicGI.UpdateEnvironment();
            CalaneraRenderer.EnsureSsao(0.45f, 0.6f);

            var gru = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/M_Porto_Gru.mat");
            if (gru != null) { gru.SetColor("_L0_Tint", new Color(0.86f, 0.82f, 0.68f)); EditorUtility.SetDirty(gru); }
            var vetro = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/M_Porto_Vetro.mat");
            if (vetro != null) { vetro.SetColor("_BaseColor", new Color(0.24f, 0.29f, 0.31f)); EditorUtility.SetDirty(vetro); }

            var suolo = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/M_Porto_Suolo.mat");
            if (suolo != null)
            {
                suolo.SetColor("_L0_Tint", new Color(0.92f, 0.90f, 0.86f));
                suolo.SetFloat("_L0_NormalStrength", 0.15f);
                suolo.SetFloat("_L0_Occlusion", 0.40f);
                suolo.SetFloat("_L0_Smoothness", 0.35f);
                suolo.SetFloat("_MacroStrength", 0.25f);
                EditorUtility.SetDirty(suolo);
            }
        }

        // ------------------------------------------------------------------ blockout e viste
        /// <summary>
        /// Sonda di riflessione del porto: la usano i vetri (M_Porto_Vetro) per riflettere
        /// il cielo e le facciate. Senza sonda il vetro dielettrico ha solo il 4% di speculare
        /// e non ha nulla da campionare: restava nero.
        /// </summary>
        /// <summary>
        /// I vetri erano dielettrici scuri senza sonda di riflessione: il 4% di Fresnel
        /// non aveva nulla da campionare e restavano neri. Ora sono un vetro metallico
        /// (metallic 0,85, smoothness 0,96) e la sonda "Riflessi Porto" ci riflette il cielo.
        /// </summary>
        private static void Vetri()
        {
            var glass = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/M_Porto_Vetro.mat");
            if (glass == null) return;
            glass.shader = Shader.Find("Universal Render Pipeline/Lit");
            glass.SetColor("_BaseColor", new Color(0.035f, 0.06f, 0.075f, 1f));
            glass.SetFloat("_Metallic", 0.85f);
            glass.SetFloat("_Smoothness", 0.96f);
            glass.SetFloat("_Surface", 0f);
            glass.SetFloat("_Blend", 0f);
            glass.SetFloat("_ZWrite", 1f);
            glass.renderQueue = 2000;
            EditorUtility.SetDirty(glass);
        }

        private static void Riflessi()
        {
            var old = GameObject.Find("Riflessi Porto");
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject("Riflessi Porto");
            go.transform.position = new Vector3(0, 7f, -89f);
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            p.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            p.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            p.size = new Vector3(64f, 26f, 46f);
            p.resolution = 256;
            p.hdr = true;
            p.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            p.cullingMask = ~0;
            p.boxProjection = true;
            p.importance = 1;
            p.intensity = 1f;
            p.blendDistance = 1f;
            p.nearClipPlane = 0.2f;
            p.farClipPlane = 350f;
            p.RenderProbe();
        }

        private static void NascondiBlockout()
        {
            // il vestito ora copre TUTTA la mappa: il blockout non serve più, nemmeno
            // fuori dal porto. Prima si riaccendeva fuori dallo spawn e copriva l'arte.
            foreach (var b in Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude))
            {
                var r = b.GetComponent<MeshRenderer>();
                if (r != null) r.enabled = false;
                var col = b.GetComponent<BoxCollider>();
                if (col != null) col.enabled = false;
            }
            var segni = GameObject.Find("Spawn e punti");
            if (segni != null)
                foreach (var r in segni.GetComponentsInChildren<MeshRenderer>(true))
                    if (r != null) r.enabled = false;
        }

                private static void Viste()
        {
            var root = GameObject.Find("Viste");
            if (root == null) root = new GameObject("Viste");
            foreach (var t in root.transform.Cast<Transform>().Where(t => t.name.StartsWith("P")).ToList())
                Object.DestroyImmediate(t.gameObject);
            void V(string n, Vector3 pos, Vector3 look)
            {
                var t = new GameObject(n).transform;
                t.SetParent(root.transform);
                t.position = pos;
                t.rotation = Quaternion.LookRotation(look - pos);
            }
            V("P0_Porto", new Vector3(-1.0f, 1.7f, -88.4f), new Vector3(-9f, 5.8f, -98.2f));
            V("P1_Spawn", new Vector3(0, 1.7f, -90.8f), new Vector3(-7.5f, 2.2f, -84f));
            V("P2_Piazzale", new Vector3(8.5f, 1.7f, -85.2f), new Vector3(-2f, 4.4f, -98.5f));
            V("P3_Container", new Vector3(-11.5f, 1.6f, -90.5f), new Vector3(-13.2f, 1.5f, -96.3f));
            V("P4_Gru", new Vector3(-2.5f, 1.7f, -92.5f), new Vector3(9f, 25.5f, -100.5f));
            V("P5_Case", new Vector3(0, 1.7f, -90.2f), new Vector3(2.5f, 4.6f, -80.5f));
            V("P6_Casse", new Vector3(-12.6f, 1.45f, -88.4f), new Vector3(-10.8f, 0.8f, -85.4f));
            V("P7_Alto", new Vector3(0, 26, -79), new Vector3(0, 2, -94));
            V("P8_Molo", new Vector3(0f, 3.2f, -104f), new Vector3(0f, 7f, -125f));
            V("P9_Est", new Vector3(16.5f, 1.7f, -90.2f), new Vector3(21.5f, 3.2f, -95.5f));
            V("P10_Insegna", new Vector3(0, 1.7f, -90f), new Vector3(0, 12.6f, -98.8f));
        }
    }
}
