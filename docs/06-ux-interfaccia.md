# 06 · UX e interfaccia

## Principio guida

> **L'interfaccia mostra solo ciò che il giocatore deve *decidere*.**
> Tutto ciò che è informativo ma non decisionale va in un pannello secondario, non sul HUD.

In un gioco senza respawn, ogni pixel di HUD che copre il campo visivo è un danno.

---

## HUD di gioco

### Sempre visibile (angoli, mai centro)
| Elemento | Posizione | Note |
|---|---|---|
| Salute + stato piastre | basso sinistra | Piastre come 2–3 segmenti che si svuotano |
| Munizioni | basso destra | Caricatore attuale / riserva |
| Utility disponibili | basso destra, sopra le munizioni | Icone piccole, con tasto |
| Compagni vivi / abbattuti | alto sinistra | **Il dato più importante del gioco** |
| Timer di round | alto centro | **Si sbiadisce e pulsa** quando un'estrazione è attiva |

### Visibile solo quando serve
| Elemento | Quando |
|---|---|
| Portatore del Nucleo | Solo per la **tua** squadra (icona discreta) |
| Annuncio "NUCLEO PRELEVATO" | 3 s, banner grande, sting audio |
| Faro di estrazione | Indizio ambientale (fumo + suono), **nessuna icona HUD** |
| Ping del sensore | Zona sbiadita sulla minimappa, 2,5 s |
| **Overclock in cassaforte** | Icona discreta accanto al timer, solo per la squadra che lo possiede |
| **Lockdown disponibile** | Indicatore persistente per chi l'ha comprato, **dal prelievo fino all'avvio dell'estrazione** (è la finestra utile) |
| **Attivazione del Lockdown** | Banner pubblico a entrambe le squadre + chiusura fisica visibile del punto |
| Rianimazione / channel | Barra circolare al centro, sottile |

### Esplicitamente assenti
- **Nessuna minimappa con nemici.** Solo l'area pingata dai sensori e i compagni.
- **Nessun marcatore sul Nucleo nemico.** Mai.
- **Nessun indicatore di danno direzionale "a torta"** in stile arcade — solo una **direzione vaga**
  (un arco), perché in un gioco dove il suono è un sistema, dare la direzione esatta annulla il lavoro
  sull'audio.
- **Nessun kill feed** con nomi e icone. Solo un contatore di compagni vivi: la morte è già abbastanza
  comunicata.
- **Nessun hit marker aggressivo.** Un feedback visivo minimo, nessun "X" gigante, nessun suono
  soddisfacente di uccisione: non è un gioco dove uccidere è divertente, è un gioco dove uccidere è pesante.

---

## Buy Phase — l'interfaccia più importante del gioco

Ha 40 secondi per far collaborare 5 estranei. Deve essere **leggibile in 5 secondi**.

```
┌──────────────────────────────────────────────────────────────────┐
│  CASSA COMUNE   $4.320              ROUND 6/…      NORD 3 — 2 SUD │
├──────────────────────────────────────────────────────────────────┤
│  Politica: chiunque fino a $1.000 · oltre serve 1 approvazione    │
├───────────────────────────────┬──────────────────────────────────┤
│  LIBERO  (≤ $300)             │  ÉLITE  (> $300)                 │
│  [Filo spinato]     $200      │  [Kit Decodifica]    $600  !     │
│  [Sensore acustico] $250      │  [SMG]             $900  !       │
│  [Medikit]          $250      │  [Fucile d'assalto] $1400 !      │
│  [Mina laser]       $300      │  [LOCKDOWN]        $2200 !       │
│                               │  [Overclock]        $700  ! (1)  │
├───────────────────────────────┴──────────────────────────────────┤
│  IL TUO CARICO:  SMG (conservata) · Piastra leggera · 1 flash    │
│  [ PRONTO ]                    Timer: 27s                        │
└──────────────────────────────────────────────────────────────────┘
```

### Quello che esiste oggi (stanza debug)

Il pannello c'è, si apre con **B**, ed è la versione a un giocatore della schermata qui sopra:
niente cassa comune né approvazioni (arriveranno col round), ma la struttura è quella.

