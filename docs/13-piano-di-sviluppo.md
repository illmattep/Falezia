# 13 · Piano di sviluppo

> **Stato al 19 ottobre 2026.** Il design è chiuso (12 documenti, 3.800+ righe). Manca la decisione
> sul motore, e questo piano comincia esattamente da lì.
>
> **Questo documento contiene numeri scomodi.** Sono stimati per difetto, non per eccesso: un piano
> che dice "12 mesi" e ne impiega 30 è un piano che ti ha mentito per due anni e mezzo.

---

## 1 · Il numero che nessuno vuole sentire

La prima cosa da guardare non è il calendario: è **quanto lavoro è questo gioco**, in persona-settimane.

### Tecnico — 112 persona-settimane

| Voce | pw |
|---|---|
| Spike motore (i tre test) | 2 |
| Simulazione: movimento, collisioni, stato, determinismo | 7 |
| Netcode: predizione, riconciliazione, lag compensation, snapshot delta | 13 |
| Armi, balistica, hitbox per osso, danni, piastre | 6 |
| Ciclo del round: le 9 fasi, timer, condizioni di fine | 7 |
| Economia, Buy Phase, approvazioni, rendiconto, validazione server | 6 |
| Deployabili, trappole, sensori, sistema porte | 5 |
| Loadout e 5 specializzazioni | 3 |
| DBNO, rianimazione, team wipe | 2 |
| UI, HUD, menu, impostazioni competitive, editor mirino | 11 |
| Audio: occlusione, 3D, passi, informazione sonora | 5 |
| Server dedicati e orchestrazione | 9 |
| Matchmaking, lobby, party, riconnessione | 8 |
| Backend: auth, statistiche, Glicko-2, inventario | 8 |
| Anti-cheat: validazione, replay, euristiche | 6 |
| Telemetria, netgraph, strumenti di debug | 4 |
| Build, CI, tooling | 4 |
| Ottimizzazione (due passaggi) | 6 |
| **Totale tecnico** | **112** |

### Arte e audio — 91 persona-settimane

| Voce | pw |
|---|---|
| Graybox della mappa | 4 |
| Arte della mappa: kit modulari, materiali, lightmap, decorazione | 20 |
| 16 armi: modelli, texture, animazioni, viewmodel | 20 |
| Personaggi (2 squadre) e animazioni | 16 |
| Prop e libreria dei kit | 10 |
| VFX: vampa, impatti, fumo, flash, traccianti | 6 |
| UI art, icone, elementi di HUD | 4 |
| SFX (esternalizzato) | 9 |
| Musica e ambienza (esternalizzata) | 2 |
| **Totale arte** | **91** |

### Design, produzione, QA — 47 persona-settimane

| Voce | pw |
|---|---|
| Bilanciamento e tuning (il lavoro non finisce mai, ma questo è il minimo) | 12 |
| Conduzione e analisi dei playtest | 8 |
| Produzione e project management | 16 |
| QA (funzionale, regressione, multiplayer, prestazioni) | 11 |
| **Totale** | **47** |

### Il totale

```
Tecnico          112
Arte e audio      91
Design/QA/PM      47
─────────────────────
                  250 persona-settimane
+ contingenza 15%  38
─────────────────────
                  ~290 persona-settimane
                  ≈ 5,6 persona-anni
```

**Questo è il gioco che hai progettato.** Non è troppo: è un tattico 5v5 completo con netcode
competitivo, server dedicati, 15 armi, 5 specializzazioni, una mappa a 5 zone e un modello commerciale
senza pay-to-win. **È un gioco vero, e i giochi veri costano questo.**

---

## 2 · Cosa significa in tempo, per dimensione del team

