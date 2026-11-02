# Falezia · client Unity

Il client Unity vive **alla radice del repo** (Unity apre `Falezia/` come progetto). La simulazione è la stessa del
progetto Godot: `game/sim` + `game/net` sono C# puro (compilati da `shared/Falezia.Core`), quindi non sanno
niente del motore e li usano client, server e test.

## Cosa è installato (e perché)

| Pacchetto | Versione | Serve per |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.6.0 | pipeline grafica: ombre, HDR, MSAA, post-processing |
| `com.unity.cloud.gltfast` | 6.20.0 | caricare a runtime i GLB delle armi (viewmodel + animazioni) |

Sono nel `Packages/manifest.json`: si installano da soli al primo apertura del progetto.

## Grafica: com'è configurata

Gli asset stanno in `Assets/Settings` e li rigenera `Falezia > Configura URP` (oppure
`-executeMethod Falezia.EditorTools.FaleziaGraphics.Setup`):

| Impostazione | Valore | Dove |
|---|---|---|
| Ombre | 90 m, 4 cascate `0.08 / 0.22 / 0.48`, soft **High** | `URP.asset` |
| Shadow map | 4096 px, depth bias 1, normal bias 1 | `URP.asset` |
| MSAA / HDR | 4× / sì | `URP.asset` |
| Anti-aliasing camera | SMAA alto | `PlayerRig` |
| Colore | tonemap **ACES**, bloom, contrasto, vignetta | `Assets/Resources/Falezia-Volume.asset` |
| Shader sempre inclusi | glTF (armi) + URP Lit/Unlit | `ProjectSettings/GraphicsSettings.asset` |

L'ultima riga è la meno ovvia: mappa e FX creano i materiali **a runtime**, quindi senza
gli shader nella lista "Always Included" la build perde le varianti con le ombre e il mondo
resta illuminato in modo piatto (bug visto e risolto: le armi uscivano perfino magenta).

Il cielo è il colore di sfondo della camera + nebbia lineare 120→400 m; il sole viene
dall'ovest a 42° con ombre soft.

## Primo avvio

