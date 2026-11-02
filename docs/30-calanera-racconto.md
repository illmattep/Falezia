# 30 · Calanera raccontata — area per area, edificio per edificio

> Questo documento descrive **cosa si vede** in ogni punto della mappa. Le misure, le quote, i ripari
> e i lotti degli edifici vengono **tutti dal blockout** (`calanera.json`, generato da
> `tools/mapgen/calanera.py`): qui non si sposta niente, si veste. Le tabelle degli edifici sono
> generate dallo stesso blockout (`tools/calanera2/racconto.py`), quindi restano vere finché il
> blockout non cambia.
>
> È il copione della produzione (doc 29, fasi F2–F6): si costruisce **un'area alla volta**, a partire
> dagli spawn, e ogni area si confronta con questo testo.

---

## Come leggerlo

- **Coordinate**: metri. **X** = est, **Z** = sud (−Z è nord), **Y** = su. Il centro della cassa è (0, 0).
  "x 7…13 · z −70…−65" è un rettangolo in pianta.
- **Edifici**: ogni lotto del blockout ha un codice (es. **PV-03** = Porto Vecchio, terzo da ovest).
  Le colonne dicono: lotto, pianta, piani (2 = 6,2 m · 3 = 9,0 m · 4 = 11,8 m), quali **fronti** danno su
  quale area e quanto sono lunghi, e com'è l'edificio.
- **Metà Sud**: è la metà Nord ruotata di 180° (D2): stessa architettura, **pelle diversa**. Per ogni area
  sud si dice da quale area nord nasce e cosa cambia.
- **Ogni area** ha: pianta e misure · ripari e quote (dal blockout) · carattere e landmark · suolo ·
  edifici · arredo.

## Regole che valgono ovunque (vincoli di produzione)

1. **Ciò che si tocca è il blockout.** Ogni oggetto contro cui si può sbattere o dietro cui ci si
   ripara **riempie esattamente** una scatola del blockout (un container è 6 × 2,4 × 2,6 m perché la
   scatola è così). Nient'altro di solido nelle zone calpestabili.
2. **Dettaglio libero** (non serve una scatola): decal a terra e sui muri, oggetti **sopra i 2,3 m**
   (insegne, lanterne, tende, panni, cavi), oggetti **dentro** i volumi esistenti, rilievi di facciata
   **≤ 4 cm sotto i 3 m** (sopra i 3 m cornicioni e balconi possono sporgere).
3. **Le finestre sono incassate e cieche** (vetro scuro, persiane): nessun foro passante. Si vede solo
   dove si spara.
4. **D4**: il colore saturo è solo informazione di gioco — ciano Nord, arancio Sud, giallo obiettivo,
   rosso pericolo. Intonaci, persiane e tende sono **desaturati**.
5. **Stile Valorant / Ascent**: forme pulite e leggibili, materiali dipinti ma ricchi, fusioni morbide
   (intonaco che si stacca sul mattone, piede dei muri sporco), luce calda da ovest.

## Il paese in breve

Calanera è un borgo di mare su una penisola: a **nord il Porto Vecchio** (lavoro, cemento, ferro, la
gru), a **sud il Forte San Verano** (pietra, ocra, legno, stendardi). Al centro la **Piazza Vecchia**
con la banca, i portici, due fontane e il **campanile** che si vede da tutta la mappa. A est la
**Grotta del Levante** si apre nella scogliera; a ovest la **Galleria Vecchia** è una strada in
galleria dismessa.

| | Metà Nord (Porto) | Metà Sud (Forte) |
|---|---|---|
| Intonaci | crema, bianco calce, grigio perla, sabbia | ocra, terra di Siena, salmone spento, sabbia calda |
| Persiane | verde bottiglia, ardesia, salvia | verde oliva, castagna, rosso ossido scuro |
| Materiali "duri" | cemento dipinto, lamiera, acciaio | pietra calcarea, legno, ferro battuto |
| Arredo tipico | reti, cassette, bitte, container | botti, stendardi, bracieri, carri |
| Landmark | la **gru** (30 m) sul molo | la **torre del Forte** (24 m) sul bastione |

**Tre landmark si vedono da ovunque**: la gru (nord), la torre del Forte (sud), il campanile (centro).
Il sole viene da ovest: nessuna squadra gioca controluce.

---

# METÀ NORD

## 1 · Porto Vecchio — spawn Nord

**Pianta.** Piazzale di banchina **40 × 17 m** (x −20…20 · z −99…−82), tutto a quota 0. Uscite:
a **sud** la Salita del Porto (8 m, x −4…4); a **ovest** la Via Stretta (4 m, z −93…−89); a **est** un
varco di 4 m (x 20…24 · z −95…−87) verso la Banchina.

**Ripari (blockout).**
- **Due container** 6 × 2,4 × 2,6 m lungo il bordo nord: x −16…−10 e x 10…16, z −97…−94,6.
  Uno **blu polvere** con le scritte della compagnia, uno **rosso ruggine spento**; porte chiuse verso
  il mare, il lato verso lo spawn è liscio (si legge come riparo).
- **Due pile di casse** da pesce 2,5 × 2,5 × 1,0 m: x −12…−9,5 e x 9,5…12, z −86,5…−84 — legno
  chiaro, reggette di ferro, una rete buttata sopra.
- **Cinque piazzole di spawn** 1,2 × 1,2 m (x −8, −4, 0, 4, 8 · z −91): lastre di cemento con il
  bordo **ciano** dipinto, 2 cm. Sono l'unico ciano della zona.

**Carattere.** È il lato "luminoso" e industriale: luce piena, colori chiari, tutto aperto verso il
mare. Si sente il porto prima di vederlo: gabbiani, cime che sbattono, l'acqua sotto le banchine.
Dietro i magazzini a nord spunta la **gru a traliccio** (30 m, braccio a 28 m che passa sopra lo
spawn): è il landmark del Nord. Sopra il magazzino centrale c'è l'**insegna antica "FALEZIA"** in
lettere di ferro arrugginite (il nome del gioco, con la Z).

**Suolo.** Lastre di cemento portuale grandi (1,5 × 1 m) con giunti scuri, **binari incassati** a filo
che attraversano il piazzale da est a ovest (decal + normal, nessun rilievo), chiazze d'olio e
d'acqua, segnaletica gialla **spenta** (ocra, non gialla: D4) per le corsie dei carrelli. Verso il bordo
nord una fascia di **cordolo di pietra** con le **bitte** di ghisa dipinte a terra come decal (non
sporgono).

**Oltre il bordo (solo vista).** Il **molo** di cemento prosegue in mare per 12 m (x −6…6, z −112…−100),
con una lanterna verde in punta. Il mare è un piano animato a −2 m. La gru poggia sul molo.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PV-01** | x -25,5…-20 · z -89…-81,5 | 5,5 × 7,5 m | 3 (9 m) | nord su Via Stretta 5,5 m; est su Porto Vecchio 7 m | intonaco sabbia chiara; piano terra: fabbrica del ghiaccio; 2 finestre per piano sul fronte est con persiane verde bottiglia; cassette di polistirolo impilate sui davanzali; reti appese ad asciugare a 3 m |
| **PV-02** | x -20…-10 · z -100…-99 | 10 × 1 m | 2 (6,2 m) | sud su Porto Vecchio 10 m | **facciata di magazzino sul mare** alta 6,2 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera verde bottiglia scuro, finestre alte con inferriate |
| **PV-03** | x -20…-12 · z -82…-76 | 8 × 6 m | 4 (11,8 m) | nord su Porto Vecchio 8 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: bar del porto (tavolini sotto la tenda); 2 finestre per piano sul fronte nord con persiane verde bottiglia; reti appese ad asciugare a 3 m; cavi elettrici fra le finestre |
| **PV-04** | x -12…-4 · z -82…-76 | 8 × 6 m | 2 (6,2 m) | nord su Porto Vecchio 8 m; est su Salita del Porto 6 m | intonaco avorio; piano terra: magazzino del pesce; 2 finestre per piano sul fronte nord con persiane grigio piombo; numeri civici su piastrelle; reti appese ad asciugare a 3 m |
| **PV-05** | x -10…0 · z -100…-99 | 10 × 1 m | 4 (11,8 m) | sud su Porto Vecchio 10 m | **facciata di magazzino sul mare** alta 11,8 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera ruggine spenta, finestre alte con inferriate; sopra il cornicione la prima metà dell'**insegna antica FALEZIA**, lettere di ferro arrugginite alte 1,2 m |
| **PV-06** | x 0…10 · z -100…-99 | 10 × 1 m | 4 (11,8 m) | sud su Porto Vecchio 10 m | **facciata di magazzino sul mare** alta 11,8 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera grigio piombo, finestre alte con inferriate; sopra il cornicione la seconda metà dell'**insegna FALEZIA** |
| **PV-07** | x 4…12 · z -82…-76 | 8 × 6 m | 2 (6,2 m) | nord su Porto Vecchio 8 m; ovest su Salita del Porto 6 m | intonaco crema; piano terra: officina nautica con saracinesca; 2 finestre per piano sul fronte nord con persiane verde bosco; cassette di polistirolo impilate sui davanzali; insegne dipinte a mano scolorite |
| **PV-08** | x 10…20 · z -100…-99 | 10 × 1 m | 2 (6,2 m) | sud su Porto Vecchio 10 m | **facciata di magazzino sul mare** alta 6,2 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera grigio piombo, finestre alte con inferriate |
| **PV-09** | x 12…20 · z -82…-76 | 8 × 6 m | 4 (11,8 m) | nord su Porto Vecchio 8 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: bar del porto (tavolini sotto la tenda); 2 finestre per piano sul fronte nord con persiane legno naturale schiarito; reti appese ad asciugare a 3 m |
| **PV-10** | x 20…24 · z -87…-79,3 | 4 × 7,7 m | 4 (11,8 m) | nord su Porto Vecchio (uscita est) 4 m; est su Banchina 3 m; ovest su Porto Vecchio 5 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: Capitaneria di Porto (orologio fermo alle 7:10, salvagente appeso a muro, bandiera); 1 finestra per piano sul fronte ovest con persiane verde bottiglia; cassette di polistirolo impilate sui davanzali; cavi elettrici fra le finestre; balcone in ferro al primo piano (sopra i 3,6 m) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PVE-01** | x 20…24 · z -99…-95 | 4 × 4 m | 4 (11,8 m) | sud su Porto Vecchio (uscita est) 4 m; est su Banchina 4 m; ovest su Porto Vecchio 4 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: magazzino doganale; 1 finestra per piano sul fronte sud con persiane legno naturale schiarito; segnaletica portuale; scaletta in ferro sul fianco; balcone in ferro al primo piano (sopra i 3,6 m); alla sua base, lato mare, il pilone della **gru** (il traliccio sale oltre il bordo) |


**Arredo e dettagli.** Reti stese ad asciugare **sopra i 3 m** fra i magazzini, cassette di
polistirolo sui davanzali, un salvagente bianco e rosso (spento) appeso al muro della Capitaneria,
lampioni portuali a braccio, cavi fra le facciate, un orologio fermo alle 7:10 sulla Capitaneria,
pozzanghere decal vicino ai container.

## 2 · Salita del Porto

**Pianta.** Strada di **8 × 12 m** (x −4…4 · z −82…−70) che collega lo spawn al Largo. È **in piano**
(il nome è storico): nessun gradino, nessun riparo. Gli spigoli delle case ai due lati (**PV-04**,
**PV-07** e i loro vicini del Largo) fanno da quinte.

**Carattere.** Una via di passaggio stretta fra case basse a due piani: è la prima "porta" verso il
paese. **Suolo**: sampietrini, con una canaletta di scolo in pietra al centro (decal).
**Arredo**: due lampioni a muro a 3,2 m, una targa "SALITA DEL PORTO" in marmo, un'edicola votiva.

_Nessun edificio ha qui il suo fronte principale: le facciate che si vedono sono elencate nelle aree vicine._


## 3 · Largo

**Pianta.** Piazzetta di **26 × 18 m** (x −13…13 · z −70…−52). Quattro uscite: nord la Salita, sud
l'**Arco** (6 m), ovest ed est i due **passaggi coperti** (5 m) verso le corsie laterali.

**Ripari e quote (blockout).**
- **Fontana**: vasca di pietra 4 × 4 × 0,8 m al centro (x −2…2 · z −63…−59), con una **colonna** di
  0,8 × 0,8 m alta fino a 1,9 m e un mascherone di bronzo che butta acqua. Spezza la vista spawn → arco.
- **Casse** di legno 2,5 × 2,5 × 1,0 m nell'angolo sud-ovest (x −11…−8,5 · z −57…−54,5): il deposito
  del bar.