| Squadra | Durata all'alpha chiusa | Durata al lancio | Verdetto |
|---|---|---|---|
| **1 persona** | ~2,9 anni | **~5,6 anni** | ❌ **Non è un'opzione.** Il gioco sarebbe obsoleto prima di uscire |
| **2 persone** | ~1,7 anni | **~2,9 anni** | ⚠️ Possibile solo con i tagli della sezione 12 |
| **3 persone** *(2 dev + 1 artista)* | ~14 mesi | **~19 mesi** | ✅ **Lo scenario base di questo piano** |
| **4 persone** *(2 dev + 1 artista + 1 PM/designer part-time)* | ~11 mesi | **~15 mesi** | ✅ Ideale |
| **5 persone** | ~9 mesi | ~13 mesi | ⚠️ Rende, ma il coordinamento costa: non lineare |

**Assunzioni:** 40 ore/settimana effettive, 15% di contingenza, nessuna fase di crunch.
**Il primo assunto deve essere il programmatore di netcode**, non l'artista: è la competenza più
rara, la più lunga da imparare, e quella su cui il progetto può morire.

### Se sei da solo, questo gioco non esiste — ma un altro sì

290 persona-settimane da solo sono **5,6 anni**, e a quel punto il gioco è obsoleto. Non è pessimismo:
è aritmetica. Ma esiste una versione **da solo, in circa 18 mesi**, che è un gioco diverso e va
riconosciuta come tale:

| Cosa resta | Cosa sparisce |
|---|---|
| **1 zona sola** (la Piazza Vecchia) invece di 5 | L'anello, le rotte e il trasporto perdono senso |
| **4–6 armi** invece di 15 | La varietà della composizione |
| **Nessuna specializzazione** | La composizione emergente |
| **4v4, 3 round** invece di 5v5 primo-a-5 | La partita lunga e l'economia a lungo termine |
| **Server browser** invece di matchmaking | La ladder e le code |
| **Arte da asset store** invece di produzione interna | L'identità visiva di Calanera |

> **Quella versione non si chiama "FALEZIA più piccolo": è un altro gioco.**
> Può valere la pena farlo — ma va **scelto consapevolmente**, non subito per esaurimento al mese 18.

---

## 3 · Le fasi

Ogni fase ha **deliverable** (cosa consegni), **criterio di uscita** (quando puoi passare) e
**dipendenze** (cosa ti blocca).

### FASE 0 · Spike motore — *3 settimane*

**Obiettivo:** decidere il motore con i fatti, non con l'entusiasmo.

| Deliverable | Criterio di uscita |
|---|---|
| **Test 1** — processo server headless su Linux a 64 Hz, senza finestra né renderer, raggiungibile via rete | **Meno di 3 giorni** di lavoro |
| **Test 2** — libreria `Sim` pura, zero dipendenze dal motore, eseguibile **sia nel client sia nel server** | Il motore non la contamina |
| **Test 3** — preset basso su GPU classe **GTX 970**: 1080p, 60 fps, mappa completa in graybox | Il forward path si spegne abbastanza |

**Dipendenze:** nessuna.
**Se un test fallisce:** si cambia motore, non si "aggiusta con più tempo". Vedi sezione 9.

### FASE 1 · Combattimento — *14 settimane*

**Obiettivo:** rispondere alla domanda *"spararsi è bello?"*. Nient'altro.

| Deliverable | |
|---|---|
| `Sim.step()` completa: movimento, collisioni, stamina, vault, porte | |
| Netcode: input buffer 256 tick, predizione, riconciliazione, **lag compensation con rewind** | |
| 3 armi giocabili: Standard, Service, Anvil | |
| Hitbox per osso, moltiplicatori, piastre, DBNO | |
| **1 zona graybox**: Piazza Vecchia | |
| Server headless + client, **5v5 su LAN** | |

**Criterio di uscita — Gate 1:** *una 5v5 con 200 ms di ping e 2% di perdita pacchetti è fluida,
e le morti sono comprensibili.* Se non lo è, **non si passa**: si taglia scopo e si risolve.

