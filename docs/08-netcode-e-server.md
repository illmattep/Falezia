# 08 · Netcode e server

## Scelta del motore: stato della verifica

**Il motore non è ancora deciso: Godot 4 o Flax.** Questo documento è scritto sulle API di Godot, ma
**l'architettura che descrive è indipendente dal motore** — ed è esattamente il motivo per cui è stata
progettata così. Vedi in fondo la sezione *"Cosa cambia se si passa a Flax"*.

### Flax Engine — fatti verificati dalla fonte *(11 ottobre 2026)*

| Fatto | Dettaglio | Fonte |
|---|---|---|
| **Licenza** | *"Ship your game and pay a **4% royalty on earnings exceeding $250.000** (per calendar quarter)"*. Gratis per uso non commerciale o didattico. Tutte le funzionalità, tutte le piattaforme, tutto il codice sorgente dell'editor inclusi | `flaxengine.com/licensing` |
| **Natura** | **EULA proprietario, non open source.** Il repository è *source-available*, ma l'uso è regolato dalla EULA (PDF scaricabile) | `LICENSE.md` del repo |
| **Stack** | Core in C++, scripting in **C#**, richiede **.NET 10 SDK**. Sviluppo su Windows e Linux | `README.md` del repo |
| **Piattaforme di build** | Android · Apple · GDK · Linux · Mac · UWP · Unix · Web · Windows · iOS | `Source/Tools/Flax.Build/Platforms` |
| **Server dedicato** | ⚠️ **Non esiste un target "Dedicated Server".** La modalità headless compare solo in tutorial della community, non nella documentazione ufficiale | `Flax.Build/Platforms` + ricerca |
| **Rete** | Documentata su due livelli: *high-level* (replicatori) e *low-level* (`NetworkPeer` + `INetworkDriver` con pacchetti personalizzati) | `docs.flaxengine.com/manual/networking` |

### I due punti che decidono

**✅ A favore di Flax — il linguaggio, e non è poco.**
La nostra architettura è una **simulazione pura a 64 Hz** che gira due volte per tick (predizione +
autorità) più il rewind della lag compensation. In **GDScript** questo sarebbe troppo lento, e si
finirebbe in GDExtension C++ — una tassa di complessità enorme. **In Flax il C# è di prima classe e
basta.** E c'è un secondo vantaggio: essendo C#, si ha accesso **all'ecosistema .NET di rete** —
`System.Net.Sockets`, o librerie UDP collaudate come **LiteNetLib** — invece di essere legati al
netcode del motore. **È un vantaggio che cade esattamente sulla parte più difficile del progetto.**

**⚠️ Contro Flax — non esiste un server dedicato di prima classe.**
La nostra architettura è **server autoritativo con server dedicati**, e la ragione per cui l'abbiamo
scelta è l'integrità competitiva. In Unity si spunta una casella e si ottiene un binario headless.
In Flax **il target non esiste**, e la modalità headless è roba da tutorial di terzi.
Non è detto che sia impossibile — ma **è un rischio che cade esattamente sull'asse più critico.**

**E il rischio di fondo, che vale per tutto il resto:** l'ecosistema di Flax è **molto più piccolo**
di quello di Godot (che è già piccolo). Meno tutorial, meno problemi già risolti, meno strumenti di
terzi. Per un team piccolo che deve scrivere netcode competitivo da zero, **significa restare soli sui
problemi difficili.**

### La raccomandazione: uno spike di 3 giorni prima di impegnarsi

Prima di migrare i documenti, **M0 diventa una prova, non una milestone.** Tre giorni, due domande:

| # | La prova | Criterio di superamento |
|---|---|---|
| **1** | **Un processo server headless su Linux a 64 Hz**, senza finestra né renderer, raggiungibile via rete | Se ci vogliono **più di 3 giorni**, è un segnale |
| **2** | **Una libreria di simulazione pura in C#**, senza nessuna dipendenza dal motore, eseguibile **sia nel client sia nel processo server** | Se il motore la "contamina", l'architettura è a rischio |

