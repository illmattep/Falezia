# CODE · architettura, convenzioni, ricette

---

## La mappa del repository

```
falezia/
├── docs/              15 documenti di design, arte e produzione (in italiano)
├── ai/                questo pacchetto: contesto condensato per agenti
├── game/              progetto Godot 4.7 .NET
│   ├── project.godot
│   ├── Falezia.csproj · Falezia.sln
│   ├── sim/           ★ SIMULAZIONE PURA — zero dipendenze dal motore
│   │   ├── Constants.cs     tutti i numeri di design
│   │   ├── State.cs         InputCmd · PlayerState · Box · WorldState (+ griglia) · SimState
│   │   ├── Sim.cs           Simulation.Spawn / Step / ApplyInput (gradini, aggancio al suolo)
│   │   └── MapLoader.cs     JSON della mappa → WorldState (riceve testo, non percorsi)
│   ├── maps/
│   │   └── calanera.json    GENERATO da tools/mapgen — non si modifica a mano
│   ├── common/        lato motore, condiviso da client e server
│   │   └── MapFiles.cs      res:// → MapLoader, spawn per squadra
│   ├── net/           ★ protocollo binario, puro anch'esso
│   │   └── Protocol.cs
│   ├── server/        server headless + console
│   │   ├── ServerMain.cs
│   │   └── server.tscn
│   └── client/
│       ├── MainMenu.cs · main_menu.tscn
│       ├── ClientMain.cs · client.tscn
│       ├── DebugRoom.cs · debug_room.tscn
│       ├── MapView.cs       Atmosphere (sole, cielo, foschia) + mesh graybox fuse per settore
│       ├── GraphicsSettings.cs · SettingsPanel.cs · LatencyGovernor.cs
│       ├── Crosshair.cs · Perf.cs · InputConfig.cs
├── tools/mapgen/      calanera.py (genera + verifica la mappa) · simport.py (porting Python del movimento)
└── tests/             test harness .NET puro — IL CANCELLO ARCHITETTURALE
    ├── Falezia.Tests.csproj
    └── Program.cs
```

★ = **non possono contenere `using Godot;`**

> **La copia dentro `Assets/Scripts/Sim|Net`.** Unity non compila file fuori da `Assets/`, quindi la
> simulazione esiste in due posti: quella vera in `game/sim|net` e una **copia** in `Assets/Scripts/`.
> `tools/unity/sync-core.sh` le allinea, e lo fanno da soli `./run.sh unity` e `./run.sh build`.
> Se apri l'editor Unity a mano, lancia prima `./run.sh sync`: due copie che si allontanano sono un
> bug silenzioso (è già successo — il client girava con cadenze vecchie).

---

## Il cancello architetturale

`tests/Falezia.Tests.csproj` compila **solo** `game/sim/**` e `game/net/**` con .NET puro.

```xml
<Compile Include="../game/sim/**/*.cs" />
<Compile Include="../game/net/**/*.cs" />
```

> **Se qualcuno mette una dipendenza dal motore nella simulazione, il progetto di test non compila più.**
> Il confine non è una linea guida: è un errore di build. **Non aggirarlo.**

Per questo la simulazione usa `System.Numerics.Vector3` e non `Godot.Vector3`. La conversione avviene
**solo** nei file di `client/` e `server/`, che sono gli unici che vedono il motore.

---

## Tre livelli, tre responsabilità

| Livello | Chi decide | Cosa NON deve fare |
|---|---|---|
| **Simulazione** (`sim/`) | Le regole del movimento e dello stato | Conoscere il tempo reale, la rete o il rendering |
| **Rete** (`net/`) | Il formato dei byte | Interpretare il gioco |
| **Client / Server** (`client/`, `server/`) | I/O, rendering, autorità | Duplicare la logica di gioco |

**Il server è autorevole. Il client predice.** Il client manda `InputCmd`; il server manda `Snapshot`
con l'ack degli input di *quel* client. La riconciliazione riapplica gli input non confermati — è
possibile solo perché la simulazione è deterministica.

---

## Convenzioni

| Ambito | Regola |
|---|---|
| **Lingua del codice** | Identificatori in **inglese**: `Constants.WalkSpeed`, `PlayerState.OnFloor` |
| **Lingua dei commenti** | **Italiano**, e spiegano *perché*, non *cosa* |
| **Lingua dei documenti** | Italiano |
| **Namespace** | `Falezia.Sim`, `Falezia.Net`, `Falezia.Tests`. I nodi Godot stanno nel namespace globale |
| **Script Godot** | `public partial class Nome : TipoNodo`, nome del file = nome della classe |
| **Costanti** | Mai numeri magici: tutto in `sim/Constants.cs` e in `ai/DATA.json` |
| **Commit** | `area: cosa` in italiano — es. `sim: correggi la risoluzione verticale` |

---

## Ricette

### Aggiungere un'arma

1. I numeri nel **doc 11** (danno, colpi, cadenza, caricatore, ADS, prezzo, ruolo)
2. Gli stessi numeri in **`ai/DATA.json`**
3. Se ha un mirino diverso: un profilo in `Crosshair.For()`
4. Una riga nel **dump armi** della stanza debug
5. Il prezzo nel **listino** del doc 04
6. Il **carattere sonoro** in `DESIGN` dentro `tools/audio/make_weapon_sounds.py`, poi `./run.sh sound`
   (se manca la voce il generatore si ferma: nessuna arma muta per distrazione)
7. `./run.sh test && ./run.sh build`, e `-faleziaAudioProbe` per vedere che i suoni si caricano

### Aggiungere un passo al flusso di partita (matchmaking → brief → round)