- **La gradinata**: un **palco di pietra a 1,53 m** nell'angolo nord-est (x 7…13 · z −70…−65), con
  **nove gradini** da 17 cm larghi 6 m (z −65…−62,3) che scendono verso sud. Dal palco si vede l'arco,
  non lo spawn. Il palco ha un parapetto dipinto sul fronte (decal) ma nessun ostacolo in più.

**Carattere.** Il salotto del Nord: un bar con i tavolini, la tabaccheria, la farmacia, gerani alle
finestre. Il palco è il **sagrato di una piccola chiesa** (facciata sul lato nord, LA-xx): il fronte
della chiesa sta dietro il palco, con un portale chiuso e un rosone cieco.

**Suolo.** Lastre di pietra chiara a correre, un anello di sampietrini intorno alla fontana, la
gradinata in pietra consumata al centro dei gradini.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **LA-01** | x -20…-12 · z -76…-70 | 8 × 6 m | 4 (11,8 m) | sud su Largo 1 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: barbiere con l'insegna a spirale; 1 finestra per piano sul fronte sud con persiane grigio ardesia; persiane socchiuse; orologio pubblico; balcone in ferro al primo piano (sopra i 3,6 m) |
| **LA-02** | x -13…-3 · z -52…-43,5 | 10 × 8,5 m | 3 (9 m) | nord su Largo 10 m; est su Arco 8 m | intonaco grigio perla; piano terra: tabaccheria; 3 finestre per piano sul fronte nord con persiane verde bottiglia; orologio pubblico; vasi di gerani sui davanzali |
| **LA-03** | x -12…-4 · z -76…-70 | 8 × 6 m | 2 (6,2 m) | sud su Largo 8 m; est su Salita del Porto 6 m | intonaco bianco calce; piano terra: panetteria; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; persiane socchiuse; vasi di gerani sui davanzali |
| **LA-04** | x 3…13 · z -52…-43 | 10 × 9 m | 3 (9 m) | nord su Largo 10 m; ovest su Arco 8 m; ovest su Via del Mercato 1 m | intonaco rosa antico spento; piano terra: bar con tavolini e ombrelloni chiusi; 3 finestre per piano sul fronte nord con persiane verde bottiglia; balconcino in ferro battuto; persiane socchiuse; balcone in ferro al primo piano (sopra i 3,6 m) |
| **LA-05** | x 4…12 · z -76…-70 | 8 × 6 m | 2 (6,2 m) | sud su Largo 8 m; ovest su Salita del Porto 6 m | intonaco grigio perla; piano terra: latteria; 2 finestre per piano sul fronte sud con persiane verde bosco; persiane socchiuse; orologio pubblico |
| **LA-06** | x 12…20 · z -76…-70 | 8 × 6 m | 4 (11,8 m) | sud su Largo 1 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: farmacia con la croce spenta; 1 finestra per piano sul fronte sud con persiane legno naturale schiarito; vasi di gerani sui davanzali; altana con pergola sul tetto (skyline) |


**Arredo.** Ombrelloni chiusi **sopra i 2,3 m** (solo le tende a muro, niente pali a terra), un
orologio pubblico sul lato sud, piccioni (animazione a terra, senza collisione), manifesti del Comune
sui muri, foglie secche decal intorno alla fontana.

## 4 · Arco

**Pianta.** Strozzatura di **6 × 8 m** (x −3…3 · z −52…−44): i primi **6 m sono coperti** (z −52…−46)
da un corpo di fabbrica in **pietra** che scavalca la strada da 3,2 a 6,2 m. È la "porta" fra il Largo e
la Via del Mercato.

**Carattere.** Un arco ribassato in conci di pietra con la chiave scolpita (uno stemma con un'ancora),
volta intonacata e affrescata (sbiadita), **lanterna appesa** al centro. Sopra l'arco una stanza con
due finestre per lato e un orologio solare sul fronte nord. **Suolo**: lastre di pietra lisce, consumate
nel mezzo. I fianchi sono i muri dei due palazzi del Largo (LA-02 a ovest, LA-04 a est).

## 5 · Via del Mercato

**Pianta.** Strada **a gomito** larga 6 m: x −3…3 · z −44…−34, poi verso est x −3…9 · z −34…−28,
poi x 3…9 · z −28…−18 fino alla Piazza. Nessuna linea dritta dallo spawn alla piazza.

**Ripari (blockout).** Il **carretto** di legno all'angolo (x −2,5…−0,5 · z −33…−29, alto 1,5 m):
carretto a due ruote del fruttivendolo, con cassette di frutta sopra e le stanghe appoggiate a terra
(le stanghe stanno dentro la scatola).

**Carattere.** La via delle botteghe: vetrine al piano terra, tende a strisce **sopra i 2,3 m**,
cassette di frutta sui davanzali, lavagne dei prezzi. Palazzi alti (3–4 piani) e stretti: è in ombra
per gran parte del giorno. **Suolo**: sampietrini con le guide di pietra per i carri.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **VM-01** | x -13…-3 · z -43,5…-35 | 10 × 8,5 m | 4 (11,8 m) | est su Via del Mercato 8,5 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: macelleria; 2 finestre per piano sul fronte est con persiane verde bottiglia; panni stesi fra le finestre; cassette di frutta sui davanzali |
| **VM-02** | x -13…-3 · z -35…-26,5 | 10 × 8,5 m | 4 (11,8 m) | est su Via del Mercato 1 m; est su Via del Mercato 6 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: merceria; 1 finestra per piano sul fronte est con persiane verde bottiglia; panni stesi fra le finestre; cassette di frutta sui davanzali |
| **VM-03** | x -3…3 · z -28…-18 | 6 × 10 m | 4 (11,8 m) | nord su Via del Mercato 6 m; sud su Piazza Vecchia 6 m; est su Via del Mercato 10 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: frutta e verdura; 3 finestre per piano sul fronte est con persiane verde bosco; panni stesi fra le finestre; cartelli dei prezzi in gesso; altana con pergola sul tetto (skyline) |
| **VM-04** | x 3…13 · z -43…-34 | 10 × 9 m | 3 (9 m) | sud su Via del Mercato 6 m; ovest su Via del Mercato 9 m | intonaco rosa antico spento; piano terra: pasticceria; 2 finestre per piano sul fronte ovest con persiane grigio piombo; cassette di frutta sui davanzali; cartelli dei prezzi in gesso; balcone in ferro al primo piano (sopra i 3,6 m) |
| **VM-05** | x 9…13 · z -34…-26 | 4 × 8 m | 4 (11,8 m) | ovest su Via del Mercato 6 m; ovest su Via del Mercato 2 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: cantina con le botti in vetrina; 1 finestra per piano sul fronte ovest con persiane grigio ardesia; panni stesi fra le finestre; tende a strisce sopra le vetrine (sopra i 2,3 m); altana con pergola sul tetto (skyline) |
| **VM-06** | x 9…13 · z -26…-18 | 4 × 8 m | 2 (6,2 m) | sud su Piazza Vecchia 4 m; ovest su Via del Mercato 8 m | intonaco rosa antico spento; piano terra: drogheria; 2 finestre per piano sul fronte ovest con persiane grigio piombo; cartelli dei prezzi in gesso; tende a strisce sopra le vetrine (sopra i 2,3 m) |


## 6 · I passaggi coperti del Largo

**Ovest**: x −34…−13 · z −64…−59 (21 × 5 m), coperto per 10 m (x −26…−16) da un corpo in **intonaco**
a 3,2–6,2 m. Collega il Largo alla Piazzetta del Forno.
**Est**: x 13…30 · z −64…−59 (17 × 5 m), coperto per 10 m (x 16…26). Collega il Largo al Cortile della
Dogana.

**Carattere.** Due **sottoportici**: volta a crociera intonacata, lanterne a muro, un tratto di pavimento
in mattoni a spina di pesce sotto la copertura, i fianchi in pietra fino a 1,2 m. Sono le rotazioni di
chi attacca: devono leggersi come "scorciatoie", più buie della strada.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PL-01** | x -30…-20 · z -74…-64 | 10 × 10 m | 3 (9 m) | sud su Passaggio Ovest del Largo 10 m; ovest su Piazzetta del Forno 10 m | intonaco grigio tortora; piano terra: androne; 3 finestre per piano sul fronte sud con persiane legno naturale schiarito; affreschi sbiaditi nell'intradosso |
| **PL-02** | x -30…-21,5 · z -59…-50,8 | 8,5 × 8,2 m | 4 (11,8 m) | nord su Passaggio Ovest del Largo 8,5 m; ovest su Piazzetta del Forno 3 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: deposito; 2 finestre per piano sul fronte nord con persiane verde bosco; lampada a muro sotto il sottoportico; affreschi sbiaditi nell'intradosso |
| **PL-03** | x -21,5…-13 · z -59…-50,8 | 8,5 × 8,2 m | 2 (6,2 m) | nord su Passaggio Ovest del Largo 8,5 m; est su Largo 7 m | intonaco grigio tortora; piano terra: androne; 2 finestre per piano sul fronte nord con persiane verde bottiglia; lampada a muro sotto il sottoportico |
| **PL-04** | x -20…-13 · z -70…-64 | 7 × 6 m | 4 (11,8 m) | sud su Passaggio Ovest del Largo 7 m; est su Largo 6 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: deposito; 2 finestre per piano sul fronte sud con persiane grigio ardesia; lampada a muro sotto il sottoportico; altana con pergola sul tetto (skyline) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PL-01** | x 13…20 · z -70…-64 | 7 × 6 m | 4 (11,8 m) | sud su Passaggio Est del Largo 7 m; ovest su Largo 6 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: deposito; 2 finestre per piano sul fronte sud con persiane grigio piombo; graffiti vecchi; lampada a muro sotto il sottoportico; altana con pergola sul tetto (skyline) |
| **PL-02** | x 13…20,5 · z -59…-50,8 | 7,5 × 8,2 m | 4 (11,8 m) | nord su Passaggio Est del Largo 7,5 m; ovest su Largo 7 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: androne; 2 finestre per piano sul fronte nord con persiane verde bottiglia; lampada a muro sotto il sottoportico; graffiti vecchi; altana con pergola sul tetto (skyline) |
| **PL-03** | x 20…24 · z -71,7…-64 | 4 × 7,7 m | 2 (6,2 m) | sud su Passaggio Est del Largo 4 m | intonaco rosa antico spento; piano terra: deposito; 1 finestra per piano sul fronte sud con persiane verde bottiglia; graffiti vecchi |
| **PL-04** | x 20,5…28 · z -59…-50,8 | 7,5 × 8,2 m | 3 (9 m) | nord su Passaggio Est del Largo 7,5 m; est su Cortile della Dogana 7 m | intonaco grigio tortora; piano terra: androne; 2 finestre per piano sul fronte nord con persiane verde bosco; lampada a muro sotto il sottoportico; balcone in ferro al primo piano (sopra i 3,6 m) |


## 7 · Scalinata Lunga — corsia est verso la Grotta

### 7a · Banchina

**Pianta.** **22 × 15 m** (x 24…46 · z −99…−84): la banchina commerciale, collegata allo spawn dal varco
di 4 m.
**Ripari (blockout).** Container **6 × 2,4 × 2,6** (x 30…36 · z −97…−94,6) e container **di traverso**
2,4 × 6 × 2,6 (x 40…42,4 · z −94…−88); cassa 2,5 × 2,5 × 1 (x 27…29,5 · z −88…−85,5).
**Carattere.** Magazzini della compagnia di navigazione, portoni scorrevoli con i numeri a stencil,
fusti di carburante, una gru gommata ferma (solo nello skyline oltre il bordo). **Suolo**: cemento con
binari e bitte dipinte.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **BA-01** | x 20…30 · z -100…-99 | 10 × 1 m | 3 (9 m) | sud su Banchina 6 m | **facciata di magazzino sul mare** alta 9 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera ruggine spenta, finestre alte con inferriate |
| **BA-02** | x 24…30 · z -84…-74 | 6 × 10 m | 3 (9 m) | nord su Banchina 6 m; sud su Cortile della Dogana 2 m | intonaco sabbia chiara; piano terra: ufficio della compagnia di navigazione; 1 finestra per piano sul fronte nord con persiane verde bosco; lampioni portuali; scritte con stencil sui portoni |
| **BA-03** | x 30…40 · z -100…-99 | 10 × 1 m | 4 (11,8 m) | sud su Banchina 10 m | **facciata di magazzino sul mare** alta 11,8 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera verde bottiglia scuro, finestre alte con inferriate |
| **BA-04** | x 40…50 · z -100…-99 | 10 × 1 m | 2 (6,2 m) | sud su Banchina 6 m | **facciata di magazzino sul mare** alta 6,2 m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista); portoni scorrevoli in lamiera verde bottiglia scuro, finestre alte con inferriate |
| **BA-05** | x 46…54,8 · z -99…-90,7 | 8,8 × 8,3 m | 3 (9 m) | ovest su Banchina 8,3 m | intonaco bianco calce; piano terra: magazzino portuale; 2 finestre per piano sul fronte ovest con persiane grigio piombo; bitte di ghisa lungo il bordo; scritte con stencil sui portoni |
| **BA-06** | x 46…54,8 · z -90,7…-82,3 | 8,8 × 8,3 m | 3 (9 m) | ovest su Banchina 6,7 m | intonaco grigio perla; piano terra: deposito carburante (fusti); 2 finestre per piano sul fronte ovest con persiane verde bottiglia; bitte di ghisa lungo il bordo; lampioni portuali; balcone in ferro al primo piano (sopra i 3,6 m) |


