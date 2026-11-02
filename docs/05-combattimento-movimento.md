# 05 · Combattimento e movimento

## Principi

1. **Niente classi, niente abilità innate, niente passivi.** Tutti partono con gli stessi parametri.
2. **I ruoli emergono dagli acquisti**, non da una scelta in lobby. È la lezione di CS: il "ruolo"
   è ciò che hai comprato e come lo usi.
3. **Il movimento è una decisione, non un'espressione.** Ogni spostamento ha un costo in rumore
   o in tempo.
4. **Nessuna morte immeritata.**

---

## Movimento

| Azione | Valore | Costo |
|---|---|---|
| Passo tattico | **4,2 m/s** | Silenzioso, arma pronta |
| **Sprint** | **6,5 m/s** | **Rumoroso** (raggio 35 m), arma instabile per 2 s dopo |
| Crouch | 2,2 m/s | Silenzioso, sagoma ridotta, mira più stabile |
| ADS | **55% della velocità** | Mira precisa, campo visivo ridotto |
| Vault | — | Supera ostacoli bassi, **rumoroso** |
| Salita scale | 2,6 m/s (su), 3,8 m/s (giù) | Rumoroso · *non ancora nel codice: oggi le scale si fanno a passo pieno* |
| **Gradino** | **35 cm** superati camminando | Un'alzata vera è 17 cm (doc 07). Sopra i 35 cm serve il salto (~1 m) |
| **Portatore del Nucleo** | **4,2 m/s soltanto** | **Niente sprint, niente arma primaria, niente vault** |

### Esplicitamente esclusi
- **Nessun bunny hop.** Nessuna accelerazione in aria sotto il 70% della velocità.
- **Nessuno strafe aereo innaturale.** Controllo aereo minimo, solo correzione di traiettoria.
- **Nessuno slide.**
- **Nessun incremento di velocità da rotazione del mouse.**

### Stamina

| Parametro | Valore |
|---|---|
| Durata sprint | 10 s |
| Recupero completo | 8 s |
| Recupero mentre si cammina | 2× più veloce |
| Effetto a stamina zero | Solo passo tattico per 3 s |

La stamina non esiste per punire, esiste per rendere lo sprint una **scelta con un costo** e non
la velocità di default. Serve anche a limitare il "kiting" infinito.

---

## Rumore come sistema di gioco

Il rumore **non è un effetto sonoro, è informazione di rete**.

| Sorgente | Raggio udibile | Informazione trasmessa |
|---|---|---|
| Passo tattico | 8 m | Presenza vaga |
| **Sprint** | **35 m** | Presenza + direzione + urgenza |
| Vault | 20 m | Presenza + direzione |
| Scale | 25 m | Presenza + direzione |
| Sparo (non soppresso) | **tutta la mappa** | Posizione + arma |
| Sparo soppresso | 60 m | Direzione |
| **Porta aperta a calcio** | **35 m** | Posizione + urgenza |
| Porta aperta normalmente | 15 m | Presenza + direzione |
| Porta aperta piano | 8 m | Presenza vaga |
| Rimozione del Nucleo | 20 m | Allarme |

Il modello audio deve essere **occluso geometricamente** (i muri smorzano) ma **non azzerato**:
un rumore forte attraverso un muro è un'informazione, non un silenzio.

---

## Modello dei danni

### Salute e piastre

| Componente | Valore |
|---|---|
| Salute | **100** |
| **Piastra leggera** | **+20 punti effettivi** al torso — **$250**, fascia libera: è il primo acquisto del round |
| **Piastra pesante** | **+40 punti effettivi** al torso ($500) |
| Testa | **Mai protetta dalle piastre.** Moltiplicatore **×4,0** |
| Piastre | **Si rompono** con l'uso, non si rigenerano in round |
| Rigenerazione salute | **Nessuna** |
| Medikit | Ripristina 50 salute, 3 s di channel |