```
┌───────────────────────────────────────────────────────────────┐
│  ARSENALE                                     $1.000          │
│  una sola primaria e una sola secondaria · B per chiudere     │
├──────────────────────────────┬────────────────────────────────┤
│  SECONDARIE · pistole        │  PRIMARIE · SMG, pompe, AR     │
│  Standard  25 · 300 c/min    │  Cadence  20 · 800 c/min  $900 │
│   17/51 · gratis   [IN MANO] │   30/90 · punto rosso  [COMPRA] │
│  Hush      22 · 273 c/min    │  …                             │
├──────────────────────────────┴────────────────────────────────┤
│  IN MANO Cadence · PRIMARIA Cadence · SECONDARIA Standard · COLTELLO │
│  click su un'arma per comprarla e metterla in mano            │
└───────────────────────────────────────────────────────────────┘
```

- **Una primaria e una secondaria.** Comprarne un'altra dello stesso tipo prende il posto della
  precedente, che viene **consumata** (doc 04: «la vecchia viene consumata»). La Standard e il
  coltello non si consumano mai: quelli ci sono sempre.
- **Si spawna con la Standard e il coltello**: un loadout nuovo non ha primarie, e nessuna arma
  arriva gratis. Dopo l'acquisto si spawna con quello che hai in mano.
- **In debug i soldi sono illimitati** e il contatore mostra `∞`.
- Ogni riga porta i numeri che contano per scegliere: danno, cadenza, caricatore/riserva, ADS,
  ottica. Il prezzo è a destra, e diventa rosso quando non te lo puoi permettere.
- I tasti **1 / 2 / 3** sono i tre posti (primaria, secondaria, coltello); la **rotella** gira solo
  fra quelli che hai.

### Flusso di approvazione

```
Tu clicchi "Fucile d'assalto $1400"
      │
      ├── Dentro la politica → va a buon fine, il rendiconto si aggiorna
      │
      └── Fuori politica → banner non bloccante per i compagni:
                        "Kappa chiede: Fucile d'assalto $1400  [OK] [NO]"
                          │
                          ├── Un compagno clicca OK → acquisto eseguito
                          ├── Passano 5 s di silenzio → assenso automatico
                          └── Un compagno clicca NO → annullato (tracciato nel rendiconto)
```

**Regole UX critiche:**
1. **Il banner di richiesta NON blocca la schermata.** Chi chiede continua a comprare utility.
2. **Il rendiconto è sempre visibile in basso**: chi ha speso cosa, in ordine cronologico.
   *La vergogna pubblica è una meccanica, e va progettata.*
3. **Il silenzio-assenso è mostrato come countdown**, non nascosto: così nessuno può dire "non
   avevo capito".
4. **L'annullamento (rimborso) è un solo tasto** sulle cose comprate in questa Buy Phase.

### Rendiconto di fine round

Prima della Buy Phase successiva, un pannello di 5 secondi obbligatorio:

```
ROUND 6 — PERSA
Spese del round:   Kappa $1400 (fucile) · Vera $600 (kit) · Leo $450 (utility)
Sopravvissuti:     Vera, Leo, Rokka → attrezzatura conservata
Deceduti:          Kappa, Nile → riarmare ($1.400+ di costo stimato)
Cassa dopo gli introiti: $4.320
```

**Questo pannello è il cuore sociale del gioco.** Non è un log: è il momento in cui la squadra
si guarda in faccia.

---

## Menù e flussi

```
AVVIO
 ├── Rapida          → coda non classificata, primo a 5
 ├── Classificata     → coda con MMR, primo a 5
 ├── Allenamento      → poligono di tiro + tutorial delle fasi
 └── Personalizza     → mirino, sensibilità, HUD, audio

PRE-PARTITA
 ├── Lobby (5 giocatori, 60 s)
 │    ├── Scelta della Politica di Spesa
 │    └── Spesa del fondo personale ($1.000)
 └── Caricamento mappa + planimetria

POST-PARTITA
 └── Riepilogo: estrazioni, economia totale, grafico dei round, MVP per obiettivo
```

---

## Impostazioni competitive (non negoziabili)

| Impostazione | Perché è obbligatoria |
|---|---|
| **Sensibilità separata per ogni livello di zoom** | Standard dei tattici. Senza, le ottiche sono ingiocabili |
| **Editor del mirino** (forma, spessore, colore, gap, punto centrale, contorno) | Assoluto obbligo competitivo |
| **FOV viewmodel separato dal FOV di gioco** | I giocatori pro lo richiedono sempre |
| **Raw input / disattivazione dell'accelerazione del mouse** | Obbligatorio |
| **Limite di FPS + vsync + modalità a schermo** | I pro disattivano tutto: va supportato |
| **Netgraph** (ping, jitter, tickrate, perdita pacchetti, correzioni di riconciliazione) | Diagnostica e trasparenza |
| **Audio HRTF / occlusione** | Il suono è un sistema di gioco: deve essere preciso |
| **Ricerca partita per ping massimo** | Con server regionali |