### 7b · Varco della Dogana

**Pianta.** Strozzatura di **6 × 10 m** (x 36…42 · z −84…−74), **coperta per 6 m** (z −82…−76) da un
corpo in **cemento** a 3,4–6,2 m: la **casa doganale** che scavalca la strada.
**Carattere.** Sbarra a bilico alzata, garitta dipinta (dentro il lotto VD), cartello "DOGANA — ALT"
sbiadito, faretti sotto il solaio. Il soffitto basso e piatto fa sentire la strozzatura.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **VD-01** | x 30…36 · z -84…-74 | 6 × 10 m | 3 (9 m) | nord su Banchina 6 m; sud su Cortile della Dogana 6 m; est su Varco della Dogana 10 m | intonaco sabbia chiara; piano terra: corpo di guardia della Dogana; 3 finestre per piano sul fronte est con persiane salvia; garitta; sbarra a bilico alzata |
| **VD-02** | x 42…46 · z -84…-74 | 4 × 10 m | 2 (6,2 m) | nord su Banchina 4 m; sud su Cortile della Dogana 4 m; ovest su Varco della Dogana 10 m | intonaco bianco calce; piano terra: ufficio pratiche; 3 finestre per piano sul fronte ovest con persiane grigio piombo; sbarra a bilico alzata |


### 7c · Cortile della Dogana

**Pianta.** Cortile di **26 × 22 m** (x 28…54 · z −74…−52). Entrate: nord il Varco, ovest il passaggio
dal Largo, sud la Discesa al Molo (x 46…52).
**Ripari (blockout).** Muretto 2,4 × 0,6 × 1,1 (x 33…35,4 · z −66…−65,4); **carro** del dazio
2 × 4 × 1,5 (x 44…46 · z −69…−65); cassa 2,5 × 2,5 × 1 (x 48…50,5 · z −58…−55,5); **pilastro** di
cemento 1,4 × 3 × 2,6 (x 30…31,4 · z −57…−54): il basamento di una **pesa pubblica** con la sua
garitta.
**Carattere.** Il cortile di un edificio pubblico: la **Dogana** con lo stemma sopra il portone, grate
al piano terra, l'orologio, una bandiera ammainata. È il punto chiamato "Scalinata Lunga" sulla mappa.
**Suolo**: lastre di pietra grigia in quadrati, con i segni dei parcheggi dei carri.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **CD-01** | x 24…28 · z -74…-64 | 4 × 10 m | 4 (11,8 m) | sud su Passaggio Est del Largo 4 m; est su Cortile della Dogana 10 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: uffici con le grate; 3 finestre per piano sul fronte est con persiane grigio ardesia; bandiera ammainata |
| **CD-02** | x 28…37 · z -52…-43,4 | 9 × 8,6 m | 3 (9 m) | nord su Cortile della Dogana 9 m | intonaco bianco calce; piano terra: magazzino sigillato; 2 finestre per piano sul fronte nord con persiane grigio piombo; stemma in pietra sopra il portone; grate alle finestre del piano terra |
| **CD-03** | x 37…46 · z -52…-43,4 | 9 × 8,6 m | 3 (9 m) | nord su Cortile della Dogana 9 m; est su Discesa al Molo 8,6 m; est su Molo 0,6 m | intonaco bianco calce; piano terra: Dogana (portone con lo stemma); 2 finestre per piano sul fronte nord con persiane legno naturale schiarito; bandiera ammainata; grate alle finestre del piano terra |
| **CD-04** | x 46…54,8 · z -82,3…-74 | 8,8 × 8,3 m | 2 (6,2 m) | sud su Cortile della Dogana 8 m | intonaco grigio perla; piano terra: pesa pubblica; 2 finestre per piano sul fronte sud con persiane grigio piombo; stemma in pietra sopra il portone |
| **CD-05** | x 54…63 · z -74…-65 | 9 × 9 m | 4 (11,8 m) | ovest su Cortile della Dogana 9 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: uffici con le grate; 2 finestre per piano sul fronte ovest con persiane salvia; orologio della Dogana; balcone in ferro al primo piano (sopra i 3,6 m) |
| **CD-06** | x 54…63 · z -65…-56 | 9 × 9 m | 4 (11,8 m) | sud su Terrazza del Molo 5 m; ovest su Cortile della Dogana 9 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: magazzino sigillato; 2 finestre per piano sul fronte ovest con persiane grigio piombo; stemma in pietra sopra il portone; bandiera ammainata |


### 7d · Discesa al Molo

**Pianta.** **6 × 12 m** (x 46…52 · z −52…−40), in piano, fra due palazzi alti. Un lampione, una
fontanella a muro (dentro il lotto), la targa "AL MOLO" con la freccia.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **DM-01** | x 52…54 · z -52…-44 | 2 × 8 m | 4 (11,8 m) | nord su Cortile della Dogana 2 m; sud su Molo 2 m; ovest su Discesa al Molo 8 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: torre delle scale; 2 finestre per piano sul fronte ovest con persiane verde bosco; corrimano a muro |


### 7e · Molo

**Pianta.** **36 × 14 m** (x 46…82 · z −44…−30). A sud, fra x 66 e 70, si apre l'**ingresso nord della
Grotta** (3,4 m, soffitto a 3 m).
**Ripari (blockout).** Cassa 2,5 × 2,5 × 1 (x 52…54,5 · z −36…−33,5); **barca in secca** su cavalletti
2 × 6 × 1,4 (x 62…64 · z −38…−32), un gozzo di legno con lo scafo bianco e la fascia verde spenta;
muretto 2,4 × 0,6 × 1,1 (x 76…78,4 · z −42…−41,4).
**Carattere.** Il posto dei pescatori: rimessaggio, osteria con la pergola, reti e boe appese, odore
di catrame. Il bordo sud è una fila di **case basse sul mare** (lotti profondi 2 m: facciate con il corpo
che prosegue sugli scogli, oltre il bordo).
**Suolo**: pietra di banchina scura e bagnata vicino all'ingresso della Grotta (fusione verso la roccia).

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **MO-01** | x 37…46 · z -43,4…-34,8 | 9 × 8,6 m | 2 (6,2 m) | est su Discesa al Molo 3,4 m; est su Molo 8,6 m | intonaco crema; piano terra: casa del pescatore; 2 finestre per piano sul fronte est con persiane verde bosco; reti e boe appese |
| **MO-02** | x 37…46 · z -34,8…-26,2 | 9 × 8,6 m | 2 (6,2 m) | est su Molo 4,8 m | intonaco sabbia chiara; piano terra: deposito attrezzi; 1 finestra per piano sul fronte est con persiane salvia; scafo rovesciato sulla tettoia; remi appoggiati sopra i 2,5 m |
| **MO-03** | x 46…56 · z -30…-28 | 10 × 2 m | 4 (11,8 m) | nord su Molo 10 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: cantiere navale; 3 finestre per piano sul fronte nord con persiane grigio piombo; reti e boe appese; remi appoggiati sopra i 2,5 m; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |
| **MO-04** | x 56…66 · z -30…-28 | 10 × 2 m | 2 (6,2 m) | nord su Molo 10 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco bianco calce; piano terra: rimessaggio barche; 3 finestre per piano sul fronte nord con persiane legno naturale schiarito; scafo rovesciato sulla tettoia; remi appoggiati sopra i 2,5 m |
| **MO-05** | x 70…76 · z -30…-28 | 6 × 2 m | 2 (6,2 m) | nord su Molo 6 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco grigio perla; piano terra: osteria del Molo; 1 finestra per piano sul fronte nord con persiane verde bosco; reti e boe appese; lanterne |
| **MO-06** | x 72…81 · z -50…-44 | 9 × 6 m | 2 (6,2 m) | sud su Molo 9 m; ovest su Terrazza del Molo 6 m | intonaco avorio; piano terra: casa del pescatore; 2 finestre per piano sul fronte sud con persiane salvia; lanterne; scafo rovesciato sulla tettoia |
| **MO-07** | x 76…82 · z -30…-28 | 6 × 2 m | 2 (6,2 m) | nord su Molo 6 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco sabbia chiara; piano terra: deposito attrezzi; 1 finestra per piano sul fronte nord con persiane verde bosco; lanterne; reti e boe appese |
| **MO-08** | x 81…90 · z -50…-44 | 9 × 6 m | 2 (6,2 m) | sud su Molo 1 m | intonaco avorio; piano terra: cantiere navale; 1 finestra per piano sul fronte sud con persiane verde bosco; reti e boe appese; remi appoggiati sopra i 2,5 m |
| **MO-09** | x 82…90 · z -44…-36 | 8 × 8 m | 2 (6,2 m) | ovest su Molo 8 m | intonaco crema; piano terra: rimessaggio barche; 2 finestre per piano sul fronte ovest con persiane grigio ardesia; reti e boe appese; scafo rovesciato sulla tettoia |
| **MO-10** | x 82…90 · z -36…-28 | 8 × 8 m | 3 (9 m) | ovest su Molo 6 m | intonaco camoscio; piano terra: osteria del Molo; 1 finestra per piano sul fronte ovest con persiane verde bosco; remi appoggiati sopra i 2,5 m; reti e boe appese; balcone in ferro al primo piano (sopra i 3,6 m) |


### 7f · Terrazza del Molo

**Pianta e quote (blockout).** Un **terrazzo di pietra a 3,4 m** (x 58…72 · z −56…−50), con la
**scala** di 20 gradini (x 66…70 · z −50…−44) che sale dal Molo verso nord, e il **parapetto** alto
1,05 m sul bordo sud (x 58…66 e x 70…72, z −50,3…−50; varco sulla scala). Davanti al terrazzo, a
terra, restano due rientranze (x 58…66 e x 70…72, z −50…−44).
**Carattere.** Il belvedere dei pescatori: panche in muratura **dentro** il parapetto, vasi di agavi
sul parapetto (sopra 1,05 m solo foglie, nessun volume), una pergola in ferro senza tralci. Domina
l'ingresso nord della Grotta: deve leggersi chiaramente come **posizione alta**.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **TM-01** | x 54…58 · z -56…-50 | 4 × 6 m | 3 (9 m) | est su Terrazza del Molo 6 m; ovest su Cortile della Dogana 4 m | intonaco sabbia chiara; piano terra: magazzino; 1 finestra per piano sul fronte est con persiane grigio piombo; pergola in ferro senza tralci; balcone in ferro al primo piano (sopra i 3,6 m) |
| **TM-02** | x 54…58 · z -50…-44 | 4 × 6 m | 3 (9 m) | sud su Molo 4 m; est su Terrazza del Molo 6 m | intonaco grigio perla; piano terra: casa a schiera; 1 finestra per piano sul fronte est con persiane grigio ardesia; pergola in ferro senza tralci; vasi sul parapetto |
| **TM-03** | x 63…72 · z -65…-56 | 9 × 9 m | 4 (11,8 m) | sud su Terrazza del Molo 9 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: magazzino; 2 finestre per piano sul fronte sud con persiane grigio ardesia; pergola in ferro senza tralci; altana con pergola sul tetto (skyline) |
| **TM-04** | x 72…81 · z -56…-50 | 9 × 6 m | 2 (6,2 m) | ovest su Terrazza del Molo 6 m | intonaco grigio perla; piano terra: casa a schiera; 1 finestra per piano sul fronte ovest con persiane legno naturale schiarito; pergola in ferro senza tralci |


## 8 · Via Stretta — corsia ovest verso la Galleria

