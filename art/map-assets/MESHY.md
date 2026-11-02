# Meshy — Calanera, passo per passo

> 88 asset, 219 varianti. Questo file è la **ricetta operativa**: le impostazioni si mettono
> una volta, poi si scende per i passi nell'ordine dato. I prompt sono in inglese (Meshy rende
> meglio), il resto è in italiano.
>
> **Sprint 1** = ciò che si vede nei primi 30 secondi di gioco: fallo tutto prima di toccare il
> resto. **Sprint 2** = le due estrazioni. **Sprint 3** = il completamento della città.

---

## 1 · Impostazioni, una volta sola

| Campo Meshy | Valore |
|---|---|
| Versione modello | la più recente disponibile |
| Stile | **Realistic** (non Sculpture, non Cartoon) |
| Topologia | **Quad** per manufatti (facciate, prop, obiettivo) · **Triangle + auto-remesh** per roccia e vegetazione |
| Polycount target | quello del passo (è già il tetto del doc 07) |
| Simmetria | **Off**, salvo indicazione |
| Texture | PBR **on** · dimensione del passo · **Remove lighting: on** |
| UV | automatiche |
| Prompt negativo | vedi sotto, uguale per tutti |

### Blocco di stile (da anteporre a ogni prompt)

```text
Photoreal game asset for a Mediterranean coastal town, Italian Riviera, 1960s-80s, weathered and sun-bleached, muted palette, matte surfaces, no sci-fi elements.
```

### Prompt negativo (uguale per tutti)

```text
text, letters, logo, watermark, background, ground plane, cast shadow, baked lighting, multiple objects, floating parts, cartoon, stylized, low resolution texture, blurry, extra parts, human figure
```

> Se esce un basamento, un'ombra o un fondale: **scarta e rigenera**. Un asset col terreno
> sotto è inutilizzabile in mappa.

---

## 2 · Regole di variante

Le varianti non si riscrivono: si tiene lo **stesso prompt** e si aggiunge in coda il
modificatore del passo. Così restano lo stesso oggetto e diversi come esemplari — che è
esattamente ciò che evita la città ripetuta.

| Categoria | Modificatori |
|---|---|
| facade | with the shutters closed and weathered green paint · with peeling plaster and exposed stone patches · in a different palette: pale sand plaster and dark brown wood … |
| street | rusted and slightly dented · with chipped dark green paint · with faded posters and stickers … |
| market | filled with tomatoes and basil · filled with lemons and oranges · filled with aubergines and peppers … |
| harbour | heavily rusted, orange-brown · faded blue paint, peeling · with salt crust and old rope … |
| nature | dry summer condition, dusty leaves · wind-bent, grown toward the sea · with exposed roots and rough bark … |
| cave | wetter, with dark damp streaks · with moss and green algae patches · with calcite flowstone bands … |
| roof | with moss and lichen · newer, clean terracotta · with broken and missing tiles … |
| objective | worn and scratched · newer, factory finish · with stencil markings … |
| signage | faded and scratched · torn at one corner · in a different colour scheme … |

---

## 3 · Controllo qualità dopo ogni batch

1. **Scala**: misura l'apertura, non l'ingombro (finestra 1,0 × 1,4 m dentro un muro 3 × 3 m).
2. **Pivot**: alla base, al centro. Se è al centro geometrico, il prop affonda di metà altezza.
3. **Orientamento**: +Z avanti, **Y su** (Meshy a volte esporta Z-up).
4. **Triangoli**: contati, non stimati.
5. **Texture**: ≤ 2048, nessuna luce cotta nell'albedo, nessun testo.
6. **Pulizia**: nessun piano d'appoggio, nessuna parte staccata.

Se un asset non passa 1 o 2 si sistema all'import, non si rigenera.

---

## 4 · I passi


## SPRINT 1 — si vede subito

### 1. `objective_chest` — Cassa (obiettivo)

- **Dimensioni**: 1.2 × 1.0 × 0.8 m · **triangoli max**: 2500 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~1
- **Salva come**: `map/objective/objective_chest_v1.glb`

```text
Heavy metal objective chest, 1.2 m wide, riveted, industrial, with open lid state, game-ready, two states
```

### 2. `objective_core` — Nucleo

- **Dimensioni**: 0.5 × 0.35 × 0.4 m · **triangoli max**: 1800 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~1
- **Salva come**: `map/objective/objective_core_v1.glb`

```text
Reinforced briefcase containing a glowing core, 0.5 m, sci-fi industrial, game-ready, emissive part
```

### 3. `objective_beacon` — Faro di estrazione

- **Dimensioni**: 0.8 × 2.5 × 0.8 m · **triangoli max**: 1500 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~2
- **Salva come**: `map/objective/objective_beacon_v1.glb`

```text
Extraction beacon device, 2.5 m tall, tripod base, smoke emitter on top, emissive, game-ready
```

### 4. `objective_pad` — Piastra di estrazione

