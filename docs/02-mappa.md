# 02 · La mappa

**Calanera** — città costiera italiana immaginaria, penisola con fortezza, porto vecchio e grotte marine.

---

## La regola che tiene in piedi tutto: l'anello

La pianta è **una croce con un anello**, non una croce.

> **Ogni squadra deve poter raggiungere ogni punto di estrazione con almeno 2 rotte, di cui almeno
> una che NON passa per il centro esatto.**

Senza l'anello, chi controlla la piazza controlla il round, il retake diventa impossibile e ogni
partita si decide nella stessa sparatoria. L'anello è la condizione di esistenza del gioco.

L'anello serve anche a **fare da intercettazione**: chi trasporta il Nucleo può scegliere la rotta
interna (veloce, esposta) o l'anello (lenta, sicura), e la scelta è la fase tattica centrale.

---

## Geometria — lo schema a corsie (versione di ottobre 2026)

> **Revisione:** la prima pianta (croce + anello di strade dritte, 240 × 220 m) era troppo grande,
> troppo "a blocchi" e piena di corridoi lunghi inutili. La pianta attuale segue lo schema delle mappe
> di **Valorant**: da ogni spawn partono **tre corsie corte e spezzate**, ognuna con una
> **strozzatura** e uno **spazio aperto** prima dell'obiettivo; i collegamenti fra le corsie sono
> pochi, corti e voluti. L'anello non è più una strada dedicata: **nasce dalle corsie laterali**.

```
                              PORTO VECCHIO (spawn Nord)
                 Via Stretta ┘      │ Salita      └ Banchina
                     │              │                 │  Varco (strozzatura)
        Piazzetta del Forno ── LARGO ── Cortile della Dogana
                     │        (gradinata)          │
                 PIAZZALE         Arco         MOLO ── Terrazza
                     │        Via del Mercato      │
   GALLERIA ─ Cortile O ─ Mercato ─ PIAZZA ─ Mercato ─ Cortile E ─ GROTTA
   VECCHIA                       (banca, cassa,                 DEL LEVANTE
                     │            porticato)        │
                   MOLO ...       (ruotato)      ... PIAZZALE
                                     │
                            FORTE SAN VERANO (spawn Sud)
```

| Corsia (da Nord) | Percorso | Carattere |
|---|---|---|
| **Centrale** | Salita → **Largo** (fontana, gradinata a 1,5 m) → **Arco** coperto → Via del Mercato a gomito → Piazza | Nessuna linea dritta dallo spawn alla cassa |
| **Est — Scalinata Lunga** | Banchina → **Varco della Dogana** (6 m, coperto) → Cortile della Dogana → **Molo** (+ Terrazza a 3,4 m) → Grotta | Aperta, a quote, la terrazza domina l'ingresso nord della Grotta |
| **Ovest — Via Stretta** | Via Stretta a gomito (4 m) → **Piazzetta del Forno** (il forno è una stanza attraversabile) → **Piazzale** → Galleria | Ravvicinata, angoli e porte |

La metà Sud è la Nord ruotata di 180°: la Scalinata ruotata diventa le **Mura** (verso la Galleria),
la Via Stretta ruotata diventa il **Lungomare** (verso la Grotta).

**Rotazioni (le "link" di Valorant):** dal **Largo** due passaggi coperti verso le corsie laterali (il
lato di chi attacca); dalla **piazza** due collegamenti verso le estrazioni — il **Mercato coperto**
(sala a pilastri) e un **cortile** — (il lato di chi difende).

**Ogni estrazione ha tre ingressi:** uno per squadra (dal Molo / dal Piazzale) e uno dal centro.

### Dimensioni

| Elemento | Valore |
|---|---|
| Spawn Nord / Sud | **91 m** dal centro |
| Estrazione Est (Grotta del Levante) | **77 m** |
| Estrazione Ovest (Galleria Vecchia) | **72 m** |
| Footprint totale | **180 × 200 m** |

### Tempi di percorrenza (misurati camminando con la simulazione, passo 4,2 m/s)

