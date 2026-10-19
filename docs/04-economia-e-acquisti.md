# 04 · Economia e acquisti

## Il principio

> Il denaro **non è tuo**. È della squadra. E ogni spesa è un fatto pubblico.

Questo crea la tensione sociale che è il marchio del gioco: nessuno può comprarsi il fucile
"perché gli piace", e il rendiconto di fine round rende evidente chi sta sbagliando.

---

## I due fondi

| Fondo | Importo | Chi decide | Approvazione |
|---|---|---|---|
| **Personale** | **$1.000**, una volta, prima del match | Il singolo | Nessuna |
| **Cassa comune** | **$5.000** a inizio partita + introiti | La squadra | Sì, sopra soglia |

Il fondo personale serve a garantire che **nessuno parta mai disarmato**: la pistola arriva dal loadout,
e i $1.000 coprono un'utility, una piastra leggera e quello che resta.
**La piastra non è gratuita: è il primo acquisto che fai.**
Esaurito quello, tutto passa dalla cassa.

---

## I 3 slot consumabili

Il consumabile non è tutto uguale: **uno è tuo per sempre, due li compri.**

| Slot | Provenienza | Persistenza | Variabile? |
|---|---|---|---|
| **1** | Dal **loadout** | Si rinnova **gratis ogni round** | No — scelto una volta, mai cambiato |
| **2** | **Comprato** in Buy Phase | **Si conserva se sopravvivi** | Sì, ogni round |
| **3** | **Comprato** in Buy Phase | **Si conserva se sopravvivi** | Sì, ogni round |

**Conseguenza economica:** il consumabile del loadout è **la tua assicurazione contro la povertà**.
Anche con la cassa a zero, hai sempre una granata o una trappola gratis ogni round. Chi sceglie
"Filo spinato" nel loadout non spenderà *mai* per la trappola di base. Chi sceglie "Granata flash"
non spenderà mai per l'ingresso. **Sono due modi diversi di essere poveri, ed è una scelta reale.**

I due slot acquistati sono la parte variabile: ogni round decidi se comprare due trappole, due granate,
un sensore e un medikit, o niente e tenere i soldi per un Lockdown.

**Dettaglio completo:** doc [10 · Loadout e progressione](10-loadout-e-progressione.md).

---

## La soglia

| Fascia | Ammontare | Regola |
|---|---|---|
| **Libero** | **≤ $300** | Acquisto istantaneo, nessuna approvazione |
| **Élite** | **> $300** | **1 click** di un compagno **oppure 5 s di silenzio** = assenso |

La soglia è tarata perché **tutta l'utility e tutte le trappole stiano sotto i $300**. Ne risulta una
regola memorabile in una frase: *utility libera, armi e poteri si approvano.*

### Perché il silenzio-assenso

Senza di esso, **un singolo compagno può tenere la squadra in ostaggio** negando ogni approvazione.
I 5 s di silenzio trasformano l'ostruzionismo in un'azione attiva che costa tempo — e il tempo,
in una Buy Phase da 40 s, è la risorsa più scarsa.

---

## La Politica di Spesa

Fissata una volta, prima del match. Serve perché l'approvazione a ogni acquisto, con 40 s di Buy Phase,
è un collo di bottiglia: nessuno compra niente e si litiga.

**Esempio di politica di un team affiatato:**

```
Decifratore ......... élite fino a $1.200 in autonomia
Ricognitore ......... élite fino a $1.000 in autonomia
Ingegnere ........... élite fino a $1.200 in autonomia (deve poter comprare il Robot)
Portatore ........... $600
Rianimatore ......... $600
Lockdown ............ richiede sempre approvazione esplicita (2 click)
```

**Politica predefinita per il solo-queue:**
```
Chiunque può comprare fino a $1.000 in autonomia. Sopra, 1 approvazione.
Lockdown e Overclock richiedono sempre approvazione esplicita.
```

La politica è **modificabile a metà partita** (in pausa tattica), perché le esigenze cambiano.

---

## Regole di rivendita

> **Si vende solo il presente. Il passato si scambia o si regala.**

| Caso | Regola |
|---|---|
| Comprato **in questa** Buy Phase, ci ripensi | **Rimborso pieno** (è un annulla, non una vendita) |
| Sopravvissuto con un'arma conservata, vuoi tenerla | Nessun costo |
| Sopravvissuto, vuoi un'arma diversa | Paghi **solo la differenza**; la vecchia viene consumata |
| Sopravvissuto, vuoi dare l'arma a un compagno | Puoi **lasciarla a terra** (nessun rimborso) |
| Sopravvissuto, vuoi monetizzare l'arma | **Vietato** |

### Perché la terza colonna è indispensabile

Senza di essa:

> Un giocatore sopravvissuto con un fucile **conservato gratis** lo venderebbe alla cassa comune,
> **creando denaro dal nulla**. Ogni round di sopravvivenza diventerebbe una stampante di soldi
> e l'economia esploderebbe in tre partite.

È l'**unica regola anti-exploit** dell'economia, e va implementata **lato server**: il client non
deve mai poter inviare una transazione non valida.

