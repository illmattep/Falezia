# STATE · cosa esiste nel codice, oggi

> **Aggiorna questo file quando cambi qualcosa di osservabile.** Un agente che legge uno stato vecchio
> ricostruisce cose che esistono già, o peggio assume che esistano cose che non ci sono.

**Ultimo aggiornamento:** 19 ottobre 2026 · milestone **M0** in corso.

---

## ✅ Funziona

| Area | Cosa | Dove |
|---|---|---|
| **Simulazione** | Movimento a passo fisso 64 Hz, deterministico: passo/sprint/crouch, stamina, salto, controllo aereo limitato, collisioni AABB a assi separati, atterraggio su volumi | `game/sim/Sim.cs` |
| **Stato** | `PlayerState`, `SimState`, `WorldState` (solo scatole statiche) | `game/sim/State.cs` |
| **Protocollo** | Binario, input 22 B, snapshot ~34 B/giocatore, con l'ack per client | `game/net/Protocol.cs` |
| **Test** | **26/26** — gravità, velocità, salto, assenza di air-strafe, muri, cassa, determinismo, protocollo, cancello architetturale | `tests/Program.cs` |
| **Server** | Headless, tick 64 Hz verificato, console con comandi, join/timeout/kick, log categorizzati | `game/server/ServerMain.cs` |
| **Client** | Predizione locale, riconciliazione con replay degli input non confermati, netgraph | `game/client/ClientMain.cs` |
| **Input** | Mouse catturato, raw input (`UseAccumulatedInput = false`), ESC libera / click ricattura | `game/client/InputConfig.cs` |
| **Mirino** | Classe `Crosshair` con 6 profili (pistol/smg/shotgun/rifle/sniper/knife), contorno, spread dinamico | `game/client/Crosshair.cs` |
| **Performance** | `Perf.Line()` e `Perf.Short()`: fps, frame ms, fisica ms, draw call, primitivi, VRAM | `game/client/Perf.cs` |
| **Menu** | Voci incomplete dichiarate; riga dell'insegna ("con la Z") | `game/client/MainMenu.cs` |
| **Stanza debug** | Dock scrollabile, ispettore live, teleport, 6 mirini, latenza/loss simulati, spawn bot, dump armi, performance | `game/client/DebugRoom.cs` |
| **Grafica** | Cielo procedurale, foschia, sole caldo con ombre, palette del doc 07, pad emissivi | `DebugRoom.BuildWorld` |

## ❌ Non esiste ancora

| Cosa | Nota |
|---|---|
| **Sparo** | Nessuna arma equipaggiabile, nessun raycast, nessun danno applicato |
| **Lag compensation** | Prevista: storia delle pose 1 s, rewind max 200 ms |
| **Round ed economia** | Nessuna fase, nessun timer, nessuna cassa, nessun Nucleo, nessun assedio |
| **Deployabili e porte** | Progettati, non implementati |
| **HUD di gioco reale** | I placeholder mostrano `—` di proposito |
| **Interpolazione dei remoti** | Per ora i giocatori remoti vengono copiati dallo snapshot (scattano) |
| **Latenza/loss sul socket vero** | I pulsanti della stanza debug cambiano solo un'etichetta |
| **Matchmaking, account, backend** | Fase 3 del piano (doc 13/14) |
| **Arte** | Zero, per scelta: si parte dai kit dopo il graybox validato |

## 🎯 Il prossimo passo (Gate G1)

> **Far parlare server e client end-to-end con 200 ms di ping e 2% di perdita pacchetti**, e misurare
> la riconciliazione. Criterio di superamento: movimento fluido, errori di correzione piccoli e rari,
> nessun "rubber banding" percepibile.

Come:
```bash
./run.sh server     # terminale 1
./run.sh game       # terminale 2
```
Poi: collegare la latenza simulata **al socket** (ritardare/scartare i pacchetti in uscita e in entrata),
così il gate si misura con numeri e non a occhio.

## 📌 Convenzioni di stato

- Test rossi = **niente commit**
- Build con avvisi = **da sistemare subito** (siamo a 0)
- Se aggiungi un file nuovo in `game/`, aggiungilo alla tabella qui sopra
