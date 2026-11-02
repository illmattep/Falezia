# Armi da Meshy — versione pronta per il gioco

Quattordici modelli generati con Meshy (copie da `~/Scaricati`, originali non toccati), portati nel formato delle
armi di FALEZIA e **in uso nel client Unity** al posto di quelli di arsenal2 (`Assets/StreamingAssets/weapons/<id>/`,
più le miniature dello shop in `ui/<id>.png`). Gli originali di arsenal2 sono salvati in
`art/weapons/arsenal2_backup/`, fuori da StreamingAssets (non finiscono nella build).

| Arma | Viewmodel | Terza persona | Misure (L×A×P mm) | Doc 12 | Pezzi mobili |
|---|---|---|---|---|---|
| standard | 7.950 tri | 1.484 | 197 × 143 × 29 | 197 × 138 × 33 | slide, mag (fondello) |
| hush | 7.916 | 1.488 | 250 × 119 × 38 | 250 × 145 × 47 | slide, mag (fondello) |
| snub | 7.994 | 1.496 | 165 × 109 × 35 | 165 × 125 × 38 | — |
| hammer | 7.997 | 1.500 | 270 × 137 × 34 | 270 × 165 × 45 | slide, mag (fondello) |
| chatter | 7.995 | 1.498 | 210 × 171 × 28 | 210 × 195 × 35 | slide, mag (parte sporgente) |
| blitz | 7.978 | 1.486 | 500 × 214 × 44 | 500 × 220 × 58 | — |
| cadence | 7.982 | 1.486 | 679 × 301 × 82 | 680 × 260 × 60 | mag (banana intera) |
| scrap | 7.998 | 1.500 | 550 × 292 × 58 | 550 × 240 × 52 | mag (parte sporgente) |
| choke | 7.997 | 1.496 | 1150 × 247 × 63 | 1150 × 190 × 55 | pump (astina) |
| sweeper | 7.971 | 1.484 | 950 × 349 × 74 | 950 × 250 × 62 | mag |
| anvil | 7.998 | 1.496 | 879 × 261 × 73 | 880 × 250 × 58 | mag (banana) |
| service | 7.999 | 1.496 | 850 × 248 × 70 | 850 × 245 × 60 | mag |
| spotter | 7.998 | 1.500 | 1225 × 377 × 91 | 1225 × 250 × 62 | mag |
| longshot | 7.994 | 1.494 | 1399 × 298 × 126 | 1400 × 260 × 70 | mag |

Tutti passano `pipeline/validate.py` (`review/validazione.txt`). La **lunghezza** è esatta. Altezza e
larghezza sono quelle che Meshy ha dato all'arma: la scala è uniforme, quindi non si deformano. Per
Cadence, Scrap e Choke l'altezza in più è soprattutto la cinghia o il calcio.

## Tornare indietro (o rimetterle)

```bash
cd art/weapons/meshy
bash usa.sh originale standard   # rimette il modello di arsenal2 (anche: tutte)
bash usa.sh meshy standard       # di nuovo Meshy
```

Copia `.glb`, `.json` e miniatura.

## Formato (lo stesso che si aspetta `WeaponView` di Unity)

- **Radice** `<id>` con trasformata identità (niente posa di "draw" cotta nella radice).
- **Figli:** `body` più i pezzi mobili (`slide` / `mag` / `pump`) e i socket `socket_muzzle`, `socket_aim`,
  `socket_eye`, `socket_eject`, `socket_hand_r` e `socket_hand_l`.
- **Assi e misure:** volata verso −Z, alto +Y, destra dell'arma +X, metri.
- **Origine:** quella di arsenal2. `align.py` sposta il modello in modo che `socket_hand_r` cada nello
  stesso punto dell'arma che sostituisce, così la posa da fianco di `WeaponView` (tarata su arsenal2) non cambia.
- **Socket dell'occhio:** `socket_eye` sta sulla linea di mira, 16 cm dietro la tacca.
- **Materiale:** uno solo, `<id>_mat`, con texture 2048 per colore, ORM glTF (G = rugosità, B = metallo)
  e normal. Le tangenti sono nel file.
