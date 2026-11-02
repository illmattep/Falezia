# 11 · Armi

## I principi

1. **Arsenale neutro.** Nessuna arma è esclusiva. Tutte disponibili dal **livello 1**.
2. **Si comprano con il denaro di partita**, dalla cassa comune (doc 04).
3. **L'unico attachment è l'ottica.** Niente canne, niente impugnature, niente laser.
4. **Un'arma ha un carattere, e l'ottica non lo cambia.**
5. **Ogni arma deve avere un motivo per esistere.** Due armi che si giocano uguale sono un'arma sola
   con due nomi.

> **I nomi sono inglesi, funzionali, e non sono quelli di controparti reali.** `Glock`, `USP` e
> `Desert Eagle` sono marchi registrati che richiedono licenze: qui si chiamano **Standard**, **Hush**,
> **Hammer**. Stessa logica per cui la mappa si chiama *Calanera* e non Amalfi.

---

## La direzione estetica: meno stile, più funzione

> **Le armi di FALEZIA non sono belle. Sono attrezzi.**

| Regola | Dettaglio |
|---|---|
| **La silhouette comanda** | Ogni arma deve essere **identificabile dal suo profilo a 30 m**, in un render a un colore solo. Non è estetica: è leggibilità in combattimento |
| **Materiali reali** | Acciaio stampato, polimero opaco, brunitura consumata, legno verniciato scuro, nastro adesivo, cinghie di tela |
| **Vietato il luminoso** | Nessun LED, nessuna luce, nessuna finitura cromata, nessun elemento futuristico |
| **Tutte le armi sono usate** | Graffi, vernice mancante, numeri di serie incisi, riparazioni da officina, nastro sull'impugnatura |
| **Le skin sono mimetiche, vernici, nastro** | **Mai** neon, mai colori saturi, mai decori. Il colore saturo è riservato all'informazione di gioco (doc 07) |

---

## Il modello dei danni: piastre a punti effettivi

| Componente | Valore |
|---|---|
| Salute | **100** |
| **Piastra leggera** | **+20 punti effettivi** al torso — **$250**, fascia libera |
| **Piastra pesante** | **+40 punti effettivi** al torso — **$500**, richiede approvazione |
| Testa | **Mai protetta dalle piastre.** Moltiplicatore **×4,0** |
| Braccia | ×0,85 · Gambe ×0,75 |
| Rigenerazione | **Nessuna**. Medikit: 50 salute, 3 s |

**Nessuna piastra è gratuita.** Il riferimento di tutte le tabelle è quindi **il bersaglio senza
piastra** — lo stato reale di chi è in ECO o compra l'arma e non l'armatura.

**La regola in una frase: la piastra leggera costa un colpo, la pesante due.**

---

## Come si legge una scheda

| Voce | Significato |
|---|---|
| **Danno** | Danno per colpo al torso, senza piastre |
| **Colpi** | Colpi necessari: **senza piastra / con piastra leggera / con piastra pesante** |
| **Cadenza** | Intervallo fra un colpo e il successivo |
| **Caricatore** | Colpi in caricatore / riserva totale |
| **Ricarica** | **Tattica** (colpo in canna) / **a vuoto** (caricatore esaurito) |
| **ADS** | Tempo per portare l'arma in mira, **con mire metalliche**. L'ottica aggiunge il suo tempo |
| **Movimento** | Variazione della velocità di movimento con l'arma in mano |
| **Precisione** | Dispersione in mira a 25 m |
| **Rinculo** | Come si comporta la canna fra un colpo e l'altro |

**Valori di riferimento:** passo 4,2 m/s · sprint 6,5 m/s · movimento in ADS 55% del passo ·
sprint con l'arma in mano mai possibile.

---
---

## Dispersione e cadenza: fermo, in movimento, in mira

Il cono è in **gradi di semiapertura**. Non è un numero decorativo: la simulazione lo calcola a
ogni tick (`Arsenal.SpreadAt`), il mirino sullo schermo è la **proiezione del cono vero**
(`tan(cono) / tan(FOV/2)`), e ogni colpo campiona una direzione **dentro** quel cono con un seme
deterministico: client e server possono ricalcolare la stessa traiettoria.

| Arma | Fermo | In movimento | In mira | In mira e in movimento | Bloom per colpo | Tetto bloom | Richiusura |
|---|---|---|---|---|---|---|---|
| **Standard** | **0,50°** (22 cm a 25 m) | **2,60°** | **0,035°** (1,5 cm a 25 m) | 0,55° | +0,28° | 1,50° | 3,6°/s |
| **Hush** | **0,42°** | **2,30°** | **0,025°** (1,1 cm a 25 m) | 0,48° | +0,24° | 1,30° | 3,4°/s |

Modificatori: **accovacciato ×0,80** · **in aria ×1,80** (niente tiri al volo) · il bloom si somma
al cono e rientra da solo.

**Cadenza:** Standard **0,21 s = 288 RPM**, Hush **0,23 s = 264 RPM**. Semiautomatiche: un colpo
per click, il client manda solo il fronte di salita.

**Click in anticipo.** Se clicchi mentre l'arma è ancora in ciclo il colpo **non si perde**: resta
in coda e parte appena la cadenza lo permette, ma solo entro **100 ms** (110 la Hush). Oltre quella
finestra il click si scarta: niente colpi in ritardo di mezzo secondo e niente colpi in più.
Tenere premuto non fa fuoco automatico — si contano i fronti del grilletto, non i tick.

> Perché conta: il vantaggio di una pistola non è il danno, è **quanto in fretta il cono si
> richiude**. La Standard è più veloce e più larga da fianco; la Hush è più lenta ma più precisa
> in mira. Se un giorno i numeri non convincono, si cambiano in `game/sim/Weapons.cs` e i test
> dicono subito cosa si è rotto.

---
---

# IL COLTELLO

## Combat Knife · sempre nel loadout · gratis

**Estetica:** lama in acciaio annerito da 17 cm, impugnatura in gomma con guardia minima, fodero in
nailon sulla coscia. **Nessun elemento decorativo**: è un attrezzo da lavoro con una lama.

| Azione | Danno | Tempo | Note |
|---|---|---|---|
| **Colpo normale** | **55** | 0,6 s | 2 colpi per uccidere. **Silenzioso** |
| **Melee rapido** *(pulsante dedicato)* | **45** | 0,7 s | **Non uccide mai da solo.** Durante l'animazione **non spari** |
| **Colpo alle spalle** | **letale** | 0,6 s | Solo se il bersaglio **non ti sta guardando** |

**Con il coltello in mano ti muovi l'8% più veloce** e il rumore dei passi resta normale. È l'arma
delle rotazioni: attraversi la mappa in fretta, ma sei disarmato.

