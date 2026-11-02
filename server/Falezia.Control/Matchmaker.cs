using System.Security.Cryptography;

namespace Falezia.Control;

/// <summary>Un giocatore in coda. Il ticket è la ricevuta: dice dove andare e quando.</summary>
public sealed class Ticket
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>queued · allocated · expired · cancelled</summary>
    public string State { get; set; } = "queued";
    public string InstanceId { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    /// <summary>Monouso, legato a quella partita. Oggi è una ricevuta; domani lo valida il server.</summary>
    public string MatchToken { get; set; }
    public string Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AllocatedAt { get; set; }
}

/// <summary>
/// LA CODA. Non decide chi vince: decide solo dove giocare.
///
/// Regole, in ordine:
///   1. se una partita ha posto, il giocatore va lì;
///   2. se non c'è posto e il limite rigido lo permette, si apre una partita nuova;
///   3. se il limite rigido è spento, si apre comunque;
///   4. se il limite rigido è acceso e siamo al tetto, si aspetta in coda.
///
/// Il limite è quindi un tetto di *istanze* (e di conseguenza di costo), non un tetto
/// di giocatori: è la manopola che serve su una VPS.
/// </summary>
public sealed class Matchmaker
{
    private readonly ControlConfig _cfg;
    private readonly InstanceManager _instances;
    private readonly LogHub _log;
    private readonly Dictionary<string, Ticket> _tickets = new();
    private readonly object _lock = new();

    public Matchmaker(ControlConfig cfg, InstanceManager instances, LogHub log)
    {
        _cfg = cfg;
        _instances = instances;
        _log = log;
    }

    public List<Ticket> Tickets()
    {
        lock (_lock) return _tickets.Values.OrderByDescending(t => t.CreatedAt).ToList();
    }

    public Ticket Get(string id)
    {
        lock (_lock) return _tickets.TryGetValue(id, out var t) ? t : null;
    }

    public Ticket Enqueue(string name)
    {
        var t = new Ticket
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(),
            Name = string.IsNullOrWhiteSpace(name) ? "anonimo" : name.Trim()[..Math.Min(24, name.Trim().Length)],
            CreatedAt = DateTime.UtcNow,
            Note = _cfg.MatchmakingEnabled ? "in coda" : "matchmaking spento"
        };
        lock (_lock) _tickets[t.Id] = t;
        _log.Info($"coda: {t.Name} in attesa (ticket {t.Id})");
        return t;
    }

    public bool Cancel(string id)
    {
        var t = Get(id);
        if (t == null || t.State != "queued") return false;
        lock (_lock) t.State = "cancelled";
        _log.Info($"coda: ticket {id} annullato");
        return true;
    }

    /// <summary>Il giro della coda: chiamato una volta al secondo dal control plane.</summary>
    public void Tick()
    {
        Prune();
        if (!_cfg.MatchmakingEnabled) return;

        List<Ticket> waiting;
        lock (_lock) waiting = _tickets.Values.Where(t => t.State == "queued").OrderBy(t => t.CreatedAt).ToList();
        if (waiting.Count == 0) return;

        // si apre solo quando la coda basta, oppure quando il primo aspetta troppo
        int waited = (int)(DateTime.UtcNow - waiting[0].CreatedAt).TotalSeconds;
        if (waiting.Count < _cfg.MinPlayersToStart && waited < _cfg.QueueWaitSeconds) return;

        foreach (var t in waiting)
        {
            var inst = FindInstanceWithRoom();
            if (inst == null)
            {
                if (!CanOpenMore()) { t.Note = "limite rigido raggiunto: in attesa"; continue; }
                try
                {
                    inst = _instances.Start();
                    _log.Info($"matchmaking: aperta partita {inst.Id} su UDP {inst.Port}");
                }
                catch (Exception e)
                {
                    t.Note = e.Message;
                    _log.Error($"matchmaking: partita non aperta ({e.Message})");
                    continue;
                }
            }
            Assign(t, inst);
        }
    }

    private bool CanOpenMore() => !_cfg.HardLimitEnabled || _instances.Count < _cfg.HardLimitMaxInstances;

    private GameInstance FindInstanceWithRoom()
    {
        return _instances.All()
            .Where(i => i.State is "starting" or "ready")
            .Where(i => i.Players + ReservedFor(i.Id) < i.MaxPlayers)
            .OrderBy(i => i.Players + ReservedFor(i.Id))
            .ThenBy(i => i.Id)
            .FirstOrDefault();
    }

    /// <summary>I posti già promessi a qualcuno che non è ancora entrato in partita.</summary>
    private int ReservedFor(string instanceId)
    {
        lock (_lock)
            return _tickets.Values.Count(t => t.State == "allocated" && t.InstanceId == instanceId);
    }

    private void Assign(Ticket t, GameInstance inst)
    {
        string host = !string.IsNullOrWhiteSpace(_cfg.PublicHost) ? _cfg.PublicHost
                    : _cfg.Bind is "0.0.0.0" or "::" ? "127.0.0.1"
                    : _cfg.Bind;

        t.State = "allocated";
        t.InstanceId = inst.Id;
        t.Host = host;
        t.Port = inst.Port;
        t.AllocatedAt = DateTime.UtcNow;
        t.Note = "partita trovata";
        t.MatchToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        _log.Info($"coda: {t.Name} → {inst.Id} ({host}:{inst.Port})");
    }

    /// <summary>Pulisce i ticket vecchi e libera i posti prenotati da chi non si è presentato.</summary>
    private void Prune()
    {
        var now = DateTime.UtcNow;
        List<Ticket> stale;
        lock (_lock)
            stale = _tickets.Values
                .Where(t => t.State == "queued" && (now - t.CreatedAt).TotalMinutes > 10 ||
                            t.State == "allocated" && t.AllocatedAt.HasValue && (now - t.AllocatedAt.Value).TotalMinutes > 5)
                .ToList();

        foreach (var t in stale)
        {
            lock (_lock) t.State = "expired";
            if (t.State == "expired")
                _log.Warn($"coda: ticket {t.Id} di {t.Name} scaduto ({(t.InstanceId == null ? "mai assegnato" : "non si è presentato")})");
        }
    }
}