**Pianta.** Vicolo largo **4 m**, a gomito: dallo spawn verso ovest (x −35…−20 · z −93…−89), poi verso
sud (x −35…−31 · z −93…−72) fino alla Piazzetta del Forno. Nessun riparo: i ripari sono gli spigoli.
**Carattere.** Il vicolo più stretto della mappa: case alte (3–4 piani) quasi a toccarsi, **panni stesi a
4,6 m**, edicola votiva con il lumino, scritte vecchie sui muri, vasi di basilico sui davanzali, contatori
dell'acqua. Luce che entra a lame. **Suolo**: lastre di pietra con la canaletta centrale, muschio ai piedi
dei muri.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **VS-01** | x -44,2…-35 · z -93…-83,5 | 9,2 × 9,5 m | 4 (11,8 m) | est su Via Stretta 4 m; est su Via Stretta 9,5 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: piccola cappella votiva; 3 finestre per piano sul fronte est con persiane grigio ardesia; edicola votiva con lumino; scritta di propaganda sbiadita; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |
| **VS-02** | x -44,2…-35 · z -83,5…-74 | 9,2 × 9,5 m | 3 (9 m) | sud su Piazzetta del Forno 9,2 m; est su Via Stretta 9,5 m | intonaco avorio; piano terra: ingresso di casa; 3 finestre per piano sul fronte est con persiane salvia; panni stesi a 4,6 m |
| **VS-03** | x -40…-30 · z -99…-93 | 10 × 6 m | 4 (11,8 m) | sud su Via Stretta 5 m; sud su Via Stretta 4 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: ciabattino; 1 finestra per piano sul fronte sud con persiane grigio piombo; edicola votiva con lumino; contatori a muro |
| **VS-04** | x -31…-25,5 · z -89…-81,5 | 5,5 × 7,5 m | 3 (9 m) | nord su Via Stretta 5,5 m; ovest su Via Stretta 7,5 m | intonaco sabbia chiara; piano terra: portone di androne; 2 finestre per piano sul fronte ovest con persiane grigio piombo; vasi di basilico |
| **VS-05** | x -31…-25,5 · z -81,5…-74 | 5,5 × 7,5 m | 4 (11,8 m) | sud su Piazzetta del Forno 1 m; ovest su Via Stretta 7,5 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: cantina; 2 finestre per piano sul fronte ovest con persiane salvia; scritta di propaganda sbiadita; vasi di basilico |
| **VS-06** | x -30…-20 · z -99…-93 | 10 × 6 m | 3 (9 m) | sud su Via Stretta 10 m; est su Porto Vecchio 6 m | intonaco grigio perla; piano terra: piccola cappella votiva; 3 finestre per piano sul fronte sud con persiane legno naturale schiarito; contatori a muro; vasi di basilico |


## 9 · Piazzetta del Forno

**Pianta.** **28 × 18 m** (x −58…−30 · z −74…−56). Entrate: la Via Stretta da nord-est, il passaggio
coperto dal Largo da est, il Passaggio del Piazzale a sud-ovest (x −62…−54).
**Il forno (blockout).** Una **stanza attraversabile** di 10 × 8 m (x −50…−40 · z −70…−62), muri da
40 cm alti 4 m, **due porte** da 1,6 × 2,05 m al centro dei lati est e ovest (z −66,8…−65,2), tetto a
4–4,4 m. Dentro, il **forno a legna** in muratura 2 × 2 × 1,1 (x −46…−44 · z −69,4…−67,4). Fuori:
muretto 1,4 × 2,4 × 1,1 (x −56…−54,6 · z −61…−58,6) e cassa 2,5 × 2,5 × 1 (x −36…−33,5 · z −60…−57,5).
**Carattere.** Il forno è l'edificio più caldo della mappa: intonaco giallo paglia **spento**, insegna
"PANE" in ferro, sacchi di farina dipinti, il camino che fuma. Dentro: la bocca del forno illuminata
(luce arancione **bassa**, non saturata), pale di legno a muro, un bancone sotto le finestre (dentro i
muri). **Suolo**: sampietrini, farina e cenere come decal davanti alle porte.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PF-01** | x -66…-58 · z -74…-65 | 8 × 9 m | 4 (11,8 m) | est su Piazzetta del Forno 9 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: latteria; 2 finestre per piano sul fronte est con persiane verde bosco; insegna PANE in ferro; sacchi di farina dipinti sui muri; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PF-02** | x -66…-58 · z -65…-56 | 8 × 9 m | 4 (11,8 m) | sud su Passaggio del Piazzale 4 m; est su Piazzetta del Forno 9 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: falegnameria; 2 finestre per piano sul fronte est con persiane verde bottiglia; camino fumante del forno; fascine di legna sopra i 2,5 m |
| **PF-03** | x -62,5…-53,3 · z -83,5…-74 | 9,2 × 9,5 m | 3 (9 m) | sud su Piazzetta del Forno 4,7 m | intonaco camoscio; piano terra: lavatoio coperto; 1 finestra per piano sul fronte sud con persiane verde bottiglia; sacchi di farina dipinti sui muri |
| **PF-04** | x -53,3…-44,2 · z -83,5…-74 | 9,2 × 9,5 m | 2 (6,2 m) | sud su Piazzetta del Forno 9,2 m | intonaco grigio perla; piano terra: deposito della farina; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; insegna PANE in ferro; camino fumante del forno |
| **PF-05** | x -46…-38 · z -56…-46,6 | 8 × 9,4 m | 2 (6,2 m) | nord su Piazzetta del Forno 8 m | intonaco camoscio; piano terra: forno (la stanza attraversabile); 2 finestre per piano sul fronte nord con persiane grigio piombo; camino fumante del forno; fascine di legna sopra i 2,5 m |
| **PF-06** | x -38…-30 · z -56…-46,6 | 8 × 9,4 m | 3 (9 m) | nord su Piazzetta del Forno 8 m | intonaco bianco calce; piano terra: latteria; 2 finestre per piano sul fronte nord con persiane grigio piombo; insegna PANE in ferro; camino fumante del forno |


## 10 · Passaggio del Piazzale e Piazzale della Galleria

**Passaggio**: x −62…−54 · z −56…−44 (8 × 12 m), in piano, fra officine.
**Piazzale**: **26 × 14 m** (x −82…−56 · z −44…−30). A sud si apre la **bocca nord della Galleria**
(12 m, x −78…−66).
**Ripari (blockout).** **Camion** fermo 6 × 2,4 × 3 (x −68…−62 · z −41…−38,6): un autocarro anni '60
con il telone verde spento; **new jersey** di cemento 2 × 0,6 × 0,8 (x −79…−77 · z −36…−35,4); muretto
1,4 × 2,4 × 1,1 (x −60…−58,6 · z −36…−33,6).
**Carattere.** L'ingresso di una strada statale dimenticata: casello, officina, distributore dismesso,
cartello "GALLERIA VECCHIA" con la sagoma del tunnel, segnaletica arrugginita. **Suolo**: **asfalto**
vecchio crepato con le strisce sbiadite, che si fonde nel lastricato verso il paese.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PP-01** | x -71,3…-62 · z -56…-50 | 9,3 × 6 m | 2 (6,2 m) | est su Passaggio del Piazzale 6 m | intonaco sabbia chiara; piano terra: deposito; 1 finestra per piano sul fronte est con persiane legno naturale schiarito; pneumatici appesi; cartelli stradali |
| **PP-02** | x -54…-46 · z -56…-46,6 | 8 × 9,4 m | 4 (11,8 m) | nord su Piazzetta del Forno 8 m; ovest su Passaggio del Piazzale 9,4 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: officina; 3 finestre per piano sul fronte ovest con persiane verde bottiglia; pneumatici appesi |
| **PP-03** | x -54…-46 · z -46,6…-37,2 | 8 × 9,4 m | 2 (6,2 m) | ovest su Passaggio del Piazzale 2,6 m | intonaco rosa antico spento; piano terra: deposito; 1 finestra per piano sul fronte ovest con persiane verde bosco; pneumatici appesi |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PG-01** | x -90…-80,7 · z -50…-44 | 9,3 × 6 m | 4 (11,8 m) | sud su Piazzale della Galleria 1,3 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: distributore di benzina dismesso; 1 finestra per piano sul fronte sud con persiane grigio ardesia; segnaletica stradale arrugginita |
| **PG-02** | x -90…-82 · z -44…-37 | 8 × 7 m | 2 (6,2 m) | est su Piazzale della Galleria 7 m | intonaco rosa antico spento; piano terra: trattoria per camionisti; 2 finestre per piano sul fronte est con persiane legno naturale schiarito; fari stradali; pompa di benzina anni '60 (dentro il lotto) |
| **PG-03** | x -90…-82 · z -37…-30 | 8 × 7 m | 4 (11,8 m) | est su Piazzale della Galleria 7 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: deposito dei cantonieri; 2 finestre per piano sul fronte est con persiane verde bottiglia; cartello GALLERIA VECCHIA con la sagoma del tunnel; altana con pergola sul tetto (skyline) |
| **PG-04** | x -80,7…-71,3 · z -50…-44 | 9,3 × 6 m | 2 (6,2 m) | sud su Piazzale della Galleria 9,3 m | intonaco bianco calce; piano terra: officina meccanica; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; pompa di benzina anni '60 (dentro il lotto) |
| **PG-05** | x -71,3…-62 · z -50…-44 | 9,3 × 6 m | 2 (6,2 m) | sud su Piazzale della Galleria 9,3 m; est su Passaggio del Piazzale 6 m | intonaco bianco calce; piano terra: casello dell'ANAS; 2 finestre per piano sul fronte sud con persiane salvia; segnaletica stradale arrugginita; cartello GALLERIA VECCHIA con la sagoma del tunnel |
| **PG-06** | x -62…-56 · z -30…-20,7 | 6 × 9,3 m | 3 (9 m) | nord su Piazzale della Galleria 6 m | intonaco avorio; piano terra: distributore di benzina dismesso; 1 finestra per piano sul fronte nord con persiane verde bosco; fari stradali; cartello GALLERIA VECCHIA con la sagoma del tunnel; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PG-07** | x -56…-54 · z -44…-35,6 | 2 × 8,4 m | 4 (11,8 m) | nord su Passaggio del Piazzale 2 m; ovest su Piazzale della Galleria 8,4 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: trattoria per camionisti; 2 finestre per piano sul fronte ovest con persiane legno naturale schiarito; cartello GALLERIA VECCHIA con la sagoma del tunnel; segnaletica stradale arrugginita; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |
| **PG-08** | x -56…-54 · z -35,6…-27,2 | 2 × 8,4 m | 3 (9 m) | ovest su Piazzale della Galleria 5,6 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco crema; piano terra: deposito dei cantonieri; 1 finestra per piano sul fronte ovest con persiane legno naturale schiarito; fari stradali; segnaletica stradale arrugginita |


---

# CENTRO

## 11 · Piazza Vecchia — la cassa

**Pianta.** **40 × 36 m** (x −20…20 · z −18…18). Ingressi: dalla Via del Mercato (nord, x 3…9) e dalla
sua ruotata (sud, x −9…−3); dai due Mercati coperti (est e ovest, 14 m).

