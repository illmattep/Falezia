using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    public static partial class CalaneraSpawn
    {
        private const string LetterDir = "Assets/Art/Calanera/Props/Insegna";
        private const string NeonMatDir = "Assets/Art/Calanera/Materials/Neon";

        /// <summary>
        /// FALEZIA fatta con Meshy: sette lettere vere (GLB con texture PBR), ognuna
        /// appesa alla facciata con la sua staffa di ferro imbullonata, il suo cavo e la
        /// canalina. Niente pannello unico. Le lettere 1 e 5 sfarfallano; la luce fredda
        /// proietta sulla parete. Il materiale è URP Lit con emissione dal colore del
        /// neon, così la texture Meshy resta leggibile.
        /// </summary>
        private static void InsegnaNeon(Facciata f, float cx, float y)
        {
            string word = "FALEZIA";
            var fontMat = new Dictionary<char, Material>();
            var steel = CalaneraLibreriaMat("M_Porto_NeroFumo");
            var metal = CalaneraLibreriaMat("M_Porto_Acciaio");

            var root = new GameObject("Insegna FALEZIA Meshy");
            var spawnRoot = GameObject.Find(RootName);
            if (spawnRoot != null) root.transform.SetParent(spawnRoot.transform, false);
            // local +Z = fuori dalla parete; local +X = sinistra di chi guarda
            root.transform.position = f.P(cx, y, 0.14f);
            root.transform.rotation = Quaternion.LookRotation(f.Out, Vector3.up);

            System.IO.Directory.CreateDirectory(NeonMatDir);
            var items = new List<(Renderer r, float width, float height, Vector3 center, GameObject go, char ch)>();

            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LetterDir}/{ch}.glb");
                if (prefab == null) { Debug.LogWarning("Meshy: manca la lettera " + ch); continue; }
                var inst = (GameObject)Object.Instantiate(prefab);
                inst.name = "Lettera " + ch;
                // misura a identità: i bounds sono locali, non mondiali (prima il centro
                // veniva letto nel mondo e la lettera finiva metri sotto l'insegna)
                var rs = inst.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) { Object.DestroyImmediate(inst); continue; }
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float scale = 1.45f / Mathf.Max(0.001f, b.size.y);
                inst.transform.SetParent(root.transform, false);
                inst.transform.localScale = Vector3.one * scale;

                if (!fontMat.TryGetValue(ch, out var mat))
                {
                    var src = rs[0].sharedMaterial;
                    var baseTex = src != null ? src.GetTexture("baseColorTexture") : null;
                    var normalTex = src != null ? src.GetTexture("normalTexture") : null;
                    string matPath = $"{NeonMatDir}/M_Neon_{ch}.mat";
                    mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Neon_" + ch }; AssetDatabase.CreateAsset(mat, matPath); }
                    mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                    mat.SetTexture("_BaseMap", baseTex);
                    mat.SetColor("_BaseColor", Color.white);
                    mat.SetFloat("_Metallic", 0.15f);
                    mat.SetFloat("_Smoothness", 0.55f);
                    if (normalTex != null) { mat.SetTexture("_BumpMap", normalTex); mat.EnableKeyword("_NORMALMAP"); }
                    mat.EnableKeyword("_EMISSION");
                    mat.SetTexture("_EmissionMap", baseTex);
                    mat.SetColor("_EmissionColor", new Color(1.25f, 2.65f, 4.1f, 1f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    EditorUtility.SetDirty(mat);
                    fontMat[ch] = mat;
                }
                foreach (var r in rs) r.sharedMaterial = mat;
                items.Add((rs[0], b.size.x * scale, b.size.y * scale, b.center, inst, ch));
            }

            if (items.Count == 0) { Debug.LogWarning("Insegna Meshy: nessuna lettera"); return; }

            float gap = 0.12f;
            float total = 0f;
            foreach (var it in items) total += it.width;
            total += gap * (items.Count - 1);
            float cursor = total * 0.5f;   // si parte da sinistra di chi guarda (local +X)
            var letters = new List<Renderer>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                float x = cursor - it.width * 0.5f;
                cursor -= it.width + gap;
                var t = it.go.transform;
                t.localPosition = new Vector3(x, -it.center.y * t.localScale.y, 0.26f);
                letters.Add(it.r);

                // staffa che va nel muro, piastra murale, bullone
                Cube("Staffa " + it.ch, root.transform, new Vector3(x, 0, 0.06f), new Vector3(0.065f, 0.065f, 0.40f), metal);
                Cube("Piastra muro " + it.ch, root.transform, new Vector3(x, 0, -0.105f), new Vector3(0.22f, 0.22f, 0.035f), metal);
                Cube("Bullone " + it.ch, root.transform, new Vector3(x, 0, -0.085f), new Vector3(0.10f, 0.10f, 0.025f), steel);
            }

            // canalina e scatole di derivazione
            float yC = -0.98f;
            Cyl("Canalina", root.transform, new Vector3(0, yC, -0.115f), 0.035f, total + 0.70f, Vector3.right, metal);
            for (int i = -1; i <= 1; i++)
                Cube("Scatola " + i, root.transform, new Vector3(i * total * 0.32f, yC, -0.11f), new Vector3(0.18f, 0.18f, 0.10f), steel);

            // luce fredda: proietta sulla facciata, cala con le lettere che sfarfallano
            var lgo = new GameObject("Luce insegna");
            lgo.transform.SetParent(root.transform, false);
            lgo.transform.localPosition = new Vector3(0, -0.10f, 0.95f);
            var glow = lgo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.56f, 0.78f, 1f);
            glow.range = 14f;
            glow.intensity = 18f;
            glow.shadows = LightShadows.None;
            glow.renderMode = LightRenderMode.ForcePixel;

            var flick = root.AddComponent<NeonSign>();
            flick.letters = letters.ToArray();
            flick.glow = glow;
            flick.flickerLetters = new[] { 1, 5 };
            flick.glowFlicker = 0.30f;
            flick.minWait = 3.5f;
            flick.maxWait = 10f;

            Debug.Log($"Insegna FALEZIA Meshy: {letters.Count} lettere, larghezza {total:0.00} m, texture PBR");
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        private static GameObject Cyl(string name, Transform parent, Vector3 localPos, float radius, float length, Vector3 axis, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }
    }
}
