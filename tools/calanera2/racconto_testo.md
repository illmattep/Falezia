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

{{T:Porto Vecchio}}
{{T:Porto Vecchio (uscita est)}}

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

{{T:Salita del Porto}}

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

{{T:Largo}}

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

{{T:Via del Mercato}}

## 6 · I passaggi coperti del Largo

**Ovest**: x −34…−13 · z −64…−59 (21 × 5 m), coperto per 10 m (x −26…−16) da un corpo in **intonaco**
a 3,2–6,2 m. Collega il Largo alla Piazzetta del Forno.
**Est**: x 13…30 · z −64…−59 (17 × 5 m), coperto per 10 m (x 16…26). Collega il Largo al Cortile della
Dogana.

**Carattere.** Due **sottoportici**: volta a crociera intonacata, lanterne a muro, un tratto di pavimento
in mattoni a spina di pesce sotto la copertura, i fianchi in pietra fino a 1,2 m. Sono le rotazioni di
chi attacca: devono leggersi come "scorciatoie", più buie della strada.

{{T:Passaggio Ovest del Largo}}
{{T:Passaggio Est del Largo}}

## 7 · Scalinata Lunga — corsia est verso la Grotta

### 7a · Banchina

**Pianta.** **22 × 15 m** (x 24…46 · z −99…−84): la banchina commerciale, collegata allo spawn dal varco
di 4 m.
**Ripari (blockout).** Container **6 × 2,4 × 2,6** (x 30…36 · z −97…−94,6) e container **di traverso**
2,4 × 6 × 2,6 (x 40…42,4 · z −94…−88); cassa 2,5 × 2,5 × 1 (x 27…29,5 · z −88…−85,5).
**Carattere.** Magazzini della compagnia di navigazione, portoni scorrevoli con i numeri a stencil,
fusti di carburante, una gru gommata ferma (solo nello skyline oltre il bordo). **Suolo**: cemento con
binari e bitte dipinte.

{{T:Banchina}}

### 7b · Varco della Dogana

**Pianta.** Strozzatura di **6 × 10 m** (x 36…42 · z −84…−74), **coperta per 6 m** (z −82…−76) da un
corpo in **cemento** a 3,4–6,2 m: la **casa doganale** che scavalca la strada.
**Carattere.** Sbarra a bilico alzata, garitta dipinta (dentro il lotto VD), cartello "DOGANA — ALT"
sbiadito, faretti sotto il solaio. Il soffitto basso e piatto fa sentire la strozzatura.

{{T:Varco della Dogana}}

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

{{T:Cortile della Dogana}}

### 7d · Discesa al Molo

**Pianta.** **6 × 12 m** (x 46…52 · z −52…−40), in piano, fra due palazzi alti. Un lampione, una
fontanella a muro (dentro il lotto), la targa "AL MOLO" con la freccia.

{{T:Discesa al Molo}}

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

{{T:Molo}}

### 7f · Terrazza del Molo

**Pianta e quote (blockout).** Un **terrazzo di pietra a 3,4 m** (x 58…72 · z −56…−50), con la
**scala** di 20 gradini (x 66…70 · z −50…−44) che sale dal Molo verso nord, e il **parapetto** alto
1,05 m sul bordo sud (x 58…66 e x 70…72, z −50,3…−50; varco sulla scala). Davanti al terrazzo, a
terra, restano due rientranze (x 58…66 e x 70…72, z −50…−44).
**Carattere.** Il belvedere dei pescatori: panche in muratura **dentro** il parapetto, vasi di agavi
sul parapetto (sopra 1,05 m solo foglie, nessun volume), una pergola in ferro senza tralci. Domina
l'ingresso nord della Grotta: deve leggersi chiaramente come **posizione alta**.

{{T:Terrazza del Molo}}

## 8 · Via Stretta — corsia ovest verso la Galleria

**Pianta.** Vicolo largo **4 m**, a gomito: dallo spawn verso ovest (x −35…−20 · z −93…−89), poi verso
sud (x −35…−31 · z −93…−72) fino alla Piazzetta del Forno. Nessun riparo: i ripari sono gli spigoli.
**Carattere.** Il vicolo più stretto della mappa: case alte (3–4 piani) quasi a toccarsi, **panni stesi a
4,6 m**, edicola votiva con il lumino, scritte vecchie sui muri, vasi di basilico sui davanzali, contatori
dell'acqua. Luce che entra a lame. **Suolo**: lastre di pietra con la canaletta centrale, muschio ai piedi
dei muri.

{{T:Via Stretta}}

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

{{T:Piazzetta del Forno}}

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

{{T:Passaggio del Piazzale}}
{{T:Piazzale della Galleria}}

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

{{T:Piazza Vecchia}}

