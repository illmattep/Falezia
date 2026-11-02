using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DA MATERIALE glTF A MATERIALE URP.
///
/// glTFast crea i materiali con lo shader del pacchetto (`Shader Graphs/glTF-pbrMetallicRoughness`),
/// che sotto URP **non disegna le texture**: gli oggetti uscivano bianchi (i prop) o arancioni
/// piatti (le mani del player). Le texture però ci sono, sono nel materiale: il problema è solo
/// lo shader.
///
/// Qui ogni renderer viene rimappato su `Universal Render Pipeline/Lit`:
///   · colore di base, mappa di base, normal map;
///   · metallic e smoothness come costanti (la mappa MR di glTF è G=roughness/B=metallic,
///     URP la vuole R=metallic/A=smoothness: convertirla a mano non vale il rischio);
///   · niente `HasProperty`, che sui materiali di Shader Graph risponde di no:
///     le texture si cercano per nome fra quelle che il materiale dichiara.
///
/// È idempotente: chiamarla due volte non fa danni.
/// </summary>
public static class GltfMaterials
{
    private static Shader _lit;
    private static readonly Dictionary<Material, Material> Cache = new();
    private static bool _logged;

    private static Shader Lit => _lit != null ? _lit : _lit = Shader.Find("Universal Render Pipeline/Lit");

    public static void ToUrp(GameObject root)
    {
        if (root == null || Lit == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = Convert(r.sharedMaterial);
    }

    public static Material Convert(Material src)
    {
        if (src == null || Lit == null) return src;
        if (src.shader == Lit) return src;
        if (src.shader != null && src.shader.name.StartsWith("Universal Render Pipeline")) return src;
        if (Cache.TryGetValue(src, out var cached) && cached != null) return cached;

        var dst = new Material(Lit) { name = src.name + " (URP)" };

        var baseTex = FindTexture(src, "basecolor", "albedo", "diffuse", "maintex", "base map");
        var normalTex = FindTexture(src, "normal", "bump");

        if (baseTex != null)
        {
            dst.SetTexture("_BaseMap", baseTex);
            dst.SetTexture("_MainTex", baseTex);
        }

        if (normalTex != null)
        {
            dst.SetTexture("_BumpMap", normalTex);
            dst.EnableKeyword("_NORMALMAP");
        }

        // colore: se c'è la texture, il colore resta bianco (moltiplica 1)
        dst.SetColor("_BaseColor", baseTex != null ? Color.white : ReadColor(src));

        dst.SetFloat("_Metallic", 0f);
        dst.SetFloat("_Smoothness", 0.35f);
        dst.SetFloat("_Surface", 0f);          // opaco
        dst.SetFloat("_WorkflowMode", 1f);     // metallic
        dst.renderQueue = 2000;

        if (!_logged && baseTex != null)
        {
            _logged = true;
            Debug.Log($"GltfMaterials: {src.shader.name} → {dst.shader.name} · " +
                      $"base={baseTex.name} ({baseTex.width}×{baseTex.height}) · " +
                      $"normal={(normalTex != null ? normalTex.name : "no")} · " +
                      $"dst._BaseMap={(dst.GetTexture("_BaseMap") != null ? dst.GetTexture("_BaseMap").name : "NULL")}");
        }

        Cache[src] = dst;
        return dst;
    }

    private static Texture FindTexture(Material m, params string[] keys)
    {
        var names = m.GetTexturePropertyNames();
        foreach (var key in keys)
        {
            foreach (var n in names)
            {
                if (n.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var t = m.GetTexture(n);
                if (t != null) return t;
            }
        }
        return null;
    }

    private static Color ReadColor(Material m)
    {
        foreach (var n in new[] { "baseColorFactor", "_BaseColor", "_Color" })
        {
            if (!m.HasProperty(n)) continue;
            var c = m.GetColor(n);
            if (c.a > 0.01f) return c;
        }
        return Color.white;
    }
}
