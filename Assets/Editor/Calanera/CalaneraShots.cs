using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Fotografie delle inquadrature fisse ("Viste" nella scena) in PNG, con post-processing.
    /// Servono per i resoconti: stessa inquadratura prima e dopo ogni modifica.
    ///
    /// Dall'Editor: Falezia → Calanera → Foto delle viste.
    /// Da fuori: CalaneraShots.Shoot("/tmp/fz/shots", 1600, 900).
    /// </summary>
    public static class CalaneraShots
    {
        [MenuItem("Falezia/Calanera/Foto delle viste")]
        public static void Menu() => Shoot("/tmp/fz/shots", 1600, 900);

        /// <summary>Una sola inquadratura (per iterare): restituisce il percorso del PNG.</summary>
        public static string ShootOne(string dir, int width, int height, string viewName)
        {
            Directory.CreateDirectory(dir);
            var v = GameObject.Find("Viste/" + viewName);
            if (v == null) return "vista non trovata: " + viewName;
            var camGo = new GameObject("_FotoCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            camGo.transform.SetPositionAndRotation(v.transform.position, v.transform.rotation);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            rt.Create();
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            string file = Path.Combine(dir, viewName + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log("Calanera: foto " + file);
            return file;
        }

        public static string Shoot(string dir, int width, int height, string scenePath = null)
        {
            if (!string.IsNullOrEmpty(scenePath) && EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Directory.CreateDirectory(dir);
            var root = GameObject.Find("Viste");
            if (root == null) return "nessun oggetto 'Viste' nella scena";

            var camGo = new GameObject("_FotoCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            rt.Create();
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            int n = 0;
            var log = new System.Text.StringBuilder();
            foreach (Transform v in root.transform)
            {
                camGo.transform.SetPositionAndRotation(v.position, v.rotation);
                cam.targetTexture = rt;
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                string file = Path.Combine(dir, $"{v.name}.png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                log.AppendLine(file);
                n++;
            }
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log($"Calanera: {n} foto in {dir}");
            return log.ToString();
        }
    }
}