---

## Conservazione dell'attrezzatura

> **L'attrezzatura è tua finché sei vivo. Se muori, nel round successivo devi ricomprare.**

| Stato | Conseguenza |
|---|---|
| Sopravvivi | Mantieni arma, piastre, utility non consumate |
| Muori | Perdi tutto, ricompri dalla cassa |
| Muori ma la squadra estrae | Perdi comunque tutto — **la sopravvivenza individuale è l'unica cosa che conta** |

### La proprietà emergente (la più bella del progetto)

> **Il costo economico di un round è proporzionale a quanti sono morti.**

Un round dove si muore tutti è un disastro doppio: perdi il round *e* devi riarmare 5 persone.
Un round dove sopravvivono in 4 costa quasi nulla.

Questo punisce automaticamente lo scontro frontale suicida e premia il gioco che preserva le vite.
**Rende il no-respawn una conseguenza economica, non solo una punizione.**

---

## Gli introiti

| Evento | Importo | Note |
|---|---|---|
| **Vittoria round** | **$3.000** | Fisso |
| **Sconfitta round** | **$2.000 → $2.400 → $2.800 → $3.200** | **Progressivo sulle sconfitte consecutive** |
| Round nullo (timer 8:00) | $1.200 | Punizione reciproca |
| Apertura della cassa | **+$150** | Alla cassa comune |
| Prelievo del Nucleo | **+$200** | Alla cassa comune |
| Estrazione completata | **+$300** | Alla cassa comune |

### La curva delle sconfitte è il motore delle rimonte

Hai scelto **primo a 5** proprio "per dare più speranza alle rimonte". Ecco il sistema che lo rende vero:

> Un team che perde 3 round di fila riceve **$3.200**, cioè **più di quanto riceva chi vince** ($3.000).

Senza questa curva, in un formato a 5-9 round la partita è decisa al round 4. Con questa curva,
una squadra 0-3 ha ancora i mezzi per risalire *e* per adattarsi alla strategia nemica — che è
esattamente la motivazione che hai dato per scegliere il formato.

### Flusso di cassa realistico (partita a 9 round, 5 vinte / 4 perse)

```
Cassa iniziale             $5.000
5 vittorie × $3.000       $15.000
4 sconfitte progressive   $10.400
Obiettivi (medio)          $1.500
─────────────────────────────────
TOTALE                    $31.900   →  ~$3.540 per round
```

**Verifica di tenuta:** due fucili d'assalto costano $2.800, più ~$600 di utility = $3.400.
Il budget è di ~$3.540. **Quindi il team può permettersi di riarmare ~2 élite per round, non 5.**
La pressione è reale, il margine esiste, e il cosiddetto "Forced ECO" **emerge da sé**:
se 4 giocatori muoiono, il round successivo si gioca con 2 fucili e 2 pistole.

---

## Il pavimento anti-spirale

> **Se all'inizio di una Buy Phase la cassa comune è sotto $1.000, la squadra riceve un sussidio di $1.500.**

Serve a garantire che nessuna partita diventi ingiocabile prima del 60° minuto. È l'unico aiuto
diretto, ed è *condizionato*, non regalato.

**La pistola e il consumabile del loadout sono sempre gratuiti: nessuno è mai disarmato.**
**La piastra, invece, si compra** — ed è una scelta deliberata, con un rischio che va scritto.

### La piastra è un secondo asse di neve (e come lo teniamo a bada)

Togliere la piastra gratuita significa che **una squadra povera non è solo peggio armata: è anche più
fragile.** Senza piastra si muore in un colpo in meno. Due svantaggi che si sommano.

Non è un difetto, **a patto che la piastra sia il primo acquisto possibile**:

| Contromisura | Perché funziona |
|---|---|
| **Piastra leggera a $250, in fascia libera** | Si compra **senza approvazione** con qualunque budget. È il Kevlar di questo gioco |
| **Bonus sconfitta progressivo** (fino a $3.200) | Una squadra che perde ha **più soldi** di chi vince, quindi si riarma l'armatura per prima |
| **Pavimento anti-spirale a $1.500** | Anche nel round peggiore, $250 per la piastra ci sono sempre |

> **La regola da tenere a mente in fase di taratura:** se un giocatore in ECO deve scegliere fra una
> granata e una piastra, **deve scegliere la piastra**. Se scegliesse la granata, il prezzo è sbagliato.

---

## I due poteri di squadra

Sono le uniche due cose che si comprano per *tutta la squadra* e non per un singolo. Hanno
**filosofie opposte**, e questa asimmetria è voluta.

| | **Overclock · $700** | **Lockdown · $2.200** |
|---|---|---|
| Categoria | **Risparmio durevole** | **Decisione del round** |
| Quando si compra | In qualsiasi Buy Phase | Solo in Buy Phase, e ne esiste uno solo per round |
| Quando si attiva | Al momento di **avviare una tua estrazione** | Dopo il **prelievo del Nucleo**, fino all'avvio dell'estrazione |
| Se non lo usi | **Si conserva** al round successivo | **Scade a fine round** |
| Effetto | Il tuo assedio passa da 120 a **90 s** | Chiude **fisicamente** Est o Ovest |
| Quantità | Massimo **1** in cassaforte | **1** per round, globale |
| Visibilità | Acquisto visibile, attivazione pubblica | Acquisto visibile, attivazione pubblica |

