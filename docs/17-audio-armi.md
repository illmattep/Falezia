# Audio delle armi

> 17 armi, **133 file**, nessun campione di terzi. Ogni arma ha sparo, ultimo colpo, ricarica
> tattica, ricarica a vuoto, estrazione, colpo a vuoto, **caduta a terra** e **raccolta**; il coltello
> ha il fendente e anche i suoni di drop e raccolta.

---

## Perché suoni generati

I suoni di Valorant sono di Riot: non si possono usare. Una libreria CC0 avrebbe voluto dire
scegliere 119 file a orecchio, con il rischio che due armi suonassero uguali — che è esattamente
il difetto che si vuole evitare. Qui ogni arma è una **scheda di parametri** e il suono esce da un
modello fisico semplificato: originale, riproducibile (stesso seme = stessi byte), e ogni arma ha
il suo carattere perché il suo carattere è scritto nei numeri.

Il generatore è `tools/audio/make_weapon_sounds.py` (Python 3 + numpy). Si rigenera tutto con:

```bash
./run.sh sound            # 133 file in Assets/StreamingAssets/audio/weapons
./run.sh sound --preview  # + tools/audio/preview/weapon-sounds.png (foglio di controllo)
```

Nessun file va modificato a mano: quello che si cambia è la scheda, poi si rigenera. Le durate
delle ricariche vengono **lette da `game/sim/Weapons.cs`**, quindi il suono finisce quando finisce
l'animazione: se un giorno la ricarica dell'Anvil scende a 2,2 s, il suono la segue senza toccare
niente.

---

## Come è fatto uno sparo

Cinque strati, come un colpo vero. Il carattere di un'arma è la ricetta di questi cinque.

| Strato | Cosa rappresenta | Come si sente |
|---|---|---|
| **crepitio** | il fronte d'onda supersonico | 2–3 ms di rumore filtrato in alto: è il "crack" |
| **corpo** | l'esplosione dentro la canna | rumore filtrato con una risonanza (95–300 Hz) e saturazione morbida |
| **tonfo** | la spinta sul petto | sinusoide che scende (155 → 34 Hz), secondo il calibro |
| **meccanica** | otturatore, carrello, pompa | due click metallici in ritardo di 11–50 ms |
| **coda** | il ritorno dei muri | riverbero sintetico: 0,12 s per una SMG, 1,2 s per il Longshot |

Esempi di carattere: la **Hush** non ha crepitio (è silenziata: crepitio 0,12, corpo tagliato a
900 Hz, e uno *sbuffo* al posto dello schiocco); l'**Hammer** ha il corpo a 105 Hz con la coda più
lunga di tutte le pistole; il **Blitz** ha il corpo a 300 Hz e la coda più corta, perché a 837 RPM
una coda lunga si impasterebbe con i colpi successivi; il **Choke** aggiunge la "spruzzata" delle
12 palle; **Longshot** e **Spotter** hanno le code più lunghe.

## Cosa c'è per ogni arma

| File | Quando |
|---|---|
| `fire` · `fire_last` | ogni colpo; l'ultimo del caricatore ha il fermo dell'otturatore |
| `reload` · `reload_empty` | la ricarica tattica e quella a vuoto, lunghe quanto la scheda |
| `draw` | l'arma che entra in scena (cambio, spawn) |
| `empty` | grilletto a vuoto |
| **`drop`** | l'arma che cade a terra: tonfo, un rimbalzo, la canna che stride (~0,55 s) |
| **`pickup`** | quando la raccogli: stoffa, click, molla |

Il suono della caduta è **3D** (`DroppedWeapons`): parte dalla posizione dell'arma, una volta sola,
quando la simulazione dice che si è posata (`LandTick`). Il peso conta: le pompe e i cecchini
sbattono più grosso delle pistole.

## Come è fatta una ricarica

Una **timeline di eventi meccanici** costruita sulla durata vera della scheda. Gli eventi sono
click, tonfi, attriti (carrello/pompa/otturatore), cartucce di plastica, molle e bossoli che cadono.

