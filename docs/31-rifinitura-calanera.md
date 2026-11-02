# Calanera — piano di rifinitura totale

> Documento di regia della passata finale sulla mappa. Fissa **cosa** deve diventare ogni
> zona, **con quali materiali, luci, oggetti e dettagli**, e **in che ordine** si costruisce.
> È la fonte per i tool (Unity, Meshy, Blender, texture) e per la QA. Non sostituisce
> `07-artstyle`, `23-bibbia-artistica-calanera`, `29-workflow-mappa`, `30-blockout`: li applica.

## Stato al 29/10/2026 (dopo la passata)

- 467.762 tris d'arte, 58 oggetti, **23.474** scatole di collisione, 36 MeshCollider.
- Hero Meshy **dentro** la scena con LODGroup, MeshCollider e AABB: fontana, banco del pesce, barca, bitta, lampione, 3 tavolini, cannone. Rete procedurale al porto.
- Shader **`Falezia/Acqua`** (2 normal map scroll, Fresnel, foam depth, riflesso sonda); texture PBR portate a **4096** (49 mappe); `Forte San Verano` con cortina, merli, portale, feritoie, bandiera, cannoni; persiane chiuse al 60%.
- `Trace.Ray` con **broadphase a griglia** (solo le celle attraversate dal raggio).
- Build: **2547 MB, 0 errori, 1 warning** (pre-baked collision delle mesh con MeshCollider); `./run.sh test` **244/244**. P2 est vestito con PESCHERIA, tenda a strisce, pluviali.
- Debiti: warning residuo; mercato sotto porticato basso; salita con muri ciechi; P2/P8 da rifinire; texture 4K sono upscale, non nuove fonti.

## 0. Stato di partenza (misurato)

- 671 scatole di blockout **sostituite** da arte: 412.788 tris, 55 oggetti, 36 MeshCollider.
- Collisione arte: **19.552 scatole** nel sidecar `calanera.art.json`; blockout spento e
  rimosso dal JSON (strada compresa). I fori dei proiettili usano i MeshCollider dell'arte.
- Città generica già vestita: facciate su lati esposti, tetti, scale, rocce, arredi,
  34 tombini con crepe. Porto Nord hand-made; insegna FALEZIA Meshy; vetri con reflection probe.
- Debiti noti: facciate ripetitive, lati ciechi (palazzo in piazza), timpani chiari, nessun
  interno visibile, pochi arredi hero, trace proiettili su 19.552 scatole, dettaglio fine
  discontinuo (porto curato, città a macchia).

**Obiettivo:** portare ogni zona allo stesso livello del porto, con identità riconoscibile,
dettaglio continuo, materiali PBR di alta qualità, arredi hero, luce e atmosfera, senza
perdere la leggibilità competitiva (D4) e il budget (D7).

## 1. Visione e pilastri

**Calanera è un porto ligure di giorno, vivo e consumato dal sale.** Non è una città fantasy:
è un posto che esiste, con muri rifatti, ringhiere rugginose, casse di pesce, neon accesi
anche di giorno nelle zone in ombra. La bellezza viene da **proporzioni, materiali e
stratificazione**, non dal numero di oggetti.

1. **Leggibilità prima di tutto.** Ogni zona ha una silhouette e un colore dominante
   riconoscibili in 0,3 s. Le linee di tiro e i passaggi restano puliti; il dettaglio vive
   sopra i 2,3 m, sui bordi e nelle zone di attesa, non nelle corsie.
2. **Contrasto di valori.** Sole caldo neutro, ombre fresche, interni/neon come accenti.
   Niente velo giallo globale: il giallo è un materiale (ocra, coppi), non un grading.
3. **Materia vera.** Intonaco con rifacimenti, pietra con salnitro, ferro con ruggine,
   legno con venature, vetro che riflette il cielo. Ogni superficie ha una storia.
4. **Dettaglio a strati.** Macro (quartiere) → medio (edificio) → micro (davanzale, cardine,
   crepa, tombino, cavo). Il giocatore vede il macro, il fotogramma vede il micro.
5. **Colore saturo = informazione.** Le tinte accese restano riservate a squadre, objective,
   pericolo e neon-landmark. L'ambiente è desaturato caldo/freddo (D4).
6. **Performance è stile.** 60 fps, ≤900 draw call, ≤1,2 M tris. Il dettaglio si paga con
   atlanti, trim sheet, LOD, static batching, non con draw call impazzite.