### Accessibilità
- **Modalità daltonici** (protanopia, deuteranopia, tritanopia) con palette di squadra dedicate
- **Contorni nemici opzionali** (2 px, attivabili) — *non* un vantaggio, una compensazione dichiarata
- **Scala HUD** e dimensione dei font
- **Sottotitoli per tutti gli avvisi sonori di gioco** (prelievo, faro, porta forzata) — cruciale per
  i non udenti, dato che il suono trasmette informazione tattica
- **Opzione "riduci effetti"** (sangue, particelle) senza penalità di prestazioni

---

## Onboarding

**Il problema:** un tattico senza respawn con economia condivisa è ostile ai nuovi. Se non lo
risolvi, il gioco muore con 500 giocatori.

| Strumento | Contenuto |
|---|---|
| **Poligono di tiro** | Armi, rinculo, tempi di uccisione. Nessuna pressione |
| **Tutorial guidato** | Un round scriptato: apri la cassa, preleva, trasporta, difendi |
| **Tutorial economia** | Spiega la cassa comune con una simulazione, non con un muro di testo |
| **Tutorial difensivo** | Come si impila una squadra, come si apre una porta, come si usa lo specchio |
| **Partite con bot** | Riempimento della coda con IA a difficoltà crescente dopo X secondi |
| **Modalità "Sito"** | Formato compresso 4v4 su una sola zona, round da 3 minuti — l'entry point rapido |

> **La modalità "Sito" è la tua rete di sicurezza commerciale.** Serve ai nuovi giocatori, ai
> giocatori con poco tempo, e come warmup. La mappa è la stessa: costo di produzione quasi nullo.

---

## Sociale

| Funzione | Note |
|---|---|
| Party (fino a 5) | La politica di spesa si eredita dal party leader |
| Ricongiunzione | **120 s** per rientrare in partita dopo una disconnessione |
| Segnalazioni | Categorie specifiche: ostruzionismo economico, team-kill, abbandono |
| Blocco della coda | Chi abbandona 2 volte in 24 h entra in coda con altri abbandonanti |
| Testo/voce | **Voce obbligatoria consigliata** — il gioco è sociale per design, ma non forzata |
| Comandi rapidi radiali | "Copri", "Spingi", "Ripiega", "Ho il Nucleo", "Serve rianimazione" |

I comandi rapidi contano: **un gioco che richiede coordinazione deve permetterla anche a chi non
usa il microfono.**


---

## Il riscontro del colpo (stanza debug)

Quattro cose, tutte **immediate** e tutte spente in meno di mezzo secondo — perché il riscontro non
deve mai coprire il bersaglio:

| Segnale | Quando | Come |
|---|---|---|
| **Segno del colpo** | ogni palla a segno | quattro trattini attorno al mirino, bianchi; **rossi e più larghi** quando il colpo uccide |
| **Numero del danno** | ogni palla a segno | numero 3D sul punto d'impatto, sale di 0,85 m/s e svanisce in 0,9 s; **giallo con "!"** per la testa |
| **Kill feed** | ogni abbattimento | riga in alto a destra: bersaglio, come, punti; resta 3,2 s e sfuma |
| **Punteggio** | sempre | in alto: `N abbattuti · N punti · N in piedi` (100 punti per un corpo, 150 per la testa) |

**Barra della vita** sopra ogni bersaglio: verde → gialla → rossa, si accorcia da destra, sparisce
quando è a terra. È un quad che guarda sempre la camera: si legge da qualsiasi angolo.

**Rinculo della camera.** Ogni colpo alza la mira di `0,42° + 0,045° × danno` (una pistola ~1,5°, il
cecchino ~4,6°) e la sposta di lato in modo casuale; in mira è il 55%. Rientra da solo, sempre più
lentamente. **Si spara dove si vede**: la direzione delle palle include il rinculo, quindi quello che
il mirino mostra è quello che la simulazione traccia.

**Lampo di volata.** Una luce vera (`OmniLight3D`, 7 m di raggio) alla bocca della canna per 45 ms —
20 ms e metà energia per le armi silenziate. Non è un cartellino: illumina il muro davanti.