**Dipendenze:** Fase 0. **L'arte può partire in parallelo da qui** (l'artista lavora i kit mentre
i dev lavorano la sim).

### FASE 2 · Il ciclo di gioco — *16 settimane*

**Obiettivo:** un round intero, dall'inizio alla fine, senza buchi.

| Deliverable | |
|---|---|
| Le 9 fasi del round: Buy, Approccio, Cassa, Apertura, Prelievo, Trasporto, Avvio, Assedio, fine | |
| Cassa: 15 s / 10 s con Kit / 15 s con Robot | |
| Nucleo, faro di estrazione, rimozione durante l'assedio | |
| Economia completa: cassa comune, soglia $300, approvazione, silenzio-assenso, rendiconto | |
| Conservazione dell'attrezzatura, regole di rivendita, anti-stampa-soldi | |
| **Tutte le 15 armi + coltello**, con i numeri del doc 11 | |
| Le 5 specializzazioni | |
| **Tutte e 5 le zone in graybox** + l'anello | |
| Deployabili, trappole, sensori, sistema porte a 3 stati | |

**Criterio di uscita — Gate 2:** *una partita completa primo-a-5, con timer e condizioni di fine,
si gioca dall'inizio alla fine senza interruzioni.* E: **i tempi di percorrenza misurati col
cronometro corrispondono a quelli progettati.**

**Dipendenze:** Fase 1.

### FASE 3 · Infrastruttura — *10 settimane*

**Obiettivo:** uscire dalla LAN. La prima volta che il gioco incontra persone vere.

| Deliverable | |
|---|---|
| Server dedicati in **almeno 2 regioni** (EU-Ovest, EU-Est) | |
| Allocazione automatica dei match | |
| Matchmaking per MMR e ping, lobby, party, riconnessione a 120 s | |
| Backend: autenticazione, statistiche, Glicko-2 | |
| **L'economia validata interamente lato server** | |
| Netgraph in gioco, telemetria di base | |
| Anti-cheat di prima generazione: validazione di rate, velocità, angoli, linea di vista | |

**Criterio di uscita — Gate 3:** *10 persone che non si conoscono giocano una partita completa
su server remoti, senza assistenza.* È il primo playtest vero.

**Dipendenze:** Fase 2.

### FASE 4 · Alpha chiusa — *14 settimane*

**Obiettivo:** il gioco è completo nelle regole e comincia a essere un posto in cui stare.

| Deliverable | |
|---|---|
| **Primo passaggio artistico**: kit modulari, 8 materiali master, lightmap cotte, foschia | |
| Personaggi e animazioni (prima versione) | |
| UI completa: menu, HUD, Buy Phase, rendiconto, impostazioni competitive | |
| Loadout: 5 preset, finestra di drafting, preset d'arma | |
| Audio integrato: occlusione, passi, informazione sonora | |
| Replay e registrazione degli input | |
| **Playtest settimanali con 20–40 persone** | |

**Criterio di uscita — Gate 4:** *il tasso di abbandono nei primi 3 round scende sotto il 20%*, e
almeno il 40% dei tester torna a giocare la settimana successiva. **È il gate che conta più di tutti.**

### FASE 5 · Beta — *14 settimane*

**Obiettivo:** bilanciare e rifinire. Da qui in poi si tocca solo il tuning.

| Deliverable | |
|---|---|
| Bilanciamento completo: armi, prezzi, curve economiche, timer | |
| Arte della mappa finita (decorazione, differenziazione nord/sud, landmark) | |
| Ottimizzazione sui due target: **GTX 970 @1080p/60** e **GTX 1070 Ti @1080p/120** | |
| Preset "PRO" ufficiale che disattiva tutto | |
| Server in 4–6 regioni | |
| Anti-cheat di seconda generazione: euristiche statistiche, revisione manuale | |
| Accessibilità completa: daltonismo, sottotitoli degli eventi sonori, scala HUD | |
| Modalità **"Sito"** 4v4 compressa (la rete di sicurezza commerciale) | |

