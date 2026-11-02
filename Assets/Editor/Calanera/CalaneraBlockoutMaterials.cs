using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Veste il blockout con la libreria della mappa nuova (F1): i volumi grigi diventano
    /// intonaci, pietra, lamiera, legno e coppi veri, con UV del mondo (niente stiramenti).
    /// Non tocca chi è già vestito: cambia solo il materiale dei volumi del blockout.
    ///
    /// Le piazzole di squadra (team_north/team_south) si spengono: sono dati di regia, non
    /// segnaletica; lo spawn Nord ha le sue piazzole dipinte.
    /// </summary>
    public static class CalaneraBlockoutMaterials
    {
        private static readonly Dictionary<string, string> Materiale = new()
        {
            ["plaster"] = "M_Intonaco_Mattone",
            ["plaster_ochre"] = "M_Intonaco_Ocra_Mattone",
            ["stone"] = "M_Pietra_Conci",
            ["rock"] = "M_Grezzo_Mattone",
            ["concrete"] = "M_Porto_MuroCementizio",
            ["steel"] = "M_Porto_LamieraGrigio",
            ["wood"] = "M_Porto_LegnoChiaro",
            ["cover"] = "M_Pietra_Conci",
            ["stairs"] = "M_Pietra_Conci",
            ["roof"] = "M_Coppi",
            ["objective"] = "M_Porto_GialloSpento",
        };

        /// <summary>
        /// Torna ai materiali di servizio (grigi/piatti) del blockout: il vestito vero si fa
        /// con le facciate, non texturendo le scatole. Utile finché le zone non sono finite.
        /// </summary>
        [MenuItem("Falezia/Calanera/Blockout: materiali di servizio")]
        public static void Ripristina()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != CalaneraBlockout.ScenePath)
                scene = EditorSceneManager.OpenScene(CalaneraBlockout.ScenePath, OpenSceneMode.Single);

            int n = 0;
            foreach (var b in Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude))
            {
                var r = b.GetComponent<MeshRenderer>();
                if (r == null) continue;
                if (b.kind == "team_north" || b.kind == "team_south") { r.enabled = false; continue; }
                var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Calanera/Materials/Blockout/Blockout_{b.kind}.mat");
                if (m != null && r.sharedMaterial != m) { r.sharedMaterial = m; n++; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blockout: materiali di servizio ripristinati su {n} volumi");
        }

        [MenuItem("Falezia/Calanera/Blockout: materiali della libreria")]
        public static void Applica()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != CalaneraBlockout.ScenePath)
                scene = EditorSceneManager.OpenScene(CalaneraBlockout.ScenePath, OpenSceneMode.Single);

            var cache = new Dictionary<string, Material>();
            Material Load(string name)
            {
                if (cache.TryGetValue(name, out var m) && m != null) return m;
                m = AssetDatabase.LoadAssetAtPath<Material>($"{CalaneraLibreria.MatRoot}/{name}.mat");
                if (m == null) Debug.LogWarning($"Blockout materiali: manca {name}");
                cache[name] = m;
                return m;
            }

            int cambiati = 0, piazzole = 0;
            foreach (var b in Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude))
            {
                var r = b.GetComponent<MeshRenderer>();
                if (r == null) continue;
                if (b.kind == "team_north" || b.kind == "team_south")
                {
                    r.enabled = false;   // piazzole: le disegna solo lo spawn vestito
                    piazzole++;
                    continue;
                }
                if (b.kind == "street") continue;   // c'è il suolo città
                if (!Materiale.TryGetValue(b.kind, out var matName)) continue;
                var mat = Load(matName);
                if (mat == null || r.sharedMaterial == mat) continue;
                r.sharedMaterial = mat;
                cambiati++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blockout materiali: {cambiati} volumi vestiti, {piazzole} piazzole spente");
        }
    }
}
