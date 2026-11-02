using System;
using System.Collections.Generic;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>Dove è arrivato un colpo su un bersaglio.</summary>
    public enum HitZone : byte
    {
        None = 0,
        Head = 1,
        Torso = 2,
        Legs = 3
    }

    /// <summary>
    /// Esito di una singola palla. Il danno è già calcolato con la distanza vera
    /// (il falloff dell'arma) e con la zona colpita: chi disegna non deve rifare i conti.
    /// </summary>
    public readonly struct ShotHit
    {
        public readonly bool Hit;
        public readonly int Target;      // -1 = muro, -2 = niente
        public readonly HitZone Zone;
        public readonly int Damage;
        public readonly Vec3 Point;
        public readonly float Distance;
        public readonly bool Killed;

        /// <summary>Normale della superficie colpita: serve ai fori sui muri e alle scintille.</summary>
        public readonly Vec3 Normal;

        /// <summary>Materiale del volume colpito (indice di <see cref="WorldState.Kinds"/>).</summary>
        public readonly byte Surface;

        public ShotHit(bool hit, int target, HitZone zone, int damage, Vec3 point, float distance, bool killed,
                       Vec3 normal = default, byte surface = 0)
        {
            Hit = hit;
            Target = target;
            Zone = zone;
            Damage = damage;
            Point = point;
            Distance = distance;
            Killed = killed;
            Normal = normal.LengthSquared() > 1e-8f ? Vec3.Normalize(normal) : Vec3.UnitY;
            Surface = surface;
        }

        public static readonly ShotHit None = new(false, -2, HitZone.None, 0, Vec3.Zero, 0f, false);
    }

    /// <summary>
    /// Un bersaglio: due scatole (testa e corpo), vita, morte e rinascita.
    /// Vive nella simulazione, non nel motore: si può colpire e uccidere anche nei test,
    /// senza Godot, con la stessa funzione che usa il gioco.
    /// </summary>
    public sealed class TargetState
    {
        public const float MaxHealth = 100f;

        // misure di un uomo in piedi (metri). Le stesse della capsula che si vede.
        public const float Height = 1.80f;
        public const float BodyTop = 1.52f;   // sopra: testa
        public const float HalfWidth = 0.24f;
        public const float HalfDepth = 0.16f;
        public const float HeadHalf = 0.125f;
        public const float RespawnSeconds = 4.0f;

        public int Id;
        public Vec3 Feet;
        public float Yaw;
        public float Health = MaxHealth;
        public float DeadFor;

        public bool Alive => Health > 0f;

        public Vec3 Center => Feet + new Vec3(0f, Height * 0.5f, 0f);

        /// <summary>Scatola della testa: cubo attorno alla sommità.</summary>
        public void HeadBox(out Vec3 min, out Vec3 size)
        {
            min = Feet + new Vec3(-HeadHalf, Height - HeadHalf * 2f, -HeadHalf);
            size = new Vec3(HeadHalf * 2f, HeadHalf * 2f, HeadHalf * 2f);
        }

        /// <summary>Scatola del corpo: dalle gambe alle spalle.</summary>
        public void BodyBox(out Vec3 min, out Vec3 size)
        {
            min = Feet + new Vec3(-HalfWidth, 0f, -HalfDepth);
            size = new Vec3(HalfWidth * 2f, BodyTop, HalfDepth * 2f);
        }

        /// <summary>Applica il danno. Torna true se questo colpo lo ha ucciso.</summary>
        public bool Apply(int damage)
        {
            if (!Alive || damage <= 0) return false;
            Health -= damage;
            if (Health > 0f) return false;
            Health = 0f;
            DeadFor = 0f;
            return true;
        }

        /// <summary>Rimette in piedi il bersaglio. Torna true se è stato resuscitato adesso.</summary>
        public bool TryRespawn(float dt)
        {
            if (Alive) return false;
            DeadFor += dt;
            if (DeadFor < RespawnSeconds) return false;
            Health = MaxHealth;
            DeadFor = 0f;
            return true;
        }

        public void Reset(Vec3 feet, float yaw = 0f)
        {
            Feet = feet;
            Yaw = yaw;
            Health = MaxHealth;
            DeadFor = 0f;
        }
    }

    /// <summary>
    /// Risoluzione dei colpi: bersagli e muri, zona colpita, danno con la distanza.
    /// È l'unico posto in cui si decide "chi ha preso cosa": la usano il client, il server
    /// (quando ci sarà) e i test, quindi tutti vedono lo stesso risultato dallo stesso seme.
    /// </summary>
    public static class Combat
    {
        public const float HeadshotMultiplier = 2.2f;
        public const float LegsMultiplier = 0.75f;
        public const float MeleeMultiplier = 2.0f;

        /// <summary>Distanza di ingresso del raggio in una scatola (slab method). -1 = non entra.</summary>
        public static float RayBox(Vec3 origin, Vec3 dir, Vec3 min, Vec3 size, float maxDist)
        {
            float tmin = 0f, tmax = maxDist;
            var bmax = min + size;

            for (int a = 0; a < 3; a++)
            {
                float o = a == 0 ? origin.X : a == 1 ? origin.Y : origin.Z;
                float d = a == 0 ? dir.X : a == 1 ? dir.Y : dir.Z;
                float lo = a == 0 ? min.X : a == 1 ? min.Y : min.Z;
                float hi = a == 0 ? bmax.X : a == 1 ? bmax.Y : bmax.Z;

                if (MathF.Abs(d) < 1e-7f)
                {
                    if (o < lo || o > hi) return -1f;
                    continue;
                }

                float inv = 1f / d;
                float t1 = (lo - o) * inv;
                float t2 = (hi - o) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);

                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) return -1f;
            }
            return tmin;
        }

        /// <summary>
        /// Un colpo: si guarda prima il muro (blocca tutto) e poi i bersagli.
        /// Il muro più vicino del bersaglio lo copre: non si spara attraverso i muri.
        /// </summary>
        public static ShotHit Shoot(WorldState world, IReadOnlyList<TargetState> targets,
                                    Vec3 origin, Vec3 dir, WeaponDef def, float maxDist = 250f)
        {
            float len = dir.Length();
            if (len < 1e-6f) return ShotHit.None;
            dir /= len;

            var wallHit = Trace.Ray(world, origin, dir, maxDist);
            float wall = wallHit.Hit ? wallHit.Distance : maxDist;

            TargetState bestTarget = null;
            HitZone bestZone = HitZone.None;
            float bestDist = wall;
            Vec3 bestPoint = Vec3.Zero;

            for (int i = 0; targets != null && i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.Alive) continue;

                t.HeadBox(out var hMin, out var hSize);
                float th = RayBox(origin, dir, hMin, hSize, maxDist);
                if (th >= 0f && th < bestDist)
                {
                    bestDist = th;
                    bestTarget = t;
                    bestZone = HitZone.Head;
                }

                t.BodyBox(out var bMin, out var bSize);
                float tb = RayBox(origin, dir, bMin, bSize, maxDist);
                if (tb >= 0f && tb < bestDist)
                {
                    bestDist = tb;
                    bestTarget = t;
                    bestZone = HitZone.Torso;
                }
            }

            if (bestTarget == null)
                return wallHit.Hit
                    ? new ShotHit(true, -1, HitZone.None, 0, wallHit.Point, wallHit.Distance, false,
                                  wallHit.Normal, wallHit.Kind)
                    : ShotHit.None;

            bestPoint = origin + dir * bestDist;

            // zona fine: sotto il bacino sono gambe
            if (bestZone == HitZone.Torso && bestPoint.Y - bestTarget.Feet.Y < TargetState.Height * 0.42f)
                bestZone = HitZone.Legs;

            int dmg = Arsenal.DamageAt(def, bestDist);
            if (def.Melee) dmg = def.Damage;
            if (bestZone == HitZone.Head) dmg = (int)MathF.Round(dmg * HeadshotMultiplier);
            else if (bestZone == HitZone.Legs) dmg = (int)MathF.Round(dmg * LegsMultiplier);
            if (def.Melee) dmg = (int)MathF.Round(dmg * MeleeMultiplier);
            dmg = Math.Max(1, dmg);

            bool killed = bestTarget.Apply(dmg);
            return new ShotHit(true, bestTarget.Id, bestZone, dmg, bestPoint, bestDist, killed);
        }

        /// <summary>Le due scatole di un GIOCATORE: corpo e testa (stesse misure dei bersagli).</summary>
        public static void PlayerBoxes(PlayerState p, out Vec3 bodyMin, out Vec3 bodySize,
                                       out Vec3 headMin, out Vec3 headSize)
        {
            float h = p.Crouching ? Constants.CrouchHeight : Constants.Height;
            float feet = p.Pos.Y - h * 0.5f;
            const float half = 0.24f;
            const float headR = 0.125f;

            bodyMin = new Vec3(p.Pos.X - half, feet, p.Pos.Z - half);
            bodySize = new Vec3(half * 2f, h - headR * 2f, half * 2f);

            headMin = new Vec3(p.Pos.X - headR, feet + h - headR * 2f, p.Pos.Z - headR);
            headSize = new Vec3(headR * 2f, headR * 2f, headR * 2f);
        }

        /// <summary>
        /// Un colpo contro i GIOCATORI (server autoritativo). Il muro blocca come per i
        /// bersagli; testa ×2,2, gambe ×0,75, danno della scheda alla distanza vera.
        /// Il colpo che uccide toglie il giocatore dal round: chi resta in piedi decide il round.
        /// </summary>
        public static ShotHit ShootPlayers(WorldState world, SimState state, int shooterId,
                                           Vec3 origin, Vec3 dir, WeaponDef def, float maxDist = 250f)
        {
            float len = dir.Length();
            if (len < 1e-6f) return ShotHit.None;
            dir /= len;

            var wallHit = Trace.Ray(world, origin, dir, maxDist);
            float wall = wallHit.Hit ? wallHit.Distance : maxDist;

            PlayerState best = null;
            HitZone zone = HitZone.None;
            float bestDist = wall;
            Vec3 bestPoint = Vec3.Zero;

            foreach (var p in state.Players.Values)
            {
                if (p == null || !p.Alive || p.Id == shooterId) continue;

                PlayerBoxes(p, out var bMin, out var bSize, out var hMin, out var hSize);

                float th = RayBox(origin, dir, hMin, hSize, maxDist);
                if (th >= 0f && th < bestDist)
                {
                    best = p; zone = HitZone.Head; bestDist = th;
                }

                float tb = RayBox(origin, dir, bMin, bSize, maxDist);
                if (tb >= 0f && tb < bestDist)
                {
                    best = p; zone = HitZone.Torso; bestDist = tb;
                }
            }

            if (best == null)
                return wallHit.Hit
                    ? new ShotHit(true, -1, HitZone.None, 0, wallHit.Point, wallHit.Distance, false,
                                  wallHit.Normal, wallHit.Kind)
                    : ShotHit.None;

            bestPoint = origin + dir * bestDist;

            float feetY = best.Pos.Y - (best.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
            if (zone == HitZone.Torso && bestPoint.Y - feetY < Constants.Height * 0.42f)
                zone = HitZone.Legs;

            int dmg = DamageFor(def, bestDist, zone);
            if (def.Melee) dmg = Math.Max(1, (int)MathF.Round(def.Damage * MeleeMultiplier));

            best.Health -= dmg;
            bool killed = best.Health <= 0f;
            if (killed)
            {
                best.Health = 0f;
                best.Alive = false;
                best.Deaths++;
            }

            return new ShotHit(true, best.Id, zone, dmg, bestPoint, bestDist, killed, -dir, 0);
        }

        /// <summary>Un colpo di coltello: raggio corto, danno pieno, niente falloff.</summary>
        public static ShotHit Melee(WorldState world, IReadOnlyList<TargetState> targets,
                                    Vec3 origin, Vec3 dir, WeaponDef def, float reach = 1.75f)
            => Shoot(world, targets, origin, dir, def, reach);

        /// <summary>Quanti danni fa un colpo a una certa distanza e zona (per l'HUD e i test).</summary>
        public static int DamageFor(WeaponDef def, float meters, HitZone zone)
        {
            int d = def.Melee ? def.Damage : Arsenal.DamageAt(def, meters);
            if (zone == HitZone.Head) d = (int)MathF.Round(d * HeadshotMultiplier);
            else if (zone == HitZone.Legs) d = (int)MathF.Round(d * LegsMultiplier);
            if (def.Melee) d = (int)MathF.Round(d * MeleeMultiplier);
            return Math.Max(1, d);
        }
    }
}