## 2. Palette, materiali, finiture

| Famiglia | Tinte | Uso | Finitura |
|---|---|---|---|
| Intonaco avorio | `#E8DDC8` → `#C9B99A` | palazzi, case | opaco, macchie di umido, rifacimenti |
| Intonaco ocra | `#D9A96B` → `#B77F4A` | case del mercato, fondali | opaco, salnitro, crepe |
| Intonaco rosa | `#D8A79A` → `#B97F70` | contorno piazza | opaco, scrostature |
| Pietra calcarea | `#CFC6B4` → `#8F8778` | zoccoli, portali, cornici, molo | spaccata, levigata nei bordi |
| Pietra scura | `#6F6A60` → `#45423C` | scogliera, contrafforti | ruvida, alghe, salnitro |
| Mattone | `#9C5B43` → `#6E3D2E` | rappezzi, archi, ciminiere | irregolare, malta |
| Cotto | `#B65A3C` → `#8A3F2B` | tetti, grondaie | canali, muschio, coppi mancanti |
| Legno | `#8A6A45` → `#4E3826` | porte, casse, pallet, barche | venato, vernice scrostata |
| Ferro/acciaio | `#3B3B3A` → `#6B6152` | ringhiere, lampade, bitte, container | ruggine, sale, vernice |
| Vetro | blu-verde scuro | finestre, vetrine, lanterne | riflesso cielo, sporco, tende |
| Acqua | `#1E5A63` → `#0C3840` | mare, porto | onde corte, schiuma, riflessi |
| Neon | bianco-azzurro freddo | FALEZIA e insegne | HDR, flicker, luce proiettata |
| Gameplay | ciano/oro/rosso | squadre, objective, pericolo | saturo, pulito, non ambientale |

**Regole materiali**
- Ogni famiglia ha **3 varianti** (pulita, consumata, rovinata) + una **trim sheet** per
  cornici/davanzali/zoccoli. Niente materiali unici per singolo oggetto se si può atlante.
- **Scala reale**: 1 unità = 1 m; tile 2 m per intonaco, 1,2 m pietra, 0,8 m legno.
- **Orientamento**: normal map OpenGL (+Y verde), world-planar UV sul guscio, UV0 per decal.
- **Dirt pass**: ogni materiale ha maschera di sporco (R umido, G sale, B ruggine, A polvere)
  per shader/vertex paint; il porto usa già `Falezia/Superficie`.
- **Decal**: crepe, macchie olio, pozzanghere, ruggine, salnitro, poster, graffiti, stencil,
  numeri civici, targhe, strisce, fori proiettili; tutte con alpha e render queue controllata.

## 3. Luce, atmosfera, post

- **Sole**: 38° O→E, colore `#FFF7F0`, 1,00; ombre 110 m, 4 cascate, 4096, soft High.
- **Ambiente**: cielo freddo, rimbalzo caldo del selciato, terra scura; 0,85.
- **Nebbia**: 70→330 m, `#C9CFD1`; non toccata dai preset.
- **Post**: ACES, bloom 0,06/1,35, contrasto 10, saturazione 4, esposizione −0,15,
  white balance −12, vignetta 0,08. **Niente** giallo globale.
- **Riflessi**: reflection probe `Riflessi Porto` (realtime 256) + vetro metallico; aggiungere
  una sonda per piazza/sud e una per il molo, oppure SSR se disponibile.
- **Pratiche**: lampade a braccio calde (2700 K) accese nelle ombre, neon freddi, finestre
  interne con luce calda nelle ore basse, lanterne del porto, luci di servizio sui moli.
- **Acqua**: shader con profondità, schiuma sulla banchina, riflessi cielo, increspature;
  nessun piano piatto.
- **Atmosfera**: polvere sottile nei fasci di sole, gabbiani, fumo di pescheria, vapore nei
  vicoli, foglie/immondizia che rotolano; particelle leggere, mai davanti al mirino.

## 4. Linguaggio architettonico

- **Palazzi liguri** 3–5 piani, facciate piatte, finestre con persiane, portali in pietra,
  cornici marcapiano, balconi con ringhiere in ferro, tetti a due falde in cotto, comignoli.
- **Case del porto** 2–3 piani, intonaco ocra/rosa, zoccolo in pietra, persiane verdi/ardesia,
  inferriate, vasi, panni, condizionatori, targhe.