| Tratta | Passo tattico | Sprint (senza stamina) |
|---|---|---|
| Spawn → cassa (corsia centrale) | **22,7 s** | ~14,7 s |
| Cassa → Grotta / Galleria (via Mercato) | **20,2 s / 19,6 s** | ~13 s |
| Spawn → estrazione per la corsia laterale, senza piazza | **34,6 – 36,0 s** | ~23 s |
| Grotta ↔ Galleria senza piazza (giro dalle corsie) | **59,4 s** | ~38 s |

**I tempi delle due squadre coincidono entro il 3%** — imposto dalla simmetria rotazionale e
verificato a ogni generazione della mappa.

---

## Le 5 zone

### 1 · Porto Vecchio — *spawn Nord*
Banchine, container, gru arrugginite, reti, barche in secca. Approccio aperto con molto riparo
basso e linee di tiro medie. Il contrasto: questo è il lato "luminoso" e industriale della mappa.

- **Accessi al centro:** 2 (Via Stretta e Scalinata)
- **Accessi all'anello:** Lungomare (est), Mura (ovest)
- **Pericolo:** attraversare il piazzale del porto in sprint è visibile da metà mappa

### 2 · Forte San Verano — *spawn Sud*
Fortezza su terrazze, quota rialzata, vista sul paese. Interni in pietra, camminamenti, feritoie.
Approccio verticale: si scende verso la piazza.

- **Accessi al centro:** 2 (Mura est, Lungomare ovest)
- **Accessi all'anello:** Via Stretta (est), Scalinata (ovest)
- **Pericolo:** la quota dà vantaggio di vista ma crea **minaccia a 360°** — è la zona che più
  facilmente diventa frustrante. Va limitata a **2 livelli combattibili**, il resto è skyline.

### 3 · Piazza Vecchia — *centro, la cassa*
Piazza su due livelli, porticato, fontana, la vecchia banca con la cassaforte. **La cassa del
Nucleo è nella cassaforte della banca.** Quattro approcci (due dai portici, due laterali),
più il livello superiore del porticato.

**Regole di design obbligatorie:**
- **Non deve essere una killzone singola.** Almeno 3 linee di tiro che si incrociano e almeno
  4 ripari che rompono la visuale.
- **La banca deve essere difendibile ma non una fortezza.** Se chi arriva primo alla piazza vince
  sempre, il round è deciso in 30 secondi.
- **Deve avere 2 livelli combattibili.** La piazza del riferimento ce li ha già: sfruttali.

### 4 · Grotta del Levante — *estrazione Est*
Grotta marina con imbocco sul mare, banchina di pietra, un motoscafo. **Un solo ingresso
frontale, buio, corridoi stretti** — ma una **rotta di fiancheggiamento via mare** (bagnasciuga
allagato, lento, rumoroso).

**Carattere dell'assedio:** claustrofobico. Pochi angoli, granate decisive, il fianco marino è
la variabile. Il difensore ha un vantaggio forte sul primo accesso.

### 5 · Galleria Vecchia — *estrazione Ovest*
Tunnel stradale dismesso, nicchie di manutenzione, condotti di ventilazione, un convoglio.
**Ingressi multipli, linee di tiro lunghe.**

**Carattere dell'assedio:** aperto. L'assaltatore ha più opzioni, il difensore deve coprire di più.
Il contrario esatto della Grotta.

> **Regola:** Est e Ovest devono essere **due giochi diversi**. Se l'assedio alla Grotta e quello
> alla Galleria si giocano allo stesso modo, 120 secondi × 5 round diventano ripetitivi.

---

## Connettori (l'anello)

| Zona | Ruolo | Carattere |
|---|---|---|
| **Via Stretta** | Connettore NE–SO | Vicolo in forte pendenza, CQB, zero linee lunghe |
| **Scalinata Lunga** | Connettore NO–SE | Verticale, scoperta, lenta a salire, veloce a scendere |
| **Lungomare** | Anello est | Aperto, balaustre, mare a strapiombo. Esposto ma corto |
| **Mura** | Anello ovest | Camminamento sulle mura, coperto, tortuoso, lento |

