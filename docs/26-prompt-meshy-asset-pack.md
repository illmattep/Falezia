# Meshy — asset pack per Calanera (prompt pronti)

## BLOCCO DI STILE (da anteporre a OGNI prompt)

```
STYLE: stylized hand-painted Mediterranean town asset, early-1900s Italian coastal village,
flat painted colours, soft gouache look, minimal surface noise, clean silhouettes,
slightly chunky proportions, no photorealistic detail, no HDR look, no baked lighting,
no ambient occlusion baked into the texture, neutral daylight albedo only.
PALETTE: warm ochre, terracotta, sun-bleached cream, dusty rose, sage green,
bottle-green shutters, slate-blue shutters, iron grey, weathered stone.
TECH: single object, real-world scale in metres, pivot at the base centre, Y-up,
centred on XZ, no ground plane, no shadows, no background, no text, no logos, no people.
TEXTURES: 1024x1024 PBR (albedo + roughness + normal), albedo kept light and flat,
roughness mostly 0.75-0.95, metalness 0 except iron parts.
BUDGET: 800-4000 triangles per building element, 300-1500 per prop.
```

## BLOCCO DI USCITA (da anteporre a ogni prompt)

```
OUTPUT: one mesh per material group, separate objects allowed, UV unwrapped,
no overlapping UVs, textures embedded, export GLB.
```

---

## 1. Kit edilizio

| ID | Prompt | Misure |
|---|---|---|
| `casa_1f_ocra` | two-storey-less single floor Italian townhouse, plastered ochre walls, terracotta gable roof with eaves overhang, one wooden door with stone frame, three shuttered windows, stone plinth at the base | 6.4 × 7.0 × 5.2 m |
| `casa_1f_crema` | single floor cream plaster house, terracotta roof, two windows with slate-blue shutters, arched stone door surround | 7.0 × 7.6 × 5.2 m |
| `casa_1f_giallo` | single floor pale yellow plaster house, low terracotta roof, bottle-green shutters, stone doorstep | 6.8 × 7.2 × 5.2 m |
| `casa_bottega` | single floor corner shop house, plastered walls, wide shop window, striped fabric awning over the entrance, painted sign board above, terracotta roof | 9.4 × 7.4 × 5.2 m |
| `casa_2f_ocra` | two-storey ochre plaster townhouse, terracotta gable roof, stone cornice between floors, four shuttered windows, iron balcony on the first floor, wooden door | 8.0 × 8.0 × 8.3 m |
| `casa_2f_terra` | two-storey terracotta plaster house, slightly weathered walls, terracotta roof, two iron balconies, wooden shutters | 7.4 × 8.4 × 8.3 m |
| `casa_2f_crema` | two-storey cream plaster house, clean walls, slate-blue shutters, stone window sills, terracotta roof | 8.6 × 7.8 × 8.3 m |
| `casa_2f_rosa` | two-storey dusty rose plaster house, warm terracotta roof, dark shutters, small iron balcony | 7.8 × 8.2 × 8.3 m |
| `casa_2f_salvia` | two-storey sage green plaster house, cream stone details, terracotta roof, wooden shutters | 8.2 × 7.6 × 8.3 m |
| `casa_3f_ocra` | three-storey ochre plaster apartment house, terracotta roof, repeating shuttered windows, two iron balconies, stone plinth | 7.6 × 9.0 × 11.4 m |
| `casa_3f_terra` | three-storey terracotta plaster house, weathered, terracotta roof, three balconies with iron railings | 8.0 × 9.4 × 11.4 m |
| `casa_3f_rosa` | three-storey dusty rose house, stone quoins at the corners, terracotta roof | 7.4 × 8.8 × 11.4 m |
| `tetto_coppi` | terracotta gable roof section with ridge tiles and eaves, two slopes, slight sag, standalone roof piece | 8 × 8 × 2.4 m |
| `campanile` | Italian bell tower, square stone shaft, arched belfry openings on four sides, stone cornice, terracotta pyramid cap with iron cross | 4.2 × 4.2 × 19 m |
| `portico_campata` | one bay of a stone arcade: two stone piers, round arch, architrave, terracotta lean-to roof | 3.2 × 3.0 × 4.6 m |