**Dove finiscono i colpi.** Ogni palla che prende un muro lascia un **foro**: buca scura, orlo
scheggiato, polvere attorno, crepe raggiate. Resta dodici secondi e poi si dissolve: si vede dove
hai sparato senza che la mappa diventi un colabrodo. Il foro è un quad **rivolto alla camera** (`ShotHit.Normal` serve per la polvere e per appoggiarlo 6 mm fuori): non si vede mai di taglio, quindi né strisce sul muro né buchi "stirati", e uno sparo sul pavimento resta a terra.

**Il cambio d'arma non spara.** I contatori degli eventi (`Shots`, `ReloadStarts`) vivono **nell'arma**:
quando la riprendi in mano sono già avanti. Il client li **adotta** al primo frame invece di
azzerarli — azzerarli faceva credere che fosse partito un colpo, e al primo 1/2 si sentiva uno
sparo fantasma (con traccia, bossolo, lampo e rinculo).


---

## Il kit della UI (una sola, in tutto il gioco)

Tutta l'interfaccia nasce da due pezzi soli:

| Pezzo | Cosa fa |
|---|---|
| `UiKit` | Colori (notte, corallo, ciano, oro), scala responsive, testo (titolo/corpo/didascalia/tag), bottoni (pieno e vetro), divisori, barre delle statistiche, righe chiave-valore, etichetta del tipo di fuoco |
| `PanelStyle` / `CardStyle` | Pannelli **pieni** (fondo notturno, bordo sottile, angoli morbidi) e schede, con bordo d'accento quando sono selezionate. Niente shader: la UI si legge sopra qualunque scena e non dipende da copie dello schermo |

> Il blur dietro i pannelli (`GlassPanel` + `glass.gdshader`) è pronto ma **non è in uso**:
> nell'ultima prova i pannelli risultavano invisibili (il nodo partiva 0×0 e lo shader
> rendeva alfa 0). Prima di riattivarlo va verificato in motore; finché non lo è, la UI
> usa pannelli pieni, che sono leggibili sempre.

**La scala.** Ogni misura nasce su un disegno di 1600×900 e viene moltiplicata per
`min(larghezza/1600, altezza/900)` (fra 0,72× e 1,6×): la UI sta dentro lo schermo in 4:3, 16:9 e
21:9 senza elementi tagliati. I margini sono percentuali, non pixel fissi.

**Dove si usa.** Negozio (`ShopPanel`) e impostazioni (`SettingsPanel`) sono già sul kit; HUD e menu
useranno gli stessi pezzi. Il negozio è **lo stesso pannello ovunque**: si apre dalla stanza debug,
dal menu e — quando ci sarà — dalla schermata di acquisto del round.

**La schermata di debug si apre con F8.** La colonna con dati, teleport, cronometro e log parte
nascosta: in gioco si vede solo l'HUD (punteggio, arma, munizioni, mirino). `F8` la apre e la
chiude, e il punteggio torna a filo del bordo quando è chiusa.

## Il negozio (B)

Rifatto sul modello di Valorant: **colonna delle categorie** a sinistra, **griglia di schede** al
centro, **pannello dei dettagli** a destra.

| Zona | Cosa c'è |
|---|---|
| **Intestazione** | `NEGOZIO`, la regola ("un'arma per slot"), i soldi (∞ in debug) e `B per chiudere` |
| **Categorie** | PISTOLE · MITRAGLIETTE · FUCILLI A POMPA · FUCILLI D'ASSALTO · CECCHINI · ARMI PESANTI · MISCHIA |
| **Schede** | ritratto dell'arma (render laterale 512×256 con sfondo trasparente, `game/assets/weapons/ui/`), nome, classe, **danno, cadenza, colpi e ricarica** (`ric. 1,95/2,35 s`), stato (`IN MANO` / `TUA`) |
| **Dettagli** | ritratto grande, nome, barre di **danno, cadenza, precisione, caricatore**, **tempo di mira**, **ricarica tattica e a vuoto**, ottica, tipo di fuoco, prezzo e pulsante `ACQUISTA` |

Un click sulla scheda la seleziona (i dettagli si aggiornano), il doppio click compra. Il pulsante
dice sempre perché è disabilitato (`SOLDI INSUFFICIENTI`, `IN MANO`). Le regole non cambiano: **una
sola primaria e una sola secondaria**, la Standard e il coltello ci sono sempre, i soldi in debug
sono infiniti.
