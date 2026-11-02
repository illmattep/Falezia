# 19 · Asset 3D della mappa — cosa serve davvero a Calanera

> Analisi fatta sui dati veri (`game/maps/calanera.json`, 671 volumi + 1200 decor) e sul
> generatore (`tools/mapgen/calanera.py`). L'elenco completo e leggibile da macchina è in
> `art/map-assets/assets.json`; l'elenco **edificio per edificio** è in
> `art/map-assets/buildings.csv` (358 righe).

---

## 0 · Il fatto che cambia tutto

Calanera **non è fatta di edifici separati**. Il generatore riempie tutto ciò che non è
strada con una massa edilizia continua, poi la taglia in lotti ≤ 10 m (`fill_blocks`).
Il risultato è che le facciate esposte sono **2.528 metri lineari per piano** e i "palazzi"
condividono i muri: non esiste il "palazzo A" da modellare, esiste **un fronte strada** da
rivestire.

Quindi il piano non è "88 palazzi", è:

| Cosa | Quantità misurata |
|---|---|
| Superficie edificata | **21.148 m²** (su 36.000 m² di mappa) |
| Lotti edilizi (≤ 10 m) | **358**, di cui **218 con facciata esposta** e 140 interni (nessuna arte) |
| Altezze | 6,2 m (114 lotti, 2 piani) · 9,0 m (124, 3 piani) · 11,8 m (120, 4 piani) |
| Facciata esposta per piano | **2.528 m** |
| Facciata pesata per piano | **7.610 m** → ~2.537 moduli da 3 m se si riveste tutto in 3D |
| Caverne | Grotta 20×24×6 m · Galleria 12×60×6 m |
| Prop già presenti (volumi bassi) | 152 (casse, container, camion, barche, muretti…) |

Rivestire 2.537 moduli in 3D è possibile, ma è il 90% del lavoro artistico della mappa.
Per questo il documento propone **due percorsi** (sezione 3) e consiglia il primo.

---

## 1 · Com'è distribuita la città

Zone ricavate dai 16 punti nominati della mappa (attribuzione al punto più vicino, quindi
approssimata sui confini):

| Zona | Lotti con facciata | Facciata (m, un piano) | Carattere |
|---|---:|---:|---|
| MURA | 45 | 512 | fortezza, pietra, bastioni |
| PORTO | 38 | 452 | industriale, container, gru |
| VIA_STRETTA | 32 | 320 | vicoli, forno, intonaco |
| LUNGOMARE | 32 | 315 | balneare, teli, ombrelloni |
| GALLERIA | 16 | 203 | tunnel dismesso, cemento |
| PIAZZA | 17 | 190 | civico, porticato, cassa |
| MERCATO | 16 | 168 | banchi, tende, cassette |
| SCALINATA | 13 | 129 | gradoni, muretti |
| GROTTA | 9 | 114 | roccia naturale |

I lotti per altezza: 6,2 m × 114 · 9,0 m × 124 · 11,8 m × 120.

> **Dove guarda il gioco:** le zone si vedono in sequenza, non tutte insieme. Un giocatore
> passa il 70% del tempo tra Piazza, Mercato, Via Stretta e le due estrazioni. La cura
> artistica va concentrata lì; Mura e Lungomare possono restare a densità più bassa.

---

## 2 · Cosa manca oggi (graybox ≠ brutto, graybox = vuoto)

Oggi la mappa è volumi colorati con texture tileable. Mancano, in ordine di impatto visivo:

1. **Facciate**: nessuna finestra, nessuna porta, nessun balcone, nessuna grondaia. Sono 2.528 m.
2. **Quota strada**: nessun arredo. Le strade sono lastricato nudo.
3. **Tetti**: ci sono lastre e comignoli come scatole; nessun coppo, nessuna antenna decente.
4. **Caverne**: rettangoli con facce piane e angoli a 90°. È il punto più debole della mappa.
5. **Obiettivo**: cassa, Nucleo e faro non hanno modello.
6. **Segni di vita**: insegne, manifesti, panni stesi, biciclette, motorini. È ciò che
   trasforma "un posto" in "un posto dove vive qualcuno".

---

## 3 · Facciate: due percorsi, e quale consiglio

### Percorso A — materiali + accenti 3D *(consigliato)*

| Strato | Cosa | Costo |
|---|---|---|
| Base | 6 materiali facciata tileable (intonaco chiaro, ocra, pietra, cemento, mattone, intonaco scrostato) con albedo + normal + ORM | 6 texture, zero mesh |
| Accenti a terra | moduli 3D solo al piano terra: portoni, negozi, basculanti, cantonali | ~840 moduli |
| Accenti ai piani | balconi + persiane solo sul 1° piano, dove l'occhio arriva | ~400 moduli |
| Cornici | cornice di gronda su tutti i fronti | ~840 segmenti da 3 m |

