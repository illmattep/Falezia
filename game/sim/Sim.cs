using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim;

/// <summary>
/// LA SIMULAZIONE. Funzioni pure, passo fisso esplicito, zero dipendenze dal motore.
/// Il client la esegue in predizione, il server in autorità, e il replay la riesegue
/// identica per la riconciliazione. È lo stesso codice, sempre.
/// </summary>
public static class Simulation
{
    private struct MoveResult
    {
        public Vec3 Pos;
        public bool Floor, Ceil, WallX, WallZ;
    }

    public static void Spawn(SimState state, int id, Vec3 feetPos)
    {
        var p = state.GetOrCreate(id);
        p.Pos = feetPos + new Vec3(0, Constants.Height * 0.5f, 0);
        p.Vel = Vec3.Zero;
        p.Yaw = 0;
        p.Pitch = 0;
        p.OnFloor = false;
        p.Crouching = false;
        p.Stamina = Constants.StaminaMax;
        p.JumpCd = 0;
    }

    /// <summary>Avanza lo stato di un tick e applica gli input ricevuti.</summary>
    public static void Step(SimState state, IReadOnlyDictionary<int, InputCmd> inputs, WorldState world, float dt = Constants.Dt)
    {
        state.Tick++;
        foreach (var kv in inputs)
            ApplyInput(state, kv.Key, kv.Value, world, dt);
    }

    /// <summary>
    /// Avanza UN giocatore senza toccare il tick. È quello che serve al replay
    /// della riconciliazione: riapplicare gli input non confermati.
    /// </summary>
    public static void ApplyInput(SimState state, int id, InputCmd cmd, WorldState world, float dt = Constants.Dt)
    {
        var p = state.Get(id);
        if (p == null) return;
        StepPlayer(p, cmd, world, dt);
    }

    private static void StepPlayer(PlayerState p, InputCmd cmd, WorldState world, float dt)
    {
        p.Yaw = cmd.Yaw;
        p.Pitch = Math.Clamp(cmd.Pitch, -1.55f, 1.55f);
        p.JumpCd = MathF.Max(0f, p.JumpCd - dt);

        var halfStand = new Vec3(Constants.Radius, Constants.Height * 0.5f, Constants.Radius);
        var halfCrouch = new Vec3(Constants.Radius, Constants.CrouchHeight * 0.5f, Constants.Radius);

        // --- accovacciato: si abbassa subito, si rialza solo se c'è spazio
        bool wantCrouch = cmd.Crouch && p.OnFloor;
        if (p.Crouching && !wantCrouch)
        {
            var raised = p.Pos + new Vec3(0, (Constants.Height - Constants.CrouchHeight) * 0.5f, 0);
            if (!OverlapsWorld(raised, halfStand, world))
            {
                p.Pos = raised;
                p.Crouching = false;
            }
            // altrimenti resta accovacciato: c'è un soffitto basso
        }
        else if (wantCrouch && !p.Crouching)
        {
            p.Pos.Y -= (Constants.Height - Constants.CrouchHeight) * 0.5f;
            p.Crouching = true;
        }

        var half = p.Crouching ? halfCrouch : halfStand;

        // --- velocità massima consentita da questo input
        bool moving = cmd.Move.Length() > 0.01f;
        float maxSpeed = Constants.WalkSpeed;
        bool sprinting = false;
        if (p.Crouching)
        {
            maxSpeed = Constants.CrouchSpeed;
        }
        else if (cmd.Ads)
        {
            maxSpeed = Constants.WalkSpeed * Constants.AdsMult;
        }
        else if (cmd.Sprint && moving && p.Stamina > 0f)
        {
            maxSpeed = Constants.SprintSpeed;
            sprinting = true;
        }

        // --- stamina
        if (sprinting && p.OnFloor)
            p.Stamina = MathF.Max(0f, p.Stamina - dt);
        else
            p.Stamina = MathF.Min(Constants.StaminaMax, p.Stamina + Constants.StaminaMax / Constants.StaminaRecovery * dt);

        // --- direzione desiderata, in spazio mondo (-Z è avanti)
        float sy = MathF.Sin(p.Yaw), cy = MathF.Cos(p.Yaw);
        var forward = new Vec3(-sy, 0f, -cy);
        var right = new Vec3(cy, 0f, -sy);
        var wish = right * cmd.Move.X + forward * cmd.Move.Y;
        if (wish.LengthSquared() > 1e-6f) wish = Vec3.Normalize(wish);
        float wishSpeed = maxSpeed * Math.Clamp(cmd.Move.Length(), 0f, 1f);

        // --- attrito e accelerazione
        if (p.OnFloor)
        {
            p.Vel = Friction(p.Vel, dt);
            p.Vel = Accelerate(p.Vel, wish, wishSpeed, Constants.GroundAccel, dt);
        }
        else
        {
            // controllo aereo limitato: niente bhop, niente air-strafe
            p.Vel = Accelerate(p.Vel, wish, MathF.Min(wishSpeed, Constants.MaxAirWishSpeed), Constants.AirAccel, dt);
        }

        // --- salto
        if (cmd.Jump && p.OnFloor && p.JumpCd <= 0f)
        {
            p.Vel.Y = Constants.JumpVelocity;
            p.OnFloor = false;
            p.JumpCd = Constants.JumpCooldown;
        }

        // --- gravità / aggancio al suolo
        if (p.OnFloor)
            p.Vel.Y = -2f;   // piccola spinta verso il basso: serve a RESTARE a terra
        else
            p.Vel.Y -= Constants.Gravity * dt;

        // --- movimento e collisioni, un asse alla volta
        var r = Move(p.Pos, half, p.Vel * dt, world);
        p.Pos = r.Pos;
        p.OnFloor = r.Floor;
        if (r.Floor && p.Vel.Y < 0f) p.Vel.Y = 0f;
        if (r.Ceil && p.Vel.Y > 0f) p.Vel.Y = 0f;
        if (r.WallX) p.Vel.X = 0f;
        if (r.WallZ) p.Vel.Z = 0f;

        // --- caduto fuori dal mondo
        if (p.Pos.Y < Constants.WorldFloorY)
        {
            p.Pos = new Vec3(0f, Constants.Height * 0.5f + 0.1f, 0f);
            p.Vel = Vec3.Zero;
        }
    }