**La banca (blockout).** **Banca di Calanera**: 12 × 8 m (x −6…6 · z −4…4), muri di pietra da 40 cm
alti 4,6 m, tetto a 4,6–5,0 m, **quattro porte** da 1,6 × 2,05 m al centro di ogni lato. Dentro: due
**banconi** di legno 0,6 × 4 × 1,1 (x ±2,8…±3,4 · z −2…2) e al centro **la cassa** (1,2 × 0,8 × 1,0,
gialla: è l'obiettivo). Fuori: bugnato di pietra al piano terra, colonnine ai lati delle porte, l'insegna
in lettere di bronzo, le grate. Dentro: pavimento a scacchi bianco e grigio, la cassaforte a muro
(decorazione) dietro la cassa, lampadari, vetri smerigliati sopra i banconi (sopra 1,1 m).

**Ripari (blockout).**
- **Due fontane** 3 × 3 × 0,8 (x −1,5…1,5 · z −12,5…−9,5 e z 9,5…12,5): vasche ottagonali basse.
- **Quattro fioriere** 2 × 2 × 1 (x ±10…±12 · z ±13…±15): vasi di pietra con aranci potati.
- **Due muretti** 0,8 × 4 × 1,1 (x 9,6…10,4 · z 4…8 e x −10,4…−9,6 · z −8…−4): panchine in pietra.

**I portici (blockout).** Su ogni lato (est x 14…20, ovest x −20…−14) un **porticato a due livelli**:
a terra si passa sotto (altezza 3 m) fra **quattro pilastri** 0,6 × 0,6 (z ±6, ±10); sopra c'è il
**loggiato** a 3,3 m (solaio 3,0–3,3) con il **parapetto** alto 1,05 m, raggiunto da una **scala** di
20 gradini (est: x 8…14 · z −12…−9; ovest: x −14…−8 · z 9…12). Archi a tutto sesto in pietra sopra i
pilastri, volte intonacate sotto, il **Caffè Centrale** sotto il portico est con le vetrine.

**Il campanile.** Sul palazzo a nord-est (lotto x 20,5…28 · z −26,2…−18) si alza il **campanile**
(x 22…27 · z −26…−21) fino a 28 m, con la cella campanaria aperta e la cuspide di coppi: landmark del
centro. *Correzione rispetto al vecchio decor: deve partire dal tetto del suo palazzo (9 m), non da 11,8 m.*

**Carattere.** Il cuore civico: palazzi nobiliari con cornicioni sagomati, lesene agli spigoli,
balconi d'onore, il Municipio. Due metà con pelle diversa: **lato nord** (verso il Porto) intonaci chiari
e persiane verdi; **lato sud** (verso il Forte) ocra e pietra. **Suolo**: grandi **lastre** di pietra
chiara a correre, con una **rosa dei venti** in pietra scura intarsiata intorno alla banca (decal +
normal) e fasce di sampietrini ai bordi.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PZ-01** | x -30…-20 · z -18…-12,5 | 10 × 5,5 m | 3 (9 m) | est su Piazza Vecchia 5,5 m | intonaco grigio perla; piano terra: libreria; 1 finestra per piano sul fronte est con persiane grigio ardesia; bandiere del Comune; lanterne in ghisa a muro; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PZ-02** | x -28…-20 · z 7…16 | 8 × 9 m | 4 (11,8 m) | nord su Mercato coperto Ovest 8 m; est su Piazza Vecchia 9 m | intonaco albicocca spento; piano terra: Caffè Centrale sotto il porticato; 2 finestre per piano sul fronte est con persiane rosso ossido scuro; lesene di pietra agli spigoli; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PZ-03** | x -28…-20 · z 16…25 | 8 × 9 m | 4 (11,8 m) | est su Piazza Vecchia 2 m | intonaco albicocca spento; piano terra: palazzo nobiliare con portone; 1 finestra per piano sul fronte est con persiane rosso ossido scuro; lesene di pietra agli spigoli; lanterne in ghisa a muro; altana con pergola sul tetto (skyline) |
| **PZ-04** | x -21,5…-13 · z -26,2…-18 | 8,5 × 8,2 m | 2 (6,2 m) | sud su Piazza Vecchia 7 m | intonaco grigio tortora; piano terra: gioielleria con le saracinesche abbassate; 2 finestre per piano sul fronte sud con persiane verde bottiglia; lanterne in ghisa a muro |
| **PZ-05** | x -20…-14,5 · z 18…26,5 | 5,5 × 8,5 m | 4 (11,8 m) | nord su Piazza Vecchia 5,5 m | intonaco albicocca spento; piano terra: Municipio con il balcone d'onore; 1 finestra per piano sul fronte nord con persiane noce; lanterne in ghisa a muro; lesene di pietra agli spigoli |
| **PZ-06** | x -13…-3 · z -26,5…-18 | 10 × 8,5 m | 2 (6,2 m) | sud su Piazza Vecchia 10 m | intonaco crema; piano terra: Circolo dei Marinai; 3 finestre per piano sul fronte sud con persiane legno naturale schiarito; cornicioni sagomati; lesene di pietra agli spigoli |
| **PZ-07** | x 4,7…12,3 · z 18…28 | 7,7 × 10 m | 3 (9 m) | nord su Piazza Vecchia 7,7 m | muratura di pietra mista intonacata solo al primo piano; piano terra: libreria; 2 finestre per piano sul fronte nord con persiane marrone castagna; bandiere del Comune; lanterne in ghisa a muro; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PZ-08** | x 12,3…20 · z 18…28 | 7,7 × 10 m | 3 (9 m) | nord su Piazza Vecchia 7,7 m | muratura di pietra mista intonacata solo al primo piano; piano terra: Caffè Centrale sotto il porticato; 2 finestre per piano sul fronte nord con persiane noce; lanterne in ghisa a muro; cornicioni sagomati; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PZ-09** | x 13…20,5 · z -26,2…-18 | 7,5 × 8,2 m | 4 (11,8 m) | sud su Piazza Vecchia 7 m | edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: palazzo nobiliare con portone; 2 finestre per piano sul fronte sud con persiane salvia; cornicioni sagomati; bandiere del Comune; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PZ-10** | x 20…28 · z -18…-12,5 | 8 × 5,5 m | 2 (6,2 m) | ovest su Piazza Vecchia 5,5 m | intonaco rosa antico spento; piano terra: gioielleria con le saracinesche abbassate; 1 finestra per piano sul fronte ovest con persiane verde bottiglia; lanterne in ghisa a muro |
| **PZ-11** | x 20…28 · z 7…16,8 | 8 × 9,8 m | 3 (9 m) | nord su Mercato coperto Est 8 m; ovest su Piazza Vecchia 9,8 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: Municipio con il balcone d'onore; 3 finestre per piano sul fronte ovest con persiane rosso ossido scuro; balconi d'onore in ferro; lesene di pietra agli spigoli |
| **PZ-12** | x 20…28 · z 16,8…26,6 | 8 × 9,8 m | 2 (6,2 m) | ovest su Piazza Vecchia 1,2 m | intonaco terra di Siena chiara; piano terra: Circolo dei Marinai; 1 finestra per piano sul fronte ovest con persiane ardesia; cornicioni sagomati; lesene di pietra agli spigoli |


## 12 · Mercati coperti

**Est**: x 20…36 · z −7…7 (16 × 14 m); **Ovest**: x −36…−20 · z −7…7. Una **sala a pilastri** coperta
da un tetto a 4,5–6,2 m, **sei pilastri** 0,6 × 0,6 × 4,5 (x ±24, ±28, ±32 · z ±3,5) e al centro il
**banco del pesce** 2,4 × 2,4 × 1,0 (x ±26,8…±29,2 · z −1,2…1,2).
**Carattere.** Una tettoia in ferro e vetro su pilastri di ghisa (rivestiti in pietra fino a 1,2 m),
lucernari, ventilatori a pale fermi, lavagne dei prezzi, il banco del pesce in marmo con il ghiaccio.
È la rotazione del difensore: aperto ai due capi, chiuso ai lati. **Suolo**: piastrelle di graniglia
sbeccate, canalette di scolo.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **ME-01** | x 20…28 · z -12,5…-7 | 8 × 5,5 m | 2 (6,2 m) | sud su Mercato coperto Est 8 m; ovest su Piazza Vecchia 5,5 m | intonaco sabbia chiara; piano terra: banchi coperti; 2 finestre per piano sul fronte sud con persiane verde bosco; lavagne dei prezzi; teli tesi |
| **ME-02** | x 28…36 · z -9…-7 | 8 × 2 m | 3 (9 m) | sud su Mercato coperto Est 8 m; est su Cortile Est 2 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco bianco calce; piano terra: pescheria; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; lavagne dei prezzi |
| **ME-03** | x 28…36 · z 7…16,8 | 8 × 9,8 m | 2 (6,2 m) | nord su Mercato coperto Est 8 m; est su Cortile Est 2 m | intonaco ocra; piano terra: banchi coperti; 2 finestre per piano sul fronte nord con persiane verde salvia scuro; lavagne dei prezzi; teli tesi |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **MW-01** | x -36…-30 · z -9…-7 | 6 × 2 m | 4 (11,8 m) | sud su Mercato coperto Ovest 6 m; ovest su Cortile Ovest 2 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro; piano terra: pescheria; 1 finestra per piano sul fronte sud con persiane grigio ardesia; teli tesi; altana con pergola sul tetto (skyline) |
| **MW-02** | x -36…-28 · z 7…16 | 8 × 9 m | 3 (9 m) | nord su Mercato coperto Ovest 8 m; ovest su Cortile Ovest 2 m | muratura di pietra mista intonacata solo al primo piano; piano terra: banchi coperti; 2 finestre per piano sul fronte nord con persiane marrone castagna; teli tesi; lavagne dei prezzi |
| **MW-03** | x -30…-20 · z -12,5…-7 | 10 × 5,5 m | 3 (9 m) | sud su Mercato coperto Ovest 10 m; est su Piazza Vecchia 5,5 m | intonaco grigio perla; piano terra: pescheria; 3 finestre per piano sul fronte sud con persiane grigio piombo; teli tesi |


## 13 · Cortili

**Est**: x 36…52 · z −9…9 (16 × 18 m), davanti all'ingresso principale della Grotta.
**Ovest**: x −52…−36 · z −9…9, davanti all'ingresso di servizio della Galleria.
**Ripari (blockout).** Un **pozzo** 4 × 4 × 0,9 (x ±42,5…±46,5 · z −2…2) con la **statua** (1,2 × 1,2,
fino a 2,6 m) di un pescatore con la rete, che taglia la linea dritta piazza → estrazione; una cassa
2,5 × 2,5 × 1; un muretto 2,4 × 0,6 × 1,1.
**Carattere.** Cortili di case a corte: pergolati, panche di pietra, una cappella, piante in vaso. Est
più marino (ancore, nasse), ovest più stradale (un vecchio cartello della statale).

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **CE-01** | x 28…37 · z -17,6…-9 | 9 × 8,6 m | 3 (9 m) | sud su Cortile Est 1 m | intonaco rosa antico spento; piano terra: casa a corte; 1 finestra per piano sul fronte sud con persiane salvia; panche di pietra |
| **CE-02** | x 36…44 · z 9…18,4 | 8 × 9,4 m | 4 (11,8 m) | nord su Cortile Est 8 m | intonaco terra di Siena chiara; piano terra: scuderia trasformata in garage; 2 finestre per piano sul fronte nord con persiane verde oliva; panche di pietra; pergolato |
| **CE-03** | x 37…46 · z -17,6…-9 | 9 × 8,6 m | 3 (9 m) | sud su Cortile Est 9 m | intonaco crema; piano terra: cappella; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; panche di pietra; pergolato; balcone in ferro al primo piano (sopra i 3,6 m) |
| **CE-04** | x 44…52 · z 9…18,4 | 8 × 9,4 m | 3 (9 m) | nord su Cortile Est 8 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: casa a corte; 2 finestre per piano sul fronte nord con persiane verde salvia scuro; pergolato; panche di pietra; balcone in ferro al primo piano (sopra i 3,6 m) |
| **CE-05** | x 46…52 · z -18,5…-9 | 6 × 9,5 m | 4 (11,8 m) | sud su Cortile Est 6 m | palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura; piano terra: scuderia trasformata in garage; 1 finestra per piano sul fronte sud con persiane verde bottiglia; panche di pietra; pergolato; altana con pergola sul tetto (skyline) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **CW-01** | x -62…-52 · z 2…11,3 | 10 × 9,3 m | 4 (11,8 m) | est su Cortile Ovest 7 m | intonaco ocra rossa chiara; piano terra: cappella; 2 finestre per piano sul fronte est con persiane marrone castagna; pergolato; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |
| **CW-02** | x -54…-46 · z -18,4…-9 | 8 × 9,4 m | 2 (6,2 m) | sud su Cortile Ovest 6 m | intonaco avorio; piano terra: lavanderia; 1 finestra per piano sul fronte sud con persiane grigio ardesia; pergolato |
| **CW-03** | x -54…-52 · z -9…-2 | 2 × 7 m | 3 (9 m) | est su Cortile Ovest 7 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco grigio perla; piano terra: casa a corte; 2 finestre per piano sul fronte est con persiane verde bosco; pergolato |
| **CW-04** | x -52…-44 · z 9…16 | 8 × 7 m | 2 (6,2 m) | nord su Cortile Ovest 8 m | intonaco albicocca spento; piano terra: cappella; 2 finestre per piano sul fronte nord con persiane noce; pergolato; panche di pietra |
| **CW-05** | x -46…-38 · z -18,4…-9 | 8 × 9,4 m | 3 (9 m) | sud su Cortile Ovest 8 m | intonaco camoscio; piano terra: lavanderia; 2 finestre per piano sul fronte sud con persiane legno naturale schiarito; panche di pietra |
| **CW-06** | x -44…-36 · z 9…16 | 8 × 7 m | 3 (9 m) | nord su Cortile Ovest 8 m | muratura di pietra mista intonacata solo al primo piano; piano terra: casa a corte; 2 finestre per piano sul fronte nord con persiane noce; panche di pietra |
| **CW-07** | x -38…-30 · z -18,4…-9 | 8 × 9,4 m | 4 (11,8 m) | sud su Cortile Ovest 2 m | palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare; piano terra: cappella; 1 finestra per piano sul fronte sud con persiane salvia; panche di pietra; pergolato |


---

# ESTRAZIONI

## 14 · Grotta del Levante — estrazione Est

**Pianta (blockout).** Tutto a quota 0, nella roccia (x 52…90 · z −28…28):
- **ingresso frontale** dal Cortile Est: galleria di 3 × 14 m (x 52…66 · z −1,5…1,5), **soffitto a 3 m**;
- la **camera**: 20 × 24 m (x 66…86 · z −12…12), **volta a 6 m**;
- due **ingressi laterali a gomito** larghi 3,4 m (soffitto 3 m): dal Molo (nord, x 66…70 da z −30) e
  dal Piazzale della Grotta (sud, da z 30), che entrano nella camera a z ±12;
- dentro: due **motoscafi** 4 × 5 × 1,4 (x 81…85 · z ±4…±9) ormeggiati alla banchina di pietra, due
  **scogli** 2 × 2,5 × 1,3 (x 70…72 · z ±5…±7,5), due pile di **casse** 1,5 × 1,5 × 1 (x 76,5…78 · z ±2,5…±4);
- il **punto di estrazione** (x 75…79 · z −2…2): una piattaforma con il **bordo giallo**.

**Carattere.** Chiusa e verticale: buio, roccia calcarea levigata dal mare, **acqua** che entra dal
fondo (est, oltre x 86: solo vista, bagnasciuga animato), riflessi d'acqua sulla volta, luce che filtra
dagli ingressi, lanterne dei contrabbandieri. La roccia deve leggersi come **parete** (niente rocce
sporgenti sotto i 3 m fuori dalle scatole). **Suolo**: roccia bagnata e sabbia scura che si fondono.

## 15 · Galleria Vecchia — estrazione Ovest

**Pianta (blockout).** Il **tunnel stradale**: 12 × 60 m (x −78…−66 · z −30…30), **volta a 6 m**,
aperto a nord sul Piazzale e a sud sulla Marina; **ingresso di servizio** dal Cortile Ovest
(x −66…−52 · z −2…2, 4 m, soffitto 3 m); due **nicchie di manutenzione** 4 × 4 (x −82…−78 · z ±10…±14,
soffitto 3 m). Dentro il **convoglio** fermo: due camion 2,5 × 7 × 3 (x −76…−73,5 · z ±3…±10) e due
2,5 × 6 × 3 (x −70,5…−68 · z ±16…±22), due **new jersey** 3 × 0,6 × 0,8 (x −75…−72 · z ±25…±25,6).
**Punto di estrazione**: x −74…−70 · z −2…2.

**Carattere.** Lunga e orizzontale: volta in calcestruzzo con le **lampade al sodio** (luce arancione
**spenta**, giallastra ma desaturata), cartelli di uscita, cavi lungo le pareti, il manto stradale con
la mezzeria, i camion del convoglio con i teloni. **Suolo**: asfalto e ghiaia.

---

# METÀ SUD (la Nord ruotata, con un'altra pelle)

> Stessa architettura, stessi ripari, stesse quote: cambiano i materiali (ocra, pietra, legno) e il
> racconto. I **nomi nuovi** di questa sezione (Spalti, Porta delle Mura, Marina, Bastione della Marina,
> Piazzetta del Lungomare, Piazzale della Grotta) vanno aggiunti al GLOSSARY se approvati.

## 16 · Forte San Verano — spawn Sud *(ruota il Porto Vecchio)*

**Pianta.** La **piazza d'armi** del Forte, 40 × 17 m (x −20…20 · z 82…99). Uscite: nord la Salita del
Forte, est il **Lungomare** (z 89…93), ovest un varco di 4 m verso gli **Spalti**.
**Ripari (blockout).** Dove a nord ci sono i container, qui ci sono **due ricoveri di legno**
6 × 2,4 × 2,6 (x −16…−10 e x 10…16, z 94,6…97): tettoie chiuse di tavole con il tetto a una falda,
piene di casse di munizioni e fieno. Le **pile di casse** (x ±9,5…±12 · z 84…86,5) sono casse di
munizioni con lo stemma. Le **piazzole** di spawn hanno il bordo **arancio**.
**Carattere.** Una fortezza vissuta: **stendardi rosso scuro**, bracieri in ferro **sopra i 2,5 m**,
caserma, armeria, corpo di guardia. A sud la **cortina muraria** (i lotti profondi 1 m) con merli e
camminamento; oltre, il **bastione** e la **torre** del Forte (24 m, landmark del Sud).
**Suolo**: acciottolato di fiume grigio con fasce di mattoni, l'erba fra le pietre vicino ai muri.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **FS-01** | x -20,5…-13 · z 73…82 | 7,5 × 9 m | 4 (11,8 m) | sud su Forte San Verano 7 m | intonaco ocra rossa chiara; piano terra: cucine del presidio; 2 finestre per piano sul fronte sud con persiane noce; rastrelliere di legno; balcone in ferro al primo piano (sopra i 3,6 m) |
| **FS-02** | x -20…-10 · z 99…100 | 10 × 1 m | 2 (6,2 m) | nord su Forte San Verano 10 m | tratto di **cortina muraria** del Forte alto 6,2 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista) |
| **FS-03** | x -13…-4 · z 76…82 | 9 × 6 m | 2 (6,2 m) | sud su Forte San Verano 9 m; est su Salita del Forte 6 m | intonaco ocra rossa chiara; piano terra: corpo di guardia (portone con la garitta); 2 finestre per piano sul fronte sud con persiane marrone castagna; stendardi rosso scuro sulle facciate; rastrelliere di legno |
| **FS-04** | x -10…0 · z 99…100 | 10 × 1 m | 4 (11,8 m) | nord su Forte San Verano 10 m | tratto di **cortina muraria** del Forte alto 11,8 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista); al centro della cortina la **porta del soccorso** murata, con lo stemma della torre |
| **FS-05** | x 0…10 · z 99…100 | 10 × 1 m | 4 (11,8 m) | nord su Forte San Verano 10 m | tratto di **cortina muraria** del Forte alto 11,8 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista); sopra i merli lo **stendardo** più grande del Forte |
| **FS-06** | x 4…13 · z 76…82 | 9 × 6 m | 2 (6,2 m) | sud su Forte San Verano 9 m; ovest su Salita del Forte 6 m | intonaco rosa salmone spento; piano terra: cappella del Forte; 2 finestre per piano sul fronte sud con persiane marrone castagna; stemma della torre; stendardi rosso scuro sulle facciate |
| **FS-07** | x 10…20 · z 99…100 | 10 × 1 m | 2 (6,2 m) | nord su Forte San Verano 10 m | tratto di **cortina muraria** del Forte alto 6,2 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista) |
| **FS-08** | x 13…21,5 · z 73…82 | 8,5 × 9 m | 2 (6,2 m) | sud su Forte San Verano 7 m | intonaco terra di Siena chiara; piano terra: scuderia; 2 finestre per piano sul fronte sud con persiane marrone castagna; stemma della torre; bracieri in ferro sopra i 2,5 m |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **FSO-01** | x -24…-20 · z 84…87 | 4 × 3 m | 4 (11,8 m) | sud su Forte San Verano (uscita ovest) 4 m; est su Forte San Verano 3 m; ovest su Spalti 3 m | intonaco ocra rossa chiara; piano terra: torretta di guardia; 1 finestra per piano sul fronte sud con persiane verde salvia scuro; stendardo; feritoie |
| **FSO-02** | x -24…-20 · z 95…100 | 4 × 5 m | 4 (11,8 m) | nord su Forte San Verano (uscita ovest) 4 m; est su Forte San Verano 4 m; ovest su Spalti 4 m | intonaco ocra rossa chiara; piano terra: postierla; 1 finestra per piano sul fronte nord con persiane noce; feritoie; stendardo |


