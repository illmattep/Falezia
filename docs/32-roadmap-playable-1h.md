# Roadmap 1 ora — gioco pronto e giocabile

Finestra: 13:36 → 14:36 (29 ott 2026). Obiettivo: build **giocabile**, non perfezione.

## Tappe
1. **13:36–13:46 · Dieta asset (build stabile)**
   - LOD0 = decimato alto (fontana 80k, banco 200k, barca 200k, lampione 60k, tavolino 60k, cannone 60k, bitta 25k) → totale ~685k.
   - LOD1 = decimato medio (26k/60k/60k/20k/20k/20k/8k).
   - LOD2 = vecchio decimato leggero.
   - Spostare gli **originali Meshy** fuori da `Assets/` (troppa memoria: la build crasha).
   - `Eroi` usa `LOD/{id}_lod0.glb`, non l'originale.
2. **13:46–14:00 · Build**
   - Editor chiuso, `./run.sh build` batch; 0 errori; player aggiornato.
   - Se crash: ridurre ancora i LOD / rimuovere gli asset non referenziati.
3. **14:00–14:20 · QA in-game**
   - Viste player: piazza, porto, mercato, forte, salita.
   - Fix immediati: travi timpani, scale, vani, mattoni, LOD visibili.
4. **14:20–14:32 · Playtest**
   - Spawn, arma, HUD, collisioni, fori, 60 fps; test 244.
5. **14:32–14:36 · Chiusura**
   - Build finale, `ai/STATE` e docs aggiornati, elenco debiti residui.

## Stato alle 13:50
- Dieta asset fatta; LOD0/1/2 aggiornati; originali fuori da Assets.
- Build batch **exit 0**, **2577 MB, 0 errori, 1 warning**; scena **450.144 tris**.
- Player avviato con `-faleziaShots`: porto, forte, piazza, mercato, molo, alto.
- Fatti: vetro a filo facciata, timpani senza oculi/travi, scale a gradini.
- Restano: **falde in coppi sospese** su edifici di sfondo, **qualche telaio chiaro**, **toppe mattoni** da rifinire.

## Criterio di “pronto”
- `build/linux/Falezia.x86_64` aggiornato e avviabile.
- Scena senza i bug bloccanti mostrati (travi sospese, scale nel muro, vani vuoti).
- `./run.sh test` 244/244, build 0 errori.
