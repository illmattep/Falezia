using Falezia.Sim;
using Godot;

/// <summary>
/// Il ponte fra il filesystem di Godot (res://, anche dentro il .pck esportato) e la
/// simulazione, che riceve solo testo. Lo usano il client e il server: stessa mappa,
/// stesso ordine delle scatole, stessa simulazione.
///
/// Nota per l'export: i .json non sono risorse Godot. Nel preset di esportazione va
/// aggiunto il filtro "maps/*.json" ai file non-risorsa, o la mappa non entra nel pacchetto.
/// </summary>
public static class MapFiles
{
    public const string Calanera = "res://maps/calanera.json";

    /// <summary>Carica una mappa. Se manca o è rotta, ripiega sul mondo di test e lo dice.</summary>
    public static WorldState Load(string path = Calanera)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushWarning($"[mappa] {path} non trovato: uso la stanza di test");
            return WorldState.MakeTestWorld();
        }
        try
        {
            string artPath = path.Replace(".json", ".art.json");
            string art = Godot.FileAccess.FileExists(artPath) ? Godot.FileAccess.GetFileAsString(artPath) : null;
            var world = MapLoader.Parse(Godot.FileAccess.GetFileAsString(path), art);
            GD.Print($"[mappa] {world.Name}: {world.Boxes.Count} volumi, {world.Decor.Count} decorazioni");
            return world;
        }
        catch (System.Exception e)
        {
            GD.PushError($"[mappa] {path} non valido ({e.Message}): uso la stanza di test");
            return WorldState.MakeTestWorld();
        }
    }

    /// <summary>
    /// Dove nasce un giocatore. Le squadre non esistono ancora nel protocollo: per ora
    /// gli id dispari vanno a Nord (Porto Vecchio), i pari a Sud (Forte San Verano).
    /// </summary>
    public static System.Numerics.Vector3 SpawnFor(WorldState world, int id)
    {
        var list = (id % 2 == 1 || world.SpawnsSouth.Count == 0) ? world.SpawnsNorth : world.SpawnsSouth;
        if (list.Count == 0) return world.FallbackFeet;
        return list[((id - 1) / 2) % list.Count];
    }

    public static bool IsNorth(int id) => id % 2 == 1;
}