---
---

# LE PISTOLE

## 1 · STANDARD · gratis

**Estetica:** pistola di servizio in **polimero nero opaco**, canna corta, mire fisse bianche.
Vernice consumata sugli spigoli, numero di serie inciso sul fusto. Sembra uno strumento, non un'arma.

**Funzionamento:** semiautomatico a rinculo corto, percussore lanciato.

| | |
|---|---|
| **Danno** | **25** · testa ×4 = **100 → un colpo alla testa uccide** |
| **Colpi** | **4 / 5 / 6** |
| **Cadenza** | 0,21 s (288 RPM) |
| **Caricatore** | 17 / 51 |
| **Ricarica** | 1,5 s / 2,0 s |
| **ADS** | **0,20 s** — il secondo più rapido del gioco |
| **Movimento** | 0% · in ADS **92%** (le pistole non ti rallentano quasi) |
| **Precisione** | 1,5 cm a 25 m (in mira) · 22 cm da fianco |
| **Rinculo** | verticale contenuto, riallineamento in 0,18 s |

**Ruolo:** non sei mai disarmato. Serve a vincere i round in ECO e a finire chi è già ferito.

## 2 · HUSH · $400

**Estetica:** pistola di servizio con **soppressore integrato nella canna**: il profilo è **più lungo
e più grosso** del normale, con una guaina in metallo opaco. Mire nere, nessun marchio, nastro
sull'impugnatura. Si riconosce subito: **è l'unica pistola col profilo lungo.**

**Funzionamento:** semiautomatico, soppressore integrato. **Gli spari si sentono a 40 m invece che in
tutta la mappa.** È l'unica arma silenziosa del gioco, ora che il soppressore non è più un attachment.

| | |
|---|---|
| **Danno** | **22** · testa ×4 = **88 → NON uccide con un colpo alla testa** |
| **Colpi** | 5 / 6 / 7 |
| **Cadenza** | 0,23 s (264 RPM) |
| **Caricatore** | 15 / 45 |
| **Ricarica** | 1,7 s / 2,2 s |
| **ADS** | 0,24 s · in ADS 90% |
| **Precisione** | **1,1 cm a 25 m** — la pistola più precisa |
| **Rinculo** | minimo, riallineamento in 0,20 s |

**Ruolo:** l'unica arma che spara **senza annunciarti**. In un gioco dove il suono è un sistema, è
un'arma di posizionamento: uccidi e nessuno sa dove sei. Il prezzo è che **non ha il colpo alla testa**.

## 3 · SNUB · $500

**Estetica:** rivoltella a **canna cortissima**, tamburo a 6 colpi, finitura **acciaio brunito
consumato**, guancette in gomma nera. Sembra un'arma vecchia con segni di officina sulla canna.
**Il profilo è inconfondibile: nessun carrello, tutto tamburo.**

**Funzionamento:** doppia azione, tamburo a 6 colpi.

| | |
|---|---|
| **Danno** | **50** |
| **Colpi** | **2 / 3 / 3** |
| **Cadenza** | 0,28 s (216 RPM) |
| **Caricatore** | 6 / 18 |
| **Ricarica** | **3,0 s / 3,0 s** — con speedloader. **La più lenta del gioco**, e non c'è differenza fra tattica e a vuoto |
| **ADS** | **0,16 s — il più rapido del gioco** |
| **Movimento** | 0% · in ADS 92% |
| **Precisione** | 2,2 cm a 25 m (la peggiore delle pistole) |
| **Rinculo** | forte salto verticale, riallineamento 0,35 s |

**Ruolo:** la pistola della **reazione**. Con un ADS di 0,16 s vinci qualunque duello ravvicinato
contro chi non era già in mira. Il prezzo è **tre secondi di ricarica** e nessuna seconda occasione.

## 4 · HAMMER · $700

**Estetica:** pistola **enorme**, acciaio inossidabile con canna a fessura, mire posteriori
regolabili, impugnatura in legno. **È l'unica arma vistosa del gioco, e lo è perché è un pezzo civile
da tiro, non militare** — ed è giusto che si veda che è fuori posto.

**Funzionamento:** semiautomatico a gas, rinculo massiccio.

| | |
|---|---|
| **Danno** | **70** |
| **Colpi** | **2 / 2 / 2** — uccide in due colpi **anche contro la piastra pesante** |
| **Cadenza** | **0,56 s** (108 RPM) — la più lenta fra le pistole |
| **Caricatore** | 7 / 21 |
| **Ricarica** | 2,1 s / 2,8 s |
| **ADS** | 0,35 s · in ADS 84% |
| **Precisione** | 1,8 cm a 25 m · **45 cm da fianco: inutilizzabile senza mirare** |
| **Rinculo** | **il più violento del gioco.** Riallineamento 0,55 s |

**Ruolo:** due colpi e hai vinto, **anche contro la piastra pesante**. Ma se sbagli il primo, hai
perso mezzo secondo — e con nessun respawn, mezzo secondo è tutto.

## 5 · CHATTER · $600

**Estetica:** pistola compatta in polimero con **selettore di fuoco** sul lato e **caricatore esteso
che sporge sotto l'impugnatura**. Compensatore stampato in punta. **Sembra un pezzo aggiunto, non
progettato** — e infatti lo è.

**Funzionamento:** **full-auto** con caricatore esteso.

| | |
|---|---|
| **Danno** | **18** · testa ×4 = 72 |
| **Colpi** | 6 / 7 / 8 |
| **Cadenza** | **742 RPM (0,081 s)** |
| **Caricatore** | 20 / 60 |
| **Ricarica** | 1,8 s / 2,4 s |
| **ADS** | 0,28 s · in ADS 90% |
| **Precisione** | 4,5 cm a 25 m · 30 cm da fianco |
| **Rinculo** | sale molto e **in modo irregolare** |

**Ruolo:** per l'ECO in cui non puoi permetterti altro, o per finire un nemico in **0,36 s** a 5 metri.
Svuota il caricatore in 1,2 s: consuma munizioni a una velocità che non puoi mantenere.

---
---

# LE SMG

## 6 · CADENCE · $900

**Estetica:** SMG in **acciaio stampato e polimero nero opaco**, calcio pieghevole in tubo,
caricatore curvo a banana. Vernice consumata sul selettore di fuoco e sull'impugnatura. **Il profilo
è basso e compatto: si distingue da un fucile a colpo d'occhio.**

**Funzionamento:** semiauto / 3 colpi / full-auto, selezionabile.