## 2. Elementi di facciata (modulari)

| ID | Prompt | Misure |
|---|---|---|
| `porta_legno` | weathered wooden Italian house door, two panels, iron handle and hinges, stone frame surround with lintel and step | 1.28 × 0.2 × 2.5 m |
| `porta_bottega` | shop entrance: wooden door with glazed upper half, stone frame, small step | 1.4 × 0.2 × 2.6 m |
| `finestra_persiane` | window with stone sill, recessed reveal, two open wooden shutters, dark glass | 1.5 × 0.25 × 2.0 m |
| `finestra_arco` | arched window with stone surround and iron grille | 1.2 × 0.25 × 2.1 m |
| `balcone` | small stone balcony slab with three corbels and iron railing, painted dark | 2.0 × 1.0 × 1.1 m |
| `tenda_bottega` | striped fabric shop awning on two iron arms, ochre and oxide red stripes | 3.4 × 1.1 × 0.6 m |
| `insegna_bandiera` | hanging shop sign, painted wooden board on iron bracket, blank face | 1.1 × 0.6 × 0.15 m |
| `grondaia` | copper gutter with downpipe section, weathered | 3.0 × 0.2 × 0.3 m |

## 3. Suolo e collegamenti

| ID | Prompt | Misure |
|---|---|---|
| `marciapiede` | stone kerb and sidewalk section, light grey slabs, granite kerb edge | 6.0 × 1.6 × 0.2 m |
| `scala` | outdoor stone staircase, six steps, worn treads, low side walls | 2.4 × 3.2 × 1.2 m |
| `scala_larga` | wide stone stair, eight steps, for a public staircase between streets | 4.0 × 4.5 × 1.6 m |
| `lastricato_piazza` | plaza stone paving tile, large irregular slabs, subtle joints | 6 × 6 × 0.1 m |
| `sampietrini` | cobblestone street tile, rounded setts, warm grey | 6 × 6 × 0.1 m |
| `muro_forte` | fortress curtain wall in rough stone with merlons and a walkway on top | 8 × 1.1 × 5.5 m |
| `porta_forte` | fortress gate: stone arch with wooden doors, drawbridge chains, two flanking towers | 8 × 6 × 9 m |

## 4. Piazza

| ID | Prompt | Misure |
|---|---|---|
| `fontana` | octagonal stone fountain, moulded rim, central column with upper bowl, dark water | 3.1 × 3.1 × 1.2 m |
| `pozzo` | old stone well, round low wall, iron arch and bucket | 1.6 × 1.6 × 2.2 m |
| `panca` | wooden bench with iron legs, three slats, worn paint | 1.8 × 0.5 × 0.95 m |
| `lampione` | cast iron street lamp, fluted post, curved arm, glazed lantern | 0.4 × 0.4 × 3.8 m |
| `lampada_muro` | wall mounted lantern on an iron bracket, glazed body | 0.3 × 0.4 × 0.7 m |
| `vaso` | terracotta planter with a rounded bush | 0.7 × 0.7 × 1.3 m |
| `albero` | stylized Mediterranean tree, thick trunk, three rounded foliage masses | 2.6 × 2.6 × 5.0 m |
| `bancarella` | market stall: wooden counter, four posts, striped awning | 2.6 × 1.5 × 2.2 m |
| `cassa` | wooden crate with iron straps | 0.9 × 0.9 × 0.8 m |
| `barile` | wooden barrel with iron hoops | 0.7 × 0.7 × 0.95 m |
| `sacco` | burlap sack, slumped, tied top | 0.5 × 0.5 × 0.7 m |
| `carretto` | two-wheeled wooden hand cart, empty bed, iron-rimmed wheels | 2.4 × 1.2 × 1.2 m |
| `giornali` | stack of newspapers and a folded chair outside a shop | 0.6 × 0.5 × 0.5 m |

