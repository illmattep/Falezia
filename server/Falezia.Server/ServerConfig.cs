namespace Falezia.Server;

/// <summary>
/// Configurazione del server dedicato. Si passa da riga di comando:
///   --port=27015 --map=calanera.json --max=10 --log=info
/// e da variabili d'ambiente (FALEZIA_PORT, FALEZIA_MAP, FALEZIA_MAX, FALEZIA_LOG)
/// per i container e per il control plane, che passa il livello scelto nel pannello.
/// </summary>
public sealed class ServerConfig
{
    public int Port = 27015;
    public int MaxPlayers = 10;

    /// <summary>
    /// Quanti giocatori UMANI servono perché la lobby si apra. Il matchmaking è questo:
    /// chi si collega entra in coda, e quando in coda ci sono abbastanza persone la
    /// partita parte (brief → acquisti). Con `--players=1` si prova da soli coi bot.
    /// </summary>
    public int PlayersToStart = 2;
    public string Map = "calanera.json";
    /// <summary>Quanti bot per squadra mettere in partita (0 = nessuno). Servono a far girare il round.</summary>
    public int BotsPerTeam = 0;
    /// <summary>0 = bot divisi fra le squadre · 1 o 2 = tutti in quella squadra (allenamento).</summary>
    public int BotsTeam = 0;
    /// <summary>debug · info · warn · error: quanto finisce su stdout.</summary>
    public string LogLevel = "info";
    /// <summary>Compatibilità: --quiet equivale a --log=warn.</summary>
    public bool Verbose = true;

    /// <summary>0 debug · 1 info · 2 warn · 3 error.</summary>
    public int LogRank => LogLevel switch
    {
        "debug" => 0,
        "warn" => 2,
        "error" => 3,
        _ => 1
    };
    public string MapPath => Path.Combine(AppContext.BaseDirectory, "maps", Map);

    public static ServerConfig Parse(string[] args)
    {
        var c = new ServerConfig();

        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_PORT"), out int p)) c.Port = p;
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_MAX"), out int m)) c.MaxPlayers = m;
        var envMap = Environment.GetEnvironmentVariable("FALEZIA_MAP");
        if (!string.IsNullOrWhiteSpace(envMap)) c.Map = envMap;
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_BOTS"), out int eb)) c.BotsPerTeam = eb;
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_PLAYERS"), out int ep)) c.PlayersToStart = Math.Clamp(ep, 1, 10);
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_BOTS_TEAM"), out int ebt)) c.BotsTeam = ebt;
        var envLog = Environment.GetEnvironmentVariable("FALEZIA_LOG");
        if (!string.IsNullOrWhiteSpace(envLog)) c.LogLevel = envLog.Trim().ToLowerInvariant();

        foreach (var a in args)
        {
            if (a.StartsWith("--port=") && int.TryParse(a[7..], out int ap)) c.Port = ap;
            else if (a.StartsWith("--max=") && int.TryParse(a[6..], out int am)) c.MaxPlayers = am;
            else if (a.StartsWith("--map=")) c.Map = a[6..];
            else if (a.StartsWith("--log=")) c.LogLevel = a[6..].Trim().ToLowerInvariant();
            else if (a.StartsWith("--bots=") && int.TryParse(a[7..], out int ab)) c.BotsPerTeam = Math.Clamp(ab, 0, 10);
            else if (a.StartsWith("--players=") && int.TryParse(a[10..], out int ap2)) c.PlayersToStart = Math.Clamp(ap2, 1, 10);
            else if (a.StartsWith("--bots-team=") && int.TryParse(a[12..], out int abt)) c.BotsTeam = Math.Clamp(abt, 0, 2);
            else if (a is "--quiet" or "-q") { c.Verbose = false; c.LogLevel = "warn"; }
        }
        return c;
    }
}