| | |
|---|---|
| **Danno** | **20** · testa ×4 = **80 → NON uccide con un colpo alla testa** |
| **Colpi** | 5 / 6 / 7 |
| **Cadenza** | 626 RPM (0,096 s) |
| **Caricatore** | 30 / 90 |
| **Ricarica** | 2,2 s / 2,9 s |
| **ADS** | 0,26 s |
| **Movimento** | **−4%** (più veloce di un fucile) |
| **Precisione** | 3,0 cm a 25 m · utile fino a 25 m |
| **Rinculo** | verticale contenuto, **orizzontale quasi assente: la raffica lunga resta gestibile** |

**Ruolo:** l'arma della **mobilità**. Ti muovi più veloce, ricarichi in fretta, e a 20 metri tieni la
raffica. Non vince mai un duello a 40 m.

## 7 · BLITZ · $1.000

**Estetica:** PDW **bullpup** compatta, corpo in polimero grigio-verde con **caricatore orizzontale
sopra la canna**. Ingombro ridottissimo: **sembra un attrezzo, e la sua silhouette è unica** — un
blocco squadrato senza caricatore sporgente.

**Funzionamento:** full-auto a cadenza altissima, caricatore orizzontale.

| | |
|---|---|
| **Danno** | **16** · testa ×4 = 64 |
| **Colpi** | 7 / 8 / 9 |
| **Cadenza** | **837 RPM (0,072 s)** |
| **Caricatore** | 30 / 90 |
| **Ricarica** | 2,4 s / 3,1 s |
| **ADS** | 0,24 s |
| **Movimento** | **−2%** — il più agile delle primarie |
| **Precisione** | 5,0 cm a 25 m · **oltre 15 m la dispersione cresce rapidamente** |
| **Rinculo** | **sale a picco dopo il decimo colpo.** Non è un'arma da raffica: è un'arma da **mezzo secondo** |

**Ruolo:** pulizia di stanze. Sotto i 12 m nessuno spara più veloce di te. Oltre i 20 m stai solo
bruciando munizioni.

## 8 · SCRAP · $600

**Estetica:** SMG **aperta**, corpo in **lamiera piegata**, calcio in filo metallico saldato, finitura
grigia sfogliata, nastro adesivo sull'impugnatura. **È visibilmente la più economica dell'arsenale**,
e deve sembrarlo: la silhouette è spigolosa e grezza.

**Funzionamento:** full-auto, caricatore corto.

| | |
|---|---|
| **Danno** | **17** · testa ×4 = 68 |
| **Colpi** | 6 / 8 / 9 |
| **Cadenza** | 720 RPM (0,083 s) |
| **Caricatore** | 20 / 60 |
| **Ricarica** | **2,0 s / 2,6 s** — la più rapida delle primarie |
| **ADS** | 0,24 s |
| **Movimento** | −3% |
| **Precisione** | **6,0 cm a 25 m** — la peggiore delle primarie |
| **Rinculo** | **irregolare e laterale**: la canna deriva a destra o a sinistra in modo casuale |

**Ruolo:** l'arma che esiste per **il round in ECO**. $600 + $250 di piastra = **$850**: ci stai anche
con la cassa vuota. Imprecisa, ma **è un fucile** — e senza respawn, la differenza fra "ho un fucile"
e "ho una pistola" è tutta la partita.

---
---

# I FUCILI A POMPA

## 9 · CHOKE · $1.100

**Estetica:** fucile a pompa a **canna lunga**, acciaio brunito con **calcio e astina in legno**,
strozzatore fisso in punta. **Sembra un fucile da caccia, perché lo è**: è un'arma civile requisita.
Silhouette lunga, sottile e senza caricatore.

**Funzionamento:** **pompa a ripetizione manuale.** 12 pallettoni per cartuccia, cono stretto.

| | |
|---|---|
| **Danno** | **130** a bruciapelo · 90 a 6 m · 45 a 12 m · 15 a 20 m |
| **Colpi** | **1 / 1 / 2** |
| **Cadenza** | **0,91 s** (66 RPM, pompata manuale) |
| **Caricatore** | 6 / 24 — **a tubo, cartuccia per cartuccia** |
| **Ricarica** | 0,6 s per cartuccia, **parziale e interrompibile** |
| **ADS** | 0,30 s |
| **Movimento** | −6% |
| **Precisione** | in mira il cono è **1/3 più stretto**: raddoppia la gittata utile |
| **Rinculo** | violento, riallineamento 0,5 s |

**Ruolo:** **il re dei 6 metri.** Nessuno vince un duello contro il Choke dentro una stanza. Il round
dopo, a 20 metri, sei un bersaglio che cammina.

## 10 · SWEEPER · $1.400

**Estetica:** fucile a pompa **semiautomatico** con corpo in **polimero nero e acciaio**, **caricatore
a scatola estraibile** (non a tubo) e maniglione di trasporto. Più militare e più moderno del Choke:
**la silhouette ha un caricatore sporgente che il Choke non ha.**

**Funzionamento:** semiautomatico a recupero di gas, caricatore a scatola.

| | |
|---|---|
| **Danno** | **100** a bruciapelo · 75 a 6 m · 40 a 12 m · 12 a 20 m |
| **Colpi** | **2 / 2 / 2** — **non uccide mai con un colpo** |
| **Cadenza** | **0,45 s** (132 RPM) |
| **Caricatore** | 7 / 21 |
| **Ricarica** | **2,6 s / 3,4 s** — con caricatore, non cartuccia per cartuccia |
| **ADS** | 0,32 s |
| **Movimento** | −5% |
| **Rinculo** | forte ma verticale e prevedibile |

**Ruolo:** il fucile a pompa che **perdona**. Due colpi in 0,35 s e una ricarica vera. È la pompa che
puoi portare nella Galleria Vecchia, dove il Choke arriverebbe già morto.

---
---

# I FUCILI D'ASSALTO

## 11 · ANVIL · $1.400

**Estetica:** fucile d'assalto a **otturatore rotante**, **acciaio stampato** con calcio e impugnatura
in **legno verniciato scuro**, caricatore curvo in metallo. Finitura nera consumata fino al metallo sul
paramano. **È l'arma più vecchia dell'arsenale, e la più rispettata.**

**Funzionamento:** semiauto / full-auto. Rinculo pesante, recupero lento.

| | |
|---|---|
| **Danno** | **30** · testa ×4 = **120 → un colpo alla testa uccide** |
| **Colpi** | **4 / 4 / 5** — **l'unica AR che non perde un colpo contro la piastra leggera** |
| **Cadenza** | 649 RPM (0,093 s) |
| **Caricatore** | 30 / 90 |
| **Ricarica** | 2,6 s / 3,3 s |
| **ADS** | 0,30 s · in ADS 55% |
| **Movimento** | −7% |
| **Precisione** | 2,2 cm a 25 m · **il primo colpo è il più preciso del gioco** |
| **Rinculo** | **il più forte della categoria**: sale di 3° per colpo e **deriva a destra**. Oltre 4 colpi la raffica è inutile |

