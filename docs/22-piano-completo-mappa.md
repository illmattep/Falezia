# Piano completo — ridisegno della mappa di Calanera

## 1. Obiettivo e vincoli

**Obiettivo**: sostituire la mappa a scatole con una città disegnata, costruita in Blender,
zona per zona, con materiali e geometria su misura.

**Vincoli che non si toccano**
- La **collisione resta il blockout** (`calanera.json`): rotte dei bot, test, spawn, obiettivi
  non cambiano. La nuova geometria è solo pelle.
- **Deterministico**: stessa città a ogni partita (tinte e varianti da hash della posizione).
- **Prestazioni**: budget 2,5–3 M triangoli visibili, ≤ 60 draw call per la pelle (mesh unite
  per materiale), LOD a 40 m per i dettagli piccoli.
- **Stile**: dipinto, non fotografico. Colori pieni, grana bassa, normali morbide, spigoli
  smussati. Niente sporco fotografico.

## 2. Pipeline

```
calanera.json ──► generatore Blender ──► GLB per edificio/zona ──► Unity (MapSkin)
     │                    │                        │
     │                    └── materiali PBR 1024² ─┘
     └── collisione, spawn, punti (restano invariati)
```

1. **Generatore** (`tools/map-assets/building.py`): legge un lotto, produce pareti con
   aperture, zoccolo, cornice, tetto, comignolo, pluviale.
2. **Batch** (`build_town.py`): cicla su tutti i lotti edificabili, esporta **un GLB per
   isolato** in `StreamingAssets/map/skin/quartiere_XX.glb`.
3. **Unity** (`MapSkin.cs`): carica i GLB in coordinate mondo, li unisce per materiale,
   spegne il disegno dei volumi del blockout (la collisione resta).
4. **Materiali**: set unico condiviso, UV a scala mondo, 6 famiglie.

## 3. Kit di componenti (si costruiscono una volta)

| Componente | Note |
|---|---|
| Parete con aperture | pannelli attorno ai fori, vano incassato 16 cm |
| Finestra | vetro, telaio, davanzale, architrave, persiane (2 tinte) |
| Porta | battente in legno, stipiti, gradino, soglia |
| Vetrina | apertura larga, telaio, tenda opzionale |
| Cornice | due risalti, aggetto 26 cm |
| Tetto | due falde, colmo, fascioni, comignolo |
| Balcone | soletta 1,5 m, ringhiera in ferro |
| Pluviale | tubo in ferro agli angoli |
| Zoccolo | pietra 0,95 m, stacco netto |

## 4. Fasi

| Fase | Contenuto | Deliverable | Tempo |
|---|---|---|---|
| **P0** | edificio campione: pareti+finestre+porta+cornice+tetto, materiali | render isolato + check in gioco | 45 min |
| **P1** | tutti gli edifici della **Piazza Vecchia** + porticato | Piazza giocabile con pelle nuova | 60 min |
| **P2** | **Mercato**: edifici + tende + banchi + casse | zona Mercato | 45 min |
| **P3** | **Porto Vecchio**: capannone, container, gru, banchina | spawn Nord completo | 60 min |
| **P4** | **Forte e Mura**: muraglia, porta, torri, bracieri | spawn Sud completo | 45 min |
| **P5** | **vicoli e riempimento**: Via Stretta, Largo, Lungomare, Grotta | mappa completa | 60 min |
| **P6** | rifinitura: dettagli, decal, segnaletica, LOD, verifica prestazioni | build finale | 45 min |

Totale ~6 ore di lavoro effettivo, in sessioni da 45–60 minuti con verifica a ogni fine fase.

## 5. Criteri di qualità (checklist per edificio)

- [ ] Nessuna parete completamente cieca su strada: almeno una finestra o una porta.
- [ ] Ogni apertura ha vetro, telaio e davanzale.
- [ ] Zoccolo e cornice sempre presenti: danno la scala.
- [ ] Tetto con sporto e comignolo: mai un tetto piatto senza cornice.
- [ ] Due edifici adiacenti mai con la stessa tinta.
- [ ] Nessun oggetto che sporge oltre il filo del tetto.

## 6. Rischi

| Rischio | Mitigazione |
|---|---|
| Geometria troppo pesante | un GLB per isolato, merge per materiale, LOD 40 m |
| Buco fra pelle e collisione | la pelle nasce dai volumi del JSON, non a mano |
| Tempi | ogni fase produce una zona giocabile: si può fermare a qualsiasi P |
| Regressioni sul gameplay | Unity disegna solo la pelle; collisione, spawn e obiettivi non toccati |

## 7. Ordine di lavoro immediato

1. Chiudere P0 (tetto in coordinate Blender, fori su tutte le facciate, vetro/persiane).
2. Batch su tutti i lotti della Piazza + porticato.
3. `MapSkin` in Unity: caricare, unire, spegnere i volumi.
4. Build + screenshot della Piazza, confronto con lo stato attuale.
5. Solo se regge: passare a Mercato e Porto.

## 8. Cosa mi serve da te

- Conferma che la **collisione resta il blockout** (è il punto su cui si regge tutto).
- Se vuoi il porticato rifatto in geometria vera (archi, volte) o lasciato com'è.
