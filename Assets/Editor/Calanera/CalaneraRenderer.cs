using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Feature del renderer URP che servono alla mappa. Per ora: SSAO (occlusione negli angoli,
    /// sotto i cornicioni, fra i sampietrini), che legge le normali vere dello shader Superficie.
    /// La classe di URP è interna: si crea per nome, come fa l'Inspector.
    /// </summary>
    public static class CalaneraRenderer
    {
        private const string RendererPath = "Assets/Settings/URP-Renderer.asset";

        [MenuItem("Falezia/Calanera/Renderer: SSAO")]
        public static void MenuSsao() => Debug.Log(EnsureSsao(0.45f, 0.6f));

        public static string EnsureSsao(float intensity, float radius)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return "renderer non trovato";
            var type = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return "tipo SSAO non trovato";

            var feat = data.rendererFeatures.FirstOrDefault(f => f != null && f.GetType() == type);
            if (feat == null)
            {
                feat = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                feat.name = "SSAO";
                AssetDatabase.AddObjectToAsset(feat, data);
                data.rendererFeatures.Add(feat);
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
            var fso = new SerializedObject(feat);
            void F(string p, float v) { var sp = fso.FindProperty(p); if (sp != null) sp.floatValue = v; }
            void I(string p, int v) { var sp = fso.FindProperty(p); if (sp != null) sp.intValue = v; }
            F("m_Settings.Intensity", intensity);
            F("m_Settings.Radius", radius);
            F("m_Settings.DirectLightingStrength", 0.15f);
            F("m_Settings.Falloff", 100f);
            I("m_Settings.Source", 1);          // normali dal pass DepthNormals (geometriche)
            I("m_Settings.NormalSamples", 1);   // medie: meno rumore sulle facce piatte
            I("m_Settings.Samples", 0);         // High
            I("m_Settings.AOMethod", 1);        // Interleaved gradient: niente chiazze di blue noise
            I("m_Settings.BlurQuality", 0);     // High
            I("m_Settings.SampleCount", -1);
            var ds = fso.FindProperty("m_Settings.Downsample"); if (ds != null) ds.boolValue = false;
            var ao = fso.FindProperty("m_Settings.AfterOpaque"); if (ao != null) ao.boolValue = false;
            fso.ApplyModifiedPropertiesWithoutUndo();
            feat.SetActive(true);
            EditorUtility.SetDirty(feat);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return "SSAO attivo; feature: " + string.Join(", ", data.rendererFeatures.Where(f => f != null).Select(f => f.name + (f.isActive ? "" : " (off)")));
        }
    }
}
