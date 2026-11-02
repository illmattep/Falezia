using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Falezia.Control;

/// <summary>Un processo di gioco gestito dal control plane.</summary>
public sealed class GameInstance
{
    public string Id { get; set; } = "";
    public int Port { get; set; }
    public string Map { get; set; } = "";
    public int MaxPlayers { get; set; }
    /// <summary>starting · ready · stopped · dead</summary>
    public string State { get; set; } = "starting";
    public int Players { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int? ExitCode { get; set; }
    public string LastLine { get; set; } = "";
    public long Lines { get; set; }
    public int Pid { get; set; }

    [JsonIgnore] public Process Proc { get; set; }
    [JsonIgnore] public string LogSource => "g" + Id.TrimStart('g');
}

/// <summary>
/// Chi accende e spegne le partite. Un'istanza = un processo del server dedicato,
/// con la sua porta UDP, la sua mappa e i suoi limiti. Il control plane non simula
/// niente: legge il log del processo per sapere quanti giocatori ha dentro.
/// </summary>
public sealed class InstanceManager
{
    private readonly ControlConfig _cfg;
    private readonly LogHub _log;
    private readonly Dictionary<string, GameInstance> _instances = new();
    private readonly object _lock = new();
    private int _next;

    public InstanceManager(ControlConfig cfg, LogHub log)
    {
        _cfg = cfg;
        _log = log;
    }

    public List<GameInstance> All()
    {
        lock (_lock) return _instances.Values.OrderBy(i => i.Id).ToList();
    }

    public GameInstance Get(string id)
    {
        lock (_lock) return _instances.TryGetValue(id, out var i) ? i : null;
    }

    public int Count { get { lock (_lock) return _instances.Values.Count(i => i.State != "dead" && i.State != "stopped"); } }

    /// <summary>Compila una volta il server. Incrementale: se niente è cambiato costa poco.</summary>
    public async Task<(bool ok, string output)> BuildAsync()
    {
        var project = Path.Combine(_cfg.RepoRoot, "server/Falezia.Server/Falezia.Server.csproj");
        if (!File.Exists(project)) return (false, $"progetto non trovato: {project}");

        var sw = Stopwatch.StartNew();
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = _cfg.RepoRoot
        };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(project);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("--nologo");
        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("q");
        psi.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        try
        {
            using var p = Process.Start(psi);
            string stdout = await p.StandardOutput.ReadToEndAsync();
            string stderr = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            string all = (stdout + "\n" + stderr).Trim();
            bool ok = p.ExitCode == 0;
            _log.Add("build", ok ? "info" : "error",
                ok ? $"server compilato in {sw.Elapsed.TotalSeconds:F1}s" : $"build fallita: {all}");
            return (ok, all);
        }
        catch (Exception e)
        {
            _log.Error($"build impossibile: {e.Message}");
            return (false, e.Message);
        }
    }

    public GameInstance Start()
    {
        if (!TryPickPort(out int port))
            throw new InvalidOperationException($"nessuna porta UDP libera in {_cfg.PortRangeStart}-{_cfg.PortRangeEnd}");

        string id = "g" + Interlocked.Increment(ref _next);
        var inst = new GameInstance
        {
            Id = id,
            Port = port,
            Map = _cfg.Map,
            MaxPlayers = _cfg.MaxPlayersPerInstance
        };

        string dll = _cfg.ResolveServerDll();
        string command = _cfg.ServerCommand
            .Replace("{server}", _cfg.ResolveServerLauncher())
            .Replace("{dll}", dll)
            .Replace("{port}", port.ToString())
            .Replace("{max}", inst.MaxPlayers.ToString())
            .Replace("{map}", _cfg.Map)
            .Replace("{root}", _cfg.RepoRoot)
            .Replace("{level}", _cfg.LogLevel)
            .Replace("{bots}", _cfg.BotsPerTeam.ToString());

        var (exe, argv) = SplitCommand(command);
        if (exe.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            argv.Insert(0, exe);
            exe = "dotnet";
        }

        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            WorkingDirectory = _cfg.RepoRoot,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var a in argv) psi.ArgumentList.Add(a);
        psi.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        try
        {
            var proc = Process.Start(psi);
            inst.Proc = proc;
            inst.Pid = proc.Id;
            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) =>
            {
                inst.ExitCode = SafeExit(proc);
                inst.State = inst.State == "stopped" ? "stopped" : "dead";
                _log.Warn($"istanza {inst.Id} terminata (exit {inst.ExitCode})", inst.LogSource);
            };

            lock (_lock) _instances[id] = inst;
            _log.Info($"istanza {id} avviata · UDP {port} · mappa {_cfg.Map} · max {inst.MaxPlayers}", inst.LogSource);

