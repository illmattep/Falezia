# Matchmaking e brief

> Premi **GIOCA** → il client contatta il server e si mette in coda → quando la lobby è
> completa il server apre il **brief** (un minuto, la scelta dell'operatore) → poi **acquisti**
> e partita. Questo documento dice come funziona, dove sta il codice e cosa manca.

---

## Il flusso, passo per passo

| # | Cosa succede | Dove |
|---|---|---|
| 1 | Il menu mostra **GIOCA**: il client si collega al server (host/porta salvati, default `127.0.0.1:27015`) | `MainMenu.PlayOnline` |
| 2 | Il primo pacchetto di input fa **entrare in coda**: `coda: giocatore 1 (1/2)` | `GameServer.Join` |
| 3 | Il client vede `IN CODA · 1 / 2` con la barra che avanza (i numeri arrivano dallo snapshot) | `MatchFlow` (coda) |
| 4 | Quando in coda ci sono `--players=N` persone, la **lobby è completa**: il server azzera il round e apre il brief | `GameServer.TryFormLobby` |
| 5 | Il client mostra **MATCH FOUND** per 1,6 s | `MatchFlow` (found) |
| 6 | **BRIEF**: si sceglie l'operatore (le 5 specializzazioni del doc 10) e l'arma dichiarata, poi si **BLOCCA**. La scelta è **pubblica** | `DraftScreen` · `Match.TryDraft` |
| 7 | Il brief dura **al massimo 60 s** e si chiude **prima** se hanno bloccato tutti | `Match.BriefSeconds` · `Match.AllDrafted` · `Match.EndBrief` |
| 8 | Chi non blocca in tempo la scelta la prende lo stesso (una per posto in coda, sempre la stessa) | `Match.AutoDraft` · `Draft.Suggested` |
| 9 | Fase di **acquisti** (20 s), poi il round: da qui è il gioco di sempre | `Match.Tick` |

Il brief **è** la fase `Warmup`: nome storico nel codice, comportamento nuovo. `Match.Reset`
la apre a 60 s, e `EndBrief` la chiude in anticipo.

### Scegliere non basta: si blocca

Il clic su una carta **seleziona**, non sceglie. La scelta parte solo dal bottone
`BLOCCA · <operatore>` in alto a destra, e da lì non si cambia più (`BLOCCATO · <operatore>`).

Non è un vezzo: senza il passo di conferma un clic per curiosità mandava il draft al server,
`AllDrafted` diventava vero, e il minuto di brief finiva **subito** per tutti. Il minuto deve
valere un minuto, e la scelta deve essere una decisione.

### Coda e brief sono schermate, non HUD

Mentre `MatchFlow` o `DraftScreen` sono aperti, `Hud` e `Minimap` non disegnano niente:
mirino, munizioni, orologio e mappa davanti a "scegli l'operatore" sono rumore. La pausa
(`Esc`) resta invece disponibile, perché è da lì che si esce.

### Se il server non risponde

Con UDP "connesso" non vuol dire niente: il socket resta aperto anche se dall'altra parte non
c'è nessuno. La coda guarda quindi **l'età dell'ultimo snapshot** (`NetClient.SinceSnapshot`):
oltre 3 secondi il pannello passa da `IN CODA` a `IN ATTESA DEL SERVER`, con la barra che
scorre e l'invito a controllare che il server sia acceso.

## Perché la coda sta nel server e non nel client

Il client non conta niente: riceve `QueueCount` e `QueueNeeded` nello snapshot e li disegna.
Chi decide che la lobby è piena è **solo** il server. Un client che si dichiara "pronto" non
può far partire niente: la partita si apre quando il *server* vede abbastanza connessioni.

Nel protocollo, tre byte in più per ogni snapshot (subito dopo le casse di squadra):

```
queueCount u8 · queueNeeded u8 · inMatch bool
```

`inMatch` è quello che distingue "sono in coda" da "sono in partita": serve al brief, che non
deve comparire mentre aspetti.

## Come si prova

```bash
# due giocatori veri (due client sullo stesso server)
./run.sh server --players=2
./run.sh play -faleziaOnline 127.0.0.1:27015     # in due terminali

# da soli, con i bot a riempire la partita
./run.sh server --players=1 --bots=4
```

Il server stampa il percorso completo:

```
matchmaking: in attesa di 2 giocatori in coda
coda: giocatore 1 (1/2)
coda: giocatore 2 (2/2)
lobby completa: 2 giocatori · BRIEF 60 s
brief: hanno scelto tutti · si va agli acquisti
fase: Warmup → Buy (round 1)
```

E se nessuno blocca, il minuto scorre fino in fondo:

```
[23:51:40] lobby completa: 1 giocatori · BRIEF 60 s
[23:52:40] brief scaduto: 1 senza scelta · specializzazione automatica
[23:52:40] fase: Warmup → Buy (round 1)
```

`--players=N` (o `FALEZIA_PLAYERS`) è la manopola del matchmaking: è il numero di **umani**
che fa partire la lobby.

## Cosa manca (dichiarato)

| Cosa | Nota |
|---|---|
| **Matchmaking vero** (skill, latenza, regioni) | Oggi è "primo che arriva, primo che gioca". Il control plane (`server/Falezia.Control`, con `Matchmaker` e `InstanceManager`) esiste ma **il client non lo usa**: la coda è dentro il game server |
| **Rientro in coda dopo la partita** | A fine partita (`MatchEnd`) non si riparte: bisogna riconnettersi |
| **Gruppi / party** | Si entra da soli |
| **Riempimento a tempo** | Se manca un giocatore la lobby aspetta all'infinito: non c'è un timeout che riempia coi bot o chiuda la coda |
| **Veto delle mappe** | Una mappa sola (Calanera) |
| **Riconnessione** | Chi perde la connessione durante il brief torna in coda da capo |