**Criterio di uscita:** nessun crash bloccante in 100 partite consecutive; i due target di
prestazione sono rispettati su hardware reale.

### FASE 6 · Lancio — *6 settimane*

| Deliverable | |
|---|---|
| Certificazione e valutazione per età (PEGI) | |
| Documenti legali: EULA, privacy GDPR, note sul trattamento dei dati | |
| Verifica del marchio e del nome | |
| Sito, pagina store, trailer | |
| Piani di capacità per il giorno uno | |
| **Lancio** | |

---

## 4 · La linea del tempo (team da 3)

```
2026  Nov ██ FASE 0 (spike)
      Dic ████████ FASE 1 ────────────────────────
2027  Gen ████████         ────────────────────────
      Feb ████████         ────────────────────────
      Mar ████████████ FASE 2 ────────────────────────
      Apr ████████████         ────────────────────────
      Mag ████████████         ────────────────────────
      Giu ████████████         ────────────────────────
      Lug ██████████ FASE 3 ──────────────────
      Ago ██████████        ──────────────────
      Set ██████████        ──────────────────  ▲ GATE 3
      Ott ██████████████ FASE 4 ────────────────────────
      Nov ██████████████        ────────────────────────
2028  Dic ██████████████        ────────────────────────
      Gen ██████████████        ────────────────────────  ▲ GATE 4
      Feb ██████████████ FASE 5 ────────────────────────
      Mar ██████████████        ────────────────────────
      Apr ██████████████        ────────────────────────
      Mag ██████████████        ────────────────────────
      Giu ██████ FASE 6 ──────
      Lug ██████ ──────  ▲ LANCIO
```

**Alpha chiusa: ~14 mesi. Lancio: ~19–20 mesi.**

### La data che conta più di tutte

> **Gate 3 — la prima partita con 10 sconosciuti su server remoti — cade intorno al mese 10.**
>
> È il **primo momento in cui puoi sapere se il gioco funziona.** Tutto ciò che viene prima è
> costruzione; tutto ciò che viene dopo è taratura. **Ogni decisione di produzione deve proteggere
> quella data.** Se una cosa rischia di spostarla, si taglia.

**Conseguenza operativa:** **l'arte è la cosa più sicura da ritardare**, perché non risponde a nessuna
domanda da cui dipende la sopravvivenza del gioco. Un tattico con i rettangoli grigi risponde
perfettamente alla domanda *"è divertente spararsi?"*. Un tattico bellissimo senza netcode, no.

---

## 5 · Chi fa cosa, e quando

| Ruolo | Mesi 1–6 | Mesi 7–14 | Mesi 15–20 |
|---|---|---|---|
| **Programmatore netcode** *(primo assunto)* | Sim, netcode, lag comp | Ciclo del round, economia, deployabili | Anti-cheat, ottimizzazione |
| **Programmatore gameplay/UI** | Armi, hitbox, input | UI, HUD, buy phase, loadout | Matchmaking, backend, modalità Sito |
| **Artista** | Kit modulari, materiali, prop | Mappa, personaggi, viewmodel armi | Decorazione, VFX, UI art |
| **Audio** *(esternalizzato, dal mese 6)* | — | SFX, passi, ambienza | Mixaggio, musica |
| **PM / designer** *(part-time, dal mese 8)* | Design (fatto) | Playtest, bilanciamento | Bilanciamento, lancio |

**Regola di assunzione:** il **netcode engineer** assume per primo e non si esternalizza mai. È l'unica
competenza che, se manca, non si compra in ritardo.

---

## 6 · Ordine tecnico delle dipendenze

Non è possibile parallelizzare tutto. Questa è la catena reale:

