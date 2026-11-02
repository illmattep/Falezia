# Cielo di Calanera

In questa cartella c'è **`sky_panorama.jpg`**, generata da `tools/skybox/make_sky.py`
(4096 × 2048, sole a Ovest come la luce del gioco, mare sotto l'orizzonte). È il cielo che
il gioco usa adesso.

Vuoi mettere il tuo — per esempio lo skybox di Sketchfab? **Basta copiarlo qui** con uno dei
nomi elencati sotto: vince lui, senza toccare una riga di codice. Se non c'è nessun file,
resta il cielo procedurale (`Atmosphere.ProceduralSky()` in `game/client/MapView.cs`).

## 1 · Che immagine serve

Godot vuole una **panorama equirettangolare**: una sola immagine larga il doppio
dell'altezza (2:1), dove la riga centrale è l'orizzonte.

| | |
|---|---|
| Formato | `.jpg` o `.png` (LDR) · `.exr` o `.hdr` (HDR, se il cielo ha un sole vero) |
| Dimensione | **4096 × 2048** è lo standard; 8192 × 4096 solo se hai VRAM da spendere |
| Colore | LDR in sRGB (default di Godot). Un JPG di Sketchfab va già bene così |

## 2 · Da Sketchfab

1. Apri il modello, **Download 3D Model** → *Original* o *Autoconverted* (serve l'account,
   il download gratuito basta);
2. guarda cosa c'è nello zip:
   - **una sola immagine 2:1** → è già una panorama, rinominata e sei a posto;
   - **sei immagini** (`px`, `nx`, `py`, `ny`, `pz`, `nz`, oppure `right`/`left`/`up`/`down`/
     `front`/`back`) → sono le facce di un cubo: vanno unite;
   - **una sfera o un `.blend`** → non serve il modello, serve solo la sua texture: cercala
     dentro lo zip (`textures/`).

### Se sono sei facce

```bash
python3 tools/skybox/cubemap_to_panorama.py cartella_delle_facce/ \
        --out game/assets/sky/sky_panorama.jpg --width 4096
```

Lo script riconosce i nomi delle facce anche con prefissi (`basic_sky_px.jpg`) e ha due
sole correzioni se il risultato non torna: `--yaw 90` (ruota) e `--flip-v` (capovolge).
Con `--test` fa una prova interna senza file.

## 3 · Installazione

Metti il file qui con uno di questi nomi — **il primo che esiste vince**, quindi un `.png`,
`.exr` o `.hdr` battuto da te ha la precedenza sul `.jpg` generato:

```
sky_panorama.exr
sky_panorama.hdr
sky_panorama.png
sky_panorama.jpg
sky_panorama.webp
```

Poi **apri l'editor una volta** (importa la texture: `./run.sh editor`) e avvia il gioco.
Nei log compare:

```
[cielo] panorama: res://assets/sky/sky_panorama.jpg (4096x2048)
```

Se non compare, il file non è stato importato o il nome non è fra quelli sopra: il gioco
resta sul cielo procedurale senza rompersi.

## 4 · Allineare il sole

Il sole del gioco è una luce **direzionale** (`Sun` in `Atmosphere.Create`), il cielo è
solo quello che si vede. Se la panorama ha un sole dipinto, i due devono coincidere,
altrimenti si vedono due soli.

- Ruota il cielo: `Atmosphere.SkyYawDeg` (gradi, in `MapView.cs`) finché il sole dipinto
  non finisce dove punta la luce. La luce da Ovest è una scelta di gameplay (doc 07):
  **si muove il cielo, non la luce**.
- Se la panorama è HDR e il sole è fortissimo, abbassa `PanoramaSkyMaterial.EnergyMultiplier`
  (in `LoadPanorama`) invece di scurire l'immagine.

## 5 · Rigenerare il cielo di casa

```bash
./run.sh sky make                       # riscrive sky_panorama.jpg a 4096×2048
./run.sh sky make --width 8192 --out /tmp/prova.jpg
```

Il sole della panorama è calcolato sulla stessa direzione della luce direzionale
(`DirectionalLight3D` a −42° / −90°): se cambi l'una, rigenera l'altro.

## 6 · Quando il cielo non convince

| Sintomo | Cosa toccare |
|---|---|
| Scena troppo chiara o troppo blu | `Environment.AmbientLightSkyContribution` (oggi 0,45) e `AmbientLightEnergy` |
| Orizzonte che stona con la foschia | `Environment.FogLightColor`: va preso dal cielo, non scelto a caso |
| Cielo sfocato | è `RadianceSize` (oggi 2048): alzalo a 4096 se la GPU regge |
| Cuciture visibili ai poli | `PanoramaSkyMaterial.Filter` è già attivo; se resta, la panorama ha i poli rotti |