- **Dimensioni**: 4.0 × 0.05 × 4.0 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~2
- **Salva come**: `map/objective/objective_pad_v1.glb`

```text
Painted extraction pad decal, 4x4 m, worn industrial markings, game-ready
```

### 5. `facade_window_shuttered` — Finestra con persiane

- **Dimensioni**: 3.0 × 3.0 × 0.22 m · **triangoli max**: 1100 · **texture**: 2048
- **Varianti**: 4 · **istanze in mappa**: ~520
- **Salva come**: `map/facade/facade_window_shuttered_v1.glb`

```text
Italian window with open wooden shutters, 1.0 x 1.4 m recessed opening in 3x3 m plaster wall panel, stone sill, green or brown shutters, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_window_shuttered_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_window_shuttered_v3.glb`
4. `in a different palette: pale sand plaster and dark brown wood` → `facade_window_shuttered_v4.glb`

### 6. `facade_arch_door` — Portone ad arco

- **Dimensioni**: 3.0 × 3.0 × 0.3 m · **triangoli max**: 1400 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~180
- **Salva come**: `map/facade/facade_arch_door_v1.glb`

```text
Arched wooden double door, 1.6 m wide 2.6 m tall, recessed 0.25 m in plaster wall, stone arch surround, weathered, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_arch_door_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_arch_door_v3.glb`

### 7. `facade_shop_front` — Fronte negozio con tenda

- **Dimensioni**: 3.0 × 3.0 × 0.45 m · **triangoli max**: 1800 · **texture**: 2048
- **Varianti**: 4 · **istanze in mappa**: ~140
- **Salva come**: `map/facade/facade_shop_front_v1.glb`

```text
Small Italian shop front with metal roller or glass door, striped awning, chalkboard sign, 3 m wide, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_shop_front_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_shop_front_v3.glb`
4. `in a different palette: pale sand plaster and dark brown wood` → `facade_shop_front_v4.glb`

### 8. `facade_plaster_plain` — Modulo facciata — intonaco liscio

- **Dimensioni**: 3.0 × 3.0 × 0.2 m · **triangoli max**: 900 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~180
- **Salva come**: `map/facade/facade_plaster_plain_v1.glb`

```text
Plain weathered Mediterranean plaster wall panel, 3 m wide by 3 m tall, subtle cracks and stains, warm ochre tone, flat back, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_plaster_plain_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_plaster_plain_v3.glb`

### 9. `facade_cornice` — Cornice di gronda

- **Dimensioni**: 3.0 × 0.35 × 0.45 m · **triangoli max**: 500 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~300
- **Salva come**: `map/facade/facade_cornice_v1.glb`

```text
Plaster cornice moulding segment, 3 m long, projecting 0.35 m, for Mediterranean facade, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_cornice_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_cornice_v3.glb`

### 10. `facade_window_balcony` — Finestra con balconcino in ferro

- **Dimensioni**: 3.0 × 3.0 × 0.55 m · **triangoli max**: 1600 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~260
- **Salva come**: `map/facade/facade_window_balcony_v1.glb`

```text
Italian window with small wrought iron balcony, 1.6 m wide, 0.5 m deep, 3x3 m plaster wall panel, laundry optional, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_window_balcony_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_window_balcony_v3.glb`

### 11. `facade_corner_pilaster` — Pilastro d'angolo

- **Dimensioni**: 0.6 × 3.0 × 0.6 m · **triangoli max**: 600 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~90
- **Salva come**: `map/facade/facade_corner_pilaster_v1.glb`

```text
Corner quoin pilaster of a Mediterranean building, weathered plaster and stone edges, 3 m tall, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_corner_pilaster_v2.glb`

### 12. `roof_tile_edge` — Filo di coppi

- **Dimensioni**: 3.0 × 0.3 × 0.6 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~300
- **Salva come**: `map/roof/roof_tile_edge_v1.glb`

```text
Terracotta roof tile edge segment, 3 m long, traditional Italian coppi, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_tile_edge_v2.glb`

### 13. `roof_chimney` — Comignolo

- **Dimensioni**: 0.62 × 1.37 × 0.62 m · **triangoli max**: 600 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~180
- **Salva come**: `map/roof/roof_chimney_v1.glb`

```text
Small Italian rooftop chimney with cap, 0.6 x 0.6 m, 1.4 m tall, plaster and terracotta, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_chimney_v2.glb`
3. `newer, clean terracotta` → `roof_chimney_v3.glb`

### 14. `street_curb` — Cordolo marciapiede

- **Dimensioni**: 1.0 × 0.16 × 0.3 m · **triangoli max**: 200 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~400
- **Salva come**: `map/street/street_curb_v1.glb`

```text
Stone kerb segment 1 m long, 0.16 m tall, weathered granite, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_curb_v2.glb`

### 15. `street_manhole` — Tombino

- **Dimensioni**: 0.6 × 0.04 × 0.6 m · **triangoli max**: 250 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~60
- **Salva come**: `map/street/street_manhole_v1.glb`