**Se passano entrambe, Flax è la scelta giusta e i documenti si migrano con fiducia.**
Se la prova 1 è una lotta, **il rischio è caduto proprio dove non possiamo permettercelo.**

---

## La decisione che salva il progetto

> **In nessun motore il MultiplayerAPI di alto livello basta per un FPS competitivo.**
>
> In Godot significa non usare `MultiplayerSynchronizer` e `SceneMultiplayer`. In Flax significa non
> usare i replicatori di alto livello. Fra tre mesi, altrimenti, hai una riscrittura completa davanti.
> **Va deciso al primo commit.**

L'architettura corretta è:

```
┌─────────────────────────────────────────────────────────────┐
│                  SIMULAZIONE PURA (condivisa)                │
│                                                             │
│   Sim.step(state, inputs, dt)  →  new_state                 │
│                                                             │
│   • Funzione pura, nessun nodo Godot                        │
│   • Passo fisso: dt = 1/64                                  │
│   • Stesso codice su CLIENT e SERVER                        │
│   • Movimento, collisioni, proiettili, trappole, obiettivi  │
└─────────────────────────────────────────────────────────────┘
        ↑                                              ↑
   ┌────┴────┐                                   ┌─────┴─────┐
   │ CLIENT  │                                   │  SERVER   │
   │ prevede │                                   │  autorità │
   │ + rende │                                   │  headless │
   └─────────┘                                   └───────────┘
```

**Godot fornisce solo tre cose:** input, rendering e trasporto di rete.
La logica di gioco non è mai dentro un nodo.

### Perché è indispensabile

1. **Determinismo di riproduzione.** Il server può ricalcolare un tick passato per la lag
   compensation, riusando *esattamente* lo stesso codice del client.
2. **Rewind e lag compensation** diventano possibili senza duplicare la logica.
3. **Testabilità.** Puoi testare il combattimento headless in CI, senza grafica.
4. **Tickrate indipendente dal frame rate.** In Godot la fisica è legata a `_physics_process`:
   questo è incompatibile con un tickrate competitivo e con la predizione client.

---

## Tickrate e frequenze

| Parametro | Valore | Note |
|---|---|---|
| **Tick di simulazione** | **64 Hz** | Sufficiente per un tattico con TTK medio |
| Tick di simulazione (server competitivo) | 128 Hz | Opzione, raddoppia il costo CPU |
| **Invio input del client** | **64 Hz** | Un input per tick, con ridondanza degli ultimi 3 |
| **Snapshot del server** | **32 Hz** (delta) | 64 Hz in modalità competitiva |
| **Interpolazione entità remote** | 2 snapshot indietro (~62 ms) | Compromesso fluidità/latenza |
| **Rewind massimo** | **200 ms** | Oltre, è colpa del giocatore, non del server |
| Storia delle pose per rewind | **1,0 s** | Buffer circolare |

---

## Il ciclo del client

```
1. Campiona l'input                       (64 Hz)
2. Sim.step(state, input, 1/64)           → PREDIZIONE LOCALE
3. Invia l'input con sequence number      → server
4. Rende il proprio personaggio           (istantaneo, zero latenza percepita)
5. Riceve lo snapshot del server con l'ultima sequenza applicata
6. Confronta la propria posizione prevista con quella autoritativa
7. Se divergono oltre la soglia → RECONCILIAZIONE:
      · riavvolge allo stato del server
      · riapplica tutti gli input non ancora confermati
      · prosegue
```

**Soglia di riconciliazione:** 1 cm o 0,5°. Sotto quella soglia, correzione morbida
(interpolazione) per non produrre scatti visibili. Sopra, correzione dura.

**Buffer circolare degli input:** 256 tick. Ogni input inviato 3 volte (ridondanza) finché non
è confermato, così una perdita di pacchetto non fa perdere un tick di movimento.

---

## Il ciclo del server

