using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools
{
    /// <summary>
    /// Crea e assegna la pipeline URP con ombre definite ad alta qualità.
    /// Menu: Falezia > Configura URP.
    /// Batch: -executeMethod Falezia.EditorTools.FaleziaGraphics.Setup
    /// </summary>
    public static class FaleziaGraphics
    {
        const string Dir = "Assets/Settings";
        const string RendererPath = Dir + "/URP-Renderer.asset";
        const string PipelinePath = Dir + "/URP.asset";
        const string ProfilePath = "Assets/Resources/Falezia-Volume.asset";

        [MenuItem("Falezia/Configura URP")]
        public static void Setup()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                asset.name = "URP-Falezia";
                AssetDatabase.CreateAsset(asset, PipelinePath);
            }

            // ---- ombre: definite, 4 cascate, 90 m ----
            asset.mainLightShadowmapResolution = 4096;
            asset.shadowDistance = 90f;
            asset.shadowCascadeCount = 4;
            asset.cascade4Split = new Vector3(0.08f, 0.22f, 0.48f);
            asset.shadowDepthBias = 1.0f;
            asset.shadowNormalBias = 1.0f;

            // ---- qualità immagine ----
            asset.msaaSampleCount = 4;
            asset.supportsHDR = true;
            asset.useSRPBatcher = true;
            asset.colorGradingLutSize = 32;

            // ---- campi con setter interno in URP 17 (soft shadows High) ----
            var so = new SerializedObject(asset);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", 3);      // SoftShadowQuality.High
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048);
            SetFloat(so, "m_CascadeBorder", 0.12f);
            SetBool(so, "m_AnyShadowsSupported", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            IncludeRequiredShaders();
            BuildVolumeProfile();
            EnsureLayer("ViewModel");
            BuildSkyMaterial();

            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
                QualitySettings.shadowDistance = 90f;
                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.shadowCascades = 4;
                QualitySettings.shadowCascade4Split = new Vector3(0.08f, 0.22f, 0.48f);
                QualitySettings.antiAliasing = 4;
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FALEZIA] URP configurato: " + PipelinePath +
                      " · ombre 90 m / 4 cascate / 4096 / soft High · MSAA 4 · HDR");
        }

        /// <summary>
        /// I materiali delle armi nascono a runtime da glTFast: senza questa lista gli
        /// shader degli shadergraph non finiscono nella build e le armi escono magenta.
        /// </summary>
        static readonly string[] RequiredShaders =
        {
            // armi: i materiali nascono a runtime dal GLB
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrMetallicRoughness.shadergraph",
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrSpecularGlossiness.shadergraph",
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-unlit.shadergraph",
            // mappa e FX: materiali creati a runtime, quindi le varianti con le ombre
            // vanno tenute nella build o il terreno resta illuminato in modo piatto
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader",
            "Packages/com.unity.render-pipelines.universal/Shaders/Unlit.shader",
            "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader",
            // effetti (fori, scie): materiali creati a runtime con Shader.Find
            "Assets/Shaders/FxUnlit.shader",
            "Assets/Shaders/Tracer.shader",
            "Skybox/Panoramic",
        };

        static void IncludeRequiredShaders()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[FALEZIA] GraphicsSettings.asset non leggibile");
                return;
            }

            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                Debug.LogWarning("[FALEZIA] m_AlwaysIncludedShaders non trovato");
                return;
            }

            int added = 0;
            foreach (var path in RequiredShaders)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null)
                {
                    Debug.LogWarning("[FALEZIA] shader non trovato: " + path);
                    continue;
                }

                bool already = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        already = true;
                        break;
                    }
                }
                if (already) continue;

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[FALEZIA] shader sempre inclusi: " + added + " aggiunti");
        }

        /// <summary>
        /// Il "look" dell'immagine: tonemap ACES, un filo di bloom, contrasto e vignetta.
        /// Il profilo sta in Resources così il gioco lo carica da solo all'avvio.
        /// </summary>
        static void BuildVolumeProfile()
        {
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(1.25f);
            bloom.intensity.Override(0.14f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(1f, 0.97f, 0.93f));

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0f);
            color.contrast.Override(14f);
            color.saturation.Override(10f);

            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.Override(0.10f);
            vig.smoothness.Override(0.65f);
            vig.color.Override(Color.black);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("[FALEZIA] profilo volume: " + ProfilePath + " (ACES, bloom, vignetta)");
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            foreach (var c in profile.components)
                if (c is T found) return found;

            var comp = profile.Add<T>(true);
            comp.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(comp, profile);
            return comp;
        }

        /// <summary>
        /// Materiale del cielo salvato come asset: così shader e panorama finiscono nella
        /// build anche se il materiale si usa solo a runtime (con Shader.Find il cielo
        /// nell'eseguibile non c'era).
        /// </summary>
        static void BuildSkyMaterial()
        {
            var shader = Shader.Find("Skybox/Panoramic");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Sky/sky_panorama.jpg");
            if (shader == null || tex == null)
            {
                Debug.LogWarning("[FALEZIA] cielo: shader o panorama mancante");
                return;
            }

            const string path = "Assets/Resources/Sky/Cielo.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "Cielo" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Mapping", 1f);
            mat.SetFloat("_ImageType", 0f);
            mat.SetFloat("_Exposure", 1.15f);
            mat.SetFloat("_Rotation", 118f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[FALEZIA] materiale cielo: " + path);
        }

        /// <summary>
        /// Layer "ViewModel": ci vive solo l'arma in mano, disegnata da una seconda camera
        /// così non entra nei muri quando ci si avvicina.
        /// </summary>
        static void EnsureLayer(string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            if (layers == null) return;

            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name)
                {
                    Debug.Log("[FALEZIA] layer già presente: " + name + " (" + i + ")");
                    return;
                }

            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[FALEZIA] layer creato: " + name + " (" + i + ")");
                return;
            }

            Debug.LogWarning("[FALEZIA] nessuno slot libero per il layer " + name);
        }

        static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.boolValue = value;
        }

        static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.intValue = value;
        }

        static void SetFloat(SerializedObject so, string name, float value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.floatValue = value;
        }
    }
}
