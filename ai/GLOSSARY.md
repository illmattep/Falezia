# GLOSSARY · il vocabolario di FALEZIA

> Usa **questi** termini. Un sinonimo, in un documento condiviso, è un malinteso che aspetta.

---

## Nomi propri

| Termine | Cosa è |
|---|---|
| **FALEZIA** | Il nome del gioco. **Con la Z**, maiuscolo. Dal latino *falesia*; a Porto Vecchio c'è l'insegna antica che lo scrive così |
| **Calanera** | La mappa: città costiera italiana immaginaria, 180 × 200 m, tre corsie per lato |
| **Porto Vecchio** | Spawn Nord |
| **Forte San Verano** | Spawn Sud |
| **Piazza Vecchia** | La zona centrale, dove sta la cassa |
| **Grotta del Levante** | Estrazione **Est**. Chiusa, un ingresso, fianco marino |
| **Galleria Vecchia** | Estrazione **Ovest**. Lunga, ingressi multipli |
| **Via Stretta · Scalinata Lunga · Lungomare · Mura** | Le corsie laterali (Nord: Via Stretta, Scalinata; Sud: Mura, Lungomare). Insieme fanno l'anello |
| **Largo** | La piazzetta della corsia centrale, con la gradinata. Da qui partono i passaggi verso le corsie laterali |
| **Arco · Varco della Dogana** | Le strozzature coperte delle corsie |
| **Mercato coperto** | La sala a pilastri fra la piazza e ciascuna estrazione: la rotazione del difensore |
| **Molo · Piazzale** | Gli spazi aperti davanti a Grotta e Galleria, alla fine delle corsie laterali |
| **Corsia** | Uno dei tre percorsi che partono da ogni spawn (schema Valorant) |

## Oggetti e stato

| Termine | Cosa è |
|---|---|
| **Nucleo** | L'oggetto da estrarre. Sta nella cassa, al centro |
| **Cassa** | Il contenitore del Nucleo. Si apre in 15 s a mano, 10 s col Kit, 15 s col Robot |
| **Prelievo** | L'atto di prendere il Nucleo (3 s). **Pubblico**: entrambe le squadre lo sanno |
| **Portatore** | Chi trasporta il Nucleo: cammina, ha solo la pistola, non fa vault |
| **Faro** | L'evento pubblico che segnala l'estrazione attiva: colonna di fumo + sirena |
| **Assedio** | I 120 s (90 con Overclock) in cui il difensore tiene il Nucleo e l'altro assalta |
| **Retake** | L'assalto dell'avversario per rimuovere il Nucleo dal punto |
| **DBNO** | *Down But Not Out*: abbattuto, 30 s di dissanguamento, rianimabile in 5 s |
| **Trade** | Lo scambio: muoio per aprire un angolo, il compagno mi rianima |

## Economia

| Termine | Cosa è |
|---|---|
| **Cassa comune** | Il fondo di squadra ($5.000 + introiti). Non è denaro personale |
| **Fondo personale** | I $1.000 spesi una volta prima del match |
| **Fascia libera** | Acquisti **≤ $300**: nessuna approvazione |
| **Approvazione** | 1 click di un compagno, oppure 5 s di silenzio (= assenso) |
| **Politica di Spesa** | Le regole d'acquisto decise a inizio partita |
| **Rendiconto** | Il registro pubblico delle spese, mostrato in Buy Phase e a fine round |
| **ECO / Forced ECO** | Round giocato con poco: la squadra non può riarmare tutti |
| **Bonus sconfitta progressivo** | $2.000 → $2.400 → $2.800 → $3.200 sulle sconfitte consecutive |

## Acquisti chiave

| Termine | Effetto |
|---|---|
| **Lockdown** | Chiude **fisicamente** un punto di estrazione per il round. Uno solo, globale, attivato **dopo il prelievo**. Scade a fine round |
| **Overclock** | Consumabile **durevole**: 120 s → 90 s sulla *tua* estrazione. Si conserva finché non lo usi. Max 1 |
| **Kit Decodifica** | Cassa in 10 s (5 con Decifratore) |
| **Robot** | Deployabile che apre la cassa da remoto (15 s, 10 con Ingegnere). **Non trasporta**: il Nucleo va preso a mano |
| **Piastra** | Leggera +20 ($250) · pesante +40 ($500) |
| **Endoscopio** | Specchio sotto la porta: vedi la stanza, non ti vedono |

## Verbi tattici

| Termine | Cosa significa qui |
|---|---|
| **Socchiusa** | Stato della porta: vedi e spari, non passi. **Il verbo centrale dell'ingresso** |
| **Ping** | L'informazione dei sensori: una zona sbiadita per 2,5 s. **Mai un marcatore live** |
| **Tell** | Il segnale audiovisivo obbligatorio di ogni trappola letale |
| **Impilarsi** | La squadra si dispone su un ingresso prima di entrare |
| **Specchio** | Ricognizione sotto la porta |
| **Anello** | Il giro formato dalle corsie laterali delle due squadre: senza, il retake è impossibile |

## Termini tecnici

| Termine | Cosa significa |
|---|---|
| **Simulazione pura** | `game/sim/` — zero dipendenze dal motore, deterministica |
| **Passo fisso** | `dt = 1/64`, esplicito. Mai dedotto dal frame rate |
| **Predizione** | Il client applica subito il proprio input |
| **Riconciliazione** | Il client confronta col server e **rigioca** gli input non confermati |
| **Lag compensation** | Il server riavvolge le pose di max 200 ms per validare un colpo |
| **Ack** | Il tick dell'ultimo input di quel client che il server ha applicato |
| **Il cancello** | `tests/` compila solo `sim/` e `net/`: se importi Godot lì, non compila |
| **Graybox** | La mappa fatta di sole scatole, prima dell'arte |

## Parole vietate

| Non dire | Perché |
|---|---|
| "Classe", "eroe", "operatore" | Il gioco ha **specializzazioni**, condivise e pubbliche, senza identità |
| "Respawning" | Non esiste. Esiste lo **stato abbattuto** e la rianimazione |
| "Barricata", "breach", "cuneo" | **Esclusi** per progetto (regola C3) |
| "Loot", "drop", "rarità" | Zero progressione funzionale (regola B1) |