## 5. Porto

| ID | Prompt | Misure |
|---|---|---|
| `gru` | lattice steel harbour crane, four legs, horizontal jib, hook and cable, rusty paint | 3.5 × 11 × 12 m |
| `container` | corrugated shipping container, faded blue paint, closed doors | 2.4 × 6.0 × 2.6 m |
| `bitta` | cast iron mooring bollard, mushroom head, worn | 0.5 × 0.5 × 0.7 m |
| `boa` | floating mooring buoy, striped paint, rope loop | 0.6 × 0.6 × 0.8 m |
| `cima` | coiled mooring rope on the quay | 0.8 × 0.8 × 0.2 m |
| `rete` | folded fishing net pile with floats | 1.4 × 1.0 × 0.4 m |
| `barca` | small wooden fishing boat, empty, oars stowed, weathered paint | 5.0 × 1.8 × 1.2 m |
| `capannone` | harbour warehouse: corrugated metal walls, low gable roof, big sliding door | 18 × 11 × 4.5 m |
| `canna_fumo` | rusty harbour smokestack, banded | 1.0 × 1.0 × 8 m |

## 6. Forte

| ID | Prompt | Misure |
|---|---|---|
| `merlo` | single stone merlon block with slight taper | 0.7 × 0.6 × 1.0 m |
| `torre_forte` | squat round fortress tower, rough stone, slit windows, crenellated top | 5 × 5 × 10 m |
| `braciere` | iron brazier on a tripod, cold ash inside | 0.9 × 0.9 × 1.6 m |
| `bandiera` | torn cloth banner on a pole, plain, weathered | 1.6 × 0.1 × 3.0 m |
| `cannone` | old iron cannon on a wooden carriage | 2.6 × 1.2 × 1.3 m |
| `casse_munizioni` | stack of three wooden ammunition crates | 1.2 × 0.8 × 0.9 m |

## 7. Vicoli e dettagli

| ID | Prompt | Misure |
|---|---|---|
| `panni` | laundry line with hanging clothes, rope and pegs | 4.0 × 0.6 × 1.0 m |
| `vasi_finestra` | two small terracotta pots with herbs on a window sill | 0.8 × 0.3 × 0.4 m |
| `targa` | small painted street name plate, blank face | 0.5 × 0.05 × 0.25 m |
| `rifiuti` | overflowing rubbish bin with two bags beside it | 0.8 × 0.8 × 1.1 m |
| `pozzanghera` | shallow irregular puddle, dark reflective water, flat | 1.6 × 1.6 × 0.02 m |
| `graffiti` | painted wall graffiti panel, abstract, single flat colour | 1.6 × 0.05 × 0.9 m |
| `cancello` | wrought iron gate, two leaves, closed | 2.0 × 0.1 × 2.2 m |
| `concio` | loose fallen stone block beside a wall | 0.6 × 0.5 × 0.4 m |
| `gatto_pietra` | small carved stone cat statue on a plinth with a tiny plaque | 0.5 × 0.4 × 0.6 m |

---

## Regole di import nel progetto

1. Export **GLB**, scala 1 unità = 1 metro, pivot al centro della base, Y-up.
2. Un file per asset, nome = ID della tabella (`casa_2f_ocra.glb`, `fontana.glb`, …).
3. Nel layout (`Assets/StreamingAssets/map/props.json`) si aggiunge una riga per ogni istanza:
   `{"model":"map/kit/<ID>", "x":…, "y":0, "z":…, "rot":…, "scale":1}`.
4. Non serve alcun codice nuovo: `MapProps` carica, `LevelKit` mette i collider.

## Prompt negativi (nel campo "negative prompt")

```
photorealistic, PBR photo textures, dirt decals, baked shadows, ambient occlusion,
text, letters, logos, watermark, people, animals except the stone cat, vehicles,
ground plane, sky, background, blurry, low contrast, neon colours, sci-fi, modern
street furniture, glass skyscraper, oversaturated, noisy texture
```
