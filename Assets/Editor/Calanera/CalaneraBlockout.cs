using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// BLOCKOUT IN UNITY (doc 29, F0).
    ///
    /// Importa: legge calanera.json e mette ogni scatola nella scena Assets/Scenes/Calanera.unity
    /// come GameObject con BoxCollider (sotto "Blockout/&lt;tipo&gt;"), più spawn e punti con nome.
    /// Esporta: riscrive SOLO l'elenco "boxes" del json; tutto il resto (decor, spawn, punti,
    /// rotte) resta com'è, carattere per carattere. Una scatola non toccata si riscrive identica,
    /// quindi importare ed esportare senza modifiche lascia il file uguale byte per byte
    /// (Blockout: verifica).
    ///
    /// Le rotte nel json sono state verificate dal generatore: se si spostano scatole che le
    /// toccano, vanno riverificate (lo dice il log dell'esportazione).
    /// </summary>
    public static class CalaneraBlockout
    {
        public const string ScenePath = "Assets/Scenes/Calanera.unity";
        public const string JsonPath = "Assets/StreamingAssets/maps/calanera.json";
        public const string GodotJsonPath = "game/maps/calanera.json";
        private const string MatDir = "Assets/Art/Calanera/Materials/Blockout";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------ lettura del json
        private class Parsed
        {
            public string text;
            public string[] kinds;
            public int boxesStart, boxesEnd;        // intervallo di testo con le righe delle scatole
            public List<string> boxLines = new();
            public List<(string name, Vector3 pos)> points = new();
            public List<Vector3> north = new(), south = new();
        }

        private static Parsed Parse(string text)
        {
            var p = new Parsed { text = text };
            // kinds: una riga sola
            int k = text.IndexOf("\"kinds\":", StringComparison.Ordinal);
            int k0 = text.IndexOf('[', k), k1 = text.IndexOf(']', k0);
            p.kinds = text.Substring(k0 + 1, k1 - k0 - 1).Split(',').Select(s => s.Trim().Trim('"')).ToArray();

            // boxes: una scatola per riga fino alla "]" che chiude l'elenco
            int b = text.IndexOf("\"boxes\":", StringComparison.Ordinal);
            int open = text.IndexOf('[', b);
            int lineStart = text.IndexOf('\n', open) + 1;
            p.boxesStart = lineStart;
            int pos = lineStart;
            while (true)
            {
                int eol = text.IndexOf('\n', pos);
                string line = text.Substring(pos, eol - pos);
                string t = line.Trim();
                if (!t.StartsWith("[")) { p.boxesEnd = pos; break; }
                p.boxLines.Add(line);
                pos = eol + 1;
            }

            // spawn e punti (solo per vederli in scena: non si esportano)
            foreach (var side in new[] { "north", "south" })
            {
                int s = text.IndexOf($"\"{side}\":", StringComparison.Ordinal);
                if (s < 0) continue;
                int s0 = text.IndexOf('[', s);
                int depth = 0, i = s0;
                for (; i < text.Length; i++) { if (text[i] == '[') depth++; else if (text[i] == ']' && --depth == 0) break; }
                var inner = text.Substring(s0 + 1, i - s0 - 1);
                foreach (var tri in inner.Split(new[] { "],", "]" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var n = Numbers(tri);
                    if (n.Length == 3) (side == "north" ? p.north : p.south).Add(new Vector3(n[0], n[1], n[2]));
                }
            }
            int pi = text.IndexOf("\"points\":", StringComparison.Ordinal);
            if (pi >= 0)
            {
                int pe = text.IndexOf("\"routes\":", pi, StringComparison.Ordinal);
                var block = text.Substring(pi, (pe > 0 ? pe : text.Length) - pi);
                int at = 0;
                while ((at = block.IndexOf("\"name\":", at, StringComparison.Ordinal)) >= 0)
                {
                    int q0 = block.IndexOf('"', at + 7) + 1, q1 = block.IndexOf('"', q0);
                    string name = block.Substring(q0, q1 - q0);
                    int ps = block.IndexOf("\"pos\":", q1, StringComparison.Ordinal);
                    int a0 = block.IndexOf('[', ps), a1 = block.IndexOf(']', a0);
                    var n = Numbers(block.Substring(a0 + 1, a1 - a0 - 1));
                    if (n.Length == 3) p.points.Add((name, new Vector3(n[0], n[1], n[2])));
                    at = a1;
                }
            }
            return p;
        }

        private static float[] Numbers(string s) =>
            s.Split(new[] { ',', '[', ']', ' ' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(x => float.Parse(x, NumberStyles.Float, Inv)).ToArray();

        // ------------------------------------------------------------------ import
        [MenuItem("Falezia/Calanera/Blockout: importa")]
        public static void MenuImport() => Debug.Log(Import());

        public static string Import()
        {
            string text = File.ReadAllText(JsonPath);
            var p = Parse(text);

            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            foreach (var name in new[] { "Blockout", "Spawn e punti" })
            {
                var old = GameObject.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }
            var root = new GameObject("Blockout");
            var groups = new Dictionary<string, Transform>();
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Directory.CreateDirectory(MatDir);

            for (int i = 0; i < p.boxLines.Count; i++)
            {
                var v = Numbers(p.boxLines[i]);
                var min = new Vector3(v[0], v[1], v[2]);
                var size = new Vector3(v[3], v[4], v[5]);
                string kind = p.kinds[(int)v[6]];
                if (!groups.TryGetValue(kind, out var g))
                {
                    g = new GameObject(kind).transform;
                    g.SetParent(root.transform);
                    groups[kind] = g;
                }
                var go = new GameObject($"{kind} {i}");
                go.transform.SetParent(g);
                go.transform.position = min + size * 0.5f;
                go.transform.localScale = size;
                var col = go.AddComponent<BoxCollider>();
                col.size = Vector3.one;
                go.AddComponent<MeshFilter>().sharedMesh = cube;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = KindMaterial(kind);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var bb = go.AddComponent<BlockoutBox>();
                bb.kind = kind;
                bb.index = i;
                bb.original = p.boxLines[i];
                bb.importedMin = min;
                bb.importedSize = size;
            }

            var marks = new GameObject("Spawn e punti").transform;
            void Mark(string n, Vector3 pos, Color c)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                m.name = n;
                m.transform.SetParent(marks);
                m.transform.position = pos + Vector3.up * 1f;
                m.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                UnityEngine.Object.DestroyImmediate(m.GetComponent<Collider>());
                m.GetComponent<MeshRenderer>().sharedMaterial = ColorMaterial("Segno_" + ColorUtility.ToHtmlStringRGB(c), c);
            }
            foreach (var s in p.north) Mark("Spawn Nord", s, new Color(0.18f, 0.61f, 0.90f));
            foreach (var s in p.south) Mark("Spawn Sud", s, new Color(0.91f, 0.39f, 0.18f));
            foreach (var (n, pos) in p.points) Mark(n, pos, new Color(0.95f, 0.76f, 0.19f));

            // luce e inquadrature fisse per i resoconti (solo la prima volta: poi si ritoccano a mano)
            if (GameObject.Find("Sole") == null) CalaneraBanco.Luce();
            if (GameObject.Find("Viste") == null)
            {
                var viste = new GameObject("Viste").transform;
                void V(string n, Vector3 pos, Vector3 look)
                {
                    var t = new GameObject(n).transform;
                    t.SetParent(viste);
                    t.position = pos;
                    t.rotation = Quaternion.LookRotation(look - pos);
                }
                V("0_Alto", new Vector3(0, 175, -95), new Vector3(0, 0, 8));
                V("0_Obliqua", new Vector3(-110, 70, -110), new Vector3(0, 0, 0));
                int k = 1;
                foreach (var (n, pos) in p.points)
                {
                    if (k > 6) break;
                    var eye = pos + Vector3.up * 1.7f;
                    var look = new Vector3(0, 1.5f, 0);
                    if ((look - eye).sqrMagnitude < 4f) look = eye + Vector3.forward * 10f;
                    V($"{k}_{n}", eye, look);
                    k++;
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            return $"Calanera: blockout importato ({p.boxLines.Count} scatole, {p.kinds.Length} tipi, {p.points.Count} punti) in {ScenePath}";
        }

        // ------------------------------------------------------------------ export
        [MenuItem("Falezia/Calanera/Blockout: esporta")]
        public static void MenuExport() => Debug.Log(Export(JsonPath, true));

        [MenuItem("Falezia/Calanera/Blockout: verifica (andata e ritorno)")]
        public static void MenuVerify() => Debug.Log(Verify());

        /// <summary>Esporta in un file temporaneo e confronta col json attuale.</summary>
        public static string Verify()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "calanera_verifica.json");
            string r = Export(tmp, false);
            bool same = File.ReadAllBytes(tmp).SequenceEqual(File.ReadAllBytes(JsonPath));
            return (same ? "IDENTICO byte per byte. " : "DIVERSO dal json attuale. ") + r;
        }

        public static string Export(string outPath, bool alsoGodot)
        {
            string text = File.ReadAllText(JsonPath);
            var p = Parse(text);
            var kindIndex = p.kinds.Select((k, i) => (k, i)).ToDictionary(t => t.k, t => t.i);

            var boxes = UnityEngine.Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude);
            if (boxes.Length == 0) return "nessuna scatola nella scena: apri Calanera.unity";

            var byIndex = new SortedDictionary<int, BlockoutBox>();
            var added = new List<BlockoutBox>();
            foreach (var b in boxes)
            {
                if (b.index >= 0 && !byIndex.ContainsKey(b.index)) byIndex[b.index] = b;
                else added.Add(b);   // nuove o duplicate (Ctrl+D copia anche l'indice)
            }
            added.Sort((a, c) => string.CompareOrdinal(a.name, c.name));

            int changed = 0, removed = p.boxLines.Count - byIndex.Count;
            var lines = new List<string>();
            var errors = new List<string>();
            foreach (var kv in byIndex)
            {
                var b = kv.Value;
                b.GetBounds(out var min, out var size);
                bool same = !string.IsNullOrEmpty(b.original) && Close(min, b.importedMin) && Close(size, b.importedSize)
                            && p.kinds[(int)Numbers(b.original)[6]] == b.kind;
                if (same) { lines.Add(b.original); continue; }
                if (!kindIndex.TryGetValue(b.kind, out int ki)) { errors.Add($"{b.name}: tipo '{b.kind}' sconosciuto"); continue; }
                lines.Add("    " + Line(min, size, ki));
                changed++;
            }
            foreach (var b in added)
            {
                b.GetBounds(out var min, out var size);
                if (!kindIndex.TryGetValue(b.kind, out int ki)) { errors.Add($"{b.name}: tipo '{b.kind}' sconosciuto"); continue; }
                lines.Add("    " + Line(min, size, ki));
            }
            if (errors.Count > 0) return "ESPORTAZIONE ANNULLATA: " + string.Join("; ", errors);

            // la virgola: tutte le righe la vogliono tranne l'ultima
            for (int i = 0; i < lines.Count; i++)
            {
                string l = lines[i].TrimEnd();
                if (l.EndsWith(",")) l = l.Substring(0, l.Length - 1);
                lines[i] = i < lines.Count - 1 ? l + "," : l;
            }
            var sb = new StringBuilder();
            sb.Append(text, 0, p.boxesStart);
            foreach (var l in lines) sb.Append(l).Append('\n');
            sb.Append(text, p.boxesEnd, text.Length - p.boxesEnd);
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));

            if (alsoGodot && File.Exists(GodotJsonPath)) File.WriteAllText(GodotJsonPath, sb.ToString(), new UTF8Encoding(false));
            if (alsoGodot) AssetDatabase.Refresh();

            string note = (changed + added.Count + removed) > 0
                ? " Le rotte del json vanno riverificate (tools/mapgen)."
                : "";
            return $"esportate {lines.Count} scatole: {changed} modificate, {added.Count} nuove, {removed} tolte → {outPath}.{note}";
        }

        private static bool Close(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-6f;

        /// <summary>Numeri come li scrive il generatore Python: interi senza decimali, il resto al centimetro.</summary>
        private static string Num(float f)
        {
            float r = Mathf.Round(f * 1000f) / 1000f;
            if (Mathf.Abs(r - Mathf.Round(r)) < 1e-6f) return ((int)Mathf.Round(r)).ToString(Inv);
            return r.ToString("0.###", Inv);
        }

        private static string Line(Vector3 min, Vector3 size, int kind) =>
            $"[{Num(min.x)},{Num(min.y)},{Num(min.z)},{Num(size.x)},{Num(size.y)},{Num(size.z)},{kind}]";

        // ------------------------------------------------------------------ materiali di servizio
        private static Material KindMaterial(string kind)
        {
            var c = kind switch
            {
                "street" => new Color(0.45f, 0.44f, 0.42f),
                "plaster" => new Color(0.85f, 0.70f, 0.52f),
                "plaster_ochre" => new Color(0.80f, 0.58f, 0.34f),
                "stone" => new Color(0.78f, 0.75f, 0.66f),
                "rock" => new Color(0.50f, 0.48f, 0.45f),
                "concrete" => new Color(0.55f, 0.54f, 0.52f),
                "steel" => new Color(0.35f, 0.37f, 0.40f),
                "wood" => new Color(0.55f, 0.42f, 0.29f),
                "cover" => new Color(0.70f, 0.67f, 0.60f),
                "stairs" => new Color(0.74f, 0.70f, 0.61f),
                "roof" => new Color(0.62f, 0.36f, 0.25f),
                "objective" => new Color(0.95f, 0.76f, 0.19f),
                "team_north" => new Color(0.18f, 0.61f, 0.90f),
                "team_south" => new Color(0.91f, 0.39f, 0.18f),
                _ => new Color(0.6f, 0.6f, 0.6f),
            };
            return ColorMaterial("Blockout_" + kind, c);
        }

        private static Material ColorMaterial(string name, Color c)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ vista
        [MenuItem("Falezia/Calanera/Blockout: mostra o nascondi")]
        public static void Toggle()
        {
            var root = GameObject.Find("Blockout");
            if (root == null) return;
            var rs = root.GetComponentsInChildren<MeshRenderer>(true);
            bool on = rs.Length > 0 && !rs[0].enabled;
            foreach (var r in rs) r.enabled = on;
        }
    }
}
