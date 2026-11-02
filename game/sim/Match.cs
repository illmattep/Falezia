using System;
using System.Collections.Generic;

namespace Falezia.Sim
{
    /// <summary>Le fasi di un round, come in un tattico 5v5.</summary>
    public enum RoundPhase : byte
    {
        Warmup = 0,     // attesa prima del primo round
        Buy = 1,        // acquisti: si può comprare, non si spara
        Live = 2,       // round in corso
        RoundEnd = 3,   // fine round: si mostra il risultato
        MatchEnd = 4    // partita finita
    }

    /// <summary>
    /// Lo stato della PARTITA: round, punteggio, striscia di sconfitte, crediti.
    /// È dentro la simulazione, quindi lo usano server, client (per l'HUD) e test.
    /// Il server decide CHI vince il round; qui c'è solo la regia.
    /// </summary>
    public sealed partial class MatchState
    {
        public RoundPhase Phase = RoundPhase.Warmup;
        public float TimeLeft = 4f;
        public int Round = 1;               // 1-based
        public int ScoreNorth;
        public int ScoreSouth;
        public int StreakNorth;             // sconfitte consecutive (per il bonus)
        public int StreakSouth;
        public int LastWinner;              // 0 = nessuno, 1 = Nord, 2 = Sud
        public bool HalftimeSwapped;

        /// <summary>
        /// LA CASSA COMUNE (doc 04). Non è del giocatore: è della squadra, e da lì escono le armi.
        /// I premi di round entrano qui; le uccisioni vanno nel fondo personale.
        /// </summary>
        public int TeamFundsNorth = Economy.TeamStartFunds;
        public int TeamFundsSouth = Economy.TeamStartFunds;

        public int RoundNumber => Round;
        public bool Live => Phase == RoundPhase.Live;
    }

    /// <summary>
    /// REGOLE DEI ROUND ed ECONOMIA (doc 03/04, semplificate per M0):
    ///   · 13 round per vincere, 24 al massimo;
    ///   · acquisti 20 s, round 90 s, risultato 6 s;
    ///   · si vince eliminando tutta la squadra avversaria o allo scadere del tempo
    ///     (in M0 non c'è ancora la spike: il tempo premia chi difende);
    ///   · crediti: 800 di partenza, +3000 se vinci, +1900/2400/2900 se perdi
    ///     (bonus crescente con le sconfitte consecutive), +200 per uccisione.
    /// </summary>
    public static class Match
    {
        public const int BuySeconds = 20;
        /// <summary>
        /// Durata massima del round. Deve contenere tutto il ciclo: approccio, apertura (15 s),
        /// prelievo (3), trasporto (~25), avvio (5) e assedio (120). Sotto i 200 s il round
        /// finirebbe nullo mentre l'assedio è ancora in corso.
        /// </summary>
        public const int LiveSeconds = 300;
        public const int EndSeconds = 6;
        /// <summary>
        /// IL BRIEF (doc 10): la schermata prima del round 1 dove si sceglie l'operatore —
        /// cioè la specializzazione — e la si dichiara. Dura **al massimo un minuto**, e
        /// finisce prima se hanno scelto tutti (`AllDrafted`). Al fischio d'inizio la scelta
        /// si blocca per tutta la partita.
        /// </summary>
        public const int BriefSeconds = 60;

        /// <summary>Nome storico della fase di brief: `RoundPhase.Warmup` è il brief.</summary>
        public const int WarmupSeconds = BriefSeconds;
        /// <summary>Tempo minimo perché una scelta sia considerata valida: non si drafa all'ultimo tick.</summary>
        public const float DraftMinSeconds = 0f;

        public const int RoundsToWin = 13;
        public const int MaxRounds = 24;

        public const int StartCredits = 800;
        public const int MaxCredits = 9000;
        public const int WinReward = 3000;
        public const int KillReward = 200;
        public const int LossBase = 1900;
        public const int LossStep = 500;
        public const int LossMax = 2900;
        /// <summary>Round nullo (nessuna estrazione entro il tempo): introiti ridotti a entrambi (doc 04).</summary>
        public const int NullReward = 1200;

        public static void Reset(MatchState m)
        {
            m.Phase = RoundPhase.Warmup;
            m.TimeLeft = WarmupSeconds;
            m.Round = 1;
            m.ScoreNorth = m.ScoreSouth = 0;
            m.StreakNorth = m.StreakSouth = 0;
            m.LastWinner = 0;
            m.HalftimeSwapped = false;
            m.TeamFundsNorth = Economy.TeamStartFunds;
            m.TeamFundsSouth = Economy.TeamStartFunds;
            Objective.ResetObjective(m);
        }

        /// <summary>Avanza il cronometro della fase. Restituisce true se la fase è cambiata.</summary>
        public static bool Tick(MatchState m, float dt)
        {
            if (m.Phase == RoundPhase.MatchEnd) return false;

            // Durante l'assedio il timer del round è SOSPESO: l'assedio ha il suo (doc 03).
            // Un round non può morire a metà assedio.
            if (m.Phase == RoundPhase.Live && m.Core == CorePhase.Siege) return false;

            float before = m.TimeLeft;
            m.TimeLeft = MathF.Max(0f, m.TimeLeft - dt);
            if (m.TimeLeft > 0f) return false;

            switch (m.Phase)
            {
                case RoundPhase.Warmup:
                    m.Phase = RoundPhase.Buy;
                    m.TimeLeft = BuySeconds;
                    return true;

                case RoundPhase.Buy:
                    m.Phase = RoundPhase.Live;
                    m.TimeLeft = LiveSeconds;
                    return true;

                case RoundPhase.Live:
                    // Tempo scaduto senza che un'estrazione sia stata completata: ROUND NULLO.
                    // Non conviene a nessuno stallerare (doc 03), e senza questa regola chi
                    // lascia il Nucleo a terra per non esporsi verrebbe premiato.
                    EndRound(m, 0);
                    return true;

                case RoundPhase.RoundEnd:
                    NextRound(m);
                    return true;
            }
            return before != m.TimeLeft;
        }