            _ = Task.Run(() => PumpAsync(inst, proc.StandardOutput, "info"));
            _ = Task.Run(() => PumpAsync(inst, proc.StandardError, "warn"));
            return inst;
        }
        catch (Exception e)
        {
            inst.State = "dead";
            lock (_lock) _instances[id] = inst;
            _log.Error($"istanza {id} non avviata: {e.Message}", inst.LogSource);
            throw;
        }
    }

    private static int SafeExit(Process p)
    {
        try { return p.ExitCode; } catch { return -1; }
    }

    private async Task PumpAsync(GameInstance inst, StreamReader reader, string level)
    {
        while (true)
        {
            string line = await reader.ReadLineAsync();
            if (line == null) break;
            inst.Lines++;
            inst.LastLine = line;
            Classify(inst, line);
            _log.Add(inst.LogSource, level, line);
        }
    }

    /// <summary>Il log del server è la nostra telemetria: da qui ricaviamo stato e giocatori.</summary>
    private void Classify(GameInstance inst, string line)
    {
        if (line.Contains("in ascolto su UDP", StringComparison.OrdinalIgnoreCase))
            inst.State = "ready";
        else if (line.Contains("join:", StringComparison.OrdinalIgnoreCase))
            inst.Players = Math.Min(inst.MaxPlayers, inst.Players + 1);
        else if (line.Contains("timeout:", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("kick:", StringComparison.OrdinalIgnoreCase))
            inst.Players = Math.Max(0, inst.Players - 1);
        else if (line.Contains("server fermato", StringComparison.OrdinalIgnoreCase))
            inst.State = "stopped";
        else if (line.Contains("fase:", StringComparison.OrdinalIgnoreCase))
            if (inst.State == "ready") inst.State = "ready";
    }

    /// <summary>Chiede al server di uscire dal suo stesso stdin: chiusura pulita, non un kill.</summary>
    public bool Stop(string id)
    {
        var inst = Get(id);
        if (inst?.Proc == null) return false;
        if (inst.State is "stopped" or "dead") return true;

        try
        {
            inst.State = "stopped";
            inst.Proc.StandardInput.WriteLine("quit");
            inst.Proc.StandardInput.Flush();
            if (!inst.Proc.WaitForExit(4000))
            {
                _log.Warn($"istanza {id}: uscita forzata", inst.LogSource);
                try { inst.Proc.Kill(entireProcessTree: true); } catch { }
            }
            _log.Info($"istanza {id} fermata", inst.LogSource);
            return true;
        }
        catch (Exception e)
        {
            _log.Error($"istanza {id}: stop fallito ({e.Message})", inst.LogSource);
            return false;
        }
    }

    public void StopAll()
    {
        foreach (var i in All()) Stop(i.Id);
    }

    /// <summary>La prima istanza con posto libero, oppure null.</summary>
    public GameInstance WithFreeSlot()
    {
        lock (_lock)
            return _instances.Values
                .Where(i => i.State == "ready" && i.Players < i.MaxPlayers)
                .OrderBy(i => i.Players)
                .ThenBy(i => i.Id)
                .FirstOrDefault();
    }

    private bool TryPickPort(out int port)
    {
        for (int p = _cfg.PortRangeStart; p <= _cfg.PortRangeEnd; p++)
        {
            bool used;
            lock (_lock) used = _instances.Values.Any(i => i.Port == p && i.State is not ("dead" or "stopped"));
            if (used) continue;
            if (!UdpFree(p)) continue;
            port = p;
            return true;
        }
        port = 0;
        return false;
    }

    private static bool UdpFree(int port)
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch (SocketException) { return false; }
    }

    /// <summary>Divide una riga di comando rispettando le virgolette.</summary>
    public static (string exe, List<string> args) SplitCommand(string command)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        bool quote = false;

        foreach (char c in command)
        {
            if (c == '"') { quote = !quote; continue; }
            if (char.IsWhiteSpace(c) && !quote)
            {
                if (sb.Length > 0) { parts.Add(sb.ToString()); sb.Clear(); }
                continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) parts.Add(sb.ToString());

        if (parts.Count == 0) throw new InvalidOperationException("comando vuoto");
        return (parts[0], parts.Skip(1).ToList());
    }

    /// <summary>Apre il client (player o editor) dal pannello, per provare subito.</summary>
    public bool StartClient(int port)
    {
        if (string.IsNullOrWhiteSpace(_cfg.ClientCommand)) return false;

        string command = _cfg.ClientCommand
            .Replace("{port}", port.ToString())
            .Replace("{root}", _cfg.RepoRoot);

        var (exe, argv) = SplitCommand(command);
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            WorkingDirectory = _cfg.RepoRoot,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        foreach (var a in argv) psi.ArgumentList.Add(a);

        try
        {
            Process.Start(psi);
            _log.Info($"client avviato su 127.0.0.1:{port}");
            return true;
        }
        catch (Exception e)
        {
            _log.Error($"client non avviato: {e.Message}");
            return false;
        }
    }
}
