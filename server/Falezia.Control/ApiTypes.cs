namespace Falezia.Control;

/// <summary>Modifica parziale delle impostazioni dal pannello: solo i campi presenti cambiano.</summary>
public sealed class SettingsPatch
{
    public bool? HardLimitEnabled { get; set; }
    public int? HardLimitMaxInstances { get; set; }
    public int? MaxPlayersPerInstance { get; set; }
    public int? MinPlayersToStart { get; set; }
    public int? QueueWaitSeconds { get; set; }
    public bool? MatchmakingEnabled { get; set; }
    public bool? AutoStartInstance { get; set; }
    public int? BotsPerTeam { get; set; }
    public string Map { get; set; }
    public string LogLevel { get; set; }
    public string PublicHost { get; set; }
    public string ClientCommand { get; set; }
}

/// <summary>Richiesta di entrare in coda.</summary>
public sealed class QueueRequest
{
    public string Name { get; set; }
}
