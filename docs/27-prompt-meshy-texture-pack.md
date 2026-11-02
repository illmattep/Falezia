# Meshy — texture per Calanera (prompt pronti)

## BLOCCO DI STILE (da anteporre a OGNI prompt texture)

```
STYLE: hand-painted gouache texture for a stylized Mediterranean town, 1900s Italian coast.
FLAT COLOUR PASSES ONLY: no photorealism, no visible photo detail, no baked lighting,
no ambient occlusion, no shadows, no highlights, no vignette, no perspective.
Even, neutral illumination. Soft brush variation, gentle value shifts, thin brush strokes.
TILEABLE: the texture must repeat seamlessly on all four edges, no visible border, no objects
crossing the edge. Orthographic top-down / face-on view, filling the whole frame.
TECH: 1024x1024 (2048 for large surfaces), PBR albedo + roughness, roughness 0.75-0.95,
metalness 0 except metal materials. No alpha unless stated.
PALETTE: warm ochre, terracotta, sun-bleached cream, dusty rose, sage green,
bottle green, slate blue, iron grey, weathered limestone.
NEGATIVE: photographic, gritty noise, dirt decals not asked for, text, logos, people,
objects in frame, lighting, drop shadows, HDR, bloom, chromatic aberration.
```

## Regole di scala (tassello)

| Famiglia | Tassello consigliato | Note |
|---|---|---|
| intonaco | 3 – 4 m | una texture copre circa un piano di facciata |
| pietra a blocchi | 2 m | conci leggibili a 5 m di distanza |
| coppi | 1 m | tegole grandi ~25 cm |
| legno | 1 – 1.5 m | assi larghe 12–18 cm |
| sampietrini | 2 – 3 m | pietre 8–12 cm |
| lastre di piazza | 3 – 4 m | lastre 60–90 cm |
| metallo | 1 m | lamiere e ferro |

---

## 1. Intonaci (la base di ogni casa)

| ID | Prompt | Colore base | Rough |
|---|---|---|---|
| `wall_venez_01` | smooth Venetian lime plaster, pale warm cream, barely visible trowel sweeps, a few faint damp blooms near the bottom edge | #E8DCC4 | 0.90 |
| `wall_venez_02` | smooth lime plaster, sun-bleached ochre yellow, subtle horizontal brush rhythm, light chalking at the top | #E0C08A | 0.90 |
| `wall_venez_03` | smooth lime plaster, dusty rose with cream patches where the sun hits, very even | #E2BEB0 | 0.90 |
| `wall_venez_04` | smooth lime plaster, pale sage green, faint vertical wash streaks | #CBD2C0 | 0.90 |
| `wall_grezzo_01` | rough render plaster, warm sand colour, pronounced stucco grain, small pits and trowel dents, a few hairline cracks | #D8C39C | 0.95 |
| `wall_grezzo_02` | rough render plaster, grey-beige, heavy grain, patches worn smoother by hands at mid height | #CFC6B6 | 0.95 |
| `wall_grezzo_03` | rough render plaster, terracotta tint, chipped lower band showing a warmer undercoat | #D9A98A | 0.95 |
| `wall_damaged_01` | plaster fallen away in irregular patches revealing handmade brick underneath, flat painted brick faces, cream plaster remains | #DCCDB2 / #A9705A | 0.95 |
| `wall_damaged_02` | plaster peeling in long flakes along a vertical crack, exposed rough stone beneath, warm grey palette | #D6CBB6 / #9A9084 | 0.95 |
| `wall_stone_01` | coursed limestone ashlar, warm ivory blocks, thin mortar joints, hand-dressed faces, slight size variation | #D9D0BC | 0.92 |
| `wall_stone_02` | rubble stone wall, irregular grey-brown stones, wide pale mortar, no order | #BFB4A2 | 0.94 |

## 2. Coperture

| ID | Prompt | Colore base | Rough |
|---|---|---|---|
| `roof_coppi_01` | terracotta barrel roof tiles, new, warm orange, light on the crown of each tile, repeating vertical rows | #C4623C | 0.85 |
| `roof_coppi_02` | terracotta roof tiles, aged, faded to dusty salmon with grey lichen flecks | #B26A50 | 0.88 |
| `roof_coppi_03` | terracotta roof tiles, very old, dark burnt red with moss in the joints | #8E4A34 | 0.90 |
| `roof_coppi_04` | mixed roof tiles, three tones of terracotta laid together, repaired patch look | #B8613F | 0.87 |
| `roof_edge_01` | terracotta ridge tile row, half-round, seen from above | #A8552F | 0.85 |

## 3. Pietre e suolo

