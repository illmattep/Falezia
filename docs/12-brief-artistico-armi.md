# 12 · Brief artistico delle armi

> **Documento per il modellatore e per l'artista.** I numeri di gioco stanno nel doc 11.
> Qui c'è solo **come deve essere fatta ogni arma** per poter essere modellata, resa e letta in partita.

---

## La regola prima di tutte: la silhouette

> **Un'arma di FALEZIA deve essere identificabile dal suo profilo a 30 metri,
> in un render a un colore solo, sbattendoci sopra gli occhi per mezzo secondo.**

Non è un esercizio di stile. In un tattico senza respawn, **riconoscere che arma ha in mano il nemico
è un'informazione di gioco**: sapere che è un Longshot cambia la tua rotta, sapere che è uno Scrap
cambia il tuo coraggio. Se le armi si somigliano, quell'informazione non esiste.

### Il test obbligatorio

Prima di approvare un modello:

1. Rendering **a tinta unita nera**, sfondo bianco, ortografico laterale
2. **Sfocatura di 8 px**
3. Guarda per **mezzo secondo**
4. Se non riesci a dire **quale delle 15 armi è**, il modello va rifatto

**E il test collettivo:** metti tutte e 16 le silhouette **in nero pieno, affiancate, alla stessa scala**.
Se due si confondono, una delle due deve cambiare forma — non colore, non texture: **forma.**

---

## Il modello che conta è il *viewmodel*

In un FPS non vedi l'arma: vedi **la parte posteriore dell'arma e le mani.**

| Classe | Quanto se ne vede in prima persona |
|---|---|
| **Pistole** | **Tutto.** Sono corte: l'intera arma sta in schermo, in basso a destra |
| **SMG** | **~65%** — dalla culatta alla volata. La canna esce dal bordo destro |
| **Fucili a pompa** | **~45%** — la canna è quasi tutta fuori schermo |
| **AR** | **~55%** — dal grilletto in poi. Della volata vedi solo il fumo |
| **Cecchini** | **~50–60%**; in mira la vista è dal cannocchiale |
| **Coltello** | **Tutto** |

> **Conseguenza pratica per il modellatore:** i poligoni e i dettagli vanno spesi su
> **culatta, selettore di fuoco, caricatore, impugnatura e calcio**. Sono le superfici che il giocatore
> tocca con gli occhi per cento ore. **Sulla volata non spendere niente**: la vede solo il nemico, e
> la vede da venti metri.

**L'eccezione è l'arma in mano al nemico.** Quella la vedi per intero, da 10-40 m, controluce. Ed è
per questo che la silhouette conta più del dettaglio: **è l'unica cosa che vedrai davvero.**

---

## Il foglio di render

Ogni arma va consegnata su **una tavola**, con queste viste:

| Vista | Note |
|---|---|
| **1 · Profilo laterale sinistro** | **Ortografico.** È la vista principale, quella del test di silhouette. Arma centrata, riempie l'85% del frame |
| **2 · Tre quarti anteriore sinistro** | Prospettica 35 mm, per leggere il volume |
| **3 · Tre quarti posteriore destro** | La vista che il giocatore vede davvero in prima persona |
| **4 · Dettaglio culatta** | Ingrandimento, per i marchi e i comandi |
| **5 · Barra di scala** | Un righello da **100 mm** accanto all'arma. Obbligatorio: le proporzioni sono reali |

### Setup di luce (identico per tutte)

| Luce | Posizione | Temperatura | Intensità |
|---|---|---|---|
| **Principale** | 45° in alto a sinistra | **5600 K** | riferimento |
| **Riempimento** | −30° a destra | 5600 K | **−1,5 stop** |
| **Bordo** | dietro, leggermente in alto | **6500 K** | +0,5 stop, sottile |

**Sfondo: grigio neutro 18% (`#4A4A4A`), piatto, nessun gradiente.**
**Vietato:** fumo, scintille, bossoli, motion blur, messa a fuoco selettiva, luci colorate drammatiche.
Deve leggere come un **catalogo militare**, non come una copertina.

---

## Regole di finitura valide per tutte le armi

| Regola | Valore |
|---|---|
| **Nessun metallo a specchio** | Ogni metallo è **ossidato, brunito, fosfatato o verniciato**. Nessuna cromatura, mai |
| **Rugosità** | Polimero **0,80** · metallo fosfatato **0,65** · canna **0,40** · brunitura **0,50** · cuoio **0,85** |
| **Usura obbligatoria** | **Almeno 3 punti di usura visibili per arma.** Vernice mancante sugli spigoli, metallo a nudo, graffi paralleli dove si appoggia |
| **Nessuna superficie pulita** | Neanche l'arma più nuova è nuova. Le armi hanno tutte almeno un proprietario precedente |
| **Vietato** | LED, olografie, pannelli luminosi, loghi colorati, decori, scritte sgargianti, qualsiasi elemento futuristico |
| **Vietato il legno lucido** | Il legno è **verniciato opaco, consumato, con venature in vista** dove la vernice è andata via |

### I fabbricanti fittizi

Nessun nome reale. Cinque marchi, ognuno con un carattere preciso:

| Marchio | Carattere | Armi |
|---|---|---|
| **SERAFINI ARMI** | Pistole di servizio, industriali, anonime | Standard, Hush, Chatter |
| **VITALE & FIGLI** | Armi **civili** da caccia e tiro. Legno, ottone, eleganza fuori posto | Snub, Hammer, Choke |
| **KESSLER** | Militare **moderno**. Polimero nero, alluminio, linee dritte | Service, Blitz, Sweeper |
| **ARSENALE 7** | Armi d'ordinanza **vecchie e robuste**. Acciaio stampato, legno | Anvil, Cadence |
| **TIRRENO PRECISION** | Armi da **tiratore**. Canne lunghe, ottiche, cuoio | Spotter, Longshot |
| **(nessuno)** | Lo **Scrap non ha fabbricante**: è costruito in officina. Numero graffiato a mano | Scrap |

### Palette condivisa

| Materiale | Hex | Rugosità |
|---|---|---|
| Polimero nero opaco | `#1B1B1D` | 0,80 |
| Acciaio fosfatato | `#3A3D40` | 0,65 |
| Acciaio brunito | `#56483C` | 0,50 |
| Metallo a nudo (usura) | `#8A8D8F` | 0,45 |
| Legno verniciato scuro | `#4A3226` | 0,70 |
| Legno a nudo (usura) | `#6B4A34` | 0,75 |
| Lamiera grigia sfogliata | `#6E7275` | 0,72 |
| Nastro adesivo telato | `#3E4145` | 0,90 |
| Gomma guancette | `#141416` | 0,92 |
| Cuoio (guancette, cinghie) | `#6B4A2E` | 0,85 |
| Tela (cinghie) | `#5A5245` | 0,88 |
| Ottone (parti civili) | `#8C7434` | 0,45 |