- **Magazzini** capannoni in lamiera (verde, grigio, ruggine), portoni scorrevoli, baie di
  carico, bocche di lupo, grigliati, tubi, lampade industriali, insegne dipinte.
- **Capitaneria/Dogana** edifici civici in pietra/intonaco chiaro, cornicioni, stemmi, orologio,
  bandiera, gradinata, lampioni.
- **Forte San Verano** bastioni in pietra, feritoie, portale, ponte/rampa, cortile, corpo di
  guardia, cannoni, polveriera, bandiera, vegetazione costiera.
- **Molo/banchina** pietra, cordoli, bitte, anelli, catene, gomme, bitte di ormeggio, gru,
  container, barche, reti, casse, gabbiani.
- **Piazza** lastricato, fontana, caffè, sedute, alberi/piante, edicola, orologio, lampioni,
  portici, negozi con vetrine e tende.
- **Mercato** struttura aperta con pilastri, travi, teli, banchi, cassette, ghiaccio, pesce,
  verdure, bilance, lampade appese, insegne, carriole.

## 5. Le zone, nel dettaglio

### 5.1 Porto Vecchio (spawn Nord)
- **Identità**: il porto dei magazzini; FALEZIA; pescheria; casse, reti, nafta, sale.
- **Da fare**: chiudere i lati ciechi dei magazzini con baie di carico, tubi, scale a pioli,
  grigliati, lampade industriali, stencil, ruggine sotto i pluviali; aggiungere barche,
  bitte, gomme, cime, reti, cassette, ghiaccio, bilance; dettagliare il molo.
- **Hero**: insegna FALEZIA Meshy + lampada a braccio + barca + cassa di pesce + rete.
- **Luce**: neon freddo, lampade calde sotto i portici, interni dei magazzini caldi.
- **Collisione**: banchina, bitte, casse, barche (se a terra); molo.

### 5.2 Piazza Vecchia
- **Identità**: il salotto civico; fontana, caffè, palazzi, lastricato, ombra.
- **Da fare**: fontana in pietra (hero Meshy), caffè con tavolini/sedie/ombrelloni, sedute,
  alberi/piante, edicola, orologio, portici, negozi con vetrine e tende, sampietrini con
  disegno, tombini e crepe, lampioni, gente? (no personaggi).
- **Lati ciechi**: il palazzo in piazza va vestito su **tutti** i lati visibili (finestre,
  portale, cornice, tetto, pluviali, lampada).
- **Luce**: sole diretto, ombra dei portici, lampioni caldi, vetrine.
- **Hero**: fontana, tavolini, lampione, orologio.

### 5.3 Mercato
- **Identità**: il mercato del pesce e delle verdure; teli, cassette, ghiaccio, colori.
- **Da fare**: banchi in legno con teli a strisce, cassette di pesce/verdura, ghiaccio,
  bilance, coltelli, carta, lavagna prezzi, lampade appese, casse, barili, carriole,
  pozzanghere, odori (vapore), gabbiani, insegne, numeri.
- **Hero**: banco del pesce Meshy, cassetta, barile, bilancia.
- **Luce**: teli che filtrano, lampade appese, ombra fresca.

### 5.4 Salita del Porto e Via Stretta
- **Identità**: vicoli verticali, scale, archi, panni, cavi, lampade, piante.
- **Da fare**: scale in pietra con nosing, archi, portoni, inferriate, vasi, panni stesi,
  cavi, lampade, cassette, biciclette, motorini, poster, crepe, erbacce, tombini, scarichi.
- **Luce**: controluce, lampade calde, neon lontani.

### 5.5 Banchina e Molo
- **Identità**: il bordo acqua; bitte, cime, barche, gru, container, faro.
- **Da fare**: bitte in ghisa (Meshy), anelli, catene, gomme, cime, boe, barche, reti,
  casse, ghiaccio, gru, container, faro, scala di risalita, cima di sicurezza, acqua,
  schiuma, alghe, gabbiani.
- **Hero**: bitta, boa, barca, faro (se previsto), gru.
- **Luce**: riflessi, controluce, lampade del molo.

### 5.6 Forte San Verano (spawn Sud)
- **Identità**: forte costiero; pietra, polvere, bandiere, cannoni, ombra.
- **Da fare**: bastioni, feritoie, portale, rampa, cortile, corpo di guardia, cannoni,
  palle, polveriera, bandiera, pozzo, vegetazione, graffiti, catene, lanterne.
