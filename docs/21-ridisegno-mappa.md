# Ridisegno della mappa dal blockout

La mappa attuale è una scatola con texture sopra. Si rifà così:

## Principio
Il **blockout resta la verità fisica** (collisione, rotte, test). Sopra ci va una **pelle
costruita in Blender**: pareti con aperture vere, cornici, tetti, dettagli. Unity non disegna
più i volumi: disegna la pelle.

## Costruzione di un edificio (Blender, da calanera.json)
1. **Pareti con fori veri**: ogni lato è diviso in pannelli attorno alle aperture (sotto,
   sopra, a sinistra, a destra). Niente facce piatte con la finestra dipinta.
2. **Aperture**: vano incassato di 18 cm, davanzale in pietra che sporge, architrave.
3. **Serramenti**: persiane in legno dipinto, aperte o accostate, con telaio.
4. **Zoccolo** in pietra fino a 0,9 m, staccato dall'intonaco.
5. **Cornicione** a due risalti, aggettante 25 cm.
6. **Tetto a falde** con coppi, sporto, colmo, comignolo.
7. **Plinth e pluviali** agli angoli.

## Materiali (PBR, stilizzati, UV a scala mondo)
intonaco (4 tinte), pietra (2), coppi, legno dipinto (3), ferro, vetro.
Texture 1024², grana bassa, colore pieno: stile dipinto, non fotografico.

## Ordine dei pezzi
| # | Pezzo | Verifica |
|---|---|---|
| P0 | un edificio campione (Piazza) | render Blender + in gioco |
| P1 | isolato della Piazza | in gioco |
| P2 | Mercato | in gioco |
| P3 | Porto | in gioco |
| P4 | Forte e Mura | in gioco |
| P5 | vicoli, riempimento, dettagli | in gioco |
