# 18 · Control plane: pannello, coda e istanze

> Il server di gioco sa fare **una** partita. Questo documento descrive il pezzo che sta
> sopra e che le mette in fila: un processo solo che accende le partite, le assegna ai
> giocatori e si amministra da browser.

---

## 1 · Perché un processo separato

`Falezia.Server` è deliberatamente stupido: un socket, una simulazione, una partita. Non sa
quante partite esistono, non sa chi aspetta in coda, non ha un pannello. Se ci mettessimo
anche questo dentro, ogni partita avrebbe una copia del pannello e nessuna saprebbe delle
altre.

`Falezia.Control` è il contrario: **non simula niente**. Accende processi, legge il loro
stdout, tiene una coda e serve una pagina web.

```
                    ┌──────────────── Falezia.Control ────────────────┐
  browser ── HTTP ──►  pannello + API (HttpListener :8090)             │
                    │                                                 │
  client  ── HTTP ──►  coda di matchmaking  ── spawn ──►  Falezia.Server :27015/udp
                                    │                     Falezia.Server :27016/udp
                                    └──── log (stdout) ◄── Falezia.Server :27017/udp
```

La telemetria passa dal **log**: il server stampa `join:`/`timeout:`/`kick:` e il control plane
conta da lì. Non c'è un protocollo di controllo dedicato — e non serve finché le istanze sono
processi figli sulla stessa macchina.

---

## 2 · Avviarlo

```bash
./run.sh dev                 # pannello su http://127.0.0.1:8090
./run.sh dev --log=debug     # passa opzioni al control plane
```

Il primo avvio genera il token in `build/control/control.json` e stampa l'indirizzo completo.
Ricompila **solo** il control plane (e il server, una volta) — il gioco non si ricompila.

Il client si apre dal pannello, pulsante *Client*, oppure a mano:

```bash
./run.sh play -faleziaOnline 127.0.0.1:27015
```

---

## 3 · Il limite rigido

È la manopola che conta su una VPS.

| | |
|---|---|
| `hardLimitEnabled` | se è spento, la coda apre una partita per ogni giocatore che arriva |
| `hardLimitMaxInstances` | quante partite possono esistere **contemporaneamente** |

Il controllo è a **istanze**, non a giocatori: il costo di una VPS è il numero di processi di
simulazione a 64 Hz, non il numero di posti. Al tetto, i giocatori restano in coda con
`note: "limite rigido raggiunto: in attesa"` invece di far fallire l'allocazione.

Il limite si cambia dal pannello e vale **subito**, anche a partite aperte: abbassarlo non
chiude le partite esistenti, impedisce solo di aprirne altre.

---

## 4 · La coda

```
POST /api/queue            {"name":"matteo"}   → {"ticket":"cd8ed3aa","state":"queued"}
GET  /api/queue/{ticket}                       → host, port, matchToken, stato
DELETE /api/queue/{ticket}                     → annulla
```

Regole, in ordine:

1. se una partita ha posto libero (contando anche i posti **prenotati** da chi non è ancora
   entrato), il giocatore va lì;
2. se non c'è posto e il limite lo permette, si apre una partita nuova;
3. altrimenti si aspetta.

Un ticket `allocated` che non si presenta entro **5 minuti** scade e libera il posto. Uno in
coda da più di **10 minuti** scade.

> **Limite noto.** Il `matchToken` viene emesso monouso ma il server di gioco **non lo valida
> ancora**: chi conosce `host:porta` entra. La validazione server→server è il passo successivo
> (doc 14, "le tre regole che non si violano").

---

## 5 · API

Tutte sotto `/api/admin` richiedono `X-Admin-Token: <token>` (o `?token=`).

