# Meshy — moduli per i due spawn

> 12 pezzi, non una mappa. Si assemblano in Blender (o in Unity col piazzatore) e valgono per
> tutte le zone: la stessa grammatica (pannello, merlo, arco, arredo) ricompone Mercato, Molo,
> Mura e Piazza.

## Impostazioni, una volta sola

| Campo | Valore |
|---|---|
| Modello | l'ultima versione disponibile |
| Stile | **Realistic** (la stilizzazione la chiede il prompt, non il preset) |
| Topologia | **Quad**, simmetria **Off** |
| Texture | **2048**, PBR, **Remove lighting: ON** |
| UV | automatiche |
| Pivot | **base, centro dell'ingombro** · +Z avanti, Y su |

**Prompt negativo (uguale per tutti)**

```text
text, letters, logo, watermark, background, ground plane, cast shadow, baked lighting,
multiple objects, floating parts, photorealistic grunge, noisy dirt, broken thin geometry,
human figure, cartoon outline, cel shading, low resolution texture, blurry
```

**Blocco di stile** — da mettere davanti a ogni prompt:

```text
Stylized game asset, Valorant-like art direction: clean chunky forms, readable silhouette,
generous chamfers, flat color blocks with subtle hand-painted wear, matte PBR, no
photorealism. Single object, real-world scale in meters.
```

---

## 1 · Pannello capannone portuale
**6,0 × 4,0 × 0,45 m** · ~8.000 tri · 2048² · 2 varianti (vernice integra / scrostata)

```text
Warehouse wall panel 6 m wide, 4 m tall: vertically corrugated steel cladding between two
riveted I-beam pilasters, concrete plinth 0.6 m at the base, heavy rivet rows, faded blue-grey
paint with rust streaks running down from the bolts.
```

Varianti: in coda `with peeling paint and exposed bare steel` · `in weathered teal green`.

## 2 · Container 20 piedi
**6,06 × 2,59 × 2,44 m** · ~6.000 tri · 2048² · 3 varianti colore

```text
Shipping container 20 ft: corrugated steel body, double doors at one end with four vertical
locking bars and handles, corner castings at all eight corners, fork pockets, faded paint with
rust bleeding from the edges and the door hinges.
```

Varianti: `paint: deep rust red` · `paint: navy blue` · `paint: faded olive green`.

## 3 · Tratto di muraglia merlata
**6,0 × 4,5 × 1,5 m** · ~10.000 tri · 2048² · 2 varianti (intonaco / pietra a vista)

```text
Fortress rampart section 6 m long, 4.5 m tall, 1.5 m thick: battered stone base, regular
ashlar courses with recessed mortar joints, projecting string course, walkway on top with two
chamfered merlons, one arrow slit in the middle of the wall.
```

## 4 · Porta ad arco
**5,0 × 5,5 × 2,0 m** · ~12.000 tri · 2048² · 1 variante

```text
Medieval gate 5 m wide, 5.5 m tall: semicircular arch of wedge-shaped stone voussoirs, thick
jambs with quoins, wooden double doors with three horizontal iron bands and rows of round
studs, stone threshold step, two empty torch brackets beside the arch.
```

## 5 · Sezione di torre
**5,0 × 9,0 × 5,0 m** · ~12.000 tri · 2048² · 1 variante

```text
Square fortress tower section 5 by 5 m, 9 m tall: heavy ashlar masonry, battered base, three
narrow arrow slits at different heights, corbel table under the top, crown of merlons with
chamfered caps, small iron ring set into the wall.
```

## 6 · Braciere
**0,9 × 1,1 × 0,9 m** · ~3.000 tri · 1024² · 2 varianti (acceso / spento)

```text
Wrought iron brazier 0.9 m wide: shallow bowl on three curved legs, riveted rim, glowing coals
inside, soot-blackened iron, small hanging chain on one side.
```

## 7 · Lampada a muro
**0,40 × 0,65 × 0,50 m** · ~2.000 tri · 1024² · 2 varianti (vetro caldo / spenta)

```text
Wrought iron wall lantern: scrolled bracket, tapered glass housing with iron cross bars, small
decorative finial on top, weathered black iron.
```

## 8 · Stendardo
**0,15 × 3,6 × 1,3 m** · ~1.500 tri · 1024² · 2 varianti (rosso / blu)

```text
Tall banner on an iron pole: spear finial, crossbar at the top, heavy cloth hanging with deep
folds, faded red fabric with a simple painted heraldic band, frayed lower edge.
```

## 9 · Botte
**0,55 × 0,90 × 0,55 m** · ~2.000 tri · 1024² · 2 varianti

```text
Wooden barrel 0.9 m tall: vertical staves, three iron hoops, chipped rim, dark weathered oak.
```

## 10 · Cassa
**0,85 × 0,75 × 0,85 m** · ~1.500 tri · 1024² · 3 varianti (integna, aperta, scrostata)

```text
Wooden shipping crate 0.85 m: plank sides with corner battens, stencilled markings, one side
with a hinged lid slightly open, weathered pine.
```

## 11 · Cassetta del pesce
**0,80 × 0,28 × 0,50 m** · ~1.200 tri · 1024² · 2 varianti (piena / vuota)

```text
Insulated fish crate 0.8 m: white plastic box with reinforced rim, drain slots, two rope
handles, crushed ice and a few fish inside.
```

## 12 · Mucchio di reti
**2,00 × 0,50 × 1,40 m** · ~3.000 tri · 1024² · 2 varianti

```text
Pile of fishing nets on the ground: tangled green and orange netting, cork floats and a couple
of small buoys caught inside, salt-stained rope coiled on top.
```

---

## Dopo la generazione

1. Salva i GLB in `art/map-assets/modules/<nome>.glb`.
2. Li processo con `tools/map-assets/process_meshy.py` (scala, pivot, decimazione, texture).
3. Li assemblo nei due spawn in Blender; la collisione in Unity resta il JSON.

**Ordine di produzione:** 1, 2, 3, 4, 5 (struttura) → 8, 6, 7 (colore e luce) → 9-12 (riempimento).
