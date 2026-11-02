using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falezia.Control;

/// <summary>
/// Il pannello e la sua API. HttpListener invece di ASP.NET Core: su questa macchina
/// c'è solo il runtime .NET, non quello web. Meno dipendenze, stesso risultato, e il
/// deploy resta "un eseguibile + un json".
/// </summary>
public sealed class ApiServer : IDisposable
{
    private readonly ControlConfig _cfg;
    private readonly LogHub _log;
    private readonly InstanceManager _instances;
    private readonly Matchmaker _matchmaker;
    private readonly DateTime _started;

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly string _prefix;
    private bool _disposed;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ApiServer(ControlConfig cfg, LogHub log, InstanceManager instances, Matchmaker matchmaker, DateTime started)
    {
        _cfg = cfg;
        _log = log;
        _instances = instances;
        _matchmaker = matchmaker;
        _started = started;

        string host = cfg.Bind is "0.0.0.0" or "::" or "*" ? "*" : cfg.Bind;
        _prefix = $"http://{host}:{cfg.Port}/";
        _listener.Prefixes.Add(_prefix);
    }

    public void Start()
    {
        _listener.Start();
        _log.Info($"pannello in ascolto su {_prefix}");
        _ = Task.Run(AcceptLoop);
    }

    public void Stop()
    {
        if (_disposed) return;
        _disposed = true;
        try { _stop.Cancel(); } catch { }
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }
    }

    public void Dispose() => Stop();

    private async Task AcceptLoop()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (InvalidOperationException) { break; }

            _ = Task.Run(() => Handle(ctx));
        }
    }

    // ------------------------------------------------------------------ routing

    private async Task Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        string path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/');
        if (path.Length == 0) path = "/";
        string method = req.HttpMethod.ToUpperInvariant();

        try
        {
            if (path.StartsWith("/api/admin", StringComparison.Ordinal) && !IsAdmin(req))
            {
                await Send(res, 401, new { error = "token di amministrazione mancante o errato" });
                return;
            }

            if (path is "/" or "/index.html") { await SendHtml(res, PanelHtml.Value); return; }
            if (path is "/health") { await SendText(res, "ok"); return; }

            if (path is "/api/admin/state" && method == "GET") { await Send(res, 200, State()); return; }
            if (path is "/api/admin/settings" && method == "POST") { await SaveSettings(req, res); return; }
            if (path is "/api/admin/logs" && method == "GET") { await Logs(req, res); return; }

            if (path is "/api/admin/instances")
            {
                if (method == "POST") { await StartInstance(res); return; }
                if (method == "GET") { await Send(res, 200, _instances.All()); return; }
            }

            if (path is "/api/admin/instances/stop-all" && method == "POST")
            {
                _instances.StopAll();
                await Send(res, 200, new { ok = true });
                return;
            }

            var seg = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // /api/admin/instances/{id}/{stop|client}
            if (seg.Length == 5 && seg[0] == "api" && seg[1] == "admin" && seg[2] == "instances" && method == "POST")
            {
                string id = Uri.UnescapeDataString(seg[3]);
                var inst = _instances.Get(id);
                if (inst == null) { await Send(res, 404, new { error = "istanza non trovata" }); return; }

                if (seg[4] == "stop")
                {
                    bool ok = _instances.Stop(id);
                    await Send(res, ok ? 200 : 500, new { ok });
                    return;
                }

                if (seg[4] == "client")
                {
                    bool ok = _instances.StartClient(inst.Port);
                    await Send(res, ok ? 200 : 400, ok
                        ? new { ok, port = inst.Port }
                        : new { error = "client non avviato: controlla ClientCommand o il player compilato" });
                    return;
                }
            }

            if (path is "/api/queue" && method == "POST")
            {
                var body = await ReadJson<QueueRequest>(req);
                var t = _matchmaker.Enqueue(body?.Name);
                await Send(res, 200, new { ticket = t.Id, state = t.State, name = t.Name });
                return;
            }

            // /api/queue/{id}
            if (seg.Length == 3 && seg[0] == "api" && seg[1] == "queue")
            {
                string id = Uri.UnescapeDataString(seg[2]);

                if (method == "GET")
                {
                    var t = _matchmaker.Get(id);
                    if (t == null) { await Send(res, 404, new { error = "ticket sconosciuto" }); return; }
                    await Send(res, 200, new
                    {
                        ticket = t.Id,
                        state = t.State,
                        name = t.Name,
                        host = t.Host,
                        port = t.Port,
                        matchToken = t.MatchToken,
                        note = t.Note,
                        waitedSeconds = (int)((t.AllocatedAt ?? DateTime.UtcNow) - t.CreatedAt).TotalSeconds
                    });
                    return;
                }

                if (method == "DELETE")
                {
                    bool ok = _matchmaker.Cancel(id);
                    await Send(res, ok ? 200 : 404, ok ? new { ok } : new { error = "ticket non annullabile" });
                    return;
                }
            }

            await Send(res, 404, new { error = "rotta sconosciuta", path });
        }
        catch (Exception e)
        {
            _log.Error($"http {method} {path}: {e.Message}");
            try { await Send(res, 500, new { error = e.Message }); } catch { }
        }
    }

    private bool IsAdmin(HttpListenerRequest req)
    {
        string header = req.Headers["X-Admin-Token"];
        string query = req.QueryString["token"];
        return string.Equals(header ?? query, _cfg.AdminToken, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------ rotte

    private object State()
    {
        string dll = _cfg.ResolveServerDll();
        var tickets = _matchmaker.Tickets();
        return new
        {
            config = _cfg,
            configFile = _cfg.ConfigFile,
            hardLimit = new
            {
                enabled = _cfg.HardLimitEnabled,
                max = _cfg.HardLimitMaxInstances,
                used = _instances.Count,
                room = _cfg.HardLimitEnabled ? Math.Max(0, _cfg.HardLimitMaxInstances - _instances.Count) : -1
            },
            instances = _instances.All(),
            tickets,
            queue = tickets.Count(t => t.State == "queued"),
            uptimeSeconds = (int)(DateTime.UtcNow - _started).TotalSeconds,
            serverDll = dll,
            serverDllExists = File.Exists(dll),
            playerExists = File.Exists(Path.Combine(_cfg.RepoRoot, "build/linux/Falezia.x86_64")),
            version = "0.1"
        };
    }

    private async Task StartInstance(HttpListenerResponse res)
    {
        if (_cfg.HardLimitEnabled && _instances.Count >= _cfg.HardLimitMaxInstances)
        {
            await Send(res, 400, new
            {
                error = $"limite rigido raggiunto: {_instances.Count}/{_cfg.HardLimitMaxInstances} istanze. " +
                        "Alza il limite o spegnilo dal pannello."
            });
            return;
        }

        try
        {
            var inst = _instances.Start();
            await Send(res, 200, new { ok = true, instance = inst });
        }
        catch (Exception e)
        {
            await Send(res, 400, new { error = e.Message });
        }
    }

    private async Task SaveSettings(HttpListenerRequest req, HttpListenerResponse res)
    {
        var p = await ReadJson<SettingsPatch>(req);
        if (p == null) { await Send(res, 400, new { error = "corpo mancante" }); return; }

        if (p.HardLimitEnabled.HasValue) _cfg.HardLimitEnabled = p.HardLimitEnabled.Value;
        if (p.HardLimitMaxInstances.HasValue) _cfg.HardLimitMaxInstances = p.HardLimitMaxInstances.Value;
        if (p.MaxPlayersPerInstance.HasValue) _cfg.MaxPlayersPerInstance = p.MaxPlayersPerInstance.Value;
        if (p.MinPlayersToStart.HasValue) _cfg.MinPlayersToStart = p.MinPlayersToStart.Value;
        if (p.QueueWaitSeconds.HasValue) _cfg.QueueWaitSeconds = p.QueueWaitSeconds.Value;
        if (p.MatchmakingEnabled.HasValue) _cfg.MatchmakingEnabled = p.MatchmakingEnabled.Value;
        if (p.AutoStartInstance.HasValue) _cfg.AutoStartInstance = p.AutoStartInstance.Value;
        if (p.BotsPerTeam.HasValue) _cfg.BotsPerTeam = p.BotsPerTeam.Value;
        if (!string.IsNullOrWhiteSpace(p.Map)) _cfg.Map = p.Map.Trim();
        if (!string.IsNullOrWhiteSpace(p.LogLevel)) _cfg.LogLevel = p.LogLevel.Trim().ToLowerInvariant();
        if (p.PublicHost != null) _cfg.PublicHost = p.PublicHost.Trim();
        if (p.ClientCommand != null) _cfg.ClientCommand = p.ClientCommand;

        _cfg.Sanitize();
        _cfg.Save();
        _log.Info($"impostazioni salvate · limite rigido {(_cfg.HardLimitEnabled ? "ON" : "OFF")} " +
                  $"{_cfg.HardLimitMaxInstances} · max {_cfg.MaxPlayersPerInstance}/partita · " +
                  $"bot {_cfg.BotsPerTeam}/squadra · log {_cfg.LogLevel}");
        await Send(res, 200, new { ok = true, config = _cfg });
    }

    private async Task Logs(HttpListenerRequest req, HttpListenerResponse res)
    {
        long after = long.TryParse(req.QueryString["after"], out long a) ? a : 0;
        int limit = int.TryParse(req.QueryString["limit"], out int l) ? Math.Clamp(l, 1, 2000) : 400;

        List<LogLine> selected;
        if (after <= 0) selected = _log.Tail(limit);
        else selected = _log.Tail(4000).Where(x => x.Seq > after).Take(limit).ToList();

        long last = after;
        if (selected.Count > 0) last = selected[^1].Seq;
        else if (after <= 0) { var all = _log.Tail(1); if (all.Count > 0) last = all[0].Seq; }

        await Send(res, 200, new { lines = selected, last });
    }

    // ------------------------------------------------------------------ trasporto

    private static async Task<T> ReadJson<T>(HttpListenerRequest req) where T : class
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body)) return null;
        return JsonSerializer.Deserialize<T>(body, Json);
    }

    private static async Task Send(HttpListenerResponse res, int status, object body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, Json));
        res.StatusCode = status;
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        res.Headers["Cache-Control"] = "no-store";
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private static async Task SendHtml(HttpListenerResponse res, string html)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        res.StatusCode = 200;
        res.ContentType = "text/html; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private static async Task SendText(HttpListenerResponse res, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        res.StatusCode = 200;
        res.ContentType = "text/plain; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }
}
