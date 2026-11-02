# 00 · Panoramica

> **Un documento, dieci minuti.** Se leggi solo questo, sai cosa stiamo costruendo e come.
> Tutto il resto è dettaglio: gli altri documenti approfondiscono, questo riassume.

---

## 1 · Il gioco in cinque righe

**FALEZIA** è un tattico 5v5 in prima persona, a round, senza respawn, ambientato in una città
costiera italiana immaginaria.

Due squadre partono dai lati opposti della mappa e corrono verso una cassa al centro: chi la apre
prende il **Nucleo**, e da quel momento diventa il **difensore** di un punto di estrazione. L'altra
squadra ha **120 secondi** per organizzare un assalto e portarglielo via. Chi estrae vince il round.
**Primo a 5 estrazioni.**

Non ci sono eroi, non ci sono classi, non ci sono abilità magiche. Ci sono cinque **specializzazioni**
condivise, una mappa con un anello, un'economia di squadra con la cassa comune, e la regola che ogni
morte deve essere meritata.

> **Perché si scrive con la Z.** A Porto Vecchio, sopra l'imbocco del molo, c'è un'insegna sbiadita:
> **FALEZIA**. È il nome antico del promontorio, come lo scrivevano prima. La Z non è un refuso:
> è una data.

---

## 2 · Il round, in ordine

| # | Fase | Durata | Cosa succede |
|---|---|---|---|
| 0 | **Pianificazione** | una volta | Si sceglie il loadout (pubblico) e la politica di spesa |
| 1 | **Buy Phase** | max 40 s | Cassa comune, acquisti, approvazioni, Lockdown / Overclock |
| 2 | **Approccio** | 0–30 s | Sprint (veloce, rumoroso) o passo tattico (lento, silenzioso) |
| 3 | **Contesa** | 0:30–1:30 | Il 5v5 in Piazza Vecchia: chi vince controlla la cassa |
| 4 | **Apertura** | 15 s / 10 s / 15 s | A mano, col Kit Decodifica, o col Robot (che non trasporta) |
| 5 | **Prelievo** | 3 s | **Beat pubblico:** «NUCLEO PRELEVATO». La destinazione resta segreta |
| 6 | **Trasporto** | ~25 s | Centro (veloce, esposto) o anello (lento, sicuro). Si può fingere |
| 7 | **Avvio** | 5 s | Faro di fumo + sirena: tutti sanno dove. Il Lockdown chiude la finestra |
| 8 | **Assedio** | **120 s** (90 con Overclock) | Il momento Ready or Not: assalto, specchio, flash, pulizia |
| — | **Fine round** | — | Estrazione · team wipe · rimozione del Nucleo · round nullo |

**Fondamentale:** il timer del round (8:00) si **sospende** durante un'estrazione attiva. Un round
non muore a metà assedio.

---

## 3 · Le dieci regole che definiscono il gioco

1. **Nessun respawn.** Ma esiste lo **stato abbattuto**: 30 s di dissanguamento, 5 s di rianimazione,
   nessuna auto-rianimazione. È ciò che rende possibile lo **scambio** (trade).
2. **L'attacco/difesa è emergente.** Non è assegnato: lo decide chi prende il Nucleo.
3. **La cassa è di tutti.** Spese sotto $300 libere, sopra serve **1 click** di un compagno
   (o 5 s di silenzio). Il rendiconto è pubblico.
4. **L'attrezzatura è tua finché sei vivo.** Se muori, ricompri. Il kit permanente non si perde mai.
5. **Si vende solo il presente.** Ciò che compri in questa Buy Phase si annulla; ciò che conservi
   si regala o si scambia — **mai** si monetizza. È l'anti-stampa-soldi.
6. **L'informazione è la risorsa più preziosa.** Il prelievo è pubblico, la destinazione no.
   I sensori danno un **ping**, mai un marcatore. Anche il loadout è pubblico.
7. **Ogni morte deve essere meritata.** Ogni trappola letale ha un tell. Nessun muro invisibile.
8. **Niente sfondamento, niente cuneo.** Le porte si aprono e si chiudono, e niente le blocca:
   **il gioco non ha meccaniche di negazione del passaggio**, solo di rallentamento e di allarme.
9. **Le trappole tolgono tempo e fanno rumore.** Non uccidono senza preavviso.
10. **Zero pay-to-win.** Armi, ottiche, attachment e specializzazioni sono tutti disponibili dal
    livello 1. Lo shop vende **solo estetica**.

---

## 4 · L'economia in una tabella

| Voce | Valore |
|---|---|
| Fondo personale pre-match | **$1.000** |
| Cassa comune iniziale | **$5.000** |
| Vittoria round | **$3.000** |
| Sconfitta (progressiva) | **$2.000 → $2.400 → $2.800 → $3.200** |
| Soglia acquisto libero | **$300** |
| Pavimento anti-spirale | se la cassa è sotto $1.000, **+$1.500** |

La curva della sconfitta è il motore delle rimonte: **chi perde 3 round riceve più di chi vince.**
Un round in ECO costa **$850** (Scrap $600 + piastra $250): ci stai anche con la cassa vuota.

