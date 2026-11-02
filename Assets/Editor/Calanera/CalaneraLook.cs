using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Look di Calanera: sole, esposizione, volume post, cielo e lens flare.
    ///
    /// È il posto unico dove si regola l'aspetto della mappa: lo stesso asset di volume e lo
    /// stesso flare li usa il gioco a runtime (`GameRoot`), così quello che si vede negli
    /// screenshot delle "Viste" è quello che si vede in partita.
    ///
    /// Menu: Falezia → Calanera → Look: applica.
    /// </summary>
    public static class CalaneraLook
    {
        public const string FlarePath = "Assets/Resources/LensFlare/SunLensFlare.asset";
        public const string ProfilePath = "Assets/Resources/Falezia-Volume.asset";
        public const string PostRoot = "Post";

        // --- numeri del look (tenerli allineati con GameRoot.BuildLight / BuildSky)
        public static readonly Color SunColor = new(1.00f, 0.965f, 0.915f);
        public const float SunIntensity = 0.84f;
        public static readonly Vector3 SunEuler = new(33f, 103f, 0f);

        public static readonly Color Zenith = new(0.115f, 0.265f, 0.545f);
        public static readonly Color Horizon = new(0.745f, 0.815f, 0.875f);
        public static readonly Color Ground = new(0.265f, 0.270f, 0.265f);

        // nuvole: rade, grandi, con bordo leggibile (stile dipinto, non fumo)
        public const float CloudCover = 0.56f;
        public const float CloudScale = 1.50f;
        public const float CloudSoftness = 0.05f;

        [MenuItem("Falezia/Calanera/Look: applica")]
        public static void Menu() => Apply(true);

        public static string Apply(bool save)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var sun = GameObject.Find("Sole");
            if (sun == null)
            {
                sun = new GameObject("Sole");
                Undo.RegisterCreatedObjectUndo(sun, "Sole");
            }

            var light = sun.GetComponent<Light>();
            if (light == null) light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.92f;
            light.shadowBias = 0.045f;
            light.shadowNormalBias = 0.35f;
            sun.transform.rotation = Quaternion.Euler(SunEuler);
            RenderSettings.sun = light;

            // luce di riempimento fredda, senza ombre: alza il lato in ombra senza appiattire
            var fill = sun.transform.Find("Riempimento");
            var fillGo = fill != null ? fill.gameObject : new GameObject("Riempimento");
            fillGo.transform.SetParent(sun.transform, false);
            var fl = fillGo.GetComponent<Light>();
            if (fl == null) fl = fillGo.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.62f, 0.72f, 0.86f);
            fl.intensity = 0.18f;
            fl.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(18f, -78f, 0f);

            // --- lens flare del sole
            var flare = AssetDatabase.LoadAssetAtPath<LensFlareDataSRP>(FlarePath);
            if (flare == null) flare = CreateFlare();
            var flareComp = sun.GetComponent<LensFlareComponentSRP>();
            if (flareComp == null) flareComp = sun.AddComponent<LensFlareComponentSRP>();
            flareComp.lensFlareData = flare;
            flareComp.intensity = 0.42f;
            flareComp.scale = 1.0f;
            flareComp.maxAttenuationDistance = 220f;
            flareComp.attenuationByLightShape = false;
            flareComp.useOcclusion = false;
            flareComp.allowOffScreen = false;
            flareComp.enabled = true;

            // --- cielo: il materiale della scena è lo stesso shader del runtime
            var sky = RenderSettings.skybox;
            if (sky == null || sky.shader == null || sky.shader.name != "Skybox/Falezia Nuvole")
            {
                var sh = Shader.Find("Skybox/Falezia Nuvole");
                if (sh != null) sky = new Material(sh) { name = "M_CieloBanco" };
            }
            if (sky != null)
            {
                sky.SetColor("_Zenith", Zenith);
                sky.SetColor("_Horizon", Horizon);
                sky.SetColor("_Ground", Ground);
                sky.SetColor("_SunColor", new Color(1.0f, 0.90f, 0.74f));
                sky.SetColor("_CloudLight", new Color(0.92f, 0.94f, 0.97f));
                sky.SetColor("_CloudDark", new Color(0.66f, 0.71f, 0.78f));
                sky.SetVector("_SunDir", -sun.transform.forward);
                sky.SetFloat("_Cover", CloudCover);
                sky.SetFloat("_Scale", CloudScale);
                sky.SetFloat("_Softness", CloudSoftness);
                sky.SetFloat("_Speed", 0.006f);
                RenderSettings.skybox = sky;
                EditorUtility.SetDirty(sky);
            }

            // --- ambiente e foschia
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.60f, 0.73f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.62f, 0.59f);
            RenderSettings.ambientGroundColor = new Color(0.33f, 0.31f, 0.29f);
            RenderSettings.ambientIntensity = 0.70f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.735f, 0.775f, 0.805f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 360f;
            RenderSettings.reflectionIntensity = 1.15f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            // --- volume post, attivo anche in edit mode: gli screenshot sono veri
            var post = GameObject.Find(PostRoot);
            if (post == null) post = new GameObject(PostRoot);
            var volume = post.GetComponent<Volume>();
            if (volume == null) volume = post.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            post.SetActive(true);

            TuneProfile(volume.sharedProfile);
            DynamicGI.UpdateEnvironment();

            if (save && scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return "CalaneraLook: sole, cielo, volume e flare applicati";
        }

        /// <summary>Il profilo è lo stesso del runtime: qui si regolano esposizione e bloom.</summary>
        private static void TuneProfile(VolumeProfile profile)
        {
            if (profile == null) return;

            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = true;
                bloom.threshold.overrideState = true; bloom.threshold.value = 1.20f;
                bloom.intensity.overrideState = true; bloom.intensity.value = 0.18f;
                bloom.scatter.overrideState = true; bloom.scatter.value = 0.62f;
                bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.92f, 0.82f);
                bloom.highQualityFiltering.overrideState = true; bloom.highQualityFiltering.value = true;
                bloom.maxIterations.overrideState = true; bloom.maxIterations.value = 7;
            }
            if (profile.TryGet<ColorAdjustments>(out var color))
            {
                color.postExposure.overrideState = true; color.postExposure.value = -0.36f;
                color.contrast.overrideState = true; color.contrast.value = 13f;
                color.saturation.overrideState = true; color.saturation.value = 7f;
                color.hueShift.overrideState = true; color.hueShift.value = 0f;
            }
            if (profile.TryGet<WhiteBalance>(out var wb))
            {
                wb.temperature.overrideState = true; wb.temperature.value = -8f;
                wb.tint.overrideState = true; wb.tint.value = 3f;
            }
            if (profile.TryGet<Tonemapping>(out var tm))
            {
                tm.mode.overrideState = true; tm.mode.value = TonemappingMode.ACES;
            }
            if (profile.TryGet<Vignette>(out var vg))
            {
                vg.intensity.overrideState = true; vg.intensity.value = 0.10f;
                vg.smoothness.overrideState = true; vg.smoothness.value = 0.65f;
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Flare del sole: alone caldo, anello e due fantasmi freddi. Tutto procedurale.</summary>
        private static LensFlareDataSRP CreateFlare()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FlarePath) ?? "Assets/Resources/LensFlare");
            var flare = ScriptableObject.CreateInstance<LensFlareDataSRP>();
            flare.elements = new[]
            {
                new LensFlareDataElementSRP
                {
                    visible = true, flareType = SRPLensFlareType.Circle,
                    position = 0f, sizeXY = new Vector2(1.15f, 1.15f),
                    localIntensity = 0.26f, fallOff = 2.4f,
                    tint = new Color(1f, 0.86f, 0.64f, 0.55f),
                    blendMode = SRPLensFlareBlendMode.Additive,
                    preserveAspectRatio = true, autoRotate = false,
                    modulateByLightColor = true, allowMultipleElement = false,
                },
                new LensFlareDataElementSRP
                {
                    visible = true, flareType = SRPLensFlareType.Ring,
                    position = 0f, sizeXY = new Vector2(3.8f, 3.8f), ringThickness = 0.030f,
                    localIntensity = 0.10f, fallOff = 2.0f,
                    tint = new Color(1f, 0.72f, 0.48f, 0.22f),
                    blendMode = SRPLensFlareBlendMode.Additive,
                    preserveAspectRatio = true, autoRotate = false,
                },
                new LensFlareDataElementSRP
                {
                    visible = true, flareType = SRPLensFlareType.Circle,
                    position = 0.55f, sizeXY = new Vector2(0.30f, 0.30f),
                    localIntensity = 0.10f, fallOff = 1.3f,
                    tint = new Color(0.55f, 0.78f, 1f, 0.35f),
                    blendMode = SRPLensFlareBlendMode.Additive,
                    preserveAspectRatio = true, autoRotate = false,
                },
                new LensFlareDataElementSRP
                {
                    visible = true, flareType = SRPLensFlareType.Circle,
                    position = 1.05f, sizeXY = new Vector2(0.18f, 0.18f),
                    localIntensity = 0.08f, fallOff = 1.2f,
                    tint = new Color(1f, 0.70f, 0.42f, 0.30f),
                    blendMode = SRPLensFlareBlendMode.Additive,
                    preserveAspectRatio = true, autoRotate = false,
                },
            };
            AssetDatabase.CreateAsset(flare, FlarePath);
            AssetDatabase.SaveAssets();
            return flare;
        }
    }
}
