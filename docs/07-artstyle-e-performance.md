# 07 · Artstyle e performance

## La direzione artistica

> **Realismo proporzionale, densità bassa.**
> Proporzioni 1:1 vere, dettaglio minimo. Il realismo viene dalla **luce**, dalla **scala** e dai
> **materiali**, non dal numero di poligoni.

Un vicolo di Calanera deve sembrare un vicolo italiano vero perché è **largo 2,4 metri come un
vicolo italiano vero**, non perché ha 40.000 triangoli di intonaco crepato.

---

## Riferimenti visivi

| Riferimento | Cosa prendiamo |
|---|---|
| **Sniper Elite 5** (mappe italiane) | Il paese costiero: intonaco caldo, persiane, salite, terrazze |
| **Arma Reforger** (Everon) | La scala credibile e la leggibilità del terreno |
| **Ready or Not** | La pulizia degli interni e la leggibilità degli angoli |
| **Valorant** | La disciplina: colore = informazione, mai decorazione |
| **Insurgency: Sandstorm** | Il peso e la brutalità sobria del combattimento |

---

## La palette

| Uso | Colore | Codice |
|---|---|---|
| **Squadra Nord** | Ciano | `#2E9BE6` |
| **Squadra Sud** | Arancio | `#E8632E` |
| **Obiettivo / segnale** | Giallo | `#F2C230` |
| **Pericolo** | Rosso | `#D93B3B` |
| **Pietra calcarea** | Base chiara | `#C9BFA8` |
| **Intonaco caldo** | Base ocra | `#D8A97A` |
| **Intonaco ocra** | Base satura | `#C98A4B` |
| **Cemento** | Base fredda | `#8C8880` |
| **Acciaio** | Base metallica | `#55585C` |
| **Terra e coppi** | Base della copertura | `#9C5B3F` |

### La regola del colore

> **Il colore saturo è riservato all'informazione di gioco. L'ambiente è desaturato.**

Se un muro è rosso, deve significare qualcosa. Se un oggetto è giallo, deve essere un obiettivo.
Questa singola regola fa risparmiare mesi di testing sulla leggibilità e rende il gioco
comprensibile anche a 1080p con texture piccole.

---

## Regole di produzione degli asset

| Regola | Dettaglio |
|---|---|
| **Regola del 3 cm** | Sotto i 3 cm non si modella nulla: il dettaglio è solo texture |
| **Proporzioni reali** | Porte 2,05 m · piani 2,80 m · alzata scale 17 cm · parapetti 1,05 m |
| **6–8 kit di edifici** | Ogni edificio = combinazione di kit su griglia da 4 m |
| **4 kit di muri · 3 kit di tetti** | Ricombinati all'infinito |
| **70% gusci chiusi / 30% interni giocabili** | I gusci hanno finestre finte e nessun interno. Gli interni giocabili sono scelti per gameplay |
| **8 materiali master × varianti colore** | Intonaco chiaro, intonaco caldo, pietra, mattone, cemento, metallo verniciato, legno, vetro |
| **Prop singolo ≤ 3.000 triangoli** | Sedie, casse, reti, boe: tutto a basso conteggio |
| **Personaggio 12–18 k triangoli** | Nessuna simulazione di stoffa, animazioni pre-bakate |
| **Nessun asset unico** | Se non è riciclabile almeno 4 volte, non entra nella mappa |

---

## Le armi tecniche

| Tecnica | Effetto |
|---|---|
| **MultiMeshInstance3D** per ogni prop ripetuto | 1 draw call per tipo (sedie, rocce, casse, vegetazione) |
| **LightmapGI bakata** | Luce statica a costo runtime ≈ 0. È la chiave per far sembrare bella una mappa a basso dettaglio |
| **Blob shadow** sui preset bassi | Le ombre dinamiche dei personaggi costano quanto 20 giocatori |
| **Foschia mediterranea come culling** | 150 m al preset basso, 400 m all'alto. **Si giustifica da sola: è il mare** |
| **LOD a 3–4 stadi + impostor** oltre i 120 m | Obbligatorio per una mappa di 180 × 200 m |
| **Occlusion culling con poligoni** | Meno utile su una mappa aperta: la foschia è il vero culling |
| **Atlanti di texture e atlanti di materiali** | Massimo 8 materiali per zona |
| **Vertex color per le varianti** | Invece di texture duplicate |
| **Vegetazione in MultiMesh con alpha-test, mai alpha-blend** | L'alpha-blend è la morte del fill rate |