- **Hero**: cannone, portale, bandiera.
- **Luce**: sole radente, ombre profonde, lanterne.

### 5.7 Città e tetti
- **Identità**: case, tetti, terrazze, comignoli, panni, antenne, AC, piccioni.
- **Da fare**: variare le facciate generiche (colori, finestre, balconi, insegne), correggere
  i timpani chiari, aggiungere tetti/terrazze/comignoli/antenne/AC/panni/piante, dettagli
  sui lati visibili dall'alto (P7), vasi, grondaie, lucernari.
- **Luce**: tetti in sole, ombre nette.

### 5.8 Acqua e scogliera
- **Identità**: mare, onde corte, schiuma, rocce, alghe.
- **Da fare**: shader acqua, schiuma alla banchina, riflessi, increspature, rocce con strati,
  massi, alghe, detriti, gabbiani.

### 5.9 Interni visibili
- **Identità**: dentro i vetri c'è vita, non nero.
- **Da fare**: bar, pescheria, negozi, magazzini: fondali caldi, scaffali, lampade, banconi,
  casse, specchi, tende; niente geometria walkable, solo **diorama** dietro il vetro.

## 6. Checklist micro-dettagli (da spuntare per zona)

**Suolo**
- sampietrini/lastre con giunti, cordoli, scarichi, tombini in ghisa con coperchio, crepe,
  rappezzi, buche, macchie (olio, pesce, sale, ruggine, vino), pozzanghere, umido, erbacce,
  rifiuti, gusci, reti, cime, strisce, numeri, impronte di pneumatici, foglie, sabbia.

**Muri**
- intonaco scrostato, mattoni a vista, zoccolo in pietra, salnitro, umido, crepe, fori,
  poster, graffiti, stencil, targhe, numeri, cassette posta, campanelli, allarmi, cavi,
  tubi, pluviali, grondaie, lampade, inferriate, vasi, panni, AC, satelliti, tende.

**Tetti**
- coppi, colmo, grondaie, pluviali, comignoli, vasche, lucernari, terrazze, ringhiere, AC,
  pannelli, antenne, piccioni, piante, panni.

**Acqua/banchina**
- bitte, anelli, catene, gomme, cime, boe, reti, casse, ghiaccio, bilance, barche, remi,
  salvagenti, alghe, linea di marea, schiuma, riflessi, gabbiani.

**Mercato**
- banchi, teli, cassette, pesce, verdure, ghiaccio, bilance, coltelli, carta, lavagne,
  lampade, casse, barili, carriole, pozzanghere, vapore.

**Forte**
- pietra, feritoie, portale, ponte, cortile, cannoni, palle, polveriera, bandiera, pozzo,
  lanterne, catene, vegetazione, graffiti.

**Segni**
- FALEZIA neon, BAR, PESCHERIA, GELO, OFFICINA, TRATTORIA, CAPITANERIA, DOGANA, mercato,
  nomi vie, numeri, prezzi, orari, divieti, frecce, strisce.

**Luce**
- sole, ombre, ambiente, nebbia, lampade, neon, vetrine, lanterne, riflessi, acqua,
  particelle, gabbiani, fumo, vapore.

## 7. Asset hero (Meshy/Blender)

Priorità alta (bloccano la resa di zona):
1. **Fontana** della piazza (pietra, ottagonale, zampillo).
2. **Banco del pesce** con tenda a strisce, ghiaccio, cassette, bilancia.
3. **Barca da pesca** piccola (legno, vernice scrostata, remi, reti).
4. **Bitta di ormeggio** in ghisa (base scanalata, fungo).
5. **Lampione** in ghisa con lanterna.
6. **Tavolino + sedie** da caffè (ferro, marmo).
7. **Boa** di pesca (vernice, cima).
8. **Barile** in legno con cerchi.
9. **Cannone** costiero su affusto.
10. **Rete da pesca** (riprovare, con galleggianti).
11. **Coperchio tombino** in ghisa (se il procedurale non basta).
12. **Cassa di pesce** (riuso Meshy esistente, varianti).
13. **Cassetta verdure/limoni** (o texture).
14. **AC/unità tetto** (procedurale/texture).
15. **Portale in pietra** e **balcone** (kit architettonico, Meshy o geometria).