| Metodo | Rotta | Cosa fa |
|---|---|---|
| GET | `/api/admin/state` | configurazione, istanze, ticket, coda, uptime, stato del limite |
| POST | `/api/admin/settings` | modifica parziale delle impostazioni e le salva |
| POST | `/api/admin/instances` | apre una partita (rispetta il limite) |
| POST | `/api/admin/instances/{id}/stop` | chiude una partita con `quit` su stdin |
| POST | `/api/admin/instances/stop-all` | chiude tutte |
| POST | `/api/admin/instances/{id}/client` | avvia il client su quella porta |
| GET | `/api/admin/logs?after=N` | righe di log dopo la sequenza N (il pannello fa polling) |
| GET | `/health` | `ok`, senza token |

`GET /api/admin/state` è pensato anche per essere letto da script: è tutta la realtà del
control plane in un JSON.

---

## 6 · Configurazione

`control.json` (in `build/control/` in sviluppo, `/var/lib/falezia/` su VPS). Precedenza:
**file < variabili d'ambiente < riga di comando**.

| Campo | Default | Note |
|---|---|---|
| `bind` / `port` | `127.0.0.1` / `8090` | su VPS `0.0.0.0` |
| `publicHost` | vuoto | l'host che il client deve usare; vuoto = dedotto da `bind` |
| `adminToken` | generato | |
| `hardLimitEnabled` / `hardLimitMaxInstances` | `true` / `2` | |
| `maxPlayersPerInstance` | `10` | |
| `minPlayersToStart` / `queueWaitSeconds` | `1` / `30` | in sviluppo 1, in produzione 10 |
| `map` / `logLevel` | `calanera.json` / `info` | passati alle istanze nuove |
| `botsPerTeam` | `0` | bot per squadra: servono a vedere il ciclo del round senza dieci persone |
| `portRangeStart` / `portRangeEnd` | `27015` / `27099` | porte UDP |
| `autoBuild` / `autoStartInstance` | `true` / `true` | |
| `serverCommand` / `clientCommand` | vedi file | segnaposto `{dll} {port} {max} {map} {level} {root}` |

Variabili: `FALEZIA_CONTROL_BIND`, `FALEZIA_CONTROL_PORT`, `FALEZIA_CONTROL_TOKEN`,
`FALEZIA_CONTROL_DATA`, `FALEZIA_PUBLIC_HOST`, `FALEZIA_HARD_LIMIT`, `FALEZIA_HARD_LIMIT_MAX`.

---

## 6b · Provare il round da soli

Il ciclo dell'obiettivo (cassa → Nucleo → estrazione) si può vedere girare senza
collegare nessuno:

```bash
# dal pannello: "Bot per squadra" = 2, poi "Avvia partita"
# oppure a mano, in allenamento (tutti i bot in una squadra, nessun wipe):
dotnet server/Falezia.Server/bin/Debug/net8.0/Falezia.Server.dll --port=27015 --bots=2 --bots-team=1 --log=info
```

Nei log compaiono gli eventi del round:

```
obiettivo: CASSA APERTA
obiettivo: NUCLEO PRELEVATO da 100 (NORD)
obiettivo: ESTRAZIONE AVVIATA · GROTTA (Est) · assedio 120s
obiettivo: ESTRAZIONE COMPLETATA · vince NORD
```

I bot non hanno un socket e non combattono bene: sono un **attrezzo di sviluppo**, non
un'IA da vendere. Seguono le rotte verificate della mappa, quindi se una rotta è rotta
si vede subito.

## 7 · Cosa non c'è ancora

| Cosa | Perché conta |
|---|---|
| **Validazione del `matchToken`** | oggi il token è una ricevuta, non una chiave |
| **TLS e login al pannello** | il token viaggia in chiaro: tunnel SSH o nginx |
| **Istanze su macchine diverse** | il control plane assume processi figli locali |
| **Riavvio automatico di un'istanza morta** | viene marcata `dead` e ne serve una nuova |
| **Riconnessione del giocatore** | il ticket scade, non si riprende la partita |
| **Metriche** | solo log; niente Prometheus |
| **Selezione personaggio** | vedi doc 10, ancora da implementare |
