# 03 · Svolgimento del round

## Vista d'insieme

```
FASE 0   Pianificazione        una volta, in lobby
────────────── ciclo di round ──────────────
FASE 1   Buy Phase             max 40 s   (avvio anticipato a 5/5 pronti)
FASE 2   Approccio             0:00–0:30
FASE 3   Contesa della cassa   0:30–1:30
FASE 4   Apertura della cassa  15 s / 10 s / 15 s
FASE 5   Prelievo              3 s  ← beat pubblico
FASE 6   Trasporto             ~25 s
FASE 7   Avvio estrazione      5 s  ← faro pubblico
FASE 8   Assedio               120 s (90 s con Overclock)  ← il momento Ready or Not
────────────── fine round ───────────────
         Inter-round            15 s
```

**Round tipico: ~5:00. Tetto: 8:00. Partita: primo a 5 estrazioni.**

---

## FASE 0 · Pianificazione

*Una volta, prima del match. Non è un menu: è la fondazione sociale della partita.*

### 0.1 · Scelta del loadout *(durante il caricamento)*

- Ognuno sceglie **uno dei suoi 5 loadout**: pistola, specializzazione, consumabile fisso.
  **Si sceglie mentre la partita carica**, e resta **modificabile fino al via del round 1**.
- **Il loadout è pubblico:** entrambe le squadre lo vedono prima del round 1.
  Vedi due Decifratori nemici? Sai che correranno alla cassa. È la prima informazione della partita,
  ed è gratis per chi la sa leggere.

### 0.2 · Spesa personale

- Ogni giocatore spende i suoi **$1.000 personali**: la pistola arriva dal loadout, e i soldi coprono
  un'utility, **una piastra leggera** e quello che resta. **Nessuna approvazione** — sono soldi suoi.
- La squadra fissa la **Politica di Spesa** (vedi doc 04): chi può comprare élite, quanto, senza chiedere.
- **Per il solo-queue esiste una politica predefinita automatica**, altrimenti il gioco è ingiocabile
  con gli sconosciuti.

**Dinamica dietro:** un team con una dottrina di spesa reale gioca visibilmente meglio. È il premio
per giocare con gli amici — e va protetto, non annullato.

---

## FASE 1 · Buy Phase *(max 40 s)*

### Cosa si compra

| Fascia | Cosa | Regola |
|---|---|---|
| **Libero** | Utility, munizioni, piastre, filo, sensori (≤ $300) | Nessuna approvazione, istantaneo |
| **Élite** | Armi, Kit Decodifica, Robot, Overclock, Lockdown (> $300) | **1 click** di un compagno, oppure **5 s di silenzio** = assenso |

Ogni transazione finisce nel **rendiconto pubblico** della Buy Phase.

### I due poteri di squadra

#### Overclock · $700 · consumabile durevole

> **Si compra una volta e si conserva finché non lo usi.** Se non sei stato tu a estrarre, resta in
> cassaforte per il round successivo. Nessuno spreco, nessuna scommessa.

- **Effetto:** quando parte una *tua* estrazione, il tuo assedio scende da 120 a **90 s**.
- **Attivazione manuale:** al momento dell'"Avvio estrazione", chi avvia vede il prompt
  *"Overclock disponibile — usa?"*. Un click, in una finestra di 5 s.
- **Non si impila:** se ne può tenere **uno solo**. Comprarne due non dà 60 s.
- **Acquisto visibile, attivazione pubblica.** Il nemico sa che possiedi un Overclock e deve
  pianificare l'eventualità di un assedio più corto. Non sa *quando*.

*Perché l'attivazione è manuale e non automatica:* se scattasse da sola alla prima estrazione,
smetteresti di decidere. Con il click, ogni round in cui estrai diventa una domanda —
**lo brucio adesso, o lo tengo per il round che conta?**

#### Lockdown · $2.200 · decisione del round

> **Si compra in Buy Phase, ma si attiva quando il Nucleo è in gioco.** Chi l'ha comprato sceglie
> *se* e *quando* chiudere uno dei due accessi.

- **Finestra di attivazione:** dal **prelievo del Nucleo** fino all'**avvio dell'estrazione**.
  Dopo è troppo tardi: la partita è già indirizzata.
- **Uno solo per round, globale:** il primo che conferma in Buy Phase se lo aggiudica. Se non lo usi,
  **scade a fine round** — a differenza dell'Overclock.
- **Attivazione pubblica:** quando scatta, entrambe le squadre lo vedono e il punto si chiude
  **fisicamente** — un portone, una barriera, un cancello. Nessuna spiegazione testuale.
- **L'acquisto si vede nel rendiconto** appena avviene. Il nemico sa che esiste una porta che può
  chiudersi, e giocherà **tutto il trasporto sotto quella minaccia**.