## 17 · Salita del Forte, Largo del Forte, Arco Sud, Via del Mercato Sud *(ruotano la corsia centrale)*

Stessa sequenza del Nord, rovesciata: Salita (8 × 12 m) → **Largo del Forte** con la **fontana**
(qui una fontana a muro più rustica: vasca di pietra e un leone che butta acqua, stessa scatola) e la
**gradinata** a 1,53 m nell'angolo **sud-ovest**, che qui è il sagrato della **chiesa di San Verano**
(facciata in pietra con il campanile a vela) → **Arco Sud** (la porta del borgo, con lo stemma della
torre) → **Via del Mercato Sud** con il **carretto** (qui un carro a buoi senza buoi, carico di botti).
Botteghe dell'olio, salumeria, cestaio; tende color sabbia, corde di peperoncini.

_Nessun edificio ha qui il suo fronte principale: le facciate che si vedono sono elencate nelle aree vicine._

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **LF-01** | x -14,5…-9 · z 43,5…52 | 5,5 × 8,5 m | 3 (9 m) | sud su Largo del Forte 4 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: fabbro; 1 finestra per piano sul fronte sud con persiane verde salvia scuro; fontanella in ghisa; insegne in ferro battuto; balcone in ferro al primo piano (sopra i 3,6 m) |
| **LF-02** | x -13…-4 · z 70…76 | 9 × 6 m | 2 (6,2 m) | nord su Largo del Forte 9 m; est su Salita del Forte 6 m | intonaco albicocca spento; piano terra: forno del presidio; 2 finestre per piano sul fronte nord con persiane verde oliva; pergola di glicine (rami spogli) |
| **LF-03** | x 3…11,5 · z 44…52 | 8,5 × 8 m | 4 (11,8 m) | sud su Largo del Forte 8,5 m; ovest su Arco Sud 8 m | intonaco sabbia calda; piano terra: bottega del sellaio; 2 finestre per piano sul fronte sud con persiane noce; pergola di glicine (rami spogli); insegne in ferro battuto |
| **LF-04** | x 4…13 · z 70…76 | 9 × 6 m | 2 (6,2 m) | nord su Largo del Forte 9 m; ovest su Salita del Forte 6 m | intonaco ocra; piano terra: erboristeria; 2 finestre per piano sul fronte nord con persiane rosso ossido scuro; orci di terracotta; pergola di glicine (rami spogli) |
| **LF-05** | x 11,5…20 · z 44…52 | 8,5 × 8 m | 4 (11,8 m) | sud su Largo del Forte 1,5 m | intonaco ocra rossa chiara; piano terra: osteria; 1 finestra per piano sul fronte sud con persiane rosso ossido scuro; insegne in ferro battuto; orci di terracotta; balcone in ferro al primo piano (sopra i 3,6 m) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **AS-01** | x -9…-3 · z 43…52 | 6 × 9 m | 4 (11,8 m) | sud su Largo del Forte 6 m; est su Arco Sud 8 m; est su Via del Mercato Sud 1 m | intonaco rosa salmone spento; piano terra: porta del borgo; 2 finestre per piano sul fronte est con persiane verde oliva; stemma; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **VMS-01** | x -14,5…-9 · z 18…26,5 | 5,5 × 8,5 m | 4 (11,8 m) | nord su Piazza Vecchia 5,5 m; est su Via del Mercato Sud 8,5 m | intonaco ocra; piano terra: cestaio; 2 finestre per piano sul fronte est con persiane verde salvia scuro; tende color sabbia sopra le vetrine |
| **VMS-02** | x -14,5…-9 · z 26,5…35 | 5,5 × 8,5 m | 4 (11,8 m) | est su Via del Mercato Sud 6 m; est su Via del Mercato Sud 1,5 m | intonaco rosa salmone spento; piano terra: salumeria; 1 finestra per piano sul fronte est con persiane verde oliva; persiane a ventola; altana con pergola sul tetto (skyline) |
| **VMS-03** | x -9…-3 · z 34…43 | 6 × 9 m | 4 (11,8 m) | nord su Via del Mercato Sud 6 m; est su Via del Mercato Sud 9 m | intonaco sabbia calda; piano terra: vinaio; 2 finestre per piano sul fronte est con persiane noce; cesti appesi sopra i 2,3 m; tende color sabbia sopra le vetrine; balcone in ferro al primo piano (sopra i 3,6 m) |
| **VMS-04** | x -3…4,7 · z 18…28 | 7,7 × 10 m | 3 (9 m) | nord su Piazza Vecchia 7,7 m; sud su Via del Mercato Sud 6 m; ovest su Via del Mercato Sud 10 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: ferramenta; 3 finestre per piano sul fronte ovest con persiane verde salvia scuro; persiane a ventola; cesti appesi sopra i 2,3 m |
| **VMS-05** | x 3…11,5 · z 28…36 | 8,5 × 8 m | 3 (9 m) | ovest su Via del Mercato Sud 2 m; ovest su Via del Mercato Sud 6 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: bottega dell'olio; 1 finestra per piano sul fronte ovest con persiane noce; tende color sabbia sopra le vetrine; persiane a ventola; balcone in ferro al primo piano (sopra i 3,6 m) |
| **VMS-06** | x 3…11,5 · z 36…44 | 8,5 × 8 m | 2 (6,2 m) | ovest su Via del Mercato Sud 8 m | intonaco albicocca spento; piano terra: cestaio; 2 finestre per piano sul fronte ovest con persiane verde salvia scuro; cesti appesi sopra i 2,3 m; tende color sabbia sopra le vetrine |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PLS-01** | x 13…20 · z 52…59 | 7 × 7 m | 2 (6,2 m) | sud su Passaggio Est del Largo Sud 7 m; ovest su Largo del Forte 7 m | intonaco sabbia calda; piano terra: androne; 2 finestre per piano sul fronte sud con persiane rosso ossido scuro; volta a botte con lanterna |
| **PLS-02** | x 13…21,5 · z 64…73 | 8,5 × 9 m | 2 (6,2 m) | nord su Passaggio Est del Largo Sud 8,5 m; ovest su Largo del Forte 6 m | intonaco sabbia calda; piano terra: deposito; 2 finestre per piano sul fronte nord con persiane noce; volta a botte con lanterna |
| **PLS-03** | x 20…30 · z 56…59 | 10 × 3 m | 3 (9 m) | sud su Passaggio Est del Largo Sud 10 m; est su Piazzetta del Lungomare 3 m | muratura di pietra mista intonacata solo al primo piano; piano terra: androne; 3 finestre per piano sul fronte sud con persiane marrone castagna; volta a botte con lanterna; balcone in ferro al primo piano (sopra i 3,6 m) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PLS-01** | x -28…-20,5 · z 52…59 | 7,5 × 7 m | 2 (6,2 m) | sud su Passaggio Ovest del Largo Sud 7,5 m; ovest su Mura 7 m | intonaco sabbia calda; piano terra: deposito; 2 finestre per piano sul fronte sud con persiane noce; volta a botte con lanterna |
| **PLS-02** | x -20,5…-13 · z 52…59 | 7,5 × 7 m | 3 (9 m) | sud su Passaggio Ovest del Largo Sud 7,5 m; est su Largo del Forte 7 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: androne; 2 finestre per piano sul fronte sud con persiane verde salvia scuro; volta a botte con lanterna |
| **PLS-03** | x -20,5…-13 · z 64…73 | 7,5 × 9 m | 4 (11,8 m) | nord su Passaggio Ovest del Largo Sud 7,5 m; est su Largo del Forte 6 m | intonaco rosa salmone spento; piano terra: deposito; 2 finestre per piano sul fronte nord con persiane noce; volta a botte con lanterna |


