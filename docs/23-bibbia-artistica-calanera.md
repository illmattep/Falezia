# Calanera — bibbia artistica

Tutto in metri. Unità di misura del progetto: **1 unità Unity = 1 m**.
Modulo base della città: **3,20 m** (interasse finestre e passo dei pannelli).

---

## A. Regole generali

- **Niente pareti cieche su strada**: ogni fronte ha almeno una finestra o una porta.
- **Tre linee orizzontali** su ogni facciata: zoccolo (0,95), marcapiano (3,60), cornice (in cima).
- **Due tinte per edificio**: corpo principale + una seconda per il piano terra.
- **Mai due edifici adiacenti con la stessa tinta.**
- **Spigoli smussati** 15–25 mm: la luce radente li disegna.
- **Texture**: 1024², grana bassa, colore pieno. Nessun rumore fotografico.

---

## B. Moduli edilizi al centimetro

### B1. Piano terra — altezza 3,60 m
| Elemento | Misura |
|---|---|
| Zoccolo in pietra | h 0,95 · sporgenza 0,05 · stacco 0,02 |
| Porta d'ingresso | 1,10 × 2,25 · vano incassato 0,16 · soglia 0,06 |
| Gradino | 0,30 di pedata · 0,15 di alzata |
| Vetrina bottega | 2,20 × 2,60 · telaio 0,07 · traversa a 0,90 |
| Mostra (sporgenza) | 0,12 |
| Pluviale | Ø 0,09 · a 0,15 dallo spigolo · fascetta ogni 1,8 |

### B2. Piani superiori — altezza 3,20 m
| Elemento | Misura |
|---|---|
| Finestra | 1,05 × 1,55 · base a 1,05 dal piano |
| Vano | incasso 0,16 · spalle 0,03 |
| Davanzale | sporgenza 0,09 · spessore 0,06 · larghezza +0,12 |
| Architrave | 0,10 sopra il vano · sporgenza 0,04 |
| Persiane | 0,52 × 1,55 ognuna · spessore 0,05 |
| Interasse finestre | 2,60 – 3,00 |

### B3. Cornice e tetto
| Elemento | Misura |
|---|---|
| Cornice | 2 risalti: 0,26 e 0,14 · altezza totale 0,42 |
| Pendenza tetto | 22° |
| Sporto di gronda | 0,50 · fascia 0,18 |
| Coppo | 0,18 × 0,42 · fila ogni 0,42 |
| Colmo | 0,30 largo × 0,14 alto |
| Comignolo | 0,55 × 0,55 × 1,15 · cappello 0,10 |

### B4. Balcone
| Elemento | Misura |
|---|---|
| Soletta | 1,50 × 0,55 × 0,10 · sporgenza 0,55 |
| Mensole | 2–3 · 0,10 × 0,40 × 0,14 |
| Ringhiera | h 0,95 · barre Ø 0,018 ogni 0,12 · corrimano 0,04 |
| Quota | primo piano, base a 3,60 |

---

## C. Materiali

| Famiglia | Colore base | Roughness | Tiling |
|---|---|---|---|
| Intonaco chiaro | `#D8C4A8` | 0,90 | 2,5 m |
| Intonaco ocra | `#C08A4E` | 0,88 | 2,5 m |
| Pietra a conci | `#B8B0A0` | 0,85 | 2,5 m |
| Coppi | `#9E4426` | 0,82 | 1,6 m |
| Legno dipinto | `#8F7E66` | 0,80 | 2,0 m |
| Ferro battuto | `#1A1A1C` | 0,45 | 1,0 m |
| Vetro | `#33414C` | 0,20 | — |

Tinte per edificio: 8 varianti per famiglia, dalla posizione (deterministiche).
Persiane: 8 tinte — verde bosco, salvia, blu polvere, grigio, rosso scuro, ardesia, oliva, panna.

---

## D. Zone

