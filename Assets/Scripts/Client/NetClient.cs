using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Falezia.Net;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// IL CLIENT DI RETE. Un socket UDP, un thread che riceve, una coda di snapshot.
/// Il client manda SOLO input (22 byte); lo stato lo decide il server.
///
/// Predizione: l'input si applica anche in locale (il giocatore non deve sentire
/// il ping), e quando arriva uno snapshot la posizione viene corretta se lo scarto
/// supera la soglia. È la versione semplice della riconciliazione: sufficiente per
/// giocare, da raffinare con il replay degli input non confermati (doc 08).
/// </summary>
public sealed class NetClient : MonoBehaviour
{
    private sealed class Inbox
    {
        public Snapshot Snap;
        public double Age;
    }

    public bool Online { get; private set; }
    public int MyId { get; private set; } = -1;
    public MatchState Match { get; private set; } = new();
    public int PingMs { get; private set; }

    private Socket _socket;
    private Thread _thread;
    private volatile bool _running;
    private readonly ConcurrentQueue<Snapshot> _incoming = new();
    private int _badPackets;      // snapshot che non si decodificano: protocollo disallineato
    private IPEndPoint _server;
    private float _lastSnapAt = -1f;

    /// <summary>
    /// Secondi dall'ultimo snapshot. Con UDP "connesso" non vuol dire niente: il socket non
    /// si accorge se dall'altra parte non c'è nessuno, quindi la coda guarda questo numero.
    /// </summary>
    public float SinceSnapshot => _lastSnapAt < 0f
        ? float.PositiveInfinity
        : Mathf.Max(0f, Time.realtimeSinceStartup - _lastSnapAt);

    public void Connect(string host, int port)
    {
        try
        {
            _server = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Connect(_server);
            _socket.Blocking = false;
            _running = true;

            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "net" };
            _thread.Start();
            Online = true;
            Debug.Log($"rete: connesso a {_server}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"rete: connessione fallita ({e.Message}), si gioca in locale");
            Online = false;
        }
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[4096];
        while (_running)
        {
            try
            {
                int n = _socket.Receive(buffer);
                if (n <= 0) continue;
                if (Protocol.TryDecodeSnapshot(buffer[..n], out var snap))
                {
                    _incoming.Enqueue(snap);
                }
                else
                {
                    Interlocked.Increment(ref _badPackets);
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock)
            {
                Thread.Sleep(1);
            }
            catch (Exception)
            {
                Thread.Sleep(2);
            }
        }
    }

    /// <summary>Manda l'input al server. Si chiama a ogni tick simulato.</summary>
    public void Send(in InputCmd cmd)
    {
        if (!Online || _socket == null) return;
        try
        {
            var data = Protocol.EncodeInput(cmd);
            _socket.Send(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"rete: invio fallito ({e.Message})");
        }
    }

    /// <summary>Chiede un'arma al server. È il server a decidere se i soldi bastano.</summary>
    public void SendBuy(WeaponSlot slot)
    {
        if (!Online || _socket == null) return;
        try { _socket.Send(Protocol.EncodeBuy(slot)); }
        catch (Exception e) { Debug.LogWarning($"rete: acquisto non inviato ({e.Message})"); }
    }

    /// <summary>Chiede al server di prendere in mano un'arma che hai già.</summary>
    public void SendSwitch(WeaponSlot slot)
    {
        if (!Online || _socket == null) return;
        try { _socket.Send(Protocol.EncodeSwitch(slot)); }
        catch (Exception e) { Debug.LogWarning($"rete: cambio arma non inviato ({e.Message})"); }
    }

    /// <summary>
    /// Manda la scelta del draft: specializzazione e arma dichiarata. Il server la accetta
    /// solo nel riscaldamento, poi resta quella per tutta la partita.
    /// </summary>
    public void SendDraft(Specialization spec, byte intent)
    {
        if (!Online || _socket == null) return;
        try { _socket.Send(Protocol.EncodeDraft(spec, intent)); }
        catch (Exception e) { Debug.LogWarning($"rete: draft non inviato ({e.Message})"); }
    }

    /// <summary>G: lascia l'arma in mano. Il server la fa cadere e la mette nello snapshot.</summary>
    public void SendDrop()
    {
        if (!Online || _socket == null) return;
        try { _socket.Send(Protocol.EncodeDrop()); }
        catch (Exception e) { Debug.LogWarning($"rete: drop non inviato ({e.Message})"); }
    }

    /// <summary>F: raccogli l'arma a terra più vicina.</summary>
    public void SendPickup()
    {
        if (!Online || _socket == null) return;
        try { _socket.Send(Protocol.EncodePickup()); }
        catch (Exception e) { Debug.LogWarning($"rete: raccolta non inviata ({e.Message})"); }
    }

    /// <summary>Lo snapshot più recente, o null se non è arrivato niente di nuovo.</summary>
    public Snapshot TakeLatest()
    {
        Snapshot latest = null;
        while (_incoming.TryDequeue(out var s)) latest = s;
        if (latest != null)
        {
            MyId = latest.YourId;
            Match = latest.Match;
            _lastSnapAt = Time.realtimeSinceStartup;
        }

        // un client e un server con formati diversi darebbero silenzio, non errori:
        // meglio dirlo una volta ogni tanto, dal thread principale
        int bad = Interlocked.Exchange(ref _badPackets, 0);
        if (bad > 0)
            Debug.LogWarning($"rete: {bad} snapshot non decodificati — il server ha un altro protocollo?");

        return latest;
    }

    private void OnDestroy()
    {
        _running = false;
        try { _socket?.Close(); } catch { }
    }
}