**Perché è reattivo e non pre-committato:** nella versione pre-committata compravi *prima* di sapere
chi avrebbe tenuto il Nucleo. Era un lancio di dadi da $2.000, non una decisione. Nella versione
reattiva sai *chi* ha il Nucleo, *dove* si trova e *quanto* tempo gli resta — e decidi di conseguenza.
**È la differenza tra una scommessa e una mossa.**

| Situazione | Uso | Perché funziona |
|---|---|---|
| **Hai tu il Nucleo** | Chiudi il punto dove *non* stai andando | Neghi al nemico la possibilità di dividersi: deve concentrare tutto su un punto che hai preparato |
| **Ha il Nucleo il nemico** | Chiudi il punto verso cui *sembra* diretto | Lo costringi a una deviazione da ~25 s e lo rendi prevedibile |
| **Sei in svantaggio** | Chiudi per ridurre le opzioni | Ridurre la varianza è la mossa razionale del disperato |
| **Sei in vantaggio** | Non lo compri | Con il vantaggio vuoi flessibilità, non un punto solo |

> **Il costo nascosto:** chiudere una porta la chiude anche per te. Il Lockdown **toglie opzioni a
> entrambi** — è per questo che non è mai un vantaggio netto.

#### Robot deployabile · $1.200
Apre la cassa da remoto. Si piazza da copertura, si muove da solo, è distruttibile. Vedi Fase 4.

### La dinamica dietro

Ora hai due strumenti con **filosofie opposte**, ed è esattamente questo che li rende interessanti:

| | **Overclock** | **Lockdown** |
|---|---|---|
| Natura | **Risparmio durevole** | **Decisione del round** |
| Quando si compra | Quando vuoi, anche in anticipo | Solo in Buy Phase |
| Quando si usa | Al momento dell'estrazione | Dopo il prelievo |
| Se non lo usi | **Lo tieni** | **Lo perdi** |
| Effetto | −30 s sul tuo assedio | Chiude un accesso fisicamente |

Uno è un conto in banca, l'altro un grilletto. **E la tensione è che non puoi avere entrambi:**
$700 in cassaforte sono $700 che non hai speso per un fucile.

### Trovata mia, da validare

I 40 s sono un **massimo, non la norma**: se tutti e 5 premono "pronto", il round parte subito.
Nelle partite con pochi morti si scende a 15 s. Senza questo, 40 s × 6 round = 4 minuti di menu,
il 13% della partita.

---

## FASE 2 · Approccio *(0:00 – 0:30)*

- Le due squadre escono da **Porto Vecchio** (Nord) e **Forte San Verano** (Sud). Distanze identiche per costruzione.
- **La decisione:** sprint (18,5 s al centro, **rumorosissimo**, arma instabile per 2 s) o passo tattico
  (28,6 s, silenzioso, arma pronta).
- Sensori e filo spinato piazzati sugli approcci e sui fianchi.
- Il primo contatto di solito **non è visivo, è sonoro**. Chi corre regala il timing.

> **Il dilemma fondante:** arrivare primo o arrivare invisibile? Non puoi avere entrambi.

---

## FASE 3 · Contesa della cassa *(0:30 – 1:30)*

- Scontro 5v5 in Piazza Vecchia, **su due livelli**, CQB.
- **Il DBNO entra in scena subito:** il primo abbattuto crea una scelta atroce — spendere 5 s a
  rianimare (esposti) o abbandonare. Qui si vede se la squadra sa giocare.
- Le trappole della fase 2 scattano sugli approcci.
- Chi vince questa fase **controlla la cassa**, ma non ha ancora vinto nulla.

---

## FASE 4 · Apertura della cassa

| Metodo | Tempo base | Con la specializzazione | Esposizione | Costo | Rumore |
|---|---|---|---|---|---|
| **A mano** | **15 s** | **7,5 s** (Decifratore) | **Totale** — fermo, arma abbassata | $0 | Basso |
| **Kit Decodifica** | **10 s** | **5 s** (Decifratore) | **Totale** | $600 | Basso |
| **Robot deployabile** | **15 s** + 5 s di piazzamento | **10 s** + **1,5 s** (Ingegnere) | **Nessuna** — lo guidi da dietro un muro | $1.200 | **Altissimo** — motore, cingoli, sirena |

> **Il Decifratore è un moltiplicatore, non un'alternativa.** Dimezza il tempo di *ogni* metodo che
> scegli: 15→7,5 a mani nude, 10→5 col Kit. Non rende inutile il Kit Decodifica — **lo rende più
> forte**. È il tipo di sinergia che vuoi: **sovrapposizione invece di sostituzione**, così il loadout
> e l'economia si parlano.
>
> **E il Robot non vende velocità, vende sicurezza.** A mani nude apri in 15 s ma col petto scoperto;
> col Robot apri in 15 s **da dietro un muro**. L'Ingegnere è l'unico che le unisce: 10 s e zero esposizione.