**Ruolo:** **il danno per colpo.** Contro un nemico corazzato serve un colpo in meno di qualsiasi
altra AR, e uccide con un colpo alla testa. In cambio: **è l'unica AR che non puoi usare a raffica.**

## 12 · SERVICE · $1.400

**Estetica:** fucile d'assalto in **alluminio e polimero nero**, maniglione di trasporto, impugnatura
anteriore, **caricatore diritto in polimero**. Vernice uniforme ma **graffiata sull'otturatore**. È
l'arma più istituzionale: sembra emessa da un deposito.

**Funzionamento:** semiauto / 3 colpi / full-auto. Rinculo leggero, recupero rapido.

| | |
|---|---|
| **Danno** | **25** · testa ×4 = **100 → un colpo alla testa uccide** |
| **Colpi** | 4 / 5 / 6 |
| **Cadenza** | 735 RPM (0,082 s) |
| **Caricatore** | 30 / 90 |
| **Ricarica** | **2,3 s / 3,0 s** |
| **ADS** | 0,28 s · in ADS 55% |
| **Movimento** | **−5%** |
| **Precisione** | **2,0 cm a 25 m — la più precisa delle AR** |
| **Rinculo** | verticale contenuto, orizzontale minimo: **la raffica da 6 colpi resta gestibile** |

**Ruolo:** **la standard.** Nessun difetto, nessun pregio: se non sai cosa comprare, compra questa.
Vince i duelli perché **non ti fa sbagliare**.

## 13 · VANTAGE · $1.800

**Estetica:** fucile **da battaglia** in acciaio e polimero nero, **canna più lunga e più pesante**,
calcio regolabile, ottica obbligata. **Visibilmente più grosso e più lento del Service**: non è un
fucile d'assalto, è un fucile da tiro militare.

**Funzionamento:** **solo semiautomatico.** Rinculo molto forte, recupero lento.

| | |
|---|---|
| **Danno** | **60** · testa ×4 = 240 |
| **Colpi** | **2 / 2 / 3** |
| **Cadenza** | **0,45 s** (132 RPM, semiauto forzato) |
| **Caricatore** | 20 / 60 |
| **Ricarica** | 2,8 s / 3,5 s |
| **ADS** | **0,45 s — il più lento del gioco** |
| **Movimento** | **−9%** · in ADS **45%** |
| **Precisione** | **1,4 cm a 25 m**, affidabile fino a 70 m |
| **Rinculo** | violento ma **perfettamente verticale**: si controlla, se hai tempo di mirare |

**Ruolo:** **fortissimo oltre i 25 m, inutile sotto i 10.** Con un ADS di 0,45 s non ripulisci una
stanza. È l'arma della Galleria Vecchia e delle linee lunghe, non dell'assedio.

---
---

# I CECCHINI

## 14 · SPOTTER · $1.800

**Estetica:** fucile da tiratore **semiautomatico** in acciaio e **legno**, ottica su slitta laterale,
**calcio scheletrato in metallo** e guancetta in cuoio consumato. Sembra un fucile sportivo adattato
alla guerra. **L'ottica è parte della silhouette**, e la distingue da tutte le AR.

**Funzionamento:** semiautomatico a gas, ritorno rapido sul bersaglio.

| | |
|---|---|
| **Danno** | **75** |
| **Colpi** | **2 / 2 / 2** — uccide in due colpi **anche contro la piastra pesante** |
| **Cadenza** | **0,42 s** (144 RPM) |
| **Caricatore** | 10 / 30 |
| **Ricarica** | 3,0 s / 3,8 s |
| **ADS** | 0,40 s · in ADS **32%** |
| **Movimento** | −8% |
| **Precisione** | 1,0 cm a 25 m, affidabile a 150 m |
| **Rinculo** | salto verticale forte, ma **rientra in 0,35 s**: il secondo colpo arriva dove vuoi |

**Ruolo:** il cecchino **aggressivo**. Segue il team, spinge, e non teme il secondo colpo perché il
secondo colpo è già letale.

## 15 · LONGSHOT · $2.400

**Estetica:** fucile a **otturatore pesante**, canna lunga con freno di bocca, calcio in polimero nero
con guanciale rialzato, **ottica grossa su slitta**. Il pezzo più costoso dell'arsenale, e deve
sembrarlo: **non per eleganza, per massa.** Sembra un attrezzo da cantiere.

**Funzionamento:** **otturatore a ripetizione manuale.** Ogni colpo richiede un ciclo completo.

| | |
|---|---|
| **Danno** | **150** · testa ×4 = 600 |
| **Colpi** | **1 / 1 / 1** — uccide con un colpo al torso o più in alto, **sempre** |
| **Colpo alle gambe** | **60 danni** → il bersaglio sopravvive e ti ha visto |
| **Cadenza** | **1,25 s** (48 RPM, ciclo dell'otturatore) |
| **Caricatore** | 5 / 20 |
| **Ricarica** | 3,2 s / 4,0 s |
| **ADS** | 0,50 s · in ADS **22%** |
| **Movimento** | **−12%** — il più penalizzante |
| **Precisione** | **0,8 cm a 25 m**, affidabile oltre 200 m |
| **Rumore** | **in tutta la mappa.** Ogni colpo dice a tutti dove sei |
| **Rinculo** | rinculo tale che **il colpo dopo va riallineato a mano** |

**Ruolo:** il colpo che decide il round. Con 1,40 s di ciclo, **sbagliare è perdere**: il nemico ha
tutto il tempo per aggirarti. **E la mappa ha un anello proprio per questo.**

---
---

# L'arsenale nel codice

Tutte e quindici le armi (più la mitragliatrice e il coltello) esistono in gioco, con modello,
animazioni e numeri della scheda. Un solo posto decide i numeri: `game/sim/Weapons.cs`
(`WeaponDef`, `Arsenal.All`). Il client non li ricopia: legge `Arsenal.Get(slot)`.

| Arma | `Id` | Modello | Slot | Classe di posa |
|---|---|---|---|---|
| Standard | `standard` | art/weapons (originale) | 0 | pistola |
| Hush | `hush` | art/weapons (originale) | 1 | pistola |
| Chatter | `chatter` | `arsenal.py` | 2 | pistola |
| Snub | `snub` | `arsenal.py` | 3 | pistola |
| Hammer | `hammer` | `arsenal.py` | 4 | pistola |
| Cadence | `cadence` | `arsenal.py` | 5 | SMG |
| Scrap | `scrap` | `arsenal.py` | 6 | SMG |
| Blitz | `blitz` | `arsenal.py` | 7 | SMG |
| Choke | `choke` | `arsenal.py` | 8 | pompa |
| Sweeper | `sweeper` | `arsenal.py` | 9 | pompa |
| Anvil | `anvil` | `arsenal.py` | 10 | fucile |
| Service | `service` | `arsenal.py` | 11 | fucile |
| Vantage | `vantage` | `arsenal.py` | 12 | fucile |
| Spotter | `spotter` | `arsenal.py` | 13 | cecchino |
| Longshot | `longshot` | `arsenal.py` | 14 | cecchino |
| **Heavy** *(fuori roster)* | `heavy` | `arsenal.py` | 15 | LMG |
| **Combat Knife** | `melee` | `arsenal.py` | 16 | coltello |

> **L'indice dello slot va in rete**: si aggiunge in fondo, non si rinumera.

## Le sette clip di ogni arma

Ogni GLB porta `idle`, `fire`, `fire_last`, `reload`, `reload_empty`, `draw`, `holster`, cotte a
30 fps. Due dettagli che contano:

- **La clip del colpo dura quanto il ciclo.** Il Blitz (0,052 s) ha un `fire` di 4 fotogrammi, il
  Longshot (1,40 s) di 14. Così l'otturatore torna a posto prima del colpo dopo, invece di
  sovrapporsi.
- **La ricarica si adatta alla scheda.** La clip è cotta a una certa durata, ma è la simulazione a
  decidere (`ReloadTactical`/`ReloadEmpty`): il viewmodel cambia la **velocità di riproduzione**
  (`clip / durata`) invece di tagliare l'animazione a metà. È per questo che la Standard, con una
  clip da 1,97 s e una ricarica da 1,5 s, finisce esattamente quando il caricatore è pieno.

`fire_last` finisce con l'otturatore indietro e `reload_empty` riparte da lì: a caricatore vuoto
l'arma **resta scarrellata** (il player va in pausa sull'ultimo fotogramma) finché non ricarichi.