> **Modello a punti effettivi, non a percentuale.** Una piastra non "riduce del 25%": **aggiunge vita**.
> Il motivo è la leggibilità: con i punti effettivi il giocatore sa che **ogni piastra costa un colpo in
> più**, e i danni delle armi restano numeri interi. Con le percentuali, 30 danni diventano 22,5 e
> nessuno sa più quanti colpi servono. *(Il modello precedente, a percentuale, va considerato superato.)*

**La regola in una frase: una piastra leggera = un colpo in più, una pesante = due.**

Le piastre sono il vero strato di profondità: **ripristinare una piastra costa tempo e posizione**,
e il Kit Piastra da $200 rende quella scelta economica.

### Zone del corpo

| Zona | Moltiplicatore |
|---|---|
| Testa | **×4,0** (letale con la maggior parte delle armi) |
| Collo | ×2,5 |
| Torso | ×1,0 |
| Braccia | ×0,85 |
| Gambe | ×0,75 |

### Ferite localizzate (leggere)

| Zona colpita | Effetto |
|---|---|
| Gamba (a 0 salute locale) | **Zoppichi**: −35% velocità di movimento per 20 s |
| Braccio (a 0 salute locale) | **Mira instabile**: +60% di rinculo per 20 s |
| Nessun sanguinamento progressivo | **Escluso deliberatamente** |

