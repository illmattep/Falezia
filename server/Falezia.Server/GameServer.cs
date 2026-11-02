using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Falezia.Net;
using Falezia.Sim;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Server;

/// <summary>
/// IL SERVER DEDICATO. Nessun motore, nessuna finestra: solo la simulazione a 64 Hz
/// e un socket UDP. È l'autorità su tutto: posizioni, colpi, vite, round, crediti.
///
/// Responsabilità:
///   · riceve input (client → server), applica la simulazione, manda snapshot;
///   · risolve i colpi con <see cref="Combat.ShootPlayers"/> (muri compresi);
///   · gestisce i round con <see cref="Match"/>: acquisti, live, fine round, crediti;
///   · vittoria del round per eliminazione della squadra avversaria o allo scadere.
///
/// Il client non decide niente: manda solo l'input. Vedi docs/08-netcode-e-server.md.
/// </summary>
public sealed class GameServer
{
    private sealed class Client
    {
        public int Id;
        public int Team;              // 1 = Nord, 2 = Sud
        public IPEndPoint EndPoint;
        public InputCmd Cmd;
        public int LastCmdTick;
        public double LastSeen;
        public long Packets;
        public int SeenShots;
        /// <summary>False finché la lobby non si riempie: è in coda, non in partita.</summary>
        public bool InMatch;
    }

    private readonly ServerConfig _cfg;
    private readonly Socket _socket;
    private readonly Dictionary<string, Client> _clients = new();

    private WorldState _world;
    private SimState _state;
    private MatchState _match = new();
    private int _nextId = 1;
    private bool _matchStarted;                       // la lobby è completa: brief in corso
    private readonly HashSet<int> _survivors = new();   // chi arriva vivo alla fase di acquisto
    private readonly List<Bot> _bots = new();            // giocatori finti che giocano l'obiettivo
    private double _uptime;
    private long _packetsIn, _packetsOut;
    private volatile bool _running = true;

    private const float Dt = 1f / 64f;
    private readonly int _logRank;

    public GameServer(ServerConfig cfg)
    {
        _cfg = cfg;
        _logRank = cfg.LogRank;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Blocking = false;
    }

    // ------------------------------------------------------------------ avvio

    public void Start()
    {
        _world = LoadWorld();
        _state = new SimState();
        Match.Reset(_match);

        _socket.Bind(new IPEndPoint(IPAddress.Any, _cfg.Port));
        Log($"in ascolto su UDP {_cfg.Port} · mappa {_world.Name} · {_world.Boxes.Count} volumi · max {_cfg.MaxPlayers}");
        Log("comandi: status · round · kick <id> · quit");
        SpawnBots();
        Log($"matchmaking: in attesa di {_cfg.PlayersToStart} giocatori in coda");
    }

    /// <summary>
    /// Mette in partita i bot. Sono giocatori veri per la simulazione: hanno un id, una
    /// squadra, un'arma e un cervello (<see cref="Bot"/>). Non hanno un socket e non
    /// ricevono snapshot: servono a far girare il ciclo del round senza dieci persone.
    /// </summary>
    private void SpawnBots()
    {
        if (_cfg.BotsPerTeam <= 0) return;

        int id = 100;
        int[] teams = _cfg.BotsTeam is 1 or 2 ? new[] { _cfg.BotsTeam } : new[] { 1, 2 };

        foreach (int team in teams)
        {
            for (int i = 0; i < _cfg.BotsPerTeam && id < 250; i++, id++)
            {
                var spawns = team == 1 ? _world.SpawnsNorth : _world.SpawnsSouth;
                var feet = spawns.Count > 0 ? spawns[i % spawns.Count] : _world.FallbackFeet;

                Simulation.Spawn(_state, id, feet);
                if (_state.Get(id) is { } p)
                {
                    p.Team = team;
                    p.Credits = Match.StartCredits;
                }

                Simulation.Give(_state, id, WeaponSlot.Anvil);
                Simulation.Equip(_state, id, WeaponSlot.Anvil);

                _bots.Add(new Bot(id, team));
            }
        }

        Log(_cfg.BotsTeam is 1 or 2
            ? $"bot: {_bots.Count} tutti in squadra {( _cfg.BotsTeam == 1 ? "NORD" : "SUD")} (allenamento)"
            : $"bot: {_bots.Count} in partita ({_cfg.BotsPerTeam} per squadra)");
    }