## I numeri, e da dove vengono

| Cosa | Regola | Dove |
|---|---|---|
| **Cadenza** | `Cycle` in secondi fra due colpi (prima era `60 / RPM`: la divisione si leggeva male e sfuggiva al controllo) | `WeaponDef.Cycle` |
| **Precisione** | centimetri a 25 m → gradi con `atan(cm / 2500)`; da fianco i numeri sono più larghi | `SpreadAds` · `SpreadStill` |
| **Dispersione** | una formula sola per client, server e test | `Arsenal.SpreadAt` |
| **Danno** | per **palla**; le pompe hanno `Pellets` (Choke 12 × 11 = 132, Sweeper 10 × 10 = 100) | `WeaponDef.Damage` · `Pellets` |
| **Caduta** | da `FalloffStart` a `FalloffEnd`, con `FalloffMin` (0,12 per le pompe, 0,55 per le pistole) | `Arsenal.DamageAt` |
| **Peso** | `MoveScale` toglie il 2–12% al passo; il coltello dà l'8% in più | `Simulation.StepPlayer` |
| **Mira** | `AdsMoveScale` è il passo in mira: pistole 0,90–0,92, AR 0,55, Longshot 0,22 | idem |
| **Rumore** | `NoiseMeters` 40 per la Hush, tutta la mappa per le altre | `WeaponDef.NoiseMeters` |

**Automatiche e semiautomatiche.** Con `Automatic` il grilletto tenuto spara (Chatter, Cadence,
Scrap, Blitz, Anvil, Service, Heavy); senza, conta solo il **fronte di salita** e tenere premuto
non fa fuoco (Standard, Hush, Snub, Hammer, Choke, Sweeper, Vantage, Spotter, Longshot). Il click
in anticipo resta in coda nella stessa finestra per tutte: metà ciclo, tappata a 0,7 s.

**Il coltello** non consuma munizioni e non si ricarica: un colpo per click, 55 danni, 0,6 s.

**Le pompe** sparano tutte le palle con lo stesso seme del colpo (`Arsenal.PelletDirection`): chi
ricalcola il colpo ottiene le stesse dodici traiettorie, nello stesso ordine.

## Modi di fuoco e ottiche

**Nessun selettore: un'arma, un comportamento.** Non c'è un tasto per cambiare modo (la X non
fa più niente). Ogni scheda dichiara come spara, e basta:

| Arma | Comportamento | Come si usa |
|---|---|---|
| **Cadence** | **raffica da 3** | un click parte la raffica; **tenendo premuto le raffiche si susseguono** (0,115 s fra i colpi, 0,30 s di pausa fra una raffica e l'altra) |
| Chatter, Scrap, Blitz, **Anvil, Service**, Heavy | **automatica** | tieni premuto e spara |
| tutte le altre | **semiautomatica** | un colpo per click |

**La raffica.** Un click arma tre colpi che escono alla cadenza dell'arma (Cadence: 0,096 s →
0,17 s per la raffica). Tenendo premuto, finita una raffica parte la successiva dopo la pausa:
non serve rilasciare e ripremere. Un click nuovo apre subito la raffica (salta la pausa), un click
durante la raffica non aggiunge colpi, e se il caricatore si svuota a metà la raffica muore lì.
Ricaricare annulla la raffica in corso.

**Il grilletto è uno stato, non un fronte.** `InputCmd.Fire` dice "premuto", e la simulazione
ricava da sola il click (`FireLatch`). Così automatiche e raffiche funzionano anche quando il
client manda lo stato a 64 Hz, e un click più rapido di un frame non si perde.

## La serie di colpi (dispersione)

Il cono non cresce in modo lineare: **i primi colpi sono puliti, poi si apre di molto**, come in
CS/Valorant. Ogni scheda dichiara quanti colpi restano precisi e quanto ci mette il cono ad
arrivare al tetto:

| Classe | Colpi puliti | Colpi per arrivare al tetto | Tetto |
|---|---|---|---|
| Fucili d'assalto | 3 | 7 | 2,4–3,6° |
| SMG / PDW | 4 | 8 | 2,2–3,2° |
| Mitragliatrice | 5 | 9 | 3,4° |
| Pistole | 2 | 4 | 1,3–1,9° (rivoltelle 3–4,2°) |
| Cecchini | 2 | 3 | 3,8–6,0° |
| Pompe | 1 | 3 | 3,2–3,5° |

Il cono in più è `SpreadPeak · (k / SpreadRamp)^1,55`, dove `k` è il numero di colpi oltre la
finestra pulita; **0,34 s di silenzio azzerano la serie**. Quindi: tre colpi mirati, il quarto
comincia a spostarsi, dall'ottavo in poi il fucile "si siede" e conviene lasciarlo raffreddare.
La curva sta nella simulazione (`Arsenal.SeriesSpread`), quindi client, server e test vedono la
stessa dispersione.

**Le ottiche.** L'ingrandimento è un dato dell'arma e agisce sul campo visivo in mira:

| Arma | Ottica | Ingrandimento | FOV in mira (da 60°) | Reticolo |
|---|---|---|---|---|
| Vantage | ottica 3× | 3× | 21,8° | croce |
| Spotter | ottica 4× | 4× | 16,4° | croce |
| Longshot | ottica 6× | 6× | 11,0° | croce + tacche di profondità |
| Cadence, Blitz | punto rosso | 1,2× | 60° | puntino rosso e anello |
| le altre | mire metalliche | 1× | 58–64° per classe | — |

La maschera nera col buco **entra solo a mira quasi completa** (0,55 → 0,92 di `AdsT`): prima
sarebbe un telo nero. Col cannocchiale in faccia **si nasconde anche l'arma** (col FOV a 11° il
viewmodel coprirebbe il cerchio) e la **sensibilità del mouse si divide per lo zoom**: un pixel
vale quanto il campo visivo che hai davanti. Il mirino normale si nasconde quando l'ottica è in faccia, altrimenti si
vedrebbero due mirini sovrapposti. Il vuoto del reticolo si allarga col **cono vero** della
dispersione: quello che vedi è quello che spari.

## I modelli: come sono fatti

`art/weapons/arsenal/arsenal.py` genera i quindici modelli mancanti con la grammatica della
Standard: masse semplici, spigoli vivi, bevel 1,2 mm a 40°, materiali opachi. Palette per
carattere — polimero nero (Standard, Service, Cadence, Vantage, Longshot), legno (Anvil, Choke,
Spotter, Hammer), acciaio (Snub, Hammer), lamiera grigia (Scrap, Heavy), polimero grigio-verde
(Blitz). Origine al centro dell'impugnatura, **+Y = canna**, socket `socket_muzzle`,
`socket_eject`, `socket_aim`, `socket_hand_r`, `socket_hand_l`.

Il foglio di controllo è `art/weapons/arsenal/sheet.py` (17 armi in profilo) e
`art/weapons/arsenal/closeup.py` (primo piano di due o tre): servono a giudicare le silhouette
senza aprire Godot, che su questa macchina non è installato.

## Il viewmodel, visto fuori dal motore

Anche la posa in prima persona si controlla senza Godot: `art/weapons/arsenal/viewmodel_preview.py`
rimette il modello sotto una camera con **FOV 85 verticale**, applica la stessa posa di
`WeaponView.cs` (hip e mira, scala per classe, `socket_aim`) e rende un'immagine per ogni arma.
Le pose sono **copiate** dal C#: se cambiano là, si cambiano qui.

Le regole che sono uscite da quelle prove:

| Regola | Perché |
|---|---|
| **L'impugnatura in mira sta a una distanza fissa per classe** (pistola 22,5 cm, fucile 17) | Allineare il mirino a una distanza fissa incollava all'occhio le armi con l'ottica molto avanti: il Blitz in mira era un muro grigio. Ora X e Y vengono dal `socket_aim`, la Z dalla posa |
| **Le armi lunghe stanno più avanti e più al centro** (x 14,5 cm, z 29 cm) | A 20 cm e 17,6 cm a destra il castello usciva dal bordo dello schermo |
| **Il coltello si tiene di taglio** (22° di becco, −52° di imbardata) | Con la lama dritta davanti alla camera si vedeva solo la punta |
| **Il coltello non "mira"** | Non ha mirino: in ADS si alza e si porta verso il centro, senza allineare niente |

---
---

# Tabella riassuntiva

Tutti i colpi a segno sul torso. **La colonna di sinistra è il caso base**, perché nessuno parte con
la piastra.

| Arma | Classe | Danno | **Senza piastra** | Tempo | **Con piastra leggera** | Tempo | Fascia utile | $ |
|---|---|---|---|---|---|---|---|---|
| **Chatter** | Pistola auto | 18 | 6 | **0,30 s** | 7 | 0,36 s | 0–10 m | $600 |
| **Blitz** | PDW | 16 | 7 | **0,31 s** | 8 | 0,37 s | 0–12 m | $1.000 |
| **Choke** | Pompa | 130 | **1** | **0,00 s** | **1** | **0,00 s** | 0–6 m | $1.100 |
| **Sweeper** | Pompa semiauto | 100 | **1** | **0,00 s** | 2 | 0,35 s | 0–12 m | $1.400 |
| **Snub** | Rivoltella | 50 | 2 | 0,25 s | 3 | 0,50 s | 0–15 m | $500 |
| **Cadence** | SMG | 20 | 5 | **0,30 s** | 6 | 0,38 s | 0–25 m | $900 |
| **Hammer** | Magnum | 70 | 2 | 0,55 s | 2 | 0,55 s | 0–20 m | $700 |
| **Anvil** | AR 7,62 | 30 | 4 | **0,30 s** | **4** | **0,30 s** | 0–35 m | $1.400 |
| **Service** | AR 5,56 | 25 | 4 | 0,26 s | 5 | 0,34 s | 0–35 m | $1.400 |
| **Scrap** | SMG ECO | 17 | 6 | 0,33 s | 8 | 0,47 s | 0–18 m | $600 |
| **Vantage** | AR semiauto | 60 | 2 | 0,40 s | 2 | 0,40 s | 25–60 m | $1.800 |
| **Spotter** | Cecchino SA | 75 | 2 | 0,35 s | 2 | 0,35 s | 30–80 m | $1.800 |
| **Longshot** | Bolt | 150 | **1** | **0,00 s** | **1** | **0,00 s** | 40–150 m | $2.400 |
| **Standard** | Pistola | 25 | 4 | 0,60 s | 5 | 0,80 s | 0–15 m | gratis |
| **Hush** | Pistola sil. | 22 | 5 | 0,88 s | 6 | 1,10 s | 0–15 m | $400 |

### La tabella che spiega perché l'armatura conta

Guarda **Anvil contro Service**, la scelta più comune del gioco:

| Bersaglio | Anvil (30 dmg) | Service (25 dmg) | Chi vince |
|---|---|---|---|
| **Senza piastra** | 4 colpi, **0,30 s** | 4 colpi, **0,26 s** | **Service** — spara più veloce |
| **Con piastra leggera** | 4 colpi, **0,30 s** | 5 colpi, **0,34 s** | **Anvil** — il danno per colpo buca l'armatura |

> **Comprare la piastra non ti fa vincere lo scontro: ti fa vincere il *tipo* di scontro.**

> **Tutti i TTK stanno nella fascia 0,30–0,55 s** decisa nel doc 05. I tempi *reali* saranno più lunghi,
> perché includono rinculo, spread e tempo di reazione: il TTK teorico serve solo a garantire che
> nessuna arma sia fuori scala.

---