| Stile | Armi | Sequenza |
|---|---|---|
| `pistol` | Standard, Hush, Chatter | sgancio → caricatore fuori → caricatore dentro → carrello (a vuoto: carrello indietro e rilascio) |
| `revolver` | Snub | tamburo che si apre → sei bossoli che cadono → speedloader → chiusura |
| `magnum` | Hammer | tamburo → sette bossoli → sette cartucce una a una → chiusura |
| `smg` | Cadence, Scrap, Blitz | sgancio → caricatore lungo → dentro → otturatore |
| `rifle` | Anvil, Service, Vantage | sgancio → caricatore → dentro → rilascio otturatore |
| `sniper` | Spotter | caricatore → otturatore → chiusura |
| `bolt` | Longshot | caricatore → maniglia su → indietro → avanti → giù |
| `pump` | Choke | una cartuccia alla volta (6), poi la pompa |
| `auto_shotgun` | Sweeper | quattro cartucce nel tubo → pompa |
| `lmg` | Heavy | coperchio → nastro che scorre → coperchio che sbatte → maniglione |

Se la ricarica è **a vuoto** c'è in più il carrello indietro all'inizio e il rilascio alla fine: la
sequenza è più lunga perché lo è anche l'animazione (`ReloadEmpty`).

---

## Audio spaziale e distanze

Il suono che senti **da dove viene** e **fin dove arriva**. Regola: la portata non si inventa nel
client — viene dal design. Ogni arma dichiara `WeaponDef.NoiseMeters` (doc 11), e il servizio
`SpatialAudio` la usa così com'è.

| Suono | Portata | Curva | Note |
|---|---|---|---|
| **Sparo** (armi normali) | **260 m** — la diagonale della mappa | `Far`: 1,0 → 0,70 (10 m) → 0,42 (30 m) → 0,25 (65 m) → 0,10 (155 m) → 0,05 | si sente ovunque, ma lontano è un rimbombo: l'informazione resta |
| **Sparo silenziato** (Hush) | **40 m** (`NoiseMeters`) | `Tight`: 1,0 → 0,55 → 0,20 → **0** al limite | oltre i 40 m non parte proprio |
| **Coltello** (fendente) | **6 m** (`NoiseMeters`) | `Tight` | chi lo usa si muove anche più veloce: il silenzio è il suo vantaggio |
| **Ricarica / estrazione** (altri giocatori) | **16 m** | `Tight` | la meccanica non si sente da lontano |
| **Arma che cade a terra** | **26 m** | `Tight` | più le pistole, meno i cecchini |
| **La tua arma** (sparo, ricarica, drop in mano) | **2D** | — | è a 30 cm dalla faccia: spazializzarla sarebbe sbagliato |
| **La tua raccolta** | **2D** | — | è la tua mano |

Quattro cose rendono un suono "lontano", tutte in `SpatialAudio`:

1. **curva di volume** (due curve condivise, `Far` e `Tight`);
2. **filtro passa-basso**: 19 kHz da vicino → ~900 Hz al limite della portata. È il tonfo sordo che
   senti dall'altra parte della mappa;
3. **occlusione**: se un muro sta in mezzo (traccia contro le scatole della simulazione, la stessa
   dei proiettili) il volume scende al 55% e il filtro si chiude a un terzo;
4. **pool di 20 voci**: nessuno sparo taglia il precedente, e chi è fuori portata **non occupa voce**.

Il doppler è a zero: a 64 Hz di snapshot la posizione arriva a scatti e il pitch "ubriaco" si sente
più del movimento. Minimo 3 m (dentro quella sfera il suono è pieno), `priority` bassa: se le voci
finiscono, Unity scarta i suoni più lontani.

### Come ci arrivano gli eventi

Il client non indovina: lo **snapshot** porta, per ogni giocatore, i contatori `Shots` e
`ReloadStarts` (8 bit, girano: il client conta il delta modulo 256). Un incremento = uno sparo o una
ricarica da suonare nella posizione di quel giocatore. Vale anche per gli spari **dietro un muro**:
parte il suono, ma ovattato dall'occlusione.

```bash
./run.sh play -batchmode -nographics -faleziaSpatialProbe   # la tabella delle portate
```

### Cosa manca

| Suono | Perché non c'è |
|---|---|
| **Passi** | Non esistono come suono: sarebbe il prossimo passo naturale (camminata ~18 m, sprint ~26 m, accovacciato ~7 m, atterraggio ~20 m). Gli eventi ci sono già: `OnFloor` e la velocità arrivano nello snapshot |
| **Impatto su materiale** | Il foro sa che faccia ha colpito, il suono no |
| **Riverbero per zona** | La coda è dentro la clip: la Grotta dovrebbe suonare diversa dalla piazza |
| **Voci dei compagni** | Nessun audio di voce o di comunicazione |