    private WorldState LoadWorld()
    {
        try
        {
            if (File.Exists(_cfg.MapPath))
            {
                string artPath = Path.ChangeExtension(_cfg.MapPath, ".art.json");
                var w = MapLoader.Parse(File.ReadAllText(_cfg.MapPath), File.Exists(artPath) ? File.ReadAllText(artPath) : null);
                if (File.Exists(artPath)) Log($"collisione vestito: {artPath}");
                Log($"mappa caricata: {_cfg.MapPath}");
                return w;
            }
            Log($"mappa non trovata ({_cfg.MapPath}): uso il mondo di test", "warn");
        }
        catch (Exception e)
        {
            Log($"mappa non valida ({e.Message}): uso il mondo di test", "warn");
        }
        return WorldState.MakeTestWorld();
    }

    public void Stop() => _running = false;

    // ------------------------------------------------------------------ ciclo

    public void Run()
    {
        var clock = Stopwatch.StartNew();
        double next = 0;
        var last = 0.0;

        while (_running)
        {
            double now = clock.Elapsed.TotalSeconds;
            int guard = 0;
            while (next <= now && guard++ < 8)
            {
                ReceiveAll();
                Step(Dt);
                SendSnapshots();
                next += Dt;
            }

            if (now - last > 5.0 && _bots.Count > 0)
            {
                foreach (var b in _bots)
                {
                    var bp = _state.Get(b.Id);
                    if (bp == null) continue;
                    Log($"bot {b.Id}: pos {bp.Pos.X:F1},{bp.Pos.Y:F1},{bp.Pos.Z:F1} " +
                        $"vivo={bp.Alive} core={_match.Core} portatore={_match.CoreOwner}", "debug");
                }
            }

            if (now - last > 5.0)
            {
                last = now;
                Log($"tick {_state.Tick} · {_clients.Count} giocatori · fase {_match.Phase} " +
                    $"round {_match.Round} · {_match.ScoreNorth}-{_match.ScoreSouth} · " +
                    $"pkt in/out {_packetsIn}/{_packetsOut}", "debug");
            }

            Thread.Sleep(1);
        }

        _socket.Close();
        Log("server fermato");
    }

    // ------------------------------------------------------------------ rete