# Le ottiche: l'unico attachment

**E ho tolto tutto il resto.** Canna, sottocanna, impugnature e laser **non esistono più**.

**Perché:** ogni attachment che tocca il rinculo è **una variazione sullo stesso asse**, e quell'asse è
il meno interessante da dare a un giocatore. Tre impugnature che spostano numeri di rinculo rendono il
fucile *uguale* in tre modi, e costringono a leggere una tabella prima di giocare.

> **La stessa ragione per cui abbiamo tolto i perk (doc 10): un'arma deve leggersi in un secondo.**

E c'è un guadagno che non era previsto:

> **Un'arma ha un carattere, e le ottiche non lo cambiano.**
> L'Anvil deriva a destra e l'ottica non lo aggiusta. Il Blitz sale a picco dopo il decimo colpo e
> l'ottica non lo salva. **Non puoi omogeneizzare l'arsenale con gli attachment** — quindi 15 armi
> restano davvero 15 armi.

## Le opzioni, per categoria

L'ADS indicato nelle schede è **con mire metalliche**: l'ottica aggiunge il suo tempo.

| Categoria | Opzioni | Effetto | Costo |
|---|---|---|---|
| **Pistole** | Mire metalliche | Riferimento | — |
| | **Punto rosso mini** | Mira più chiara a distanza | ADS **+0,04 s** |
| **SMG · Pompe · AR** | Mire metalliche | Riferimento | — |
| | **Punto rosso** | Bersaglio leggibile a media distanza | ADS **+0,04 s** · −5% campo visivo |
| | **Olografica 2×** | Ingaggio oltre i 30 m | ADS **+0,10 s** · **−12% campo visivo** |
| **Spotter** *(cecchino SA)* | **4×** | Riferimento, ingaggio a 50–80 m | ADS 0,40 s |
| | **8×** | Ingaggio oltre 80 m | ADS **0,55 s** · ti perdi tutto ciò che è vicino |
| **Longshot** *(bolt)* | **6×** | Riferimento | ADS 0,50 s |
| | **10×** | Ingaggio oltre 150 m | ADS **0,60 s** · campo visivo minimo |

> **Nessuna ottica riduce il rinculo, aumenta il danno o migliora la precisione.**
> Cambia **cosa vedi e quanto ci metti a mirare.** Solo quello.

Il **Longshot** ha il minor numero di opzioni di proposito: un'arma che uccide con un colpo
**non deve poter essere anche personalizzata**.

---

# Conviene altro? — la risposta finale

**❌ NO alla mitragliatrice leggera.** Servirebbe un sistema di soppressione che non abbiamo; senza, è
solo "un fucile peggiore". E senza respawn, un'arma pesante e lenta è una passività, non una scelta.

**✅ SÌ allo Scrap a $600.** È la raccomandazione più forte di questo documento: senza un'arma primaria
sotto i $900, **una squadra in ECO ha solo pistole** — e senza respawn significa perdere altri due
round a catena. Con la piastra ora a pagamento ($250), un round in ECO costa **$850** con lo Scrap: ci
stai anche con la cassa vuota.

**❌ NO a una categoria DMR separata.** Vantage e Spotter coprono già la nicchia, con filosofie diverse.

**❌ NO a lanciagranate o esplosivi pesanti.** Ovvio, ma prima o poi qualcuno lo propone.

**Roster finale: 15 armi + il coltello.** 5 pistole · 3 SMG · 2 pompe · 3 AR · 2 cecchini.
Ogni arma si descrive in una riga, e **nessuna coppia si gioca uguale**.

---

## Cosa resta da decidere

1. **Scrap a $600** — lo confermo nel roster?
2. **I nomi:** *Standard, Hush, Snub, Hammer, Chatter, Cadence, Blitz, Scrap, Choke, Sweeper, Anvil,
   Service, Vantage, Spotter, Longshot*. Sono funzionali e inglesi, ma sono segnaposto: sostituiscili
   quando vuoi.
3. **I due numeri più a rischio:** il **Longshot** ($2.400 e 1,40 s di ciclo) e il **Vantage**
   (ADS 0,45 s). Vanno tarati nei playtest per primi.


---

# I modelli, seconda generazione

La prima generazione (`art/weapons/arsenal/arsenal.py`) era una catena di scatole: leggeva come
un'arma, ma da vicino si vedevano gli spigoli, le giunzioni e le texture impacchettate. La seconda
generazione (`art/weapons/arsenal2/`) riscrive il motore geometrico e i materiali.

| Pezzo | Cosa fa |
|---|---|
| `fetch_mats.py` | **Dodici famiglie fotografiche** scaricate da ambientCG (**polymer, stipple, steel, stainless, wood, sheet, green, brass, cloth, rubber, leather, concrete**) più due sintetiche (`void`, `glass`). CC0: nessuna attribuzione obbligatoria. Basecolor 1024², normal 512², ORM 256² (R occlusione, G roughness, B metallo), 3,1 MB in tutto |
| `texgen.py` | Il primo generatore, procedurale: resta la fonte di `void` e della logica di normal/ORM. Superato dalle foto per le altre famiglie |
| `sights.py` | **La prova delle mire**: per ogni arma mette la camera dietro il calcio, ortogonale, centrata sul socket `eye`, con due bacchette rosse che segnano la linea. Se tacca e mirino non sono alla stessa quota si vede subito |
| `gunlib.py` | Primitive con profili veri: `sweep` (sezione estrusa lungo la canna), `prism` (profilo laterale estruso in X), `prism_var` (spessore variabile: la lama si assottiglia sul filo), `tube` (tubo cavo: silenziatori, campane), `strip_profile` (impugnature e caricatori curvi), `stock_profile` (calcio con pettine e pancia a curve di Bézier), `rail` (Picatinny con passo vero di 10 mm), `guard_profile` (guardia del grilletto a U) |
| `build.py` | Le 17 schede con le misure vere e le 7 clip. Un comando: `blender -b --factory-startup -P art/weapons/arsenal2/build.py -- standard` |
| `review.py` | Tre viste di controllo fuori dal motore: `closeup`, `sheet`, `viewmodel` (con le pose esatte di `WeaponView.cs`) |

**Le regole che rendono i modelli leggibili in prima persona**

1. **Ogni pezzo nasce da un profilo, non da scatole accostate.** Il carrello è una sezione
   ottagonale estrusa; l'impugnatura è un profilo che si allarga e si restringe; il calcio ha il
   pettine e la pancia a curve. Le scatole si vedono subito come scatole.
2. **I pezzi si innestano.** L'impugnatura entra *dentro* il castello (testa piatta, nessuna
   fessura), la guardia parte dal fusto, il calcio si sovrappone alla culatta di 6 mm.
