# 09 · Roadmap e rischi

## La regola del progetto

> **Il gioco deve divertire in graybox, con i rettangoli grigi, prima che esista un solo pixel di arte.**
>
> Se non diverte lì, non divertirà mai. E per un team piccolo, l'arte è la voce di costo che
> trasforma un prototipo in un progetto abbandonato.

---

## Le milestone

### M0 · Fondamenta netcode *(~2 settimane)*
- Progetto Godot 4.x, struttura di cartelle, CI
- `Sim.step()` pura a passo fisso (64 Hz)
- Movimento: passo, sprint, crouch, stamina, vault
- Input buffer + predizione client + riconciliazione
- Server headless minimale
- **Criterio di uscita:** due client su una mappa vuota che si vedono muoversi senza scatti a 200 ms di ping

### M1 · Sparatoria *(~3 settimane)*
- Armi hitscan, rinculo, spread, ADS
- Hitbox per osso, moltiplicatori, piastre, ferite localizzate leggere
- **Lag compensation con rewind** + storia delle pose
- Prima zona graybox: Piazza Vecchia
- **Criterio di uscita:** 5v5 in piazza, i colpi "si sentono giusti"

### M2 · Il ciclo del round *(~3 settimane)*
- Cassa: apertura a mani / Kit / Robot
- Prelievo, trasporto, avvio estrazione, **assedio**
- Le 5 condizioni di fine round + timer con sospensione
- DBNO, rianimazione, team wipe
- **Criterio di uscita:** un round completo giocabile dall'inizio alla fine

### M3 · Mappa completa *(~3 settimane)*
- Graybox delle 5 zone + anello
- **Verifica cronometrata** dei tempi di percorrenza
- Verifica delle rotte indipendenti e delle linee di tiro
- Stato Lockdown (geometria che si chiude)
- **Criterio di uscita:** le 2 estrazioni si giocano in modo percettibilmente diverso

### M4 · Economia *(~3 settimane)*
- Cassa comune, soglia, policy, approvazione, silenzio-assenso
- Rendiconto live e di fine round
- Conservazione attrezzatura, regole di rivendita
- Curve di introiti, sussidio anti-spirale
- **Criterio di uscita:** 5 estranei riescono a comprare senza litigare

### M5 · Trappole, sensori, utility *(~2 settimane)*
- Filo spinato, mine, sensori con ping
- Granate, fumogeni, endoscopio, drone
- Tutti i tell audiovisivi
- **Criterio di uscita:** nessuna morte che faccia dire "che culo"

### M6 · Infrastruttura *(~3 settimane)*
- Server dedicati, allocazione, regioni
- Matchmaking, lobby, party, riconnessione
- Anti-cheat di base + registrazione replay
- **Criterio di uscita:** una partita 5v5 completa fra 10 persone reali su server dedicato

### M7 · Playtest e tuning *(aperto)*
- Il primo playtest serio è **qui**, non prima
- Bilanciamento armi, prezzi, timer, curve economiche
- **Criterio di uscita:** il tasso di abbandono nei primi 3 round scende

### M8 · Pass artistico *(aperto, solo dopo M7)*
- A1 Blocco materiali → A2 Kit modulari → A3 Decorazione → A4 Ottimizzazione
- **Si entra in M8 solo se M7 ha dato un gioco che diverte**

---

## Stima complessiva

| Fase | Durata | Con 2 sviluppatori |
|---|---|---|
| M0–M2 (gioco giocabile) | 8 settimane | **~12 settimane** |
| M3–M5 (sistemi completi) | 8 settimane | **~12 settimane** |
| M6 (infrastruttura) | 3 settimane | **~5 settimane** |
| M7 (playtest, tuning) | — | **~6 settimane** |
| **Gioco in alpha giocabile** | | **~8–9 mesi** |
| M8 (arte di produzione) | — | **+4–6 mesi con 1–2 artisti** |

**Realtà:** con 2 programmatori e 1 artista, un'alpha presentabile è a **12–15 mesi**.
Chi ti dice 4 mesi sta vendendo fumo.

---

## I rischi, in ordine di pericolosità

### 1 · Il netcode in Godot *(rischio altissimo)*
**Il rischio:** Godot non è progettato per FPS competitivi. Se si parte con l'API di alto livello,
si riscrive tutto a metà progetto.
**Mitigazione:** simulazione pura a passo fisso fin dal **primo commit** (M0). Il netcode è il
primo codice che si scrive, non l'ultimo.
**Segnale d'allarme:** se a M1 il movimento ha scatti a 200 ms di ping, fermare tutto e risolvere.

