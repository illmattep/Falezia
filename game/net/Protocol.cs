using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Falezia.Sim;

namespace Falezia.Net
{
    /// <summary>Snapshot per UN client: contiene l'ack dei SUOI input, così la riconciliazione è esatta.</summary>
    public sealed class Snapshot
    {
        public int Tick;
        public int Ack;
        public int YourId;
        public SimState State;
        /// <summary>Stato della partita al momento dello snapshot (round, fase, punteggio).</summary>
        public MatchState Match = new();

        // --- coda: il client mostra "in coda 1/2" finché la lobby non è completa
        public int QueueCount;
        public int QueueNeeded;
        /// <summary>Sono dentro la partita (non solo in coda)?</summary>
        public bool InMatch;
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
        public const byte MsgBuy = 3;      // client → server: "voglio comprare questa arma"
        public const byte MsgSwitch = 4;   // client → server: "prendo in mano questa"
        public const byte MsgDrop = 5;     // client → server: "lascio l'arma in mano" (G)
        public const byte MsgPickup = 6;   // client → server: "raccolgo quella a terra" (F)
        public const byte MsgDraft = 7;    // client → server: specializzazione + intento di loadout

        [Flags]
        private enum InputFlags : byte
        {
            None = 0, Jump = 1, Sprint = 2, Crouch = 4, Ads = 8, Fire = 16, Reload = 32, Interact = 64
        }

        [Flags]
        private enum PlayerFlags : byte
        {
            None = 0, OnFloor = 1, Crouch = 2, Alive = 4
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
            if (c.Fire) f |= InputFlags.Fire;
            if (c.Reload) f |= InputFlags.Reload;
            if (c.Interact) f |= InputFlags.Interact;
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
                cmd.Fire = f.HasFlag(InputFlags.Fire);
                cmd.Interact = f.HasFlag(InputFlags.Interact);
                cmd.Reload = f.HasFlag(InputFlags.Reload);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------------ acquisto

        /// <summary>Richiesta d'acquisto: 2 byte. Il server valida fase, crediti e arma.</summary>
        public static byte[] EncodeBuy(WeaponSlot slot)
        {
            return new[] { MsgBuy, (byte)slot };
        }

        public static bool TryDecodeBuy(byte[] data, out WeaponSlot slot)
        {
            slot = default;
            if (data == null || data.Length < 2 || data[0] != MsgBuy) return false;
            slot = (WeaponSlot)data[1];
            return (int)slot >= 0 && (int)slot < Arsenal.Count;
        }

        /// <summary>Cambio d'arma: 2 byte. Il server accetta solo armi che hai davvero.</summary>
        public static byte[] EncodeSwitch(WeaponSlot slot) => new[] { MsgSwitch, (byte)slot };

        public static bool TryDecodeSwitch(byte[] data, out WeaponSlot slot)
        {
            slot = default;
            if (data == null || data.Length < 2 || data[0] != MsgSwitch) return false;
            slot = (WeaponSlot)data[1];
            return (int)slot >= 0 && (int)slot < Arsenal.Count;
        }

        /// <summary>Drop (G) e raccolta (F): un byte, il resto lo decide il server.</summary>
        public static byte[] EncodeDrop() => new[] { MsgDrop };
        public static byte[] EncodePickup() => new[] { MsgPickup };

        // ------------------------------------------------------------------ draft

        /// <summary>
        /// La scelta del draft: 3 byte. Il server la accetta solo nel riscaldamento, e la
        /// ridistribuisce a tutti dentro lo snapshot (è pubblica per progetto).
        /// </summary>
        public static byte[] EncodeDraft(Specialization spec, byte intent)
            => new[] { MsgDraft, (byte)spec, intent };

        public static bool TryDecodeDraft(byte[] data, out Specialization spec, out byte intent)
        {
            spec = Specialization.None;
            intent = 0;
            if (data == null || data.Length < 3 || data[0] != MsgDraft) return false;
            spec = Draft.FromByte(data[1]);
            intent = data[2];
            return Draft.IsValid(spec);
        }

