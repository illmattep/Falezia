# Architettura (Unity + server dedicato)

Il progetto è diviso in **tre pezzi** che non si pestano i piedi:

```
game/sim, game/net        ← la simulazione e il protocollo (C# puro, zero motore)
shared/Falezia.Core       ← csproj netstandard2.1 che compila quei sorgenti: UNA copia
server/Falezia.Server     ← server dedicato .NET 8, headless, deployabile
engine-unity/             ← client Unity (asmdef separata per il core)
game/client, game/server  ← il vecchio client Godot (resta, non più sviluppato)
tests/                    ← 244 test sulla simulazione, girano senza motore
```

**Regola d'oro:** la simulazione non conosce né Unity né Godot. Decide movimento,
collisioni, armi, colpi, vite, round. Il client disegna e manda input; il server è
l'autorità. Se domani cambi motore, `Falezia.Core` resta identico.

## Il server

| Pezzo | Cosa fa |
|---|---|
| `GameServer` | socket UDP, 64 Hz, join/timeout, comandi da console |
| `Combat.ShootPlayers` | risolve i colpi contro i giocatori (muro → testa → corpo) |
| `Match` | fasi, punteggio, metà partita, crediti (vittoria 3000, sconfitta 1900→2900, kill 200) |
| `Simulation.Respawn` | rinascita a inizio round: vita, munizioni, armi nuove |
| `Protocol` | input 22 B, snapshot con testata del round (fase, tempo, round, punteggio) |

**Round:** riscaldamento → acquisti (20 s) → round (90 s) → risultato (6 s) → …
Si vince eliminando la squadra avversaria; a 13 round vinti la partita finisce.
Al cambio di fase il server rinasce tutti, dà i crediti e resetta le armi.

## Il client Unity

| Pezzo | Cosa fa |
|---|---|
| `GameRoot` | crea mondo, giocatore, rete; **online/offline** da Inspector; carica la scena `Calanera` in additivo |
| `Calanera.unity` | la mappa nuova: blockout (collider + 646 volumi), arte dello spawn, suolo città, banchina, cielo/luce di scena |
| `MapBuilder` / `MapProps` | il vecchio percorso (kit GLB da `props.json`): oggi spento (`MapProps.Enabled=false`) |
| `PlayerRig` | input → `InputCmd` → 64 Hz; predizione locale + correzione dallo snapshot |
| `NetClient` | UDP, thread di ricezione, coda di snapshot, ultimo `MatchState` |
| `RemotePlayers` | capsule interpolate degli altri, barra della vita, colori di squadra |
| `WeaponView` | GLB (glTFast) con le 7 clip; riscala `fire` sulla cadenza della scheda |
| `Hud` | vita, munizioni, crediti, fase/timer/punteggio, **negozio** con categorie |
| `CombatFx` / `BulletMarks` | tracce, lampo, fori billboard con texture generata |

**Predizione:** l'input si applica subito in locale; quando arriva lo snapshot, se lo
scarto di posizione supera 35 cm il client si riallinea al server. È la riconciliazione
semplificata: sufficiente per giocare, da raffinare con il replay degli input non
confermati (doc 08).

## Avvio in due minuti

```bash
# 1. server
dotnet run --project server/Falezia.Server -- --port=27015

# 2. Unity: scena vuota + GameRoot, "Online" attivo, host 127.0.0.1, porta 27015
```

Senza server: `Online` spento → si gioca in locale con tutte le armi (stanza di prova).

## Deploy

Vedi `server/README.md`: Docker (immagine runtime 8.0, utente non privilegiato, porta
UDP unica), systemd, o `dotnet publish` + `scp`.