    private void ReceiveAll()
    {
        var buffer = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                int n = _socket.ReceiveFrom(buffer, ref remote);
                if (n <= 0) break;
                _packetsIn++;
                OnPacket(buffer, n, (IPEndPoint)remote);
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.WouldBlock or SocketError.ConnectionReset)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }

        // timeout: chi non manda input da 10 s esce
        var dead = _clients.Values.Where(c => _uptime - c.LastSeen > 10.0).ToList();
        foreach (var c in dead)
        {
            Log($"timeout: giocatore {c.Id} ({c.EndPoint})", "warn");
            _clients.Remove(c.EndPoint.ToString());
            _state.Players.Remove(c.Id);
        }
    }

    private void OnPacket(byte[] data, int len, IPEndPoint from)
    {
        var packet = data.AsSpan(0, len).ToArray();
        string key = from.ToString();
        _clients.TryGetValue(key, out var c);

        // acquisto: lo valida il server (fase, crediti, arma), non il client
        if (Protocol.TryDecodeSwitch(packet, out var switchSlot))
        {
            if (c != null) TrySwitch(c, switchSlot);
            return;
        }

        if (Protocol.TryDecodeBuy(packet, out var buySlot))
        {
            if (c != null) TryBuy(c, buySlot);
            return;
        }

        if (Protocol.TryDecodeDraft(packet, out var draftSpec, out var draftIntent))
        {
            if (c != null) TryDraft(c, draftSpec, draftIntent);
            return;
        }

        if (Protocol.TryDecodeDrop(packet))
        {
            if (c != null) TryDrop(c);
            return;
        }

        if (Protocol.TryDecodePickup(packet))
        {
            if (c != null) TryPickup(c);
            return;
        }

        if (!Protocol.TryDecodeInput(packet, out var cmd)) return;

        if (c == null)
        {
            if (_clients.Count >= _cfg.MaxPlayers) return;
            c = Join(from);
            if (c == null) return;
        }

        c.Cmd = cmd;
        c.LastCmdTick = cmd.Tick;
        c.LastSeen = _uptime;
        c.Packets++;
    }

    /// <summary>
    /// Acquisto autoritativo: solo in fase di acquisti, solo se i crediti bastano.
    /// Il client manda solo lo slot: nomi, prezzi e soldi li decide il server.
    /// </summary>
    /// <summary>Il giocatore prende in mano un'arma che possiede già.</summary>
    private void TrySwitch(Client c, WeaponSlot slot)
    {
        var p = _state.Get(c.Id);
        if (p == null || !p.Alive) return;
        int i = (int)slot;
        if (i < 0 || i >= p.Cache.Length) return;
        if (p.Cache[i] == null) return;             // non ce l'hai: niente da fare
        if (p.Weapon.Slot == slot) return;

        Simulation.Equip(_state, c.Id, slot);
        Log($"cambio arma: {c.Id} → {Arsenal.Get(slot).Name}");
    }

    private void TryBuy(Client c, WeaponSlot slot)
    {
        if (!_state.Players.TryGetValue(c.Id, out var p)) return;

        // una sola porta per le regole: ricomprata, vendita della sostituita, cassa di squadra
        var outcome = Shop.Buy(_state, _match, _world, c.Id, slot);
        if (!outcome.Ok)
        {
            Log($"acquisto rifiutato: {c.Id} → {Arsenal.Get(slot).Name} · {outcome.Text}", "warn");
            return;
        }

        c.SeenShots = 0;
        Log($"acquisto: {c.Id} → {Arsenal.Get(slot).Name} · {outcome.Text} · " +
            $"personali ${p.Credits} cassa ${Economy.TeamFunds(_match, p.Team)}");
    }

    /// <summary>G: l'arma in mano finisce a terra, con le sue munizioni.</summary>
    private void TryDrop(Client c)
    {
        var p = _state.Get(c.Id);
        if (p == null || !p.Alive) return;
        var slot = p.Weapon.Slot;
        if (!Simulation.DropCurrent(_state, _world, c.Id)) return;
        Log($"drop: {c.Id} lascia {Arsenal.Get(slot).Name} (cassa ${Economy.TeamFunds(_match, p.Team)})");
    }

    /// <summary>F: raccoglie l'arma a terra più vicina. Lo slot occupato segue la regola del negozio.</summary>
    private void TryPickup(Client c)
    {
        var p = _state.Get(c.Id);
        if (p == null || !p.Alive) return;
        if (!Simulation.Pickup(_state, _world, _match, c.Id, out var slot)) return;
        c.SeenShots = 0;
        Log($"raccolta: {c.Id} prende {Arsenal.Get(slot).Name}");
    }

    /// <summary>
    /// La scelta del draft (doc 10). Il server la accetta solo nel riscaldamento: al via del
    /// round 1 resta quella per tutta la partita, come dice il pilastro "loadout pubblico".
    /// </summary>
    private void TryDraft(Client c, Specialization spec, byte intent)
    {
        var p = _state.Get(c.Id);
        if (p == null) return;

        if (!Match.TryDraft(_match, p, spec, intent))
        {
            if (_match.Phase != RoundPhase.Warmup)
                Log($"draft rifiutato: {c.Id} ha scelto fuori tempo massimo", "warn");
            return;
        }

        string weapon = intent == 0 ? "nessuna preferenza" : Arsenal.Get((WeaponSlot)intent).Name;
        Log($"draft: {c.Id} → {Draft.Name(spec)} · intento {weapon}");
    }

    private Client Join(IPEndPoint from)
    {
        int id = _nextId++;
        int team = id % 2 == 1 ? 1 : 2;      // dispari a Nord, pari a Sud (come il server Godot)

        var spawns = team == 1 ? _world.SpawnsNorth : _world.SpawnsSouth;
        var feet = spawns.Count > 0
            ? spawns[(id / 2) % spawns.Count]
            : _world.FallbackFeet;

        var c = new Client
        {
            Id = id,
            Team = team,
            EndPoint = from,
            LastSeen = _uptime,
            SeenShots = 0
        };

        Simulation.Spawn(_state, id, feet);
        var p = _state.Get(id);
        p.Team = team;
        p.Credits = Economy.PersonalStart;      // fondo personale (doc 04)

        // in acquisti o in riscaldamento si può entrare; a round vivo si entra e si aspetta
        _clients[from.ToString()] = c;
        c.InMatch = false;
        Log($"coda: giocatore {id} ({_clients.Count}/{_cfg.PlayersToStart}) da {from}");

        TryFormLobby();
        SendSnapshot(c);
        return c;
    }

    /// <summary>
    /// LA LOBBY. Quando in coda ci sono abbastanza giocatori la partita si apre: fase di
    /// BRIEF (un minuto, si chiude prima se scelgono tutti), poi acquisti e round.
    /// </summary>
    private void TryFormLobby()
    {
        if (_matchStarted) return;

        int queued = _clients.Values.Count(c => !c.InMatch);
        if (queued < _cfg.PlayersToStart) return;

        _matchStarted = true;
        Match.Reset(_match);                       // Warmup = BRIEF, 60 s

        foreach (var c in _clients.Values)
        {
            var spawns = c.Team == 1 ? _world.SpawnsNorth : _world.SpawnsSouth;
            var feet = spawns.Count > 0 ? spawns[(c.Id / 2) % spawns.Count] : _world.FallbackFeet;
            Simulation.Spawn(_state, c.Id, feet);
            if (_state.Get(c.Id) is { } p)
            {
                p.Team = c.Team;
                p.Credits = Economy.PersonalStart;
            }
            c.InMatch = true;
            c.SeenShots = 0;
        }

        Log($"lobby completa: {queued} giocatori · BRIEF {Match.BriefSeconds} s");
    }

    private void SendSnapshots()
    {
        foreach (var c in _clients.Values) SendSnapshot(c);
    }

    private void SendSnapshot(Client c)
    {
        try
        {
            var data = Protocol.EncodeSnapshot(_state.Tick, c.LastCmdTick, c.Id,
                                               _state.Players.Values, _match, _state.Drops,
                                               queueCount: _clients.Values.Count(x => !x.InMatch),
                                               queueNeeded: _cfg.PlayersToStart,
                                               inMatch: c.InMatch);
            _socket.SendTo(data, c.EndPoint);
            _packetsOut++;
        }
        catch (SocketException) { /* il client è andato: lo prende il timeout */ }
        catch (ObjectDisposedException) { }
    }

    // ------------------------------------------------------------------ tick

    private void Step(float dt)
    {
        _uptime += dt;
        _state.Tick++;

        // finché la lobby non è completa non c'è partita da simulare
        if (!_matchStarted) return;

        foreach (var c in _clients.Values)
        {
            if (!_state.Players.TryGetValue(c.Id, out var p)) continue;

            // il portatore del Nucleo cammina: niente sprint finché ce l'ha in mano
            if (Objective.IsCarrier(_match, c.Id)) c.Cmd.Sprint = false;

            if (p.Alive)
            {
                Simulation.ApplyInput(_state, c.Id, c.Cmd, _world, dt);
                Simulation.ApplyWeapon(_state, c.Id, c.Cmd.Fire, c.Cmd.Reload, c.Cmd.Ads, dt);
            }

            // Si spara solo a round vivo: durante il draft e gli acquisti i colpi non fanno danno.
            // Il contatore va comunque allineato, altrimenti al fischio d'inizio partirebbe una raffica fantasma.
            if (_match.Phase == RoundPhase.Live) ResolveShots(c.Id, p, ref c.SeenShots);
            else c.SeenShots = p.Weapon.Shots;
        }

        // i bot pensano e si muovono come i client: stessa simulazione, nessun socket
        foreach (var bot in _bots)
        {
            var bp = _state.Get(bot.Id);
            if (bp == null) continue;

            var cmd = bot.Think(_state, _match, _world, dt);
            if (Objective.IsCarrier(_match, bot.Id)) cmd.Sprint = false;

            if (bp.Alive)
            {
                Simulation.ApplyInput(_state, bot.Id, cmd, _world, dt);
                Simulation.ApplyWeapon(_state, bot.Id, cmd.Fire, cmd.Reload, cmd.Ads, dt);
            }

            if (_match.Phase == RoundPhase.Live) ResolveShots(bot.Id, bp, ref bot.SeenShots);
            else bot.SeenShots = bp.Weapon.Shots;
        }

        Simulation.StepDrops(_state, _world, dt);

        var before = _match.Phase;

        // BRIEF: se hanno scelto tutti non si aspetta il minuto intero
        if (_match.Phase == RoundPhase.Warmup)
        {
            var drafters = _clients.Values.Where(c => c.InMatch).Select(c => c.Id).ToList();
            if (Match.AllDrafted(_state, drafters))
            {
                Match.EndBrief(_match);
                Log("brief: hanno scelto tutti · si va agli acquisti");
            }
        }

        // il ciclo dell'obiettivo: apertura, prelievo, trasporto, avvio, assedio
        foreach (var e in Objective.Step(_state, _match, _world, dt)) OnCoreEvent(e);
        Match.Tick(_match, dt);

        // BRIEF SCADUTO: chi non ha bloccato la scelta la prende comunque. Meglio una
        // specializzazione per posto in coda che entrare in partita senza (doc 20).
        if (before == RoundPhase.Warmup && _match.Phase != RoundPhase.Warmup)
        {
            var drafters = _clients.Values.Where(c => c.InMatch).Select(c => c.Id).ToList();
            int auto = Match.AutoDraft(_state, drafters);
            if (auto > 0)
                Log($"brief scaduto: {auto} senza scelta · specializzazione automatica");
        }

        if (_match.Phase == RoundPhase.Live) CheckTeamWipe();
        if (before != _match.Phase) OnPhase(before, _match.Phase);
    }

    /// <summary>
    /// Ogni colpo partito (contatore Shots) viene risolto QUI: stessa direzione del
    /// client (seme del colpo), danno del server. Il client disegna solo gli effetti.
    /// </summary>
    private void ResolveShots(int playerId, PlayerState p, ref int seenShots)
    {
        if (p.Weapon.Shots == seenShots) return;

        int shots = p.Weapon.Shots;
        seenShots = shots;

        var def = Arsenal.Get(p.Weapon.Slot);
        if (def.Melee) return;

        var eye = new Vec3(p.Pos.X, p.Pos.Y + Constants.Height * 0.5f - 0.25f, p.Pos.Z);

        for (int i = 0; i < def.Pellets; i++)
        {
            var dir = Arsenal.PelletDirection(p.Yaw, p.Pitch, p.Weapon.ShotSpread, p.Weapon.ShotSeed, i, def.Pellets);
            var hit = Combat.ShootPlayers(_world, _state, p.Id, eye, dir, def);

            if (hit.Target < 0 || !hit.Hit) continue;
            if (hit.Killed)
            {
                p.Kills++;
                Match.AddCredits(p, Match.KillReward);       // la taglia è personale
                var victim = _state.Get(hit.Target);
                Simulation.DropOnDeath(_state, _world, victim);   // le sue armi restano a terra
                Log($"kill: {p.Id} ha abbattuto {hit.Target} ({(victim?.Team == 1 ? "NORD" : "SUD")})");
            }
        }
    }

    /// <summary>Se una squadra resta senza giocatori vivi, il round finisce.</summary>
    private void CheckTeamWipe()
    {
        int aliveN = 0, aliveS = 0, membersN = 0, membersS = 0;
        foreach (var p in _state.Players.Values)
        {
            if (p.Team == 1) { membersN++; if (p.Alive) aliveN++; }
            else if (p.Team == 2) { membersS++; if (p.Alive) aliveS++; }
        }

        // il wipe conta solo se ENTRAMBE le squadre hanno giocatori: in allenamento
        // (o a inizio partita) una squadra vuota non è una squadra eliminata
        if (membersN < 2 || membersS < 2) return;
        if (aliveN == 0 && aliveS > 0) Match.EndRound(_match, 2);
        else if (aliveS == 0 && aliveN > 0) Match.EndRound(_match, 1);
    }

    /// <summary>Cambio di fase: qui si danno i crediti e si rinasce.</summary>
    private void OnPhase(RoundPhase before, RoundPhase now)
    {
        Log($"fase: {before} → {now} (round {_match.Round})");

        if (now == RoundPhase.RoundEnd)
        {
            // il premio di round va nella CASSA DELLA SQUADRA (una volta per squadra): è la
            // parte condivisa dell'economia. Le uccisioni restano nel fondo personale.
            _survivors.Clear();
            foreach (var p in _state.Players.Values)
                if (p.Alive) _survivors.Add(p.Id);          // chi è vivo tiene le armi al round dopo

            foreach (int team in _state.Players.Values.Select(p => p.Team).Distinct())
            {
                int reward = Match.Reward(_match, team);
                Economy.AddTeamFunds(_match, team, reward);
                Log($"premio round: squadra {(team == 1 ? "NORD" : "SUD")} +${reward} " +
                    $"· cassa ${Economy.TeamFunds(_match, team)}");
            }
        }

        if (now == RoundPhase.Buy)
        {
            Simulation.ClearDrops(_state);          // le armi a terra non passano il round

            foreach (var p in _state.Players.Values.ToList())
            {
                var spawns = p.Team == 1 ? _world.SpawnsNorth : _world.SpawnsSouth;
                var feet = spawns.Count > 0 ? spawns[(p.Id / 2) % spawns.Count] : _world.FallbackFeet;

                // vivo alla fine del round = tiene le armi (con i caricatori pieni);
                // morto = riparte dalla Standard, come in Valorant
                Simulation.StartRound(_state, p.Id, feet, keepLoadout: _survivors.Contains(p.Id));
                if (_state.Get(p.Id) is { } q) q.Team = p.Team;
            }

            foreach (var c in _clients.Values) c.SeenShots = 0;
            foreach (var b in _bots) { b.SeenShots = 0; b.ForgetRoute(); }

            Log($"round {_match.Round}: tutti in piedi · personali " +
                string.Join(", ", _state.Players.Values.Select(p => $"{p.Id}:${p.Credits}")) +
                $" · casse N/S ${_match.TeamFundsNorth}/${_match.TeamFundsSouth}");
        }
    }

    /// <summary>Gli eventi dell'obiettivo finiscono nel log: è così che si vede il round giocarsi.</summary>
    private void OnCoreEvent(CoreEvent e)
    {
        switch (e)
        {
            case CoreEvent.CassaAperta:
                Log("obiettivo: CASSA APERTA");
                break;

            case CoreEvent.NucleoPrelevato:
            {
                var p = _state.Get(_match.CoreOwner);
                Log($"obiettivo: NUCLEO PRELEVATO da {_match.CoreOwner} ({(p?.Team == 1 ? "NORD" : "SUD")})");
                break;
            }

            case CoreEvent.NucleoLasciato:
                Log("obiettivo: NUCLEO A TERRA · chi lo vuole deve andare a prenderlo", "warn");
                break;

            case CoreEvent.AvvioEstrazione:
                Log($"obiettivo: ESTRAZIONE AVVIATA · " +
                    $"{(_match.ExtractZone == Objective.ZoneEast ? "GROTTA (Est)" : "GALLERIA (Ovest)")} · " +
                    $"assedio {Objective.SiegeSeconds:F0}s");
                break;

            case CoreEvent.Estratto:
                Log($"obiettivo: ESTRAZIONE COMPLETATA · vince " +
                    $"{(_match.LastWinner == 1 ? "NORD" : "SUD")}");
                break;

            case CoreEvent.NucleoRimosso:
                Log($"obiettivo: NUCLEO RIMOSSO durante l'assedio · vince " +
                    $"{(_match.LastWinner == 1 ? "NORD" : "SUD")}");
                break;
        }
    }

    // ------------------------------------------------------------------ comandi

    public void Command(string line)
    {
        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        switch (parts[0].ToLowerInvariant())
        {
            case "status":
                Log($"tick {_state.Tick} · uptime {_uptime:F0}s · fase {_match.Phase} · " +
                    $"round {_match.Round} · {_match.ScoreNorth}-{_match.ScoreSouth} · " +
                    $"casse ${_match.TeamFundsNorth}/${_match.TeamFundsSouth} · " +
                    $"coda {_clients.Values.Count(c => !c.InMatch)}/{_cfg.PlayersToStart} · " +
                    $"a terra {_state.Drops.Count} · " +
                    $"giocatori {_clients.Count}/{_cfg.MaxPlayers} · pkt {_packetsIn}/{_packetsOut}");
                foreach (var c in _clients.Values)
                {
                    var p = _state.Get(c.Id);
                    Log($"  {c.Id} {(c.Team == 1 ? "NORD" : "SUD")} {c.EndPoint} " +
                        $"vita {p?.Health:F0} personali ${p?.Credits} " +
                        $"arma {Arsenal.Get(p?.Weapon.Slot ?? WeaponSlot.Standard).Name} " +
                        $"k/d {p?.Kills}/{p?.Deaths}");
                }
                break;

            case "round":
                Log($"round {_match.Round} · fase {_match.Phase} · {_match.TimeLeft:F1}s · " +
                    $"{_match.ScoreNorth}-{_match.ScoreSouth}");
                break;

            case "kick" when parts.Length == 2 && int.TryParse(parts[1], out int id):
                var victim = _clients.Values.FirstOrDefault(c => c.Id == id);
                if (victim != null)
                {
                    _clients.Remove(victim.EndPoint.ToString());
                    _state.Players.Remove(id);
                    Log($"kick: giocatore {id}");
                }
                break;

            case "quit" or "stop":
                Stop();
                break;

            case "help":
                Log("status · round · kick <id> · quit");
                break;
        }
    }

    /// <summary>
    /// Scrive una riga sul log. Il livello scelto nel pannello (o con --log=) è una
    /// soglia: quello che sta sotto non viene stampato. Il control plane legge questo
    /// stdout, quindi filtrare qui alleggerisce anche ciò che arriva al pannello.
    /// </summary>
    private void Log(string msg, string level = "info")
    {
        int rank = level switch
        {
            "debug" => 0,
            "warn" => 2,
            "error" => 3,
            _ => 1
        };
        if (rank < _logRank) return;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    }
}
