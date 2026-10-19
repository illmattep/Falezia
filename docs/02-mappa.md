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

## Geometria

```
                    PORTO VECCHIO  (spawn Nord)
                           │
              Scalinata    │    Via Stretta
                   ╲       │       ╱
                    ╲      │      ╱
     GROTTA DEL      ╲     │     ╱      GALLERIA
     LEVANTE  ────────[ PIAZZA VECCHIA ]────────  VECCHIA
     (estrazione Est)      │  (cassa)      (estrazione Ovest)
                    ╱      │      ╲
                   ╱       │       ╲
              Lungomare    │    Mura
                           │
                    FORTE SAN VERANO  (spawn Sud)
```

### Dimensioni

| Elemento | Distanza dal centro |
|---|---|
| Spawn Nord / Sud | **120 m** |
| Estrazione Est (Grotta del Levante) | **95 m** |
| Estrazione Ovest (Galleria Vecchia) | **95 m** |
| Footprint totale | **~240 × 220 m** |
| Zona viva per round | tutta la mappa, ma la contesa è concentrica |

### Tempi di percorrenza (velocità: passo 4,2 m/s · sprint 6,5 m/s)

| Tratta | Passo tattico | Sprint |
|---|---|---|
| Spawn → centro | 28,6 s | 18,5 s |
| Centro → estrazione | 22,6 s | 14,6 s |
| Estrazione Est ↔ Ovest (via anello) | 52 s | 34 s |

**Tutti i tempi sono identici per le due squadre** — imposto dalla simmetria rotazionale.

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

## Da disegnare in graybox (M0–M1)

- [ ] Footprint 240 × 220 con quote di elevazione annotate
- [ ] Le 5 zone come volumi grezzi
- [ ] Verifica dei **tempi di percorrenza** a passo e in sprint (con un cronometro, non a occhio)
- [ ] Verifica che esistano **2 rotte indipendenti** per ogni coppia spawn-punto di estrazione
- [ ] Verifica che dalla piazza si vedano al massimo **3 linee di tiro lunghe**
