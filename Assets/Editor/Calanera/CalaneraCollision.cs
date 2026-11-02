using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Dalla geometria VESTITA alla collisione di gioco.
    ///
    /// Il vestito è costruito in Unity come triangoli; qui si registrano le primitive solide
    /// (Box/BoxR/BoxF/Quad/Cylinder) e si esporta un sidecar `calanera.art.json` accanto alla
    /// mappa. La simulazione toglie le scatole del blockout nascoste dal vestito e aggiunge
    /// queste: la collisione di movimento diventa la geometria che si vede, non il volume
    /// invisibile. Le stesse mesh ricevono un MeshCollider per raggi e proiettili; i BoxCollider
    /// del blockout coperto vengono spenti.
    ///
    /// Le pareti sono spesso piane (spessore 0): vengono gonfiate al minimo di 12 cm perché
    /// una scatola con una dimensione nulla non collide.
    /// </summary>
    public static class CalaneraCollision
    {
        public const string SidecarName = "calanera.art.json";
        private const float MinThickness = 0.12f;

        private static readonly string[] SolidParts =
        {
            "Intonaco", "Calce", "Mattone", "Mattoni", "Pietra", "Lastre", "Coppi",
            "Calcestruzzo", "Sampietrini", "Legno", "Ferro", "Acciaio", "Lamiera", "Muro"
        };

        private static readonly string[] SkipParts =
        {
            "Vetro", "Tenda", "Insegna", "Segno", "Cavo", "Lampada", "Decal", "Fumo", "Casse"
        };

        private struct CBox
        {
            public Vector3 Min, Max;
            public Vector3 Size => Max - Min;
        }

        public static void Export(Dictionary<string, Geo> geos)
        {
            if (geos == null) return;

            var candidates = new List<CBox>();
            foreach (var kv in geos)
            {
                if (!Structural(kv.Key)) continue;
                foreach (var (min, max) in kv.Value.Solids)
                {
                    if (TryBox(min, max, out var b)) candidates.Add(b);
                }
            }

            // una faccia dentro una scatola più grande è ridondante (es. le 6 facce di un BoxF)
            var boxes = new List<CBox>();
            foreach (var b in candidates)
            {
                bool contained = false;
                foreach (var o in candidates)
                {
                    if (ReferenceEquals(b, o)) continue;
                    if (Contains(o, b) && o.Size.sqrMagnitude > b.Size.sqrMagnitude + 1e-4f) { contained = true; break; }
                }
                if (!contained) boxes.Add(b);
            }

            // togli i volumi del blockout coperti dal vestito (tranne street: è il suolo)
            var remove = new List<int>();
            // il blockout è sostituito del tutto: via TUTTE le scatole nascoste
            foreach (var bb in UnityEngine.Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude))
            {
                var r = bb.GetComponent<MeshRenderer>();
                if (r == null || r.enabled) continue;
                remove.Add(bb.index);
                var c = bb.GetComponent<BoxCollider>();
                if (c != null) c.enabled = false;
            }
            remove.Sort();

            // i collider Unity vanno sulle mesh visibili, non sul blockout nascosto
            var root = GameObject.Find("Spawn Nord");
            int colliders = 0;
            if (root != null)
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mat = mr.sharedMaterial;
                    if (mat == null || !Structural(mat.name)) continue;
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    var mc = mr.gameObject.GetComponent<MeshCollider>();
                    if (mc == null) mc = mr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    colliders++;
                }

            // scatole degli asset hero (fontana, banco, barca): collisione dal modello nuovo
            foreach (var h in UnityEngine.Object.FindObjectsByType<CalaneraHero>(FindObjectsInactive.Exclude))
            {
                var rs = h.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (TryBox(b.min, b.max, out var hb)) boxes.Add(hb);
            }

            var json = new StringBuilder(1 << 20);
            json.Append("{\"format\":1,\"note\":\"collisione del vestito, generata da CalaneraCollision — non modificare a mano\",");
            json.Append("\"remove\":[");
            for (int i = 0; i < remove.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(remove[i].ToString(CultureInfo.InvariantCulture));
            }
            json.Append("],\"boxes\":[");
            for (int i = 0; i < boxes.Count; i++)
            {
                var b = boxes[i];
                if (i > 0) json.Append(',');
                json.Append('[')
                    .Append(F(b.Min.x)).Append(',').Append(F(b.Min.y)).Append(',').Append(F(b.Min.z)).Append(',')
                    .Append(F(b.Size.x)).Append(',').Append(F(b.Size.y)).Append(',').Append(F(b.Size.z)).Append(",0]");
            }
            json.Append("]}\n");

            string data = json.ToString();
            foreach (var dir in new[]
                     {
                         Path.Combine(Application.dataPath, "StreamingAssets", "maps"),
                         Path.GetFullPath(Path.Combine(Application.dataPath, "..", "game", "maps"))
                     })
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, SidecarName), data);
            }

            Debug.Log($"Collisione vestito: {boxes.Count} scatole, {remove.Count} volumi blockout rimossi, {colliders} MeshCollider");
        }

        private static bool TryBox(Vector3 min, Vector3 max, out CBox b)
        {
            var size = max - min;
            if (size.x < 0) { (min.x, max.x) = (max.x, min.x); size.x = -size.x; }
            if (size.y < 0) { (min.y, max.y) = (max.y, min.y); size.y = -size.y; }
            if (size.z < 0) { (min.z, max.z) = (max.z, min.z); size.z = -size.z; }
            b = default;
            if (Mathf.Max(size.x, size.y, size.z) < 0.30f) return false;
            if (size.x < MinThickness) { float d = (MinThickness - size.x) * 0.5f; min.x -= d; max.x += d; }
            if (size.y < MinThickness) { float d = (MinThickness - size.y) * 0.5f; min.y -= d; max.y += d; }
            if (size.z < MinThickness) { float d = (MinThickness - size.z) * 0.5f; min.z -= d; max.z += d; }
            b = new CBox { Min = min, Max = max };
            return true;
        }

        private static bool Contains(CBox outer, CBox inner)
        {
            const float e = 0.002f;
            return inner.Min.x >= outer.Min.x - e && inner.Max.x <= outer.Max.x + e
                && inner.Min.y >= outer.Min.y - e && inner.Max.y <= outer.Max.y + e
                && inner.Min.z >= outer.Min.z - e && inner.Max.z <= outer.Max.z + e;
        }

        private static bool Structural(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (SkipParts.Any(name.Contains)) return false;
            return SolidParts.Any(name.Contains);
        }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
