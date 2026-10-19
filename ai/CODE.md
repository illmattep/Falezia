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
│   │   ├── State.cs         InputCmd · PlayerState · Box · WorldState · SimState
│   │   └── Sim.cs           Simulation.Spawn / Step / ApplyInput
│   ├── net/           ★ protocollo binario, puro anch'esso
│   │   └── Protocol.cs
│   ├── server/        server headless + console
│   │   ├── ServerMain.cs
│   │   └── server.tscn
│   └── client/
│       ├── MainMenu.cs · main_menu.tscn
│       ├── ClientMain.cs · client.tscn
│       ├── DebugRoom.cs · debug_room.tscn
│       ├── Crosshair.cs · Perf.cs · InputConfig.cs
└── tests/             test harness .NET puro — IL CANCELLO ARCHITETTURALE
    ├── Falezia.Tests.csproj
    └── Program.cs
```

★ = **non possono contenere `using Godot;`**

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
6. `./run.sh test && ./run.sh build`

### Aggiungere un sistema di gioco

1. Prima **leggi `ai/RULES.md`**: se il sistema viola un invariante, fermati e chiedi
2. Lo stato va in `sim/State.cs`; il comportamento in `sim/Sim.cs` come funzione pura
3. I numeri in `sim/Constants.cs`
4. Un test in `tests/Program.cs` — **prima** dell'implementazione, se possibile
5. La sincronizzazione in `net/Protocol.cs` (solo se lo stato deve viaggiare)
6. Aggiorna `ai/STATE.md`

### Aggiungere una zona della mappa

1. Doc 02: ruolo, dimensioni, accessi, tempi di percorrenza **misurati**
2. `WorldState.MakeTestWorld()`: le scatole corrispondenti
3. Verifica: **≥2 rotte** verso ogni estrazione, una fuori dal centro
4. Materiali dalla palette del doc 07

### Aggiungere un comando alla console del server

In `ServerMain.DrainCommands()`, uno `case` nel `switch` + la riga nell'`help`. Log con la categoria
giusta (`BOOT`, `NET`, `SIM`, `STAT`, `CMD`, `ERR`, `SAY`).

---

## Come si testa

```bash
./run.sh test     # 26 test, ~2 s, SENZA Godot — è il ciclo veloce
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

## Cinque errori già fatti una volta (non rifarli)

| Errore | Lezione |
|---|---|
| `:=` su un valore non tipizzato in GDScript | Il progetto ora è in **C#** — ma la lezione resta: i tipi espliciti |
| Direzione di risoluzione verticale invertita | Decidere "pavimento o soffitto" dalla **direzione del movimento**, mai dal centro della scatola |
| `Size` di un `Control` figlio di `CanvasLayer` = 0 | Per centrare qualcosa: **`GetViewportRect().Size * 0.5f`**, non `Size` |
| `IsPhysicalKeyPressed` per Ctrl/Shift | I **modificatori non hanno una posizione** sulla tastiera: usa `IsKeyPressed` |
| Posizioni assolute per la UI | **Container sempre**: `VBoxContainer` + `ScrollContainer`, mai `Position = (12, 540)` |
