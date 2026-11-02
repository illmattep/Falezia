using System;
using System.Collections.Generic;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>
    /// L'arsenale. L'ordine è per classe (pistole, SMG, pompe, AR, cecchini, mitragliatrice,
    /// coltello) e l'indice è STABILE: va in rete, quindi non si rinumera.
    /// </summary>
    public enum WeaponSlot
    {
        // pistole
        Standard = 0,
        Hush = 1,
        Chatter = 2,
        Snub = 3,
        Hammer = 4,
        // SMG
        Cadence = 5,
        Scrap = 6,
        Blitz = 7,
        // pompe
        Choke = 8,
        Sweeper = 9,
        // fucili d'assalto
        Anvil = 10,
        Service = 11,
        Vantage = 12,
        // cecchini
        Spotter = 13,
        Longshot = 14,
        // mitragliatrice leggera (fuori dal roster del doc 11: c'è per la stanza debug)
        Heavy = 15,
        // coltello
        Melee = 16
    }

    /// <summary>
    /// Come spara un'arma. Una scheda può ammetterne più d'uno (Cadence e Service hanno
    /// semiauto / 3 colpi / full-auto): il giocatore cambia con X.
    /// </summary>
    [System.Flags]
    public enum FireMode
    {
        Semi = 1,     // un colpo per click
        Burst = 2,    // una raffica di <see cref="WeaponDef.BurstCount"/> colpi per click
        Auto = 4      // tenere premuto spara
    }

    /// <summary>
    /// Dati di un'arma. I numeri vengono da docs/11-armi.md, scheda per scheda: danno,
    /// colpi, cadenza, caricatore, ricarica tattica/a vuoto, ADS, movimento, precisione.
    ///
    /// La dispersione è un CONO, in gradi, e cambia con quello che stai facendo:
    /// fermo, in movimento, in mira. Vedi <see cref="Arsenal.SpreadAt"/>.
    /// Nessun riferimento a Godot: la simulazione deve girare anche in una console pura.
    /// </summary>
    public sealed class WeaponDef
    {
        public string Id;
        public string Name;
        public string Class;
        public int Price;

        // --- tiro
        public int Damage;           // danno per PALLA (per i fucili a pompa: danno × Pellets)
        public int Pellets = 1;      // palle per cartuccia: 1 per tutto tranne le pompe
        public int Magazine;
        public int Reserve;
        public float Cycle;          // secondi fra due colpi
        public float ReloadTactical; // colpo in canna
        public float ReloadEmpty;    // caricatore esaurito
        public float AdsTime;
        public bool Silenced;        // si sente solo entro NoiseMeters
        public bool Automatic;       // tenere premuto spara (scorciatoia: Auto è fra i Modi)
        public bool Melee;           // il coltello: niente munizioni, niente ricarica

        // --- modi di fuoco e raffica
        public FireMode Modes = FireMode.Semi;
        public FireMode DefaultMode = FireMode.Semi;
        public int BurstCount = 3;      // colpi per raffica (doc 11: 3 colpi su Cadence e Service)
        public float BurstReset = 0.30f; // dopo tanto silenzio la raffica si può ripartire

        // --- ottica
        public float Magnification = 1f;  // 1 = mire metalliche, 1,2 = punto rosso, 3/4/6 = ottica
        public string OpticName = "mire metalliche";
        public float NoiseMeters;    // raggio in cui lo sparo si sente (999 = tutta la mappa)

        // --- caduta del danno con la distanza
        public float FalloffStart;   // metri: oltre, il danno cala
        public float FalloffEnd;
        public float FalloffMin = 0.55f;   // quanto resta del danno a FalloffEnd

        // --- movimento
        public float MoveScale = 1f;       // moltiplicatore del passo con l'arma in mano
        public float AdsMoveScale = 0.55f; // moltiplicatore del passo in mira

        // --- rinculo della camera (gradi per colpo): pattern verticale + deriva
        public float RecoilUp = 0.8f;      // quanto sale la mira al primo colpo
        public float RecoilSide = 0.25f;   // ampiezza del zig-zag orizzontale

        // --- dispersione (gradi, semiapertura del cono)
        public float SpreadStill;    // fermo in piedi, da fianco
        public float SpreadMove;     // camminando o correndo, da fianco
        public float SpreadAds;      // fermo, in mira
        public float SpreadAdsMove;  // in movimento, in mira
        // --- dispersione: la SERIE (stile CS/Valorant). I primi colpi sono puliti,
        // poi il cono si apre a ogni colpo fino a un tetto, e si azzera col silenzio.
        public float BloomPerShot;   // (storico) usato per derivare SpreadPerShot
        public float BloomMax;       // (storico) usato per derivare SpreadPeak
        public float BloomDecay;     // (storico) non più usato dal calcolo della dispersione
        public int AccurateShots = 3;      // colpi puliti prima che il cono si apra
        public float SpreadPerShot = 0.25f; // (storico) non più usato: la curva usa il tetto
        public float SpreadCurve = 1.55f;   // quanto accelera colpo dopo colpo
        public float SpreadPeak = 3.0f;     // tetto dell'apertura
        public int SpreadRamp = 7;          // colpi (dopo la finestra pulita) per arrivare al tetto
        public float SpreadReset = 0.34f;   // secondi di silenzio che azzerano la serie

        /// <summary>
        /// Quanto un click arrivato in anticipo resta in attesa (secondi). Dentro questa
        /// finestra il colpo parte appena l'arma è pronta; oltre, il click si scarta:
        /// né colpi in più, né colpi che arrivano con un ritardo che si sente.
        /// </summary>
        public float FireBuffer;

        /// <summary>Cadenza in colpi al minuto, per la scheda e per l'HUD.</summary>
        public float RoundsPerMinute => 60f / Cycle;

        /// <summary>Danno di una cartuccia intera (una palla per le armi normali, tutte per le pompe).</summary>
        public int ShotDamage => Damage * Pellets;

        /// <summary>True se ha un'ottica vera (maschera nera e reticolo in mira).</summary>
        public bool Scoped => Magnification >= 3f;
        /// <summary>True se ha un punto rosso o un'olografica: un mirino più chiaro, non un cannocchiale.</summary>
        public bool DotSight => Magnification > 1.05f && Magnification < 3f;

        /// <summary>La primaria sta nello slot grande: SMG, pompe, AR, cecchini, mitragliatrice.</summary>
        public bool IsPrimary => Id is "cadence" or "scrap" or "blitz" or "choke" or "sweeper"
                                        or "anvil" or "service" or "vantage" or "spotter" or "longshot" or "heavy";
        /// <summary>Le pistole sono la secondaria.</summary>
        public bool IsSecondary => !IsPrimary && !Melee;

        /// <summary>In quale dei tre posti dell'arsenale finisce.</summary>
        public LoadoutSlot SlotKind => Melee ? LoadoutSlot.Melee
            : IsPrimary ? LoadoutSlot.Primary : LoadoutSlot.Secondary;

        /// <summary>Il nome del modo, per l'HUD.</summary>
        public static string ModeName(FireMode m) => m switch
        {
            FireMode.Burst => "3 COLPI",
            FireMode.Auto => "AUTO",
            _ => "SEMI"
        };

        /// <summary>La prossima modalità ammessa, girando SEMI → 3 COLPI → AUTO.</summary>
        public FireMode NextMode(FireMode current)
        {
            var order = new[] { FireMode.Semi, FireMode.Burst, FireMode.Auto };
            int i = System.Array.IndexOf(order, current);
            for (int k = 1; k <= 3; k++)
            {
                var cand = order[(i + k) % 3];
                if ((Modes & cand) != 0) return cand;
            }
            return current;
        }

        /// <summary>Profilo del mirino a schermo, per DebugRoom/Crosshair.</summary>
        public string Profile =>
            Melee ? "knife" :
            Id is "choke" or "sweeper" ? "shotgun" :
            Id is "spotter" or "longshot" ? "sniper" :
            Id is "cadence" or "scrap" or "blitz" ? "smg" :
            Id is "anvil" or "service" or "vantage" or "heavy" ? "rifle" : "pistol";
    }

    /// <summary>Stato dell'arma di un giocatore: munizioni, timer, contatori per gli eventi visivi.</summary>
    public sealed class WeaponState
    {
        public WeaponSlot Slot = WeaponSlot.Standard;
        public int Ammo;
        public int Reserve;
        public float FireCd;
        public float ReloadT;
        public bool Reloading;
        public bool ReloadWasEmpty;
        public float AdsT;          // 0 = hip, 1 = mirato

        /// <summary>Cono corrente, in gradi. Lo calcola la simulazione a ogni tick.</summary>
        public float Spread;
        /// <summary>Quanto del cono è dovuto alla serie (derivato: per l'HUD).</summary>
        public float Bloom;
        /// <summary>Colpi sparati nella serie corrente: 0 = canna fredda.</summary>
        public int ShotIndex;
        /// <summary>Secondi dall'ultimo colpo: oltre SpreadReset la serie riparte da zero.</summary>
        public float SinceShot;
        /// <summary>Cono usato dall'ULTIMO colpo: è quello che il client deve disegnare.</summary>
        public float ShotSpread;
        /// <summary>Seme dell'ultimo colpo: due client che sparano lo stesso colpo hanno la stessa traiettoria.</summary>
        public int ShotSeed;

        /// <summary>Quanto resta in attesa il click in anticipo (secondi). 0 = niente in coda.</summary>
        public float FireBuffer;
        /// <summary>Il grilletto era premuto al tick scorso: serve a contare i click, non i tick.</summary>
        public bool FireLatch;

        /// <summary>Modalità di fuoco corrente: una di quelle ammesse dalla scheda.</summary>
        public FireMode Mode = FireMode.Semi;
        /// <summary>Colpi che restano nella raffica (0 = nessuna raffica in corso).</summary>
        public int BurstLeft;
        /// <summary>Quanto è passato dall'ultimo colpo: oltre BurstReset la raffica è chiusa.</summary>
        public float BurstTimer;

        // --- l'ISTANZA: quando è stata comprata e con che soldi.
        // Serve alla regola di Valorant: quello che compri in questa fase si può vendere,
        // quello che ti porti dal round prima no (si lascia a terra).
        public bool BoughtThisBuy;
        public int BoughtRound;
        public bool PaidFromTeam;

        // contatori: il client li confronta per far partire le animazioni
        public int Shots;
        public int ShotsLast;       // colpi che hanno svuotato il caricatore
        public int ReloadStarts;
        public int DryFires;

        public void Refill(WeaponDef d)
        {
            Slot = (WeaponSlot)System.Array.IndexOf(Arsenal.All, d);
            Ammo = d.Magazine;
            Reserve = d.Reserve;
            FireCd = 0;
            ReloadT = 0;
            Reloading = false;
            AdsT = 0;
            Spread = d.SpreadStill;
            Bloom = 0;
            ShotIndex = 0;
            SinceShot = 99f;
            ShotSpread = d.SpreadStill;
            ShotSeed = 0;
            FireBuffer = 0;
            FireLatch = false;
            Mode = d.DefaultMode;
            BurstLeft = 0;
            BurstTimer = 0;
            BoughtThisBuy = false;
            BoughtRound = 0;
            PaidFromTeam = false;
        }
    }

    public static class Arsenal
    {
        /// <summary>2π. MathF.Tau non esiste in .NET Standard (Unity): costante nostra.</summary>
        public const float TwoPi = 6.28318530718f;

        // Precisione dal doc 11, convertita in gradi di semiapertura: atan(cm / 2500 cm).
        // 0,8 cm → 0,018° · 1,0 → 0,023° · 1,4 → 0,032° · 2,0 → 0,046° · 2,2 → 0,050°
        // 3,0 → 0,069° · 4,5 → 0,103° · 5,0 → 0,115° · 6,0 → 0,138°.
        // Da fianco i numeri sono più larghi: la pistola sta a 22 cm (0,50°), l'Hammer a 45 (1,03°).

        public static readonly WeaponDef Standard = new WeaponDef
        {
            Id = "standard",
            Name = "Standard",
            Class = "Pistola",
            Price = 0,
            Damage = 25,
            Magazine = 17,
            Reserve = 51,
            Cycle = 0.20833f,         // 288 RPM (era 240: +20% il 26 ott 2026)
            ReloadTactical = 1.50f,
            ReloadEmpty = 2.00f,
            AdsTime = 0.20f,
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 15f,
            FalloffEnd = 35f,
            MoveScale = 1.00f,
            AdsMoveScale = 0.92f,
            SpreadStill = 0.50f,
            SpreadMove = 2.60f,
            SpreadAds = 0.035f,
            SpreadAdsMove = 0.55f,
            BloomPerShot = 0.28f,
            BloomMax = 1.50f,
            BloomDecay = 3.60f,
            FireBuffer = 0.10f
        };

        public static readonly WeaponDef Hush = new WeaponDef
        {
            Id = "hush",
            Name = "Hush",
            Class = "Pistola silenziata",
            Price = 400,
            Damage = 22,
            Magazine = 15,
            Reserve = 45,
            Cycle = 0.2275f,          // 264 RPM (era 220: +20%)
            ReloadTactical = 1.70f,
            ReloadEmpty = 2.20f,
            AdsTime = 0.24f,
            Silenced = true,
            NoiseMeters = 40f,        // doc 11: si sente a 40 m, non in tutta la mappa
            Automatic = false,
            FalloffStart = 15f,
            FalloffEnd = 35f,
            MoveScale = 1.00f,
            AdsMoveScale = 0.90f,
            SpreadStill = 0.42f,
            SpreadMove = 2.30f,
            SpreadAds = 0.025f,
            SpreadAdsMove = 0.48f,
            BloomPerShot = 0.24f,
            BloomMax = 1.30f,
            BloomDecay = 3.40f,
            FireBuffer = 0.11f
        };

        /// <summary>Pistola compatta full-auto: il primo gradino sopra la Standard.</summary>
        public static readonly WeaponDef Chatter = new WeaponDef
        {
            Id = "chatter",
            Name = "Chatter",
            Class = "Pistola automatica",
            Price = 600,
            Damage = 18,
            Magazine = 20,
            Reserve = 60,
            Cycle = 0.08083f,         // 742 RPM (era 620: +20%)
            ReloadTactical = 1.80f,
            ReloadEmpty = 2.40f,
            AdsTime = 0.28f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 10f,
            FalloffEnd = 25f,
            MoveScale = 1.00f,
            AdsMoveScale = 0.90f,
            SpreadStill = 0.69f,      // 30 cm a 25 m da fianco
            SpreadMove = 3.10f,
            SpreadAds = 0.103f,       // 4,5 cm
            SpreadAdsMove = 0.75f,
            BloomPerShot = 0.30f,
            BloomMax = 1.90f,
            BloomDecay = 4.20f,
            FireBuffer = 0.03f
        };

        /// <summary>Rivoltella a canna corta: 50 danni ma tre secondi di ricarica.</summary>
        public static readonly WeaponDef Snub = new WeaponDef
        {
            Id = "snub",
            Name = "Snub",
            Class = "Rivoltella",
            Price = 500,
            Damage = 50,
            Magazine = 6,
            Reserve = 18,
            Cycle = 0.2775f,          // 216 RPM (era 180: +20%)
            ReloadTactical = 3.00f,   // speedloader, e non c'è differenza fra tattica e a vuoto
            ReloadEmpty = 3.00f,
            AdsTime = 0.16f,          // il più rapido del gioco
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 15f,
            FalloffEnd = 35f,
            MoveScale = 1.00f,
            AdsMoveScale = 0.92f,
            SpreadStill = 0.90f,
            SpreadMove = 2.90f,
            SpreadAds = 0.050f,       // 2,2 cm
            SpreadAdsMove = 0.62f,
            BloomPerShot = 1.10f,     // salto verticale forte
            BloomMax = 3.00f,
            BloomDecay = 3.00f,
            FireBuffer = 0.12f
        };

        /// <summary>Magnum civile: due colpi e hai vinto, anche contro la piastra pesante.</summary>
        public static readonly WeaponDef Hammer = new WeaponDef
        {
            Id = "hammer",
            Name = "Hammer",
            Class = "Magnum",
            Price = 700,
            Damage = 70,
            Magazine = 7,
            Reserve = 21,
            Cycle = 0.55583f,         // 108 RPM (era 90: +20%)
            ReloadTactical = 2.10f,
            ReloadEmpty = 2.80f,
            AdsTime = 0.35f,
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 20f,
            FalloffEnd = 45f,
            MoveScale = 0.99f,
            AdsMoveScale = 0.84f,
            SpreadStill = 1.03f,      // 45 cm da fianco: inutilizzabile senza mirare
            SpreadMove = 3.20f,
            SpreadAds = 0.041f,       // 1,8 cm
            SpreadAdsMove = 0.72f,
            BloomPerShot = 1.60f,     // il rinculo più violento del gioco
            BloomMax = 4.20f,
            BloomDecay = 2.80f,
            FireBuffer = 0.27f
        };

        /// <summary>SMG: la mobilità. Il profilo basso si distingue a colpo d'occhio.</summary>
        public static readonly WeaponDef Cadence = new WeaponDef
        {
            Id = "cadence",
            Name = "Cadence",
            Class = "SMG",
            Price = 900,
            Damage = 20,
            Magazine = 30,
            Reserve = 90,
            Cycle = 0.09583f,         // 626 RPM (era 520: +20%, raffica da 3)
            ReloadTactical = 2.20f,
            ReloadEmpty = 2.90f,
            AdsTime = 0.26f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 25f,
            FalloffEnd = 45f,
            MoveScale = 0.96f,
            AdsMoveScale = 0.62f,
            SpreadStill = 1.35f,
            SpreadMove = 3.40f,
            SpreadAds = 0.069f,       // 3,0 cm
            SpreadAdsMove = 0.62f,
            BloomPerShot = 0.35f,
            BloomMax = 2.20f,
            BloomDecay = 4.50f,       // orizzontale quasi assente: la raffica resta gestibile
            FireBuffer = 0.037f
        };

        /// <summary>La più economica: lamiera, calcio a filo, nastro. Imprecisa ma è un fucile.</summary>
        public static readonly WeaponDef Scrap = new WeaponDef
        {
            Id = "scrap",
            Name = "Scrap",
            Class = "SMG economica",
            Price = 600,
            Damage = 17,
            Magazine = 20,
            Reserve = 60,
            Cycle = 0.08333f,         // 720 RPM (era 600: +20%)
            ReloadTactical = 2.00f,
            ReloadEmpty = 2.60f,      // la più rapida delle primarie
            AdsTime = 0.24f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 18f,
            FalloffEnd = 38f,
            MoveScale = 0.97f,
            AdsMoveScale = 0.62f,
            SpreadStill = 1.95f,
            SpreadMove = 3.85f,
            SpreadAds = 0.138f,       // 6,0 cm: la peggiore delle primarie
            SpreadAdsMove = 0.85f,
            BloomPerShot = 0.45f,     // irregolare e laterale
            BloomMax = 2.60f,
            BloomDecay = 4.00f,
            FireBuffer = 0.033f
        };

        /// <summary>PDW bullpup: il caricatore orizzontale sopra la canna, ingombro minimo.</summary>
        public static readonly WeaponDef Blitz = new WeaponDef
        {
            Id = "blitz",
            Name = "Blitz",
            Class = "PDW",
            Price = 1000,
            Damage = 16,
            Magazine = 30,
            Reserve = 90,
            Cycle = 0.07167f,         // 837 RPM (era 700: +20%)
            ReloadTactical = 2.40f,
            ReloadEmpty = 3.10f,
            AdsTime = 0.24f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 12f,       // oltre 15 m la dispersione cresce rapidamente
            FalloffEnd = 28f,
            FalloffMin = 0.45f,
            MoveScale = 0.98f,
            AdsMoveScale = 0.64f,
            SpreadStill = 1.72f,
            SpreadMove = 3.60f,
            SpreadAds = 0.115f,       // 5,0 cm
            SpreadAdsMove = 0.80f,
            BloomPerShot = 0.55f,     // sale a picco dopo il decimo colpo
            BloomMax = 3.20f,
            BloomDecay = 3.60f,
            FireBuffer = 0.026f
        };

        /// <summary>Pompa a ripetizione manuale: il re dei sei metri.</summary>
        public static readonly WeaponDef Choke = new WeaponDef
        {
            Id = "choke",
            Name = "Choke",
            Class = "Pompa",
            Price = 1100,
            Damage = 11,              // × 12 palle = 132 a bruciapelo
            Pellets = 12,
            Magazine = 6,
            Reserve = 24,
            Cycle = 0.90833f,         // 66 RPM: pompata manuale (era 55: +20%)
            ReloadTactical = 3.60f,   // 0,6 s per cartuccia, tutto il tubo
            ReloadEmpty = 3.60f,
            AdsTime = 0.30f,
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 3f,
            FalloffEnd = 14f,
            FalloffMin = 0.12f,       // 130 → 90 a 6 m → 45 a 12 m → 15 a 20 m
            MoveScale = 0.94f,
            AdsMoveScale = 0.50f,
            SpreadStill = 3.60f,      // cono di pallini
            SpreadMove = 4.60f,
            SpreadAds = 2.40f,        // in mira è 1/3 più stretto
            SpreadAdsMove = 3.40f,
            BloomPerShot = 0.90f,
            BloomMax = 3.50f,
            BloomDecay = 3.00f,
            FireBuffer = 0.45f
        };

        /// <summary>Pompa semiautomatica: due colpi in 0,35 s e una ricarica vera.</summary>
        public static readonly WeaponDef Sweeper = new WeaponDef
        {
            Id = "sweeper",
            Name = "Sweeper",
            Class = "Pompa semiautomatico",
            Price = 1400,
            Damage = 10,              // × 10 palle = 100
            Pellets = 10,
            Magazine = 7,
            Reserve = 21,
            Cycle = 0.45417f,         // 132 RPM (era 110: +20%)
            ReloadTactical = 2.60f,
            ReloadEmpty = 3.40f,
            AdsTime = 0.32f,
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 2.5f,
            FalloffEnd = 13f,
            FalloffMin = 0.12f,       // 100 → 75 a 6 m → 40 a 12 m → 12 a 20 m
            MoveScale = 0.95f,
            AdsMoveScale = 0.50f,
            SpreadStill = 4.10f,
            SpreadMove = 5.00f,
            SpreadAds = 2.90f,
            SpreadAdsMove = 3.80f,
            BloomPerShot = 0.75f,
            BloomMax = 3.20f,
            BloomDecay = 3.00f,
            FireBuffer = 0.175f
        };

        /// <summary>AR 7,62: il danno per colpo. L'unica AR che non puoi usare a raffica.</summary>
        public static readonly WeaponDef Anvil = new WeaponDef
        {
            Id = "anvil",
            Name = "Anvil",
            Class = "Fucile d'assalto 7,62",
            Price = 1400,
            Damage = 30,
            Magazine = 30,
            Reserve = 90,
            Cycle = 0.0925f,          // 649 RPM (era 540: +20%)
            ReloadTactical = 2.60f,
            ReloadEmpty = 3.30f,
            AdsTime = 0.30f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 35f,
            FalloffEnd = 60f,
            MoveScale = 0.93f,
            AdsMoveScale = 0.55f,
            SpreadStill = 1.26f,
            SpreadMove = 3.50f,
            SpreadAds = 0.050f,       // 2,2 cm: il primo colpo è il più preciso del gioco
            SpreadAdsMove = 0.66f,
            BloomPerShot = 0.85f,     // sale di 3° per colpo e deriva a destra
            BloomMax = 3.60f,
            BloomDecay = 3.00f,
            FireBuffer = 0.05f
        };

        /// <summary>AR 5,56: la standard. Nessun difetto, nessun pregio.</summary>
        public static readonly WeaponDef Service = new WeaponDef
        {
            Id = "service",
            Name = "Service",
            Class = "Fucile d'assalto",
            Price = 1400,
            Damage = 25,
            Magazine = 30,
            Reserve = 90,
            Cycle = 0.08167f,         // 735 RPM (era 610: +20%)
            ReloadTactical = 2.30f,
            ReloadEmpty = 3.00f,
            AdsTime = 0.28f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 35f,
            FalloffEnd = 60f,
            MoveScale = 0.95f,
            AdsMoveScale = 0.55f,
            SpreadStill = 1.03f,
            SpreadMove = 3.30f,
            SpreadAds = 0.046f,       // 2,0 cm: la più precisa delle AR
            SpreadAdsMove = 0.58f,
            BloomPerShot = 0.40f,
            BloomMax = 2.40f,
            BloomDecay = 4.00f,
            FireBuffer = 0.043f
        };

        /// <summary>Fucile da battaglia: fortissimo oltre i 25 m, inutile sotto i 10.</summary>
        public static readonly WeaponDef Vantage = new WeaponDef
        {
            Id = "vantage",
            Name = "Vantage",
            Class = "Fucile da battaglia",
            Price = 1800,
            Damage = 60,
            Magazine = 20,
            Reserve = 60,
            Cycle = 0.45417f,         // 132 RPM, semiauto forzato (era 110: +20%)
            ReloadTactical = 2.80f,
            ReloadEmpty = 3.50f,
            AdsTime = 0.45f,          // il più lento del gioco
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 70f,
            FalloffEnd = 110f,
            MoveScale = 0.91f,
            AdsMoveScale = 0.45f,
            SpreadStill = 2.06f,
            SpreadMove = 4.10f,
            SpreadAds = 0.032f,       // 1,4 cm, affidabile fino a 70 m
            SpreadAdsMove = 0.95f,
            BloomPerShot = 1.70f,     // violento ma perfettamente verticale
            BloomMax = 4.50f,
            BloomDecay = 2.60f,
            FireBuffer = 0.20f
        };

        /// <summary>Cecchino semiautomatico: il secondo colpo è già letale.</summary>
        public static readonly WeaponDef Spotter = new WeaponDef
        {
            Id = "spotter",
            Name = "Spotter",
            Class = "Cecchino semiautomatico",
            Price = 1800,
            Damage = 75,
            Magazine = 10,
            Reserve = 30,
            Cycle = 0.41667f,         // 144 RPM (era 120: +20%)
            ReloadTactical = 3.00f,
            ReloadEmpty = 3.80f,
            AdsTime = 0.40f,
            Automatic = false,
            NoiseMeters = 999f,
            FalloffStart = 80f,
            FalloffEnd = 140f,
            FalloffMin = 0.80f,
            MoveScale = 0.92f,
            AdsMoveScale = 0.32f,
            SpreadStill = 1.60f,
            SpreadMove = 3.70f,
            SpreadAds = 0.023f,       // 1,0 cm, affidabile a 150 m
            SpreadAdsMove = 0.80f,
            BloomPerShot = 1.40f,     // salto forte, ma rientra in 0,35 s
            BloomMax = 3.80f,
            BloomDecay = 2.80f,
            FireBuffer = 0.175f
        };

        /// <summary>Otturatore manuale: un colpo, un round. Sbagliare è perdere.</summary>
        public static readonly WeaponDef Longshot = new WeaponDef
        {
            Id = "longshot",
            Name = "Longshot",
            Class = "Fucile a otturatore",
            Price = 2400,
            Damage = 150,
            Magazine = 5,
            Reserve = 20,
            Cycle = 1.25f,            // 48 RPM: ciclo dell'otturatore (era 40: +20%)
            ReloadTactical = 3.20f,
            ReloadEmpty = 4.00f,
            AdsTime = 0.50f,
            Automatic = false,
            NoiseMeters = 999f,       // si sente in tutta la mappa
            FalloffStart = 150f,
            FalloffEnd = 220f,
            FalloffMin = 0.90f,
            MoveScale = 0.88f,        // il più penalizzante
            AdsMoveScale = 0.22f,
            SpreadStill = 2.40f,
            SpreadMove = 4.60f,
            SpreadAds = 0.018f,       // 0,8 cm, affidabile oltre 200 m
            SpreadAdsMove = 1.10f,
            BloomPerShot = 2.60f,
            BloomMax = 6.00f,
            BloomDecay = 2.20f,
            FireBuffer = 0.70f
        };

        /// <summary>
        /// Mitragliatrice leggera: fuori dal roster del doc 11 (che dice no alla LMG),
        /// tenuta perché è nell'arsenale dell'utente. Tamburo, bipod, nastro.
        /// </summary>
        public static readonly WeaponDef Heavy = new WeaponDef
        {
            Id = "heavy",
            Name = "Heavy",
            Class = "Mitragliatrice leggera",
            Price = 2000,
            Damage = 26,
            Magazine = 60,
            Reserve = 120,
            Cycle = 0.07583f,         // 791 RPM (era 660: +20%)
            ReloadTactical = 4.20f,
            ReloadEmpty = 5.20f,
            AdsTime = 0.42f,
            Automatic = true,
            NoiseMeters = 999f,
            FalloffStart = 40f,
            FalloffEnd = 70f,
            MoveScale = 0.90f,
            AdsMoveScale = 0.40f,
            SpreadStill = 1.80f,
            SpreadMove = 4.00f,
            SpreadAds = 0.070f,
            SpreadAdsMove = 0.90f,
            BloomPerShot = 0.60f,
            BloomMax = 3.40f,
            BloomDecay = 3.40f,
            FireBuffer = 0.04f
        };

        /// <summary>Coltello: 55 danni, silenzioso, e con il coltello in mano ti muovi più veloce.</summary>
        public static readonly WeaponDef Melee = new WeaponDef
        {
            Id = "melee",
            Name = "Combat Knife",
            Class = "Coltello",
            Price = 0,
            Damage = 55,
            Magazine = 0,
            Reserve = 0,
            Cycle = 0.50f,            // un fendente ogni 0,5 s (era 0,6: +20%)
            ReloadTactical = 0.60f,
            ReloadEmpty = 0.60f,
            AdsTime = 0.10f,
            Melee = true,
            Silenced = true,
            NoiseMeters = 6f,
            FalloffStart = 2f,
            FalloffEnd = 3f,
            FalloffMin = 1f,
            MoveScale = 1.08f,        // l'8% più veloce
            AdsMoveScale = 1.00f,
            SpreadStill = 0f,
            SpreadMove = 0f,
            SpreadAds = 0f,
            SpreadAdsMove = 0f,
            BloomPerShot = 0f,
            BloomMax = 0f,
            BloomDecay = 1f,
            FireBuffer = 0.15f
        };

        /// <summary>
        /// Modi di fuoco e ottiche, in un posto solo (doc 11, "Le ottiche" e le schede).
        /// Le armi che ammettono più modi (Cadence, Service, Anvil) partono in AUTO.
        /// </summary>
        static Arsenal()
        {
            // pistole: mire metalliche, colpo singolo
            Config(Standard, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");
            Config(Hush, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");
            Config(Chatter, FireMode.Auto, FireMode.Auto, 1f, "mire metalliche");
            Config(Snub, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");
            Config(Hammer, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");

            // SMG: Cadence è l'unica con il selettore (semi / 3 colpi / auto)
            Config(Cadence, FireMode.Burst, FireMode.Burst, 1.2f, "punto rosso");   // raffica: tenendo premuto si susseguono
            Config(Scrap, FireMode.Auto, FireMode.Auto, 1f, "mire metalliche");
            Config(Blitz, FireMode.Auto, FireMode.Auto, 1.2f, "punto rosso");

            // pompe: un colpo per volta, mire metalliche
            Config(Choke, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");
            Config(Sweeper, FireMode.Semi, FireMode.Semi, 1f, "mire metalliche");

            // AR: Anvil e Service hanno il selettore, Vantage è solo semiauto con ottica 3×
            Config(Anvil, FireMode.Auto, FireMode.Auto, 1f, "mire metalliche");
            Config(Service, FireMode.Auto, FireMode.Auto, 1f, "mire metalliche");   // assalto full-auto
            Config(Vantage, FireMode.Semi, FireMode.Semi, 3f, "ottica 3×");

            // cecchini
            Config(Spotter, FireMode.Semi, FireMode.Semi, 4f, "ottica 4×");
            Config(Longshot, FireMode.Semi, FireMode.Semi, 6f, "ottica 6×");

            // mitragliatrice: solo auto
            Config(Heavy, FireMode.Auto, FireMode.Auto, 1f, "mire metalliche");

            Config(Melee, FireMode.Semi, FireMode.Semi, 1f, "—");
        }

        private static void Config(WeaponDef d, FireMode modes, FireMode start, float magnification, string optic)
        {
            d.Modes = modes;
            d.DefaultMode = start;
            d.Magnification = magnification;
            d.OpticName = optic;
            d.Automatic = (modes & FireMode.Auto) != 0;

            // La serie: i numeri del bloom diventano la forma della curva.
            // il rinculo nasce dal danno: un 7,62 tira su più di una 9 mm
            d.RecoilUp = 0.34f + d.Damage * 0.0165f;
            d.RecoilSide = 0.06f + d.Damage * 0.0065f;
            d.SpreadPerShot = MathF.Max(0.10f, d.BloomPerShot);
            d.SpreadPeak = MathF.Max(0.6f, d.BloomMax);
            d.SpreadRamp =
                d.Melee ? 1
                : d.Pellets > 1 ? 3
                : d.Id is "vantage" or "spotter" or "longshot" ? 3
                : d.Id == "heavy" ? 9
                : d.Id is "cadence" or "scrap" or "blitz" ? 8
                : d.IsPrimary ? 7 : 4;
            d.AccurateShots =
                d.Melee ? 0
                : d.Pellets > 1 ? 1                                  // pompe: una cartuccia e via
                : d.Id is "cadence" or "scrap" or "blitz" ? 4        // SMG: finestra più lunga ma poi sale forte
                : d.Id == "heavy" ? 5                                // LMG: parte pulita, poi si siede
                : d.Id is "vantage" or "spotter" or "longshot" ? 2   // cecchini: due colpi buoni
                : d.IsPrimary ? 3 : 2;                               // fucili 3, pistole 2
        }

        /// <summary>Tutte le armi, nell'ordine dell'enum.</summary>
        public static readonly WeaponDef[] All =
        {
            Standard, Hush, Chatter, Snub, Hammer,
            Cadence, Scrap, Blitz,
            Choke, Sweeper,
            Anvil, Service, Vantage,
            Spotter, Longshot,
            Heavy,
            Melee
        };

        public static WeaponDef Get(WeaponSlot slot)
        {
            int i = (int)slot;
            return i >= 0 && i < All.Length ? All[i] : All[0];
        }

        public static int Count => All.Length;

        /// <summary>Danno di UNA palla a una data distanza (doc 11: la fascia utile è nella scheda).</summary>
        public static int DamageAt(WeaponDef d, float meters)
        {
            if (meters <= d.FalloffStart) return d.Damage;
            if (meters >= d.FalloffEnd)
                return (int)MathF.Max(1f, d.Damage * d.FalloffMin);
            float t = (meters - d.FalloffStart) / (d.FalloffEnd - d.FalloffStart);
            return (int)MathF.Round(d.Damage * (1f - (1f - d.FalloffMin) * t));
        }

        /// <summary>Danno di una cartuccia intera: per le pompe sono tutte le palle.</summary>
        public static int ShotDamageAt(WeaponDef d, float meters)
            => d.Pellets * DamageAt(d, meters);

        /// <summary>
        /// Il cono di dispersione, in gradi, in questo istante. Un'unica formula per
        /// client, server e test: fermo è stretto, camminando si apre, in mira si
        /// richiude, accovacciato un filo meno, in aria molto di più. Il bloom dei
        /// colpi si somma e cala da solo a ogni tick.
        /// </summary>
        public static float SpreadAt(WeaponDef d, float speed, bool onFloor, bool crouching, float adsT, int shots)
        {
            if (d.Melee) return 0f;
            float move = Math.Clamp(speed / Constants.WalkSpeed, 0f, 1f);
            float stood = Lerp(d.SpreadStill, d.SpreadMove, move);
            float aimed = Lerp(d.SpreadAds, d.SpreadAdsMove, move);
            float s = Lerp(stood, aimed, Math.Clamp(adsT, 0f, 1f));
            if (crouching) s *= 0.80f;
            if (!onFloor) s *= 1.80f;          // in aria il cono si apre: niente tiri al volo
            return s + SeriesSpread(d, shots);
        }

        /// <summary>
        /// Quanto apre la serie: zero per i primi <see cref="WeaponDef.AccurateShots"/>
        /// colpi, poi cresce in modo convesso fino a <see cref="WeaponDef.SpreadPeak"/>.
        /// È la stessa curva per client, server e test: il primo colpo è sempre il più
        /// preciso, il quarto/quinto è dove il rinculo "morde" (CS/Valorant).
        /// </summary>
        public static float SeriesSpread(WeaponDef d, int shots)
        {
            int k = shots - d.AccurateShots;
            if (k <= 0) return 0f;
            float t = MathF.Min(1f, k / MathF.Max(1f, d.SpreadRamp));
            return d.SpreadPeak * MathF.Pow(t, d.SpreadCurve);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Direzione di mira da yaw/pitch (stessa convenzione della simulazione: -Z è avanti).</summary>
        public static Vec3 AimForward(float yaw, float pitch)
        {
            float sy = MathF.Sin(yaw), cy = MathF.Cos(yaw);
            float sp = MathF.Sin(pitch), cp = MathF.Cos(pitch);
            return new Vec3(-sy * cp, sp, -cy * cp);
        }

        /// <summary>
        /// Direzione effettiva di un colpo: mira più uno scostamento dentro il cono.
        /// È deterministica nel seme, quindi client e server possono ricalcolarla e
        /// ottenere la stessa traiettoria (server autoritativo, client che disegna).
        /// </summary>
        public static Vec3 ShotDirection(float yaw, float pitch, float spreadDeg, int seed)
        {
            var forward = AimForward(yaw, pitch);
            if (spreadDeg <= 0.0001f) return forward;

            float sy = MathF.Sin(yaw), cy = MathF.Cos(yaw);
            var right = new Vec3(cy, 0f, -sy);
            var up = Vec3.Cross(right, forward);

            float r = MathF.Sqrt(Noise(seed, 1)) * MathF.Tan(spreadDeg * MathF.PI / 180f);
            float a = Noise(seed, 2) * TwoPi;
            var dir = forward + right * (r * MathF.Cos(a)) + up * (r * MathF.Sin(a));
            return Vec3.Normalize(dir);
        }

        /// <summary>
        /// Direzione della palla <paramref name="pellet"/> di un colpo di pompa. Il seme
        /// del colpo resta quello della simulazione: chi ricalcola il colpo ottiene le
        /// stesse dodici traiettorie, nello stesso ordine.
        /// </summary>
        public static Vec3 PelletDirection(float yaw, float pitch, float spreadDeg, int shotSeed, int pellet, int pellets)
        {
            if (pellets <= 1) return ShotDirection(yaw, pitch, spreadDeg, shotSeed);
            int seed = unchecked(shotSeed * 7919 + pellet * 104729 + 17);
            return ShotDirection(yaw, pitch, spreadDeg, seed);
        }

        /// <summary>Numero stabile in [0,1): il seme del colpo decide lo scostamento.</summary>
        public static float Noise(int seed, int salt)
        {
            uint x = unchecked((uint)(seed * 747796405 + salt * 2891336453));
            x ^= x >> 16; x *= 0x7feb352d;
            x ^= x >> 15; x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0xFFFFFF) / 16777216f;
        }
    }
}