---

## Budget tecnico

### Preset BASSO — GTX 970 · 1080p · 60 fps

| Voce | Budget |
|---|---|
| **GPU per frame** | 16,6 ms totali → **target ≤ 8,0 ms di GPU** |
| Draw call | **≤ 900** |
| Triangoli visibili | **≤ 1,2 M** |
| Distanza di visibilità utile | **150 m** |
| LOD | 3 stadi + impostor da 120 m |
| Illuminazione | LightmapGI bakata, 1 luce direzionale |
| Ombre | **Blob shadow** sui personaggi. Nessuna ombra dinamica dai prop |
| Post-processing | **Nessuno**, solo FXAA |
| Decal | ≤ 16 |
| Particelle | ≤ 256 |
| Texture | 1024 max sulle superfici primarie |
| Renderer Godot | Forward+ con clustering ridotto al minimo |

### Preset ALTO — GTX 1070 Ti · 1080p · 120 fps

| Voce | Budget |
|---|---|
| **GPU per frame** | 8,3 ms totali → **target ≤ 5,5 ms di GPU** |
| Draw call | **≤ 1.500** |
| Triangoli visibili | **≤ 2,0 M** |
| Distanza di visibilità utile | **400 m** |
| LOD | 4 stadi + impostor da 180 m |
| Ombre | Dinamiche, **atlante 8192**, 4 split (0,08 / 0,22 / 0,48), distanza 90 m, **filtro SoftHigh** |
| Post-processing | SSAO basso, bloom basso, TAA opzionale |
| Decal | ≤ 64 |
| Particelle | ≤ 1.024 |
| Texture | 2048 sulle superfici primarie |

### Le ombre (seconda passata)

Le ombre erano morbide e puntinate perché il filtro morbido era su **SoftLow** (un solo tap),
l'atlante a 4096 su 100 m e gli split larghi: ogni metro quadro aveva pochi texel e la penombra
era rumore. Ora, con il preset **ALTO**:

| Parametro | Valore | Perché |
|---|---|---|
| Atlante direzionale | **8192** (16 bit) | 2048 texel per split: i contorni restano netti anche a 20 m |
| Split | 4, a **0,08 / 0,22 / 0,48** | i primi 7 m (dove si guarda davvero) hanno 4–5× i texel di prima |
| Distanza | **90 m** + `fade_start` 0,92 | oltre, l'ombra si spegne senza stacco visibile |
| Filtro morbido | **SoftHigh** | niente più puntinato: la penombra è continua |
| `shadow_blur` / angolo | **0,35** / **0,35°** | filo definito con una penombra credibile che si allarga con la distanza |
| Bias | 0,022 / 0,8 | niente "peter-panning" né acne |

Il **viewmodel non proietta ombre**: l'arma è attaccata alla camera e la sua ombra finiva sul muro
come una macchia puntinata dietro al mirino (si vedeva chiaramente in prima persona). I preset
**BASSO** usano 4096 + SoftMedium, **PRO** nessuna ombra dinamica.

> **Nota competitiva:** un preset "PRO" ufficiale che **disattiva tutto** (post-processing, ombre,
> dettagli) è obbligatorio. I giocatori competitivi vogliono il frame rate e la leggibilità, non la
> bellezza. Ufficializzare il preset significa non perdere tempo a bilanciare ciò che verrà spento.

### Target di SVILUPPO (graybox, pre-alpha)

| Voce | Budget |
|---|---|
| Draw call | **≤ 200** |
| Triangoli | **≤ 300 k** |
| Texture | **Zero** — checker e scale di grigi |
| Post-processing | **Zero** |
| Illuminazione | Una luce direzionale, nessuna bake |