1. Unity Hub → **Add project from disk** → questa cartella (`.`).
2. Apri `Assets/Scenes/Main.unity` (c'è già `GameRoot`).
3. Play.

La mappa si carica dalla **scena `Assets/Scenes/Calanera.unity`** (blockout + arte dello
spawn) aggiunta alla scena `Main` da `GameRoot`. Il vecchio kit GLB
(`StreamingAssets/map/props.json` e i `skin/*.glb`) **non si carica più**. Il file
`StreamingAssets/maps/calanera.json` resta la verità della simulazione (collisioni, spawn,
rotte). Per tornare al vecchio percorso: `GameRoot.useCalaneraScene = false`.

## Comandi

`WASD` muoversi · `Shift` sprint · `Ctrl` accovacciato · `Space` salto · `LMB` sparare ·
`RMB` mira · `R` ricarica · `1` assalto · `2` pistola · `3` coltello · `B` negozio ·
`F8` pannello debug · `Esc` libera il mouse.

## Script di sviluppo

```bash
./run.sh unity            # apre il progetto nell'editor
./run.sh build            # player Linux in build/linux/Falezia.x86_64 (~140 MB)
./run.sh play             # avvia il player
./run.sh server           # server dedicato (--port= --max= --map=)
./run.sh test             # 244 test della simulazione e della mappa
```

## Online o offline

Nel componente `GameRoot` (scena `Main`):

- **online = false** → si gioca in locale con tutte le armi (stanza di prova, niente round);
- **online = true** → host/porta del server (default `127.0.0.1:27015`).

Da riga di comando si può forzare senza toccare la scena:

```bash
./run.sh server --port=27015 &
./build/linux/Falezia.x86_64 -faleziaOnline 127.0.0.1:27015
```

Il client manda solo input e richieste d'acquisto; il server decide stato, colpi, crediti e round.

## Materiali della mappa

Le texture stanno in `Assets/Resources/MapTex` e le genera `tools/map/make_map_textures.py`
(`./run.sh tex`): 15 materiali tileable 512² — intonaco chiaro e ocra, pietra a corsi,
scalinata, tetto in cotto, strada, cemento, acciaio, legno, roccia, acqua, strisce
gialle, blu/arancio delle squadre.

Sono **generate**, non fotografiche: la mappa è fatta di volumi enormi e ripetuti, e una
foto fotografica su un muro di 30 m diventa un tappeto di piastrelle riconoscibili. Qui
ogni materiale ha dettaglio più grande del necessario: colore pieno, macchie larghe,
fughe sottili — si legge da lontano, come nei graybox di Valorant.

Le UV sono **ancorate al mondo** (`p.x / tileSize`), non al singolo cubo: due volumi
affiancati che formano lo stesso muro hanno la texture che continua. La scala per tipo
sta in `MapBuilder.TileSize`.

## Cielo

Panorama 4096×2048 (`Assets/Resources/Sky/sky_panorama.jpg`, lo stesso file del client
Godot) su `Skybox/Panoramic`, ruotato perché nuvole e sole cadano sulle viste di gioco.
L'ambiente è preso dallo skybox (`AmbientMode.Skybox`, intensità 0.72).

## Scia dei proiettili

`Assets/Shaders/Tracer.shader` (`Falezia/Tracer`) è il port URP del tracer del progetto
Godot: rumore che scorre su due strati, nucleo caldo, bordi che evaporano, punta che si
spegne. La LineRenderer fornisce `uv.x` lungo la scia; larghezza 7,5 mm alla volata e
2,5 mm all'impatto.

## HUD e negozio

Stile unico, sobrio: bianco al **90%**, testo piccolo, un solo accento rosso (`#FF4655`).
In basso a sinistra la vita (numero + barra sottile), in basso a destra munizioni e
crediti, in alto al centro **solo** il timer del round. Niente etichette di modo di fuoco.
Font: **Barlow** e **Barlow Condensed** (SIL OFL) in `Assets/Resources/Fonts`.

Il negozio (B) è ricalcato su quello di Valorant: fondo navy `#0F1923`, barra in alto
con crediti e fase, colonna categorie a sinistra, griglia di schede con l'arma e il
prezzo, pannello a destra con nome, statistiche a barre e il pulsante `ACQUISTA`.
L'acquisto resta validato dal server.

In mira l'arma **si avvicina** all'occhio (0,20 m pistole / 0,23 m fucili) e la camera
zooma: 72° per le mire metalliche, `85 / ingrandimento` per le ottiche (3×, 4×, 6×).

## Fori dei proiettili

Quad **appoggiati sulla superficie colpita** (normale della faccia, offset 6 mm, rotazione
casuale attorno alla normale), in spazio mondo: non seguono la camera. Restano 14 s e
svaniscono in 2. Pool di 96, texture generata in codice (buco scuro + orlo scheggiato).

Il materiale è `Falezia/FxUnlit` (`Assets/Shaders/FxUnlit.shader`), non `URP/Unlit`:
quello nasce **opaco** e la trasparenza dipende da una keyword le cui varianti possono
non arrivare nella build — il risultato erano fori disegnati come **quadrati pieni**.
Qui il `Blend` è scritto nel SubShader e non dipende da niente: quello che si vede in
editor è quello che si vede in build. Vale anche per scie e lampi.

## Modalità foto (controllo qualità)

```bash
./build/linux/Falezia.x86_64 -faleziaShots /tmp/shots
```

Teletrasporta il giocatore in punti veri della mappa (i `points` del JSON), tiene ferma la
camera e salva 12 PNG: porto, largo, piazza, mercato, molo, panoramica dall'alto, negozio,
muri colpiti, pavimento colpito, mira con pistola, mira con fucile, pannello debug. Serve a guardare ombre, armi e UI senza giocare a mano.

`-faleziaProbe` stampa invece nel log come è messa l'arma rispetto alla camera (socket
dell'occhio, della volata, clip di animazione): è così che si è verificato l'allineamento
del viewmodel dopo il passaggio a Unity.

## Cosa c'è già

| Pezzo | Stato |
|---|---|
| Simulazione (movimento, armi, combattimento, mappa, **round**, **vite**) | **completa**, la stessa del server |
| **Arma in mano** | camera dedicata (layer `ViewModel`): il fucile non entra più nei muri; in mira il socket `socket_eye` viene portato **sull'asse** della camera, quindi tacca e mirino sono centrati su ogni arma |
| Loader mappa JSON | completo, parser interno senza dipendenze |
| Mondo (scatole → cubi con materiali per tipo) | completo (graybox) |
| Giocatore in prima persona | completo (input → `InputCmd` → 64 Hz), predizione locale |
| **Rete** | `NetClient` UDP + snapshot + correzione posizione |
| **Altri giocatori** | capsule interpolate, colori di squadra, barra della vita |
| **Viewmodel** | GLB con le 7 clip via glTFast, fuoco/ricarica sincronizzati |
| Sparo: tracce, fori sui muri, lampo | completo (fori con texture generata) |
| **Audio delle armi** | 133 suoni sintetizzati, uno per arma (sparo, ultimo colpo, due ricariche, estrazione, colpo a vuoto, **caduta**, **raccolta**); vedi `docs/17-audio-armi.md` |
| **Armi a terra** | i drop della simulazione (o dello snapshot) diventano volumi sdraiati sul pavimento; il suono della caduta è 3D e parte una volta sola. **G** lascia l'arma, **F** raccoglie (con suggerimento a schermo) |
| **Cassa di squadra in HUD** | accanto ai soldi personali; il negozio dice se te lo puoi permettere contando entrambe le tasche |
| HUD: vita, munizioni, crediti, **fase/timer/punteggio** | completo (IMGUI) |
| **Negozio** | categorie, miniature, prezzi; **acquisto validato dal server** |
| Ombre, post-processing, AA | completi (vedi tabella sopra) |
| Bot/bersagli, animazioni in terza persona | da fare |
| Suoni del mondo (passi, impatti, sparo dei remoti in 3D, riverbero per zona) | da fare |

## Note

- Le armi sono tutte disponibili offline: `Simulation.Give` per ogni slot.
- I GLB stanno in `Assets/StreamingAssets/weapons/<id>/<id>.glb` (45 MB) con le texture;
  le miniature del negozio in `Assets/StreamingAssets/weapons/ui/`. Se rigeneri l'arsenale,
  ricopia entrambe le cartelle da `art/weapons/arsenal2`.
- L'acquisto è **autoritativo**: il client manda solo lo slot, il server controlla fase,
  crediti e prezzo e risponde con lo snapshot aggiornato (arma in pugno e crediti).
- I test restano nel progetto .NET (`tests/`), non in Unity.