| ID | Prompt | Colore base | Rough |
|---|---|---|---|
| `lastrico_piazza_01` | large irregular stone slabs for a town square, warm light grey, wide pale joints, flat painted faces | #C9C3B6 | 0.92 |
| `lastrico_piazza_02` | large stone slabs, worn smooth in the middle of each slab, darker dusty joints | #BFB9AC | 0.92 |
| `sampietrini_01` | rounded cobblestones in rows, warm grey with beige variation, compact earth in the joints | #A9A398 | 0.94 |
| `sampietrini_02` | cobblestones, darker basalt grey, wet-looking joints | #8C8880 | 0.88 |
| `marciapiede_01` | smooth concrete kerb stone, pale grey, one clean edge | #C6C2BA | 0.90 |
| `scala_pietra_01` | worn stone stair tread, warm grey, rounded nosing, fine grain | #C2BBAD | 0.92 |
| `terra_battuta_01` | compacted earth path, warm brown, fine gravel, wheel ruts | #9C8570 | 0.95 |

## 4. Legno

| ID | Prompt | Colore base | Rough |
|---|---|---|---|
| `legno_porta_01` | old wooden door planks, dark walnut, vertical boards, thin dark seams, iron nail heads | #6B4A34 | 0.85 |
| `legno_porta_02` | painted wooden door, faded bottle green, paint worn at the edges | #3C4E3C | 0.85 |
| `legno_persiana_01` | wooden shutter slats, bottle green paint, thin light between slats | #33472F | 0.85 |
| `legno_persiana_02` | wooden shutter slats, slate blue paint, chipped corners | #445066 | 0.85 |
| `legno_persiana_03` | wooden shutter slats, oxide red paint, sun bleached | #8A4234 | 0.85 |
| `legno_cassa_01` | rough sawn crate boards, pale pine, iron strap shadows | #B08A5E | 0.90 |
| `legno_botte_01` | barrel staves, medium oak, iron hoop bands | #8A6238 | 0.88 |
| `tela_tenda_01` | awning canvas with ochre and oxide red stripes, flat cloth, slight weave | #D8A65E / #8E3B2E | 0.95 |
| `tela_tenda_02` | awning canvas, cream and sage stripes | #E2D8BE / #9AAE8E | 0.95 |

## 5. Metalli

| ID | Prompt | Colore base | Rough | Metal |
|---|---|---|---|---|
| `ferro_battuto_01` | hand-forged iron, dark blue-grey, subtle hammer marks, dry | #3A3C40 | 0.55 | 0.85 |
| `ferro_ruggine_01` | rusted iron plate, orange-brown corrosion, flaking edges | #8A4A2A | 0.85 | 0.30 |
| `lamiera_01` | corrugated painted sheet metal, faded blue-grey, vertical ribs | #6E7C86 | 0.65 | 0.45 |
| `lamiera_02` | corrugated painted sheet metal, faded oxide red, rib highlights | #96503C | 0.65 | 0.45 |

## 6. Acqua, tessuti, vetro

| ID | Prompt | Colore base | Rough |
|---|---|---|---|
| `acqua_01` | still dark water surface, deep teal with faint pale reflections, flat | #2A4A50 | 0.10 |
| `vetro_01` | dark window glass, near-black green, faint reflection streak | #14201E | 0.08 |
| `sacco_01` | burlap sack cloth, coarse weave, warm brown | #A08256 | 0.95 |

## 7. Decal e dettagli dipinti (con alpha)

| ID | Prompt | Uso |
|---|---|---|
| `decal_graffiti_01` | abstract painted graffiti, three flat colours, rough brush | muri bassi |
| `decal_graffiti_02` | faded painted letters, no legible words, ochre and blue | muri bassi |
| `decal_manifesto_01` | torn paper posters layered, muted colours, no text | muri |
| `decal_macchia_umido_01` | damp stain spreading from a corner, grey-green, soft edge | piede dei muri |
| `decal_crepa_01` | single hairline crack with a small chipped area | intonaci |
| `decal_targa_01` | blank enamel street plate, white with blue border | angoli |
| `decal_numero_01` | painted house number, single digit, no style | portoni |
| `decal_ombra_gronda_01` | soft dark band under an eaves, gradient only | sottotetti |

---

## Come si importano

1. Salva in `Assets/Resources/MapTex/` con il nome dell'ID (`wall_venez_02.png`).
2. Nel kit i materiali si creano in `tools/map-assets/kit.py` con `mat(nome, "ID", tinta, rough)`:
   la tinta moltiplica la texture, quindi la texture deve restare **chiara e neutra**.
3. Se la texture non è perfettamente tileabile, il generatore la rende senza cuciture con
   `seamless()` in `tools/map-assets/stylize_tex.py` (tassello a specchio).
4. Dopo la sostituzione: riesportare i pezzi interessati (`kit_full.py`, `kit_palette.py`, …)
   e ricostruire. Il layout non cambia.