> **Durante lo sviluppo ci si tiene bassi.** La qualità grafica si alza *dopo* che il gioco diverte
> in graybox. Se il gioco non funziona con i rettangoli grigi, non funzionerà con l'intonaco ocra.

---

## Motore e rendering: cosa conta davvero per questa direzione artistica

La direzione artistica — *realismo proporzionale, densità bassa* — **mette tutto il peso visivo sulla
luce**: non sui poligoni, non sul numero di materiali, non sul post-processing. Quindi la domanda sul
motore si riduce a una sola:

> **Quale motore cuoce meglio la luce e dà illuminazione indiretta a minor costo?**

### Flax Engine — feature verificate dalla fonte *(11 ottobre 2026)*

| Feature | Perché conta per noi |
|---|---|
| **GPU Lightmaps Baking** | **Tutta la nostra luce è bakata.** Cucire su GPU significa iterare in **minuti invece che in ore** — e con un'artista sola, il tempo di bake è tempo di produzione |
| **Realtime GI: DDGI + riflessi con raytracing *software*** | **Non richiede hardware RTX**, quindi gira sul tier della GTX 970 e brilla sul 1070 Ti. È la luce indiretta che rimbalza sui muri ocra: **la cosa che più di ogni altra fa sembrare vero un asset a basso dettaglio** |
| **Streaming asincrono di ogni asset per default** | Per una mappa di 180×200 m è la differenza fra "carico un livello" e "lo streammo" |
| **Fog + open world tools** | La foschia mediterranea è la nostra atmosfera **e** il nostro culling principale |
| Materiali e specularità curati | Con poco dettaglio geometrico **sono i materiali a portare il look** |

### Godot 4 — il quadro

| Feature | Nota |
|---|---|
| **Tre renderer** (Forward+, Mobile, Compatibility) | ✅ **Il vero vantaggio**: un interruttore di bassa qualità già collaudato per la GTX 970 |
| `LightmapGI` | ⚠️ Baker **CPU** e notoriamente capriccioso su cuciture e artefatti |
| `SDFGI` (GI realtime software) | ⚠️ Costosa e morbida |
| `VoxelGI` | ⚠️ Risoluzione limitata |
| Fog volumetrico | ✅ Buono: la foschia è coperta |
| Documentazione e community | ✅ Molto più grandi |

> **Conclusione grafica onesta:** **Flax vince sull'asse che per noi conta più di tutti — la luce.**
> La nostra direzione artistica *pretende* buona illuminazione indiretta: senza, un paese a basso
> dettaglio resta un graybox per sempre. E Godot ha nella GI il suo punto più debole.
>
> **Ma il vantaggio grafico non salva il progetto se non gira un server dedicato**, perché un gioco
> senza server non esce. Quindi **lo spike in doc 08 resta la prima cosa da fare** — ora però con un
> motivo concreto per sperare che passi.

*(Le feature di Flax sono verificate dal sito ufficiale. Il quadro di Godot è una mia valutazione,
non riverificata in questa sessione.)*

---

## Stato del rendering nel codice (ottobre 2026)