        public static bool TryDecodeDrop(byte[] data) => data is { Length: >= 1 } && data[0] == MsgDrop;
        public static bool TryDecodePickup(byte[] data) => data is { Length: >= 1 } && data[0] == MsgPickup;

        // ------------------------------------------------------------------ snapshot

        public static byte[] EncodeSnapshot(int tick, int ack, int yourId, IEnumerable<PlayerState> players,
                                            MatchState match = null, IEnumerable<GroundWeapon> drops = null,
                                            int queueCount = 0, int queueNeeded = 0, bool inMatch = true)
        {
            using var ms = new MemoryStream(512);
            using var w = new BinaryWriter(ms);
            w.Write(MsgSnapshot);
            w.Write(tick);
            w.Write(ack);
            w.Write((byte)yourId);

            // testata della partita: fase, tempo, round, punteggio
            w.Write((byte)(match?.Phase ?? RoundPhase.Warmup));
            w.Write(match?.TimeLeft ?? 0f);
            w.Write((byte)(match?.Round ?? 0));
            w.Write((byte)(match?.ScoreNorth ?? 0));
            w.Write((byte)(match?.ScoreSouth ?? 0));
            // cassa comune: l'economia è di squadra, il client deve poterla mostrare
            w.Write((ushort)Math.Clamp(match?.TeamFundsNorth ?? 0, 0, 65535));
            w.Write((ushort)Math.Clamp(match?.TeamFundsSouth ?? 0, 0, 65535));
            // coda: quanti sono pronti, quanti ne servono, e se sei già dentro la partita
            w.Write((byte)Math.Clamp(queueCount, 0, 255));
            w.Write((byte)Math.Clamp(queueNeeded, 0, 255));
            w.Write(inMatch);

            // l'obiettivo: dov'è il Nucleo, chi lo porta, quanto resta all'assedio (doc 03)
            w.Write((byte)(match?.Core ?? CorePhase.Closed));
            w.Write((byte)Math.Clamp((match?.CoreOwner ?? -1) + 1, 0, 255));   // 0 = nessuno
            w.Write((byte)(match?.CoreTeam ?? 0));
            w.Write((byte)(match?.ExtractZone ?? 0));
            w.Write(match?.SiegeLeft ?? 0f);
            var corePos = match?.CorePos ?? System.Numerics.Vector3.Zero;
            w.Write(corePos.X); w.Write(corePos.Y); w.Write(corePos.Z);
            w.Write((byte)Math.Clamp((match?.OpenProgress ?? 0f) * 100f, 0f, 100f));
            w.Write((byte)Math.Clamp((match?.Channel ?? 0f) * 20f, 0f, 255f));

            var list = players.ToList();
            w.Write((byte)list.Count);
            foreach (var p in list)
            {
                w.Write((byte)p.Id);
                w.Write((byte)p.Team);
                w.Write((byte)p.Spec);
                w.Write(p.Intent);
                w.Write((byte)p.Weapon.Slot);
                w.Write(p.Health);
                w.Write((ushort)Math.Clamp(p.Credits, 0, 65535));
                w.Write(p.Pos.X); w.Write(p.Pos.Y); w.Write(p.Pos.Z);
                w.Write(p.Vel.X); w.Write(p.Vel.Y); w.Write(p.Vel.Z);
                w.Write(p.Yaw);
                w.Write(p.Pitch);
                w.Write(p.Stamina);
                w.Write(p.JumpCd);
                var f = PlayerFlags.None;
                if (p.OnFloor) f |= PlayerFlags.OnFloor;
                if (p.Crouching) f |= PlayerFlags.Crouch;
                if (p.Alive) f |= PlayerFlags.Alive;
                w.Write((byte)f);

                // contatori degli eventi: servono al client per far SENTIRE agli altri
                // lo sparo e la ricarica (8 bit: girano, il client conta il delta)
                w.Write((byte)(p.Weapon.Shots & 0xFF));
                w.Write((byte)(p.Weapon.ReloadStarts & 0xFF));
            }

            // armi a terra: slot, munizioni (le SUE), posizione, se si è posata, id
            var dropped = drops?.ToList() ?? new List<GroundWeapon>();
            w.Write((byte)Math.Min(255, dropped.Count));
            foreach (var g in dropped)
            {
                w.Write((byte)g.Slot);
                w.Write((byte)Math.Clamp(g.Ammo, 0, 255));
                w.Write((ushort)Math.Clamp(g.Reserve, 0, 65535));
                w.Write(g.Pos.X); w.Write(g.Pos.Y); w.Write(g.Pos.Z);
                w.Write(g.Spin);
                w.Write(g.Settled);
                w.Write((byte)(g.Id & 0xFF));
            }

            w.Flush();
            return ms.ToArray();
        }