> **Perché ho escluso il sanguinamento:** allunga i round di 1–2 minuti, svuota l'azione e trasforma
> il gioco in una simulazione di medicina. Tu hai scelto round da ~5 minuti e primo a 5: con il
> sanguinamento, le partite sfondano l'ora. Il sistema leggero dà l'80% del sapore tattico al 20%
> del costo. *(Decisione mia, rivedibile: è l'unico punto dove ti ho tolto qualcosa che potevi volere.)*

---

## Tempo per uccidere (TTK)

Obiettivo: **0,30 – 0,55 s** sul torso, contro un bersaglio con **piastra leggera** (il caso normale).

- Fucile d'assalto: **4–5 colpi** al torso, **1 alla testa** (25 × 4 = 100)
- SMG: **6–7 colpi** al torso
- AR semiautomatico (Vantage): **2 colpi** al torso
- Cecchino semiautomatico (Spotter): **2 colpi** al torso, **anche contro piastra pesante**
- Pompa (Choke): **1 colpo** a bruciapelo, ma solo contro piastra leggera
- Bolt action (Longshot): **1 colpo** al torso o più in alto

Né Arma-instantaneo (che premia solo l'imboscata) né Halo-lento (che premia solo la mira).
Un TTK medio è ciò che rende possibile il **trade** e quindi il DBNO.

**Tabella completa delle 15 armi:** doc [11 · Armi](11-armi.md).

---

## DBNO — Stato abbattuto

| Parametro | Valore |
|---|---|
| Tempo di dissanguamento | **30 s** |
| **Tempo di rianimazione** | **Matrice 2×2** — vedi sotto |
| Auto-rianimazione | **Vietata** |
| Movimento da abbattuto | Strisciamento a 0,8 m/s |
| Chi può rianimare | Chiunque, a mani nude o con Medikit |

### Matrice di rianimazione

Il tempo dipende da **due** fattori: la specializzazione **Rianimatore** e il possesso di un **Medikit**.

| | Senza Rianimatore | Con Rianimatore |
|---|---|---|
| **A mani nude** | **15 s** | **10 s** |
| **Con Medikit** | **10 s** | **5 s** |

Il Medikit da solo equivale al Rianimatore da solo: sono **due strade diverse allo stesso risultato**.
Il loadout e l'economia si toccano qui — spendere $250 per un Medikit è l'alternativa economica al
portare il Rianimatore, e il Rianimatore costa una specializzazione e uno slot consumabile.
*(Matrice dedotta dalla descrizione, da confermare.)*

**Il DBNO chiude il round quando una squadra non ha più nessuno vivo *né* rianimabile.**

### Perché esiste

> Senza DBNO, il gioco diventa **"chi peekka per primo vince"**, perché non esiste lo scambio.

Il DBNO permette il **trade**: muoio per aprire un angolo, il mio compagno mi rianima, e la
posizione si guadagna con un costo invece che con un tiro di dadi. È la meccanica più importante
di un tattico, e senza di essa il tuo gioco non è tattico.

### Perché 30 s e non 45 o 60

Ogni secondo di dissanguamento è un secondo in cui 4 giocatori aspettano un quinto. Con round da
~5 minuti, 45 s sono il 15% del round. 30 s mantiene la tensione senza fermare il gioco.

---

## Utility e trappole

### Utility
| Oggetto | Costo | Effetto |
|---|---|---|
| Granata flash | $200 | Acceca 3 s in un cono frontale; evitabile girandosi |
| Granata fumogena | $150 | Blocca la visuale per 12 s, non il suono |
| Granata stordente | $250 | Rombo e disorientamento 2,5 s; non fa danno |
| Medikit | $250 | Ripristina 50 salute, 3 s di channel |
| Kit piastra | $200 | Ripristina una piastra, 4 s di channel |
| Endoscopio | $250 | Specchio sotto la porta: vedi la stanza, non ti vedono |
| Drone da ricognizione | $300 | Ricognizione mobile, distruttibile, 25 s di autonomia |

### Trappole — la regola del tell

> **Ogni trappola letale ha un tell audiovisivo obbligatorio. Nessuna eccezione.**

| Trappola | Costo | Effetto | Tell |
|---|---|---|---|
| **Filo spinato** | $200 | Rallenta a 1,5 m/s e fa 5 danni/s | **Visibile**: luccica. Suono di strappo quando scatta |
| **Sensore acustico** | $250 | Ping su suono forte nel raggio di 12 m | Suono di attivazione udibile nell'area |
| **Sensore di movimento** | $300 | **Ping approssimativo** (zona, 2,5 s) nel raggio di 14 m | LED rosso lampeggiante |
| **Mina a pressione** | $300 | 70 danni all'esplosione | **Click arming udibile a 8 m** |
| **Mina laser direzionale** | $300 | 90 danni in un cono | **Laser visibile** anche di giorno |

**Tutte le trappole sono distruttibili o disattivabili:**
- Distruggibili con 1 colpo di fucile
- Disattivabili con un channel di 2,5 s
- **Le trappole sono visibili se le guardi.** Non sono mai invisibili.

### Sensori: ping, non marcatori

> I sensori **non mostrano il nemico sul HUD**. Mostrano un **ping approssimativo**: una zona
> sbiadita, larga diversi metri, che svanisce in 2,5 s.

**Perché:** in un gioco senza respawn, un marcatore live è un wallhack che annulla tutto il lavoro
sull'informazione (rumore, sprint, specchio sotto la porta) — che è il cuore tattico del gioco.
Il ping compra **tempo di reazione**, non la vista a raggi X. *(Decisione mia, rivedibile.)*

I sensori non coprono l'intero punto di estrazione: chi li piazza sta comprando **un allarme**,
non il controllo del territorio.

---

## Porte

**Tre stati: chiusa · socchiusa · aperta.**
**Nessuno sfondamento: le porte non si distruggono mai, e nel gioco non esiste nessuna carica da sfondamento.**

| Da → a | Azione | Tempo | Rumore | Vista | Si passa? |
|---|---|---|---|---|---|
| Chiusa → Socchiusa | **Apri piano** | **0,6 s** | 8 m | **Fessura:** vedi e spari, non entri | No |
| Chiusa → Aperta | Apri normalmente | **1,5 s** | 15 m | Totale | Sì |
| Chiusa → Aperta | **Apri a calcio** | **0,3 s** | **35 m** | Totale | Sì |
| Aperta → Chiusa | Chiudi | 1,0 s | 12 m | Nessuna | No |

### Le tre regole

1. **Le porte non si chiudono da sole.** Restano dove le lasci: una porta aperta al round 1 resta
   aperta per tutto il round.
2. **La porta socchiusa è il verbo centrale dell'ingresso.** Ti fa **vedere e sparare, non entrare.**
   È l'anti-pezzo: la usi per capire cosa c'è dentro **prima** di commettere il corpo. Costa 0,6 s e
   quasi nessun rumore, ed è la cosa più economica e più utile che puoi fare prima di un ingresso.
3. **Una porta chiusa non è una difesa.** Non esiste barricata: chi vuole entrare entra in **0,3 s**.
   La porta non protegge nessuno — **rallenta solo chi non vuole farsi sentire.**

### L'ingresso in stanza, passo per passo

È il flusso deciso in fase di design, e ora è **l'unico modo di entrare nel gioco**:

```
1. APRI       uno si posiziona       →  socchiusa (0,6 s, quasi silenzioso) per guardare dentro
                                     →  a calcio (0,3 s, 35 m di rumore) se serve velocità
2. FLASH      un secondo lancia      →  flash o stordente dentro la stanza
3. PULISCI    gli altri entrano      →  peek, angoli, spari di copertura
```

**Conseguenza di bilanciamento da sorvegliare:** con lo sfondamento fuori, **la flashbang diventa lo
strumento d'ingresso principale del gioco.** Va tenuta d'occhio — ma c'è già una regola che la rende
onesta: **è evitabile girandosi.** Il difensore che tiene l'angolo giusto non è cieco per 3 s; è cieco
per 3 s **se ha sbagliato posizione**. È il tipo di morte che il giocatore accetta.

### Nessun cuneo: il gioco non ha meccaniche di negazione

**Decisione: il cuneo è stato tolto.** Non esiste nessun oggetto che blocchi una porta.

Da qui nasce una proprietà pulita, che vale la pena scrivere perché è un **invariante del progetto**:

> **FALEZIA non ha nessuna meccanica che neghi il passaggio. Solo meccaniche che lo rallentano o lo annunciano.**

| Strumento | Cosa fa al passaggio |
|---|---|
| Filo spinato | **Rallenta** |
| Mine | **Puniscono** |
| Sensori | **Informano** |
| Porte | **Si aprono.** Sempre, in 0,3 s se serve |

**Il difensore non è rimasto nudo:** ha filo spinato, mine e sensori — cioè **tempo e informazione**,
che è esattamente quello che le trappole devono dare in questo gioco. Ha perso solo la capacità di
**chiudere** un passaggio, e non ne aveva bisogno: **la difesa qui si fa con gli angoli e le trappole,
non con le porte.**

### Cosa si guadagna togliendo lo sfondamento

Non è solo un sistema in meno: sono **vantaggi tecnici concreti**, e contano perché il target è una GTX 970.

1. **La mappa è completamente statica.** LightmapGI e occlusion culling danno il meglio proprio sulle
   mappe statiche — e sono due delle tecniche su cui si regge il budget del preset basso.
2. **La mesh di collisione è statica e bakeata:** zero costo a runtime, nessun ricalcolo.
3. **Il netcode non deve replicare geometria distrutta.** Nessuno stato di "muri rotti" da
   sincronizzare, nessuna divergenza possibile fra client e server.
4. **L'unica geometria dinamica del gioco resta il cancello del Lockdown e le porte.** Due cose sole,
   entrambe a stato binario, entrambe banali da replicare — e questo è anche il motivo per cui il
   **Robot** deve avere un percorso pre-autorizzato e non un pathfinding dinamico.

Per un progetto che deve girare a 1080p/60 su una GTX 970, questa è una decisione di performance
travestita da decisione di gameplay — e sono le migliori.

---

## Deployabili

**Definizione:** un **deployabile** è un oggetto che il giocatore **piazza nel mondo** e che da quel
momento vive di vita propria, indipendentemente da chi l'ha messo giù.

| Deployabile | Costo | Si muove | Vita nel mondo | Distruttibile |
|---|---|---|---|---|
| Filo spinato | $200 | No | Fino a fine round | Sì |
| Sensore acustico | $250 | No | Fino a fine round | Sì |
| Sensore di movimento | $300 | No | Fino a fine round | Sì |
| Mina a pressione | $300 | No | Fino a fine round | Sì |
| Mina laser direzionale | $300 | No | Fino a fine round | Sì |
| Drone da ricognizione | $300 | **Sì**, pilotato | 25 s | Sì |
| **Robot deployabile** | **$1.200** | **Sì**, autonomo | Fino a fine round | Sì |

### Il Robot — specifica

| Parametro | Valore |
|---|---|
| Piazzamento | Il giocatore lo piazza **da copertura**, entro 6 m dal punto di piazzamento |
| Tempo di piazzamento | **5 s** (esposto durante il piazzamento). **1,5 s** con l'Ingegnere |
| Navigazione | **Autonoma**, verso la cassa. Percorso **pre-autorizzato**, non generato in tempo reale |
| Tempo di apertura | **15 s**. **10 s** se piazzato da un Ingegnere |
| Riparazione | **Solo l'Ingegnere:** channel di 4 s per ripararlo e riprendere un'apertura interrotta |
| Rumore | **Altissimo** — motore, cingoli, sirena. Udibile oltre 50 m |
| Distruzione | 90 danni subiti. Se distrutto mentre apre, il progresso **si azzera** |
| Trasporto | **Impossibile.** Il Nucleo va prelevato a mano |
| Dopo l'uso | Resta a terra come relitto distruttibile |

**Perché il percorso è pre-autorizzato e non generato:** un pathfinding dinamico è non deterministico,
e quindi non riproducibile dal server per la lag compensation e i replay. Con 2-3 percorsi fissi
dalla zona di piazzamento alla cassa, il robot è **deterministico, economico e prevedibile per il
giocatore** — che è esattamente ciò che serve in un gioco dove ogni morte deve essere meritata.

### Regole generali dei deployabili

1. **Non vengono mai predetti dal client.** Sono entità del server, il client le **interpola**.
   Predire un deployabile significa desincronizzazioni che si vedono.
2. **Il mondo si resetta a ogni round.** Nessun deployabile sopravvive da un round all'altro:
   altrimenti le mine si accumulano e il gioco degenera in uno sminamento.
3. **Gli oggetti non piazzati sì.** Se sopravvivi portando addosso un robot o una mina non usata,
   **li conservi** — coerentemente con la regola dell'attrezzatura.
4. **Ogni deployabile è distruttibile o disattivabile**, e nessuno è invisibile.

---

## Regola d'oro del bilanciamento

> **Se un giocatore muore, deve poter dire "me lo sono meritato".**

Ogni meccanica che produce morti incomprensibili va tagliata. Le trappole senza tell, i punti ciechi
irrisolvibili, le armi che uccidono senza che il suono sia stato udibile — tutte fuori.
In un gioco senza respawn, la frustrazione non è un costo accettabile: è una recensione negativa.


---

## Danno, zone colpite e morte

Il danno esiste nella **simulazione**, non nel client: `game/sim/Combat.cs`. La stessa funzione
decide per il giocatore, per il server e per i test, quindi non c'è modo che i tre vedano cose
diverse.

**Come si risolve un colpo**

1. Il muro più vicino lungo il raggio (`Trace.Ray`, la stessa geometria della collisione). Se è più
   vicino del bersaglio, il colpo si ferma lì: **non si spara attraverso i muri**.
2. Altrimenti si guardano le due scatole di ogni bersaglio — **testa** (25 cm di lato in cima) e
   **corpo** (48 × 30 cm fino a 1,52 m) — e vince la più vicina.
3. La zona fine si decide sull'altezza del punto d'impatto: sotto il 42% dell'altezza sono **gambe**.

| Zona | Moltiplicatore |
|---|---|
| Testa | **× 2,2** |
| Corpo | × 1,0 |
| Gambe | **× 0,75** |
| Coltello (mischia) | **× 2,0** sul danno dell'arma, senza caduta |

Il danno di base è quello della scheda **alla distanza vera** (`Arsenal.DamageAt`): un colpo a 8 m
non fa gli stessi danni di un colpo a 40 m, e una pompa a 12 m fa meno di una a 2 m. Il minimo è
sempre 1: nessun colpo è gratis.

**Morte e rinascita** (`TargetState`): a vita zero il bersaglio cade (il corpo si corica in 0,4 s),
resta a terra **4 secondi**, poi torna in piedi con la vita piena. Non si può uccidere due volte, e
un colpo su un cadavere non conta.