---
---

# I PISTOLE

## 1 · STANDARD · *Serafini Armi*

**Silhouette a 30 m:** pistola compatta a **linea orizzontale continua**. Carrello piatto, nessun
elemento sporgente tranne il cane. **È la sagoma più "vuota" dell'arsenale**: se non riconosci niente,
è la Standard.

| | |
|---|---|
| **Dimensioni** | **197 × 138 × 33 mm** · canna **112 mm** |
| **Materiali** | Carrello: acciaio fosfatato `#3A3D40` · Fusto: polimero nero `#1B1B1D` · Mire: bianche opache |
| **Peso visivo** | Compatta, "vuota". Deve sembrare economica senza sembrare fragile |

**Cosa deve esserci:**
1. **Mire fisse a tre punti**, bianche, leggermente sporche di vernice
2. **Cane esterno** visibile sul retro del carrello
3. **Levamolle quadrato** sopra l'impugnatura
4. **Rigature di presa** sull'impugnatura: reticolo a piramidi, non linee
5. **Scanalature di presa sul carrello** (5 per lato), smussate
6. **Feritoia di espulsione** a destra, con bordo lucido da usura
7. **Nessuna slitta, nessuna sicura manuale**: solo la leva del caricatore sul lato sinistro

**Usura:** vernice del carrello consumata **sulla punta e sugli spigoli frontali** fino al metallo
`#8A8D8F`. Graffi paralleli sul lato sinistro, dove striscia nella fondina. Mire con vernice bianca
**scheggiata al 30%**.

**Marchi:** **`SERAFINI ARMI — ITALIA`** in rilievo sul lato sinistro del carrello, in carattere
sans-serif industriale. Numero di serie **inciso a laser** sul fusto, davanti al grilletto:
`S-9 41782`. Calibro `9×19` in piccolo sul lato destro.

**Nota per il render:** è l'arma che va vista **per prima** nel foglio collettivo, perché è la più
semplice. La sua funzione è fare da **metro di paragone**: se un'altra arma non si distingue da questa,
non ha abbastanza carattere.

## 2 · HUSH · *Serafini Armi*

**Silhouette a 30 m:** la Standard con **un cilindro lungo e grosso attaccato davanti**. Il rapporto
fra carrello corto e canna lunga è **l'unica cosa che conta**: il profilo è "corto dietro, lungo
davanti". Nessun'altra pistola è così sbilanciata in avanti.

| | |
|---|---|
| **Dimensioni** | **250 × 145 × 47 mm** · canna **112 mm** + soppressore **105 mm** |
| **Materiali** | Tutto **acciaio fosfatato nero opaco** `#3A3D40`, soppressore in **acciaio con guaina opaca** `#33363A` · Mire nere |
| **Peso visivo** | Ciononostante il soppressore, deve sembrare **pesante davanti**. Il baricentro visivo sta sulle dita |

**Cosa deve esserci:**
1. **Soppressore integrato**: cilindro **Ø 38 mm** con **6 anelli di lavorazione** sulla superficie
   (non scanalature profonde: solo righe larghe 2 mm) — serve a dare scala
2. **Tacca di riferimento** incisa in alto sulla guaina, allineata alla mira posteriore
3. **Canna scoperta per 15 mm** fra carrello e soppressore: quel pezzo di canna nuda è importante,
   è l'unico punto di continuità fra i due volumi
4. **Mire nere a tre punti**, più basse della Standard (per non impigliarsi col soppressore)
5. Carrello Standard, ma **serigrafia rimossa** con vernice mancante al posto del marchio
6. **Nastro adesivo telato** avvolto sull'impugnatura: 3 giri, bordi sfrangiati, `#3E4145`
7. **Nessun marchio di fabbrica**: il fusto è stato **ripulito con la mola**

**Usura:** la vernice del soppressore è **quasi completamente andata sull'anello frontale**, con metallo
a nudo `#8A8D8F` e **annerimento da gas** nella corona di uscita (fuliggine grigio-nera, opaca al 100%).
Nastro adesivo **consumato alle estremità**, con fibre sollevate.

**Marchi:** solo il numero di serie sul fusto, `S-9 21455`, **parzialmente cancellato dalla mola**.
Tre lettere incise a mano sotto il grilletto: **`M.R.`** (un proprietario precedente).

**Nota per il render:** **la vista 3/4 anteriore è obbligatoria** per questa arma. Il profilo laterale
funziona, ma la cosa che deve leggere è **il vuoto fra canna e soppressore**.

## 3 · SNUB · *Vitale & Figli*

**Silhouette a 30 m:** **nessun carrello, tutto tamburo.** Il profilo è un blocco corto con un cerchio
in mezzo e una canna mozzata. **È l'unica arma dell'arsenale con un profilo tondeggiante.**

| | |
|---|---|
| **Dimensioni** | **165 × 125 × 38 mm** · canna **51 mm** · tamburo **Ø 42 mm** |
| **Materiali** | Acciaio brunito `#56483C` con **usura estesa** a metallo `#8A8D8F` · Guancette in **gomma nera** `#141416` |
| **Peso visivo** | Massiccia per la sua lunghezza. Deve sembrare **vecchia e usata**, non moderna |

**Cosa deve esserci:**
1. **Tamburo a 6 camere**, con **6 nicchie di bloccaggio** visibili sul bordo esterno e **scanalature
   longitudinali** leggere fra le camere
2. **Astina di espulsione sotto la canna**, con **testa zigrinata** (zigrinatura incrociata, 12 righe)
3. **Cane a sperone largo**, zigrinato, con il caratteristico profilo ricurvo
4. **Canna con nervatura superiore** e mira fissa a lama
5. **Tacca posteriore a intaglio**, scalpellata: deve sembrare fatta a mano
6. **Vite di smontaggio** visibile sul lato sinistro, con **testa graffiata** da cacciavite
7. **Guancette in gomma** con **marchio a rilievo** (un ovale con dentro `V&F`)

**Usura:** **la più consumata dell'arsenale.** Brunitura andata via sul **bordo del tamburo, sulla
testa dell'astina e sulla punta della canna**, con metallo a nudo `#8A8D8F` e **ossidazione leggera**
(marrone scuro `#5C4632`) nelle nicchie di bloccaggio. Gomma delle guancette **liscia e lucida** dove
si appoggia la mano.