```
per ogni tick (64 Hz):
    1. Raccogli gli input dei client (buffer ordinato per sequence)
    2. Per ogni giocatore: Sim.step() con il suo input
    3. Simula proiettili, trappole, timers, obiettivi
    4. Valida:
         · rate dell'input (anti-speedhack)
         · velocità massima raggiungibile
         · coerenza degli angoli di mira
         · linea di vista per i colpi
         · legittimità delle transazioni economiche
    5. Risolvi i colpi con LAG COMPENSATION (vedi sotto)
    6. Serializza uno snapshot DELTA per ogni giocatore
    7. Invia (inaffidabile, canale 1)
```

---

## Lag compensation

```
Il client A spara al tick 1000, ma a causa del ping il suo colpo "appartiene" al tick T-8.
     ↓
Il server riavvolge le pose di TUTTI gli altri giocatori al tick (1000 - latenza_A)
     ↓
Usando la storia delle pose (buffer di 1 s), ricostruisce le capsule/hitbox di quel momento
     ↓
Esegue il raycast contro le hitbox RIAVVOLTE
     ↓
Applica il danno allo stato ATTUALE se il colpo è valido
```

| Regola | Valore |
|---|---|
| Rewind massimo | **200 ms** (oltre, il colpo viene scartato e loggato) |
| Risoluzione hitbox | Capsule + gruppi per osso: testa, collo, torso, braccia, gambe |
| Frequenza di campionamento delle pose | Ogni tick (64 Hz) |
| Compensazione inversa per il difensore | **Sì**: il difensore non deve essere punito dal proprio ping quando si copre |

> **Il rewind va limitato.** Un rewind illimitato premia chi ha ping alto: spara in un passato
> che gli altri non possono vedere. 200 ms è il compromesso pragmatico e va **documentato nel
> netgraph**, così il giocatore capisce perché è morto.

---

## Trasporto e protocollo

| Canale | Affidabilità | Contenuto |
|---|---|---|
| **0** | Affidabile, ordinato | Handshake, autenticazione, chat, eventi economici, approvazioni, fine round |
| **1** | Inaffidabile, non ordinato | Input del client, snapshot del server |

**Implementazione:** `ENetConnection` di Godot (livello basso) — **non** `ENetMultiplayerPeer`
di alto livello, che è legato al sistema dei nodi.

### Formato dello snapshot (delta)
```
tick                : uint32
ultima_seq_confermata: uint16
giocatori_modificati : N × { id, posizione, rotazione, velocità, salute, piastre, stato, item }
eventi               : array (sparo, trappola, prelievo, faro, ecc.)
```

Con 10 giocatori a 32 Hz, il traffico è nell'ordine di **20–40 kbit/s per client**. Trascurabile.

---

## Interest management

Con 10 giocatori non serve un sistema di visibilità complesso, ma servono **due cose**:

1. **Partizionamento spaziale delle trappole e dei sensori.** Con 5–10 trappole per squadra e
   sensori che interrogano per prossimità a 64 Hz, serve una griglia (spatial hash) o i costi
   crescono in modo non lineare.
2. **Culling degli eventi sonori.** Un suono non deve essere inviato a chi è a 300 m se il suo
   raggio è 35 m. Il rumore è **informazione**, quindi va filtrato per rilevanza, non trasmesso a tutti.

---

## Server dedicati

**Mai hostati dai giocatori. Mai listen server. Nessun P2P per il gameplay.**

### Deployment
| Elemento | Scelta |
|---|---|
| Build | Export headless Godot 4.x, **Linux x86_64** |
| Avvio | `godot --headless --main-pack server.pck --config region.json` |
| Container | Docker, immagine minimale (~150 MB) |
| Orchestrazione | Partenza semplice: script + API di allocazione. Poi Kubernetes / Agones |
| Regioni | **EU-Ovest, EU-Est, NA-Est, NA-Ovest, SA, Asia** |
| Capacità | 1 partita = 1 processo server (5v5, economico). ~10 partite per core moderno |

### Servizi esterni (fuori dal processo di gioco)
| Servizio | Funzione |
|---|---|
| **Autenticazione** | Identità, anti-ban evade |
| **Matchmaking** | MMR + ping + coda. Alloca il server e restituisce l'indirizzo |
| **Economia e inventario** | **Tutta l'economia è validata qui**, mai sul client |
| **Statistiche** | Estrazioni, K/D, economia per round, telemetria |
| **Classifica** | Glicko-2, con decadimento |
| **Anti-cheat** | Analisi statistica sui replay + revisione manuale |