| Voce | Scelta | Perché |
|---|---|---|
| **Renderer** | **Forward+**, con ripiego automatico su OpenGL (Compatibility) se Vulkan manca | È quello indicato sopra per i preset; il ripiego tiene in vita le macchine senza Vulkan |
| **Graybox** | Mesh fuse per settore da 48 m: **una draw call per settore**, non una per scatola | Budget di sviluppo ≤ 200 draw call |
| **Materiale** | **Uno shader, tre materiali** (parete, facciata con finestre, tetto): palette di questo documento + **griglia a 1 m e 4 m** proiettata nel mondo | Si leggono distanze e proporzioni senza texture (A0). Colore e rugosità viaggiano negli UV; la conversione sRGB→lineare la fa lo shader secondo il renderer |
| **Luce** | Sole caldo **da Ovest**, cielo procedurale, ambiente metà cielo e metà rimbalzo caldo, tonemap Filmic | Il sole perpendicolare all'asse Nord–Sud rende la luce **simmetrica**: nessuna squadra gioca controluce |
| **Foschia** | Di profondità, colore dell'orizzonte, fine = distanza di visibilità; il piano lontano della camera la segue | È atmosfera e culling insieme |
| **Preset** | PRO · BASSO · ALTO come nelle tabelle sopra, più PERSONALIZZATO | Il preset non tocca finestra, V-Sync, FPS e FOV: sono preferenze, non qualità |
| **Latenza** | "Bassa latenza (stile Reflex)": con V-Sync limite a refresh − 3, senza V-Sync limite al 94% di ciò che regge la GPU | **NVIDIA Reflex non è integrato in Godot** (serve l'SDK dentro il motore). Questo ne riproduce l'effetto principale: nessun fotogramma in coda |

> ⚠️ **Da decidere: la distanza di visibilità è un vantaggio di gioco.** Con i numeri di questo
> documento chi gioca in BASSO o PRO vede a 150 m, chi gioca in ALTO a 400 m, e la mappa ha linee
> fino a ~190 m. In un competitivo è un vantaggio comprato con l'hardware. Due strade: foschia
> **uguale per tutti** (e il preset cambia solo LOD e ombre), oppure un tetto comune a 250 m.
> Oggi il codice segue la tabella; il cambio è un numero in `GraphicsSettings.ApplyPreset`.

### Cosa c'è dentro il graybox, oltre alle scatole

| Elemento | Dove | Costo |
|---|---|---|
| **Tetti in coppi**: lastra rientrata di 35 cm, comignoli, corpi scala, serbatoi, antenne | `tools/mapgen/calanera.py` → `roofscape()`, scritti in `decor` | 1.200 scatole di sola decorazione, **zero collisioni**: la simulazione continua a vedere i 671 volumi |
| **Facciate**: finestre 1,1 × 1,6 m distribuite sull'altezza vera, persiane su una finestra su quattro, cornice, ombra del davanzale, zoccolo scuro alla base | shader di `MapView.cs`, ramo `windows_mode` | zero geometria: è colore |
| **Tetti visti dall'alto**: file di coppi da 34 × 42 cm sfalsate di mezza fila | shader, ramo `roof_mode`, materiale a parte | una draw call in più per settore |
| **Terrazze e porticati**: lastroni da 80 cm con giunti | shader, superfici orizzontali sopra 2,2 m | zero |
| **Lastricato**: macchie larghe a terra, solo colore | shader | zero |

> L'altezza del volume viaggia nel **colore dei vertici** (`R = altezza / 64`): è quella che fa
> distribuire i piani delle finestre sull'altezza reale, così l'ultimo piano non finisce mai sul
> cornicione e un corpo basso non mostra finestre tagliate. Le finestre non sono aperture: la
> simulazione non le conosce.

### Il cielo

Il cielo è una **panorama equirettangolare** 4096 × 2048 generata da `tools/skybox/make_sky.py`
(`./run.sh sky make`): sole a Ovest come la luce del gioco, foschia all'orizzonte, mare sotto la
linea. Se in `game/assets/sky/` metti una panorama tua — Sketchfab, HDRI, `.jpg`, `.png`, `.exr` —
**vince la tua**, con `RadianceSize` a 2048 perché anche metallo e vetro delle finestre la
riflettano. Senza nessun file resta il cielo procedurale di scorta.

Il sole resta la **luce direzionale da Ovest**: se la panorama ha un sole dipinto si ruota il
**cielo** (`Atmosphere.SkyYawDeg`), non la luce — la direzione della luce è una scelta di
gameplay (D2: nessuna squadra controluce). Le sei facce di un cubo si uniscono con
`tools/skybox/cubemap_to_panorama.py`; istruzioni passo passo in `game/assets/sky/README.md`.

### Effetti dello sparo

Due shader dedicati, entrambi `unshaded` e in trasparenza, con texture alpha generate da
`tools/fx/make_fx_textures.py` (`./run.sh fx`):

| Effetto | Shader | Come è fatto |
|---|---|---|
| **Scia del proiettile** | `tracer.gdshader` | Cilindro da **7,5 mm**, vita 60 ms. Rumore che scorre lungo l'asse con due strati, nucleo luminoso che si spegne verso la silhouette, punta che evapora. Opacità 0,42 (0,16 la Hush): una scia, non un laser |
| **Fumo di canna** | `muzzle_smoke.gdshader` | Quad billboard con `INV_VIEW_MATRIX`, due strati di texture in controscorrimento, bordo spento prima dello spigolo. **Effetto piccolo**: 2–3 sbuffi scaglionati (0 / 55 / 115 ms), 0,16–0,22 m, densità 0,12–0,22, erosione 1,45–1,75. Mentre sale, la parte alta si sfilaccia (sfumatura verso l'alto) e lo sbuffo **si allarga in orizzontale** più di quanto cresca in altezza. Quattro texture diverse, ritaglio e velocità di scorrimento casuali: non si vede mai due volte lo stesso fumo. La Hush resta a 2 sbuffi quasi trasparenti |

| **Fori sui muri** | `BulletHoles` (quad billboard + texture generate) | Quad di 6–9 cm **che guarda sempre la camera**: non può mai essere visto di taglio (era il difetto delle strisce stirate). Sei texture 160² generate in codice all'avvio (buca, orlo scheggiato, polvere, crepe): nessun foro è uguale a un altro. Posato 6 mm fuori dalla faccia colpita, polvere tinta per materiale. Resta 12 s, poi svanisce in 1,8 s. Pool di 96 |

Il punto d'impatto lo decide sempre `Trace.Ray` nella simulazione: gli shader non sanno dove si
spara, ricevono solo due punti (canna → impatto) e disegnano.

**L'ottica.** Due strati: il **modello** 3D (campana, ghiera zigrinata, paraluce, oculare in gomma,
torrette, anelli con viti, livella) e l'**overlay** `scope.gdshader` — tubo nero, vetro con
vignettatura e riflesso a mezzaluna, reticolo con pali grossi e croce sottile, mildot sul 6×. Il
reticolo "respira": due sinusoidi lente lo spostano di un pixel o due, così non sembra incollato
allo schermo.

**Anteprima fuori dal motore.** `tools/mapgen/calanera_render.py` ricostruisce la mappa in Blender
dagli stessi dati (scatole + decorazione) e ne renderizza cinque viste in `tools/mapgen/review/`
(aerea, strada, piazza, porto, tetti). Le facciate dell'anteprima replicano in nodi lo shader,
comprese finestre, persiane, zoccolo e lastroni: serve a giudicare il look senza aprire Godot.

---

## Piano di produzione artistica

| Fase | Cosa | Quando |
|---|---|---|
| **A0 · Graybox** | Solo volumi. Nessuna texture. Verifica di tempi, rotte e linee di tiro | M0–M2 |
| **A1 · Blocco materiali** | 8 materiali master, palette applicata, luce bakata grezza | M3–M4 |
| **A2 · Kit modulari** | 6–8 kit di edifici, prop in MultiMesh, dettaglio di zona | M5 |
| **A3 · Pass decorativo** | La differenziazione nord/sud, i landmark, il bucato steso, le insegne | M6+ |
| **A4 · Ottimizzazione** | LOD, atlanti, bake finale, verifica del budget su hardware reale | M6+ |

**Regola di produzione:** ogni fase artistica è **annullabile**. Si passa ad A1 solo se il gioco
funziona in A0. Per un team piccolo, questo è ciò che separa un gioco finito da un progetto
abbandonato a metà.

---

## Come verificare il budget (e non mentirsi)

1. **Un cronometro in-game** per i tempi di percorrenza in graybox. Non a occhio.
2. **Il monitor delle prestazioni di Godot** sempre aperto durante lo sviluppo, non solo alla fine.
3. **Un PC di prova con GTX 970** — non un profiler, un PC vero, con un monitor 1080p vero.
4. **Test del caso peggiore:** tutti e 10 i giocatori visibili contemporaneamente in piazza,
   con tutte le trappole piazzate e la foschia al minimo. Se regge lì, regge sempre.
