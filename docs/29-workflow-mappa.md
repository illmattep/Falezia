# 29 · Calanera da capo — il workflow

> Sostituisce la pipeline dei doc 21–28 (pelle unica, kit GLB caricati a runtime, `props.json`).
> Il **blockout resta** (`calanera.json`): forma, collisione, rotte, spawn, obiettivi. Tutto ciò che
> si vede si rifà da zero.

---

## 1. Cosa vogliamo

| Obiettivo | Come si misura |
|---|---|
| **Coerenza**: una libreria di materiali, una luce, una palette (doc 07) | Nessun materiale fuori libreria in scena |
| **Texture dettagliate, non piatte, non stirate** | Densità fissa in pixel/metro su ogni superficie, nessuna ripetizione visibile a 20 m |
| **Materiali che si fondono** dove serve (piede dei muri, intonaco che cade, selciato contro i muri) | Nessuna cucitura netta tranne dove l'architettura la vuole (zoccolo, cornice, soglia) |
| **Modificabile da Unity** | Un edificio si cambia (piani, finestre, tinta) dall'Inspector in meno di un minuto |
| **Ciò che vedi è ciò che collide** | Nessun muro invisibile: la facciata sta sulla faccia della scatola |
| **Budget D7** | Preset basso ≤ 900 draw call, ≤ 1,2 M triangoli |

## 2. Decisioni

1. **Stile: Valorant (riferimento Ascent), niente fotorealismo.** Forme pulite e leggibili,
   colore controllato, dettaglio dipinto ma ricco: mai superfici piatte e uguali per metri.
   La libreria nasce da **tre sorgenti** rese coerenti dalla stessa "forgia"
   (`tools/calanera2/forge.py`):
   - **procedurale** (mattoni, conci, intonaco dipinto): forme disegnate in codice;
   - **Meshy** (texture dipinte piatte di ottobre): diventano PBR vere — altezza ricostruita
     dalle forme, pulizia, palette;
   - **scansioni CC0** (Poly Haven) rese pittoriche (Kuwahara + rampa di colore).
   Ogni materiale esce con albedo, normal **alla scala vera** (rilievo in metri / metri per pixel)
   e maschera con l'**altezza**, che serve alla fusione. Sopra vanno decal, arredi e geometria
   del kit: è lì che sta il grosso del dettaglio.
2. **Uno shader master**, `Falezia/Superficie` (URP, HLSL, illuminazione `UniversalFragmentPBR`):
   - **UV dal mondo** a densità fissa: la texture non dipende dalla scala del pezzo, non si stira;
   - **due strati** (base + sotto-strato) fusi per altezza e rumore del mondo, più lo sporco al
     piede dei muri. Niente hex-tiling, UV3, pittura dei vertici, decal, lightmap o APV: la
     semplificazione ha tolto varianti e lavoro per pixel, e ha reso possibile un `DepthNormals`
     con **normale geometrica** (SSAO pulito, senza rumore di normale map);
   - **variazione larga** (macchie, sole, umido su 10–20 m) e dettaglio da vicino;
   - **qualità scalata dai preset**: ombre (2048→8192, 45→130 m, 2→4 cascate, soft High), SSAO
     (16 campioni, off su BASSO), bloom 0,03→0,08, saturazione 3→5, AA e scala di rendering.
3. **Edifici = kit modulare + generatore in Unity.** Blender fa i moduli (muro, finestra, porta,
   vetrina, cornice, zoccolo, balcone, tetto, comignolo, pluviale) su griglia da 0,5 m. In Unity un
   componente **Edificio** sta sulla scatola del blockout e la riempie **esattamente**: piani,
   schema delle aperture, tinta, varianti, seme → **Ricostruisci**. I pezzi toccati a mano si
   **bloccano** e il generatore non li sovrascrive.
4. **Suolo** generato dal blockout (strade, piazze, gradini, banchine) come mesh unica per zona,
   con **pittura dei vertici** per le transizioni (sampietrini → lastre → terra).
5. **Dettaglio**: decal URP (crepe, umido, manifesti, sporco agli incroci) e arredi Meshy
   ottimizzati (≤ 4k triangoli, materiali della libreria dove possibile).
6. **Il blockout vive nella scena**: scatole con `BoxCollider` e tipo, su un livello che si
   accende e si spegne. Un comando le **esporta in `calanera.json`**; server e test non cambiano.
7. **Luce**: sole, **Adaptive Probe Volumes** (luce indiretta cotta), SSAO, nebbia e grading.

## 3. Strumenti (in tempo reale)

| Strumento | Ruolo |
|---|---|
| **Blender + MCP** | Costruisce i moduli del kit e i props, rende anteprime. Script in `tools/calanera2/` |
| **Unity Editor aperto** + pacchetto `com.unity.pipeline` + Unity CLI | Comandi dal vivo (`unity command "script eval" --code ...`), screenshot della Scene view: le modifiche si vedono mentre si fanno |
| **Strumenti di progetto** (`Assets/Editor/Calanera/`) | Import/export blockout, generatore edifici, pittura, verifica budget |