---

## Anti-cheat

**La difesa principale è architetturale: il client non ha autorità su nulla di importante.**

| Vettore | Contromisura |
|---|---|
| Client che si teletrasporta | Validazione della velocità massima per tick |
| Client che spara attraverso i muri | Linea di vista calcolata **sempre** sul server |
| Client che modifica la salute | La salute vive solo sul server |
| Client che si dà soldi | **Tutte le transazioni sono server-side** (l'economia è il vettore più sottovalutato) |
| Client che scavalca l'approvazione | L'approvazione è un evento di rete validato, non un flag locale |
| Client che spamma input | Rate limit per tick |
| Wallhack (unico non risolvibile architetturalmente) | **Ping dei sensori**, niente marcatori live. Più: analisi statistica di pre-aim e reazione |

**Sui replay:** poiché la simulazione è pura e deterministica, **il server può registrare la sequenza
di input e rigiocare la partita a costo quasi zero**. Questo ti dà gratuitamente:
- Anti-cheat con revisione manuale stile Overwatch
- Strumenti di coaching (rivedere le proprie partite)
- Telemetria di bilanciamento (dove e quando le persone muoiono)

> **Questo è il motivo tecnico per cui la "simulazione pura" non è un lusso accademico: è un
> moltiplicatore di funzionalità a costo zero.**

---

## Checklist M0 (primo codice da scrivere)

- [ ] `Sim` isolato in una cartella **senza nessuna dipendenza dal motore**
- [ ] `Sim.step(state, inputs, dt)` pura, deterministica, testabile headless
- [ ] Movimento con accelerazione, attrito, controllo aereo limitato
- [ ] Input buffer circolare (256 tick)
- [ ] **Un processo server headless che gira a 64 Hz con un giocatore**
- [ ] Client che predice il proprio movimento e non scatta
- [ ] Netgraph: ping, jitter, tick, correzioni
- [ ] **Test:** 200 ms di ping artificiale, 2% di perdita pacchetti, movimento fluido

---

## Cosa cambia se si passa a Flax

**Sorpresa: quasi niente.** È il dividendo dell'architettura scelta.

### Cosa NON cambia mai (è solo regole e numeri)

| Documento | Perché è indipendente dal motore |
|---|---|
| **01 · Visione** | Idee |
| **02 · Mappa** | Geometria e tempi di percorrenza |
| **03 · Round** | Regole e timer |
| **04 · Economia** | Numeri e regole di spesa |
| **05 · Combattimento** | Danni, movimento, rumore, trappole, porte |
| **06 · UX** | Flussi, HUD, accessibilità |
| **09 · Roadmap** | Piano e rischi |
| **10 · Loadout** | Regole |
| **11 · Armi** | Numeri |
| **12 · Brief artistico** | Forma e materia |

### Cosa cambia (è la "pelle": binding, non logica)

| Ambito | Godot 4 | Flax |
|---|---|---|
| **Linguaggio hot path** | GDScript → serve GDExtension C++ | **C# di prima classe** |
| **Trasporto** | `ENetConnection`, canali 0/1 | `NetworkPeer` + `INetworkDriver`, **o direttamente .NET** (`System.Net.Sockets`, LiteNetLib) |
| **Server headless** | Export Linux + `--headless` | ⚠️ **Da verificare** — nessun target dedicato |
| **Baking luce e GI** | `LightmapGI` | Lightmap e GI del renderer Flax |
| **Instancing dei prop** | `MultiMeshInstance3D` | Instanced meshes |
| **Occlusion culling** | Poligoni di occlusione | Sistema di occlusione Flax |
| **LOD** | `VisibilityRange` | LOD integrato negli asset |

**Il criterio per decidere in fretta:** il lavoro da rifare è **solo la colonna destra**, e vale
**due o tre settimane di M0–M1** — non mesi. **Il rischio vero non è la riscrittura: sono
l'ecosistema e il server headless.**
