# Calanera — kit di pezzi + assemblaggio

## Architettura (sostituisce la "pelle" unica)

1. **Blender costruisce PEZZI SINGOLI**, ognuno con le sue texture e i suoi materiali.
   Stanno in `Assets/StreamingAssets/map/kit/*.glb` (~6,7 MB, 29 pezzi).
   Generatore: `tools/map-assets/kit.py`; esportazione: script `kit_full.py`, `kit_palette.py`, ecc.
2. **`tools/map-assets/make_layout.py`** legge il blockout (`game/maps/calanera.json`) e scrive
   `Assets/StreamingAssets/map/props.json`: elenco di istanze `{model, x, y, z, rot, scale}`.
   - Case: modello scelto per altezza, **orientate verso lo spazio aperto** (`clearance()`),
     scala contenuta per non far compenetrare i tetti.
   - Marciapiedi lungo la facciata, scale sui volumi `stairs`, merli sulla muraglia sud,
     arredi (panche, lampioni, vasi, bancarelle, alberi, fontana, campanile), porto.
3. **Unity non costruisce nulla**: `MapBuilder` ha `UseKit = true` → niente blockout né
   abbellimenti procedurali; `MapProps` istanzia i GLB del kit (cache + `GltfMaterials.ToUrp`).

## Pezzi disponibili

Case 1F/2F/3F in sei tinte (ocra, terra, crema, rosa, salvia, giallo) + bottega con tenda,
campanile, fontana, albero, bancarella, scala, marciapiede, lastricato di piazza, muro merlato,
panca, lampione, vaso, cassa, barile, bitta, container, gru, piano di terreno.

## Texture

Tutte **dipinte in codice** (`tools/map-assets/stylize_tex.py`): intonaco con colature e piede
sporco, coppi tondi con luce sul dorso (due tinte), lastre di pietra per la piazza, sampietrini
arrotondati, legno ad assi. Niente foto.

## Anteprima veloce (senza Unity)

```bash
python3 /tmp/fz/run.py /tmp/fz/preview_run4.py   # scrive /tmp/fz/prev_*.png
```
`tools/map-assets/preview_layout.py` importa ogni pezzo una volta, lo unisce e lo duplica
secondo il layout: la scena Blender diventa la città in pochi secondi.

## Stato

787 pezzi in scena · 29 GLB · caricamento ~3 s · 0 eccezioni · build 661 MB.

## Da fare

- Portico da collegare alle facciate (oggi il pezzo esiste ma una fila in piazza la ostruisce).
- Porto e forte: arredi specifici (banchine, bitte più fitte, merli continui).
- Dettagli a livello di strada: insegne, vasi sui davanzali, porta d'ingresso più leggibile.
- Effetti e animazioni in Unity.