```text
Cast iron round manhole cover, 0.6 m, worn, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_manhole_v2.glb`
3. `with chipped dark green paint` → `street_manhole_v3.glb`

### 16. `street_lamp_wall` — Lampada a muro

- **Dimensioni**: 0.35 × 0.6 × 0.5 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~90
- **Salva come**: `map/street/street_lamp_wall_v1.glb`

```text
Old wrought iron wall lamp with small frosted globe, 0.5 m projection, Mediterranean, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_lamp_wall_v2.glb`
3. `with chipped dark green paint` → `street_lamp_wall_v3.glb`
4. `with faded posters and stickers` → `street_lamp_wall_v4.glb`

### 17. `street_bollard` — Paracarri

- **Dimensioni**: 0.22 × 0.9 × 0.22 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~70
- **Salva come**: `map/street/street_bollard_v1.glb`

```text
Stone bollard 0.9 m tall, worn edges, Italian town, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_bollard_v2.glb`
3. `with chipped dark green paint` → `street_bollard_v3.glb`

### 18. `street_bench` — Panchina

- **Dimensioni**: 1.8 × 0.85 × 0.6 m · **triangoli max**: 700 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~35
- **Salva come**: `map/street/street_bench_v1.glb`

```text
Wooden slat bench with cast iron arms, 1.8 m, weathered paint, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_bench_v2.glb`
3. `with chipped dark green paint` → `street_bench_v3.glb`

### 19. `street_planter` — Fioriera

- **Dimensioni**: 1.0 × 0.55 × 0.45 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~45
- **Salva come**: `map/street/street_planter_v1.glb`

```text
Terracotta planter box 1 m with small Mediterranean plants, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_planter_v2.glb`
3. `with chipped dark green paint` → `street_planter_v3.glb`

### 20. `street_bike` — Bicicletta parcheggiata

- **Dimensioni**: 0.6 × 1.05 × 1.8 m · **triangoli max**: 1200 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~30
- **Salva come**: `map/street/street_bike_v1.glb`

```text
Old city bicycle leaning, 1.8 m long, low poly game-ready, one object
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_bike_v2.glb`
3. `with chipped dark green paint` → `street_bike_v3.glb`

### 21. `street_scooter` — Motorino parcheggiato

- **Dimensioni**: 0.7 × 1.1 × 1.7 m · **triangoli max**: 1800 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~25
- **Salva come**: `map/street/street_scooter_v1.glb`

```text
Vintage Italian scooter parked, 1.7 m long, low poly game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_scooter_v2.glb`
3. `with chipped dark green paint` → `street_scooter_v3.glb`

### 22. `street_cables` — Fascio di cavi aerei

- **Dimensioni**: 0.15 × 0.15 × 6.0 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~60
- **Salva come**: `map/street/street_cables_v1.glb`

```text
Bundle of sagging overhead utility cables, 6 m span, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_cables_v2.glb`
3. `with chipped dark green paint` → `street_cables_v3.glb`

### 23. `street_puddle` — Pozzanghera (decal)

- **Dimensioni**: 1.6 × 0.02 × 1.2 m · **triangoli max**: 40 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~50
- **Salva come**: `map/street/street_puddle_v1.glb`

```text
Flat puddle decal with soft edge, 1.6 m, game-ready, alpha blended
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_puddle_v2.glb`
3. `with chipped dark green paint` → `street_puddle_v3.glb`

### 24. `street_bin` — Cestino

- **Dimensioni**: 0.45 × 0.9 × 0.45 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~40
- **Salva come**: `map/street/street_bin_v1.glb`

```text
Street litter bin on post, 0.45 m diameter, dark green painted metal, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_bin_v2.glb`

### 25. `market_crate` — Cassetta di legno

- **Dimensioni**: 0.5 × 0.28 × 0.35 m · **triangoli max**: 250 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~60
- **Salva come**: `map/market/market_crate_v1.glb`

```text
Wooden produce crate, 0.5 m, stacked look, weathered, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_crate_v2.glb`
3. `filled with lemons and oranges` → `market_crate_v3.glb`
4. `filled with aubergines and peppers` → `market_crate_v4.glb`

### 26. `market_produce` — Cassetta con prodotti

- **Dimensioni**: 0.5 × 0.22 × 0.35 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 6 · **istanze in mappa**: ~48
- **Salva come**: `map/market/market_produce_v1.glb`

```text
Produce crate filled with tomatoes, lemons, aubergines or fish, 0.5 m, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_produce_v2.glb`
3. `filled with lemons and oranges` → `market_produce_v3.glb`
4. `filled with aubergines and peppers` → `market_produce_v4.glb`
5. `filled with fish on crushed ice` → `market_produce_v5.glb`
6. `with faded striped canvas, sun-bleached` → `market_produce_v6.glb`

### 27. `nature_pine` — Pino marittimo

- **Dimensioni**: 5.0 × 9.0 × 5.0 m · **triangoli max**: 2500 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~40
- **Salva come**: `map/nature/nature_pine_v1.glb`

