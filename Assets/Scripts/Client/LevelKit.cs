// Livello costruito dal kit di Blender: niente geometria procedurale.
// Unity qui fa solo tre cose: istanziare i pezzi, dare i collider, preparare l'atmosfera.
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public static class LevelKit
{
    public static Transform Root;
    public static Bounds WorldBounds;
    public static int Pieces;

    /// <summary>Crea il livello completo: pezzi, collider, atmosfera. Chiamata da MapBuilder.</summary>
    public static Transform Build(Transform parent)
    {
        var go = new GameObject("Livello (kit)");
        go.transform.SetParent(parent, false);
        Root = go.transform;
        Pieces = 0;

        // atmosfera: foschia leggera, sole caldo — coerente con lo stile dipinto
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.79f, 0.86f);
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 320f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.46f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.29f, 0.24f);

        return Root;
    }

    /// <summary>
    /// Collider delle case: una scatola per pezzo. Costano poco e bastano per proiettili,
    /// wallbang e per non attraversare i muri. Le mesh restano statiche.
    /// </summary>
    public static void AttachColliders(GameObject piece)
    {
        var renderers = piece.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        var box = piece.AddComponent<BoxCollider>();
        box.center = piece.transform.InverseTransformPoint(b.center);
        box.size = b.size;
        // i collider non devono ruotare con la scala del pezzo: la scatola è già in world space
        box.size = new Vector3(Mathf.Max(box.size.x, 0.05f),
                               Mathf.Max(box.size.y, 0.05f),
                               Mathf.Max(box.size.z, 0.05f));
        Pieces++;
    }

    /// <summary>Colori d'atmosfera per zona: la luce cambia entrando nei vicoli.</summary>
    public static void Tick(Vector3 playerPos)
    {
        // foschia più densa nei vicoli stretti (x fra -30 e 30, z fra -40 e 40)
        bool vicoli = Mathf.Abs(playerPos.x) < 32f && Mathf.Abs(playerPos.z) < 42f;
        float target = vicoli ? 0.72f : 0.86f;
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, target * 360f, 0.6f * Time.deltaTime);
    }
}
