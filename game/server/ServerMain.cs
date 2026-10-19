using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;
using Falezia.Net;
using Falezia.Sim;
using Godot;

/// <summary>
/// SERVER DEDICATO. Nessuna finestra, nessun renderer.
/// Console con comandi, log categorizzati, tick fisso a 64 Hz.
///   godot --headless --path game server/server.tscn -- --port=27015 --verbose
/// </summary>
public partial class ServerMain : Node
{
    private sealed class ClientSlot
    {
        public int Id;
        public string Ip = "";
        public int Port;
        public InputCmd LastInput;
        public int Ack;
        public double LastSeen;
        public long Packets;
    }

    private readonly PacketPeerUdp _peer = new();
    private readonly Dictionary<string, ClientSlot> _clients = new();
    private readonly ConcurrentQueue<string> _commands = new();
    private Thread _stdinThread;

    private WorldState _world;
    private SimState _state;
    private int _nextId = 1;
    private double _acc;
    private double _uptime;
    private double _statTimer;
    private int _port = Constants.ServerPort;
    private bool _verbose = true;
    private long _totalPackets;
    private long _badPackets;

    // ------------------------------------------------------------------ log

    private static void Log(string cat, string msg)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{cat,-5}] {msg}");

    private void Trace(string msg) { if (_verbose) Log("SIM", msg); }

    // ------------------------------------------------------------------ avvio

    public override void _Ready()
    {
        Engine.MaxFps = 128;
        ParseArgs();

        _world = WorldState.MakeTestWorld();
        _state = new SimState();

        var err = _peer.Bind(_port, "*", 65536);
        if (err != Error.Ok)
        {
            Log("ERR", $"bind su UDP {_port} fallito: {err}");
            GetTree().Quit(1);
            return;
        }

        _stdinThread = new Thread(ReadStdin) { IsBackground = true, Name = "stdin" };
        _stdinThread.Start();

        Log("BOOT", "==================================================");
        Log("BOOT", $" FALEZIA — server dedicato M0");
        Log("BOOT", $" porta UDP {_port} · tick {Constants.TickRate} Hz · snapshot {Constants.TickRate / Constants.SnapshotEvery} Hz");
        Log("BOOT", $" mondo: {_world.Boxes.Count} volumi statici");
        Log("BOOT", $" pid {System.Environment.ProcessId} · {RuntimeInformation()}");
        Log("BOOT", "==================================================");
        Log("BOOT", " comandi: status · players · kick <id> · help · quit");
    }

    private static string RuntimeInformation()
        => $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} · {System.Runtime.InteropServices.RuntimeInformation.OSDescription}";

    private void ParseArgs()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith("--port=")) int.TryParse(a[7..], out _port);
            else if (a == "--quiet") _verbose = false;
            else if (a == "--verbose") _verbose = true;
        }
    }

    // ------------------------------------------------------------------ ciclo

    public override void _Process(double delta)
    {
        DrainCommands();
        Receive();
        _uptime += delta;

        // Accumulatore: la simulazione non dipende dal frame rate dell'engine.
        _acc += delta;
        int steps = 0;
        while (_acc >= Constants.Dt && steps < 8)
        {
            _acc -= Constants.Dt;
            Tick();
            steps++;
        }

        _statTimer += delta;
        if (_statTimer >= 5.0)
        {
            _statTimer = 0;
            if (_clients.Count > 0 || _verbose) PrintStatus();
        }
    }

    private void Tick()
    {
        var inputs = new Dictionary<int, InputCmd>(_clients.Count);
        double now = _uptime;

        // timeout: 5 s senza pacchetti -> il client è fuori
        List<string> dead = null;
        foreach (var kv in _clients)
        {
            if (now - kv.Value.LastSeen > 5.0)
            {
                (dead ??= new List<string>()).Add(kv.Key);
                continue;
            }
            inputs[kv.Value.Id] = kv.Value.LastInput;
        }
        if (dead != null)
        {
            foreach (var k in dead)
            {
                Log("NET", $"client {_clients[k].Id} timeout, rimosso");
                _state.Players.Remove(_clients[k].Id);
                _clients.Remove(k);
            }
        }

        Simulation.Step(_state, inputs, _world);

        if (_state.Tick % Constants.SnapshotEvery == 0)
            SendSnapshots();
    }

    // ------------------------------------------------------------------ rete

    private void Receive()
    {
        while (_peer.GetAvailablePacketCount() > 0)
        {
            byte[] data = _peer.GetPacket();
            string ip = _peer.GetPacketIP();
            int port = _peer.GetPacketPort();
            string key = $"{ip}:{port}";
            _totalPackets++;

            if (!Protocol.TryDecodeInput(data, out var cmd))
            {
                _badPackets++;
                return;
            }

            if (!_clients.TryGetValue(key, out var slot))
            {
                slot = new ClientSlot { Id = _nextId++, Ip = ip, Port = port };
                _clients[key] = slot;
                Simulation.Spawn(_state, slot.Id, new Vec3(0f, 0.2f, slot.Id * 2f));
                Log("NET", $"JOIN  {ip}:{port}  ->  id {slot.Id}  ({_clients.Count} connessi)");
            }
            else if (cmd.Tick < slot.Ack - 32)
            {
                // pacchetto vecchio: lo ignoriamo (può succedere con UDP)
                return;
            }

            slot.LastInput = cmd;
            slot.Ack = cmd.Tick;
            slot.LastSeen = _uptime;
            slot.Packets++;
        }
    }

    private void SendSnapshots()
    {
        foreach (var kv in _clients)
        {
            var c = kv.Value;
            byte[] pkt = Protocol.EncodeSnapshot(_state.Tick, c.Ack, c.Id, _state.Players.Values);
            _peer.SetDestAddress(c.Ip, c.Port);
            _peer.PutPacket(pkt);
        }
    }

    // ------------------------------------------------------------------ console

    private void ReadStdin()
    {
        while (true)
        {
            string line;
            try { line = Console.ReadLine(); }
            catch { return; }
            if (line == null) return;
            _commands.Enqueue(line);
        }
    }

    private void DrainCommands()
    {
        while (_commands.TryDequeue(out var line))
        {
            var parts = line.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            switch (parts[0].ToLowerInvariant())
            {
                case "help":
                    Log("CMD", "status · players · kick <id> · say <testo> · tick · quit");
                    break;
                case "status":
                    PrintStatus();
                    break;
                case "players":
                    Log("CMD", $"{_clients.Count} client connessi");
                    foreach (var kv in _clients)
                        Log("CMD", $"  id {kv.Value.Id,-3} {kv.Value.Ip}:{kv.Value.Port,-6} pkts {kv.Value.Packets,-7} ack {kv.Value.Ack}");
                    break;
                case "kick":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                        Kick(id);
                    break;
                case "say":
                    Log("SAY", line.Length > 4 ? line[4..] : "");
                    break;
                case "tick":
                    Log("CMD", $"tick {_state.Tick} · uptime {_uptime:F1}s · passo {Constants.Dt * 1000:F2} ms");
                    break;
                case "quit":
                    Log("BOOT", "arresto richiesto da console");
                    GetTree().Quit(0);
                    return;
                default:
                    Log("CMD", $"comando sconosciuto: {parts[0]}");
                    break;
            }
        }
    }

    private void Kick(int id)
    {
        foreach (var kv in _clients)
        {
            if (kv.Value.Id == id)
            {
                Log("NET", $"KICK id {id} ({kv.Value.Ip}:{kv.Value.Port})");
                _state.Players.Remove(id);
                _clients.Remove(kv.Key);
                return;
            }
        }
        Log("CMD", $"id {id} non trovato");
    }

    private void PrintStatus()
    {
        Log("STAT", $"tick {_state.Tick,-8} client {_clients.Count,-3} pkt {_totalPackets,-8} bad {_badPackets,-5} up {_uptime:F0}s");
    }

    public override void _ExitTree()
    {
        _stdinThread?.Join(250);
        Log("BOOT", "server terminato");
    }
}
