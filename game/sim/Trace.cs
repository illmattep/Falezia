using System;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>Dove è finito un colpo: punto, distanza, normale della faccia colpita e tipo di volume.</summary>
    public readonly struct HitInfo
    {
        public readonly bool Hit;
        public readonly float Distance;
        public readonly Vec3 Point;
        public readonly Vec3 Normal;
        public readonly byte Kind;

        public HitInfo(bool hit, float distance, Vec3 point, Vec3 normal, byte kind)
        {
            Hit = hit;
            Distance = distance;
            Point = point;
            Normal = normal;
            Kind = kind;
        }

        public static readonly HitInfo None = new(false, 0f, Vec3.Zero, Vec3.UnitY, 0);
    }

    /// <summary>
    /// Tracciamento di un raggio contro le scatole del mondo (le stesse che collide la
    /// simulazione: la decorazione — tetti, comignoli — non c'è, e va bene così).
    ///
    /// Serve alle tracce dei proiettili e, quando ci sarà, ai danni: è la stessa funzione
    /// per client, server e test, quindi tutti vedono lo stesso impatto.
    /// </summary>
    public static class Trace
    {
        public static HitInfo Ray(WorldState world, Vec3 origin, Vec3 dir, float maxDist = 250f)
        {
            float len = dir.Length();
            if (len < 1e-6f) return HitInfo.None;
            dir /= len;

            float best = maxDist;
            bool hit = false;
            Vec3 normal = Vec3.UnitY;
            byte kind = 0;

            // Broadphase: la griglia XZ restituisce solo le scatole nelle celle attraversate
            // dal raggio, in ordine di indice (stesso risultato del giro completo).
            var boxes = world.Boxes;
            var end = origin + dir * maxDist;
            var qmin = new Vec3(MathF.Min(origin.X, end.X), 0f, MathF.Min(origin.Z, end.Z));
            var qmax = new Vec3(MathF.Max(origin.X, end.X), 0f, MathF.Max(origin.Z, end.Z));
            var near = world.Query(qmin, qmax);
            for (int k = 0; k < near.Count; k++)
            {
                int i = near[k];
                var b = boxes[i];
                var bmin = b.Min;
                var bmax = b.Min + b.Size;

                // dentro la scatola: non è un bersaglio, è il volume in cui siamo (o il pavimento sotto i piedi)
                if (origin.X > bmin.X && origin.X < bmax.X &&
                    origin.Y > bmin.Y && origin.Y < bmax.Y &&
                    origin.Z > bmin.Z && origin.Z < bmax.Z)
                    continue;

                float tmin = 0f, tmax = best;
                Vec3 n = Vec3.Zero;
                bool ok = Slab(origin.X, dir.X, bmin.X, bmax.X, Vec3.UnitX, ref tmin, ref tmax, ref n)
                       && Slab(origin.Y, dir.Y, bmin.Y, bmax.Y, Vec3.UnitY, ref tmin, ref tmax, ref n)
                       && Slab(origin.Z, dir.Z, bmin.Z, bmax.Z, Vec3.UnitZ, ref tmin, ref tmax, ref n);

                if (!ok || tmin >= best) continue;

                best = tmin;
                hit = true;
                normal = n;
                kind = b.Kind;
            }

            if (!hit) return HitInfo.None;
            return new HitInfo(true, best, origin + dir * best, normal, kind);
        }

        /// <summary>Una fetta (slab) dell'AABB. Restituisce false se il raggio non la attraversa.</summary>
        private static bool Slab(float o, float d, float lo, float hi, Vec3 axis,
                                 ref float tmin, ref float tmax, ref Vec3 normal)
        {
            if (MathF.Abs(d) < 1e-8f)
            {
                // parallelo all'asse: o siamo dentro la fetta, o non la tocchiamo mai
                return o >= lo && o <= hi;
            }

            float inv = 1f / d;
            float t1 = (lo - o) * inv;     // piano "basso"
            float t2 = (hi - o) * inv;     // piano "alto"
            Vec3 n = -axis;                // la normale del piano d'ingresso, verso l'esterno

            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                n = axis;                  // si entra dal piano alto: la faccia guarda verso di noi
            }

            if (t1 > tmin)
            {
                tmin = t1;
                normal = n;
            }
            if (t2 < tmax) tmax = t2;

            return tmin <= tmax;
        }
    }
}