Risultato: l'80% dell'effetto con il 25% delle mesh. Le facciate sopra il 1° piano restano
texture + cornice, che a 20 m di distanza è indistinguibile.

### Percorso B — kit modulare completo

2.537 moduli da 3 × 3 m. Serve se un giorno la mappa si gioca anche a 3 m dalle pareti o
in spettatore. Costo: 12 moduli × ~170 istanze.

**Consiglio: A adesso, B solo su Piazza, Mercato e Via Stretta** (le tre zone dove si
combatte davvero). Il resto della città resta A.

### I moduli (kit comune ai due percorsi)

| ID | Modulo | Dimensioni (m) | Varianti |
|---|---|---:|---:|
| `facade_plaster_plain` | intonaco liscio | 3,0 × 3,0 × 0,20 | 3 |
| `facade_stone_plain` | pietra a vista | 3,0 × 3,0 × 0,20 | 2 |
| `facade_window_shuttered` | finestra + persiane | 3,0 × 3,0 × 0,22 | 4 |
| `facade_window_balcony` | finestra + balconcino | 3,0 × 3,0 × 0,55 | 3 |
| `facade_arch_door` | portone ad arco | 3,0 × 3,0 × 0,30 | 3 |
| `facade_shop_front` | negozio con tenda | 3,0 × 3,0 × 0,45 | 4 |
| `facade_garage_shutter` | basculante | 3,0 × 3,0 × 0,15 | 2 |
| `facade_corner_pilaster` | cantonale | 0,6 × 3,0 × 0,6 | 2 |
| `facade_cornice` | cornice di gronda | 3,0 × 0,35 × 0,45 | 3 |
| `facade_downpipe` | pluviale | 0,12 × 6,0 × 0,12 | 2 |
| `facade_parapet` | parapetto terrazza | 3,0 × 1,0 × 0,25 | 3 |

**Regola di applicazione:** un modulo occupa sempre 3 m e si aggancia alla griglia del lotto;
il portone va solo a piano terra e solo su lati che danno su spazio aperto (colonna `sides`
del CSV); il balcone solo dal 1° piano.

---

## 4 · Gli edifici, uno per uno

