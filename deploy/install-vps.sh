#!/usr/bin/env bash
# FALEZIA — installa il control plane su una VPS Debian/Ubuntu (systemd).
#
#   sudo ./deploy/install-vps.sh                  installa o aggiorna
#   sudo ./deploy/install-vps.sh --uninstall      rimuove servizio e binari
#
# Presuppone che accanto a questo script ci sia dist/ prodotto da publish.sh.
set -euo pipefail

APP_DIR=/opt/falezia
DATA_DIR=/var/lib/falezia
ETC_DIR=/etc/falezia
SERVICE=falezia-control.service
UNIT=/etc/systemd/system/$SERVICE

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST="$HERE/dist"
[ -d "$DIST" ] || DIST="$(cd "$HERE/.." && pwd)/dist"

log() { printf '\033[1;34m→\033[0m %s\n' "$*"; }
die() { printf '\033[1;31m✗\033[0m %s\n' "$*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || die "serve root: sudo $0"

if [ "${1:-}" = "--uninstall" ]; then
  log "fermo e rimuovo il servizio"
  systemctl disable --now "$SERVICE" 2>/dev/null || true
  rm -f "$UNIT"
  systemctl daemon-reload
  rm -rf "$APP_DIR"
  log "binari rimossi. Dati e configurazione restano in $DATA_DIR e $ETC_DIR"
  exit 0
fi

[ -f "$DIST/control/Falezia.Control" ] || die "manca $DIST/control/Falezia.Control — lancia prima ./deploy/publish.sh"
[ -f "$DIST/server/Falezia.Server" ] || die "manca $DIST/server/Falezia.Server — lancia prima ./deploy/publish.sh"

if ! id falezia >/dev/null 2>&1; then
  log "creo l'utente di servizio falezia"
  useradd --system --home "$APP_DIR" --shell /usr/sbin/nologin falezia
fi

log "fermo il servizio, se gira"
systemctl stop "$SERVICE" 2>/dev/null || true

log "copio i binari in $APP_DIR"
mkdir -p "$APP_DIR/control" "$APP_DIR/server" "$DATA_DIR" "$ETC_DIR"
cp -a "$DIST/control/." "$APP_DIR/control/"
cp -a "$DIST/server/." "$APP_DIR/server/"
chmod +x "$APP_DIR/control/Falezia.Control" "$APP_DIR/server/Falezia.Server"
chown -R falezia:falezia "$APP_DIR" "$DATA_DIR"
chmod 750 "$DATA_DIR"

if [ ! -f "$ETC_DIR/control.env" ]; then
  log "genero $ETC_DIR/control.env con un token nuovo"
  TOKEN="$(head -c 32 /dev/urandom | od -An -tx1 | tr -d ' \n')"
  PUBLIC="${FALEZIA_PUBLIC_HOST:-}"
  if [ -z "$PUBLIC" ]; then
    PUBLIC="$(curl -fsS -4 -m 4 https://ifconfig.me 2>/dev/null || true)"
  fi
  [ -n "$PUBLIC" ] || PUBLIC="127.0.0.1"
  cat > "$ETC_DIR/control.env" <<ENV
FALEZIA_CONTROL_TOKEN=$TOKEN
FALEZIA_CONTROL_BIND=0.0.0.0
FALEZIA_CONTROL_PORT=8090
FALEZIA_PUBLIC_HOST=$PUBLIC
FALEZIA_HARD_LIMIT=true
FALEZIA_HARD_LIMIT_MAX=2
ENV
  chmod 600 "$ETC_DIR/control.env"
  log "host pubblico impostato a $PUBLIC (cambialo in $ETC_DIR/control.env se sbagliato)"
else
  log "$ETC_DIR/control.env già presente: lo lascio com'è"
fi

log "installo l'unità systemd"
install -m 644 "$HERE/$SERVICE" "$UNIT"
systemctl daemon-reload
systemctl enable --now "$SERVICE"

sleep 2
if systemctl is-active --quiet "$SERVICE"; then
  TOKEN="$(grep -oP '(?<=FALEZIA_CONTROL_TOKEN=).*' "$ETC_DIR/control.env" || true)"
  PUBLIC="$(grep -oP '(?<=FALEZIA_PUBLIC_HOST=).*' "$ETC_DIR/control.env" || echo 127.0.0.1)"
  PORT="$(grep -oP '(?<=FALEZIA_CONTROL_PORT=).*' "$ETC_DIR/control.env" || echo 8090)"
  echo
  log "servizio attivo"
  echo "  pannello   http://$PUBLIC:$PORT/?token=$TOKEN"
  echo "  stato      systemctl status $SERVICE"
  echo "  log        journalctl -u $SERVICE -f"
else
  die "il servizio non è partito: journalctl -u $SERVICE -n 50"
fi

echo
log "firewall (ufw): apri il pannello e le porte UDP delle partite"
cat <<UFW
  ufw allow 22/tcp
  ufw allow $PORT/tcp
  ufw allow 27015:27099/udp
  ufw enable
UFW
echo
echo "  Il pannello è HTTP in chiaro: mettilo dietro un tunnel SSH"
echo "    ssh -L $PORT:127.0.0.1:$PORT utente@vps"
echo "  oppure chiudilo al mondo e lascialo su 127.0.0.1 nel env."