- **Clip:** `idle`, `fire`, `fire_last`, `reload`, `reload_empty`, `draw` e `holster`, con le stesse durate
  dei JSON di arsenal2. Movimenti:
  - carrello indietro di 28–34 mm;
  - caricatore che scende e torna;
  - pompa avanti e indietro;
  - draw/holster ruotando la radice.

  Sono clip semplici, da migliorare.
- **Terza persona:** `<id>_tp.glb`, una mesh, texture 512, niente clip.
- **Scheda:** `<id>.json` con misure, socket, triangoli e durate.

## Il processo (`pipeline/`)

1. **Analisi** (`glbinfo.py`, `views.py`, `grid.py`, `comps.py`). Tutti i file Meshy sono:
   - una mesh unica fusa da 177 k a 1,3 M triangoli;
   - normalizzati a 1,9 m;
   - volata verso −X;
   - texture 4096–8192 JPEG.

   I punti (impugnatura, volata, mira, espulsione, mano sinistra, tagli dei pezzi) sono scelti a mano sui
   profili con griglia: stanno in `CFG` dentro `build.py`.
2. **Pezzi** tagliati per regioni del profilo. La cinghia della Cadence è esclusa dal taglio in base al
   colore.
3. **Scala, rotazione e origine.**
4. **Semplificazione** solo della geometria (gltfpack), poi **nuovo atlante UV** (xatlas, `atlas.cpp`) e
   **bake** dalla mesh originale (`bake.py`): colore, ORM e normal ricalcolata nello spazio tangente
   nuovo, con dilatazione dei bordi.

   Il bake serve perché le UV di Meshy sono spezzate in migliaia di isole: semplificando *con* quelle
   UV le texture venivano "a macchie". Distanza mediana fra mesh leggera e originale: 0,1–0,2 mm.
5. **`post.py`**: `socket_eye`, durata esatta delle clip e scheda JSON. Poi **`align.py`** (origine come arsenal2).
6. **`validate.py`, poi `render.sh` e `sheet.py`**: tavola per arma con le viste del doc 12, barra da
   100 mm, pezzi mossi, prima persona, silhouette e sfocatura 8 px. `collective.py` produce il foglio
   collettivo alla stessa scala. Le miniature dello shop (512×256, profilo destro, sfondo trasparente) le fa
   `thumb_proj/thumb.gd` con Godot.

Per rilanciarla su un'altra macchina servono:

- `gltfpack`: `git clone github.com/zeux/meshoptimizer && make gltfpack`;
- lo strumento atlante: `g++ -O2 atlas.cpp xatlas.cpp`, con xatlas da github.com/jpcy/xatlas;
- numpy, scipy e Pillow;
- Godot 4 per le tavole.

I percorsi in cima a `bake.py` e `render.sh` vanno adattati.

## Cosa non torna col doc 12 (sono limiti dei modelli Meshy, non della conversione)

| Arma | Problema |
|---|---|
| Hush | manca la **canna scoperta di 15 mm** fra carrello e soppressore (è "la cosa che deve leggere" nella vista 3/4) |
| Blitz | polimero **beige**, non grigio-verde `#3E4441`; il doc dice che è l'unica arma dove il colore conta più della forma |
| Snub | brunitura **blu**, non bruna `#56483C` |
| Scrap | il retro della culatta è **aperto** (un tubo vuoto): in prima persona ci guardi dentro |
| Cadence, Choke | la cinghia pende **fissa** sotto l'arma: in prima persona si vede e non oscilla |
| Choke | è a canna basculante, il pezzo `pump` è solo l'astina che scorre: per una pompa vera il modello andrebbe rifatto |
| Pistole | `mag` è solo il **fondello**: il caricatore vero è fuso dentro l'impugnatura e non si può estrarre |
| Sweeper | cinghia pendente fissa (come Cadence e Choke): metà dell'altezza in più è la cinghia |
| Service e Spotter | sono **due AR-15** (Spotter con ottica e impugnatura anteriore): nel foglio delle silhouette si somigliano, il doc 12 chiede forme diverse |
| Anvil | legno arancione lucido: il doc 12 vieta il legno lucido |
| Longshot | l'otturatore è fuso, niente animazione dell'otturatore (solo caricatore) |
| Tutte | il taglio dei pezzi è netto: col carrello indietro si vede il bordo del taglio, senza superfici interne |

Le tre armi senza versione Meshy (Vantage, Heavy, coltello) restano quelle di arsenal2.