```text
Mediterranean stone pine, umbrella canopy, 9 m tall, low poly game-ready, 3 LODs
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_pine_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_pine_v3.glb`

### 28. `nature_shrub` — Macchia mediterranea

- **Dimensioni**: 1.2 × 0.9 × 1.2 m · **triangoli max**: 700 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~80
- **Salva come**: `map/nature/nature_shrub_v1.glb`

```text
Mediterranean scrub bush, 1.2 m, dry green, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_shrub_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_shrub_v3.glb`
4. `with exposed roots and rough bark` → `nature_shrub_v4.glb`

### 29. `nature_pot` — Vaso con pianta

- **Dimensioni**: 0.45 × 0.7 × 0.45 m · **triangoli max**: 450 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~60
- **Salva come**: `map/nature/nature_pot_v1.glb`

```text
Terracotta pot with geranium or basil, 0.7 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_pot_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_pot_v3.glb`
4. `with exposed roots and rough bark` → `nature_pot_v4.glb`

### 30. `cave_portal` — Portale d'ingresso

- **Dimensioni**: 4.0 × 3.6 × 0.8 m · **triangoli max**: 2000 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~7
- **Salva come**: `map/cave/cave_portal_v1.glb`

```text
Natural rock arch entrance to a cave, 3 m opening, 3.6 m tall, irregular stone, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_portal_v2.glb`


## SPRINT 2 — le due estrazioni

### 31. `cave_vault_straight` — Volta dritta 6 m

- **Dimensioni**: 3.0 × 3.0 × 6.0 m · **triangoli max**: 3000 · **texture**: 2048
- **Varianti**: 1 · **istanze in mappa**: ~22
- **Salva come**: `map/cave/cave_vault_straight_v1.glb`

```text
Cave tunnel segment, 6 m long, 3 m wide, 3 m high, smooth eroded limestone vault, no sharp box edges, game-ready
```

### 32. `cave_vault_curve` — Volta curva 15/30/45 gradi

- **Dimensioni**: 3.0 × 3.0 × 6.0 m · **triangoli max**: 3500 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~12
- **Salva come**: `map/cave/cave_vault_curve_v1.glb`

```text
Curved cave tunnel segment, sweeping bend, 3 m wide, 3 m high, eroded limestone, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_vault_curve_v2.glb`
3. `with moss and green algae patches` → `cave_vault_curve_v3.glb`

### 33. `cave_wall_panel` — Parete di camera (pannello)

- **Dimensioni**: 8.0 × 6.0 × 1.5 m · **triangoli max**: 4500 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~18
- **Salva come**: `map/cave/cave_wall_panel_v1.glb`

```text
Large concave cave wall panel, 8 m wide 6 m tall, layered limestone, irregular relief, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_wall_panel_v2.glb`
3. `with moss and green algae patches` → `cave_wall_panel_v3.glb`

### 34. `cave_ceiling_panel` — Soffitto di camera (pannello)

- **Dimensioni**: 8.0 × 8.0 × 2.0 m · **triangoli max**: 4000 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~12
- **Salva come**: `map/cave/cave_ceiling_panel_v1.glb`

```text
Cave ceiling panel, 8x8 m, rough rock with stalactite stubs, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_ceiling_panel_v2.glb`

### 35. `cave_floor_patch` — Pavimento di caverna

- **Dimensioni**: 6.0 × 0.25 × 6.0 m · **triangoli max**: 1500 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~24
- **Salva come**: `map/cave/cave_floor_patch_v1.glb`

```text
Cave floor patch 6x6 m, damp gravel, small stones, puddle option, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_floor_patch_v2.glb`
3. `with moss and green algae patches` → `cave_floor_patch_v3.glb`

### 36. `cave_boulder` — Masso

- **Dimensioni**: 2.2 × 1.6 × 2.0 m · **triangoli max**: 1200 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~24
- **Salva come**: `map/cave/cave_boulder_v1.glb`

```text
Large angular cave boulder, 2 m, damp limestone, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_boulder_v2.glb`
3. `with moss and green algae patches` → `cave_boulder_v3.glb`

### 37. `cave_stalactites` — Colata di stalattiti

- **Dimensioni**: 1.5 × 2.2 × 1.5 m · **triangoli max**: 1600 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~14
- **Salva come**: `map/cave/cave_stalactites_v1.glb`

```text
Cluster of stalactites hanging from cave ceiling, 1.5 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_stalactites_v2.glb`

### 38. `cave_stalagmite` — Stalagmite

- **Dimensioni**: 0.8 × 1.8 × 0.8 m · **triangoli max**: 1200 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~16
- **Salva come**: `map/cave/cave_stalagmite_v1.glb`

```text
Cave stalagmite, 1.8 m tall, wet limestone, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_stalagmite_v2.glb`
3. `with moss and green algae patches` → `cave_stalagmite_v3.glb`

### 39. `cave_rubble` — Detrito

