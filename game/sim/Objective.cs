using System;
using System.Collections.Generic;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>Dov'è il Nucleo, adesso. È lo stato più importante della partita (doc 03).</summary>
    public enum CorePhase : byte
    {
        Closed = 0,   // la cassa è chiusa: si apre tenendo E dentro il cerchio
        Opened = 1,   // cassa aperta, il Nucleo è dentro e si preleva
        Carried = 2,  // qualcuno lo porta
        Dropped = 3,  // a terra: chi lo vuole deve andare a prenderlo
        Siege = 4     // piantato: faro e sirene, parte l'assedio
    }

    /// <summary>Cosa è appena successo, per i log del server e per i suoni del client.</summary>
    public enum CoreEvent : byte
    {
        CassaAperta,
        NucleoPrelevato,
        NucleoLasciato,
        AvvioEstrazione,
        Estratto,
        NucleoRimosso
    }

    /// <summary>
    /// Lo stato dell'obiettivo vive dentro <see cref="MatchState"/>, perché viaggia nello snapshot:
    /// il client deve sapere dov'è il Nucleo, chi lo porta e quanto resta all'assedio.
    /// </summary>
    public sealed partial class MatchState
    {
        public CorePhase Core = CorePhase.Closed;
        /// <summary>Chi porta il Nucleo, -1 se nessuno.</summary>
        public int CoreOwner = -1;
        /// <summary>La squadra che lo detiene (1 Nord, 2 Sud, 0 nessuno).</summary>
        public int CoreTeam;
        /// <summary>0 = nessuna, 1 = Grotta del Levante (Est), 2 = Galleria Vecchia (Ovest).</summary>
        public int ExtractZone;
        /// <summary>0..1, apertura della cassa.</summary>
        public float OpenProgress;
        /// <summary>Secondi accumulati nel gesto in corso (prelievo, avvio, rimozione).</summary>
        public float Channel;
        public float SiegeLeft;
        /// <summary>Posizione del Nucleo quando è a terra (o sulla zona d'assedio).</summary>
        public Vec3 CorePos;
    }

    /// <summary>
    /// IL CICLO DEL ROUND (doc 03). Sta nella simulazione, quindi lo usano identico il server,
    /// i test e — per previsione locale — il client.
    ///
    /// Aprire → prelevare → trasportare → avviare → resistere 120 s. Chi lo porta **cammina**:
    /// niente sprint, e il prelievo è pubblico: appena qualcuno prende il Nucleo, lo sanno tutti.
    /// Il Nucleo non torna mai alla cassa: se cade, resta dove cade.
    /// </summary>
    public static class Objective
    {
        // --- tempi (doc 03)
        public const float OpenSeconds = 15f;
        public const float OpenSecondsDecifratore = 7.5f;
        public const float PickupSeconds = 3f;
        public const float PlantSeconds = 5f;
        public const float RemoveSeconds = 5f;
        public const float SiegeSeconds = 120f;

        // --- spazi
        public const float CassaRadius = 3f;
        public const float ZoneRadius = 4.5f;
        public const float CoreRadius = 2.5f;
        public const float VerticalTolerance = 2.5f;

        public const int ZoneEast = 1;   // Grotta del Levante
        public const int ZoneWest = 2;   // Galleria Vecchia

        public static void ResetObjective(MatchState m)
        {
            m.Core = CorePhase.Closed;
            m.CoreOwner = -1;
            m.CoreTeam = 0;
            m.ExtractZone = 0;
            m.OpenProgress = 0f;
            m.Channel = 0f;
            m.SiegeLeft = 0f;
            m.CorePos = Vec3.Zero;
        }

        /// <summary>Il portatore cammina: niente sprint finché ha il Nucleo in mano.</summary>
        public static bool IsCarrier(MatchState m, int playerId) => m.Core == CorePhase.Carried && m.CoreOwner == playerId;

        /// <summary>
        /// Un tick dell'obiettivo. Restituisce gli eventi accaduti: il server li scrive nei log,
        /// il client li suona. Chiamare solo a round vivo.
        /// </summary>
        public static List<CoreEvent> Step(SimState s, MatchState m, WorldState w, float dt)
        {
            var events = new List<CoreEvent>(2);
            if (m.Phase != RoundPhase.Live) return events;

            var cassa = FindPoint(w, "cassa");
            var east = FindPoint(w, "grotta");
            var west = FindPoint(w, "galleria");

            switch (m.Core)
            {
                // ---------------------------------------------------------- apertura
                case CorePhase.Closed:
                {
                    if (!cassa.HasValue) return events;
                    var opener = FindChanneler(s, cassa.Value, CassaRadius);
                    if (opener != null)
                    {
                        float full = opener.Spec == Specialization.Decifratore
                            ? OpenSecondsDecifratore
                            : OpenSeconds;
                        m.OpenProgress = MathF.Min(1f, m.OpenProgress + dt / full);
                        if (m.OpenProgress >= 1f)
                        {
                            m.Core = CorePhase.Opened;
                            events.Add(CoreEvent.CassaAperta);
                        }
                    }
                    else
                    {
                        // mollare il gesto fa ripartire da capo: la cassa non si apre a metà
                        m.OpenProgress = MathF.Max(0f, m.OpenProgress - dt / OpenSeconds);
                    }
                    break;
                }

                // ---------------------------------------------------------- prelievo
                case CorePhase.Opened:
                case CorePhase.Dropped:
                {
                    Vec3 where = m.Core == CorePhase.Dropped ? m.CorePos : cassa ?? Vec3.Zero;
                    float radius = m.Core == CorePhase.Dropped ? CoreRadius : CassaRadius;

                    var taker = FindChanneler(s, where, radius);
                    if (taker == null) { m.Channel = 0f; break; }

                    m.Channel += dt;
                    if (m.Channel >= PickupSeconds)
                    {
                        m.Channel = 0f;
                        m.Core = CorePhase.Carried;
                        m.CoreOwner = taker.Id;
                        m.CoreTeam = taker.Team;
                        m.CorePos = taker.Pos;
                        events.Add(CoreEvent.NucleoPrelevato);
                    }
                    break;
                }

                // ---------------------------------------------------------- trasporto
                case CorePhase.Carried:
                {
                    var carrier = s.Get(m.CoreOwner);
                    if (carrier == null || !carrier.Alive)
                    {
                        if (carrier != null) m.CorePos = DropPoint(carrier);
                        m.Core = CorePhase.Dropped;
                        m.CoreOwner = -1;
                        m.CoreTeam = 0;
                        m.ExtractZone = 0;
                        m.Channel = 0f;
                        events.Add(CoreEvent.NucleoLasciato);
                        break;
                    }

                    m.CorePos = carrier.Pos;

                    int zone = ZoneAt(carrier, east, west);
                    if (zone != 0 && carrier.Interacting)
                    {
                        m.ExtractZone = zone;
                        m.Channel += dt;
                        if (m.Channel >= PlantSeconds)
                        {
                            m.Channel = 0f;
                            m.Core = CorePhase.Siege;
                            m.SiegeLeft = SiegeSeconds;
                            m.CorePos = zone == ZoneEast ? east.Value : west.Value;
                            events.Add(CoreEvent.AvvioEstrazione);
                        }
                    }
                    else
                    {
                        m.Channel = 0f;
                        if (zone == 0) m.ExtractZone = 0;
                    }
                    break;
                }

                // ----------------------------------------------------------- assedio
                case CorePhase.Siege:
                {
                    // l'assedio ha il suo timer: quello del round è sospeso (Match.Tick)
                    m.SiegeLeft = MathF.Max(0f, m.SiegeLeft - dt);

                    var remover = FindChanneler(s, m.CorePos, CoreRadius, excludeTeam: m.CoreTeam);
                    if (remover != null)
                    {
                        m.Channel += dt;
                        if (m.Channel >= RemoveSeconds)
                        {
                            m.Channel = 0f;
                            events.Add(CoreEvent.NucleoRimosso);
                            Match.EndRound(m, remover.Team);   // RULES C6: chiude subito il round
                            return events;
                        }
                    }
                    else
                    {
                        m.Channel = 0f;
                    }

                    if (m.SiegeLeft <= 0f)
                    {
                        events.Add(CoreEvent.Estratto);
                        Match.EndRound(m, m.CoreTeam);
                    }
                    break;
                }
            }

            return events;
        }

        // ------------------------------------------------------------------ util

        /// <summary>
        /// Chi sta tenendo premuto il gesto dentro il cerchio. Vince il più vicino, così il
        /// risultato non dipende dall'ordine del dizionario (la simulazione è deterministica).
        /// </summary>
        private static PlayerState FindChanneler(SimState s, Vec3 point, float radius, int excludeTeam = 0)
        {
            PlayerState best = null;
            float bestDistance = float.MaxValue;

            foreach (var p in s.Players.Values)
            {
                if (!p.Alive || !p.Interacting) continue;
                if (excludeTeam != 0 && p.Team == excludeTeam) continue;
                if (MathF.Abs(p.Pos.Y - point.Y) > VerticalTolerance) continue;

                float d = Horizontal(p.Pos, point);
                if (d > radius || d >= bestDistance) continue;
                bestDistance = d;
                best = p;
            }
            return best;
        }

        /// <summary>La zona di estrazione in cui si trova il giocatore, o 0.</summary>
        public static int ZoneAt(PlayerState p, Vec3? east, Vec3? west)
        {
            if (east.HasValue && Near(p.Pos, east.Value, ZoneRadius)) return ZoneEast;
            if (west.HasValue && Near(p.Pos, west.Value, ZoneRadius)) return ZoneWest;
            return 0;
        }

        public static bool Near(Vec3 a, Vec3 b, float radius)
            => MathF.Abs(a.Y - b.Y) <= VerticalTolerance && Horizontal(a, b) <= radius;

        private static float Horizontal(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Il Nucleo cade ai piedi di chi lo portava, non a mezz'aria.</summary>
        private static Vec3 DropPoint(PlayerState p)
            => new(p.Pos.X, p.Pos.Y - Constants.Height * 0.5f + 0.1f, p.Pos.Z);

        /// <summary>Il punto della mappa il cui nome contiene questa parola, senza badare al caso.</summary>
        public static Vec3? FindPoint(WorldState w, string contains)
        {
            foreach (var p in w.Points)
                if (p.Name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return p.Pos;
            return null;
        }
    }
}
