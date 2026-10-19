using System.Numerics;
using Falezia.Net;
using Falezia.Sim;

namespace Falezia.Tests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Console.WriteLine();
        Console.WriteLine("=========== TEST M0 — simulazione (fuori da Godot) ===========");

        Gravity();
        WalkSpeed();
        SprintSpeed();
        CrouchSpeed();
        JumpHeight();
        NoAirStrafe();
        WallCollision();
        StandOnCrate();
        Determinism();
        ProtocolInput();
        ProtocolSnapshot();
        SimHasNoGodotDependency();

        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine($"  passati: {_passed}    falliti: {_failed}");
        Console.WriteLine();
        return _failed == 0 ? 0 : 1;
    }

    private static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { _passed++; Console.WriteLine($"  OK    {name}"); }
        else { _failed++; Console.WriteLine($"  FAIL  {name}   {detail}"); }
    }

    private static float HSpeed(PlayerState p) => MathF.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Z * p.Vel.Z);

    private static void Run(SimState s, WorldState w, InputCmd cmd, int ticks)
    {
        var d = new Dictionary<int, InputCmd> { [1] = cmd };
        for (int i = 0; i < ticks; i++) Simulation.Step(s, d, w);
    }

    // ------------------------------------------------------------------ test

    private static void Gravity()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 5, 0));
        Run(s, w, new InputCmd(), 180);
        var p = s.Players[1];
        Check("gravità: atterra", p.OnFloor, $"on_floor={p.OnFloor} y={p.Pos.Y:F3}");
        Check("gravità: centro a metà altezza", MathF.Abs(p.Pos.Y - Constants.Height * 0.5f) < 0.05f,
            $"y={p.Pos.Y:F3} atteso {Constants.Height * 0.5f:F3}");
    }

    private static void WalkSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 128);
        float v = HSpeed(s.Players[1]);
        Check("passo: ~4.2 m/s", MathF.Abs(v - Constants.WalkSpeed) < 0.25f, $"{v:F3}");
    }

    private static void SprintSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1), Sprint = true }, 48);
        float v = HSpeed(s.Players[1]);
        Check("sprint: ~6.5 m/s", MathF.Abs(v - Constants.SprintSpeed) < 0.35f, $"{v:F3}");
    }

    private static void CrouchSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1), Crouch = true }, 128);
        var p = s.Players[1];
        Check("accovacciato: stato attivo", p.Crouching);
        float v = HSpeed(p);
        Check("accovacciato: ~2.2 m/s", MathF.Abs(v - Constants.CrouchSpeed) < 0.25f, $"{v:F3}");
    }

    private static void JumpHeight()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd(), 30);
        float baseY = s.Players[1].Pos.Y;
        Simulation.ApplyInput(s, 1, new InputCmd { Jump = true }, w);
        float peak = s.Players[1].Pos.Y;
        for (int i = 0; i < 60; i++)
        {
            Simulation.ApplyInput(s, 1, new InputCmd(), w);
            peak = MathF.Max(peak, s.Players[1].Pos.Y);
        }
        float h = peak - baseY;
        Check("salto: altezza 0.8–1.2 m", h > 0.8f && h < 1.2f, $"{h:F3} m");
    }

    private static void NoAirStrafe()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 64);
        Simulation.ApplyInput(s, 1, new InputCmd { Jump = true }, w);
        float before = HSpeed(s.Players[1]);
        for (int i = 0; i < 22; i++)
            Simulation.ApplyInput(s, 1, new InputCmd { Move = new Vector2(1, 0) }, w);
        float after = HSpeed(s.Players[1]);
        Check("nessun air-strafe: guadagno < 0.8 m/s", after - before < 0.8f,
            $"prima {before:F3} dopo {after:F3}");
    }

    private static void WallCollision()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(-3.0f, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(-1, 0) }, 180);
        var p = s.Players[1];
        Check("muro: non penetra", p.Pos.X >= -3.75f, $"x={p.Pos.X:F3}");
        Check("muro: si ferma al contatto", MathF.Abs(p.Pos.X - (-3.7f)) < 0.1f, $"x={p.Pos.X:F3} atteso -3.700");
    }

    private static void StandOnCrate()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(3.25f, 4.0f, 6.25f));
        Run(s, w, new InputCmd(), 180);
        var p = s.Players[1];
        float expected = 1.0f + Constants.Height * 0.5f;
        Check("cassa: atterra sopra", MathF.Abs(p.Pos.Y - expected) < 0.06f,
            $"y={p.Pos.Y:F3} atteso {expected:F3}");
    }

    private static void Determinism()
    {
        var a = ScriptedRun(12345);
        var b = ScriptedRun(12345);
        Check("determinismo: due run identiche",
            Vector3.Distance(a.Players[1].Pos, b.Players[1].Pos) < 1e-6f,
            $"a={a.Players[1].Pos} b={b.Players[1].Pos}");

        var c = ScriptedRun(999);
        Check("determinismo: seed diverso → risultato diverso",
            Vector3.Distance(a.Players[1].Pos, c.Players[1].Pos) > 1e-4f);
    }

    private static SimState ScriptedRun(int seed)
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        var rng = new Random(seed);
        var d = new Dictionary<int, InputCmd>();
        for (int i = 0; i < 300; i++)
        {
            d[1] = new InputCmd
            {
                Move = new Vector2((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)),
                Yaw = (float)(rng.NextDouble() * Math.PI * 2 - Math.PI),
                Pitch = (float)(rng.NextDouble() * 2 - 1),
                Jump = rng.NextDouble() < 0.08,
                Sprint = rng.NextDouble() < 0.5,
                Crouch = rng.NextDouble() < 0.1,
                Ads = rng.NextDouble() < 0.1
            };
            Simulation.Step(s, d, w);
        }
        return s;
    }

    private static void ProtocolInput()
    {
        var c = new InputCmd
        {
            Tick = 4242,
            Move = new Vector2(-0.5f, 0.75f),
            Yaw = 1.234f,
            Pitch = -0.567f,
            Jump = true,
            Sprint = true,
            Ads = true
        };
        bool ok = Protocol.TryDecodeInput(Protocol.EncodeInput(c), out var d);
        Check("protocollo input: decodifica", ok);
        if (!ok) return;
        Check("protocollo input: tick", d.Tick == 4242);
        Check("protocollo input: move", Vector2.Distance(d.Move, c.Move) < 1e-5f);
        Check("protocollo input: yaw/pitch", MathF.Abs(d.Yaw - c.Yaw) < 1e-5f && MathF.Abs(d.Pitch - c.Pitch) < 1e-5f);
        Check("protocollo input: flag", d.Jump && d.Sprint && d.Ads && !d.Crouch);
    }

    private static void ProtocolSnapshot()
    {
        var s = new SimState();
        var p = s.GetOrCreate(3);
        p.Pos = new Vector3(1.5f, 2.5f, -3.5f);
        p.Vel = new Vector3(0.1f, 0.2f, 0.3f);
        p.Yaw = 0.9f;
        p.Pitch = -0.2f;
        p.Stamina = 7.5f;
        p.JumpCd = 0.05f;
        p.OnFloor = true;
        p.Crouching = true;

        bool ok = Protocol.TryDecodeSnapshot(
            Protocol.EncodeSnapshot(77, 33, 3, s.Players.Values), out var snap);
        Check("protocollo snapshot: decodifica", ok);
        if (!ok) return;
        Check("protocollo snapshot: tick/ack", snap.Tick == 77 && snap.Ack == 33);
        Check("protocollo snapshot: your_id", snap.YourId == 3);
        var rp = snap.State.Players[3];
        Check("protocollo snapshot: posizione", Vector3.Distance(rp.Pos, p.Pos) < 1e-5f);
        Check("protocollo snapshot: velocità", Vector3.Distance(rp.Vel, p.Vel) < 1e-5f);
        Check("protocollo snapshot: flag", rp.OnFloor && rp.Crouching);
        Check("protocollo snapshot: stamina", MathF.Abs(rp.Stamina - 7.5f) < 1e-5f);
    }

    /// <summary>
    /// Il cancello architetturale: questo file compila solo se sim/ e net/
    /// non hanno dipendenze dal motore. Se qualcuno aggiunge "using Godot",
    /// il progetto di test non compila più e la CI si ferma.
    /// </summary>
    private static void SimHasNoGodotDependency()
    {
        Check("architettura: sim/ e net/ compilano senza Godot", true);
    }
}