- **Dimensioni**: 2.0 × 0.6 × 1.6 m · **triangoli max**: 800 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/cave/cave_rubble_v1.glb`

```text
Pile of broken rock and rubble, 2 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_rubble_v2.glb`

### 40. `cave_beam` — Puntello di legno

- **Dimensioni**: 0.2 × 2.6 × 0.2 m · **triangoli max**: 350 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~10
- **Salva come**: `map/cave/cave_beam_v1.glb`

```text
Rough wooden mine support beam, 2.6 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_beam_v2.glb`

### 41. `cave_floodlight` — Faro da cantiere

- **Dimensioni**: 0.4 × 0.5 × 0.5 m · **triangoli max**: 500 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~8
- **Salva come**: `map/cave/cave_floodlight_v1.glb`

```text
Portable work floodlight on tripod, 0.5 m head, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `cave_floodlight_v2.glb`

### 42. `galleria_lining` — Rivestimento galleria 6 m

- **Dimensioni**: 12.0 × 6.0 × 6.0 m · **triangoli max**: 4000 · **texture**: 2048
- **Varianti**: 1 · **istanze in mappa**: ~10
- **Salva come**: `map/cave/galleria_lining_v1.glb`

```text
Old concrete tunnel lining segment, 6 m long, 12 m span, stained, with cable trays, game-ready
```

### 43. `galleria_niche` — Nicchia di manutenzione

- **Dimensioni**: 4.0 × 3.0 × 1.2 m · **triangoli max**: 1500 · **texture**: 2048
- **Varianti**: 1 · **istanze in mappa**: ~2
- **Salva come**: `map/cave/galleria_niche_v1.glb`

```text
Concrete maintenance niche in tunnel wall, 4 m wide, 3 m tall, game-ready
```

### 44. `galleria_lamp` — Lampada di galleria

- **Dimensioni**: 0.3 × 0.3 × 0.6 m · **triangoli max**: 300 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~16
- **Salva come**: `map/cave/galleria_lamp_v1.glb`

```text
Caged industrial tunnel lamp, 0.6 m, dusty, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `wetter, with dark damp streaks` → `galleria_lamp_v2.glb`

### 45. `galleria_vent` — Griglia di ventilazione

- **Dimensioni**: 1.2 × 1.2 × 0.15 m · **triangoli max**: 400 · **texture**: 2048
- **Varianti**: 1 · **istanze in mappa**: ~4
- **Salva come**: `map/cave/galleria_vent_v1.glb`

```text
Rusted ventilation grille panel, 1.2 m square, game-ready
```

### 46. `harbour_bollard` — Bitta d'ormeggio

- **Dimensioni**: 0.4 × 0.55 × 0.4 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/harbour/harbour_bollard_v1.glb`

```text
Cast iron mooring bollard, 0.55 m, rusted, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_bollard_v2.glb`

### 47. `harbour_fender` — Parabordo

- **Dimensioni**: 0.3 × 0.7 × 0.3 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~25
- **Salva come**: `map/harbour/harbour_fender_v1.glb`

```text
Tyre fender hanging from rope, 0.7 m, weathered rubber, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_fender_v2.glb`

### 48. `harbour_rope` — Cima avvolta

- **Dimensioni**: 0.6 × 0.25 × 0.6 m · **triangoli max**: 350 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/harbour/harbour_rope_v1.glb`

```text
Coiled mooring rope on ground, 0.6 m, salted and worn, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_rope_v2.glb`

### 49. `harbour_nets` — Rete da pesca

- **Dimensioni**: 1.8 × 0.5 × 1.2 m · **triangoli max**: 900 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~15
- **Salva come**: `map/harbour/harbour_nets_v1.glb`

```text
Pile of fishing nets and floats, 1.8 m, green and orange, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_nets_v2.glb`

### 50. `harbour_buoy` — Boa

- **Dimensioni**: 0.4 × 0.6 × 0.4 m · **triangoli max**: 350 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~18
- **Salva come**: `map/harbour/harbour_buoy_v1.glb`

```text
Small fishing buoy, 0.4 m, faded paint, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_buoy_v2.glb`
3. `faded blue paint, peeling` → `harbour_buoy_v3.glb`

### 51. `harbour_boat_small` — Barca a remi

- **Dimensioni**: 1.4 × 0.55 × 3.6 m · **triangoli max**: 1800 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~6
- **Salva come**: `map/harbour/harbour_boat_small_v1.glb`

```text
Small wooden rowing boat, 3.6 m long, peeling paint, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_boat_small_v2.glb`

### 52. `harbour_pallet` — Pallet

- **Dimensioni**: 1.2 × 0.15 × 0.8 m · **triangoli max**: 200 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~30
- **Salva come**: `map/harbour/harbour_pallet_v1.glb`

```text
Wooden pallet 1.2 x 0.8 m, weathered, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_pallet_v2.glb`

### 53. `harbour_container` — Container

- **Dimensioni**: 6.0 × 2.6 × 2.4 m · **triangoli max**: 900 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~8
- **Salva come**: `map/harbour/harbour_container_v1.glb`

```text
Shipping container 6 m, faded paint, rust, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `heavily rusted, orange-brown` → `harbour_container_v2.glb`
3. `faded blue paint, peeling` → `harbour_container_v3.glb`

