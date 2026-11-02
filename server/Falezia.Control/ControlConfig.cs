using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falezia.Control;

/// <summary>
/// Configurazione del control plane. Vive in <c>control.json</c> accanto ai dati
/// (in sviluppo <c>build/control/</c>, su VPS la cartella indicata da --data=).
/// La stessa build gira ovunque: si cambia il file, non il codice.
///
/// Precedenza: file &lt; variabili d'ambiente &lt; riga di comando.
/// </summary>
public sealed class ControlConfig
{
    // ---------------------------------------------------------------- pannello
    /// <summary>Indirizzo di ascolto del pannello. 127.0.0.1 in sviluppo, 0.0.0.0 su VPS.</summary>
    public string Bind { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8090;
    /// <summary>
    /// L'indirizzo che il client deve usare per raggiungere le partite. Vuoto = si deduce
    /// da Bind (con 0.0.0.0 diventa 127.0.0.1). Su VPS: IP pubblico o dominio.
    /// </summary>
    public string PublicHost { get; set; } = "";
    /// <summary>Token di amministrazione. Se vuoto ne viene generato uno al primo avvio.</summary>
    public string AdminToken { get; set; } = "";

    // ------------------------------------------------------------- matchmaking
    /// <summary>Il limite rigido: se è acceso, oltre N istanze non se ne avviano altre.</summary>
    public bool HardLimitEnabled { get; set; } = true;
    /// <summary>Numero massimo di istanze di gioco contemporanee (quando il limite è acceso).</summary>
    public int HardLimitMaxInstances { get; set; } = 2;
    public int MaxPlayersPerInstance { get; set; } = 10;
    /// <summary>Quanti giocatori in coda servono per aprire una partita.</summary>
    public int MinPlayersToStart { get; set; } = 1;
    /// <summary>Dopo quanti secondi in coda si accetta una partita non piena.</summary>
    public int QueueWaitSeconds { get; set; } = 30;
    public bool MatchmakingEnabled { get; set; } = true;

    // ------------------------------------------------------------------ istanze
    public string Map { get; set; } = "calanera.json";
    /// <summary>Bot per squadra in ogni partita (0 = nessuno). Servono a vedere il round giocarsi.</summary>
    public int BotsPerTeam { get; set; } = 0;
    public int PortRangeStart { get; set; } = 27015;
    public int PortRangeEnd { get; set; } = 27099;
    /// <summary>debug · info · warn · error — passato al server come --log=.</summary>
    public string LogLevel { get; set; } = "info";
    /// <summary>All'avvio tiene già pronta un'istanza (comodo in sviluppo).</summary>
    public bool AutoStartInstance { get; set; } = true;
    /// <summary>Compila il server una volta all'avvio: il riavvio del panel non ricompila.</summary>
    public bool AutoBuild { get; set; } = true;
    /// <summary>
    /// Comando per lanciare un'istanza. Segnaposto: {server} {port} {max} {map} {level} {root}.
    /// {server} diventa il binario pubblicato se c'è, altrimenti "dotnet &lt;dll&gt;" in sviluppo.
    /// </summary>
    public string ServerCommand { get; set; } =
        "{server} --port={port} --max={max} --map={map} --log={level} --bots={bots}";
    /// <summary>Percorso della DLL del server. Vuoto = quello compilato in server/Falezia.Server.</summary>
    public string ServerDll { get; set; } = "";
    /// <summary>Comando per lanciare il client dal pannello. Vuoto = pulsante disattivato.</summary>
    public string ClientCommand { get; set; } =
        "{root}/build/linux/Falezia.x86_64 -faleziaOnline 127.0.0.1:{port}";

    // --------------------------------------------------------------------- log
    public int MaxLogLines { get; set; } = 4000;

    // ------------------------------------------------------- derivati (non salvati)
    [JsonIgnore] public string RepoRoot { get; set; } = "";
    [JsonIgnore] public string DataDir { get; set; } = "";
    [JsonIgnore] public string ConfigFile => Path.Combine(DataDir, "control.json");

    /// <summary>Aggiusta i valori che il pannello potrebbe mandare fuori scala.</summary>
    public void Sanitize()
    {
        Port = Math.Clamp(Port, 1, 65535);
        HardLimitMaxInstances = Math.Clamp(HardLimitMaxInstances, 1, 64);
        MaxPlayersPerInstance = Math.Clamp(MaxPlayersPerInstance, 2, 64);
        MinPlayersToStart = Math.Clamp(MinPlayersToStart, 1, MaxPlayersPerInstance);
        QueueWaitSeconds = Math.Clamp(QueueWaitSeconds, 1, 3600);
        PortRangeStart = Math.Clamp(PortRangeStart, 1024, 65000);
        PortRangeEnd = Math.Clamp(PortRangeEnd, PortRangeStart + 1, 65535);
        MaxLogLines = Math.Clamp(MaxLogLines, 200, 20000);
        BotsPerTeam = Math.Clamp(BotsPerTeam, 0, 10);
        if (LogLevel is not ("debug" or "info" or "warn" or "error")) LogLevel = "info";
    }

    public void Save()
    {
        Sanitize();
        Directory.CreateDirectory(DataDir);
        var json = JsonSerializer.Serialize(this, JsonOpts);
        var tmp = ConfigFile + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, ConfigFile, overwrite: true);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ControlConfig Load(string[] args)
    {
        var cfg = new ControlConfig();

        // 1. la radice del repo: serve per trovare server, mappa e client
        string root = Arg(args, "--root=") ?? FindRepoRoot() ?? Directory.GetCurrentDirectory();
        cfg.RepoRoot = Path.GetFullPath(root);

        // 2. dove stanno config e dati
        cfg.DataDir = Path.GetFullPath(Arg(args, "--data=") ??
                                       Environment.GetEnvironmentVariable("FALEZIA_CONTROL_DATA") ??
                                       Path.Combine(cfg.RepoRoot, "build", "control"));

        // 3. il file, se c'è
        if (File.Exists(cfg.ConfigFile))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<ControlConfig>(
                    File.ReadAllText(cfg.ConfigFile), JsonOpts);
                if (loaded != null) cfg = Copy(loaded, cfg.RepoRoot, cfg.DataDir);
            }
            catch (JsonException)
            {
                // config illeggibile: si riparte dai valori di default, e si riscrive
            }
        }