### 2 · Il centro diventa l'unica partita *(alto)*
**Il rischio:** la Piazza è l'unico punto in cui si combatte, e ogni round si decide negli stessi
30 secondi. Il gioco muore di noia al round 3.
**Mitigazione:** l'anello, minimo 4 approcci, la piazza su 2 livelli, e il caso peggiore testato a
M3. **Verifica: dalla piazza si devono vedere al massimo 3 linee di tiro lunghe.**

### 3 · Camping del Nucleo *(alto)*
**Il rischio:** chi prende il Nucleo si chiude in un angolo e aspetta i 120 s, e il gioco diventa
un assedio statico ripetuto 9 volte.
**Mitigazione:** nessun respawn per l'assaltante **più** nessun respawn per il difensore: chi si
chiude perde uomini e non li recupera. Il difensore **deve** coprire l'anello, quindi deve esporsi.
In più: i 120 s sono un tetto, non un obiettivo — l'estrazione si completa, non si "sopravvive".

### 4 · Spirale economica *(medio-alto)*
**Il rischio:** 0-3 al round 4, la cassa è vuota, il match è finito 20 minuti prima.
**Mitigazione:** bonus sconfitta progressivo ($3.200 > $3.000 della vittoria) + **pavimento anti-spirale
a $1.500** + pistola e utility del loadout sempre gratuite (ma **non** la piastra).
**Verifica:** simulare 1.000 partite con bot e guardare la distribuzione dei rimontaggi da 0-3.

### 5 · Il sistema di approvazione frustra il solo-queue *(medio-alto)*
**Il rischio:** è il sistema più originale del gioco e anche il più facile da odiare. Con estranei
e 40 secondi, l'approvazione può diventare un tool di griefing.
**Mitigazione:** politica predefinita automatica, silenzio-assenso a 5 s, banner non bloccante,
rendiconto pubblico, segnalazione specifica per "ostruzionismo economico".
**Segnale d'allarme:** se nei playtest il 30%+ dei giocatori disattiva l'approvazione, il sistema
va ridisegnato, non difeso.

### 6 · L'assedio è sempre uguale *(medio)*
**Il rischio:** 120 s × 7 round = 14 minuti di "tieni il punto".
**Mitigazione:** Est e Ovest devono essere **geometricamente opposti** (chiuso/verticale vs
lungo/orizzontale). Il Lockdown cambia il campo. Le trappole piazzate in anticipo cambiano la
geometria tattica di ogni round.

### 7 · Il volume di contenuti *(medio)*
**Il rischio:** 5 giocatori, ×2 squadre, ×5 tipi di arma, ×12 utility, ×6 trappole, ×5 zone —
è un gioco completo, non un prototipo.
**Mitigazione:** tagliare. Vedi sotto.

---

## Cosa tagliare, e senza discutere

| Cosa | Perché |
|---|---|
| **Console** | Porting, certificazione, controller: mesi. Solo PC al lancio |
| **Campagne o PvE** | Raddoppia il lavoro dell'IA. Fuori |
| **Personalizzazione estetica** | Costo artistico enorme, valore competitivo zero. Il colore è informazione |
| **Sistema di progressione con sblocchi** | In un gioco competitivo è un danno, non una feature |
| **Battle pass al lancio** | Prima il gioco, poi la monetizzazione |
| **Più di 2 mappe al lancio** | Una mappa fatta bene batte quattro fatte male |
| **Più di 5 tipi di arma** | Ogni arma è un problema di bilanciamento |
| **Distruzione e sfondamento** | **Esclusi del tutto.** La mappa è completamente statica: giova a occlusion culling, LightmapGI e netcode |
| **Sanguinamento e sistemi medici complessi** | Allunga i round, svuota l'azione, allontana dal formato a 5 estrazioni |
| **Veicoli** | Già deciso, ma vale la pena ripeterlo |

---

## La rete di sicurezza commerciale

Due modalità, una mappa, un motore:

| Modalità | Formato | A chi serve |
|---|---|---|
| **Extract & Secure** | 5v5, primo a 5 | Il gioco principale, il competitivo, il pubblico di R6 |
| **Sito** | 4v4, una zona, round da 3 minuti | I nuovi giocatori, chi ha 15 minuti, il warmup |

La seconda **non costa una mappa nuova**. Serve a ridurre la barriera d'ingresso, che per un
tattico no-respawn con economia condivisa è il problema esistenziale numero uno.

---

## La domanda che deciderà tutto

> **Il primo playtest (M7) risponderà a una sola domanda:**
> **le persone vogliono giocare il round successivo?**

Se sì, il gioco esiste e il resto è tempo e soldi.
Se no, nessuna quantità di intonaco ocra lo salverà.