---

## 5 · Loadout e specializzazioni

Un loadout contiene **tre cose**: pistola, **una specializzazione**, **un consumabile fisso**.
Massimo **5 loadout**, scelti durante il caricamento (modificabili fino al via del round 1), poi
**bloccati per tutta la partita**, e **pubblici**: l'avversario li vede.

| Specializzazione | Effetto | Il costo è l'opportunità |
|---|---|---|
| **Decifratore** | Cassa in **7,5 s** (15 s base); con Kit **5 s** | Rinunci alle altre quattro |
| **Rianimatore** | Rianima in **10 s** (15 s base); con Medikit **5 s** | idem |
| **Portatore** | Nucleo a **4,8 m/s**, niente consumo di stamina | idem |
| **Ingegnere** | Robot piazzato in **1,5 s**, apre in **10 s**, riparabile | idem |
| **Ricognitore** | Sensori **+40% raggio**, ping 4 s, endoscopio silenzioso | idem |

Tre slot consumabili: **1 dal loadout** (gratis ogni round), **2 comprati** in Buy Phase.

---

## 6 · L'arsenale

**15 armi + coltello.** Tutte disponibili dal livello 1, tutte comprate con il denaro di partita.

| Classe | Armi |
|---|---|
| Pistole | **Standard** (gratis) · **Hush** ($400, silenziata) · **Snub** ($500, ADS 0,16 s) · **Hammer** ($700, 2 colpi) · **Chatter** ($600, full-auto) |
| SMG | **Cadence** ($900) · **Blitz** ($1.000) · **Scrap** ($600, da ECO) |
| Pompe | **Choke** ($1.100, 1 colpo) · **Sweeper** ($1.400, semiauto) |
| AR | **Anvil** ($1.400, 30 dmg) · **Service** ($1.400, standard) · **Vantage** ($1.800, semiauto) |
| Cecchini | **Spotter** ($1.800, 2 colpi) · **Longshot** ($2.400, **1 colpo**) |

**Piastre a punti effettivi:** +20 (leggera, $250) / +40 (pesante, $500). Testa mai protetta, ×4.
**L'unico attachment è l'ottica** — e non tocca il rinculo: *un'arma ha un carattere, e l'ottica non
lo cambia.*

---

## 7 · La mappa

**Calanera**, 180 × 200 m (rivista a ottobre 2026: tre corsie per lato, vedi doc 02). **Una croce con un anello**, non una croce: ogni spawn deve poter
raggiungere ogni estrazione con **almeno due rotte**, una delle quali non passa dal centro.

| Ruolo | Zona |
|---|---|
| Spawn Nord | **Porto Vecchio** |
| Spawn Sud | **Forte San Verano** |
| Centro (la cassa) | **Piazza Vecchia** |
| Estrazione Est | **Grotta del Levante** — chiusa, un ingresso, fianco marino |
| Estrazione Ovest | **Galleria Vecchia** — lunga, ingressi multipli |

**Regola d'oro:** simmetria architettonica, asimmetria cosmetica. Si costruisce mezzo paese e si ruota.

---

## 8 · L'architettura tecnica

### Il principio

> **La simulazione è pura. Il motore è solo la vista.**

```
                 ┌──────────────────────────────────────────┐
                 │        SIMULAZIONE (C#, pura)            │
                 │                                          │
                 │  Simulation.Step(state, inputs, world)   │
                 │                                          │
                 │  · passo fisso 1/64                      │
                 │  · zero dipendenze da Godot              │
                 │  · System.Numerics.Vector3               │
                 │  · deterministica                        │
                 └──────────────────────────────────────────┘
                        ▲                              ▲
              ┌─────────┴─────────┐          ┌─────────┴─────────┐
              │      CLIENT       │          │      SERVER       │
              │  predice + rende  │          │  autorità 64 Hz   │
              │  Godot + UDP      │          │  headless + UDP   │
              └───────────────────┘          └───────────────────┘
```

**Il confine è verificato dal compilatore, non dalla buona volontà.**
`tests/Falezia.Tests.csproj` compila **solo** `game/sim/` e `game/net/` con .NET puro.
Se qualcuno scrive `using Godot;` in un file della simulazione, **il progetto di test non compila più.**
Il cancello architetturale è un errore di build, non una linea guida.

### Il ciclo del client

```
1. Campiona l'input                      (64 Hz, accumulatore — non il frame rate)
2. Simulation.ApplyInput(...)            → PREDIZIONE LOCALE (zero latenza percepita)
3. Invia l'input con un tick             → UDP, inaffidabile
4. Riceve lo snapshot del server         → contiene l'ack dei MIEI input
5. Confronta la mia posizione a quel tick con quella del server
6. Se l'errore > 1 cm → copia lo stato del server e RIGIOCA gli input non confermati
```

### Il ciclo del server

```
per ogni tick (64 Hz):
    raccogli gli input → Simulation.Step → snapshot per OGNI client (con il suo ack)
```

### Protocollo

