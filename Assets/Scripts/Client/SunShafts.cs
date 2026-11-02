using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FASCI DI LUCE VOLUMETRICA, in stile: quader grandi e morbidi allineati al sole,
/// additivi, solo dove la luce "entra" davvero (arcate del porticato, ingressi, molo).
///
/// Non è volumetria vera (non costa come tale): è il trucco che usano i giochi stilizzati —
/// un velo additivo con gradiente che si spegne verso il basso, abbastanza largo da leggere
/// come polvere illuminata. Se il sole gira, i fasci lo seguono.
/// </summary>
public sealed class SunShafts : MonoBehaviour
{
    private static readonly Vector3[] Spots =
    {
        new(6f, 3.4f, -22f), new(10f, 3.4f, -14f), new(16f, 3.4f, -6f),
        new(-22f, 3.4f, -50f), new(-16f, 3.4f, -44f),
        new(56f, 4.0f, -38f), new(48f, 4.0f, -30f),
        new(22f, 3.4f, 4f), new(-6f, 3.4f, 96f),
    };

    private readonly List<Transform> _shafts = new();
    private Mesh _quad;
    private Material _mat;
    private Transform _sun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("Fasci di luce");
        DontDestroyOnLoad(go);
        go.AddComponent<SunShafts>();
    }

    private void Start()
    {
        var shader = Shader.Find("Falezia/SunShaft");
        if (shader == null) { Debug.Log("SunShafts: shader assente"); enabled = false; return; }

        _quad = Quad();
        _mat = new Material(shader) { name = "Fascio" };
        _mat.SetColor("_Color", new Color(1f, 0.94f, 0.78f, 0.16f));

        var root = new GameObject("fasci").transform;
        root.SetParent(transform, false);

        foreach (var p in Spots)
        {
            var go = new GameObject("fascio");
            go.transform.SetParent(root, false);
            go.transform.position = p;
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _shafts.Add(go.transform);
        }
    }

    private static Mesh Quad()
    {
        var m = new Mesh { name = "fascio" };
        m.vertices = new[]
        {
            new Vector3(-1.6f, 0f, 0f), new Vector3(1.6f, 0f, 0f),
            new Vector3(1.6f, 7f, 0f), new Vector3(-1.6f, 7f, 0f),
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();
        return m;
    }

    private void LateUpdate()
    {
        if (_sun == null) _sun = RenderSettings.sun != null ? RenderSettings.sun.transform : null;
        if (_sun == null || _shafts.Count == 0) return;

        // il fascio guarda la camera ma resta inclinato lungo la direzione del sole
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = -_sun.forward;
        foreach (var t in _shafts)
        {
            var toCam = cam.transform.position - t.position;
            toCam.y = 0f;
            var flat = Quaternion.LookRotation(toCam.sqrMagnitude > 0.01f ? toCam.normalized : Vector3.forward);
            t.rotation = Quaternion.Slerp(flat, Quaternion.LookRotation(dir), 0.35f);
        }
    }
}