### Perché l'Overclock è durevole

Nella versione a uso singolo per round, l'Overclock era un **lancio di dadi**: lo compravi prima di
sapere chi avrebbe preso il Nucleo, e se il centro lo vinceva il nemico avevi bruciato $700.
Peggio: diventava un **moltiplicatore di neve** — chi dominava il centro lo sfruttava ogni round e
chi perdeva non poteva permetterselo.

Rendendolo **conservabile**, diventa due cose molto migliori:
1. **Un investimento razionale.** Nessuno spreco: lo tieni finché non ti serve.
2. **Un uso alternativo del bonus sconfitta.** La squadra che sta perdendo accumula denaro: con
   l'Overclock durevole, quel denaro può diventare **uno strumento di rimonta da usare al momento
   giusto** invece di un fucile in più. È un altro modo di spendere la speranza.

**Il limite di 1 esemplare** serve a impedire l'accumulo e a mantenere la decisione viva: *questo
round lo brucio, o lo tengo?*

### Perché il Lockdown è reattivo

Nella versione pre-committata (comprato e rivelato a fine Buy Phase), era un **lancio di dadi da
$2.200**, non una decisione. Nella versione reattiva, chi l'ha comprato sa *chi* ha il Nucleo,
*dove* si trova e *quanto* tempo gli resta — e decide di conseguenza.

**Il costo nascosto:** il Lockdown toglie opzioni a **entrambe** le squadre. Chiudere Est lo chiude
anche per chi l'ha comprato. È per questo che non è mai un vantaggio netto: è una
**riduzione di varianza**, non un bonus.

### Nota su Overclock e 120 s

Se il tuo team non estrae mai, l'Overclock non si attiva mai. Non è un difetto: è la definizione di
un **consumabile durevole** — vale quanto vale la tua capacità di arrivare all'estrazione.

---

## Listino completo

### Gratuito, sempre disponibile
| Oggetto | Costo |
|---|---|
| **Standard** (pistola base) | $0 |
| **Combat Knife** (slot dedicato + melee rapido) | $0 |

> **La piastra leggera non è più gratuita.** È il primo acquisto del round: vedi la nota sul secondo
> asse di neve nella sezione precedente.

### Libero (≤ $300)
| Oggetto | Costo |
|---|---|
| Munizioni (+1 caricatore) | $100 |
| Granata fumogena | $150 |
| Granata flash | $200 |
| Filo spinato | $200 |
| Kit piastra (ripristina 1 piastra) | $200 |
| **Piastra leggera** | **$250** |
| Medikit (1 uso, 3 s) | $250 |
| Granata stordente | $250 |
| Sensore acustico | $250 |
| Sensore di movimento | $300 |
| Mina a pressione | $300 |
| Mina laser direzionale | $300 |

### Élite (> $300, richiede approvazione)
| Oggetto | Costo |
|---|---|
| **Hush** (pistola silenziata) | $400 |
| **Piastra pesante** | $500 |
| **Snub** (rivoltella) | $500 |
| **Scrap** (SMG da ECO) | $600 |
| **Chatter** (pistola automatica) | $600 |
| **Kit Decodifica** | $600 |
| **Overclock** *(durevole, 1 solo)* | $700 |
| **Hammer** (pistola magnum) | $700 |
| **Cadence** (SMG) | $900 |
| **Blitz** (PDW) | $1.000 |
| **Choke** (fucile a pompa) | $1.100 |
| **Robot deployabile** *(deployabile)* | $1.200 |
| **Anvil** (AR 7,62) | $1.400 |
| **Service** (AR 5,56) | $1.400 |
| **Sweeper** (fucile semiautomatico) | $1.400 |
| **Vantage** (AR semiauto 7,62) | $1.800 |
| **Spotter** (cecchino semiauto) | $1.800 |
| **Lockdown** *(scade a fine round)* | $2.200 |
| **Longshot** (cecchino bolt action) | $2.400 |

**Scheda completa di ogni arma:** doc [11 · Armi](11-armi.md).
**LMG eliminata** dal listino: non c'è nessun sistema di soppressione che la giustifichi.

---

## Implementazione lato server (obbligatoria)

Tutta l'economia è **server-side**. Il client invia solo *intenzioni*.

```
Client  →  "voglio comprare X"
Server  →  valida: fondi? soglia? approvazione presente? politica?
        →  applica e replica a tutta la squadra (rendiconto live)
```

Un client che tenta di comprare senza fondi, di vendere attrezzatura conservata, o di inviare
più transazioni di quante il tempo consenta, viene **respinto silenziosamente** e loggato.

**L'approvazione è un evento di rete validato**, non un flag locale. Altrimenti è la prima cosa
che un cheat implementa.