        // 4. ambiente (utile nei container e su systemd)
        cfg.Bind = Environment.GetEnvironmentVariable("FALEZIA_CONTROL_BIND") ?? cfg.Bind;
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_CONTROL_PORT"), out int ep)) cfg.Port = ep;
        cfg.AdminToken = Environment.GetEnvironmentVariable("FALEZIA_CONTROL_TOKEN") ?? cfg.AdminToken;
        cfg.PublicHost = Environment.GetEnvironmentVariable("FALEZIA_PUBLIC_HOST") ?? cfg.PublicHost;
        if (bool.TryParse(Environment.GetEnvironmentVariable("FALEZIA_HARD_LIMIT"), out bool hl)) cfg.HardLimitEnabled = hl;
        if (int.TryParse(Environment.GetEnvironmentVariable("FALEZIA_HARD_LIMIT_MAX"), out int hlm)) cfg.HardLimitMaxInstances = hlm;

        // 5. riga di comando (vince tutto)
        if (Arg(args, "--bind=") is { } b) cfg.Bind = b;
        if (int.TryParse(Arg(args, "--port="), out int p)) cfg.Port = p;
        if (Arg(args, "--token=") is { } t) cfg.AdminToken = t;
        if (Arg(args, "--public=") is { } ph) cfg.PublicHost = ph;
        if (Arg(args, "--map=") is { } m) cfg.Map = m;
        if (int.TryParse(Arg(args, "--max="), out int mx)) cfg.MaxPlayersPerInstance = mx;
        if (Arg(args, "--log=") is { } lv) cfg.LogLevel = lv;
        if (Arg(args, "--dll=") is { } dll) cfg.ServerDll = dll;
        if (args.Any(a => a is "--no-autostart")) cfg.AutoStartInstance = false;
        if (args.Any(a => a is "--no-build")) cfg.AutoBuild = false;
        if (args.Any(a => a is "--no-matchmaking")) cfg.MatchmakingEnabled = false;

        // 6. senza token non si amministra niente: se ne genera uno e si salva
        bool fresh = string.IsNullOrWhiteSpace(cfg.AdminToken);
        if (fresh) cfg.AdminToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        cfg.Sanitize();
        cfg.Save();
        return cfg;
    }

    private static ControlConfig Copy(ControlConfig from, string root, string data)
    {
        from.RepoRoot = root;
        from.DataDir = data;
        return from;
    }

    /// <summary>
    /// Come si lancia un server su questa macchina. In sviluppo è "dotnet &lt;dll&gt;"; su una
    /// VPS il server è un eseguibile pubblicato accanto al control plane, e non c'è nessuna
    /// DLL da passare a dotnet.
    /// </summary>
    public string ResolveServerLauncher()
    {
        string quoted(string v) => v.Contains(' ') ? $"\"{v}\"" : v;

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "server", "Falezia.Server")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "server", "Falezia.Server")),
            Path.Combine(RepoRoot, "server/Falezia.Server/bin/Debug/net8.0/Falezia.Server"),
            Path.Combine(RepoRoot, "server/Falezia.Server/bin/Release/net8.0/Falezia.Server"),
        };

        foreach (var exe in candidates)
            if (File.Exists(exe)) return quoted(exe);

        return $"dotnet {quoted(ResolveServerDll())}";
    }

    /// <summary>Il segnaposto della DLL compilata, se esiste.</summary>
    public string ResolveServerDll()
    {
        if (!string.IsNullOrWhiteSpace(ServerDll) && File.Exists(ServerDll)) return ServerDll;
        var candidates = new[]
        {
            Path.Combine(RepoRoot, "server/Falezia.Server/bin/Debug/net8.0/Falezia.Server.dll"),
            Path.Combine(RepoRoot, "server/Falezia.Server/bin/Release/net8.0/Falezia.Server.dll"),
            Path.Combine(AppContext.BaseDirectory, "Falezia.Server.dll"),
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static string Arg(string[] args, string prefix)
    {
        foreach (var a in args)
            if (a.StartsWith(prefix, StringComparison.Ordinal)) return a[prefix.Length..];
        return null;
    }

    /// <summary>Risale dal binario fino alla cartella che contiene run.sh e la mappa.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "run.sh")) &&
                File.Exists(Path.Combine(dir.FullName, "game/maps/calanera.json")))
                return dir.FullName;
        }
        return null;
    }
}
