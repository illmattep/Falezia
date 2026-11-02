# Deploy su VPS

Il control plane è **un processo** che tiene insieme il pannello web, la coda di
matchmaking e le istanze di gioco. Le istanze sono processi figli: se il control
plane muore, systemd lo riavvia e le partite ripartono.

```
        ┌────────────────────────────── VPS ──────────────────────────────┐
        │                                                                 │
client ─┼─ HTTP :8090 ─►  FALEZIA CONTROL   ── spawn ──►  Falezia.Server   │
 (UDP)  │                  pannello + coda              :27015..27099/udp  │
        └─────────────────────────────────────────────────────────────────┘
```

| Cosa | Porta | Note |
|---|---|---|
| Pannello + API | **8090/tcp** | solo amministrazione, **HTTP in chiaro** |
| Partite | **27015–27099/udp** | una porta per istanza |

---

## 1 · Preparare il pacchetto (sulla macchina di sviluppo)

```bash
./deploy/publish.sh              # self-contained: nessun runtime sulla VPS
./deploy/publish.sh --portable   # più leggero, ma serve .NET 8 sulla VPS
```

Esce in `dist/control` e `dist/server`, con la mappa già accanto al server.
Se il publish self-contained si lamenta dei runtime pack, manca la connessione a
NuGet: usa `--portable` e installa il runtime sulla VPS.

## 2 · Caricare

```bash
rsync -av --delete dist/ utente@vps:/tmp/falezia-dist/
scp deploy/install-vps.sh deploy/falezia-control.service utente@vps:/tmp/falezia-dist/
```

## 3 · Installare (sulla VPS, come root)

```bash
sudo /tmp/falezia-dist/install-vps.sh
```

Lo script crea l'utente `falezia`, copia i binari in `/opt/falezia`, i dati in
`/var/lib/falezia`, la configurazione in `/etc/falezia/control.env`, installa e
avvia l'unità systemd. Stampa il pannello con il token già dentro.

Se `curl https://ifconfig.me` non funziona, l'host pubblico resta `127.0.0.1`:
correggilo a mano.

```bash
sudo nano /etc/falezia/control.env     # FALEZIA_PUBLIC_HOST = IP o dominio
sudo systemctl restart falezia-control
```

## 4 · Aprire il firewall

```bash
sudo ufw allow 22/tcp
sudo ufw allow 8090/tcp
sudo ufw allow 27015:27099/udp
sudo ufw enable
```

Se le partite devono arrivare da Internet, la **UDP deve essere aperta** anche sul
firewall del provider (Hetzner/OVH hanno un firewall di rete separato).

## 5 · Il pannello

Apri `http://IP:8090/?token=…`. Da lì:

- **limite rigido** on/off e **numero massimo di istanze** — è la manopola che
  impedisce a una coda impazzita di accendere venti partite e bruciare la VPS;
- **giocatori per partita**, quanti in coda servono per partire, attesa massima;
- **mappa** e **livello di logging** (debug/info/warn/error), applicati alle
  istanze nuove;
- **avvia/ferma** una partita e vedi i log di tutte le istanze in tempo reale.

Le impostazioni stanno in `/var/lib/falezia/control.json` e valgono subito, senza
riavviare il servizio.

---

## Aggiornare

```bash
./deploy/publish.sh
rsync -av --delete dist/ utente@vps:/tmp/falezia-dist/
sudo /tmp/falezia-dist/install-vps.sh      # ferma, copia, riavvia
```

## Comandi utili

```bash
systemctl status falezia-control
journalctl -u falezia-control -f
journalctl -u falezia-control -n 200 --no-pager
sudo systemctl restart falezia-control
sudo deploy/install-vps.sh --uninstall     # toglie binari e unità, tiene i dati
```

## Problemi tipici

| Sintomo | Causa | Rimedio |
|---|---|---|
| `Address already in use` su 8090 | un altro processo usa la porta | `ss -ltnp \| grep 8090` |
| `No frameworks were found` | publish portable senza runtime | `publish.sh` self-contained, o `apt install dotnet-runtime-8.0` |
| Le partite non si avviano, log `nessuna porta UDP libera` | range occupato | allarga `portRangeEnd` in `control.json` |
| Il pannello risponde 401 | token mancante o diverso | token in `/etc/falezia/control.env` |
| Il client non si collega | host pubblico sbagliato, UDP chiusa | `FALEZIA_PUBLIC_HOST`, firewall UDP |

## Sicurezza

Il pannello è **HTTP e senza TLS**: per ora va esposto solo via tunnel SSH

```bash
ssh -L 8090:127.0.0.1:8090 utente@vps
```

oppure messo dietro nginx con TLS e autenticazione. Il token è l'unica difesa, e
viaggia in chiaro: non usarlo su una rete che non controlli.