## 12 · Mercati coperti

**Est**: x 20…36 · z −7…7 (16 × 14 m); **Ovest**: x −36…−20 · z −7…7. Una **sala a pilastri** coperta
da un tetto a 4,5–6,2 m, **sei pilastri** 0,6 × 0,6 × 4,5 (x ±24, ±28, ±32 · z ±3,5) e al centro il
**banco del pesce** 2,4 × 2,4 × 1,0 (x ±26,8…±29,2 · z −1,2…1,2).
**Carattere.** Una tettoia in ferro e vetro su pilastri di ghisa (rivestiti in pietra fino a 1,2 m),
lucernari, ventilatori a pale fermi, lavagne dei prezzi, il banco del pesce in marmo con il ghiaccio.
È la rotazione del difensore: aperto ai due capi, chiuso ai lati. **Suolo**: piastrelle di graniglia
sbeccate, canalette di scolo.

{{T:Mercato coperto Est}}
{{T:Mercato coperto Ovest}}

## 13 · Cortili

**Est**: x 36…52 · z −9…9 (16 × 18 m), davanti all'ingresso principale della Grotta.
**Ovest**: x −52…−36 · z −9…9, davanti all'ingresso di servizio della Galleria.
**Ripari (blockout).** Un **pozzo** 4 × 4 × 0,9 (x ±42,5…±46,5 · z −2…2) con la **statua** (1,2 × 1,2,
fino a 2,6 m) di un pescatore con la rete, che taglia la linea dritta piazza → estrazione; una cassa
2,5 × 2,5 × 1; un muretto 2,4 × 0,6 × 1,1.
**Carattere.** Cortili di case a corte: pergolati, panche di pietra, una cappella, piante in vaso. Est
più marino (ancore, nasse), ovest più stradale (un vecchio cartello della statale).

{{T:Cortile Est}}
{{T:Cortile Ovest}}

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

{{T:Forte San Verano}}
{{T:Forte San Verano (uscita ovest)}}

## 17 · Salita del Forte, Largo del Forte, Arco Sud, Via del Mercato Sud *(ruotano la corsia centrale)*

Stessa sequenza del Nord, rovesciata: Salita (8 × 12 m) → **Largo del Forte** con la **fontana**
(qui una fontana a muro più rustica: vasca di pietra e un leone che butta acqua, stessa scatola) e la
**gradinata** a 1,53 m nell'angolo **sud-ovest**, che qui è il sagrato della **chiesa di San Verano**
(facciata in pietra con il campanile a vela) → **Arco Sud** (la porta del borgo, con lo stemma della
torre) → **Via del Mercato Sud** con il **carretto** (qui un carro a buoi senza buoi, carico di botti).
Botteghe dell'olio, salumeria, cestaio; tende color sabbia, corde di peperoncini.

{{T:Salita del Forte}}
{{T:Largo del Forte}}
{{T:Arco Sud}}
{{T:Via del Mercato Sud}}
{{T:Passaggio Est del Largo Sud}}
{{T:Passaggio Ovest del Largo Sud}}

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

{{T:Spalti}}
{{T:Porta delle Mura}}
{{T:Mura}}
{{T:Discesa alla Marina}}
{{T:Marina}}
{{T:Bastione della Marina}}

## 19 · Lungomare — corsia est del Sud verso la Grotta *(ruota la Via Stretta)*

**Lungomare** (x 20…35 · z 89…93, poi x 31…35 · z 72…93): un vicolo di 4 m fra **villini** con giardini
pensili, balaustre in pietra **sopra 1,1 m** (nessun volume a terra), bouganville secca. **Piazzetta del
Lungomare** (ruota la Piazzetta del Forno, x 30…58 · z 56…74): il forno diventa un **frantoio** (stessa
stanza, stesse porte), con la macina dipinta sul muro, le giare e l'insegna "OLIO". **Passaggio del
Piazzale Sud** → **Piazzale della Grotta** (ruota il Piazzale della Galleria, x 56…82 · z 30…44): il
camion diventa un **carro coperto** di legno (6 × 2,4 × 3), il new jersey un **blocco di pietra** da
ormeggio, e a nord si apre l'**ingresso sud della Grotta**. Deposito del sale, casa dei doganieri,
lazzaretto vecchio; cartello "GROTTA DEL LEVANTE" in ceramica.

{{T:Lungomare}}
{{T:Piazzetta del Lungomare}}
{{T:Passaggio del Piazzale Sud}}
{{T:Piazzale della Grotta}}

---

# Il resto del paese

## 20 · Il cuore degli isolati ({{N_INTERNI}} lotti)

I lotti che non danno su nessuna area giocabile ({{INTERNI_DETTAGLIO}}) si vedono **solo dall'alto e
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