## Come è collegato al client Unity

| Pezzo | Cosa fa |
|---|---|
| `Assets/Scripts/Client/Wav.cs` | legge i WAV PCM 16 bit da StreamingAssets (Unity non importa quella cartella: le clip si creano a runtime) |
| `Assets/Scripts/Client/WeaponAudio.cs` | le voci, i volumi, il pitch, la voce della ricarica che si può fermare |
| `PlayerRig` | aggancia i **contatori della simulazione**: `Shots`, `ShotsLast`, `ReloadStarts`, `ReloadWasEmpty`, `DryFires` |
| `Assets/Scripts/Client/SpatialAudio.cs` | il modello spaziale: curve, filtro per distanza, occlusione, pool di voci, lettura delle clip |
| `RemotePlayers` | sparo, fendente e ricarica degli **altri**, dai contatori dello snapshot |

Tre scelte che si sentono:

- **Voci multiple** (6 per gli spari, 3 per la meccanica): a 837 RPM i colpi si sovrappongono e
  nessuno taglia il precedente. Ogni colpo ha pitch ±3,5%: due colpi identici non esistono.
- **Volume che compensa la cadenza** (`WeaponAudio.FireVolume`): 0,85 per una pistola lenta, ~0,35
  per una SMG a 840 RPM, ×0,45 se silenziata, ×1,2 per le pompe. Senza questa compensazione il mix
  satura appena si tiene premuto.
- **La ricarica ha una voce sua**, e si ferma se la ricarica si interrompe o si cambia arma: il
  suono del caricatore non resta appeso a mezz'aria.

Il volume generale è in **Esc → GRAFICA → VOLUME** (0–100%, salvato in PlayerPrefs) e passa da
`AudioListener.volume`. Non c'è un mixer con i bus: oggi c'è un solo tipo di suono, un asset in più
sarebbe manutenzione senza guadagno. Quando arriveranno passi, porte e impatti, il mixer si farà.

Tutto è **2D** (`spatialBlend = 0`): l'arma è in mano, non nel mondo.

---

## Controllo qualità

```bash
# cosa ha trovato il client, arma per arma (durata dei file e volume calcolato)
./run.sh play -batchmode -nographics -faleziaAudioProbe
```

Stampa una riga per arma: sparo, ultimo colpo, ricarica, ricarica a vuoto, estrazione e volume.
Se un file manca si legge `MANCA` invece di un errore a schermo. Il generatore, a ogni esecuzione,
stampa anche durata, RMS e **centroide spettrale** di ogni sparo: è la misura di "quanto è brillante"
e serve a vedere a colpo d'occhio se due armi sono finite sulla stessa timbrica (oggi vanno da
~1.900 Hz del Choke ai ~4.700 del Blitz).

`tools/audio/preview/weapon-sounds.png` è il foglio delle forme d'onda: attacco netto, nessun
clipping (nessun tetto piatto), lunghezze diverse.

---

## Cosa manca (dichiarato, non dimenticato)

| Cosa | Nota |
|---|---|
| **Sparo degli altri giocatori** | Oggi si sente solo la propria arma. Serve audio 3D con attenuazione e occlusione, agganciato a `RemotePlayers` e agli eventi del server |
| **Passi, atterraggi, salti** | Non esistono |
| **Impatto su materiale** | Il foro sa che faccia ha colpito; il suono no (muro, metallo, legno, acqua) |
| **Riverbero per zona** | La coda è dentro la clip, uguale ovunque: la Grotta dovrebbe suonare diversa dalla piazza |
| **Coltello a segno** | `hit.wav` è generato ma non ancora collegato: manca il contatore del colpo a segno nel client |
| **Mixer e ducking** | Nessun bus, nessuna compressione fra gruppi |

---

## Aggiungere un'arma

1. Una voce in `DESIGN` dentro `tools/audio/make_weapon_sounds.py` con il suo carattere
   (crepitio, corpo, tonfo, meccanica, coda, stile di ricarica). Se manca, il generatore si ferma
   e te lo dice: nessuna arma resta muta per distrazione.
2. `./run.sh sound` → i sei file finiscono in `Assets/StreamingAssets/audio/weapons/<id>/`.
3. `./run.sh play -faleziaAudioProbe -batchmode -nographics` → la riga dell'arma nuova deve dire
   durate, non `MANCA`.
4. Ascolta: se due armi si somigliano, la differenza va messa nei **numeri**, non nel volume.
