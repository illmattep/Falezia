using System;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>Da dove escono i soldi di un acquisto.</summary>
    public enum Fund
    {
        Personal = 0,   // fondo personale: si rinnova solo con le uccisioni
        Team = 1        // cassa comune: entrano i premi di round, è di tutta la squadra
    }

    /// <summary>Come è andato un acquisto. Il client mostra il motivo, non un bool.</summary>
    public enum ShopResult
    {
        Bought,        // arma nuova in mano
        Refilled,      // la stessa arma ricomprata: munizioni piene
        Equipped,      // gratis (Standard): non c'era, ora c'è
        NotEnough,     // né la cassa né il fondo personale bastano
        NotBuyPhase,   // fuori dalla fase di acquisto
        NotForSale,    // il coltello, che non si compra
        Dead           // a terra non si compra
    }

    /// <summary>Cosa è successo davvero: serve all'HUD per dire "venduta" o "lasciata a terra".</summary>
    public struct BuyOutcome
    {
        public ShopResult Result;
        public WeaponSlot Slot;
        public int Price;           // quanto è stato pagato
        public int Refund;          // quanto è tornato indietro vendendo l'arma sostituita
        public WeaponSlot? Replaced;
        public bool OldDropped;     // la sostituita non era di questa fase: è finita a terra
        public bool FromTeam;       // pagata dalla cassa comune

        public bool Ok => Result is ShopResult.Bought or ShopResult.Refilled or ShopResult.Equipped;

        /// <summary>La riga che il client scrive in fondo allo schermo.</summary>
        public string Text
        {
            get
            {
                var def = Arsenal.Get(Slot);
                switch (Result)
                {
                    case ShopResult.NotEnough: return $"non bastano i soldi per {def.Name} (${Price})";
                    case ShopResult.NotBuyPhase: return "si compra solo nella fase di acquisto";
                    case ShopResult.NotForSale: return "il coltello è sempre con te";
                    case ShopResult.Dead: return "sei a terra";
                }

                string s = Result == ShopResult.Refilled
                    ? $"{def.Name} ricomprata · munizioni piene"
                    : $"{def.Name} comprata";
                if (Price > 0) s += $" · −${Price}" + (FromTeam ? " dalla cassa" : "");

                if (Replaced.HasValue)
                {
                    var old = Arsenal.Get(Replaced.Value);
                    if (Refund > 0) s += $" · {old.Name} venduta (+${Refund})";
                    else if (OldDropped) s += $" · {old.Name} lasciata a terra";
                }
                return s;
            }
        }
    }

    /// <summary>Un'arma a terra, con le SUE munizioni. È un'istanza, non uno slot.</summary>
    public sealed class GroundWeapon
    {
        public int Id;              // stabile finché resta a terra: il client lo usa per il suono
        public WeaponSlot Slot;
        public int Ammo;
        public int Reserve;
        public bool BoughtThisBuy;  // se raccolta, si può ancora vendere in questa fase
        public int BoughtRound;
        public bool PaidFromTeam;
        public FireMode Mode;

        public Vec3 Pos;
        public Vec3 Vel;
        public float Spin;          // rotazione a terra, solo presentazione
        public bool Settled;
        public int LandTick = -1;   // tick in cui si è posata: -1 = ancora in volo
        public int OwnerId;         // chi l'ha lasciata (log e punteggio, non regole)

        public GroundWeapon Copy() => (GroundWeapon)MemberwiseClone();
    }

    /// <summary>
    /// IL NEGOZIO E LE REGOLE DELLE ARMI (stile Valorant, con la cassa di squadra di Falezia).
    ///
    /// Regole, in ordine di come si incontrano:
    ///   · si compra solo nella fase di acquisto, e solo da vivi;
    ///   · **ricomprare la stessa arma** non è un doppione: ti ridà caricatore e riserva pieni,
    ///     e si paga (è il modo per tornare in partita senza cambiare arma);
    ///   · una primaria e una secondaria sole: la nuova arma prende il posto della vecchia;
    ///   · la vecchia, **se l'hai comprata in QUESTA fase di acquisto, si VENDE** (rimborso intero,
    ///     a chi l'aveva pagata: cassa o fondo personale);
    ///   · se invece l'hai portata da un round precedente, **non si vende: si lascia a terra**,
    ///     dove chiunque può raccoglierla (o tu stesso, più tardi);
    ///   · **i soldi**: sotto i $300 si paga di tasca propria, sopra si paga con la **cassa comune**;
    ///     se la cassa non basta si attinge al fondo personale, e viceversa. L'economia è
    ///     parzialmente condivisa: i premi di round vanno in cassa, le uccisioni in tasca.
    ///
    /// Tutto qui dentro è puro: niente motore, niente rete. Lo usano server, client offline e test.
    /// </summary>
    public static class Shop
    {
        /// <summary>La primaria in cache, se ce n'è una. Il coltello non occupa mai la cache.</summary>
        public static WeaponSlot? HeldPrimary(PlayerState p) => Held(p, primary: true);

        /// <summary>La secondaria in cache, se ce n'è una.</summary>
        public static WeaponSlot? HeldSecondary(PlayerState p) => Held(p, primary: false);

        /// <summary>
        /// L'arma di quella classe che il giocatore "ha": quella in mano se è della classe,
        /// altrimenti la più preziosa in cache. La Standard gratis può restare in cache accanto
        /// a una pistola comprata: conta la comprata, non la Standard.
        /// </summary>
        private static WeaponSlot? Held(PlayerState p, bool primary)
        {
            if (!Arsenal.Get(p.Weapon.Slot).Melee
                && Arsenal.Get(p.Weapon.Slot).IsPrimary == primary
                && Has(p, p.Weapon.Slot))
                return p.Weapon.Slot;

            WeaponSlot? best = null;
            int bestPrice = -1;
            for (int i = 0; i < p.Cache.Length; i++)
            {
                if (p.Cache[i] == null) continue;
                var d = Arsenal.Get((WeaponSlot)i);
                if (d.Melee || d.IsPrimary != primary) continue;
                if (d.Price > bestPrice) { bestPrice = d.Price; best = (WeaponSlot)i; }
            }
            return best;
        }

        /// <summary>L'arma che il giocatore ha davvero in mano (per l'HUD e per il drop).</summary>
        public static bool Has(PlayerState p, WeaponSlot slot)
            => slot >= 0 && (int)slot < p.Cache.Length && p.Cache[(int)slot] != null;

        // ------------------------------------------------------------------ soldi

        /// <summary>Quanti soldi può spendere adesso, contando cassa e fondo personale.</summary>
        public static int Available(SimState s, MatchState m, PlayerState p)
        {
            if (s.UnlimitedMoney) return int.MaxValue;
            return Economy.TeamFunds(m, p.Team) + p.Credits;
        }

        public static bool CanAfford(SimState s, MatchState m, PlayerState p, int price)
            => Available(s, m, p) >= price;

        /// <summary>
        /// Pagamento: la cassa paga le armi (> $300), il fondo personale le spese piccole.
        /// Se il fondo scelto non basta si prova l'altro: nessuno resta bloccato per un resto.
        /// </summary>
        private static bool TryPay(SimState s, MatchState m, PlayerState p, int price, out bool fromTeam)
        {
            fromTeam = false;
            if (price <= 0) return true;
            if (s.UnlimitedMoney)
            {
                fromTeam = price > Economy.FreeThreshold;
                return true;
            }

            bool preferTeam = price > Economy.FreeThreshold;
            int team = Economy.TeamFunds(m, p.Team);

            if (preferTeam && team >= price)
            {
                Economy.SpendTeam(m, p.Team, price);
                fromTeam = true;
                return true;
            }
            if (p.Credits >= price)
            {
                p.Credits -= price;
                return true;
            }
            if (team >= price)
            {
                Economy.SpendTeam(m, p.Team, price);
                fromTeam = true;
                return true;
            }
            return false;
        }

        private static void Refund(SimState s, MatchState m, PlayerState p, bool toTeam, int amount)
        {
            if (s.UnlimitedMoney || amount <= 0) return;
            if (toTeam) Economy.AddTeamFunds(m, p.Team, amount);
            else p.Credits = Math.Clamp(p.Credits + amount, 0, Match.MaxCredits);
        }

        // ------------------------------------------------------------------ acquisto

        /// <summary>Compra (o ricompra) un'arma. È l'unica porta: server, client offline e test.</summary>
        public static BuyOutcome Buy(SimState s, MatchState m, WorldState w, int id, WeaponSlot slot)
        {
            var outcome = new BuyOutcome { Slot = slot };
            var p = s.Get(id);
            var def = Arsenal.Get(slot);

            if (p == null || !p.Alive) { outcome.Result = ShopResult.Dead; return outcome; }
            if (def.Melee) { outcome.Result = ShopResult.NotForSale; return outcome; }
            if (!s.UnlimitedMoney && m.Phase != RoundPhase.Buy)
            {
                outcome.Result = ShopResult.NotBuyPhase;
                outcome.Price = def.Price;
                return outcome;
            }

            // la STESSA arma che hai già: munizioni piene, si paga (o è gratis, come la Standard)
            if (Has(p, slot))
            {
                outcome.Price = Math.Max(0, def.Price);
                if (!TryPay(s, m, p, outcome.Price, out bool paidFromTeam))
                {
                    outcome.Result = ShopResult.NotEnough;
                    return outcome;
                }
                Simulation.Give(s, id, slot);            // istanza nuova: caricatore e riserva pieni
                Mark(p, slot, m, paidFromTeam);
                Simulation.Equip(s, id, slot);
                outcome.FromTeam = paidFromTeam;
                outcome.Result = def.Price > 0 ? ShopResult.Refilled : ShopResult.Equipped;
                return outcome;
            }

            // l'arma che occupa lo slot che sto per prendere
            var occupantSlot = def.IsPrimary ? HeldPrimary(p) : HeldSecondary(p);
            outcome.Price = Math.Max(0, def.Price);

            // PRIMA i soldi, poi l'arsenale: vendere e poi non poter pagare lascerebbe il
            // giocatore con un'arma in meno e i soldi nel fondo sbagliato.
            int refundable = 0;
            if (occupantSlot.HasValue && occupantSlot.Value != slot && p.Cache[(int)occupantSlot.Value] is { } tmp
                && tmp.BoughtThisBuy && Arsenal.Get(occupantSlot.Value).Price > 0)
                refundable = Arsenal.Get(occupantSlot.Value).Price;

            if (!s.UnlimitedMoney && Available(s, m, p) + refundable < outcome.Price)
            {
                outcome.Result = ShopResult.NotEnough;
                return outcome;
            }

            if (occupantSlot.HasValue && occupantSlot.Value != slot)
            {
                var occ = p.Cache[(int)occupantSlot.Value];
                outcome.Replaced = occupantSlot.Value;

                if (occ != null && Arsenal.Get(occupantSlot.Value).Price <= 0)
                {
                    // la pistola gratis: sparisce e basta, nessuno la vuole a terra
                    p.Cache[(int)occupantSlot.Value] = null;
                }
                else if (occ != null && occ.BoughtThisBuy)
                {
                    // comprata in questa fase: si VENDE, rimborso intero a chi l'aveva pagata
                    outcome.Refund = Arsenal.Get(occupantSlot.Value).Price;
                    Refund(s, m, p, occ.PaidFromTeam, outcome.Refund);
                    p.Cache[(int)occupantSlot.Value] = null;
                }
                else
                {
                    // portata dal round prima: non si vende, si LASCIA A TERRA
                    Simulation.DropFromCache(s, w, p, occupantSlot.Value, throwForward: true);
                    outcome.OldDropped = true;
                }
            }

            if (!TryPay(s, m, p, outcome.Price, out bool fromTeam))
            {
                outcome.Result = ShopResult.NotEnough;
                return outcome;
            }

            Simulation.Give(s, id, slot);
            Mark(p, slot, m, fromTeam);
            Simulation.Equip(s, id, slot);
            outcome.FromTeam = fromTeam;
            outcome.Result = ShopResult.Bought;
            return outcome;
        }

        /// <summary>Segna l'istanza come "comprata in questa fase": è quello che la rende vendibile.</summary>
        private static void Mark(PlayerState p, WeaponSlot slot, MatchState m, bool paidFromTeam)
        {
            var w = p.Cache[(int)slot];
            if (w == null) return;
            w.BoughtThisBuy = true;
            w.BoughtRound = m.Round;
            w.PaidFromTeam = paidFromTeam;
        }

        /// <summary>
        /// Vendita esplicita (tasto nel negozio). Solo le armi comprate in questa fase, e solo
        /// nella fase di acquisto: è la stessa regola della sostituzione, applicata a mano.
        /// </summary>
        public static BuyOutcome Sell(SimState s, MatchState m, int id, WeaponSlot slot)
        {
            var outcome = new BuyOutcome { Slot = slot };
            var p = s.Get(id);
            var def = Arsenal.Get(slot);
            if (p == null || !p.Alive) { outcome.Result = ShopResult.Dead; return outcome; }
            if (def.Melee || def.Price <= 0) { outcome.Result = ShopResult.NotForSale; return outcome; }
            if (!s.UnlimitedMoney && m.Phase != RoundPhase.Buy) { outcome.Result = ShopResult.NotBuyPhase; return outcome; }

            var w = p.Cache[(int)slot];
            if (w == null || !w.BoughtThisBuy) { outcome.Result = ShopResult.NotForSale; return outcome; }

            outcome.Refund = def.Price;
            Refund(s, m, p, w.PaidFromTeam, outcome.Refund);
            p.Cache[(int)slot] = null;

            // se era in mano, si passa a quello che resta: first the other gun, then the knife
            if (p.Weapon.Slot == slot)
            {
                var next = HeldPrimary(p) ?? HeldSecondary(p) ?? WeaponSlot.Melee;
                Simulation.Equip(s, id, next);
            }
            outcome.Result = ShopResult.Bought;      // "Ok" per il client: l'arma è uscita
            return outcome;
        }

        /// <summary>
        /// Porta l'arma sostituita in mano al posto della nuova: serve quando si RACCOGLIE
        /// un'arma da terra e lo slot è già occupato.
        /// </summary>
        public static void ReplaceForPickup(SimState s, MatchState m, WorldState w, PlayerState p, WeaponSlot incoming)
        {
            var def = Arsenal.Get(incoming);
            var occupantSlot = def.IsPrimary ? HeldPrimary(p) : HeldSecondary(p);
            if (!occupantSlot.HasValue || occupantSlot.Value == incoming) return;

            var occ = p.Cache[(int)occupantSlot.Value];
            if (occ != null && occ.BoughtThisBuy)
            {
                Refund(s, m, p, occ.PaidFromTeam, Arsenal.Get(occupantSlot.Value).Price);
                p.Cache[(int)occupantSlot.Value] = null;
            }
            else
            {
                Simulation.DropFromCache(s, w, p, occupantSlot.Value, throwForward: false);
            }
        }
    }

    /// <summary>La cassa comune: costanti e operazioni. I soldi sono della squadra, non del giocatore.</summary>
    public static class Economy
    {
        /// <summary>Sotto questa cifra si paga di tasca propria, senza passare dalla cassa.</summary>
        public const int FreeThreshold = 300;

        /// <summary>La cassa comune a inizio partita (doc 04).</summary>
        public const int TeamStartFunds = 5000;

        /// <summary>Il fondo personale a inizio partita (doc 04): serve a non partire disarmati.</summary>
        public const int PersonalStart = 1000;

        public static int TeamFunds(MatchState m, int team) => Math.Max(0, team == 1 ? m.TeamFundsNorth : m.TeamFundsSouth);

        public static void AddTeamFunds(MatchState m, int team, int amount)
        {
            if (amount == 0) return;
            if (team == 1) m.TeamFundsNorth = Math.Clamp(m.TeamFundsNorth + amount, 0, Match.MaxCredits * 5);
            else m.TeamFundsSouth = Math.Clamp(m.TeamFundsSouth + amount, 0, Match.MaxCredits * 5);
        }

        public static void SpendTeam(MatchState m, int team, int amount)
            => AddTeamFunds(m, team, -Math.Max(0, amount));
    }
}