1. Lo stato che vale per tutti sta in `Match`/`MatchState` (fasi, timer, draft).
2. Le decisioni del server (chi entra in coda, quando la lobby è piena) in `GameServer`;
   il client riceve i numeri nello snapshot e **non li ricalcola**.
3. La UI della coda è `MatchFlow`, il brief è `DraftScreen`: due pannelli separati perché
   il secondo è già del draft.
4. Test del brief: `tests/Program.cs` → `MatchBrief` (durata, chiusura anticipata, scadenza).

### Rigging e animazione di un personaggio

1. La scansione (statica) entra in `tools/character/rig_operator.py`: decimazione, scheletro,
   pesi a mano, texture ridotte, export in `Assets/StreamingAssets/characters/`.
2. L'animazione è **procedurale** (`OperatorView`), guidata dallo stato della simulazione:
   niente clip da importare, e ogni arma ha la sua durata di ricarica.
3. Controllo qualità senza partita: `-faleziaOperatorProbe -faleziaSkipMenu` (tre manichini +
   screenshot frontale, laterale e in terza persona). In gioco: **Alt** = terza persona con camera
   rotante, per guardare il corpo e le animazioni.
4. Gambe e busto si ruotano ad angoli, le braccia si **puntano** a bersagli: con le ossa
   importate da Blender gli assi locali non si indovinano.

### Aggiungere un sistema di gioco

1. Prima **leggi `ai/RULES.md`**: se il sistema viola un invariante, fermati e chiedi
2. Lo stato va in `sim/State.cs`; il comportamento in `sim/Sim.cs` come funzione pura
3. I numeri in `sim/Constants.cs`
4. Un test in `tests/Program.cs` — **prima** dell'implementazione, se possibile
5. La sincronizzazione in `net/Protocol.cs` (solo se lo stato deve viaggiare)
6. Aggiorna `ai/STATE.md`

### Aggiungere o cambiare una zona della mappa

1. Doc 02: ruolo, dimensioni, accessi, tempi di percorrenza **misurati**
2. `tools/mapgen/calanera.py`: si disegna nella **metà Nord** (`north_half`), che viene ruotata;
   piazza (`center`, disegnata a Est e ruotata) ed estrazioni hanno le loro funzioni. Scale vere:
   alzata 17 cm, pedata 30 cm (`stairs`). Schema: tre corsie corte per lato, niente corridoi lunghi
3. `./run.sh map` → deve dire **ESITO: OK** (rotte, anello, equità, tempi) — guarda l'anteprima PNG.
   Le rotte trovate vengono scritte nel JSON (`routes`)
4. `./run.sh test` → ripercorre le rotte del JSON con la simulazione C#: **i test non contengono
   coordinate**, non vanno aggiornati a mano quando cambia la mappa
5. Se la zona ha un nome: aggiungila a `points` (teleport della stanza debug) e al glossario
6. Colori: solo i `kinds` della palette del doc 07. Il saturo è riservato all'informazione di gioco

> **Il movimento ha un porting in Python** (`tools/mapgen/simport.py`) per provare la mappa senza
> compilare. Se cambi `Sim.cs`, aggiorna anche quello: la verità resta `Sim.cs`.

### Aggiungere un comando alla console del server

In `ServerMain.DrainCommands()`, uno `case` nel `switch` + la riga nell'`help`. Log con la categoria
giusta (`BOOT`, `NET`, `SIM`, `STAT`, `CMD`, `ERR`, `SAY`).

---

## Come si testa

```bash
./run.sh test     # 193 test, pochi secondi, SENZA motore — è il ciclo veloce
./run.sh sound    # rigenera i suoni delle armi (Python 3 + numpy)
./run.sh sync     # allinea la copia del core dentro Assets/ (lo fa già unity/build)
./run.sh map      # rigenera e verifica la mappa (Python 3 + Pillow)
./run.sh build    # compila il progetto Godot (0 errori, 0 avvisi)
./run.sh server   # server headless con console
./run.sh game     # menu
./run.sh debug    # stanza debug
./run.sh editor   # editor Godot
```

**Requisiti già installati su questa macchina:** Godot 4.7.2 **.NET** in `~/opt/godot-mono`,
.NET 8 SDK in `~/.dotnet`. Nessuno dei due richiede root. `run.sh` mette `~/.dotnet` nel `PATH`:
**se lanci Godot senza `run.sh`, il build C# non trova il compilatore.**

---

## Errori già fatti una volta (non rifarli)

| Errore | Lezione |
|---|---|
| `:=` su un valore non tipizzato in GDScript | Il progetto ora è in **C#** — ma la lezione resta: i tipi espliciti |
| Direzione di risoluzione verticale invertita | Decidere "pavimento o soffitto" dalla **direzione del movimento**, mai dal centro della scatola |
| `Size` di un `Control` figlio di `CanvasLayer` = 0 | Per centrare qualcosa: **`GetViewportRect().Size * 0.5f`**, non `Size` |
| Colori sbagliati di un fattore "al quadrato" | Compatibility illumina in **sRGB**, Forward+ in **lineare**: negli shader custom passa sRGB e converti con `#if CURRENT_RENDERER` |
| Giocatore spinto di lato dal gradino su cui poggia | Virgola mobile: 0,165 + 0,9 − 0,9 ≠ 0,165. Per questo `Simulation.ContactSkin` |
| `FileAccess` / `Environment` ambigui | `System.IO` e `System` sono importati di default: scrivi `Godot.FileAccess`, `Godot.Environment` |
| `IsPhysicalKeyPressed` per Ctrl/Shift | I **modificatori non hanno una posizione** sulla tastiera: usa `IsKeyPressed` |
| Posizioni assolute per la UI | **Container sempre**: `VBoxContainer` + `ScrollContainer`, mai `Position = (12, 540)` |