## 18 · Mura — corsia ovest del Sud verso la Galleria *(ruota la Scalinata Lunga)*

**Spalti** (ruota la Banchina, x −46…−24 · z 84…99): il terrapieno interno delle mura, con i ricoveri di
legno al posto dei container (6 × 2,4 × 2,6 e quello di traverso) e un cannone su affusto **dentro** il
lotto. **Porta delle Mura** (ruota il Varco, x −42…−36 · z 74…84): la porta urbica coperta per 6 m da
un corpo di **pietra** (al Nord è cemento), con la saracinesca alzata e lo stemma. **Mura** (ruota il
Cortile della Dogana, x −54…−28 · z 52…74): la corte della caserma, con la pesa trasformata in
**cisterna** coperta (il pilastro), il carro dei viveri, una lapide. **Discesa alla Marina** →
**Marina** (ruota il Molo, x −82…−46 · z 30…44): il **cantiere delle barche di legno** (la barca in secca è
uno scafo in costruzione con le costole a vista), e la bocca **sud della Galleria**. **Bastione della
Marina** (ruota la Terrazza, x −72…−58 · z 44…56): il terrazzo a 3,4 m diventa un **bastione** con
merli bassi al posto del parapetto (stessa altezza 1,05 m) e una meridiana.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **SPA-01** | x -54…-46 · z 84…92 | 8 × 8 m | 2 (6,2 m) | est su Spalti 8 m | intonaco terra di Siena chiara; piano terra: polveriera; 2 finestre per piano sul fronte est con persiane verde oliva; palle di cannone impilate |
| **SPA-02** | x -54…-46 · z 92…100 | 8 × 8 m | 4 (11,8 m) | est su Spalti 7 m | intonaco ocra; piano terra: alloggi; 2 finestre per piano sul fronte est con persiane marrone castagna; palle di cannone impilate; merli; altana con pergola sul tetto (skyline) |
| **SPA-03** | x -46…-38,7 · z 99…100 | 7,3 × 1 m | 4 (11,8 m) | nord su Spalti 7,3 m | tratto di **cortina muraria** del Forte alto 11,8 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista) |
| **SPA-04** | x -38,7…-31,3 · z 99…100 | 7,3 × 1 m | 4 (11,8 m) | nord su Spalti 7,3 m | tratto di **cortina muraria** del Forte alto 11,8 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista) |
| **SPA-05** | x -31,3…-24 · z 99…100 | 7,3 × 1 m | 4 (11,8 m) | nord su Spalti 7,3 m | tratto di **cortina muraria** del Forte alto 11,8 m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista) |
| **SPA-06** | x -28…-20 · z 82…84 | 8 × 2 m | 4 (11,8 m) | sud su Spalti 4 m; est su Forte San Verano 2 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco sabbia calda; piano terra: magazzino del presidio; 1 finestra per piano sul fronte sud con persiane ardesia; cannone su affusto (dentro il lotto); merli |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PM-01** | x -48…-42 · z 74…84 | 6 × 10 m | 3 (9 m) | nord su Mura 6 m; sud su Spalti 4 m; est su Porta delle Mura 10 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: corpo di guardia; 3 finestre per piano sul fronte est con persiane rosso ossido scuro; saracinesca alzata; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PM-02** | x -36…-28 · z 74…84 | 8 × 10 m | 2 (6,2 m) | nord su Mura 8 m; sud su Spalti 8 m; ovest su Porta delle Mura 10 m | intonaco terra di Siena chiara; piano terra: torre della porta; 3 finestre per piano sul fronte ovest con persiane verde salvia scuro; saracinesca alzata |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **MU-01** | x -58…-54 · z 52…61,6 | 4 × 9,6 m | 4 (11,8 m) | est su Mura 9,6 m; ovest su Bastione della Marina 4 m | intonaco albicocca spento; piano terra: cisterna; 3 finestre per piano sul fronte est con persiane verde oliva; bracieri; altana con pergola sul tetto (skyline) |
| **MU-02** | x -58…-54 · z 61,6…71,2 | 4 × 9,6 m | 3 (9 m) | est su Mura 9,6 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: caserma; 3 finestre per piano sul fronte est con persiane verde salvia scuro; stendardi; lapide commemorativa |
| **MU-03** | x -58…-54 · z 71,2…80,8 | 4 × 9,6 m | 2 (6,2 m) | est su Mura 2,8 m | intonaco ocra rossa chiara; piano terra: alloggi degli ufficiali; 1 finestra per piano sul fronte est con persiane verde oliva; bracieri; camminamento con merli |
| **MU-04** | x -54…-48 · z 74…84 | 6 × 10 m | 3 (9 m) | nord su Mura 6 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: cappella di Santa Barbara; 1 finestra per piano sul fronte nord con persiane noce; bracieri |
| **MU-05** | x -46…-36 · z 44,7…52 | 10 × 7,3 m | 2 (6,2 m) | sud su Mura 10 m; ovest su Discesa alla Marina 7,3 m | intonaco camoscio caldo; piano terra: cisterna; 3 finestre per piano sul fronte sud con persiane noce; camminamento con merli; lapide commemorativa |
| **MU-06** | x -36…-28 · z 43…52 | 8 × 9 m | 3 (9 m) | sud su Mura 8 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: caserma; 2 finestre per piano sul fronte sud con persiane verde oliva; stendardi; bracieri |
| **MU-07** | x -28…-20,5 · z 64…73 | 7,5 × 9 m | 3 (9 m) | nord su Passaggio Ovest del Largo Sud 7,5 m; ovest su Mura 9 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: alloggi degli ufficiali; 2 finestre per piano sul fronte ovest con persiane verde salvia scuro; stendardi; lapide commemorativa |
| **MU-08** | x -28…-20,5 · z 73…82 | 7,5 × 9 m | 3 (9 m) | ovest su Mura 1 m | muratura di pietra mista intonacata solo al primo piano; piano terra: cappella di Santa Barbara; 1 finestra per piano sul fronte ovest con persiane verde oliva; camminamento con merli; balcone in ferro al primo piano (sopra i 3,6 m) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **DMS-01** | x -58…-52 · z 44…52 | 6 × 8 m | 3 (9 m) | nord su Marina 6 m; sud su Mura 2 m; est su Discesa alla Marina 8 m; ovest su Bastione della Marina 8 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: torre delle scale; 2 finestre per piano sul fronte est con persiane verde salvia scuro; corrimano |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **MA-01** | x -90…-82 · z 30…40 | 8 × 10 m | 3 (9 m) | est su Marina 10 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: cantiere delle barche di legno; 3 finestre per piano sul fronte est con persiane verde oliva; scafi in costruzione sul fianco; trucioli e tavole sopra la tettoia |
| **MA-02** | x -90…-82 · z 40…50 | 8 × 10 m | 4 (11,8 m) | est su Marina 4 m | intonaco sabbia calda; piano terra: osteria della Marina; 1 finestra per piano sul fronte est con persiane rosso ossido scuro; scafi in costruzione sul fianco; lanterne; balcone in ferro al primo piano (sopra i 3,6 m) |
| **MA-03** | x -82…-72 · z 44…53,3 | 10 × 9,3 m | 3 (9 m) | nord su Marina 10 m; est su Bastione della Marina 9,3 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: rimessa; 3 finestre per piano sul fronte nord con persiane rosso ossido scuro; lanterne; scafi in costruzione sul fianco |
| **MA-04** | x -62…-52 · z 20,7…30 | 10 × 9,3 m | 4 (11,8 m) | sud su Marina 10 m | intonaco ocra rossa chiara; piano terra: casa del maestro d'ascia; 3 finestre per piano sul fronte sud con persiane ardesia; lanterne; trucioli e tavole sopra la tettoia |
| **MA-05** | x -52…-44 · z 23…30 | 8 × 7 m | 4 (11,8 m) | sud su Marina 6 m | intonaco albicocca spento; piano terra: cantiere delle barche di legno; 1 finestra per piano sul fronte sud con persiane verde oliva; lanterne; scafi in costruzione sul fianco |
| **MA-06** | x -46…-36 · z 30…37,3 | 10 × 7,3 m | 3 (9 m) | ovest su Marina 7,3 m | muratura di pietra mista intonacata solo al primo piano; piano terra: osteria della Marina; 2 finestre per piano sul fronte ovest con persiane verde oliva; trucioli e tavole sopra la tettoia; balcone in ferro al primo piano (sopra i 3,6 m) |
| **MA-07** | x -46…-36 · z 37,3…44,7 | 10 × 7,3 m | 3 (9 m) | ovest su Discesa alla Marina 4,7 m; ovest su Marina 6,7 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: rimessa; 2 finestre per piano sul fronte ovest con persiane marrone castagna; scafi in costruzione sul fianco; trucioli e tavole sopra la tettoia |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **BM-01** | x -82…-72 · z 53,3…62,7 | 10 × 9,3 m | 2 (6,2 m) | est su Bastione della Marina 2,7 m | intonaco albicocca spento; piano terra: casa del guardiano; 1 finestra per piano sul fronte est con persiane noce; vasi di agavi |
| **BM-02** | x -72…-65 · z 56…64,8 | 7 × 8,8 m | 3 (9 m) | nord su Bastione della Marina 7 m | muratura di pietra mista intonacata solo al primo piano; piano terra: magazzino; 2 finestre per piano sul fronte nord con persiane verde salvia scuro; vasi di agavi; meridiana |
| **BM-03** | x -65…-58 · z 56…64,8 | 7 × 8,8 m | 2 (6,2 m) | nord su Bastione della Marina 7 m | intonaco ocra rossa chiara; piano terra: casa del guardiano; 2 finestre per piano sul fronte nord con persiane ardesia; meridiana; vasi di agavi |


## 19 · Lungomare — corsia est del Sud verso la Grotta *(ruota la Via Stretta)*

