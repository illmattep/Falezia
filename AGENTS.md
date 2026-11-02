# FALEZIA — istruzioni per gli agenti

Questo repository ha un pacchetto di contesto scritto apposta per te. **Leggilo prima di toccare
qualsiasi cosa**, in quest'ordine:

1. `ai/RULES.md` — gli invarianti. Se un tuo cambio ne viola uno, il cambio è sbagliato.
2. `ai/STATE.md` — cosa esiste nel codice **oggi**. Non ricostruire ciò che c'è già.
3. `ai/CONTEXT.md` — il gioco in una pagina.
4. `ai/CODE.md` — architettura, convenzioni, come si testa.
5. `ai/GLOSSARY.md` — il vocabolario: usa questi termini, non sinonimi.

`ai/` è la fonte **condensata**; `docs/` è la fonte **dettagliata**. Se sono in contraddizione,
ha ragione `docs/` — e `ai/` va aggiornato nello stesso commit.

## Regola d'oro

**Un cambio che non lascia `./run.sh test` verde non si committa.** Il progetto ha 244 test: sono
il contratto, non un optional.

## Le cinque cose che un agente sbaglia più spesso

1. **Aggiungere sistemi non richiesti** (perk, classi, loot, battle pass, veicoli). Sono già stati
   valutati e scartati, con il motivo scritto in `docs/`.
2. **Mettere `using Godot;` in `sim/` o `net/`** — rompe il cancello architetturale.
3. **Fidarsi del client**: il client manda input, non verità. Tutto si valida sul server.
4. **"Migliorare" la grafica**: colore saturo = informazione di gioco, dettaglio basso = scelta.
5. **Cambiare i numeri di design in silenzio**: si aggiornano `DATA.json` + il documento, insieme.

## Comandi

```bash
./run.sh test     # 244 test
./run.sh build    # player Linux
./run.sh unity    # apre l'editor
./run.sh map      # rigenera e verifica Calanera
```