Regole Meshy: stile Valorant-like, PBR, oggetto singolo, nessun testo/terreno/ombra cotta,
scala reale, pulito e leggibile, 2K/4K, LOD; salvare in `art/map-assets/generated/rifinitura/`.
I modelli che non superano la QA restano fuori.

## 8. Texture e materiali

- **PBR 4K**: intonaco (3 varianti), ocra, rosa, pietra chiara, pietra scura, mattone, cotto,
  legno, ferro, lamiera (3 colori), vetro, acqua, cemento, sampietrini, lastre, sabbia.
- **Trim sheet**: cornici, davanzali, zoccoli, modanature, portali, mensole.
- **Decal**: crepe, olio, pozzanghere, ruggine, salnitro, poster, graffiti, stencil, numeri,
  targhe, strisce, fori, sangue? (no), foglie, sabbia, reti.
- **Atlanti**: segnaletica, cassette, etichette, poster, numeri civici; una texture per atlante.
- **Filtri**: nessun giallo globale; correggere i materiali ocra nel shader, non nel post.

## 9. Pipeline tecnica

1. **Architettura per zona**: correggere il generico (lati ciechi, timpani, ripetizione) con
   kit di facciata parametrici; ogni edificio visibile ha dettagli sui lati in vista.
2. **Hero asset**: Meshy → Blender (pulizia/scala/orientamento, LOD, collision proxy) → GLB
   in `Assets/Art/Calanera/Props/Rifinitura/` → prefab → placement in Unity.
3. **Collisioni**: tutte le mesh strutturali nei solidi del sidecar; i prop hero hanno
   collider dedicati (box/capsule) e non 19.552 scatole; il trace dei proiettili va ottimizzato
   con la griglia del mondo, non scorrendo tutto.
4. **Fori proiettili**: MeshCollider sulle mesh visibili, BoxCollider blockout spenti, decal
   pool; verifica su muri, pietra, ferro, legno.
5. **LOD**: hero 3 LOD (0/1/2), edifici 2 LOD, decal cull; static batching, atlanti, GPU instancing
   per props ripetuti.
6. **QA**: per ogni zona 3 viste (occhio, larga, dettaglio), controllo collisioni, fori,
   z-fighting, floating, color, 60 fps; `./run.sh test`; build; docs.

## 10. Performance e budget

- ≤900 draw call, ≤1,2 M tris, texture ≤2 GB, 60 fps su GTX 970 a 1080p.
- Collisioni: ridurre le 19.552 scatole con merge dei solidi e proxy per props; trace con
  griglia; il server non deve scansionare tutta la mappa per ogni proiettile.
- Ombre: 4096/4 cascate; niente ombre sui dettagli < 20 cm; SSAO pulito.
- LOD e culling per la città; i tetti lontani semplificati.

## 11. Fasi

- **P0 — Stile lock**: questo documento, palette, riferimenti, acceptance per zona. ✔
- **P1 — Materiali/architettura**: library v2, kit facciate, correzione lati ciechi/timpani,
  varianti edifici, tetti, interni visibili.
- **P2 — Hero asset**: generazione Meshy in parallelo, QA, import, prefab, placement.
- **P3 — Micro-dettagli**: suolo, muri, tetti, acqua, mercato, forte, segni, decal.
- **P4 — Luce/atmosfera**: lampade, neon, finestre, sonde, acqua, particelle, post.
- **P5 — Collisioni/fori/perf**: sidecar, collider prop, trace, LOD, batching.
- **P6 — QA/build/docs**: viste per zona, test, build, aggiornamento `ai/` e `docs/`.

## 12. Criteri di accettazione

- Nessun blockout visibile; nessun lato cieco nelle viste di gioco; nessun oggetto flottante.
- Ogni zona ha almeno **un landmark** e **un materiale/colore dominante** distinti.
- Dettaglio continuo a 0–3 m e a 3–20 m; niente texture stirate o scala sbagliata.
- Collisioni allineate alle mesh visibili; fori proiettili su tutte le superfici strutturali.
- Palette D4 rispettata; nessun velo giallo; neon e luci calde leggibili.
- 60 fps nel percorso di gioco; budget rispettato; `./run.sh test` verde; build 0 errori.
- `ai/STATE.md` e `docs/` aggiornati con ciò che esiste davvero.