        public static bool TryDecodeSnapshot(byte[] data, out Snapshot snap)
        {
            snap = null;
            if (data == null || data.Length < 20) return false;
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
                s.Match.Phase = (RoundPhase)r.ReadByte();
                s.Match.TimeLeft = r.ReadSingle();
                s.Match.Round = r.ReadByte();
                s.Match.ScoreNorth = r.ReadByte();
                s.Match.ScoreSouth = r.ReadByte();
                s.Match.TeamFundsNorth = r.ReadUInt16();
                s.Match.TeamFundsSouth = r.ReadUInt16();
                s.QueueCount = r.ReadByte();
                s.QueueNeeded = r.ReadByte();
                s.InMatch = r.ReadBoolean();
                s.Match.Core = (CorePhase)r.ReadByte();
                s.Match.CoreOwner = r.ReadByte() - 1;
                s.Match.CoreTeam = r.ReadByte();
                s.Match.ExtractZone = r.ReadByte();
                s.Match.SiegeLeft = r.ReadSingle();
                s.Match.CorePos = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                s.Match.OpenProgress = r.ReadByte() / 100f;
                s.Match.Channel = r.ReadByte() / 20f;

                int count = r.ReadByte();
                for (int i = 0; i < count; i++)
                {
                    var p = new PlayerState();
                    p.Id = r.ReadByte();
                    p.Team = r.ReadByte();
                    p.Spec = Draft.FromByte(r.ReadByte());
                    p.Intent = r.ReadByte();
                    p.Weapon.Slot = (WeaponSlot)r.ReadByte();
                    p.Health = r.ReadSingle();
                    p.Credits = r.ReadUInt16();
                    p.Pos = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    p.Vel = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    p.Yaw = r.ReadSingle();
                    p.Pitch = r.ReadSingle();
                    p.Stamina = r.ReadSingle();
                    p.JumpCd = r.ReadSingle();
                    var f = (PlayerFlags)r.ReadByte();
                    p.OnFloor = f.HasFlag(PlayerFlags.OnFloor);
                    p.Crouching = f.HasFlag(PlayerFlags.Crouch);
                    p.Alive = f.HasFlag(PlayerFlags.Alive);
                    p.Weapon.Shots = r.ReadByte();
                    p.Weapon.ReloadStarts = r.ReadByte();
                    s.State.Players[p.Id] = p;
                }

                int dropCount = r.ReadByte();
                for (int i = 0; i < dropCount; i++)
                {
                    var g = new GroundWeapon
                    {
                        Slot = (WeaponSlot)r.ReadByte(),
                        Ammo = r.ReadByte(),
                        Reserve = r.ReadUInt16(),
                        Pos = new System.Numerics.Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                        Spin = r.ReadSingle(),
                        Settled = r.ReadBoolean(),
                        Id = r.ReadByte()
                    };
                    s.State.Drops.Add(g);
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
}