**Lungomare** (x 20…35 · z 89…93, poi x 31…35 · z 72…93): un vicolo di 4 m fra **villini** con giardini
pensili, balaustre in pietra **sopra 1,1 m** (nessun volume a terra), bouganville secca. **Piazzetta del
Lungomare** (ruota la Piazzetta del Forno, x 30…58 · z 56…74): il forno diventa un **frantoio** (stessa
stanza, stesse porte), con la macina dipinta sul muro, le giare e l'insegna "OLIO". **Passaggio del
Piazzale Sud** → **Piazzale della Grotta** (ruota il Piazzale della Galleria, x 56…82 · z 30…44): il
camion diventa un **carro coperto** di legno (6 × 2,4 × 3), il new jersey un **blocco di pietra** da
ormeggio, e a nord si apre l'**ingresso sud della Grotta**. Deposito del sale, casa dei doganieri,
lazzaretto vecchio; cartello "GROTTA DEL LEVANTE" in ceramica.

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **LU-01** | x 20…30 · z 82…89 | 10 × 7 m | 4 (11,8 m) | sud su Lungomare 10 m; ovest su Forte San Verano 7 m | intonaco terra di Siena chiara; piano terra: stabilimento chiuso per la stagione; 3 finestre per piano sul fronte sud con persiane verde oliva; bouganville secca sui muri; fanali; balcone in ferro al primo piano (sopra i 3,6 m) |
| **LU-02** | x 20…27,5 · z 93…100 | 7,5 × 7 m | 3 (9 m) | nord su Lungomare 7,5 m; ovest su Forte San Verano 6 m | muratura di pietra mista intonacata solo al primo piano; piano terra: pensione affacciata; 2 finestre per piano sul fronte nord con persiane ardesia; balaustre in pietra |
| **LU-03** | x 27,5…35 · z 93…100 | 7,5 × 7 m | 2 (6,2 m) | nord su Lungomare 7,5 m; nord su Lungomare 4 m | intonaco ocra; piano terra: casa del custode; 2 finestre per piano sul fronte nord con persiane ardesia; fanali |
| **LU-04** | x 30…31 · z 74…81,5 | 1 × 7,5 m | 2 (6,2 m) | nord su Piazzetta del Lungomare 1 m; est su Lungomare 7,5 m | lotto profondo 1 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco sabbia calda; piano terra: villino con giardino pensile; 2 finestre per piano sul fronte est con persiane marrone castagna; persiane a ventola; fanali |
| **LU-05** | x 30…31 · z 81,5…89 | 1 × 7,5 m | 3 (9 m) | sud su Lungomare 1 m; est su Lungomare 7,5 m | lotto profondo 1 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: stabilimento chiuso per la stagione; 2 finestre per piano sul fronte est con persiane verde salvia scuro; fanali |
| **LU-06** | x 35…42,7 · z 74…82,7 | 7,7 × 8,7 m | 2 (6,2 m) | nord su Piazzetta del Lungomare 7,7 m; ovest su Lungomare 8,7 m | intonaco albicocca spento; piano terra: pensione affacciata; 2 finestre per piano sul fronte ovest con persiane noce; balaustre in pietra |
| **LU-07** | x 35…42,7 · z 82,7…91,3 | 7,7 × 8,7 m | 3 (9 m) | ovest su Lungomare 2,3 m; ovest su Lungomare 8,7 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: casa del custode; 2 finestre per piano sul fronte ovest con persiane ardesia; persiane a ventola; fanali |
| **LU-08** | x 35…42,7 · z 91,3…100 | 7,7 × 8,7 m | 2 (6,2 m) | ovest su Lungomare 1,7 m; ovest su Lungomare 1,7 m | intonaco albicocca spento; piano terra: villino con giardino pensile; 1 finestra per piano sul fronte ovest con persiane rosso ossido scuro; balaustre in pietra; bouganville secca sui muri |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PLU-01** | x 21,5…30 · z 64…73 | 8,5 × 9 m | 4 (11,8 m) | nord su Passaggio Est del Largo Sud 8,5 m; est su Piazzetta del Lungomare 9 m | intonaco ocra rossa chiara; piano terra: lavatoio; 2 finestre per piano sul fronte est con persiane noce; botti sopra i 2,5 m; insegna OLIO; altana con pergola sul tetto (skyline) |
| **PLU-02** | x 21,5…30 · z 73…82 | 8,5 × 9 m | 4 (11,8 m) | est su Piazzetta del Lungomare 1 m | intonaco sabbia calda; piano terra: deposito delle olive; 1 finestra per piano sul fronte est con persiane verde salvia scuro; giare di terracotta; insegna OLIO; balcone in ferro al primo piano (sopra i 3,6 m) |
| **PLU-03** | x 28…36 · z 46,2…56 | 8 × 9,8 m | 2 (6,2 m) | sud su Piazzetta del Lungomare 6 m | intonaco albicocca spento; piano terra: frantoio (la stanza attraversabile); 1 finestra per piano sul fronte sud con persiane noce; macina in pietra dipinta sul muro; giare di terracotta |
| **PLU-04** | x 36…44 · z 46,6…56 | 8 × 9,4 m | 3 (9 m) | sud su Piazzetta del Lungomare 8 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: bottaio; 2 finestre per piano sul fronte sud con persiane verde salvia scuro; insegna OLIO |
| **PLU-05** | x 42,7…50,3 · z 74…82,7 | 7,7 × 8,7 m | 4 (11,8 m) | nord su Piazzetta del Lungomare 7,7 m | intonaco terra di Siena chiara; piano terra: cantina sociale; 2 finestre per piano sul fronte nord con persiane marrone castagna; giare di terracotta; insegna OLIO; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |
| **PLU-06** | x 44…52 · z 46,6…56 | 8 × 9,4 m | 2 (6,2 m) | sud su Piazzetta del Lungomare 8 m | intonaco sabbia calda; piano terra: lavatoio; 2 finestre per piano sul fronte sud con persiane noce; insegna OLIO |
| **PLU-07** | x 50,3…58 · z 74…82,7 | 7,7 × 8,7 m | 4 (11,8 m) | nord su Piazzetta del Lungomare 7,7 m | intonaco ocra rossa chiara; piano terra: deposito delle olive; 2 finestre per piano sul fronte nord con persiane rosso ossido scuro; insegna OLIO; macina in pietra dipinta sul muro |
| **PLU-08** | x 58…62 · z 56…64,8 | 4 × 8,8 m | 4 (11,8 m) | nord su Passaggio del Piazzale Sud 4 m; ovest su Piazzetta del Lungomare 8,8 m | intonaco terra di Siena chiara; piano terra: frantoio (la stanza attraversabile); 2 finestre per piano sul fronte ovest con persiane ardesia; macina in pietra dipinta sul muro; altana con pergola sul tetto (skyline) |
| **PLU-09** | x 58…62 · z 64,8…73,6 | 4 × 8,8 m | 4 (11,8 m) | ovest su Piazzetta del Lungomare 8,8 m | intonaco camoscio caldo; piano terra: bottaio; 2 finestre per piano sul fronte ovest con persiane noce; macina in pietra dipinta sul muro; insegna OLIO |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PPS-01** | x 52…54 · z 44…50 | 2 × 6 m | 3 (9 m) | est su Passaggio del Piazzale Sud 6 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); pietra calcarea a conci regolari, cornicione in pietra; piano terra: rimessa dei carri; 1 finestra per piano sul fronte est con persiane verde oliva; finimenti appesi |
| **PPS-02** | x 52…54 · z 50…56 | 2 × 6 m | 3 (9 m) | sud su Piazzetta del Lungomare 2 m; est su Passaggio del Piazzale Sud 6 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); pietra calcarea a conci regolari, cornicione in pietra; piano terra: stalla; 1 finestra per piano sul fronte est con persiane marrone castagna; finimenti appesi |
| **PPS-03** | x 62…72 · z 53,3…62,7 | 10 × 9,3 m | 4 (11,8 m) | ovest su Passaggio del Piazzale Sud 2,7 m | intonaco camoscio caldo; piano terra: rimessa dei carri; 1 finestra per piano sul fronte ovest con persiane verde salvia scuro; finimenti appesi; balcone in ferro al primo piano (sopra i 3,6 m); altana con pergola sul tetto (skyline) |

| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |
|---|---|---|---|---|---|
| **PGR-01** | x 52…59 · z 28…30 | 7 × 2 m | 2 (6,2 m) | sud su Piazzale della Grotta 3 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco camoscio caldo; piano terra: lazzaretto vecchio; 1 finestra per piano sul fronte sud con persiane marrone castagna; cartello GROTTA DEL LEVANTE in ceramica; fanale di segnalazione |
| **PGR-02** | x 52…56 · z 30…37 | 4 × 7 m | 2 (6,2 m) | est su Piazzale della Grotta 7 m | intonaco terra di Siena chiara; piano terra: taverna dei contrabbandieri (chiusa); 2 finestre per piano sul fronte est con persiane rosso ossido scuro; nasse impilate sopra i 2,5 m; fanale di segnalazione |
| **PGR-03** | x 52…56 · z 37…44 | 4 × 7 m | 3 (9 m) | sud su Passaggio del Piazzale Sud 2 m; est su Piazzale della Grotta 7 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: deposito del sale; 2 finestre per piano sul fronte est con persiane verde oliva; nasse impilate sopra i 2,5 m; fanale di segnalazione |
| **PGR-04** | x 59…66 · z 28…30 | 7 × 2 m | 3 (9 m) | sud su Piazzale della Grotta 7 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); muratura di pietra mista intonacata solo al primo piano; piano terra: rimessa delle barche; 2 finestre per piano sul fronte sud con persiane ardesia; cartello GROTTA DEL LEVANTE in ceramica; nasse impilate sopra i 2,5 m |
| **PGR-05** | x 62…72 · z 44…53,3 | 10 × 9,3 m | 2 (6,2 m) | nord su Piazzale della Grotta 10 m; ovest su Passaggio del Piazzale Sud 9,3 m | intonaco terra di Siena chiara; piano terra: casa dei doganieri; 3 finestre per piano sul fronte nord con persiane noce; nasse impilate sopra i 2,5 m; cartello GROTTA DEL LEVANTE in ceramica |
| **PGR-06** | x 70…80 · z 28…30 | 10 × 2 m | 4 (11,8 m) | sud su Piazzale della Grotta 10 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco rosa salmone spento; piano terra: lazzaretto vecchio; 3 finestre per piano sul fronte sud con persiane verde oliva; fanale di segnalazione; nasse impilate sopra i 2,5 m; altana con pergola sul tetto (skyline) |
| **PGR-07** | x 72…82 · z 44…53,3 | 10 × 9,3 m | 3 (9 m) | nord su Piazzale della Grotta 10 m | pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra; piano terra: taverna dei contrabbandieri (chiusa); 3 finestre per piano sul fronte nord con persiane noce; fanale di segnalazione; nasse impilate sopra i 2,5 m |
| **PGR-08** | x 80…90 · z 28…30 | 10 × 2 m | 2 (6,2 m) | sud su Piazzale della Grotta 2 m | lotto profondo 2 m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro); intonaco sabbia calda; piano terra: deposito del sale; 1 finestra per piano sul fronte sud con persiane verde salvia scuro; nasse impilate sopra i 2,5 m; cartello GROTTA DEL LEVANTE in ceramica |
| **PGR-09** | x 82…90 · z 30…40 | 8 × 10 m | 3 (9 m) | ovest su Piazzale della Grotta 10 m | pietra calcarea a conci regolari, cornicione in pietra; piano terra: rimessa delle barche; 3 finestre per piano sul fronte ovest con persiane rosso ossido scuro; nasse impilate sopra i 2,5 m |
| **PGR-10** | x 82…90 · z 40…50 | 8 × 10 m | 4 (11,8 m) | ovest su Piazzale della Grotta 4 m | intonaco albicocca spento; piano terra: casa dei doganieri; 1 finestra per piano sul fronte ovest con persiane verde salvia scuro; fanale di segnalazione; nasse impilate sopra i 2,5 m; altana con pergola sul tetto (skyline) |


---

# Il resto del paese

## 20 · Il cuore degli isolati (152 lotti)

I lotti che non danno su nessuna area giocabile (43 da 2 piani, 61 da 3, 48 da 4) si vedono **solo dall'alto e
nello skyline**. Non hanno facciate dettagliate: muri intonacati semplici nelle tinte della loro metà,
e un **tetto vero** — coppi con colmo e gronda, **comignoli** (sette tetti su dieci), qualche **altana**
con pergola, **serbatoi** dell'acqua e **antenne** (le stesse regole del vecchio `roofscape`, che si
tengono). Servono a dare al paese il disegno a "tetti fitti" visto dalle posizioni alte (Terrazza,
loggiati, Bastione).

## 21 · Oltre il bordo (solo vista)

- **Nord**: il mare, il **molo** di cemento in punta allo spawn, la **gru** a traliccio, barche ormeggiate
  ferme, un faro lontano sul promontorio a nord-est.
- **Sud**: il **bastione** del Forte e la **torre** (24 m) con lo stendardo; dietro, la collina con gli
  ulivi (pittura di sfondo).
- **Est**: la scogliera sopra la Grotta, il mare aperto.
- **Ovest**: la montagna in cui entra la Galleria, la vecchia strada statale che sale a tornanti
  (pittura di sfondo).
- **Il mare** è un piano animato a −2 m, con la schiuma contro i moli: limite invalicabile, mai giocabile.

---

## Ordine di produzione (doc 29)

1. **Porto Vecchio** (spawn Nord) con la Salita: magazzini sul mare, i quattro palazzi a sud, le due
   torri dell'uscita est, i container, le casse, il suolo, la gru e il molo oltre il bordo.
2. **Forte San Verano** (spawn Sud): la stessa architettura con la pelle del Forte.
3. Largo e Arco (Nord e Sud) → Via del Mercato → Piazza Vecchia → Mercati → Cortili.
4. Corsie laterali (Scalinata Lunga, Via Stretta, Mura, Lungomare) → Grotta e Galleria.
5. Cuore degli isolati e skyline.

Ogni area: **modelli** (Blender + Meshy) → **materiali** della libreria → **sostituzione del blockout
in Unity** (le scatole restano come collisione, si nasconde solo la loro grafica) → foto dalle viste
fisse → resoconto.