        /// <summary>Chiude il brief adesso (tutti hanno scelto): si passa agli acquisti.</summary>
        public static void EndBrief(MatchState m)
        {
            if (m.Phase != RoundPhase.Warmup) return;
            m.Phase = RoundPhase.Buy;
            m.TimeLeft = BuySeconds;
        }

        /// <summary>Hanno scelto tutti? Serve al server per non far aspettare il minuto intero.</summary>
        public static bool AllDrafted(SimState s, IEnumerable<int> ids)
        {
            int n = 0;
            foreach (int id in ids)
            {
                var p = s.Get(id);
                if (p == null) continue;
                n++;
                if (p.Spec == Specialization.None) return false;
            }
            return n > 0;
        }

        /// <summary>
        /// BRIEF SCADUTO: chi non ha bloccato la scelta entro il minuto la prende comunque.
        /// Senza questo si entrerebbe in partita senza specializzazione, e la cassa si
        /// aprirebbe a 15 s per tutti (doc 10). Non passa da <see cref="TryDraft"/> proprio
        /// perché il draft, a quel punto, è già chiuso. Restituisce quanti ha dovuto scegliere.
        /// </summary>
        public static int AutoDraft(SimState state, IEnumerable<int> ids)
        {
            int n = 0;
            foreach (int id in ids)
            {
                var p = state.Get(id);
                if (p == null || p.Spec != Specialization.None) continue;
                p.Spec = Draft.Suggested(id);
                n++;
            }
            return n;
        }

        /// <summary>Il draft è aperto solo nel riscaldamento: dal round 1 in poi la scelta è bloccata.</summary>
        public static bool CanDraft(MatchState m) => m.Phase == RoundPhase.Warmup;

        /// <summary>
        /// Registra la scelta di un giocatore. Torna false se il draft è chiuso o la scelta non è
        /// valida — così il server rifiuta senza fidarsi del client.
        /// </summary>
        public static bool TryDraft(MatchState m, PlayerState p, Specialization spec, byte intent)
        {
            if (p == null || !CanDraft(m)) return false;
            if (!Draft.IsValid(spec)) return false;
            if (intent != 0 && intent >= Arsenal.Count) return false;

            p.Spec = spec;
            p.Intent = intent;
            return true;
        }

        /// <summary>Chiude il round. <paramref name="winner"/> 1 = Nord, 2 = Sud, 0 = pareggio.</summary>
        public static void EndRound(MatchState m, int winner)
        {
            if (m.Phase is RoundPhase.MatchEnd or RoundPhase.RoundEnd) return;

            m.LastWinner = winner;
            if (winner == 1)
            {
                m.ScoreNorth++;
                m.StreakNorth = 0;
                m.StreakSouth++;
            }
            else if (winner == 2)
            {
                m.ScoreSouth++;
                m.StreakSouth = 0;
                m.StreakNorth++;
            }

            m.Phase = RoundPhase.RoundEnd;
            m.TimeLeft = EndSeconds;

            if (m.ScoreNorth >= RoundsToWin || m.ScoreSouth >= RoundsToWin || m.Round >= MaxRounds)
            {
                m.Phase = RoundPhase.MatchEnd;
                m.TimeLeft = 0f;
            }
        }

        /// <summary>Passa al round successivo; a metà partita si cambia lato.</summary>
        public static void NextRound(MatchState m)
        {
            if (m.Phase == RoundPhase.MatchEnd) return;

            m.Round++;
            if (!m.HalftimeSwapped && m.Round > MaxRounds / 2)
            {
                m.HalftimeSwapped = true;
                (m.ScoreNorth, m.ScoreSouth) = (m.ScoreSouth, m.ScoreNorth);
                int tmp = m.StreakNorth; m.StreakNorth = m.StreakSouth; m.StreakSouth = tmp;
            }

            m.Phase = RoundPhase.Buy;
            m.TimeLeft = BuySeconds;
            m.LastWinner = 0;
            Objective.ResetObjective(m);
        }

        /// <summary>Crediti di fine round: vincitore e perdente (bonus per le sconfitte di fila).</summary>
        public static int Reward(MatchState m, int team)
        {
            if (m.LastWinner == 0) return NullReward;      // round nullo: uguale per tutti
            bool won = (team == 1 && m.LastWinner == 1) || (team == 2 && m.LastWinner == 2);
            if (won) return WinReward;

            int streak = team == 1 ? m.StreakNorth : m.StreakSouth;
            return Math.Min(LossMax, LossBase + LossStep * Math.Max(0, streak - 1));
        }

        public static void AddCredits(PlayerState p, int amount)
            => p.Credits = Math.Clamp(p.Credits + amount, 0, MaxCredits);

        public static bool MatchOver(MatchState m)
            => m.Phase == RoundPhase.MatchEnd
               || m.ScoreNorth >= RoundsToWin || m.ScoreSouth >= RoundsToWin
               || m.Round >= MaxRounds;
    }
}
