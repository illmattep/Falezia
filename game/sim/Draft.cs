using System;

namespace Falezia.Sim
{
    /// <summary>
    /// Le cinque specializzazioni del doc 10. Nessuna è un eroe: sono **una riga di numeri**,
    /// tutte pescano dallo stesso arsenale, e tutte danno **tempo o informazione**, mai potenza
    /// (RULES A4). Si scelgono prima del round 1, allo scoperto, e non si cambiano più.
    /// </summary>
    public enum Specialization : byte
    {
        None = 0,        // non ha ancora scelto
        Decifratore = 1, // apre la cassa in 7,5 s (5 s col Kit)
        Rianimatore = 2, // rianima in 10 s (5 s col Medikit)
        Portatore = 3,   // Nucleo a 4,8 m/s, senza stamina
        Ingegnere = 4,   // robot piazzato in 1,5 s, apre in 10 s, riparabile
        Ricognitore = 5  // sensori +40% raggio, ping 4 s, endoscopio silenzioso
    }

    /// <summary>
    /// IL DRAFT. La scelta è **pubblica**: chi guarda la partita vede chi ha preso cosa, e due
    /// Decifratori dicono all'avversario che si corre alla cassa. È informazione, non identità.
    ///
    /// Vive dentro la simulazione perché la validano tutti allo stesso modo: server, client e test.
    /// Le regole di *quando* si può scegliere stanno in <see cref="Match"/>.
    /// </summary>
    public static class Draft
    {
        public const int Count = 5;

        public static bool IsValid(Specialization s)
            => s >= Specialization.Decifratore && s <= Specialization.Ricognitore;

        public static string Name(Specialization s) => s switch
        {
            Specialization.Decifratore => "DECIFRATORE",
            Specialization.Rianimatore => "RIANIMATORE",
            Specialization.Portatore => "PORTATORE",
            Specialization.Ingegnere => "INGEGNERE",
            Specialization.Ricognitore => "RICOGNITORE",
            _ => "—"
        };

        /// <summary>Cosa dà, in una riga: la stessa frase nel pannello e nel client.</summary>
        public static string Effect(Specialization s) => s switch
        {
            Specialization.Decifratore => "Cassa in 7,5 s · 5 s col Kit",
            Specialization.Rianimatore => "Rianima in 10 s · 5 s col Medikit",
            Specialization.Portatore => "Nucleo a 4,8 m/s · niente stamina",
            Specialization.Ingegnere => "Robot in 1,5 s · apre in 10 s",
            Specialization.Ricognitore => "Sensori +40% · ping 4 s",
            _ => ""
        };

        public static Specialization FromByte(byte v)
            => IsValid((Specialization)v) ? (Specialization)v : Specialization.None;

        /// <summary>
        /// La scelta che tocca a chi non blocca in tempo. È deterministica (dipende dal posto
        /// in coda, non dal caso) così due esecuzioni della stessa partita danno lo stesso
        /// risultato: la simulazione resta riproducibile.
        /// </summary>
        public static Specialization Suggested(int playerId)
        {
            int i = (playerId - 1) % Count;
            if (i < 0) i += Count;
            return (Specialization)(i + 1);
        }
    }
}
