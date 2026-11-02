using System;
using System.Collections.Generic;

namespace Falezia.Sim
{
    /// <summary>I tre posti dell'arsenale di un giocatore.</summary>
    public enum LoadoutSlot
    {
        Primary = 0,     // SMG, pompe, AR, cecchini, LMG
        Secondary = 1,   // pistole
        Melee = 2        // coltello, sempre
    }

    /// <summary>Come è andato un acquisto: il client mostra il motivo, non un bool.</summary>
    public enum BuyResult
    {
        Bought,        // comprata e messa in mano
        Equipped,      // già tua: solo equipaggiata
        NotEnough,     // non hai i soldi
        NotForSale,    // il coltello (e la Standard) non si comprano
        IsMelee        // il coltello ha il suo slot, non si compra
    }

    /// <summary>
    /// L'ARSENALE DEL GIOCATORE: soldi, armi possedute, una primaria e una secondaria.
    ///
    /// Regole dal doc 04 e dalla richiesta di gioco:
    ///   · si parte con la **Standard** (secondaria, gratis) e il **coltello** — niente primaria;
    ///   · si può avere **una sola primaria e una sola secondaria**: comprare un'altra arma
    ///     dello stesso tipo prende il posto della precedente (che resta posseduta, non si vende);
    ///   · le armi si comprano **con i soldi di partita**; la cassa comune arriverà col round;
    ///   · nella stanza debug `Unlimited` è true: i soldi non finiscono mai.
    ///
    /// Nessun riferimento a Godot: questo file compila in una console pura, come la simulazione.
    /// </summary>
    public sealed class LoadoutState
    {
        public const int StartingMoney = 1000;

        public int Money = StartingMoney;
        public bool Unlimited;

        public readonly HashSet<WeaponSlot> Owned = new();

        /// <summary>La primaria equipaggiata: una sola, o nessuna.</summary>
        public WeaponSlot? Primary;
        /// <summary>La secondaria equipaggiata: una sola. Allo spawn è sempre la Standard.</summary>
        public WeaponSlot Secondary;
        /// <summary>Il coltello: slot dedicato, non si compra e non si perde.</summary>
        public WeaponSlot MeleeSlot = WeaponSlot.Melee;

        public LoadoutState()
        {
            Owned.Add(WeaponSlot.Standard);
            Owned.Add(WeaponSlot.Melee);
            Secondary = WeaponSlot.Standard;
        }

        /// <summary>Quanti soldi hai davvero (per l'HUD: con Unlimited si mostra ∞).</summary>
        public int Spendable => Unlimited ? int.MaxValue : Money;

        public bool Owns(WeaponSlot slot) => Owned.Contains(slot);

        /// <summary>
        /// L'arma che hai in mano appena spawni: la primaria se l'hai comprata, altrimenti la
        /// secondaria — e per un loadout nuovo è la Standard: non si spawna mai con un fucile regalato.
        /// </summary>
        public WeaponSlot DeployWeapon => Primary ?? Secondary;

        /// <summary>La primaria in mano, se ne hai una; altrimenti null.</summary>
        public WeaponSlot? PrimaryWeapon => Primary;

        /// <summary>L'arma di uno slot, o null se quello slot è vuoto (la primaria all'inizio).</summary>
        public WeaponSlot? Get(LoadoutSlot slot) => slot switch
        {
            LoadoutSlot.Primary => Primary,
            LoadoutSlot.Secondary => Secondary,
            _ => MeleeSlot
        };

        /// <summary>True se lo slot ha un'arma da mettere in mano.</summary>
        public bool Has(LoadoutSlot slot) => slot == LoadoutSlot.Melee || Get(slot).HasValue;

        /// <summary>
        /// Compra (o equipaggia) un'arma. Il prezzo viene dalla scheda (`WeaponDef.Price`).
        /// Comprarla la mette subito nello slot giusto: una primaria sostituisce la primaria.
        /// </summary>
        public BuyResult Buy(WeaponSlot slot)
        {
            var def = Arsenal.Get(slot);
            if (def.Melee) return BuyResult.IsMelee;
            if (def.Price <= 0)
            {
                // Standard: gratis, e non serve comprarla
                Owned.Add(slot);
                Equip(slot);
                return BuyResult.Equipped;
            }
            if (Owned.Contains(slot))
            {
                Equip(slot);
                return BuyResult.Equipped;
            }
            if (!Unlimited && Money < def.Price) return BuyResult.NotEnough;

            if (!Unlimited) Money -= def.Price;

            // una sola primaria e una sola secondaria: la nuova arma CONSUMA quella che avevi
            // (doc 04: «la vecchia viene consumata»). Standard e coltello non si consumano mai.
            var previous = def.IsPrimary ? Primary : Secondary;
            if (previous.HasValue && previous.Value != slot && Arsenal.Get(previous.Value).Price > 0)
                Owned.Remove(previous.Value);

            Owned.Add(slot);
            Equip(slot);
            return BuyResult.Bought;
        }

        /// <summary>Mette l'arma nel suo slot. Non si può equipaggiare quello che non possiedi.</summary>
        public bool Equip(WeaponSlot slot)
        {
            if (!Owned.Contains(slot)) return false;
            var def = Arsenal.Get(slot);
            if (def.Melee) return false;                 // il coltello è già lì
            if (def.IsPrimary) Primary = slot;
            else Secondary = slot;
            return true;
        }

        /// <summary>
        /// Il tasto del cambio: 1 = primaria (se c'è), 2 = secondaria, 3 = coltello.
        /// Torna l'arma da mettere in mano, o null se quello slot è vuoto.
        /// </summary>
        public WeaponSlot? ForSlotKey(int key) => key switch
        {
            1 => Primary,
            2 => Secondary,
            _ => MeleeSlot
        };

        /// <summary>Gira fra gli slot che hanno un'arma: serve alla rotella del mouse.</summary>
        public WeaponSlot NextHeld(WeaponSlot current, int direction)
        {
            var list = new List<WeaponSlot>(3);
            if (Primary.HasValue) list.Add(Primary.Value);
            list.Add(Secondary);
            list.Add(MeleeSlot);
            int i = list.IndexOf(current);
            if (i < 0) return list[0];
            int n = list.Count;
            return list[((i + (direction >= 0 ? 1 : n - 1)) % n + n) % n];
        }
    }
}
