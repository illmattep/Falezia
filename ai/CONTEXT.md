# CONTEXT · FALEZIA in una pagina

> Tattico **5v5** in prima persona · **estrazione** · **round senza respawn** · Godot 4.7 .NET · server dedicati.
> Ambientazione: **Calanera**, città costiera italiana immaginaria.

---

## Il round

Due squadre partono dai lati opposti (Nord e Sud) e corrono verso una **cassa al centro**.
Chi apre la cassa prende il **Nucleo** e diventa il **difensore**; l'altra squadra assalta.
Chi estrae vince il round. **Primo a 5 estrazioni.**

| # | Fase | Durata |
|---|---|---|
| 0 | Pianificazione (loadout pubblico + politica di spesa) | una volta |
| 1 | **Buy Phase** | max 40 s |
| 2 | Approccio | 0–30 s |
| 3 | Contesa della cassa | 0:30–1:30 |
| 4 | Apertura: **15 s** a mano · **10 s** col Kit · **15 s** col Robot | |
| 5 | **Prelievo** (beat pubblico, destinazione segreta) | 3 s |
| 6 | Trasporto: centro (veloce) o anello (sicuro) | ~25 s |
| 7 | Avvio (faro di fumo + sirena) | 5 s |
| 8 | **Assedio** | **120 s** (90 con Overclock) |

**Fine round:** estrazione · team wipe · Nucleo rimosso · timer 8:00 senza estrazione = round nullo.
**Il timer del round si sospende durante un'estrazione attiva.**

---

## Numeri che contano

| | |
|---|---|
| Salute | **100** |
| Piastra leggera | **+20** effettivi, **$250**, fascia libera |
| Piastra pesante | **+40** effettivi, **$500**, richiede approvazione |
| Testa | **mai protetta**, moltiplicatore **×4** |
| DBNO | **30 s** di dissanguamento, **5 s** di rianimazione, niente auto-rianimazione |
| Rianimazione | 15 s a mani nude · 10 s con Rianimatore **o** Medikit · **5 s** con entrambi |
| Movimento | passo **4,2** · sprint **6,5** · crouch **2,2** · ADS **55%** |
| Stamina | 10 s di sprint, 8 s di recupero |
| Gravità / salto | 18 m/s² · 6,0 m/s (**~1 m**) |
| Tick | **64 Hz**, snapshot **32 Hz** |
| Acquisto libero | **≤ $300**, senza approvazione |

## Economia

Fondo personale **$1.000** · cassa iniziale **$5.000** · vittoria **$3.000** ·
sconfitta **$2.000 → $2.400 → $2.800 → $3.200** (progressiva) · round nullo **$1.200** ·
pavimento anti-spirale: cassa < $1.000 → **+$1.500**.

> Un round in ECO costa **$850** (Scrap $600 + piastra $250): ci stai anche con la cassa vuota.

## Loadout

Massimo **5 loadout**, scelti durante il caricamento, modificabili fino al via del round 1, poi **bloccati**.
Contengono: pistola · **1 specializzazione** · **1 consumabile fisso**.
Slot consumabili: **3** totali (1 dal loadout, gratis ogni round; 2 comprati).

| Specializzazione | Effetto |
|---|---|
| **Decifratore** | Cassa in 7,5 s (con Kit: 5 s) |
| **Rianimatore** | Rianima in 10 s (con Medikit: 5 s) |
| **Portatore** | Nucleo a 4,8 m/s, niente stamina |
| **Ingegnere** | Robot piazzato in 1,5 s, apre in 10 s, riparabile |
| **Ricognitore** | Sensori +40% raggio, ping 4 s, endoscopio silenzioso |

## Arsenale — 15 armi + coltello

| Classe | Armi (prezzo) |
|---|---|
| Pistole | Standard (gratis) · Hush ($400, silenziata) · Snub ($500, ADS 0,16) · Hammer ($700) · Chatter ($600) |
| SMG | Cadence ($900) · Blitz ($1.000) · Scrap ($600, ECO) |
| Pompe | Choke ($1.100, 1 colpo) · Sweeper ($1.400) |
| AR | Anvil ($1.400, 30 dmg) · Service ($1.400) · Vantage ($1.800, semiauto) |
| Cecchini | Spotter ($1.800) · Longshot ($2.400, **oneshot**) |

## Mappa — Calanera, 240 × 220 m

| Ruolo | Zona |
|---|---|
| Spawn Nord | **Porto Vecchio** |
| Spawn Sud | **Forte San Verano** |
| Centro (cassa) | **Piazza Vecchia** |
| Estrazione Est | **Grotta del Levante** — chiusa, un ingresso, fianco marino |
| Estrazione Ovest | **Galleria Vecchia** — lunga, ingressi multipli |
| Anello | Via Stretta · Scalinata Lunga · Lungomare · Mura |

Spawn a **±120 m** dal centro, estrazioni a **±95 m**. Passo: 28,6 s spawn→centro; 22,6 s centro→estrazione.

## Porte

Tre stati: **chiusa · socchiusa · aperta**. Aprire piano 0,6 s (8 m di rumore) · normalmente 1,5 s
(15 m) · **a calcio 0,3 s (35 m)**. La **socchiusa** fa vedere e sparare ma non passare: è il verbo
centrale dell'ingresso. **Nessuno sfondamento, nessun cuneo.**

## Le tre build

| Build | Come |
|---|---|
| **Server** | headless, console con comandi (`status`, `players`, `kick`, `say`, `tick`, `quit`) |
| **Gioco** | menu principale con le voci incomplete dichiarate |
| **Stanza debug** | ogni aspetto tecnico e ogni scenario, senza passare dal gioco |

## Documenti da leggere per approfondire

`docs/00-panoramica.md` (mappa generale) · `03` (round) · `04` (economia) · `05` (danni e movimento) ·
`11` (armi) · `08` (netcode) · `13` (piano di sviluppo).