---

## Regole di level design

1. **Simmetria architettonica, asimmetria cosmetica.** Layout ruotato di 180°, decorazione
   completamente diversa tra nord e sud. Il giocatore deve *sentire* di essere in due posti diversi
   e *sapere* che è bilanciato. **Si costruisce mezzo paese e si ruota: metà del tempo di produzione.**
2. **Nessun punto cieco irrisolvibile.** Ogni corridoio ha un'uscita a ogni estremità.
3. **Ogni zona ha un landmark visivo** identificabile a colpo d'occhio da qualunque punto della mappa
   (la gru del porto, la torre del forte, il campanile della piazza). Serve sulla mappa mentale
   del giocatore e costa zero.
4. **Ogni approccio ha almeno un riparo per ogni 10 m.** Serve per rendere lo sprint una scelta
   e non un suicidio.
5. **I due punti di estrazione devono avere volumetrie opposte.** Est: chiuso e verticale.
   Ovest: lungo e orizzontale.
6. **Nessun "pezzo unico" irripetibile.** Tutta la mappa è composta da kit modulari (vedi doc 07).

---

## Il problema dell'acqua

Il mare è **fondale dipinto + piano animato**, non geometria giocabile, salvo il bagnasciuga
della Grotta. L'acqua aperta è budget sprecato: la usiamo come sfondo e come limite
invalicabile, non come spazio di gioco.

---

## Graybox (M0–M1)

- [x] Footprint 180 × 200 m con le quote: suolo 0, gradinata del Largo 1,53 m, porticato 3,3 m,
      Terrazza del Molo 3,4 m
- [x] Le zone come volumi grezzi — `game/maps/calanera.json`, generato da `tools/mapgen/calanera.py`
- [x] Tempi di percorrenza **misurati** (tabella sopra)
- [x] **2 rotte indipendenti** per ogni coppia spawn-estrazione: il generatore trova le rotte, le
      percorre con il porting Python del movimento e le scrive nel file della mappa; `./run.sh test`
      le ripercorre con la simulazione C#
- [ ] Al massimo **3 linee di tiro lunghe** dalla piazza — il gomito della Via del Mercato, la banca,
      i pilastri del Mercato e la statua del Cortile le spezzano, ma manca una verifica misurata

### Come è costruita

- **Si disegna la metà Nord e si ruota di 180°** (D2): spawn, corsia centrale fino alla piazza, le due
  corsie laterali con il Molo e il Piazzale. La pelle cambia (intonaco/cemento a Nord, ocra/pietra a
  Sud), l'architettura no.
- **Piazza e collegamenti** si disegnano a Est e si ruotano. **Grotta e Galleria** sono disegnate a
  parte e sono **simmetriche rispetto all'asse Est–Ovest**: Nord e Sud ci arrivano allo stesso modo.
- Tutto ciò che non è strada o cortile è **edificio pieno** (i gusci del doc 07), lotti ≤ 10 m da 2,
  3 o 4 piani. Interni giocabili: la banca, il forno, il Mercato coperto, la Grotta, la Galleria.
- **Il sole viene da Ovest**, perpendicolare all'asse Nord–Sud: nessuna squadra gioca controluce.

### Cosa NON torna fra questo documento e la regola D2

La descrizione delle zone sopra (il Forte "su terrazze", la Lungomare "aperta") dà caratteri diversi a
pezzi che la rotazione rende **uguali**. Nel graybox **ha vinto D2**: la differenza fra le coppie è
solo di pelle. Se si vuole un carattere diverso, va deciso dove rinunciare alla rotazione esatta.

### Rigenerare e verificare

```bash
./run.sh map        # rigenera calanera.json, verifica rotte e tempi, anteprima PNG con le rotte
./run.sh test       # ripercorre le stesse rotte con la simulazione C#
```