```
Sim  →  Netcode  →  Ciclo del round  →  Economia  →  Infrastruttura  →  Contenuti
 │                       │
 │                       └──→ Deployabili → Trappole/sensori
 │
 └──→ Armi/balistica  (parallelo, ma dipende dalla sim)

Arte:  ← può partire in qualsiasi momento, ma la mappa definitiva
          richiede il GRAYBOX VALIDATO (fine Fase 2)
```

**Cosa blocca cosa, in una riga:** *non puoi bilanciare prima di avere 5v5 con ping reale* (Fase 3).
Quindi **tutto il bilanciamento è concentrato dopo il mese 10**, ed è per questo che il Gate 4 può
bocciare un gioco che sembrava pronto.

---

## 7 · I quattro gate Go/No-Go

| Gate | Quando | Domanda | Passa se |
|---|---|---|---|
| **G0** | Mese 1 | Il motore regge? | I 3 test della Fase 0 |
| **G1** | Mese 4 | Spararsi è bello? | 5v5 fluida a 200 ms, morti comprensibili |
| **G2** | Mese 8 | Il round funziona? | Partita primo-a-5 completa, tempi di percorrenza corretti |
| **G3** | Mese 10 | **La gente ci gioca?** | 10 sconosciuti, partita completa, senza assistenza |
| **G4** | Mese 14 | **La gente ci torna?** | Abbandono <20% nei primi 3 round, 40% di ritorno |

**Un gate non si passa "quasi".** Se un criterio non è soddisfatto, le opzioni sono due: **tagliare
scopo** o **spostare la data**. Non esiste la terza opzione di "aggiustiamo dopo".

---

## 8 · I criteri di kill — quando fermarsi

Questa è la sezione che nessun piano contiene, ed è quella che ti fa risparmiare anni.

> **Si ferma il progetto, senza vergogna, se si verifica una di queste condizioni:**

| # | Condizione | Cosa significa |
|---|---|---|
| **1** | **G1 fallisce due volte** (dopo due tentativi di correzione, ~6 settimane) | Non è un problema di netcode: è che il combattimento non funziona. Nessuna quantità di contenuti lo salverà |
| **2** | **G4 fallisce** (la gente non torna) | Il gioco è tecnicamente riuscito e umanamente no. È il caso più comune e il più difficile da accettare |
| **3** | Il team scende sotto **2 persone** prima del mese 10 | La matematica non torna più: 290 pw diviso 1 è 5,6 anni |
| **4** | Si sono bruciati **12 mesi** senza aver passato **G2** | Il progetto è in ritardo strutturale, non congiunturale |
| **5** | Dopo il mese 12 il **costo mensile** supera il 70% della cassa disponibile | Non arrivi al lancio. Meglio chiudere con risorse che senza |

**Il kill non è un fallimento: è una decisione.** Un progetto fermato al mese 12 con una cassa ancora
piena è un'esperienza che ti lascia in piedi per il prossimo. Un progetto trascinato al mese 30 senza
soldi ti toglie anche quello.

---

## 9 · Il piano B sul motore

La Fase 0 può fallire. Ecco cosa succede, deciso **ora** e non nel panico del momento:

| Se fallisce | Conseguenza | Rimedio |
|---|---|---|
| **Test 1** (server headless su Flax) | Il rischio cade sull'asse più critico | **Si passa a Godot.** La sim è già indipendente dal motore: costa 2–3 settimane |
| **Test 3** (preset basso su Flax) | Non raggiungi la GTX 970 | **Si passa a Godot**: il renderer Mobile è la strada già pronta |
| **Test 2** (sim contaminata dal motore) | L'architettura è a rischio su qualunque motore | **Fermati e riprogetta il confine sim/render.** Non è un problema di motore: è di disciplina |

**Il punto importante:** la scelta del motore è **reversibile a costo basso** (2–3 settimane) perché
il design è engine-agnostico. **Questa è la cosa più preziosa che il progetto ha oggi.**

---

## 10 · Costi

### Sviluppo