**Il robot ti compra sicurezza, ma la paghi in rumore e denaro.** Annuncia a tutta la mappa che
qualcuno sta aprendo la cassa, costa il doppio del Kit, è distruttibile, e se lo distruggono hai
perso tempo *e* soldi.

**E non può trasportare.** Il contenuto va prelevato a mano (Fase 5).

> **Perché è design eccellente:** separa l'esposizione dall'impegno. Il robot elimina il rischio
> della fase 1 e **non elimina mai la necessità di mettere il corpo nel punto conteso**. Non esiste
> percorso che faccia vincere un round senza esporsi.

### Regola: la cassa, una volta aperta, resta aperta

Se ti eliminano alla cassa, il nemico **non deve riaprirla**. Da lì in poi si combatte solo per il
Nucleo. Senza questa regola si entra nel loop "riapri la cassa", che ammazza il ritmo.

---

## FASE 5 · Il prelievo — *il beat pubblico*

- **Channel di 3 s**, fermo, interrompibile dal danno. Completamente esposto.
- **Annuncio a entrambe le squadre: "NUCLEO PRELEVATO."** Sting audio, banner HUD.
  È una chiamata radio, ed è giusto che sia pubblica.
- **Il portatore:** cammina soltanto (niente sprint), **nessuna arma primaria** (solo pistola),
  non può fare vault. È la persona più importante della partita.

> **Il cuore del gioco:** il prelievo è pubblico, **la destinazione è segreta.**
> Il nemico sa che il Nucleo si muove, non sa dove. A meno che il Lockdown non abbia ridotto
> le opzioni a una. **È la guerra dell'informazione nella sua forma più pura, e dura 30 secondi.**

**Se il portatore cade, il Nucleo finisce a terra** → mischia per rialzarlo.

> **Il Nucleo non torna mai indietro.** Resta dove cade: chi lo vuole, deve andare a prenderlo.
> Non esiste un timer di ritorno — il recupero è una **scelta**, non un automatismo, e questo
> aggiunge un'opzione tattica che prima non c'era: **lasciare il Nucleo a terra apposta**
> per attirare il nemico in una zona che hai già preparato.

**Requisito di level design:** il Nucleo non può mai cadere in spazio non giocabile (mare aperto,
fuori mappa, volumi sigillati). La posizione di caduta viene **agganciata lato server al punto
navigabile più vicino**, così l'oggetto è sempre recuperabile. Serve anche un margine di 2 m dai
bordi delle superfici elevate: un Nucleo caduto da una terrazza non deve finire irraggiungibile.

---

## FASE 6 · Trasporto *(~25 s)*

- Il portatore e la scorta vanno verso Est o Ovest.
- **Scelta di rotta:** attraverso il centro (22,6 s, esposto) o **via anello** (52 s, sicuro).
  Il portatore può **fintare** verso Est e tagliare a Ovest. Vera guerra psicologica.
- Il nemico deve scegliere: **intercettare** (rischioso: se non lo trova, ha perso posizione) o
  **ripiegare e preparare** (sicuro: ma lo lascia arrivare).
- **Le trappole piazzate prima adesso pagano.** Un filo spinato sull'anello rallenta il portatore
  abbastanza da renderlo raggiungibile.
- **La minaccia del Lockdown incombe su tutto il trasporto.** Se il nemico l'ha comprato, in
  qualunque momento una delle due destinazioni può chiudersi. Il portatore non può permettersi di
  essere lento: **ogni secondo che passa è un secondo in cui l'avversario può togliergli una porta.**

> Questa è la parte spaziale, alla Arma. È la ragione per cui la mappa ha un anello.

---

## FASE 7 · Avvio estrazione *(5 s)*

- Il portatore (o un compagno) avvia l'estrazione al punto.
- **Ultima chiamata per il Lockdown:** in questo istante la finestra di attivazione si chiude.
  Da qui in poi le due destinazioni restano quelle che sono.
- **Prompt Overclock:** chi avvia vede *"Overclock disponibile — usa?"*. Un solo click per portare
  l'assedio da **120 a 90 s**. È il momento in cui si capisce se il tuo investimento valeva.
- **Faro pubblico:** colonna di fumo + sirena, visibile da tutta la mappa. Entrambe le squadre sanno.
- È una dichiarazione: *"Sono qui. Venite a prendermi."* I numeri sono fissati.

---

## FASE 8 · Assedio *(120 s · 90 s con Overclock)* — **il momento Ready or Not**

### Le regole