**Marchi:** **`VITALE & FIGLI — GARDONE`** in elegante corsivo inciso sul lato sinistro della canna,
con una **cornice a doppia linea** e un piccolo **stemma a forma di fiamma**. Sotto il tamburo, il
**timbro di collaudo** della banca di prova: un piccolo punzone con un'aquila, **quasi illeggibile**.
Numero di serie `V&F 3811` sull'astina.

**Nota per il render:** il tamburo va reso con cura **nella vista 3/4 posteriore**: la zigrinatura
dell'astina e le camere del tamburo sono il dettaglio che dice al giocatore "quell'arma va ricaricata
in tre secondi".

## 4 · HAMMER · *Vitale & Figli*

**Silhouette a 30 m:** **troppo grande per essere una pistola.** Il profilo è un rettangolo alto e
lungo, con **una canna con una fessura in cima** e un impugnatura che sembra sottile in confronto.
**È l'unica arma con finitura chiara e riflettente: si riconosce dal riflesso.**

| | |
|---|---|
| **Dimensioni** | **270 × 165 × 45 mm** · canna **152 mm** con **fessura di compensazione superiore** |
| **Materiali** | **Acciaio inox `#B8BCC0`, rugosità 0,32** — l'unico metallo semi-lucido del gioco · Impugnatura in **legno di noce** `#4A3226` |
| **Peso visivo** | Il pezzo più ingombrante dell'arsenale a una mano. Deve sembrare **fuori posto in una guerra** |

**Cosa deve esserci:**
1. **Fessura di ventilazione** sulla sommità della canna: una feritoia lunga, con **bordi anneriti da
   gas** su entrambi i lati della volata
2. **Mire regolabili**: posteriore a tacca con **due viti di registrazione** e **scala numerata**
   incisa, anteriore a lama su base imbullonata
3. **Cane esterno a sperone** con **foro di alleggerimento** triangolare
4. **Sicura manuale a leva** sul carrello, zigrinata, **in posizione visibilmente rialzata**
5. **Rigature** sull'impugnatura: **diamanti incrociati** profondi, in legno
6. **Rilievo frontale sotto la canna** per il rinculo: una protuberanza tozza
7. **Righe di lavorazione visibili** sulla canna e sul carrello — l'acciaio è lavorato, non stampato

**Usura:** **graffi fini e diffusi** su tutta la canna (acciaio lucido = mostra tutto). Annerimento
**concentrato sui lati della fessura**. Legno dell'impugnatura **consumato e scuro sul lato destro**
(dove poggia il palmo) e **con una scheggiatura** sul fondo del calcio. Viti delle mire con **teste
segnate da cacciavite**.

**Marchi:** **`VITALE & FIGLI`** inciso in **grande**, con cornice ornata, sul lato sinistro del
carrello, sotto la scritta **`MAGNUM .50`** in carattere **sans-serif moderno** — il contrasto fra i
due caratteri dice che l'arma è un prodotto civile, non militare. Sulla canna: **`GARDONE — ITALIA`**.
Numero di serie `V&F 7719` sul lato destro.

**Nota per il render:** va resa **con la luce di bordo più forte del solito** (+0,5 stop), perché è
l'unica arma che specchia. Se non riflette un po', hai sbagliato la rugosità.

## 5 · CHATTER · *Serafini Armi*

**Silhouette a 30 m:** la Standard **con un blocco rettangolare che sporge sotto**. Il caricatore
esteso è **più lungo della canna**, e questa sproporzione è tutto il profilo. **È l'unica pistola
squadrata in basso.**

