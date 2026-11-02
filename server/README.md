# Falezia · server dedicato

Server **headless**: nessun motore, nessun renderer, solo la simulazione a 64 Hz e un
socket UDP. È l'autorità su posizioni, colpi, vite, round e crediti.

```
server/Falezia.Server/     Program.cs · GameServer.cs · ServerConfig.cs
shared/Falezia.Core/       la simulazione e il protocollo (netstandard2.1, zero dipendenze)
```

La simulazione è **una sola**: `shared/Falezia.Core` compila `game/sim` e `game/net`,
gli stessi sorgenti che usano i test e il client.

## Sviluppo

```bash
dotnet run --project server/Falezia.Server -- --port=27015
```

Opzioni: `--port=`, `--max=`, `--map=`, `--quiet` (o env `FALEZIA_PORT`, `FALEZIA_MAX`,
`FALEZIA_MAP`). Comandi da console: `status`, `round`, `kick <id>`, `quit`.

## Pubblicazione

```bash
dotnet publish server/Falezia.Server -c Release -o out
./out/Falezia.Server --port=27015
```

Il pacchetto `out/` contiene anche `maps/calanera.json`.

## Deploy con Docker (consigliato)

```bash
docker build -f server/Falezia.Server/Dockerfile -t falezia/server .
docker run -d --name falezia -p 27015:27015/udp --restart unless-stopped falezia/server
```

oppure, dal repo:

```bash
cd server && docker compose up -d --build
```

L'immagine runtime è `dotnet/runtime:8.0`, utente non privilegiato, root filesystem in
sola lettura, una sola porta **UDP**.

## Deploy su VPS (systemd)

```bash
sudo useradd --system --home /opt/falezia falezia
sudo mkdir -p /opt/falezia && sudo chown falezia:falezia /opt/falezia
dotnet publish server/Falezia.Server -c Release -o /tmp/falezia
sudo cp -r /tmp/falezia/* /opt/falezia/
sudo cp server/falezia-server.service /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now falezia-server
```

Apri **solo** la porta UDP sul firewall:

```bash
sudo ufw allow 27015/udp
```

## Come è fatto un tick

1. **input**: pacchetti UDP `MsgInput` (22 byte) → ultimo comando di ogni client;
2. **simulazione**: `Simulation.ApplyInput` + `ApplyWeapon` per ogni giocatore;
3. **colpi**: quando il contatore `Shots` avanza, `Combat.ShootPlayers` risolve la
   traiettoria (muro prima, poi testa e corpo), applica danno e uccisioni;
4. **round**: `Match.Tick` fa avanzare le fasi; a squadra eliminata o tempo scaduto
   `Match.EndRound`; in acquisti si rinasce e si danno i crediti;
5. **snapshot**: `Protocol.EncodeSnapshot` (stato + testata del round) a ogni client.

## Limiti noti (M0)

- nessuna compensazione di latenza (si spara dove si vede **adesso**, non dove si vedeva);
- nessun anti-cheat oltre all'autorità del server;
- riconnessione semplice: il client che tace 10 s viene rimosso;
- la mappa si carica dal file, non c'è ancora rotazione né warmup con giocatori veri.