| Voce | Stima |
|---|---|
| 3 persone × 20 mesi (costo aziendale, non stipendio netto) | **Il numero più grande del piano.** Da calcolare sul tuo contesto: è la voce che decide se il progetto esiste |
| Audio esternalizzato | €6.000–12.000 |
| Asset esterni (modelli, texture, animazioni fuori produzione) | €5.000–15.000 |
| Software e licenze | €1.000–2.000 (il motore è gratis sotto soglia) |
| Server di sviluppo e CI | €100–200/mese |
| **Legale**: EULA, privacy GDPR, verifica marchio | €2.000–5.000 |

### Costi ricorrenti a regime — e l'insidia che nessuno considera

| Giocatori concorrenti | Server necessari | Costo mensile |
|---|---|---|
| **500** | ~5 core | **€80–150** |
| **2.000** | ~20 core | **€300–500** |
| **10.000** | ~100+ core | **€1.500–2.500** |
| **10.000 su cloud con egress a consumo** | idem | **€8.000–15.000** ⚠️ |

> **La trappola della banda.** Con ~30 kbit/s per client, **10.000 giocatori = ~400 Mbit/s continui**.
> Su AWS o Azure l'egress si paga a GB e ti costa **5–10 volte** un host con banda inclusa.
> **Scelta obbligatoria: host con traffico flat** (Hetzner, OVH). È una decisione da prendere al
> mese 8, non al lancio, perché cambia l'architettura di allocazione.

---

## 11 · Il modello commerciale: la decisione che nessuno vuole prendere

Hai scelto **solo cosmetici, zero pay-to-win** — ed è giusto. Ma questo crea un problema strutturale:

> **Un 5v5 competitivo senza IP vive o muore sul matchmaking.** E il matchmaking ha bisogno di
> **massa critica**. Un gioco a pagamento senza notorietà non raggiunge la massa critica, e senza
> massa critica le code sono lunghe e il gioco muore — anche se è ottimo.

Due strade:

| | **Free-to-play + cosmetici** | **Early Access a pagamento** |
|---|---|---|
| Massa critica | ✅ La ottieni | ⚠️ Piccola, ma dedicata |
| Serve marketing | **Sì, tanto.** Senza, il F2P è invisibile | Meno: lo store di EA porta traffico |
| Ricavo | 2–4% di conversione, servono 20–50k giocatori attivi | **~5.000 copie a €14 = ~6 mesi di sviluppo di 2 persone** |
| Rischio | Code vuote, morte silenziosa | Comunità piccola, ma **finanzia lo sviluppo** |

**Raccomandazione: Early Access a prezzo basso (€12–15), con l'opzione di passare a F2P al 1.0.**
Non è la scelta più ambiziosa: è la scelta che **ti fa arrivare vivo al lancio**, e ti dà una comunità
reale a cui chiedere cosa sistemare. Con 5.000 giocatori puoi fare matchmaking. Con 0 giocatori e
un piano F2P, no.

---

## 12 · La lista dei tagli, in ordine di sacrificio

Se il tempo stringe, si taglia **in questo ordine** — dal meno doloroso al più doloroso.

| # | Taglio | Cosa costa |
|---|---|---|
| 1 | **Modalità "Sito" 4v4** | Niente: era una rete di sicurezza, non il gioco |
| 2 | **La quinta zona** (Gallerie) → si gioca su 4 | Un po' di varietà di round |
| 3 | **3 armi delle 15** → 12 | Meno varietà, ma il gioco resta completo |
| 4 | **2 specializzazioni delle 5** → 3 | Meno composizione emergente |
| 5 | **Una regione server** → solo EU | Ping peggiore fuori Europa, ma il gioco esiste |
| 6 | **Il Robot deployabile** | Una tattica in meno |
| 7 | **La finestra di drafting** | Si torna al blocco immediato del loadout |
| 8 | **La ladder classificata** → solo non classificata | Perdi la progressione vera. **Grave** |
| 9 | **Lo stato abbattuto** → morte secca | **Molto grave**: cambia il combattimento |
| 10 | **Il netcode a predizione** → server autoritativo senza predizione | **Non si taglia.** Il gioco è ingiocabile |