### 54. `harbour_crane_hook` — Gancio della gru

- **Dimensioni**: 0.4 × 1.2 × 0.4 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~1
- **Salva come**: `map/harbour/harbour_crane_hook_v1.glb`

```text
Heavy crane hook block with cable, 1.2 m, game-ready low poly
```

### 55. `sign_shop_a` — Insegna negozio (insegna)

- **Dimensioni**: 1.6 × 0.4 × 0.06 m · **triangoli max**: 250 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~30
- **Salva come**: `map/signage/sign_shop_a_v1.glb`

```text
Italian shop sign board, 1.6 m, weathered painted text, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `faded and scratched` → `sign_shop_a_v2.glb`
3. `torn at one corner` → `sign_shop_a_v3.glb`
4. `in a different colour scheme` → `sign_shop_a_v4.glb`

### 56. `sign_poster` — Manifesto (decal)

- **Dimensioni**: 0.4 × 0.6 × 0.01 m · **triangoli max**: 60 · **texture**: 1024
- **Varianti**: 5 · **istanze in mappa**: ~50
- **Salva come**: `map/signage/sign_poster_v1.glb`

```text
Torn concert poster decal, 0.4x0.6 m, weathered, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `faded and scratched` → `sign_poster_v2.glb`
3. `torn at one corner` → `sign_poster_v3.glb`
4. `in a different colour scheme` → `sign_poster_v4.glb`
5. `newer, freshly painted` → `sign_poster_v5.glb`

### 57. `sign_graffiti` — Graffiti (decal)

- **Dimensioni**: 2.0 × 1.0 × 0.01 m · **triangoli max**: 60 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~30
- **Salva come**: `map/signage/sign_graffiti_v1.glb`

```text
Faded graffiti tag decal, 2 m, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `faded and scratched` → `sign_graffiti_v2.glb`
3. `torn at one corner` → `sign_graffiti_v3.glb`
4. `in a different colour scheme` → `sign_graffiti_v4.glb`

### 58. `sign_stencil` — Numeri civici e stencil

- **Dimensioni**: 0.3 × 0.3 × 0.01 m · **triangoli max**: 40 · **texture**: 1024
- **Varianti**: 5 · **istanze in mappa**: ~60
- **Salva come**: `map/signage/sign_stencil_v1.glb`

```text
Painted house number stencil decal, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `faded and scratched` → `sign_stencil_v2.glb`
3. `torn at one corner` → `sign_stencil_v3.glb`
4. `in a different colour scheme` → `sign_stencil_v4.glb`
5. `newer, freshly painted` → `sign_stencil_v5.glb`


## SPRINT 3 — il resto della città

### 59. `facade_stone_plain` — Modulo facciata — pietra a vista

- **Dimensioni**: 3.0 × 3.0 × 0.2 m · **triangoli max**: 1200 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~120
- **Salva come**: `map/facade/facade_stone_plain_v1.glb`

```text
Rough limestone masonry wall panel 3x3 m, irregular stone courses, aged mortar, Ligurian coastal town, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_stone_plain_v2.glb`

### 60. `facade_garage_shutter` — Basculante garage

- **Dimensioni**: 3.0 × 3.0 × 0.15 m · **triangoli max**: 700 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~60
- **Salva come**: `map/facade/facade_garage_shutter_v1.glb`

```text
Corrugated metal garage shutter, 2.4 m wide, rusty, in plaster wall panel 3x3 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_garage_shutter_v2.glb`

### 61. `facade_downpipe` — Pluviale

- **Dimensioni**: 0.12 × 6.0 × 0.12 m · **triangoli max**: 350 · **texture**: 2048
- **Varianti**: 2 · **istanze in mappa**: ~120
- **Salva come**: `map/facade/facade_downpipe_v1.glb`

```text
Vertical zinc downpipe with brackets, 6 m tall, weathered, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_downpipe_v2.glb`

### 62. `facade_parapet` — Parapetto di terrazza

- **Dimensioni**: 3.0 × 1.0 × 0.25 m · **triangoli max**: 700 · **texture**: 2048
- **Varianti**: 3 · **istanze in mappa**: ~90
- **Salva come**: `map/facade/facade_parapet_v1.glb`

```text
Mediterranean roof terrace parapet, 3 m long, 1 m tall, plaster with stone coping, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with the shutters closed and weathered green paint` → `facade_parapet_v2.glb`
3. `with peeling plaster and exposed stone patches` → `facade_parapet_v3.glb`

### 63. `roof_stairhead` — Corpo scala / abbaino

- **Dimensioni**: 2.6 × 2.6 × 2.3 m · **triangoli max**: 900 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~90
- **Salva come**: `map/roof/roof_stairhead_v1.glb`

```text
Rooftop stair head with small tiled roof, 2.6 x 2.3 m, 2.6 m tall, Mediterranean, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_stairhead_v2.glb`

