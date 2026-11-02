using System.IO;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// Il ponte fra Unity e la simulazione: la simulazione riceve TESTO, non file.
/// La mappa vive in Assets/StreamingAssets/maps/calanera.json (lo stesso file del
/// progetto Godot): stessa mappa, stesse scatole, stesso ordine.
/// </summary>
public static class UnityMapFiles
{
    public static WorldState Load(string file = "calanera.json")
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "maps", file);
            if (File.Exists(path))
            {
                string art = Path.ChangeExtension(path, ".art.json");
                return MapLoader.Parse(File.ReadAllText(path), File.Exists(art) ? File.ReadAllText(art) : null);
            }

            Debug.LogWarning($"Mappa non trovata: {path}. Uso il mondo di test.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Mappa non caricata ({e.Message}). Uso il mondo di test.");
        }
        return WorldState.MakeTestWorld();
    }
}