> **Le righe 9 e 10 non si toccano.** Il DBNO è ciò che rende possibile il trade, e senza predizione
> il movimento è ingiocabile. Se devi arrivare a quei tagli, **non stai tagliando: stai facendo un
> altro gioco**, e va detto.

---

## 13 · Il piano dei playtest

| Quando | Chi | Cosa misuri | Come |
|---|---|---|---|
| **Mese 4** (G1) | 2 team interni | Fluidità a 200 ms, comprensibilità delle morti | Occhi, non analytics |
| **Mese 8** (G2) | 10–20 conoscenti | Il round si chiude? I tempi tornano? | Cronometro + osservazione |
| **Mese 10** (G3) | **10 sconosciuti** | Si gioca senza assistenza? | Solo telemetria |
| **Mese 14** (G4) | 20–40, ripetuti | **Abbandono nei primi 3 round**, ritorno settimanale | Telemetria + interviste |
| **Mese 16–18** | 200–1.000 | Bilancia, code, crash | Dashboard |

**Le tre metriche che contano, e basta:**

1. **% di partite completate** (non abbandonate) — sotto il 70% c'è un problema strutturale
2. **Ritorno alla settimana successiva** — sotto il 30% il gioco non trattiene
3. **Tempo medio di attesa in coda** — sopra i 90 secondi, il matchmaking è rotto

Non misurare il K/D. Non misurare le uccisioni per partita. **Misura se le persone tornano.**

---

## 14 · Le prime 10 cose — questa settimana

Il piano serve a niente se non comincia. **Questa settimana, in quest'ordine:**

1. **Scarica e installa Flax** e compila l'editor dai sorgenti (serve .NET 10 SDK e VS 2026)
2. **Fai il Test 1**: un processo Linux headless che stampa un tick a 64 Hz. Senza finestra. **Cronometra**
3. **Fai il Test 2**: una console app C# con `Sim.step()` che gira senza motore. Zero riferimenti a Flax
4. **Se il Test 1 supera i 3 giorni**, apri il piano B: installa Godot 4.5 e rifai i test 1 e 2
5. **Crea il repository** con la struttura `sim/ shared/ client/ server/ tests/`
6. **Scrivi il movimento**: accelerazione, attrito, controllo aereo limitato, un rettangolo grigio
7. **Scrivi il test a 200 ms di ping** prima di scrivere il resto del gioco
8. **Blocca 3 giorni alla settimana** in calendario come "sviluppo, non riunioni". Non trattabile
9. **Apri un foglio con il burn rate**: quanto ti costa un mese di sviluppo. **È il numero che decide tutto il resto**
10. **Trova il secondo programmatore**, o accetta consapevolmente lo scenario a 29 mesi

---

## 15 · Riassunto in dieci righe

> 290 persona-settimane. **19–20 mesi con 3 persone**, 29 con 2, oltre 5 anni da solo.
> Alpha chiusa a 14 mesi. **La prima partita con sconosciuti al mese 10**, ed è la data da proteggere.
> Quattro gate, e un gate non si passa "quasi".
> Se G1 fallisce due volte, o G4 fallisce, si chiude — e si chiude con la cassa piena.
> La scelta del motore è reversibile in 3 settimane perché il design è engine-agnostico.
> L'arte si ritarda senza pietà: non risponde a nessuna domanda vitale.
> Server con banda flat, o l'egress ti mangia.
> Early Access a €14 per finanziare, F2P solo se hai marketing.
> I tagli si fanno nell'ordine scritto. Il DBNO e la predizione non si toccano.
> **E la prima cosa da fare non è scrivere codice: è cronometrare un server headless.**
