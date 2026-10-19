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
| **LOD a 3–4 stadi + impostor** oltre i 120 m | Obbligatorio per una mappa di 240 m |
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
| Ombre | Dinamiche, mappa 4096, distanza 100 m |
| Post-processing | SSAO basso, bloom basso, TAA opzionale |
| Decal | ≤ 64 |
| Particelle | ≤ 1.024 |
| Texture | 2048 sulle superfici primarie |

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
| **Streaming asincrono di ogni asset per default** | Per una mappa di 240×220 m è la differenza fra "carico un livello" e "lo streammo" |
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