## 4. Struttura in Unity

```
Assets/Art/Calanera/
  Shaders/        FaleziaSuperficie.shader (+ include)
  Textures/<id>/  albedo, normal, maschera (R metallo · G occlusione · B altezza · A levigatezza)
  Materials/      master per famiglia + varianti di tinta
  Kit/            moduli FBX da Blender
  Prefabs/        moduli e arredi pronti
Assets/Scenes/Calanera.unity
  Blockout · Suolo · Edifici · Dettagli · Luce
Assets/Editor/Calanera/
```

## 5. Fasi

Ogni fase si chiude con **render** e **resoconto breve**.

| Fase | Contenuto | Fine fase |
|---|---|---|
| **F0** banco di lavoro | Unity CLI e Pipeline, scena Calanera con il blockout, export identico al JSON | Screenshot della scena, JSON uguale byte per byte |
| **F1** materiali | Libreria 2K (≈16 materiali), shader master, **baia di prova** (muro 12 m con angolo, zoccolo, finestra, selciato, gradino) | Render vicino/medio/lontano |
| **F2** primo isolato | Kit edificio in Blender, componente Edificio, isolato della Piazza completo | Render della Piazza |
| **F3** tutti gli edifici | Palette di zona (asimmetria cosmetica D2), varianti | Volo sulla città |
| **F4** suolo | Strade, piazze, scale, banchine con fusioni | Render a livello occhi |
| **F5** zone speciali | Porto, Forte e Mura, Grotta e Galleria, Mercato, Molo | Render per zona |
| **F6** dettaglio | Decal, arredi, vegetazione | Render prima/dopo |
| **F7** luce e budget | APV, nebbia, grading, LOD, occlusion culling, verifica D7 | Numeri F8 del gioco |
| **F8** nel gioco | Il client carica la scena; via `props.json`, pelle e kit GLB | Build che parte, test verdi |

## 6. Regole di lavoro

- **D4**: colore saturo solo per l'informazione di gioco. Tinte degli intonaci desaturate.
- **D2**: architettura simmetrica (metà città ruotata), cosmetica diversa fra le due metà.
- **D5**: sotto i 3 cm nessuna geometria, solo texture.
- **Tutto rigenerabile** da script, ma ciò che si ritocca a mano in Unity resta.
- Il budget D7 si misura a ogni fase, non alla fine.

## 7. Stato (28 ottobre 2026)

| Fase | Stato |
|---|---|
| F0 | **Fatta**: `Falezia → Calanera → Blockout: importa / esporta / verifica`. 671 scatole in `Assets/Scenes/Calanera.unity`; andata e ritorno **identica byte per byte**. Spawn e punti visibili; luce e viste fisse |
| F1 | **Fatta**: 8 materiali, shader `Falezia/Superficie`, banco di prova (`Falezia → Calanera → Banco di prova`), foto automatiche (`Foto delle viste`), SSAO |
| F2 | **In corso — Spawn Nord (Porto Vecchio)**: piazzale, 5 piazzole, 2 container, 2 pile di casse (con 6 casse Meshy), 4 facciate di magazzino con portoni scorrevoli e **insegna FALEZIA**, 7 edifici con persiane/tetti/tende/insegne, capitaneria, gru 30 m, segnaletica e macchie. `Falezia → Calanera → Spawn Nord: costruisci`; foto `P0…P10` |

### Lezioni già pagate
- **Hex-tiling** sugli intonaci (macchie larghe) fa un camuffamento a esagoni: si usa solo su
  texture fitte (sampietrini); gli intonaci si variano con il rumore del mondo.
- **SSAO**: il passo DepthNormals scritto a mano dava normali sbagliate (tutto nero vicino alla
  camera). Si usa il passo standard di URP (normale geometrica): l'SSAO lavora sulla forma.
- **Filtro "dipinto" a schermo** (Kuwahara): provato, appiattisce poco e costa su ogni pixel.
  Resta spento ma disponibile (`Pittura a schermo`). Il look viene da texture e luce.

### Mappa nel gioco — integrazione (28 ottobre 2026)

- La scena `Calanera.unity` è nei **Build Settings** e `GameRoot` la carica in additivo
  (`useCalaneraScene`, default vero). `MapProps` (vecchio kit GLB) è spento: `props.json` e
  `skin/*.glb` non vengono più letti.
- Il grande volume `street` del blockout è sostituito da **`Suolo Città`**: piano di
  sampietrini 180 × 200 m a −0,02 m (sotto il piazzale, niente z-fighting), banchina in
  pietra e mare a −2 m. Il blockout ha **ombre accese** e materiali di servizio; c'è anche
  `Falezia → Calanera → Blockout: materiali della libreria` per provarlo con le texture
  nuove (poi si torna con "materiali di servizio").