| Pacchetto | Dimensione | Contenuto |
|---|---|---|
| **Input** | **22 byte** | tick, move, yaw, pitch, 4 flag |
| **Snapshot** | **~34 B per giocatore** | pos, vel, yaw, pitch, stamina, jump_cd, flag |

Canali **inaffidabili e non ordinati**, come dev'essere per un FPS. La riconciliazione tollera la
perdita di pacchetti perché ogni input viene riapplicato dal client.

### Perché C#

La simulazione gira **due volte per tick** (predizione + autorità) più il replay della
riconciliazione: 10 giocatori × 64 Hz × 2. In GDScript sarebbe al limite, e si finirebbe in
GDExtension C++. In C# è comodo, e dà accesso all'ecosistema .NET di rete quando servirà
(LiteNetLib) invece di restare legati al netcode del motore.

---

## 9 · Le tre build

| Build | Come si avvia | Cosa fa |
|---|---|---|
| **Server** | `godot --headless` su `server/server.tscn` | Tick a 64 Hz, console con comandi, log categorizzati |
| **Gioco** | scena principale | Menu principale con le voci incomplete **dichiarate** |
| **Stanza Debug** | dal menu, o `client/debug_room.tscn` | Ogni aspetto tecnico e ogni scenario, senza passare dal gioco |

### La console del server

```
[17:39:12.441] [BOOT ] FALEZIA — server dedicato M0
[17:39:12.442] [BOOT ] porta UDP 27015 · tick 64 Hz · snapshot 32 Hz
[17:39:12.443] [BOOT ] comandi: status · players · kick <id> · help · quit
[17:39:31.007] [NET  ] JOIN  127.0.0.1:41222  ->  id 1  (1 connessi)
[17:39:36.012] [STAT ] tick 384      client 1   pkt 192      bad 0     up 5s
```

Comandi: `status` · `players` · `kick <id>` · `say <testo>` · `tick` · `quit`.
Argomenti: `--port=27015` · `--quiet` · `--verbose`.

### La stanza debug

Tutto ciò che è tecnico, in un posto solo:
- **Ispettore di stato** live: tick, posizione, velocità, `on_floor`, accovacciato, stamina
- **Teleport** sui punti di prova (piazza, cassa, torre, muro, gradino)
- **Latenza e perdita simulate**: 0 / 50 / 100 / 200 ms, 0 / 2 / 10 %
- **Scenari**: spawn bot, reset, dump dei dati delle 15 armi
- Le sezioni **IN COSTRUZIONE** sono marcate come tali: economia, round, armi, anti-cheat

---

## 10 · Stato del codice, oggi

| | |
|---|---|
| **Test della simulazione** | ✅ **26 / 26** — gravità, passo/sprint/crouch, salto, assenza di air-strafe, muri, atterraggio su cassa, determinismo, protocollo ×2, cancello architetturale |
| **Build Godot C#** | ✅ **0 errori, 0 avvisi** |
| **Server** | ✅ headless, console, join/timeout/kick, tick fisso |
| **Client** | ✅ predizione, riconciliazione, netgraph |
| **Menu** | ✅ con stato WIP visibile |
| **Stanza debug** | ✅ teleport, ispettore, latenza/loss, bot, dump armi |
| **Sparo** | ❌ non ancora |
| **Round ed economia** | ❌ non ancora |
| **Lag compensation** | ❌ non ancora |
| **Arte** | ❌ zero, per scelta |

**Il passo successivo:** far parlare i due processi end-to-end (server headless + client) e
misurare la riconciliazione con 200 ms di ping e 2% di perdita pacchetti. È il **Gate G1** del
piano di sviluppo.

---

## 11 · Come si esegue

```bash
cd falezia

./run.sh test      # test della simulazione, senza Godot (~2 s)
./run.sh build     # compila il progetto Godot C#
./run.sh server    # server dedicato headless, con console
./run.sh game      # menu principale
./run.sh debug     # stanza debug
```

**Requisiti installati:** Godot 4.7.2 **.NET** (in `~/opt/godot-mono`), .NET 8 SDK (in `~/.dotnet`).
Nessuno dei due richiede root.

---

## 12 · La mappa dei documenti

| Se ti interessa… | Leggi |
|---|---|
| Perché il gioco esiste, i pilastri | **01 · Visione e pilastri** |
| La mappa, le rotte, i tempi | **02 · La mappa** |
| Il round, fase per fase | **03 · Svolgimento del round** |
| I soldi, le approvazioni, i prezzi | **04 · Economia e acquisti** |
| Danni, movimento, rumore, porte, trappole | **05 · Combattimento e movimento** |
| HUD, menu, accessibilità | **06 · UX e interfaccia** |
| Arte e budget GPU | **07 · Artstyle e performance** |
| Netcode, server, anti-cheat | **08 · Netcode e server** |
| Rischi e cosa tagliare | **09 · Roadmap e rischi** |
| Loadout e specializzazioni | **10 · Loadout e progressione** |
| Le 15 armi, numeri e ruolo | **11 · Armi** |
| Come si modella e si rende un'arma | **12 · Brief artistico armi** |
| Quanto costa, in quanto tempo | **13 · Piano di sviluppo** |
| Account e backend | **14 · Account e backend** |
