# Calanera — pipeline finale (semplice)

## Flusso

1. **Blender costruisce ogni pezzo separatamente** (`tools/map-assets/kit.py`) e lo esporta
   in `Assets/StreamingAssets/map/kit/<nome>.glb`. Ogni GLB porta con sé:
   albedo (JPEG), normal (JPEG), roughness (JPEG), materiali per gruppo di superfici.
   Testo: `python3 export_kit.py` dentro Blender (MCP) — 33 pezzi.
2. **`make_layout.py`** legge il blockout e scrive `Assets/StreamingAssets/map/props.json`:
   787 istanze con posizione, rotazione, scala.
3. **Unity assembla**: `MapBuilder` con `UseKit = true` chiama `LevelKit.Build()`, che
   prepara foschia, ambiente e luci; `MapProps` istanzia i GLB e `LevelKit.AttachColliders`
   mette una scatola di collisione per pezzo.

## Perché non c'è il bake Cycles

`bpy.ops.object.bake` in questo setup fa terminare Blender (crash del processo) già al primo
pezzo. Poiché i materiali del kit sono **già statici** (texture importate × tinta cotta nelle
immagini), l'export diretto produce lo stesso risultato pratico: un asset con le sue mappe,
nessuna dipendenza da nodi procedurali. Se serve l'atlante per pezzo, va fatto con un
procedimento diverso (bake esterno o `assimp`/`gltfpack`), non dentro la sessione MCP.

## Numeri attuali

- 33 pezzi, 63 MB totali (359 immagini embeddate).
- 787 istanze in scena · caricamento ~4,5 s · 0 eccezioni · build 955 MB.

## Cosa resta

- Atlante cotto per pezzo (una sola texture per modello invece di una per gruppo).
- Dettagli: portico agganciato alle facciate, arredi del porto, vicoli.
- Effetti e animazioni in Unity.