### 64. `roof_water_tank` — Serbatoio dell'acqua

- **Dimensioni**: 1.5 × 1.55 × 1.5 m · **triangoli max**: 600 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~40
- **Salva come**: `map/roof/roof_water_tank_v1.glb`

```text
Rooftop galvanised water tank on steel legs, 1.5 m cube, weathered, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_water_tank_v2.glb`

### 65. `roof_antenna` — Antenna TV

- **Dimensioni**: 0.1 × 2.6 × 0.6 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~120
- **Salva come**: `map/roof/roof_antenna_v1.glb`

```text
Thin rooftop TV antenna with mast, 2.6 m tall, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_antenna_v2.glb`
3. `newer, clean terracotta` → `roof_antenna_v3.glb`

### 66. `roof_dish` — Parabola

- **Dimensioni**: 0.8 × 0.9 × 0.4 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~40
- **Salva come**: `map/roof/roof_dish_v1.glb`

```text
Small satellite dish on rooftop mount, 0.8 m, weathered white, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `with moss and lichen` → `roof_dish_v2.glb`

### 67. `street_drain` — Griglia di scolo

- **Dimensioni**: 0.5 × 0.05 × 0.35 m · **triangoli max**: 200 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~50
- **Salva come**: `map/street/street_drain_v1.glb`

```text
Cast iron rectangular drainage grate, 0.5 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_drain_v2.glb`

### 68. `street_lamp_post` — Lampione su palo

- **Dimensioni**: 0.3 × 4.2 × 0.8 m · **triangoli max**: 700 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~30
- **Salva come**: `map/street/street_lamp_post_v1.glb`

```text
Cast iron street lamp post 4.2 m with lantern head, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_lamp_post_v2.glb`

### 69. `street_fountain` — Fontanella

- **Dimensioni**: 0.5 × 1.1 × 0.4 m · **triangoli max**: 600 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~4
- **Salva come**: `map/street/street_fountain_v1.glb`

```text
Small Italian public drinking fountain, cast iron, 1.1 m, game-ready low poly
```

### 70. `street_kiosk` — Edicola

- **Dimensioni**: 2.4 × 2.6 × 1.6 m · **triangoli max**: 2000 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~3
- **Salva come**: `map/street/street_kiosk_v1.glb`

```text
Newspaper kiosk, 2.4 x 1.6 m, 2.6 m tall, painted metal and canvas, closed shutters, Italian, game-ready
```

### 71. `street_cabinet` — Cabina elettrica

- **Dimensioni**: 0.9 × 1.4 × 0.5 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~15
- **Salva come**: `map/street/street_cabinet_v1.glb`

```text
Utility electrical cabinet 1.4 m, weathered grey, tags and stickers, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_cabinet_v2.glb`

### 72. `street_sign` — Cartello stradale

- **Dimensioni**: 0.6 × 2.2 × 0.08 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~30
- **Salva come**: `map/street/street_sign_v1.glb`

```text
Italian street sign on pole, 2.2 m, reflective plate, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_sign_v2.glb`
3. `with chipped dark green paint` → `street_sign_v3.glb`
4. `with faded posters and stickers` → `street_sign_v4.glb`

### 73. `street_nameplate` — Targa via

- **Dimensioni**: 0.5 × 0.25 × 0.04 m · **triangoli max**: 150 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~40
- **Salva come**: `map/street/street_nameplate_v1.glb`

```text
Italian ceramic street name plate, 0.5 x 0.25 m, enamel, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `rusted and slightly dented` → `street_nameplate_v2.glb`
3. `with chipped dark green paint` → `street_nameplate_v3.glb`

### 74. `market_stall` — Banco del mercato

- **Dimensioni**: 2.4 × 0.9 × 1.2 m · **triangoli max**: 1500 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~18
- **Salva come**: `map/market/market_stall_v1.glb`

```text
Italian market stall table with wooden frame, 2.4 m, closed canvas awning, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_stall_v2.glb`
3. `filled with lemons and oranges` → `market_stall_v3.glb`

### 75. `market_awning` — Tenda parasole

- **Dimensioni**: 3.0 × 2.2 × 2.0 m · **triangoli max**: 900 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~18
- **Salva come**: `map/market/market_awning_v1.glb`

```text
Canvas market awning on thin steel poles, 3 x 2 m, striped faded colours, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_awning_v2.glb`
3. `filled with lemons and oranges` → `market_awning_v3.glb`

### 76. `market_barrel` — Barile

