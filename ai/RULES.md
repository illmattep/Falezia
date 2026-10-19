# RULES · gli invarianti di FALEZIA

> **Non sono linee guida. Sono vincoli.** Ogni regola ha il suo *test di violazione*: se stai per fare
> la cosa a destra, fermati e chiedi.

---

## A · Identità del gioco

| # | Regola | Violazione |
|---|---|---|
| A1 | **Nessun respawn nel round.** L'unica eccezione è lo stato abbattuto con rianimazione | Un giocatore che rientra dopo la morte |
| A2 | **L'attacco/difesa è emergente**, non assegnato: lo decide chi prende il Nucleo | Una modalità "attaccanti vs difensori" fissa |
| A3 | **Nessuna classe, nessun eroe, nessun perk.** Solo **5 specializzazioni** condivise | Un'abilità esclusiva, un passivo, un albero di perk |
| A4 | **Le specializzazioni danno TEMPO, non potenza né informazione** | "Vedi i nemici attraverso i muri", "+20% danno" |
| A5 | **Il loadout è pubblico** e si blocca al via del round 1 | Loadout nascosto all'avversario |
| A6 | **Ogni morte deve essere meritata.** Ogni trappola letale ha un tell audiovisivo | Una mina invisibile, un danno senza sorgente |

## B · Economia e monetizzazione

| # | Regola | Violazione |
|---|---|---|
| B1 | **Zero progressione funzionale.** Armi, ottiche, attachment, specializzazioni: tutto dal livello 1 | Un fucile da sbloccare, un'ottica da comprare |
| B2 | **Lo shop vende solo estetica.** Le chest contengono solo cosmetici e non si comprano | Skin con statistiche, chest a pagamento, "attachment raro" |
| B3 | **L'economia è validata interamente lato server** | Il client che invia "ho comprato X" |
| B4 | **Si vende solo ciò che si compra nella Buy Phase corrente.** Ciò che conservi si regala o si scambia, mai si monetizza | Vendere un fucile sopravvissuto per soldi (= stampare denaro) |
| B5 | **La piastra leggera non è gratuita** ($250, fascia libera) e **non è nel loadout** | Una piastra gratuita di partenza |
| B6 | La cassa comune è **di squadra**: sopra $300 serve **1 click** di un compagno, o 5 s di silenzio | Acquisti élite liberi |
| B7 | **Le abilità non si comprano**: si scelgono | Una specializzazione acquistabile |

## C · Round e obiettivo

| # | Regola | Violazione |
|---|---|---|
| C1 | **Il prelievo è pubblico, la destinazione è segreta** | Un marcatore sul Nucleo nemico |
| C2 | **Il Nucleo non torna mai alla cassa**: resta dove cade | Un timer che lo riporta al centro |
| C3 | **Nessuno sfondamento, nessun cuneo, nessuna barricata.** Il gioco non ha meccaniche di **negazione** del passaggio, solo di rallentamento e allarme | Un muro sfondabile, una porta bloccata a tempo indefinito |
| C4 | **I sensori danno un ping, non un marcatore live** | Un puntino che segue il nemico |
| C5 | **Il timer del round si sospende durante un'estrazione attiva** | Un round che muore a metà assedio |
| C6 | **Rimuovere il Nucleo durante l'assedio chiude subito il round** | Un ping-pong che sfonda il tetto di 8 minuti |
| C7 | **La cassa, una volta aperta, resta aperta** | Riaprire la cassa |

## D · Mondo e arte

| # | Regola | Violazione |
|---|---|---|
| D1 | **La mappa è una croce con un anello.** Ogni spawn raggiunge ogni estrazione con **≥2 rotte**, una fuori dal centro | Una mappa a croce pura, dove il centro è l'unico passaggio |
| D2 | **Simmetria architettonica, asimmetria cosmetica.** Si costruisce mezzo paese e si ruota | Due lati visivamente identici |
| D3 | **La mappa è completamente statica.** Le uniche geometrie dinamiche sono **le porte** e **il cancello del Lockdown** | Muri distrutti, ponti che crollano, prop fisici |
| D4 | **Il colore saturo è riservato all'informazione di gioco** (squadre, obiettivo, pericolo) | Un muro rosso decorativo |
| D5 | **Densità bassa, proporzioni reali.** Regola del 3 cm: sotto i 3 cm non si modella | Un'arma con 40k triangoli di dettaglio |
| D6 | **L'unico attachment è l'ottica**, e non tocca rinculo, danno o precisione | Un'impugnatura, un soppressore, un laser come attachment |
| D7 | **Target di performance = vincolo di design:** preset basso ≤ **900 draw call**, ≤ **1,2 M** triangoli, 1080p/60 su GTX 970 | "Ottimizziamo dopo" |

## E · Codice e architettura

| # | Regola | Violazione |
|---|---|---|
| E1 | **`game/sim/` e `game/net/` non importano Godot. Mai.** Il progetto `tests/` compila solo quelli, e se qualcuno aggiunge `using Godot` **smette di compilare** | `using Godot;` in un file della simulazione |
| E2 | **La simulazione è a passo fisso 64 Hz e deterministica.** Il passo è esplicito, mai dedotto dal frame rate | `delta` preso dal motore dentro `Simulation.Step` |
| E3 | **Il client predice, il server autorizza.** Il client manda **input**, non posizioni né esiti di colpi | Hit registration lato client |
| E4 | **Il vettore della simulazione è `System.Numerics.Vector3`.** La conversione verso `Godot.Vector3` avviene **solo al confine** (client/server) | Un `Godot.Vector3` dentro `sim/` |
| E5 | **I test devono restare verdi.** Se rompi un test, il cambio è sbagliato o il test va giustificato | "I test li sistemo dopo" |
| E6 | **Codice in inglese, commenti e documenti in italiano** | `funzione_italiana()` |

---

## Il test di ammissione

Prima di scrivere una riga, chiediti:

> **"Questo cambio rende il gioco più onesto, più leggibile o più giusto?"**
>
> Se la risposta è "è più divertente" o "è più ricco", **probabilmente stai aggiungendo una cosa che
> avevamo deciso di non avere.**
