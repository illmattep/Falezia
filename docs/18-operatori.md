# Operatori (i corpi dei giocatori)

> Il primo operatore è ricavato dalla scansione `Meshy_AI_tactical_mechanic_her_*`:
> **una mesh statica**, senza scheletro, senza morph target, senza mani separate.
> Questo documento dice cosa è stato ricavato, come, e cosa **non** è possibile ricavarne.

---

## Cosa c'era nel file (e cosa no)

| | |
|---|---|
| Nodi / mesh | **1 nodo, 1 mesh, 1 primitiva** |
| Triangoli | **1 034 302** (552 238 vertici) |
| Materiali | 1 (base color + metallic-roughness + normal) |
| Scheletro | **nessuno** |
| Morph target / blend shape | **nessuno** |
| Posa | A-pose, piedi a terra, altezza 1,90 m (l'antenna della radio sporge) |

**Espressioni facciali: impossibili da questo asset.** Un'espressione richiede blend shape
(scultura di faccia, bocca, sopracciglia) o una faccia separata con ossa dedicate: qui la faccia è
fusa nel corpo ed è una superficie scansionata. Si potrebbe aggiungere un'osso della mascella e
deformare i vertici della bocca a mano, ma senza una topologia facciale il risultato è una smorfia,
non un'espressione. Serve un modello con morph target (o un volto riggato) — è un asset diverso.

Le **mani** hanno ora un rig completo (vedi sotto), ma la geometria delle dita è **fusa** nella
scansione: le falangi si muovono, la pelle no. L'arma è ancorata al polso, non alle dita: la presa
si legge a distanza, non al microscopio.

---

## Il rig (`tools/character/rig_operator.py`)

```
blender -b --factory-startup -P tools/character/rig_operator.py
```

1. import del GLB, scala a 1,80 m, piedi a z = 0;
2. **decimazione** da 1 034 302 a **20 000 triangoli** (un corpo a 5–40 m non ha bisogno di più;
   10 giocatori restano dentro il budget del doc 07);
3. **scheletro a 39 ossi** costruito sulle misure del mesh: bacino, colonna, petto, collo, testa,
   clavicole, braccia, avambracci, mani, cosce, stinchi, piedi **+ venti ossi di dita** (quattro
   dita a due falangi e un pollice a due falangi, per mano);
4. **pesi calcolati a mano**, non "automatic weights": distanza punto-segmento con decadimento
   gaussiano (σ = 7,5 cm), i **3 ossi più vicini**, normalizzati, con il vincolo che un osso di
   sinistra non prenda vertici di destra. Automatic weights su una scansione fa sanguinare le
   braccia nelle cosce; questo no. L'antenna della radio è cucita al **petto**, non alla testa.
   **Le mani hanno pesi rigidi** (ogni vertice al 100% all'osso più vicino): le dita sono a 2 cm
   l'una dall'altra e con pesi sfumati si dividono i vertici fra due dita — la mano si strappa.
   Rigida fa pieghe nette, ma resta intera;
5. **le mani sono protette durante la decimazione** (`vertex_group` + `invert_vertex_group`): su
   24 000 triangoli totali **6 066** finiscono nelle mani, altrimenti le dita si sbriciolano;
6. texture da 4096 a **1024**, JPEG 85 → il GLB passa da 48 MB a **1,4 MB**;
7. export `Assets/StreamingAssets/characters/operator/operator.glb` + render di controllo.

Riferimenti visivi: `tools/character/preview/blender_*.png` (posa da gioco, accovacciata, camminata).

---

## L'animazione (`Assets/Scripts/Client/OperatorView.cs`)

Non esistono clip per questo asset, e con 17 armi × 4 stati servirebbero centinaia di clip. Il
movimento è **procedurale**, guidato dallo stato della simulazione (che è già autorevole):

| Stato | Cosa muove |
|---|---|
| **Camminata / corsa** | la fase avanza con la **distanza percorsa** (niente piedi che slittano), ampiezza e frequenza crescono con la velocità; braccia in controfase, bacino e petto ruotano, testa controbilancia |
| **Accovacciato** | anche e ginocchia si flettono, bacino scende, busto si piega in avanti, testa si alza un filo |
| **Ricarica** | la mano destra scende al caricatore e torna, per la **durata vera della scheda** (`ReloadTactical` / `ReloadEmpty`): ogni arma ha il suo tempo |
| **Sparo** | contraccolpo breve su braccio destro e petto |
| **Idle** | respiro |
| **Morte** | il corpo smette di animarsi (la caduta è ancora da fare) |

**Due modi di ruotare le ossa**, e il perché:

- **gambe e busto** usano angoli attorno agli assi *del personaggio* (destra, su, avanti), convertiti
  nello spazio locale dell'osso alla posa di riposo;
- **braccia** usano **bersagli**: gomito e mano sono punti in spazio personaggio e le due ossa vengono
  *puntate* lì (`AimAt` = `FromToRotation` sulla direzione). Con le ossa importate da Blender gli assi
  locali non si indovinano; i bersagli sì. La posa è: mano destra davanti al petto, sinistra sul
  guardamano; in ricarica scendono al caricatore.

**La presa.** Le falangi non hanno un asse di flessione comodo, quindi si *puntano* verso un punto
sul lato del palmo (verso il corpo): più la presa è forte, più il bersaglio è vicino. La mano destra
tiene l'impugnatura (0,80), la sinistra il guardamano (0,70), col coltello è un pugno (0,85);
durante la ricarica la mano si apre (0,35) per prendere il caricatore.

**L'arma in mano** è ancorata a un punto figlio del corpo che prende la **posizione dalla mano** e la
**direzione dal personaggio**: la canna deve guardare avanti, non lungo l'avambraccio. È il
compromesso che rende leggibile un fucile in terza persona senza IK sulle dita.

---

## Terza persona di debug (Alt)

Tenendo premuto **Alt** la visuale passa in **terza persona**: la camera si mette a 3,4 m dal
giocatore e **gira da sola** (30°/s) attorno a lui, sempre rivolta al petto. Serve a guardare il
corpo, il passo, la ricarica e la presa sull'arma senza organizzare una partita.

- il **corpo del giocatore locale** è un `OperatorView` indipendente dalla camera (che è il
  transform del giocatore): si attiva solo in questa modalità, e usa lo stesso stato della
  simulazione — quindi camminata, accovacciata e ricarica sono quelle vere;
- il **viewmodel** si nasconde: altrimenti l'arma in prima persona galleggerebbe davanti
  all'obiettivo;
- l'HUD scrive `TERZA PERSONA · ALT PER TORNARE IN PRIMA`, così non sembra un difetto;
- rilasciando Alt si torna in prima persona e la camera riprende l'occhio.

Nel probe: `-faleziaOperatorProbe -faleziaSkipMenu` salva anche lo scatto `_third` con questa
modalità attiva (`GameRoot.Player.SetThirdPerson(true)`).

---

## Come si guarda (controllo qualità)

```bash
./run.sh build
./build/linux/Falezia.x86_64 -faleziaOperatorProbe -faleziaSkipMenu
```

Mette tre operatori davanti alla telecamera (fermo col fucile, in camminata con la pistola,
accovacciato con la SMG), salva `unity_operator_front.png` e uno scatto dal fianco, poi chiude.
Riferimenti: `tools/character/preview/unity_*.png`.

---

## Cosa manca (dichiarato)

| Cosa | Nota |
|---|---|
| **Espressioni facciali** | impossibili da questo asset: nessun morph target, faccia fusa nel corpo |
| **Dita nella geometria** | le falangi sono riggate, ma le dita della scansione sono **fuse**: la presa si legge a distanza, non da vicino. L'arma è al polso, non alle dita |
| **Animazioni cotte** | quelle procedurali non hanno il peso di una clip d'autore: niente spallate, salti, morte ragdoll |
| **I 5 operatori** | questo è **uno**. Gli altri quattro sono modelli diversi (e diversi rig) |
| **Corpo in prima persona** | in prima persona si vede solo il viewmodel; il corpo compare con **Alt** (terza persona di debug) |
| **Sincronizzazione della ricarica** | la posa parte dal contatore `ReloadStarts` dello snapshot: giusta, ma non distingue tattica da a vuoto nei remoti |
