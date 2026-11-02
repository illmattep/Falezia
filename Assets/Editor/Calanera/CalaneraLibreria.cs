using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// LIBRERIA DEI MATERIALI di Calanera (doc 29). Le texture arrivano dalla forgia
    /// (tools/calanera2/forge.py) in Assets/Art/Calanera/Textures/&lt;id&gt;/, con un json che dice
    /// quanti metri copre una tessera. Qui si creano i materiali .mat su Falezia/Superficie.
    ///
    /// I materiali sono ASSET: dopo la creazione si ritoccano liberamente dall'Inspector.
    /// Rilanciare il menu aggiorna solo le texture e le scale, non i valori ritoccati, a meno di
    /// usare "Ricrea materiali (sovrascrivi)".
    /// </summary>
    public static class CalaneraLibreria
    {
        public const string TexRoot = "Assets/Art/Calanera/Textures";
        public const string MatRoot = "Assets/Art/Calanera/Materials";
        public const string ShaderName = "Falezia/Superficie";

        /// <summary>Ricetta di un materiale: strato base, sotto-strato opzionale, regolazioni.</summary>
        private class Ricetta
        {
            public string nome, l0, l1;
            public Color tint0 = Color.white, tint1 = Color.white;
            public float damage = 0.25f, damageBottom = 0.3f, grime = 0.8f, grimeHeight = 0.7f;
            public float hex0 = 0f, hex1 = 0f, macro = 0.35f;
            public bool world = true;
        }

        private static readonly List<Ricetta> Ricette = new()
        {
            // muri: intonaco che cade e scopre il mattone o la pietra
            new Ricetta { nome = "M_Intonaco_Mattone", l0 = "intonaco_liscio", l1 = "mattone", damage = 0.22f },
            new Ricetta { nome = "M_Intonaco_Ocra_Mattone", l0 = "intonaco_liscio", l1 = "mattone", damage = 0.18f,
                          tint0 = new Color(1.0f, 0.86f, 0.66f) },
            new Ricetta { nome = "M_Intonaco_Rosa_Mattone", l0 = "intonaco_liscio", l1 = "mattone", damage = 0.2f,
                          tint0 = new Color(1.0f, 0.86f, 0.80f) },
            new Ricetta { nome = "M_Venez_Pietra", l0 = "intonaco_venez", l1 = "pietra_conci", damage = 0.18f },
            new Ricetta { nome = "M_Grezzo_Mattone", l0 = "intonaco_grezzo", l1 = "mattone", damage = 0.3f, damageBottom = 0.45f },
            // pietra
            new Ricetta { nome = "M_Pietra_Conci", l0 = "pietra_conci", hex0 = 0f, grime = 0.6f },
            // suolo: lastre con chiazze di sampietrini (il "danno" qui è il cambio di pavimento)
            new Ricetta { nome = "M_Piazza", l0 = "lastre", l1 = "sampietrini", damage = 0.12f, damageBottom = 0f,
                          grime = 0f, hex0 = 0.0f, macro = 0.3f },
            new Ricetta { nome = "M_Sampietrini", l0 = "sampietrini", grime = 0f, hex0 = 0.35f, macro = 0.3f },
            new Ricetta { nome = "M_Lastre", l0 = "lastre", grime = 0f, hex0 = 0f, macro = 0.3f },
            // tetti
            new Ricetta { nome = "M_Coppi", l0 = "coppi", grime = 0f, hex0 = 0f, macro = 0.4f },

            // ---- Porto Vecchio (spawn Nord): cemento, lamiera, legno, ferro
            new Ricetta { nome = "M_Porto_Suolo", l0 = "cemento_lastre", grime = 0f, hex0 = 0f, macro = 0.32f, damage = 0f, damageBottom = 0f },
            new Ricetta { nome = "M_Porto_Pad", l0 = "cemento_lastre", grime = 0.12f, grimeHeight = 0.2f, hex0 = 0f, macro = 0.16f,
                          damage = 0f, damageBottom = 0f, tint0 = new Color(1.20f, 1.16f, 1.06f) },
            new Ricetta { nome = "M_Porto_Pietra", l0 = "pietra_conci", grime = 0.55f, hex0 = 0f, macro = 0.3f },
            new Ricetta { nome = "M_Porto_PietraScura", l0 = "pietra_conci", grime = 0.7f, hex0 = 0f, macro = 0.3f,
                          tint0 = new Color(0.62f, 0.60f, 0.55f) },
            new Ricetta { nome = "M_Porto_LamieraBlu", l0 = "lamiera_blu", grime = 0.5f, grimeHeight = 0.9f, hex0 = 0f, macro = 0.34f,
                          damage = 0f, damageBottom = 0f },
            new Ricetta { nome = "M_Porto_LamieraRuggine", l0 = "lamiera_ruggine", grime = 0.55f, grimeHeight = 1.0f, hex0 = 0f, macro = 0.34f,
                          damage = 0f, damageBottom = 0f },
            new Ricetta { nome = "M_Porto_LamieraVerde", l0 = "lamiera_blu", grime = 0.55f, grimeHeight = 0.9f, hex0 = 0f, macro = 0.34f,
                          damage = 0f, damageBottom = 0f, tint0 = new Color(0.72f, 0.94f, 0.80f) },
            new Ricetta { nome = "M_Porto_LamieraGrigio", l0 = "lamiera_blu", grime = 0.55f, grimeHeight = 0.9f, hex0 = 0f, macro = 0.34f,
                          damage = 0f, damageBottom = 0f, tint0 = new Color(1.05f, 1.02f, 0.98f) },
            new Ricetta { nome = "M_Porto_LegnoChiaro", l0 = "legno_chiaro", grime = 0.35f, grimeHeight = 0.4f, hex0 = 0f, macro = 0.25f },
            new Ricetta { nome = "M_Porto_LegnoScuro", l0 = "legno_scuro", grime = 0.3f, grimeHeight = 0.4f, hex0 = 0f, macro = 0.25f },
            new Ricetta { nome = "M_Porto_LegnoVerniciato", l0 = "legno_verniciato", grime = 0.25f, grimeHeight = 0.4f, hex0 = 0f, macro = 0.22f },
            new Ricetta { nome = "M_Porto_PersianaVerde", l0 = "legno_verniciato", grime = 0.2f, grimeHeight = 0.3f, hex0 = 0f, macro = 0.18f,
                          tint0 = new Color(0.30f, 0.46f, 0.37f) },
            new Ricetta { nome = "M_Porto_PersianaArdesia", l0 = "legno_verniciato", grime = 0.2f, grimeHeight = 0.3f, hex0 = 0f, macro = 0.18f,
                          tint0 = new Color(0.42f, 0.50f, 0.55f) },
            new Ricetta { nome = "M_Porto_PersianaSalvia", l0 = "legno_verniciato", grime = 0.2f, grimeHeight = 0.3f, hex0 = 0f, macro = 0.18f,
                          tint0 = new Color(0.58f, 0.66f, 0.55f) },
            new Ricetta { nome = "M_Porto_PersianaLegno", l0 = "legno_chiaro", grime = 0.2f, grimeHeight = 0.3f, hex0 = 0f, macro = 0.18f,
                          tint0 = new Color(1.02f, 0.96f, 0.88f) },
            new Ricetta { nome = "M_Porto_Ferro", l0 = "ferro", grime = 0.4f, grimeHeight = 0.6f, hex0 = 0f, macro = 0.25f },
            new Ricetta { nome = "M_Porto_Gru", l0 = "lamiera_ruggine", grime = 0.55f, grimeHeight = 1.2f, hex0 = 0f, macro = 0.3f,
                          damage = 0f, damageBottom = 0f, tint0 = new Color(1.22f, 1.08f, 0.82f) },

            // ---- intonaci delle case sul piazzale
            new Ricetta { nome = "M_Porto_MuroAvorio", l0 = "intonaco_liscio", l1 = "mattone", damage = 0.16f,
                          tint0 = new Color(1.06f, 1.01f, 0.94f) },
            new Ricetta { nome = "M_Porto_MuroCrema", l0 = "intonaco_liscio", l1 = "mattone", damage = 0.18f,
                          tint0 = new Color(1.02f, 0.94f, 0.80f) },
            new Ricetta { nome = "M_Porto_MuroGrigio", l0 = "intonaco_grezzo", l1 = "mattone", damage = 0.12f,
                          tint0 = new Color(0.86f, 0.85f, 0.82f) },
            new Ricetta { nome = "M_Porto_MuroCementizio", l0 = "intonaco_grezzo", l1 = "mattone", damage = 0.08f,
                          tint0 = new Color(0.94f, 0.92f, 0.88f) },
            new Ricetta { nome = "M_Porto_MuroPerla", l0 = "intonaco_venez", l1 = "pietra_conci", damage = 0.10f,
                          tint0 = new Color(0.96f, 0.95f, 0.92f) },
        };

        [MenuItem("Falezia/Calanera/Crea materiali")]
        public static void CreaMateriali() => Crea(false);

        [MenuItem("Falezia/Calanera/Ricrea materiali (sovrascrivi)")]
        public static void RicreaMateriali() => Crea(true);

        public static void Crea(bool sovrascrivi)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) { Debug.LogError($"Calanera: shader {ShaderName} non trovato"); return; }
            Directory.CreateDirectory(MatRoot);
            var macro = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/macro/macro_m.png");

            int creati = 0, aggiornati = 0;
            foreach (var r in Ricette)
            {
                string path = $"{MatRoot}/{r.nome}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool nuovo = mat == null;
                if (nuovo) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;

                Strato(mat, "_L0", r.l0);
                mat.SetTexture("_MacroTex", macro);
                if (!string.IsNullOrEmpty(r.l1)) Strato(mat, "_L1", r.l1);

                if (nuovo || sovrascrivi)
                {
                    mat.SetColor("_L0_Tint", r.tint0);
                    mat.SetColor("_L1_Tint", r.tint1);
                    mat.SetFloat("_UseLayer1", string.IsNullOrEmpty(r.l1) ? 0f : 1f);
                    mat.SetFloat("_DamageAmount", r.damage);
                    mat.SetFloat("_DamageBottom", r.damageBottom);
                    mat.SetFloat("_GrimeAmount", r.grime);
                    mat.SetFloat("_GrimeHeight", r.grimeHeight);
                    mat.SetFloat("_MacroStrength", r.macro);
                    creati += nuovo ? 1 : 0;
                    aggiornati += nuovo ? 0 : 1;
                }
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Calanera: materiali pronti ({creati} nuovi, {aggiornati} riscritti) in {MatRoot}");
        }

        /// <summary>Assegna albedo, normal, maschera e scala (dal json della forgia) a uno strato.</summary>
        private static void Strato(Material mat, string p, string id)
        {
            string dir = $"{TexRoot}/{id}";
            mat.SetTexture($"{p}_Albedo", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_a.png"));
            mat.SetTexture($"{p}_Normal", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_n.png"));
            mat.SetTexture($"{p}_Mask", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{id}_m.png"));
            string json = $"{dir}/{id}.json";
            if (File.Exists(json))
            {
                var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(json));
                if (meta != null && meta.tile_m > 0) mat.SetFloat($"{p}_Tile", meta.tile_m);
            }
            else Debug.LogWarning($"Calanera: manca {json}");
        }

        [System.Serializable]
        private class Meta { public string id; public float tile_m; public float relief_m; }
    }
}