### D1. Piazza Vecchia
- **Lastricato** chiaro, lastre 0,60 × 0,40, fughe 0,01.
- **Porticato**: archi a tutto sesto Ø 3,20, pilastri 0,55 × 0,55, volta a botte h 4,20.
- **Cassa** al centro: piattaforma 3,00 × 3,00 × 0,55 in pietra.
- Arredo: 4 panche (1,80 × 0,45), 1 fontana (Ø 2,20), 6 lampade a muro, 8 balconi.
- Lore: 3 insegne, 2 manifesti del Comune, 2 targhe.

### D2. Mercato
- 6 banchi: piano 2,00 × 0,80 a h 0,90, struttura in legno, **tenda a strisce** 2,40 × 1,60 a 18°.
- 12 casse (0,85³), 6 barili (Ø 0,55 × 0,90), 8 cassette del pesce (0,80 × 0,50 × 0,28).
- Cavi tesi fra i palazzi a 5,20 m, ogni 6 m.
- Lore: 4 insegne alimentari, 2 manifesti.

### D3. Via Stretta e Largo del Forno
- Vicoli larghi 3,20–4,00, edifici h 9–13.
- 10 lampade a muro a 3,20 m, 6 vasi (Ø 0,60 × 0,55), 4 targhe, 3 poster.
- Panni stesi: 3 linee a 4,60 m, teli 1,20 × 0,80.

### D4. Porto Vecchio (spawn Nord)
- Capannone: 18,0 × 7,0, altezza 5,20, lamiera grecata, portone 4,20 × 3,40.
- 6 container 6,06 × 2,44 × 2,59 (due impilati).
- Gru a traliccio: 12 m di torre, braccio 9 m.
- Banchina: 6 bitte (Ø 0,34 × 0,70), barca in secca 5,20 × 1,60.
- Lore: stemma ancora, 2 targhe, insegna pescheria.

### D5. Molo e Lungomare
- Banchina in pietra, 8 bitte, 6 lampioni (h 4,20), 4 panche rivolte al mare.
- Reti e boe: 3 mucchi di reti, 6 boe.

### D6. Mura e Forte San Verano (spawn Sud)
- Muraglia: h 4,90, spessore 2,25, merli 0,62 × 0,75 ogni 1,15.
- Porta carraia: arco Ø 5,00, battenti 3,00 × 3,20 con 3 bandelle.
- 2 torri 5,00 × 5,00 × 9,90 con merli e feritoie.
- 2 bracieri (Ø 0,90 × 1,10), 2 stendardi 1,30 × 3,60.
- Lore: stemma torre, 3 targhe.

### D7. Grotta
- Pietra nuda, luce dagli ingressi, polvere sospesa.
- 6 stalattiti, 2 bracieri all'ingresso.

---

## E. Luce e atmosfera
- Sole: direzione O→E, elevazione **38°**, colore `#FFF7F0` (neutro-caldo), intensità **1,00**.
- Ambiente: a tre colori (cielo freddo, rimbalzo caldo del selciato, terra scura), intensità **0,85**.
- Nebbia: lineare 70→330 m, colore `#C9CFD1`.
- Fasci volumetrici: 9 punti, additivi, larghezza 3,2 m, altezza 7 m.
- **Ombre**: 110 m, 4 cascate (0,05/0,15/0,35), mappa 4096 (8192 su ULTRA), soft **High**;
  il blockout proietta ombre (prima erano spente).
- **SSAO**: 16 campioni, blur **High**, normali **Medium**, **Interleaved gradient**, raggio
  **0,6**, su **normali geometriche**; spento su BASSO. La versione BlueNoise + normali High
  faceva chiazze sulle facce piatte: niente rumore ora.
- Grading: contrasto **10**, saturazione **4**, esposizione **−0,15**, **white balance −12**,
  bloom **0,06** con soglia 1,35. Meno giallo e meno bloom: il colore non deve venire dal post.

## F. Budget
| Voce | Limite |
|---|---|
| Triangoli pelle | 3.000.000 |
| Draw call pelle | 60 |
| Texture | 1024², 12 famiglie |
| Oggetti piazzati | 2.500 |

## G. Ordine di costruzione
P0 edificio campione → P1 Piazza → P2 Mercato → P3 Porto → P4 Forte → P5 vicoli → P6 rifinitura.
