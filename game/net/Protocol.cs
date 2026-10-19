using Falezia.Sim;

namespace Falezia.Net;

/// <summary>Snapshot per UN client: contiene l'ack dei SUOI input, così la riconciliazione è esatta.</summary>
public sealed class Snapshot
{
    public int Tick;
    public int Ack;
    public int YourId;
    public SimState State;
}

/// <summary>
/// Protocollo binario. Puro C#: si serializza e deserializza anche fuori da Godot.
/// Canale di gioco = inaffidabile, non ordinato. L'input lo decide il client,
/// lo stato lo decide il server. Sempre.
/// </summary>
public static class Protocol
{
    public const byte MsgInput = 1;
    public const byte MsgSnapshot = 2;

    [Flags]
    private enum InputFlags : byte
    {
        None = 0, Jump = 1, Sprint = 2, Crouch = 4, Ads = 8
    }

    [Flags]
    private enum PlayerFlags : byte
    {
        None = 0, OnFloor = 1, Crouch = 2
    }

    // ------------------------------------------------------------------ input

    public static byte[] EncodeInput(in InputCmd c)
    {
        using var ms = new MemoryStream(32);
        using var w = new BinaryWriter(ms);
        w.Write(MsgInput);
        w.Write(c.Tick);
        w.Write(c.Move.X);
        w.Write(c.Move.Y);
        w.Write(c.Yaw);
        w.Write(c.Pitch);
        var f = InputFlags.None;
        if (c.Jump) f |= InputFlags.Jump;
        if (c.Sprint) f |= InputFlags.Sprint;
        if (c.Crouch) f |= InputFlags.Crouch;
        if (c.Ads) f |= InputFlags.Ads;
        w.Write((byte)f);
        w.Flush();
        return ms.ToArray();
    }

    public static bool TryDecodeInput(byte[] data, out InputCmd cmd)
    {
        cmd = default;
        if (data == null || data.Length < 22) return false;
        try
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            if (r.ReadByte() != MsgInput) return false;
            cmd.Tick = r.ReadInt32();
            cmd.Move = new System.Numerics.Vector2(r.ReadSingle(), r.ReadSingle());
            cmd.Yaw = r.ReadSingle();
            cmd.Pitch = r.ReadSingle();
            var f = (InputFlags)r.ReadByte();
            cmd.Jump = f.HasFlag(InputFlags.Jump);
            cmd.Sprint = f.HasFlag(InputFlags.Sprint);
            cmd.Crouch = f.HasFlag(InputFlags.Crouch);
            cmd.Ads = f.HasFlag(InputFlags.Ads);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ------------------------------------------------------------------ snapshot

    public static byte[] EncodeSnapshot(int tick, int ack, int yourId, IEnumerable<PlayerState> players)
    {
        using var ms = new MemoryStream(512);
        using var w = new BinaryWriter(ms);
        w.Write(MsgSnapshot);
        w.Write(tick);
        w.Write(ack);
        w.Write((byte)yourId);
        var list = players.ToList();
        w.Write((byte)list.Count);
        foreach (var p in list)
        {
            w.Write((byte)p.Id);
            w.Write(p.Pos.X); w.Write(p.Pos.Y); w.Write(p.Pos.Z);
            w.Write(p.Vel.X); w.Write(p.Vel.Y); w.Write(p.Vel.Z);
            w.Write(p.Yaw);
            w.Write(p.Pitch);
            w.Write(p.Stamina);
            w.Write(p.JumpCd);
            var f = PlayerFlags.None;
            if (p.OnFloor) f |= PlayerFlags.OnFloor;
            if (p.Crouching) f |= PlayerFlags.Crouch;
            w.Write((byte)f);
        }
        w.Flush();
        return ms.ToArray();
    }

    public static bool TryDecodeSnapshot(byte[] data, out Snapshot snap)
    {
        snap = null;
        if (data == null || data.Length < 11) return false;
        try
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            if (r.ReadByte() != MsgSnapshot) return false;
            var s = new Snapshot
            {
                Tick = r.ReadInt32(),
                Ack = r.ReadInt32(),
                YourId = r.ReadByte(),
                State = new SimState()
            };
            int count = r.ReadByte();
            for (int i = 0; i < count; i++)
            {
                var p = new PlayerState();
                p.Id = r.ReadByte();
                p.Pos = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                p.Vel = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                p.Yaw = r.ReadSingle();
                p.Pitch = r.ReadSingle();
                p.Stamina = r.ReadSingle();
                p.JumpCd = r.ReadSingle();
                var f = (PlayerFlags)r.ReadByte();
                p.OnFloor = f.HasFlag(PlayerFlags.OnFloor);
                p.Crouching = f.HasFlag(PlayerFlags.Crouch);
                s.State.Players[p.Id] = p;
            }
            s.State.Tick = s.Tick;
            snap = s;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