- **Buchi rimasti**: fuori dallo spawn la città è ancora blockout (pareti piatte senza
  finestre); il Sud (Forte San Verano) non è vestito; i varchi mostrano le zone non finite.
  Il piano della città toglie i vuoti a terra, non finge le facciate.

### Mappa intera: dal blockout all'arte (29 ottobre 2026)

- Tutte le **671 scatole** del blockout sono state sostituite: edifici con corpo + facciate su
  ogni lato esposto, tetti, scale, rocce, arredi. Il blockout è **spento e rimosso dal JSON**
  (strada compresa); la collisione viene dal **sidecar** con le scatole delle mesh nuove
  (19.552) e i **MeshCollider** per i fori dei proiettili.
- Dettaglio fine: 34 **tombini in ghisa** con coperchio e crepe, pozzanghere, olio, erbacce,
  poster, crepe sui muri, pluviali, lampade, balconi, insegne, AC, comignoli, cavi.
- Costo: 412.788 tris, 55 oggetti; il trace dei proiettili scorre 19.552 scatole, da ottimizzare
  con la griglia se la CPU sale.

### Insegna Meshy e vetri (29 ottobre 2026)

- **FALEZIA**: sette lettere singole **Meshy 7.1** (image-to-3D) con texture PBR, staffe/piastre/
  canalina in ferro; emissione bianco-azzurra (non gialla); Bebas Neue come reference; A e I
  sfarfallano via `NeonSign`; una luce fredda proietta sulla facciata. Niente pannello unico.
- **Vetri**: un vetro scuro URP senza **reflection probe** né SSR non ha nulla da riflettere
  (resta il solo 4% di Fresnel). Aggiunta la sonda realtime `Riflessi Porto` e reso il vetro
  metallico: ora il cielo si specchia. Se una finestra resta nera è dietro le persiane o fuori
  dal box della sonda.

### Qualità grafica — lezioni (28 ottobre 2026)

- **Rumore SSAO sulle facce piatte**: con `AOMethod = BlueNoise` e `NormalSamples = High`
  l'SSAO lasciava un pulviscolo su volumi lisci in ombra (il basamento della piazza). Ora
  `InterleavedGradient`, normali **Medium**, raggio **0,6**, blur High, 16 campioni: pulito.
  Il test di confronto on/off è in `tools/calanera2/review/2026-10-28-spawn/quality/`.
- **Shader del personaggio**: `Falezia/Toon` (due tonalità + rim light, forward/MSAA, una
  luce e una texture) e materiale `M_Toon_Operatore`; da assegnare all'operatore quando
  si farà il pass dei personaggi.

### Spawn Nord — note di produzione (28 ottobre 2026)

- **Script**: `Assets/Editor/Calanera/CalaneraSpawn*.cs` (geometria, facciate, dettagli). Ogni
  ricostruzione cancella **tutte** le radici `Spawn Nord` (non solo la prima) e rifà le mesh in
  `Assets/Art/Calanera/PortoVecchio/`. Il blockout resta: si nascondono solo i renderer nella zona.
- **Texture**: `tools/calanera2/spawn_segni.py` (insegne, olio, pozzanghere, ruggine, schiuma,
  normal del mare) e `tools/calanera2/porto_suolo.py` (lastre di cemento 1,5 × 1 m, giunti chiari:
  la versione della forgia aveva orli chiari che, di taglio, facevano un tratteggio sul piazzale).
- **Meshy**: una sola generazione usata, le **casse da pesce** (1,0 M → 12k tri, texture 1024, GLB
  in `Assets/Art/Calanera/Props/casse_pesce.glb`, 6 istanze). La generazione delle **reti** è
  fallita al 99% ("generation service temporarily unavailable") dopo ~20 min: da rifare.
- **Insegna FALEZIA**: per ora è una **decal dipinta** (7,5 × 1,2 m) perché le lettere 3D del FBX
  importato (`art/map-assets/generated/insegna_falezia.fbx`) escono con l'asse sbagliato; quando si
  sistema l'orientamento si sostituisce senza toccare il resto.
- **Aperto**: le aree oltre i varchi (Salita/Largo a sud, Banchina a est) sono ancora scatole
  grigie; nel piazzale i **tavolini e le sedie non hanno collisione** (dettaglio, fuori blockout);
  il **Sud (Forte San Verano)** è la metà Nord ruotata con un'altra pelle, da fare.

### Strumenti dal vivo (sul PC di sviluppo)
- `/tmp/fz/u.sh '<comando>' --param valore` → `unity command` sul progetto aperto.
- `/tmp/fz/job.sh <nome> <comando>` → lavoro staccato, log in `/tmp/fz/jobs/<nome>.log`.
- Foto per i resoconti in `tools/calanera2/review/<versione>/`.