- La squadra che tiene il Nucleo deve **mantenerlo sul punto per 120 s**.
- L'altra squadra deve **raggiungere fisicamente il Nucleo e rimuoverlo** (channel di **5 s**) → l'estrazione
  si annulla e il Nucleo torna conteso. **Oppure** fare **team wipe**.
- **Rimuovere il Nucleo durante l'assedio chiude subito il round.** *(trovata mia, da validare — senza
  questa regola si entra in un ping-pong che sfonda gli 8 minuti)*

### Perché è un assalto alla Ready or Not

L'assaltante non deve "passare di lì": deve **entrare in una stanza, arrivare a un oggetto e portarlo
via**. Impilarsi sulle entrate, **aprire una fessura per guardare dentro**, specchio sotto la porta,
sensore per capire quanti sono dentro, flash, aprire e ripulire la stanza col Nucleo.

### Le variabili che lo rendono diverso ogni volta

| Variabile | Effetto |
|---|---|
| **Nessun respawn** | I numeri sono fissati all'inizio dell'assedio. Perdere 2 uomini nei primi 20 s è una condanna |
| **Trappole** | Filo spinato sulle entrate, sensori che segnalano l'arrivo del retake, mina direzionale a copertura del Nucleo (col suo tell) |
| **DBNO** | Un compagno a terra sulla porta del Nucleo, 40 s rimasti: lo rianimi e perdi la posizione, o lo lasci morire? |
| **Geometria del punto** | Est = chiuso, un ingresso, fianco marino. Ovest = lungo, ingressi multipli, linee lunghe. **Due giochi diversi** |
| **Lockdown** | Se è stato comprato, entrambe le squadre sapevano dove si sarebbe combattuto e hanno avuto 60 s per prepararsi |

---

## Condizioni di fine round

| # | Condizione | Esito |
|---|---|---|
| 1 | **Estrazione completata** (120 s / 90 s) | Vince chi estrae |
| 2 | **Team wipe** — una squadra non ha più nessuno vivo né rianimabile | Vince la superstite |
| 3 | **Rimozione del Nucleo durante l'assedio** | **Chiude subito il round**: vince chi ha rimosso |
| 4 | **Timer di round 8:00 scaduto** senza che un'estrazione sia stata completata | **Round nullo**: nessun punto, introiti ridotti per entrambi |
| 5 | **Tetto partita 60:00** | Vince chi ha più estrazioni. In parità → round di spareggio con Overclock forzato |

### Regola del timer

> **Il timer di round si sospende mentre un'estrazione è attiva** — l'assedio ha il suo timer.
> Un round non può quindi morire a metà assedio.

### Perché il round nullo e non un vincitore

Non conviene a nessuno stallerare: chi insegue non guadagna nulla e ci perde soldi. Il round nullo
è una punizione reciproca che rende la passività irrazionale. È la cura più economica al camping.

**Ed è più importante che mai ora che il Nucleo non torna alla cassa:** se entrambe le squadre
lasciano il Nucleo a terra per non esporsi, l'unico esito possibile è il round nullo.
**L'unica cosa che il timer punisce è la passività, e la punisce simmetricamente.**

---

## Inter-round *(15 s)*

Scoreboard + **rendiconto economico** (chi ha speso cosa, quanto resta in cassa) + transizione.
Passa direttamente alla Buy Phase successiva.

---

## L'economia in azione: un round raccontato

*Situazione: 2-3 per la squadra Nord, round 6. La cassa comune ha $1.400.*

- **Buy Phase.** Nord ha 3 sopravvissuti armati (un **Service**, una **Cadence**, un **Vantage**) e 2 morti
  nel round precedente. Con $1.400 non può riarmare entrambi: compra **una Cadence ($900)** e la dà al
  giocatore più utile, e **un Kit Decodifica ($600)**… no, non ci sta.
  → Scelta: Cadence + utility, il secondo resta con la pistola. **Forced ECO involontario.**
- **Approccio.** Il giocatore con la pistola va sull'anello come vedetta, gli altri al centro.
- **Contesa.** Nord perde il Vantage (morto) ma vince la piazza 3v1.
- **Apertura.** Kit Decodifica? Non comprato. **10 s a mani nude**, esposti. Il Sud arriva tardi.
- **Prelievo.** Pubblico. Sud sa che il Nucleo si muove, non dove.
- **Trasporto.** Finta verso Est, taglio verso la Galleria via Mura. **Sud ha il sensore sulle Mura.**
- **Avvio.** Faro. Sud converge.
- **Assedio.** 3 Nord (uno con pistola) contro 4 Sud. Il filo spinato comprato prima copre l'ingresso
  laterale: Sud perde 8 secondi. L'assedio scade. **Nord estrae. 3-3.**

Ogni singola decisione economica presa in quei 40 secondi ha cambiato l'esito.
