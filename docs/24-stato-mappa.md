# Stato della mappa — handoff

## Cosa è fatto e verificato

**Generatore** (`tools/map-assets/building.py`)
- Parete a **griglia**: si dividono quote e ascisse sugli spigoli delle aperture, si emette una
  scatola per ogni cella che non sta dentro un vano. Il muro esiste per costruzione.
- Test di aderenza: esclude la casa stessa, tolleranza 35 cm, misura la sovrapposizione del lato.
  I lati in aderenza diventano **muri ciechi** (senza finestre, spessi 62 cm).
- Il **lato principale non è mai cieco**.
- Finestre 1,15 × 1,55, interasse 2,8 m, vano incassato 16 cm, davanzale 14 cm, architrave,
  vetro chiaro a filo del muro.
- Porte 1,20 × 2,35 con stipiti e gradino; balconi con soletta e ringhiera; tetti a falde con
  colmo, fascioni e comignolo.
- Piani **arrotondati** al valore più vicino (ultima fila di finestre a filo della gronda).
- Materiali dipinti: 4 intonaci puliti (`plaster_p1..4`) e 4 danneggiati (`plaster_d1..4`,
  con pietra a vista, colature, umido alla base). Un edificio su tre usa i danneggiati.
  Tinte per edificio + palette per zona (`ZONES` in building.py).

**Export e integrazione**
- 4 chunk per posizione: `Assets/StreamingAssets/map/skin/{nord_ovest,nord_est,sud_ovest,sud_est}.glb`
  (**106 MB** totali), referenziati in `props.json`.
- `MapBuilder.HideBuildings = true`: il blockout non si disegna più, la pelle lo sostituisce.
- Verifica in gioco: **1.713 oggetti, 5,3 s, 0 eccezioni**, build 726 MB.

## Come si rigenera (ciclo rodato)

```bash
# 1. Blender acceso con MCP
blender --python /tmp/fz/blender_mcp_start.py &
# 2. quattro chunk da ~90 lotti (NON una chiamata unica: si impianta)
python3 /tmp/fz/run.py /tmp/fz/chunks_pos.py
# 3. props.json: i 4 chunk in map/skin/, poi
Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod Falezia.EditorTools.FaleziaBuild.BuildLinux
# 4. screenshot
build/linux/Falezia.x86_64 -faleziaShots /tmp/shots -screen-width 1400 -screen-height 850
```

Verifica rapida senza Unity: `facade_test.py` (una facciata a 6 m) e `town_small.py`
(un distretto, 67 edifici intorno alla Piazza) renderizzati in Cycles.

## Cosa resta

1. **Dettagli di zona** (aggiunta, non fondamenta):
   - Piazza: porticato con archi veri sopra i volumi `cover`;
   - Porto: banchina, gru a traliccio, bitte, capannone in lamiera;
   - Forte: merlature continue sul muro a z≈96, porta carraia, torri, bracieri;
   - Vicoli: lampade a muro, targhe, vasi.
2. **Difetti minori**: qualche muro cieco ancora chiaro; scale del blockout sospese.
3. **Rifiniture Unity**: effetti e animazioni (a mappa finita).

## Note che fanno risparmiare tempo

- Selezionare i lotti **per posizione**, mai per indice.
- Un export per volta da ~90 edifici: oltre, Blender si blocca.
- Prima di un render, verificare i numeri (poligoni per materiale, quote dei pannelli):
  è così che si trovano i bug veri, non a intuito sul render.

## Aggiornamento (ultima sessione)

**Aggiunto e verificato in gioco**
- **UV offset per edificio e per materiale**: le texture (intonaco, pietra, coppi, legno) non si
  allineano mai uguali fra case vicine. È in `make(name, bm, material, off)` in building.py.
- **Schemi di finestre variabili**: 2-4 per piano secondo larghezza e hash della posizione,
  alcune case con finestra larga al piano terra (`rnd` in `build`).
- **`dettagli.glb` (0,94 MB)**: 6 panche legno+ferro, 5 pozzanghere, rifiuti in 4 punti,
  3 graffiti, 4 insegne, 4 vasi, **gatto di pietra con targhetta** (easter egg) sulla piazza.
- **`merli.glb` (0,89 MB)**: merlature della muraglia sud (z≈96), passo 1,15 m.
- **`portico.glb` (1,76 MB)**: pilastri, archi a tutto sesto, architrave, tettoia.
  Posizione corretta: `Z0 = 4.2` (fondo della piazza). Con `Z0 = -8.4` tagliava la piazza.

**Pelle attuale**: 105 MB, 7 GLB (`nord_ovest`, `nord_est`, `sud_ovest`, `sud_est`,
`merli`, `dettagli`, `portico`). In gioco: 1.716 oggetti, ~5,4 s, 0 eccezioni, build 725 MB.

**Resta**
1. Porto: banchina, gru a traliccio, bitte, capannone in lamiera.
2. Vicoli: lampade a muro, targhe, panni stesi.
3. Archi del porticato più profondi (0,45 m ora) e con ghiera in risalto.
4. Rifiniture Unity: effetti e animazioni.

**Trappola nota**: `props.json` è la lista degli oggetti in scena; per togliere un pezzo
basta rimuovere la sua riga e ricostruire. Per riposizionarlo, modificare le coordinate
nello script che lo genera e riesportare.