- **Dimensioni**: 0.55 × 0.85 × 0.55 m · **triangoli max**: 500 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/market/market_barrel_v1.glb`

```text
Wooden barrel 0.85 m tall, metal hoops, weathered, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_barrel_v2.glb`

### 77. `market_scale` — Bilancia a piatti

- **Dimensioni**: 0.45 × 0.5 × 0.3 m · **triangoli max**: 600 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~10
- **Salva come**: `map/market/market_scale_v1.glb`

```text
Old balance scale with brass pans, 0.5 m, game-ready low poly
```

### 78. `market_icebox` — Cassetta frigorifera

- **Dimensioni**: 0.8 × 0.5 × 0.5 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~12
- **Salva come**: `map/market/market_icebox_v1.glb`

```text
White insulated fish box, 0.8 m, worn, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_icebox_v2.glb`

### 79. `market_bulbs` — Filo di lampadine

- **Dimensioni**: 0.1 × 0.15 × 4.0 m · **triangoli max**: 300 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~10
- **Salva come**: `map/market/market_bulbs_v1.glb`

```text
String of hanging bare bulbs, 4 m span, warm glow, game-ready low poly
```

### 80. `market_board` — Lavagnetta

- **Dimensioni**: 0.6 × 0.9 × 0.06 m · **triangoli max**: 250 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~14
- **Salva come**: `map/market/market_board_v1.glb`

```text
Chalkboard A-frame sign, 0.9 m, hand-written look, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `filled with tomatoes and basil` → `market_board_v2.glb`
3. `filled with lemons and oranges` → `market_board_v3.glb`

### 81. `nature_olive` — Olivo

- **Dimensioni**: 3.0 × 4.5 × 3.0 m · **triangoli max**: 1800 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/nature/nature_olive_v1.glb`

```text
Old olive tree, gnarled trunk, 4.5 m, low poly game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_olive_v2.glb`

### 82. `nature_agave` — Agave

- **Dimensioni**: 1.0 × 0.8 × 1.0 m · **triangoli max**: 800 · **texture**: 1024
- **Varianti**: 2 · **istanze in mappa**: ~20
- **Salva come**: `map/nature/nature_agave_v1.glb`

```text
Agave plant, 1 m spread, blue-green, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_agave_v2.glb`

### 83. `nature_ivy` — Edera a parete (card)

- **Dimensioni**: 2.0 × 2.0 × 0.05 m · **triangoli max**: 120 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~60
- **Salva come**: `map/nature/nature_ivy_v1.glb`

```text
Ivy growing on wall, 2x2 m alpha card, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_ivy_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_ivy_v3.glb`

### 84. `nature_grass` — Ciuffo d'erba (card)

- **Dimensioni**: 0.5 × 0.3 × 0.5 m · **triangoli max**: 40 · **texture**: 1024
- **Varianti**: 3 · **istanze in mappa**: ~150
- **Salva come**: `map/nature/nature_grass_v1.glb`

```text
Dry grass tuft, 0.5 m, alpha card, game-ready
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_grass_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_grass_v3.glb`

### 85. `nature_rock` — Roccia

- **Dimensioni**: 1.6 × 1.0 × 1.4 m · **triangoli max**: 900 · **texture**: 1024
- **Varianti**: 4 · **istanze in mappa**: ~60
- **Salva come**: `map/nature/nature_rock_v1.glb`

```text
Weathered coastal limestone boulder, 1.6 m, game-ready low poly
```

Varianti successive — **stesso prompt**, aggiungi in coda:
2. `dry summer condition, dusty leaves` → `nature_rock_v2.glb`
3. `wind-bent, grown toward the sea` → `nature_rock_v3.glb`
4. `with exposed roots and rough bark` → `nature_rock_v4.glb`

### 86. `objective_kit` — Kit decodifica

- **Dimensioni**: 0.6 × 0.3 × 0.4 m · **triangoli max**: 1200 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~1
- **Salva come**: `map/objective/objective_kit_v1.glb`

```text
Electronic decoding kit case open with tools, 0.6 m, game-ready
```

### 87. `objective_robot` — Robot deployabile

- **Dimensioni**: 0.7 × 0.5 × 0.9 m · **triangoli max**: 2500 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~1
- **Salva come**: `map/objective/objective_robot_v1.glb`

```text
Small tracked bomb-disposal style robot with arm, 0.9 m, game-ready
```

### 88. `objective_siren` — Sirena

- **Dimensioni**: 0.25 × 0.25 × 0.25 m · **triangoli max**: 400 · **texture**: 1024
- **Varianti**: 1 · **istanze in mappa**: ~2
- **Salva come**: `map/objective/objective_siren_v1.glb`

```text
Industrial warning siren, 0.25 m, red, game-ready
```

---

## 5 · Quando hai finito

- `map/facade/*` → materiali e moduli per `MapDressing`
- `map/cave/*` → sostituisce i volumi `rock` di Grotta e Galleria (solo decor, collisione intatta)
- `map/objective/*` → cassa, Nucleo, faro: **non sono decor**, vanno agganciati a `MatchState.Core`
  e devono leggersi a 80 m
- `map/street|market|harbour|nature/*` → addossati ai muri, **mai in mezzo alla strada**
  (richiederebbero di rifare AABB e rotte verificate)

Il confronto finale è con [../docs/19-asset-mappa.md](../docs/19-asset-mappa.md): se un asset non
rispetta quelle misure, la mappa non torna.