3. **La mira è un'altra storia.** La tacca posteriore è una U con due spalle e il mirino un palo con
   il punto: in ADS si legge *attraverso*, non si guarda un blocco. Tacca e mirino stanno alla
   **stessa quota H** (il filo delle orecchie e la punta del mirino): se una delle due è più bassa,
   in puntamento si vede il mirino galleggiare sopra o sotto l'intaglio. Per le armi lunghe
   `H = rz1 + 22 mm`, per le pistole `zs1 + 12,5 mm`, per i revolver `top + 8,5 mm`.
4. **I dettagli negativi usano `void`.** Assole M-LOK, feritoie, finestrini, anime delle canne,
   fresate: un materiale nero, opaco, senza rilievo. Prima erano scatole scure che sporgevano e
   sembravano finestre illuminate.
5. **UV a scala fissa.** `cube_project` da 8,5 cm: la densità di texel è la stessa su ogni pezzo,
   su ogni arma. Niente isole impacchettate, niente texture che si stira.
6. **I pezzi mobili hanno l'origine sul perno.** Cane, grilletto, tamburo e otturatore ruotano
   attorno al loro asse, non attorno all'origine dell'arma. Prima il cane ruotava di 24° attorno
   all'impugnatura e finiva a 4 cm dal fusto.
7. **Niente sporgenze sulla linea di mira.** Blocco gas, maniglione di trasporto, cresta del cane e
   culatta dei revolver devono restare *sotto* H. Il maniglione della Heavy è passato sulla canna,
   il tubo del gas è sceso di 6 mm, la culatta del tamburo di 11 mm: prima in ADS si vedeva un
   cilindro (o un maniglione) al posto del mirino.
8. **Il punto rosso è un TELAIO, non una scatola.** Due spalle da 2,8 mm, ponte da 4,2 mm,
   vetro da 19 mm **trasparente** (alfa 0,16, `alphaMode: BLEND` nel GLB): prima il vetro era opaco
   e in mira si guardava una biglia blu dentro un telaio da 6 mm. Sul Blitz il dot sale su un rialzo
   di 16 mm, perché il caricatore orizzontale sta sopra il castello e tagliava la linea di mira.
9. **L'occhio è un socket, non un numero.** Ogni arma ha `socket_eye` sulla linea di mira
   (15,5 cm dietro la tacca, 7,5 cm dietro l'oculare del cannocchiale). In ADS è **quello** che il
   viewmodel porta sull'asse della camera, non la mira: la mira di un fucile sta quasi alla volata,
   e allineando quella l'arma finiva dietro l'obiettivo — si vedeva il fucile tagliato e le tacche
   "in mezzo alle scatole".

**Le sette clip** durano quanto la scheda: `idle` 36 fotogrammi di respiro, `fire` e `fire_last`
(colpo secco, otturatore che resta indietro sull'ultimo), `reload` e `reload_empty` (caricatore che
esce inclinato, arma che si abbassa di 14 mm e ruota di 5°, otturatore che si chiude a fine
ricarica a vuoto), `draw` e `holster` (entra da +5,5 cm con 17° di becco).


---

# Cadenza e rinculo (seconda passata)

**Cadenze.** Tutte le armi sono state rallentate del 20–40% e riallineate ai riferimenti di
Valorant: pistole 240/220 RPM, automatica 620, rivoltelle 180/90, SMG 520 (raffica) / 600 / 700,
pompe 55/110, fucili **540 (Anvil) e 610 (Service) — entrambi full-auto**, cecchini 110/120/40,
mitragliatrice 660. Il ciclo (`WeaponDef.Cycle`, in secondi) è anche la durata della clip `fire`,
che viene rigenerata a ogni build: le armi lente hanno animazioni lunghe quanto il loro ciclo
(il tetto della clip è passato da 24 a 40 fotogrammi, altrimenti la pompa da 1,09 s veniva tagliata).

**Rinculo alla Valorant.** La canna sale di molto nei primi colpi e sempre meno dopo
(`RecoilUp = 0,34 + 0,0165 × danno`), con una deriva orizzontale a zig-zag (`0,06 + 0,0065 × danno`),
ridotta del 28% in mira. La mira **rientra esattamente dove era** 0,10 s dopo l'ultimo colpo, e i
colpi seguono la camera VERA (rinculo compreso): prima la traiettoria usava yaw/pitch senza il
rinculo, quindi si sparava sempre al punto pre-rinculo mentre lo schermo saliva.

## Aggiornamento 26 ottobre 2026 · cadenze +20%

Su richiesta di design **tutte le cadenze sono state velocizzate del 20%**: pistole 288/264,
automatica 742, rivoltelle 216/108, SMG 626/720/837, pompe 66/132, fucili 649/735/132,
cecchini 144/48, mitragliatrice 791, coltello un fendente ogni 0,50 s. Le schede qui sopra e i
test sono allineati a questi numeri: il riferimento Valorant resta il punto di partenza, non un
vincolo. Le **ricariche non sono cambiate**: il tempo per ricaricare è la valvola che tiene in
piedi il ritmo quando la cadenza sale.

Con l'occasione è stato corretto un difetto che rendeva la cadenza **finta**. Il cooldown del colpo
veniva azzerato a ogni sparo, quindi a 64 Hz la cadenza vera era sempre il multiplo di tick
successivo: una SMG da 837 RPM ne faceva **640**, e il +20% sarebbe stato invisibile proprio sulle
armi veloci (dove conta). Ora il resto che avanza sotto il tick viaggia nel colpo successivo
(`Simulation.ApplyWeapon`, pavimento a −dt, nessun accumulo dopo una pausa) e un test misura la
cadenza vera in un secondo di grilletto: *armi: a 64 Hz la cadenza vera è quella della scheda*.

---

## Le armi sono ISTANZE (26 ottobre 2026)

Un'arma non è uno slot: è un oggetto con le **sue** munizioni, il segno di "comprata in questa fase
di acquisto" e la tasca che l'ha pagata. Da lì vengono le regole:

- **ricomprarla** ridà caricatore e riserva pieni, e la paghi;
- la sostituita **comprata in questa fase si vende** (rimborso intero); quella **portata dal round
  prima si lascia a terra**;
- **G** la lancia ~1,2 m in avanti, **F** raccoglie quella più vicina (entro 2 m) con le sue munizioni;
- morendo le armi cadono dove sei; a fine round il campo si pulisce, e chi è sopravvissuto tiene il
  loadout con i caricatori pieni.

Regole e soldi: `game/sim/Economy.cs` (`Shop`, `Economy`) e doc [04](04-economia-e-acquisti.md).
Suoni: `docs/17-audio-armi.md`. Nel client: `DroppedWeapons` (volumi a terra, suono 3D della caduta,
suggerimento di raccolta).