| | |
|---|---|
| **Dimensioni** | **210 × 195 × 35 mm** (l'altezza include il caricatore esteso) · canna **115 mm** |
| **Materiali** | Carrello acciaio fosfatato `#3A3D40` · Fusto polimero `#1B1B1D` · **Compensatore stampato** `#2C2F32`, finitura grezza da stampo |
| **Peso visivo** | Sproporzionata e **visibilmente assemblata male**. Deve sembrare un pezzo aggiunto dopo |

**Cosa deve esserci:**
1. **Caricatore esteso da 20**: sporge di **55 mm** sotto l'impugnatura, con **nervature orizzontali**
   e **indicatori di colpo laterali** (una fila di 5 fori)
2. **Compensatore avvitato** in punta: **2 feritoie laterali** e una superiore, con **bordi anneriti**
   e **segni di chiave** sulle facce piatte (è stato stretto con una pinza)
3. **Selettore di fuoco rotante** sul lato sinistro, **zigrinato**, con **tre posizioni incise**:
   un punto, due punti, un simbolo di raffica
4. **Cane interno** (nessuno sperone): il retro del carrello è liscio, e questa è la differenza
   principale dalla Standard nel profilo
5. **Rigature a piramidi**, identiche alla Standard (stesso fabbricante)
6. **Asola di sgancio del caricatore allargata** — è stata limata per farla funzionare meglio
7. **Nessuna slitta**

**Usura:** vernice del **compensatore quasi assente** e **annerimento da gas molto marcato** (la zona
più sporca di tutto l'arsenale). Bordo del caricatore esteso **graffiato in basso** da ogni caduta.
Limatura visibile attorno all'asola del caricatore.

**Marchi:** **`SERAFINI ARMI — ITALIA`** sul lato sinistro, **parzialmente coperto dal compensatore**.
Numero di serie `S-9 60233` sul fusto. Accanto al selettore, **tre simboli incisi** piccoli:
`1` · `3` · `∞`. Il simbolo dell'infinito è stato **inciso a mano dopo**, con un bulino, e si vede
che è storto.

**Nota per il render:** va resa **con il caricatore pieno in vista**. La cosa che deve comunicare è
"quest'arma ha 20 colpi e li spara in un secondo e due": il modellatore deve far sentire **che il
caricatore è troppo lungo per quell'arma**.

---
---

# LE SMG

## 6 · CADENCE · *Arsenale 7*

**Silhouette a 30 m:** **calcio pieghevole a tubo, caricatore curvo lungo, maniglione di puntamento.**
Il profilo è basso e allungato, con **un'asta sottile dietro** e **una banana in mezzo**. **È l'unica
SMG con il caricatore molto curvo.**

| | |
|---|---|
| **Dimensioni** | **680 × 260 × 60 mm** (calcio esteso) |
| **Materiali** | Corpo **acciaio stampato fosfatato** `#3A3D40` · Paramano e impugnatura **polimero nero** `#1B1B1D` · Calcio **tubo in acciaio** `#4A4E52` |
| **Peso visivo** | Sottile e lunga. Deve sembrare **un attrezzo industriale**: niente curve, solo linee dritte |

**Cosa deve esserci:**
1. **Caricatore curvo a banana da 30**, con **nervature orizzontali** e **bolli di saldatura** sui
   fianchi — deve sembrare stampato, non fresato
2. **Calcio a tubo telescopico** con **3 fori di regolazione**: si vede quale posizione è stata usata
   dalla **consumazione della vernice attorno al foro**
3. **Maniglione di puntamento anteriore** con **impugnatura anteriore** (è l'unico modo di tenere
   quest'arma) — zigrinato
4. **Selettore di fuoco** a levetta sul lato sinistro, con **tre tacche stampate** e **pittogrammi**
5. **Mire: tamburo posteriore a 4 fori** (una ghiera rotante) e **mira anteriore ad anello**
6. **Feritoia di espulsione** con **deflettore** piegato, e **annerimento da gas** dietro la feritoia
7. **Cinghia di tela** `#5A5245` agganciata sul lato sinistro, **annodata con un nodo semplice** perché
   la fibbia originale è andata persa

**Usura:** vernice **completamente andata sull'impugnatura anteriore e sul tubo del calcio**, metallo a
nudo `#8A8D8F`. **Annerimento da gas** sull'otturatore visibile. Paramano **graffiato in modo
irregolare**, con un solco profondo sul lato destro (è stato strisciato su un muro). Bolli sul
caricatore, che è stato **lasciato cadere molte volte**.

**Marchi:** **`ARSENALE 7`** stampato in **rilievo** sul lato sinistro della culatta, in carattere
**stencil militare**, accanto al numero di serie `A7-19288` e a un **timbro circolare di collaudo**
sbiadito. Sulla canna, vicino alla culatta: **`9×19 — LOTTO 4/86`**.

**Nota per il render:** **la vista laterale deve mostrare il calcio esteso al massimo**, perché il tubo
è il tratto che la distingue da qualsiasi altra SMG del foglio.

## 7 · BLITZ · *Kessler*

**Silhouette a 30 m:** **un blocco squadrato. Nient'altro.** Nessun caricatore sporgente, nessun tubo,
nessun maniglione. Il profilo è **un rettangolo compatto con un buco per la mano** e un'ottica in cima.
**È l'unica arma quasi rettangolare dell'arsenale.**

| | |
|---|---|
| **Dimensioni** | **500 × 220 × 58 mm** |
| **Materiali** | Corpo monoscocca in **polimero grigio-verde** `#3E4441` (l'unico polimero non nero del gioco) · Parti in movimento **acciaio fosfatato** `#3A3D40` |
| **Peso visivo** | Compatto e pieno. Deve sembrare **un attrezzo chiuso**, quasi sigillato |

**Cosa deve esserci:**
1. **Caricatore orizzontale sopra la canna**, con i proiettili **visibili nella finestra superiore**
   (è il dettaglio che dice "50 colpi in questo blocco"). Il caricatore è **un coperchio traslucido
   fume**, non trasparente: si intravedono le cartucce, non si vedono bene
2. **Scanalature di ventilazione** sul paramano, **7 fessure parallele** allineate alla canna
3. **Manico a pistola integrato** nel monoscocca, con **rigature a griglia** e **guardia del grilletto
   squadrata**
4. **Slitta superiore con ottica montata**: anelli in acciaio, ottica cilindrica con **ghiera di
   regolazione zigrinata** su entrambi i lati
5. **Espulsore inferiore**: la finestra di espulsione è **sotto l'arma**, non sul lato — è la
   particolarità tecnica che si deve vedere
6. **Selettore a pulsante** sul lato sinistro, **grande e squadrato**, con due posizioni
7. **Asola di sgancio del caricatore** sulla sommità posteriore, con **leva a molla** protetta

**Usura:** **il polimero si graffia in modo diverso dal metallo**: usura **biancastra e opaca** sugli
spigoli `#5C625E`, non metallo a nudo. Annerimento **nella finestra di espulsione inferiore**. La
finestra del caricatore in fume ha **una crepa** su un angolo. Ghiera dell'ottica con **segni di
moneta** (è stata regolata con una moneta).

**Marchi:** **`KESSLER`** in **rilievo elegante** sulla parte sinistra del monoscocca, in carattere
sans-serif **moderno e stretto**, accanto a un **piccolo marchio geometrico** (un esagono tagliato).
Numero di serie `K-46001` **stampato a punti** (matrice a punti, non inciso). Sulla slitta:
**`KESSLER RAIL — 4,6×30`**.

**Nota per il render:** questa è l'arma dove **il colore conta più che altrove**. Il polimero
grigio-verde `#3E4441` deve staccare dal nero degli altri: se nel foglio collettivo il Blitz sembra
un'altra arma nera, **il brief è stato disatteso**.

## 8 · SCRAP · *nessun fabbricante*

**Silhouette a 30 m:** **lamiera piegata e un'asta di filo metallico dietro.** Il profilo è
**spigoloso e aperto**: si vede attraverso, non c'è una monoscocca. **È l'unica arma dell'arsenale in
cui si vede il vuoto attraverso il corpo.**

| | |
|---|---|
| **Dimensioni** | **550 × 240 × 52 mm** |
| **Materiali** | **Lamiera d'acciaio piegata** `#6E7275`, con **vernice grigia sfogliata** (sotto: acciaio `#8A8D8F`) · Calcio in **filo d'acciaio Ø 8 mm** saldato · Impugnatura in **polimero nero** `#1B1B1D` |
| **Peso visivo** | Deve sembrare **il più economico dell'arsenale**, e non nasconderlo. Le tolleranze sono visibili |

**Cosa deve esserci:**
1. **Corpo a U rovesciata** in lamiera da 2 mm, con **saldature a punti visibili** lungo i bordi
   (7-8 punti per lato, ognuno un piccolo imbuto)
2. **Caricatore corto da 20**, diritto, **in lamiera**, con **nervature poco profonde**
3. **Calcio scheletrico in filo metallico**: un'asta piegata che forma un rettangolo, **saldato in
   tre punti** alla culatta, con **la saldatura non carteggiata**
4. **Fessura di espulsione grande e irregolare**, **ritagliata a mano** (i bordi non sono diritti)
5. **Mira posteriore**: una **lamella piegata** saldata, con un intaglio a V **fatto con la lima**
6. **Mira anteriore**: un **perno saldato** in verticale
7. **Manico a pistola separato**, avvitato al corpo con **due viti a testa esagonale** visibili, con
   **rondelle che sporgono**

**Usura:** **la più evidente dell'arsenale.** Vernice sfogliata **in grandi chiazze** su tutto il
corpo, con il contorno irregolare della sfogliatura visibile. **Ossidazione superficiale** marrone
`#5C4632` sulle saldature. **Nastro adesivo telato** `#3E4145` avvolto due volte sull'impugnatura,
con **bordi sfrangiati e uno strappo a metà**. Il calcio in filo è **piegato fuori asse di 3 mm**
(è stato usato come leva). Teste delle viti **spanate**.

**Marchi:** **nessun marchio di fabbrica.** Sulla culatta, un **numero graffiato a mano con un bulino**
— `0 4 7` — **irregolare, tre colpi di mano diversi**. Accanto, un **timbro a vernice rossa
sbiadito**, quasi illeggibile: si intuisce solo la parola **`SEQUESTRO`**.

**Nota per il render:** il modellatore deve **resistere alla tentazione di raddrizzare le cose.** Le
saldature storte, il calcio piegato e la fessura irregolare **non sono errori**: sono l'arma. Se lo
Scrap sembra ben fatto, hai sbagliato il brief.

---
---

# I FUCILI A POMPA

## 9 · CHOKE · *Vitale & Figli*

**Silhouette a 30 m:** **una linea lunga e sottile, con un'asta di legno in mezzo.** Nessun
caricatore, nessuna massa centrale: è **tutta canna e tutto calcio**. **È l'arma più lunga e sottile
dell'arsenale**, e si riconosce da quello.

| | |
|---|---|
| **Dimensioni** | **1150 × 190 × 55 mm** · canna **710 mm** |
| **Materiali** | Canna **acciaio brunito** `#56483C` · Calcio e astina **legno di noce verniciato scuro** `#4A3226` · Piastra di culatta **acciaio brunito con incisioni** |
| **Peso visivo** | Lunga e **civile**. Deve sembrare un fucile da caccia requisito, e in effetti lo è |

**Cosa deve esserci:**
1. **Canna singola** con **nervatura superiore** e **mirino a sfera d'ottone** `#8C7434` in punta
2. **Strozzatore fisso** visibile come un **leggero ispessimento** agli ultimi 40 mm di canna, con
   **`FULL` punzonato** sopra
3. **Astina in legno** con **6 scanalature longitudinali** per la presa e **un incavo per il pollice**
   sul davanti
4. **Piastra di culatta in acciaio brunito** con **incisioni a volute** (poco profonde, consumate)
   e **una testa di cinghiale in rilievo** al centro — il marchio di fabbrica
5. **Doppio cane interno** non visibile; **leva di apertura** sul lato destro, **unghia larga**
6. **Guardia del grilletto in acciaio**, con **due grilletti** affiancati (il secondo è un residuo
   storico, e va modellato) — **è un dettaglio che nessun altro fucile del gioco ha**
7. **Cinghia in cuoio `#6B4A2E`** con **fibbia in ottone** e **fori consumati**: la fibbia è su un
   foro che si vede **allargato dall'uso**

**Usura:** brunitura **consumata sul vivo della canna** (i bordi, non i fianchi) fino a metallo
`#8A8D8F`. **Legno con due scheggiature**: una sul calcio, una sull'astina, entrambe con **bordo
scuro** (vecchie). **Ossidazione leggera** sul fondo della canna, vicino alla culatta. Ottone del
mirino **annerito e lucido** (l'unica parte lucida dell'arma: viene toccata).

**Marchi:** **`VITALE & FIGLI`** in **corsivo elegante con cornice**, inciso sul lato sinistro della
canna, sopra la culatta, con la testa di cinghiale ripetuta in piccolo. Sulla canna, **`CAMERA 12/70`**
e il **timbro di prova** con la data `4/71`. Numero di serie `V&F 20934` sotto la culatta.

**Nota per il render:** **è l'arma più difficile da rendere leggibile.** Canna nera lunga su fondo
grigio = una linea. Il modellatore deve **far vivere il legno**: venature, scheggiature, la differenza
di lucentezza fra il legno e il metallo. Se il Choke è solo una linea, non funziona.

## 10 · SWEEPER · *Kessler*

**Silhouette a 30 m:** **un blocco nero con un caricatore a scatola che sporge sotto.** Rispetto al
Choke, il profilo è **corto e grosso** e con **una sporgenza rettangolare in basso**. **È l'unico
fucile a pompa con il caricatore estraibile.**

| | |
|---|---|
| **Dimensioni** | **950 × 250 × 62 mm** · canna **470 mm** |
| **Materiali** | Corpo **polimero nero** `#1B1B1D` · Canna e otturatore **acciaio fosfatato** `#3A3D40` · Caricatore **lamiera fosfatata** |
| **Peso visivo** | Compattta e militare. Deve sembrare **attrezzata**, non civile |

**Cosa deve esserci:**
1. **Caricatore a scatola estraibile da 7**, dritto, con **finestra di controllo laterale** e
   **nervature orizzontali**
2. **Maniglione di trasporto superiore** con **slitta integrata** e **fori di alleggerimento**: è il
   tratto che lo distingue maggiormente dal Choke
3. **Astina con impugnatura anteriore** integrata, in polimero, **con superficie zigrinata a griglia**
4. **Guardia del grilletto squadrata con ponte**, abbastanza grande per un guanto
5. **Leva di sgancio del caricatore** dietro la guardia, **grande e squadrata**
6. **Anelli di sospensione** sul lato sinistro della culatta, con **una cinghia in tela** agganciata
   con un **moschettone**
7. **Finestra di espulsione laterale destra** con **deflettore**, e **annerimento da gas**

**Usura:** polimero con **usura biancastra sugli spigoli** `#5C625E`. **Annerimento da gas molto
marcato** attorno alla finestra di espulsione e sul deflettore. **Caricatore con bordo inferiore
graffiato** e **vernice mancante sull'angolo frontale**. La zigrinatura dell'astina **consumata al
centro** (il punto dove poggia sempre la mano).

**Marchi:** **`KESSLER`** in rilievo stretto sul lato sinistro del corpo, con l'**esagono tagliato**,
accanto a **`12 GAUGE — SEMIAUTO`** in carattere tecnico e al numero di serie `K-31774` stampato a
punti. Sul caricatore, **inciso a mano**: **`7`**.

**Nota per il render:** il contrasto con il Choke deve essere **immediato**: stesso foglio, stesso
sfondo, e uno è legno e acciaio brunito, l'altro è tutto polimero nero e spigoli. Se in bianco e nero
si confondono, il problema è il **caricatore sporgente** — rendilo più evidente.

---
---

# I FUCILI D'ASSALTO

## 11 · ANVIL · *Arsenale 7*

**Silhouette a 30 m:** **caricatore molto curvo, calcio in legno, canna scoperta che finisce in un
grosso mirino.** Il profilo **si inclina in avanti**: il baricentro visivo è sulla canna. **È l'unica
AR con il legno e con la canna scoperta così a lungo.**

| | |
|---|---|
| **Dimensioni** | **880 × 250 × 58 mm** · canna **415 mm** |
| **Materiali** | Culatta e canna **acciaio stampato fosfatato** `#3A3D40` · **Calcio e impugnatura in legno verniciato scuro** `#4A3226` · Caricatore **lamiera brunita** `#56483C` |
| **Peso visivo** | Massiccio, lungo, **vecchio**. Deve imporre rispetto: è l'arma più rispettata del gioco |

**Cosa deve esserci:**
1. **Caricatore molto curvo da 30**, con **nervature trasversali profonde** e **bolli di saldatura**
   sui fianchi. La curvatura è **il tratto più riconoscibile** dopo il legno
2. **Calcio in legno pieno**, con **incavo per la guancia** sul lato sinistro e **calciolo in acciaio**
   con **due viti e una piastra zigrinata**
3. **Paramano in legno** con **due feritoie di raffreddamento** e **un incavo per la mano**
4. **Mirino a corona (ad anello)** in punta, su **base rialzata in acciaio** — è grosso e visibile,
   e contribuisce alla silhouette
5. **Mira posteriore a tacca** regolabile in deriva, su **slitta laterale**, con **scala numerata**
   incisa
6. **Leva di armamento sul lato destro**, **grande e ricurva**, con **battuta in acciaio** e **la
   brunitura consumata dal movimento** (una striscia lucida chiara lungo la corsa)
7. **Copriculatta stampato con nervature** longitudinali, **graffiato in modo evidente**

**Usura:** **il legno è il protagonista**: vernice **consumata sul calcio** dove appoggia la guancia
`#6B4A34`, **scheggiatura profonda** sull'angolo inferiore del calcio, e **venature in vista** sulle
parti più esposte. Brunitura **completamente andata** sulla leva di armamento, sulla punta della canna
e sull'angolo anteriore del caricatore. **Nastro adesivo telato** avvolto **tre volte attorno al
paramano di legno**, sopra una **crepa longitudinale** nel legno (il paramano è stato rotto e riparato).

**Marchi:** **`ARSENALE 7`** stampato in rilievo sul copriculatta, in **stencil militare largo**,
accanto a **`7,62×39`** e a un **numero di serie** `A7-0-4471`. Sulla culatta, un **timbro circolare**
con **`4/78`** e un **piccolo stemma con tre stelle**. Sul calcio in legno, **inciso a mano con un
chiodo**: **`III`** (tre tacche, come un conteggio).

**Nota per il render:** **la vista laterale sinistra è quella definitiva.** Legno scuro, caricatore
curvo, canna scoperta, mirino ad anello grosso: quattro elementi che nessun'altra AR possiede.
Il modellatore non deve "pulire" il nastro sul paramano: **la riparazione è il carattere dell'arma.**

## 12 · SERVICE · *Kessler*

**Silhouette a 30 m:** **una linea superiore diritta e piatta, un maniglione, un caricatore diritto.**
Il profilo è **orizzontale e regolare**: nessuna curva, nessuna sporgenza. **È l'arma più anonima
dell'arsenale, e l'anonimato è il suo tratto.**

| | |
|---|---|
| **Dimensioni** | **850 × 245 × 60 mm** · canna **370 mm** |
| **Materiali** | **Alluminio anodizzato nero opaco** `#232528` · Polimero nero `#1B1B1D` · Canna **acciaio fosfatato** `#3A3D40` |
| **Peso visivo** | Leggera e regolare. Deve sembrare **emessa da un deposito**, non scelta |

**Cosa deve esserci:**
1. **Maniglione di trasporto integrato** con **slitta nella parte inferiore** — è il tratto che la
   identifica contro l'Anvil, che ha invece il legno e il mirino ad anello
2. **Caricatore diritto da 30 in polimero**, con **nervature verticali** e **finestra di controllo
   laterale**. **Diritto, non curvo:** è la differenza principale contro l'Anvil nel profilo
3. **Paramano in polimero** con **6 feritoie di raffreddamento** e **guancia termica in alluminio**
4. **Otturatore forato**: un **buco di alleggerimento** sulla faccia destra, e **la brunitura consumata
   in una striscia** lungo la corsa
5. **Pulsante di sgancio caricatore** sulla sinistra, **squadrato e grande**
6. **Leva di armamento sul retro dell'otturatore**, a **forma di T**, zigrinata
7. **Guardia del grilletto squadrata con ponte**, e **leva del selettore a 45°** sul lato sinistro
   con **tre tacche stampate** (sicura, colpo singolo, raffica)

**Usura:** **l'usura più "burocratica" dell'arsenale**: niente sfogliature, solo **consumazione
uniforme**. Anodizzazione **consumata sugli spigoli inferiori** del corpo (appoggiato a terra mille
volte) fino a **alluminio a nudo `#9A9DA0`**. **Vernice mancante sul bordo del maniglione** dove si
afferra. **Graffi paralleli e regolari** sull'otturatore, lungo la corsa. **Nessuna ammaccatura
importante**: quest'arma non è mai caduta davvero.

**Marchi:** **`KESSLER`** in rilievo stretto sul lato sinistro, con l'**esagono tagliato**, accanto a
**`SERVICE RIFLE 5,56`** e al numero di serie `K-10022` stampato a punti. Sopra la culatta, una
**targhetta rettangolare in alluminio** fissata con **due rivetti**, con un **numero di matricola
diverso** da quello stampato: `4711-A`. Un **adesivo rettangolare bianco sbiadito** sul paramano, con
scritto a penna **`2°`** e una **croce**.

**Nota per il render:** è l'arma più difficile da rendere **interessante**, perché deve essere
anonima per progetto. La soluzione è **l'usura burocratica**: targhetta rivettata, adesivo, graffi
paralleli. Chi guarda deve pensare "questa è stata in un armadio per dieci anni", non "questa è noiosa".

## 13 · VANTAGE · *Kessler*

**Silhouette a 30 m:** **un'arma chiaramente più lunga e più grossa delle altre AR, con un'ottica
lunga e un calcio pieno e alto.** Il profilo è **pesante in tutte le direzioni**. **È l'unica AR con
un calcio regolabile a guancia alta.**

| | |
|---|---|
| **Dimensioni** | **1020 × 270 × 68 mm** · canna **510 mm** |
| **Materiali** | **Acciaio e polimero nero** `#1B1B1D` e `#3A3D40` · Calcio **polimero con guanciale regolabile** · Ottica **alluminio anodizzato nero** |
| **Peso visivo** | Il più grosso e più lento. **Deve sembrare pesante da alzare** |

**Cosa deve esserci:**
1. **Canna più lunga e più spessa** delle altre AR, con **4 feritoie di raffreddamento** vicino alla
   culatta e **freno di bocca a due camere** in punta
2. **Ottica lunga su anelli in acciaio**, con **ghiere di regolazione zigrinate** su due lati e
   **una torretta di elevazione alta**: l'ottica occupa più spazio dell'otturatore
3. **Calcio regolabile**: **guanciale alto in polimero** su **due colonne**, con **ghiera di
   regolazione** a vite. Si vede la **polvere nelle scanalature**
4. **Caricatore a scatola da 20**, diritto, **in acciaio stampato**, più corto ma più grosso di quello
   del Service — e **le due linguette di aggancio sono in acciaio, non polimero**
5. **Biella di armamento sul lato destro**, corta, con **pomello zigrinato**
6. **Bipiede ripiegabile?** — **no.** Il Vantage non ha bipiede, e va lasciato senza: deve sembrare
   un fucile da tiro portato a mano
7. **Paramano in polimero con nervature**, più corto e più tozzo di quello del Service

**Usura:** **metallo più che polimero**, perché pesa e tocca. Annerimento da gas **sui lati del freno
di bocca**. Vernice **consumata sullo spigolo inferiore della culatta** fino a metallo `#8A8D8F`.
**Polvere e sabbia** `#6B6255` impastata **nelle scanalature del guanciale** e **nelle feritoie di
raffreddamento**. Ottica con **segni di moneta sulle ghiere** e **un graffio obliquo** sulla campana
posteriore.

**Marchi:** **`KESSLER`** sul lato sinistro, con l'esagono, accanto a **`BATTLE RIFLE 7,62×51
SEMIAUTO`** in carattere tecnico stretto, e **`K-88012`** stampato a punti. Sull'ottica, un **timbro
ovale** con **`4×32`** e un **codice di lotto**. Sul calcio, **inciso a mano**: **`OK`** e una
**freccia verso l'alto**.

**Nota per il render:** il Vantage deve sembrare **troppo per il gioco**: troppo lungo, troppo grosso,
troppo ottica. È un'arma che comunica "se ti vedo da lontano, sei morto" — e per farlo deve **sembrare
difficile da portare**. Se sembra maneggevole, hai sbagliato.

---
---

# I CECCHINI

## 14 · SPOTTER · *Tirreno Precision*

**Silhouette a 30 m:** **canna lunga, ottica corta e alta, calcio scheletrato con un buco in mezzo.**
Il profilo ha **un vuoto nel calcio**, ed è quello che lo rende inconfondibile. **È l'unica arma con
il calcio a traliccio.**

| | |
|---|---|
| **Dimensioni** | **1225 × 250 × 62 mm** · canna **620 mm** |
| **Materiali** | Canna e culatta **acciaio fosfatato** `#3A3D40` · Calcio **traliccio in acciaio** `#4A4E52` con **guancetta in cuoio** `#6B4A2E` · Legno sull'impugnatura `#4A3226` |
| **Peso visivo** | Lungo e **sottile ma non fragile**. Deve sembrare un attrezzo da tiro, non un'arma da guerra |

**Cosa deve esserci:**
1. **Calcio scheletrato**: un **traliccio in tubi e piastre** che lascia **un vuoto rettangolare**
   grande al centro del calcio. È il tratto numero uno
2. **Guancetta in cuoio fissata con 4 viti a testa piatta**, consumata e lucida al centro
3. **Ottica corta e alta** su **slitta laterale** (non superiore): gli anelli sono **alti 35 mm**,
   abbastanza per vedere sotto. **La slitta laterale è una particolarità che si deve vedere**
4. **Canna lunga con nervatura superiore** e **freno di bocca a tre camere** con **feritoie laterali**
5. **Impugnatura a pistola in legno** con **rigature a diamanti** e **incavo per il pollice**
6. **Leva di armamento corta sul lato destro**, con **pomello sferico** zigrinato
7. **Caricatore curvo da 10**, **corto**, in lamiera brunita, con **nervature orizzontali**

**Usura:** **canna con brunitura consumata sui bordi** e **annerimento da gas** marcato davanti alla
culatta e sul freno di bocca. **Cuoio della guancetta liscio e lucido** al centro, con **il bordo
consumato fino alla tela**. Traliccio del calcio con **vernice mancante nei punti di saldatura**.
Ottica con **un graffio lungo sulla campana anteriore** e **le viti degli anelli con le teste
segnate**.

**Marchi:** **`TIRRENO PRECISION`** inciso in **carattere tecnico sottile** sul lato sinistro della
culatta, sopra una **canna stilizzata** e sotto la scritta **`SPOTTER 7,62×54`**. Sull'ottica: **`TP
4×26`** e un **numero di lotto**. Sulla culatta, il **timbro di prova con l'aquila**. Sul cuoio della
guancetta, **impresso a caldo**: una **ancora** e le lettere **`C.M.`**.

**Nota per il render:** **il vuoto nel calcio è tutto.** Il modellatore deve assicurarsi che nella
vista laterale ortografica **quel buco si veda contro lo sfondo** — cioè: l'arma non deve essere resa
su un fondo che riempie il vuoto. Va resa **con la vista 3/4 obbligatoria** per far leggere lo spessore
del traliccio.

## 15 · LONGSHOT · *Tirreno Precision*

**Silhouette a 30 m:** **la cosa più lunga del gioco, con una canna che non finisce mai e un'ottica
grossa.** Il profilo è **una linea continua e massiccia**, senza vuoti, senza sporgenze: solo **massa
orizzontale**. **È l'unica arma con il calcio pieno alto e il freno di bocca a tre camere.**

| | |
|---|---|
| **Dimensioni** | **1400 × 260 × 70 mm** · canna **740 mm** |
| **Materiali** | Canna **acciaio fosfatato** `#3A3D40` · Calcio **polimero nero pieno** `#1B1B1D` · Freno di bocca **acciaio brunito** `#56483C` · Ottica **alluminio anodizzato** |
| **Peso visivo** | **Il pezzo più pesante dell'arsenale.** Deve sembrare **un attrezzo da cantiere**, non un fucile elegante |

**Cosa deve esserci:**
1. **Freno di bocca a tre camere**, tozzo e grosso (**Ø 32 mm**), con **feritoie laterali profonde**
   e **bordi anneriti**: è il primo elemento che si nota
2. **Canna molto lunga** con **nervatura superiore continua** e **6 feritoie di raffreddamento**
   vicino alla culatta
3. **Ottica grande su anelli in acciaio bassi**, con **torretta di elevazione alta** e **torretta di
   deriva** laterale, entrambe **zigrinate**. La campana anteriore è **Ø 50 mm**
4. **Calcio in polimero pieno** con **guanciale rialzato integrato** e **calciolo in gomma nera** con
   **nervature orizzontali**. **Nessun buco, nessun traliccio:** è pieno
5. **Impugnatura a pistola in polimero** con **rigature a griglia** e **appoggia-pollice rialzato**
6. **Leva dell'otturatore** lunga e diritta con **pomello sferico grosso**, **piegata leggermente
   verso l'esterno** per fare spazio all'ottica
7. **Caricatore a scatola da 5**, corto e tozzo, **in acciaio**, che **sporge appena** e ha una
   **linguetta di estrazione piegata** ben visibile

**Usura:** **la canna mostra tutto**: brunitura consumata **in modo esteso sui bordi superiori** e
**macchie di ossidazione** `#5C4632` lungo la nervatura (è un'arma che sta ferma a lungo in ambienti
umidi). **Freno di bocca quasi completamente annerito**, con **la vernice mancante nelle camere**.
Polimero del calcio **graffiato sugli spigoli**, con **una scheggiatura sul calciolo in gomma**.
Ottica con **la vernice mancante sui bordi degli anelli** e **una lente leggermente rigata**.
**Nastro adesivo telato** avvolto **due volte** attorno al collo del calcio, su una **crepa**.

**Marchi:** **`TIRRENO PRECISION`** inciso sul lato sinistro della culatta, con **`LONGSHOT .338`**
sotto, in carattere **tecnico e stretto**. Sull'ottica: **`TP 10×42`**. Sulla canna, verso il freno:
**`.338 LAPUA — 5 COLPI`** e un **timbro di prova con la data `9/79`**. Sul calcio in polimero,
**inciso in profondità a bulino**: **`1/5`** (il primo di cinque) — l'unico decoro concesso, ed è un
numero, non un ornamento.

**Nota per il render:** il Longshot deve **riempire il frame orizzontalmente** e il modellatore deve
farlo sembrare **scomodo da portare**. Nella vista 3/4 posteriore deve emergere che **il calcio è
pieno e alto**, perché è la parte che il giocatore vedrà per cento ore.

---
---

# IL COLTELLO

## 16 · COMBAT KNIFE · *nessun fabbricante — dotazione*

**Silhouette a 30 m:** **una lama corta e dritta con un'impugnatura tozza.** Niente di
straordinario: **è esattamente quello che deve sembrare: un attrezzo.**

| | |
|---|---|
| **Dimensioni** | **300 mm totali** · lama **170 mm**, spessore **5 mm** |
| **Materiali** | Lama **acciaio annerito** `#2A2C2F`, rugosità 0,45 · Impugnatura **gomma nera** `#141416` · Guardia **acciaio** `#3A3D40` · Fodero **nailon** `#3A3A34` |
| **Peso visivo** | Piccolo, solido, **funzionale**. Nessun elemento decorativo |

**Cosa deve esserci:**
1. **Lama a punta** con **falsa controricerca** (nessun filo sul dorso) e **bisello piano**
2. **Annerimento a immersione** sulla lama, con **il filo che resta grigio chiaro**
   `#8A8D8F`: **l'unico punto chiaro del coltello**, ed è ciò che gli dà lettura
3. **Zigrinatura sulla costola** della lama, **12 righe trasversali** nella zona vicino all'impugnatura
4. **Guardia inferiore e superiore minima**, appena accennata, spessore 4 mm
5. **Impugnatura in gomma** con **anelli di presa** (5 anelli) e **un foro passante** in fondo per il
   cordino — **con il cordino in paracord verde oliva `#4A5238` che pende**
6. **Nessuna punta a pugnale, nessun seghetto, nessun apribottiglie**: **niente multitool**
7. **Fodero in nailon** con **due passanti per la cintura** e **una linguetta di ritenzione in velcro**,
   consumata

**Usura:** lama con **graffi paralleli e fini** su entrambe le facce (affilatura frequente), e **il filo
consumato irregolarmente**, con **due ammaccature** nella zona centrale. **Annerimento andato via sulla
punta** fino a metallo nudo `#8A8D8F`. Gomba dell'impugnatura **liscia e lucida al centro** per l'uso,
con **un taglio** corrispondente all'indice. **Il paracord sfrangiato in punta**, con **i fili
aperti**.

**Marchi:** **nessuna iscrizione sulla lama** — è un'attrezzatura, non un'arma da presentare.
Sull'impugnatura, **impresso a stampo** un **quadrato con un numero**: `04`. Sul fodero, un
**pennarello indelebile nero**, scritto a mano: **`M. RINALDI`** — e sotto, **cancellato con un
graffio**: un altro nome.

**Nota per il render:** il coltello va reso **con il cordino e il fodero**, non da solo. Il paracord
che pende e il fodero consumato sono ciò che dice al giocatore **"questo lo porti addosso da mesi"**.
Un coltello pulito, senza cordino, è un coltello generico: e il generico è l'unica cosa che questo
arsenale non può permettersi.

---

## Riepilogo: il foglio collettivo delle silhouette

Da produrre **prima** dei modelli definitivi. **16 silhouette in nero pieno, sfondo bianco, stessa
scala, in fila.**

| # | Arma | Il tratto che deve sopravvivere alla sfocatura |
|---|---|---|
| 1 | Standard | La sagoma più vuota. Il riferimento di paragone |
| 2 | Hush | Corto dietro, lungo davanti |
| 3 | Snub | Tutto tamburo, nessun carrello |
| 4 | Hammer | Troppo grosso, con la fessura in cima |
| 5 | Chatter | Il caricatore più lungo della canna |
| 6 | Cadence | Tubo dietro, banana in mezzo |
| 7 | Blitz | Un rettangolo, e basta |
| 8 | Scrap | Lamiera piegata, si vede attraverso |
| 9 | Choke | Una linea lunga e sottile con l'asta di legno |
| 10 | Sweeper | Blocco nero con scatola che sporge |
| 11 | Anvil | Legno + caricatore curvo + canna scoperta |
| 12 | Service | Linea superiore piatta, caricatore **diritto** |
| 13 | Vantage | Il più grosso, con l'ottica lunga |
| 14 | Spotter | **Il buco nel calcio** |
| 15 | Longshot | La linea più lunga del gioco |
| 16 | Knife | Piccolo, tozzo, senza pretese |

> **Se due righe di questa tabella descrivono la stessa forma, il problema è a monte:**
> sono io che ho progettato due armi troppo simili, non il modellatore che le ha fatte uguali.
> In quel caso torna da me e cambiamo il design, non l'arte.
