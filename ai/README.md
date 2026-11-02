# ai/ — pacchetto di contesto per agenti

> **Se sei un modello linguistico e stai lavorando su FALEZIA, leggi questi file in quest'ordine.**
> Sono scritti per essere letti in cinque minuti e per evitare che tu rifaccia decisioni già prese.

| # | File | A cosa serve |
|---|---|---|
| 1 | **[RULES.md](RULES.md)** | **Gli invarianti.** Cose che non si possono violare. Se un tuo cambio ne viola una, il cambio è sbagliato |
| 2 | **[CONTEXT.md](CONTEXT.md)** | Il gioco in una pagina: regole, numeri, mappa |
| 3 | **[STATE.md](STATE.md)** | Cosa esiste nel codice **oggi**, e cosa manca |
| 4 | **[CODE.md](CODE.md)** | Architettura, convenzioni, ricette, come si testa |
| 5 | **[DATA.json](DATA.json)** | Gli stessi numeri, in forma macchina |
| 6 | **[GLOSSARY.md](GLOSSARY.md)** | Il vocabolario. Usa **questi** termini, non sinonimi |
| 7 | **[BLENDER_MCP.md](BLENDER_MCP.md)** | **Prima di toccare Blender.** Come sei collegato (MCP → addon, porta 9876), gli strumenti, cosa era rotto e non va rifatto |

---

## La regola d'oro

`docs/` è la fonte **dettagliata**. `ai/` è la fonte **condensata**.

> Se `ai/` contraddice `docs/`, **ha ragione `docs/`** — e `ai/` va aggiornato nello stesso commit.

---

## Le cinque cose che un agente sbaglia più spesso

1. **Aggiungere sistemi non richiesti.** Perk, classi, eroi, loot, battle pass, veicoli, distruzione,
   progressione funzionale, cunei per le porte. **Tutti già valutati e scartati**, con il motivo scritto
   in `docs/`. Se ti sembrano mancanti, non lo sono: sono **esclusi**.
2. **Mettere `using Godot;` in `sim/` o `net/`.** Rompe il cancello architetturale, e il progetto di
   test smette di compilare. È voluto.
3. **Fidarsi del client.** Il client manda **input**, non verità. Ogni cosa che conta si valida sul server.
4. **"Migliorare" la grafica.** Colore saturo = informazione di gioco. Dettaglio basso = scelta.
   Leggi `docs/07`.
5. **Cambiare i numeri di design in silenzio.** Se un numero deve cambiare, si cambia **anche** in
   `DATA.json` e nel documento relativo, nello stesso commit.

---

## Come lavorare qui

```
1. Leggi RULES.md            → se il compito le viola, fermati e chiedi
2. Cerca in docs/ il sistema toccato
3. Controlla STATE.md        → esiste già? è incompleto?
4. Implementa piccolo, in un file che ha già un posto nella struttura
5. ./run.sh test             → 193 test devono restare verdi
6. ./run.sh build            → 0 errori, 0 avvisi
7. Aggiorna STATE.md e DATA.json se hai cambiato qualcosa di osservabile
```

**Il commit deve stare in piedi da solo:** se `./run.sh test` non passa, non si committa.
