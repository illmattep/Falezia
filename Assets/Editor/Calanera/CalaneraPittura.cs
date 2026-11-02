using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Prova dello stile "dipinto" in gioco: un passo a schermo intero (Kuwahara, <c>PitturaSchermo</c>)
    /// aggiunto al renderer URP prima del post-processing. Si accende e si spegne senza toccare altro,
    /// così le foto prima/dopo sono confrontabili.
    /// </summary>
    public static class CalaneraPittura
    {
        private const string RendererPath = "Assets/Settings/URP-Renderer.asset";
        private const string MatPath = "Assets/Art/Calanera/Materials/M_PitturaSchermo.mat";
        private const string FeatureName = "Pittura a schermo";

        [MenuItem("Falezia/Calanera/Pittura a schermo: accendi")]
        public static void On() => Set(true, 3f, 1f);

        [MenuItem("Falezia/Calanera/Pittura a schermo: spegni")]
        public static void Off() => Set(false, 3f, 1f);

        public static string Set(bool on, float radius, float strength)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return "renderer non trovato: " + RendererPath;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                var sh = Shader.Find("Hidden/Falezia/PitturaSchermo");
                if (sh == null) return "shader Hidden/Falezia/PitturaSchermo non trovato";
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            mat.SetFloat("_Radius", radius);
            mat.SetFloat("_Strength", strength);
            EditorUtility.SetDirty(mat);

            var feat = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == FeatureName);
            if (feat == null)
            {
                feat = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feat.name = FeatureName;
                feat.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                feat.fetchColorBuffer = true;
                feat.passMaterial = mat;
                AssetDatabase.AddObjectToAsset(feat, data);
                data.rendererFeatures.Add(feat);
                // la mappa delle feature (id locali) deve seguire la lista, come fa l'Inspector di URP
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out string _, out long localId);
                var so = new SerializedObject(data);
                var map = so.FindProperty("m_RendererFeatureMap");
                if (map != null)
                {
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            feat.passMaterial = mat;
            feat.SetActive(on);
            EditorUtility.SetDirty(feat);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return $"pittura {(on ? "accesa" : "spenta")} (raggio {radius}, forza {strength}); feature: " +
                   string.Join(", ", data.rendererFeatures.Where(f => f != null).Select(f => f.name + (f.isActive ? "" : " (off)")));
        }
    }
}