    // ------------------------------------------------------------------ movimento

    private static Vec3 Friction(Vec3 vel, float dt)
    {
        float speed = MathF.Sqrt(vel.X * vel.X + vel.Z * vel.Z);
        if (speed < 0.01f) return new Vec3(0f, vel.Y, 0f);
        float drop = speed * Constants.Friction * dt;
        float newSpeed = MathF.Max(speed - drop, 0f);
        float s = newSpeed / speed;
        return new Vec3(vel.X * s, vel.Y, vel.Z * s);
    }

    private static Vec3 Accelerate(Vec3 vel, Vec3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishDir.LengthSquared() < 1e-8f || wishSpeed <= 0f) return vel;
        float current = vel.X * wishDir.X + vel.Z * wishDir.Z;
        float add = wishSpeed - current;
        if (add <= 0f) return vel;
        float accelSpeed = MathF.Min(accel * wishSpeed * dt, add);
        return new Vec3(vel.X + wishDir.X * accelSpeed, vel.Y, vel.Z + wishDir.Z * accelSpeed);
    }

    private static MoveResult Move(Vec3 pos, Vec3 half, Vec3 delta, WorldState world)
    {
        var r = new MoveResult { Pos = pos };

        r.Pos.X += delta.X;
        Resolve(ref r, half, world, 0, delta.X);

        r.Pos.Z += delta.Z;
        Resolve(ref r, half, world, 2, delta.Z);

        r.Pos.Y += delta.Y;
        Resolve(ref r, half, world, 1, delta.Y);

        return r;
    }

    /// <summary>
    /// Risolve la penetrazione su UN asse, usando la DIREZIONE del movimento.
    /// Usare il centro della scatola per decidere "pavimento o soffitto" è sbagliato:
    /// un giocatore in piedi ha il centro sopra quello del pavimento, quindi verrebbe
    /// classificato come "soffitto". La direzione non mente.
    /// </summary>
    private static void Resolve(ref MoveResult r, Vec3 half, WorldState world, int axis, float delta)
    {
        if (MathF.Abs(delta) < 1e-9f) return;

        foreach (var b in world.Boxes)
        {
            if (!Overlaps(r.Pos, half, b)) continue;

            if (axis == 0)
            {
                r.Pos.X = delta > 0f ? b.Min.X - half.X : b.Max.X + half.X;
                r.WallX = true;
            }
            else if (axis == 2)
            {
                r.Pos.Z = delta > 0f ? b.Min.Z - half.Z : b.Max.Z + half.Z;
                r.WallZ = true;
            }
            else
            {
                if (delta < 0f)
                {
                    // stava scendendo: era SOPRA la scatola, va spinto in su sul tetto
                    r.Pos.Y = b.Max.Y + half.Y;
                    r.Floor = true;
                }
                else
                {
                    // stava salendo: era SOTTO la scatola, va spinto in giù sul fondo
                    r.Pos.Y = b.Min.Y - half.Y;
                    r.Ceil = true;
                }
            }
        }
    }

    private static bool Overlaps(Vec3 pos, Vec3 half, Box b)
    {
        var min = pos - half;
        var max = pos + half;
        return !(max.X <= b.Min.X || min.X >= b.Max.X
              || max.Y <= b.Min.Y || min.Y >= b.Max.Y
              || max.Z <= b.Min.Z || min.Z >= b.Max.Z);
    }

    /// <summary>Vero se il volume è in collisione con QUALSIASI scatola del mondo.</summary>
    private static bool OverlapsWorld(Vec3 pos, Vec3 half, WorldState world)
    {
        foreach (var b in world.Boxes)
            if (Overlaps(pos, half, b)) return true;
        return false;
    }
}
