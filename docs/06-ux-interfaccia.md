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
