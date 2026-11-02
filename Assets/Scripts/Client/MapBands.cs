using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// FASCIA DIPINTA ALLA BASE DEGLI EDIFICI.
///
/// Il dettaglio che fa leggere una parete come "casa di paese" invece che come scatola:
/// uno zoccolo alto ~0,9 m, leggermente in fuori, di un colore diverso dall'intonaco —
/// ocra, rosso pompeiano, grigio-azzurro, oliva.
///
/// Sta in un componente suo e non tocca né <see cref="MapBuilder"/> né
/// <see cref="MapDressing"/>: legge gli stessi volumi del mondo, costruisce una fascia per
/// ogni edificio e la fonde in poche mesh. Se un giorno la fascia non serve, si spegne con
/// <see cref="SetDetail"/> e la mappa resta identica.
/// </summary>
public static class MapBands
{
    private const float Height = 0.95f;   // quanto sale lo zoccolo
    private const float Proud = 0.045f;   // quanto sporge dal muro

    private static readonly Color[] Palette =
    {
        new(0.52f, 0.26f, 0.18f),   // rosso pompeiano
        new(0.30f, 0.34f, 0.36f),   // grigio-azzurro
        new(0.44f, 0.36f, 0.20f),   // ocra scuro
        new(0.28f, 0.33f, 0.24f),   // oliva
        new(0.58f, 0.52f, 0.44f),   // sabbia
        new(0.24f, 0.24f, 0.26f),   // ardesia
    };

    private static GameObject _root;
    private static Material _mat;

    public static void SetDetail(bool on)
    {
        if (_root != null) _root.SetActive(on);
    }

    public static void Build(WorldState world, Transform parent)
    {
        if (_root != null) Object.Destroy(_root);
        _root = new GameObject("Fasce");
        _root.transform.SetParent(parent, false);

        var buckets = new Dictionary<int, (List<Vector3> p, List<Vector3> n, List<int> t)>();

        foreach (var b in world.Boxes)
        {
            string kind = b.Kind < world.Kinds.Length ? world.Kinds[b.Kind] : "";
            if (kind != "plaster" && kind != "plaster_ochre" && kind != "concrete") continue;

            float h = b.Size.Y;
            if (h < 3.5f) continue;                     // tetti, parapetti, sottoscala: no fascia

            float x0 = b.Min.X, z0 = b.Min.Z;
            float x1 = x0 + b.Size.X, z1 = z0 + b.Size.Z;

            int seed = Mathf.Abs((int)x0 * 73856093 ^ (int)z0 * 19349663);
            int ci = seed % Palette.Length;
            if (!buckets.TryGetValue(ci, out var g))
                buckets[ci] = g = (new List<Vector3>(), new List<Vector3>(), new List<int>());

            // quattro lati, ognuno un quad
            AddQuad(g, new Vector3(x0 - Proud, 0f, z0 - Proud), new Vector3(x1 + Proud, 0f, z0 - Proud),
                       new Vector3(x1 + Proud, Height, z0 - Proud), new Vector3(x0 - Proud, Height, z0 - Proud),
                       Vector3.back);
            AddQuad(g, new Vector3(x1 + Proud, 0f, z1 + Proud), new Vector3(x0 - Proud, 0f, z1 + Proud),
                       new Vector3(x0 - Proud, Height, z1 + Proud), new Vector3(x1 + Proud, Height, z1 + Proud),
                       Vector3.forward);
            AddQuad(g, new Vector3(x0 - Proud, 0f, z1 + Proud), new Vector3(x0 - Proud, 0f, z0 - Proud),
                       new Vector3(x0 - Proud, Height, z0 - Proud), new Vector3(x0 - Proud, Height, z1 + Proud),
                       Vector3.left);
            AddQuad(g, new Vector3(x1 + Proud, 0f, z0 - Proud), new Vector3(x1 + Proud, 0f, z1 + Proud),
                       new Vector3(x1 + Proud, Height, z1 + Proud), new Vector3(x1 + Proud, Height, z0 - Proud),
                       Vector3.right);
        }

        if (_mat == null)
        {
            _mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Fascia" };
            _mat.SetFloat("_Smoothness", 0.10f);
            _mat.SetFloat("_Metallic", 0f);
            _mat.enableInstancing = true;
        }

        int quads = 0;
        foreach (var pair in buckets)
        {
            var g = pair.Value;
            if (g.p.Count == 0) continue;

            var mesh = new Mesh { name = "Fascia_" + pair.Key };
            mesh.indexFormat = g.p.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(g.p);
            mesh.SetNormals(g.n);
            mesh.SetTriangles(g.t, 0);
            mesh.RecalculateBounds();

            var mat = new Material(_mat) { name = "Fascia_" + pair.Key };
            mat.color = Palette[pair.Key];
            var go = new GameObject("Fascia_" + pair.Key);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            quads += g.p.Count / 4;
        }

        Debug.Log($"MapBands: {quads} fasce su {buckets.Count} tinte");
    }

    private static void AddQuad((List<Vector3> p, List<Vector3> n, List<int> t) g,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        int i = g.p.Count;
        g.p.Add(a); g.p.Add(b); g.p.Add(c); g.p.Add(d);
        for (int k = 0; k < 4; k++) g.n.Add(normal);
        // verso antiorario visto da fuori: con l'altro verso il culling li scarta
        g.t.Add(i); g.t.Add(i + 2); g.t.Add(i + 1);
        g.t.Add(i); g.t.Add(i + 3); g.t.Add(i + 2);
    }
}
