using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim;

/// <summary>Input di un tick. È l'unica cosa che il client ha il diritto di inviare.</summary>
public struct InputCmd
{
    public int Tick;
    public Vector2 Move;      // X = strafe, Y = avanti
    public float Yaw;
    public float Pitch;
    public bool Jump;
    public bool Sprint;
    public bool Crouch;
    public bool Ads;
}

/// <summary>Stato di un giocatore. <see cref="Pos"/> è il CENTRO dell'AABB, non i piedi.</summary>
public sealed class PlayerState
{
    public int Id;
    public Vec3 Pos;
    public Vec3 Vel;
    public float Yaw;
    public float Pitch;
    public bool OnFloor;
    public bool Crouching;
    public float Stamina = Constants.StaminaMax;
    public float JumpCd;

    public void CopyFrom(PlayerState o)
    {
        Id = o.Id; Pos = o.Pos; Vel = o.Vel; Yaw = o.Yaw; Pitch = o.Pitch;
        OnFloor = o.OnFloor; Crouching = o.Crouching; Stamina = o.Stamina; JumpCd = o.JumpCd;
    }

    public PlayerState Clone()
    {
        var p = new PlayerState();
        p.CopyFrom(this);
        return p;
    }
}

/// <summary>Scatola statica del mondo. Niente mesh, niente fisica del motore.</summary>
public struct Box
{
    public Vec3 Min;
    public Vec3 Size;
    public Vec3 Max => Min + Size;
    public Vec3 Center => Min + Size * 0.5f;
}

/// <summary>Mondo statico. Client e server costruiscono lo STESSO mondo dalla stessa funzione.</summary>
public sealed class WorldState
{
    public readonly List<Box> Boxes = new();

    public void AddBox(Vec3 min, Vec3 size) => Boxes.Add(new Box { Min = min, Size = size });

    public static WorldState MakeTestWorld()
    {
        var w = new WorldState();
        w.AddBox(new Vec3(-60, -1, -60), new Vec3(120, 1, 120));   // pavimento
        w.AddBox(new Vec3(-10, 0, -6), new Vec3(6, 3, 12));        // muro lungo (-X)
        w.AddBox(new Vec3(2, 0, 5), new Vec3(2.5f, 1, 2.5f));      // cassa 1 m
        w.AddBox(new Vec3(8, 0, -4), new Vec3(2, 5, 2));           // torre 5 m
        w.AddBox(new Vec3(-4, 0, 8), new Vec3(6, 0.4f, 2));        // gradino 40 cm
        w.AddBox(new Vec3(0, 0, -10), new Vec3(8, 1.1f, 0.4f));    // muretto 1,1 m
        return w;
    }
}

/// <summary>Stato completo della simulazione. Oggetto puro, serializzabile, clonabile.</summary>
public sealed class SimState
{
    public int Tick;
    public readonly Dictionary<int, PlayerState> Players = new();

    public PlayerState Get(int id) => Players.TryGetValue(id, out var p) ? p : null;

    public PlayerState GetOrCreate(int id)
    {
        if (!Players.TryGetValue(id, out var p))
        {
            p = new PlayerState { Id = id };
            Players[id] = p;
        }
        return p;
    }

    public SimState Clone()
    {
        var s = new SimState { Tick = Tick };
        foreach (var kv in Players) s.Players[kv.Key] = kv.Value.Clone();
        return s;
    }
}