`art/map-assets/buildings.csv` ha una riga per ognuno dei **358 lotti** (140 hanno
`facade_m = 0`: sono lotti interni, senza nessuna superficie visibile — non richiedono arte,
servono solo a completare l'inventario):

Facciata media per lotto visibile: **11,0 m**, cioè ~4 moduli da 3 m. Lotti su un solo lato:
128 · su due lati: 79 · su tre lati (angolari): 11.

```
id, zone, x0, z0, w, d, h, floors, material, sides, facade_m, ground
L001, MURA, -90, -100, 10.0, 9.4, 9.0, 3, stone, NE, 19.4, portone
```

- `x0, z0` = angolo minimo in metri (stesse coordinate della mappa e della simulazione)
- `w, d, h` = larghezza, profondità, altezza
- `sides` = lati esposti (N/S/O/E): solo quelli ricevono facciata
- `facade_m` = metri di facciata di quel lotto
- `ground` = suggerimento per il piano terra (negozio nelle zone commerciali)

Serve a **generare le istanze automaticamente**: ogni lato esposto → una fila di moduli da 3 m,
gli avanzi si chiudono con `facade_plaster_plain` tagliato.

### Kit di varianti per non avere la città ripetuta

Con 358 lotti e 2.537 moduli, la ripetizione è il rischio numero uno. Regole:

| Regola | Valore |
|---|---|
| Varianti minime per tipo di modulo | **3** (finestra 4) |
| Due moduli identici adiacenti | **mai** |
| Stessa variante nella stessa facciata | max 2 volte |
| Palette per zona | 3 combinazioni (chiaro/ocra/pietra) assegnate per isolato, non per lotto |
| Rotazione/ribaltamento | ammessi (i moduli sono specchiati), **non** lo scaling |
| Dettagli "unici" | 1 su 20 moduli: porta con numero civico, balcone con panni, targa |

---

## 5 · Prop: la lista (88 asset in `assets.json`)

Dimensioni reali, quelle che il gioco si aspetta. Tutte ≤ 3.000 triangoli (doc 07).

### Strada e arredo (18)
cordolo 1×0,16×0,30 · tombino Ø0,6 · griglia 0,5 · lampada a muro 0,5 · lampione 4,2 m ·
paracarri 0,9 · panchina 1,8 · fioriera 1,0 · cestino 0,9 · fontanella 1,1 · edicola 2,4×1,6×2,6 ·
cabina elettrica 1,4 · bicicletta 1,8 · motorino 1,7 · cartello 2,2 · targa via 0,5 ·
fascio di cavi 6 m · pozzanghera (decal 1,6)

### Mercato (9)
banco 2,4×1,2 · tenda 3×2 · cassetta 0,5 · cassetta con prodotti (6 varianti: pomodori, limoni,
melanzane, pesce…) · barile 0,85 · bilancia · cassetta frigorifera 0,8 · filo di lampadine 4 m · lavagnetta

### Porto (9)
bitta 0,55 · parabordo · cima avvolta · reti da pesca 1,8 · boa · barca a remi 3,6 m ·
pallet 1,2×0,8 · container 6 m · gancio della gru

### Natura (8)
pino marittimo 9 m · olivo 4,5 · macchia 1,2 · agave 1,0 · vaso con pianta 0,7 ·
edera (card) · ciuffo d'erba (card) · roccia 1,6

### Caverne (16) — vedi sezione 6

### Tetti (6)
filo di coppi 3 m · comignolo 0,62×1,37 · corpo scala 2,6×2,3×2,6 · serbatoio 1,5 ·
antenna 2,6 · parabola

### Obiettivo (7)
cassa 1,2×1,0×0,8 · Nucleo 0,5×0,35×0,4 · faro 2,5 m · piastra 4×4 (decal) ·
kit decodifica · robot · sirena

### Insegne (4)
insegna negozio 1,6 · manifesto · graffiti · numeri civici

---

## 6 · Le caverne: come togliere l'effetto "blocchi" senza spostare niente

### Com'è adesso (misure esatte)

| Elemento | Volume scavato | Note |
|---|---|---|
| **Grotta del Levante** — camera | 20 × 24 m, altezza 6 m (x 66–86, z −12…12) | 3 ingressi |
| ingresso frontale | corridoio 14 m × 3 m, altezza 3 m (x 52–66, z −1,5…1,5) | dal lato ovest |
| ingressi laterali | corridoi 3,4 m larghi, altezza 3 m, a gomito | da Molo e dal suo ruotato |
| **Galleria Vecchia** — tunnel | 12 × 60 m, altezza 6 m (x −78…−66, z −30…30) | 2 nicchie 4×4×3 |
| ingresso di servizio | 14 × 4 m, altezza 3 m | dal Cortile Ovest |

### Il vincolo che comanda tutto

La collisione **non è il modello**: la simulazione conosce solo gli AABB di `boxes`.
Quindi possiamo scolpire la roccia quanto vogliamo, a due condizioni:

1. **Fuori, il profilo dall'alto non cambia.** Nessuna superficie può uscire dal volume
   scavato; l'unica aggiunta ammessa all'esterno sta dentro il rettangolo della regione
   (Grotta: x 52–90, z −28…28 · Galleria: x −90…−62, z −30…30).
2. **Sotto 2,2 m di quota, la superficie coincide col volume di collisione.** Il giocatore
   è alto 1,8 m: se la roccia rientra a quota petto, il corpo atraversa la pietra. Sopra
   i 2,2 m invece si può rientrare quanto si vuole.

> In una riga: **la curva è tutta in alto e negli angoli; a terra resta il volume esatto.**

### Cosa generare

| ID | Modulo | Dimensioni (m) | Come si usa |
|---|---|---:|---|
| `cave_vault_straight` | volta dritta | 3 × 3 × 6 | corridoi e ingresso frontale; sezione a tutto sesto, spalle a 2,2 m |
| `cave_vault_curve` | volta curva (15°/30°/45°) | 3 × 3 × 6 | sostituisce i gomiti a 90° degli ingressi laterali, raggio interno **1,5 m** |
| `cave_portal` | portale naturale | 4 × 3,6 × 0,8 | cornice irregolare attorno a ogni ingresso (3 e 3,4 m) |
| `cave_wall_panel` | parete camera | 8 × 6 × 1,5 | 11 pannelli per il perimetro della Grotta (88 m) |
| `cave_ceiling_panel` | soffitto camera | 8 × 8 × 2 | 8 pannelli per i 480 m² della camera |
| `cave_floor_patch` | pavimento | 6 × 6 × 0,25 | 13 moduli; 3 varianti (ghiaia, sabbia, pozza) |
| `cave_boulder` / `rubble` / `stalagmite` / `stalactites` | dettagli | 0,8–2,2 m | 24 + 20 + 16 + 14 istanze sparse |
| `galleria_lining` | rivestimento cemento | 12 × 6 × 6 | 10 segmenti per i 60 m della Galleria |
| `galleria_niche` / `vent` / `lamp` | dettagli tunnel | 4×3×1,2 · 1,2² · 0,6 | 2 + 4 + 16 |

### Prima → dopo

| Zona | Adesso | Dopo |
|---|---|---|
| Ingressi Grotta | tubo squadrato, 4 spigoli vivi | volta a botte, gomiti con R 1,5 m, portale irregolare, detriti che nascondono lo spigolo |
| Camera Grotta | stanza 20×24 con pareti piane e soffitto piatto a 6 m | 11 pannelli concavi con rilievo 0,4–0,8 m (tutti rientranti), soffitto a cupola con stalattiti, 3 pozze |
| Galleria | tubo 12×60 di cemento piatto | 10 conci ad arco in cemento macchiato, nicchie, passacavi, 16 lampade, detriti ai bordi |
| Silhouette esterna | scatole di roccia visibili dall'alto | stessa impronta, ma i bordi superiori sono roccia irregolare **dentro** la regione |

### Come si aggancia al codice

I moduli sono **decorativi**: si aggiungono come decor (come già fa `roofscape`), non come
`boxes`. Zero modifiche alla simulazione, zero rischio sui test. La sostituzione dei volumi
`rock` con questi moduli è un lavoro del client (`MapBuilder`/`MapDressing`).

---

## 7 · Pipeline Meshy

> **Pronto all'uso:** `art/map-assets/MESHY.md` contiene gli **88 passi numerati** in ordine di
> produzione, ognuno con prompt copia-incolla, dimensioni, budget, varianti e controllo qualità.
> Questa sezione spiega il perché di quei numeri.

| Parametro | Valore |
|---|---|
| Formato | GLB, Y-up, scala reale in metri |
| Pivot | base, centro dell'ingombro |
| Texture | 1024 (prop) · 2048 (facciate, caverne) |
| Materiali | PBR separati: albedo, normal, ORM. **Niente luce cotta** |
| Triangoli | prop ≤ 3.000 · facciata ≤ 1.500 · caverna ≤ 5.000 |
| LOD | 3 stadi per prop > 3.000 vertici e per ogni modulo caverna |
| Nome | `map/<categoria>/<id>_v<n>.glb` |
| Prompt base | *"Game-ready low poly asset, PBR, neutral studio lighting, no baked shadow, single object, real-world scale in meters: …"* |

### Prompt d'esempio (i tre che partirei a fare)

**Cassa obiettivo**
> Heavy industrial objective chest, 1.2 m wide, 1.0 m tall, 0.8 m deep, riveted steel with
> worn yellow-black stripes, hinged lid open at 60°, empty interior, game-ready low poly,
> PBR, neutral studio lighting, real-world scale, single object, GLB.

**Finestra con persiane**
> Italian window with open wooden shutters, 1.0 × 1.4 m opening recessed in a 3 × 3 m
> weathered ochre plaster wall panel, stone sill, game-ready low poly, PBR, real-world
> scale, single object.

**Volta di caverna**
> Cave tunnel segment, 6 m long, 3 m wide, 3 m high, smooth eroded limestone barrel vault,
> no sharp box edges, damp floor, game-ready low poly, PBR, real-world scale, single object.

### Controllo all'import (5 minuti, evita di rifare tutto)

1. **Scala**: la finestra deve misurare 1,0 × 1,4 m sull'apertura, non sull'ingombro.
2. **Pivot**: alla base, al centro. Se è al centro geometrico, i prop affondano di metà altezza.
3. **Orientamento**: +Z avanti, Y su. Meshy a volte esporta Z-up.
4. **Triangoli**: contati, non stimati.
5. **Materiali**: nessuna texture sopra 2048, nessuna luce cotta nell'albedo.
6. **Niente testo generato** sulle insegne: il testo lo mette Unity, non Meshy.

---

## 8 · In che ordine (tre sprint)

**Sprint 1 — si vede subito (≈ 30 asset):**
cassa, Nucleo, faro · 4 moduli facciata (finestra, portone, negozio, intonaco) · cornice ·
coppi, comignolo · 8 prop strada (cordolo, tombino, lampada, paracarro, panchina, fioriera,
bicicletta, motorino) · 2 nature (pino, macchia) · portale caverna.

**Sprint 2 — le due estrazioni (≈ 25 asset):**
tutto il kit caverna (16) · galleria (3) · dettagli porto (bitta, reti, boa, barca) ·
segnaletica.

**Sprint 3 — il resto della città (≈ 30 asset):**
mercato completo · balconi · tetti (serbatoio, antenna, corpo scala) · natura rimanente ·
decal e manifesti.

---

## 9 · Cosa **non** serve

- **Edifici interi**: non esistono come oggetti. Solo facciate.
- **Interni**: la mappa è chiusa, non si entra in nessuna casa (a parte il forno, che è già un volume).
- **Varianti distrutte**: la distruzione è esclusa dal progetto.
- **Prop con collisione**: tutto ciò che è più alto di 35 cm e sta in mezzo alla strada
  richiederebbe di aggiornare gli AABB e le rotte verificate. I prop vanno **addossati ai muri**
  o dentro le zone già occupate (casse, container).
- **Texture di mappa nuove**: ci sono già (`Assets/Resources/MapTex`), servono solo i materiali
  facciata.
