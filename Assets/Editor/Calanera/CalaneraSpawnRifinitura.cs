using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Falezia.EditorTools.Calanera
{
    public static partial class CalaneraSpawn
    {
        private const string HeroDir = "Assets/Art/Calanera/Props/Rifinitura/LOD";
        private const string HeroSourceDir = "Assets/Art/Calanera/Props/Rifinitura";

        private static void Eroi()
        {
            Hero("fontana_piazza", new Vector3(6f, 0f, -16f), 1.35f, 0f,
                 new Vector3(3.2f, 2.0f, 3.2f));
            Hero("banco_pesce", new Vector3(22f, 0f, 0f), 0.95f, 200f,
                 new Vector3(3.2f, 2.5f, 1.6f));
            Hero("barca_pesca", new Vector3(16f, 0f, -92f), 1.45f, 95f,
                 new Vector3(4.0f, 1.0f, 2.1f));
            Hero("bitta_ormeggio", new Vector3(17f, 0f, -99.1f), 1.0f, 0f,
                 new Vector3(0.4f, 1.0f, 0.4f));
            Hero("lampione_ghisa", new Vector3(3.9f, 0f, -12.1f), 1.0f, 0f,
                 new Vector3(0.5f, 3.6f, 0.5f));
            Hero("tavolino_caffe", new Vector3(9.2f, 0f, -14.2f), 1.0f, 35f,
                 new Vector3(1.3f, 0.8f, 1.3f));
            Hero("tavolino_caffe", new Vector3(2.6f, 0f, -14.5f), 1.0f, -35f,
                 new Vector3(1.3f, 0.8f, 1.3f));
            Hero("tavolino_caffe", new Vector3(9.6f, 0f, -18.2f), 1.0f, 75f,
                 new Vector3(1.3f, 0.8f, 1.3f));
            Hero("cannone_forte", new Vector3(5f, 0f, 90f), 1.0f, 195f,
                 new Vector3(2.4f, 1.2f, 1.4f));

            FontanaFx();
        }

        /// <summary>
        /// Fontana viva con il Particle System di Unity (niente shader scritti a mano):
        /// getto dal catino alto e velo di schizzi sul pelo dell'acqua.
        /// </summary>
        private static void FontanaFx()
        {
            var fountain = GameObject.Find("Hero fontana_piazza");
            if (fountain == null) return;

            const string texPath = "Assets/Art/Calanera/Textures/fx/soft_dot.png";
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            const string matPath = "Assets/Art/Calanera/Materials/M_FontanaAcqua.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "M_FontanaAcqua" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.80f, 0.92f, 1f, 0.70f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);

            // getto alto
            var jet = new GameObject("Fontana getto");
            jet.transform.SetParent(fountain.transform, false);
            jet.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var ps = jet.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.25f, 1.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.085f, 0.170f);
            main.startColor = new Color(0.78f, 0.90f, 0.97f, 0.75f);
            main.gravityModifier = 1.7f;
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 140f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f; shape.radius = 0.05f; shape.length = 0.04f;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1.25f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.80f, 0.92f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var pr = jet.GetComponent<ParticleSystemRenderer>();
            pr.material = mat;
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;

            // velo di schizzi sul pelo dell'acqua
            var mist = new GameObject("Fontana schizzi");
            mist.transform.SetParent(fountain.transform, false);
            mist.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            var ps2 = mist.AddComponent<ParticleSystem>();
            var m2 = ps2.main;
            m2.loop = true;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.110f);
            m2.startColor = new Color(0.80f, 0.92f, 1f, 0.30f);
            m2.gravityModifier = 0.02f;
            m2.maxParticles = 260;
            m2.simulationSpace = ParticleSystemSimulationSpace.Local;
            var e2 = ps2.emission; e2.rateOverTime = 60f;
            var s2 = ps2.shape;
            s2.shapeType = ParticleSystemShapeType.Circle;
            s2.radius = 0.55f; s2.radiusThickness = 0f;
            var c2 = ps2.colorOverLifetime; c2.enabled = true;
            var g2 = new Gradient();
            g2.SetKeys(
                new[] { new GradientColorKey(new Color(0.82f, 0.93f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0f, 1f) });
            c2.color = new ParticleSystem.MinMaxGradient(g2);
            var pr2 = mist.GetComponent<ParticleSystemRenderer>();
            pr2.material = mat;
            pr2.renderMode = ParticleSystemRenderMode.Billboard;
            pr2.shadowCastingMode = ShadowCastingMode.Off;
            pr2.receiveShadows = false;

        }

        /// <summary>Velo d'acqua a cono: geometria semplice, materiale URP Lit trasparente.</summary>
        private static void VeloFontana(Transform parent)
        {
            const string matPath = "Assets/Art/Calanera/Materials/M_FontanaVelo.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_FontanaVelo" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", new Color(0.64f, 0.82f, 0.93f, 0.42f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);

            var go = new GameObject("Fontana velo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            const int seg = 24;
            var verts = new Vector3[seg * 2];
            var uvs = new Vector2[seg * 2];
            var tris = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                verts[i] = new Vector3(ca * 0.07f, 0.55f, sa * 0.07f);
                verts[seg + i] = new Vector3(ca * 0.52f, -0.38f, sa * 0.52f);
                uvs[i] = new Vector2(i / (float)seg, 1f);
                uvs[seg + i] = new Vector2(i / (float)seg, 0f);
                int t = i * 6;
                int n = (i + 1) % seg;
                tris[t] = i; tris[t + 1] = seg + i; tris[t + 2] = n;
                tris[t + 3] = n; tris[t + 4] = seg + i; tris[t + 5] = seg + n;
            }
            var mesh = new Mesh { name = "VeloFontana" };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
        }

        private static void Hero(string id, Vector3 pos, float scale, float yaw, Vector3 boxSize)
        {
            var root = new GameObject("Hero " + id);
            var spawn = GameObject.Find(RootName);
            if (spawn != null) root.transform.SetParent(spawn.transform, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            root.transform.localScale = Vector3.one * scale;

            var group = root.AddComponent<LODGroup>();
            var lods = new LOD[3];
            float[] heights = { 0.55f, 0.22f, 0.06f };
            for (int i = 0; i < 3; i++)
            {
                // LOD0 alto ma decimato (UV stabili), LOD1/LOD2 per la distanza
                string path = $"{HeroDir}/{id}_lod{i}.glb";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning($"Hero {id}: manca LOD{i}"); continue; }
                var go = (GameObject)Object.Instantiate(prefab, root.transform);
                go.name = id + "_lod" + i;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                lods[i] = new LOD(heights[i], go.GetComponentsInChildren<Renderer>(true));
                if (i == 0)
                {
                    var mf = go.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                    }
                }
            }
            group.SetLODs(lods);
            group.RecalculateBounds();

            var hero = root.AddComponent<CalaneraHero>();
            hero.size = boxSize;
            hero.center = new Vector3(0f, boxSize.y * 0.5f, 0f);
        }
    }
}
