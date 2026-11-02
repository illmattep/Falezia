#!/usr/bin/env python3
"""
I SUONI DELLE ARMI — generati, non campionati.

Perché non i suoni di Valorant: sono di Riot, non si possono usare. Perché non una
libreria CC0: 17 armi × 7 suoni = 119 file da scegliere a orecchio, con il rischio di
due armi che suonano uguali. Qui ogni arma è una **scheda di parametri** e il suono esce
da un modello fisico semplificato: il risultato è originale, riproducibile (stesso seme =
stesso file) e ogni arma ha il suo carattere.

Il modello di uno sparo ha cinque strati, come un colpo vero:

  1. **crepitio**  — il fronte d'onda supersonico: rumore filtrato in alto, 2–3 ms
  2. **corpo**     — l'esplosione dentro la canna: rumore filtrato con una risonanza
  3. **tonfo**     — la spinta sul petto: sinusoide che scende (130 → 45 Hz)
  4. **meccanica** — otturatore, carrello, pompa: click metallici in ritardo di 12–50 ms
  5. **coda**      — il ritorno del muro: riverbero sintetico, più o meno lungo

Ricarica, estrazione, colpo a vuoto e fendente sono **timeline di eventi meccanici**
(click, tonfo, slide, caricatore, raffica di cricchetto) costruite sulla durata VERA
della ricarica letta da `game/sim/Weapons.cs`: il suono finisce quando finisce l'animazione.

Uso:
    python3 tools/audio/make_weapon_sounds.py            # genera tutto
    python3 tools/audio/make_weapon_sounds.py --preview  # + foglio di controllo PNG
    ./run.sh sound

Requisiti: Python 3 + numpy (+ Pillow solo per --preview). Nessun campione di terzi.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
WEAPONS_CS = ROOT / "game" / "sim" / "Weapons.cs"
OUT = ROOT / "Assets" / "StreamingAssets" / "audio" / "weapons"
PREVIEW = ROOT / "tools" / "audio" / "preview"

SR = 48_000              # 48 kHz: il crepitio ha bisogno di aria in alto
SEED = 20261026          # il seme della sessione: stessi file a ogni esecuzione


# ---------------------------------------------------------------- primitive audio

def nsamp(sec: float) -> int:
    return max(1, int(round(sec * SR)))


def white(n: int, rng) -> np.ndarray:
    return rng.standard_normal(n)


def fir_lp(x: np.ndarray, fc: float, taps: int = 257) -> np.ndarray:
    """Passa-basso FIR a fase lineare (sinc finestrato). Semplice e senza scipy."""
    fc = max(20.0, min(fc, SR * 0.45))
    m = np.arange(taps) - (taps - 1) / 2.0
    h = np.sinc(2.0 * fc / SR * m) * np.hamming(taps)
    h /= h.sum()
    return np.convolve(x, h, mode="same")


def fir_hp(x: np.ndarray, fc: float) -> np.ndarray:
    return x - fir_lp(x, fc)


def fir_bp(x: np.ndarray, lo: float, hi: float) -> np.ndarray:
    return fir_lp(x, hi) - fir_lp(x, lo)


def decay(n: int, tau: float, attack: float = 0.0015) -> np.ndarray:
    """Inviluppo: attacco corto, poi decadimento esponenziale."""
    t = np.arange(n) / SR
    env = np.exp(-t / max(1e-5, tau))
    a = max(1, min(n, nsamp(attack)))
    env[:a] *= np.linspace(0.0, 1.0, a)
    return env


def glide(n: int, f0: float, f1: float, curve: float = 1.0) -> np.ndarray:
    t = np.arange(n) / SR
    dur = max(1e-6, n / SR)
    f = f0 * (f1 / f0) ** ((t / dur) ** curve)
    return np.sin(2.0 * np.pi * np.cumsum(f) / SR)


def place(buf: np.ndarray, sig: np.ndarray, at: float, gain: float = 1.0) -> None:
    """Somma `sig` dentro `buf` a partire dal tempo `at` (secondi), in place."""
    i = nsamp(at)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sig))
    buf[i:j] += gain * sig[: j - i]


def soft_clip(x: np.ndarray, drive: float = 2.2) -> np.ndarray:
    return np.tanh(x * drive) / np.tanh(drive)


def room_ir(tau: float, rng, brightness: float = 5000.0) -> np.ndarray:
    """Risposta all'impulso sintetica: rumore che decade + prime riflessioni."""
    n = nsamp(max(0.08, tau * 4.0))
    t = np.arange(n) / SR
    ir = white(n, rng) * np.exp(-t / tau)
    ir = fir_lp(ir, brightness)
    for delay, g in ((0.006, 0.55), (0.011, 0.4), (0.019, 0.3), (0.031, 0.22)):
        i = nsamp(delay)
        if i < n:
            ir[i] += g
    ir /= np.max(np.abs(ir)) + 1e-9
    return ir


def convolve_fft(x: np.ndarray, h: np.ndarray) -> np.ndarray:
    n = 1 << int(np.ceil(np.log2(len(x) + len(h) - 1)))
    y = np.fft.irfft(np.fft.rfft(x, n) * np.fft.rfft(h, n), n)
    return y[: len(x)]


def finish(x: np.ndarray, peak_db: float = -1.0, fade_ms: float = 3.0) -> np.ndarray:
    """Toglie la continua, sfuma gli estremi (niente click) e normalizza il picco."""
    x = x - float(np.mean(x))
    f = min(nsamp(fade_ms / 1000.0), len(x) // 2)
    if f > 1:
        x[:f] *= np.linspace(0.0, 1.0, f)
        x[-f:] *= np.linspace(1.0, 0.0, f)
    p = float(np.max(np.abs(x))) + 1e-9
    return x * (10.0 ** (peak_db / 20.0) / p)


def write_wav(path: Path, x: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.clip(x, -1.0, 1.0)
    pcm = (pcm * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


# ---------------------------------------------------------------- le schede delle armi

# Ogni voce è il CARATTERE dell'arma. I numeri sono Hz, secondi e guadagni relativi.
#
# crack / crack_hp   crepitio: quanto è secco e quanto sta in alto
# body / body_cut    corpo: risonanza della canna e taglio del filtro
# thump              tonfo sul petto: da dove parte, dove arriva, quanto dura
# mech / mech_delay  meccanica: quanto si sente l'otturatore e dopo quanto
# tail               coda: durata e percentuale di ritorno
# puff               (solo silenziate) lo sbuffo del moderatore
# level              picco del file in dBFS
DESIGN = {
    "standard": dict(crack=0.95, crack_hp=3400, body=1.00, body_cut=2600, body_f=195, body_q=0.9,
                     body_tau=0.045, thump=0.55, thump_f0=130, thump_f1=48, thump_tau=0.050,
                     mech=0.35, mech_delay=0.022, tail=0.28, tail_mix=0.30, level=-1.0,
                     reload="pistol"),
    "hush": dict(crack=0.12, crack_hp=6200, body=0.55, body_cut=900, body_f=150, body_q=1.1,
                 body_tau=0.050, thump=0.35, thump_f0=110, thump_f1=42, thump_tau=0.060,
                 mech=0.50, mech_delay=0.030, tail=0.16, tail_mix=0.18, puff=0.55, level=-7.0,
                 reload="pistol"),
    "chatter": dict(crack=1.00, crack_hp=4200, body=0.90, body_cut=3200, body_f=240, body_q=1.0,
                    body_tau=0.032, thump=0.45, thump_f0=140, thump_f1=55, thump_tau=0.042,
                    mech=0.50, mech_delay=0.016, tail=0.16, tail_mix=0.26, level=-1.5,
                    reload="pistol"),
    "snub": dict(crack=1.00, crack_hp=3200, body=1.15, body_cut=1800, body_f=130, body_q=1.4,
                 body_tau=0.070, thump=0.75, thump_f0=115, thump_f1=40, thump_tau=0.080,
                 mech=0.30, mech_delay=0.028, tail=0.55, tail_mix=0.38, level=-1.0,
                 reload="revolver"),
    "hammer": dict(crack=1.00, crack_hp=2800, body=1.30, body_cut=1400, body_f=105, body_q=1.5,
                   body_tau=0.095, thump=0.95, thump_f0=105, thump_f1=36, thump_tau=0.105,
                   mech=0.30, mech_delay=0.030, tail=0.85, tail_mix=0.45, level=-0.5,
                   reload="magnum"),
    "cadence": dict(crack=0.95, crack_hp=4000, body=0.95, body_cut=2800, body_f=220, body_q=1.0,
                    body_tau=0.030, thump=0.45, thump_f0=135, thump_f1=52, thump_tau=0.040,
                    mech=0.55, mech_delay=0.014, tail=0.15, tail_mix=0.24, level=-1.5,
                    reload="smg"),
    "scrap": dict(crack=0.85, crack_hp=4800, body=0.80, body_cut=3000, body_f=265, body_q=0.8,
                  body_tau=0.026, thump=0.35, thump_f0=150, thump_f1=60, thump_tau=0.036,
                  mech=0.65, mech_delay=0.012, tail=0.12, tail_mix=0.20, level=-2.0,
                  reload="smg"),
    "blitz": dict(crack=1.00, crack_hp=5200, body=0.85, body_cut=3400, body_f=300, body_q=0.9,
                  body_tau=0.022, thump=0.30, thump_f0=155, thump_f1=65, thump_tau=0.032,
                  mech=0.60, mech_delay=0.011, tail=0.12, tail_mix=0.20, level=-2.0,
                  reload="smg"),
    "choke": dict(crack=0.80, crack_hp=2600, body=1.35, body_cut=1200, body_f=95, body_q=1.5,
                  body_tau=0.090, thump=1.00, thump_f0=100, thump_f1=34, thump_tau=0.120,
                  mech=0.35, mech_delay=0.050, tail=0.70, tail_mix=0.40, pellets=12, level=0.0,
                  reload="pump"),
    "sweeper": dict(crack=0.85, crack_hp=2600, body=1.20, body_cut=1400, body_f=110, body_q=1.4,
                    body_tau=0.070, thump=0.85, thump_f0=110, thump_f1=40, thump_tau=0.090,
                    mech=0.50, mech_delay=0.040, tail=0.48, tail_mix=0.34, pellets=10, level=-0.5,
                    reload="auto_shotgun"),
    "anvil": dict(crack=1.05, crack_hp=3600, body=1.20, body_cut=2000, body_f=150, body_q=1.3,
                  body_tau=0.060, thump=0.70, thump_f0=115, thump_f1=42, thump_tau=0.070,
                  mech=0.45, mech_delay=0.024, tail=0.38, tail_mix=0.35, level=-0.5,
                  reload="rifle"),
    "service": dict(crack=1.00, crack_hp=4200, body=1.05, body_cut=2600, body_f=190, body_q=1.1,
                    body_tau=0.045, thump=0.55, thump_f0=125, thump_f1=48, thump_tau=0.052,
                    mech=0.45, mech_delay=0.020, tail=0.28, tail_mix=0.30, level=-1.0,
                    reload="rifle"),
    "vantage": dict(crack=1.10, crack_hp=3400, body=1.20, body_cut=1700, body_f=135, body_q=1.4,
                    body_tau=0.075, thump=0.80, thump_f0=110, thump_f1=40, thump_tau=0.080,
                    mech=0.40, mech_delay=0.030, tail=0.70, tail_mix=0.40, level=-0.5,
                    reload="rifle"),
    "spotter": dict(crack=1.15, crack_hp=3200, body=1.10, body_cut=1600, body_f=120, body_q=1.4,
                    body_tau=0.080, thump=0.85, thump_f0=108, thump_f1=38, thump_tau=0.090,
                    mech=0.40, mech_delay=0.034, tail=0.90, tail_mix=0.45, level=0.0,
                    reload="sniper"),
    "longshot": dict(crack=1.25, crack_hp=3000, body=1.25, body_cut=1400, body_f=100, body_q=1.6,
                     body_tau=0.100, thump=1.00, thump_f0=100, thump_f1=34, thump_tau=0.115,
                     mech=0.35, mech_delay=0.040, tail=1.20, tail_mix=0.50, level=0.5,
                     reload="bolt"),
    "heavy": dict(crack=0.95, crack_hp=3200, body=1.15, body_cut=1800, body_f=125, body_q=1.3,
                  body_tau=0.065, thump=0.80, thump_f0=112, thump_f1=40, thump_tau=0.080,
                  mech=0.50, mech_delay=0.025, tail=0.30, tail_mix=0.32, level=-0.5,
                  reload="lmg"),
    "melee": dict(melee=True),
}


# ---------------------------------------------------------------- gli eventi meccanici

def ev_click(rng, bright=4600.0, dur=0.010, ping=3000.0) -> np.ndarray:
    """Click metallico: rumore brevissimo + un ping che gli dà il metallo."""
    n = nsamp(dur)
    x = fir_bp(white(n, rng), bright * 0.45, bright * 2.0) * decay(n, dur * 0.30, 0.0002)
    t = np.arange(n) / SR
    x += 0.35 * np.sin(2 * np.pi * ping * t) * decay(n, dur * 0.35, 0.0002)
    return x / (np.max(np.abs(x)) + 1e-9)


def ev_thunk(rng, f=95.0, dur=0.070) -> np.ndarray:
    """Tonfo sordo: caricatore che entra, culatta che sbatte."""
    n = nsamp(dur)
    x = fir_lp(white(n, rng), 900) * decay(n, dur * 0.28)
    x += 0.8 * glide(n, f, f * 0.6) * decay(n, dur * 0.35)
    return x / (np.max(np.abs(x)) + 1e-9)


def ev_slide(rng, dur=0.090, lo=900.0, hi=2600.0) -> np.ndarray:
    """Attrito: un pezzo che scorre (carrello, pompa, otturatore)."""
    n = nsamp(dur)
    x = white(n, rng)
    x = fir_bp(x, lo, hi)
    t = np.arange(n) / n
    env = np.sin(np.pi * t) ** 1.5          # entra ed esce
    x *= env
    # un filo di "grana": la superficie non è liscia
    x += 0.15 * fir_hp(white(n, rng), 3000) * env * (0.5 + 0.5 * np.sin(2 * np.pi * 90 * t * dur))
    return x / (np.max(np.abs(x)) + 1e-9)


def ev_shell(rng, dur=0.022) -> np.ndarray:
    """Cartuccia di plastica che entra nel tubo."""
    n = nsamp(dur)
    x = fir_bp(white(n, rng), 1200, 3600) * decay(n, dur * 0.25, 0.0003)
    x += 0.4 * glide(n, 320, 180) * decay(n, dur * 0.3)
    return x / (np.max(np.abs(x)) + 1e-9)


def ev_spring(rng, dur=0.080, f=1500.0) -> np.ndarray:
    """Molla dell'otturatore che si distende."""
    n = nsamp(dur)
    t = np.arange(n) / SR
    mod = 1.0 + 0.35 * np.sin(2 * np.pi * 45 * t)
    x = np.sin(2 * np.pi * f * t * (1.0 - 0.15 * t / dur)) * decay(n, dur * 0.30) * mod
    x += 0.25 * fir_bp(white(n, rng), f * 0.7, f * 1.6) * decay(n, dur * 0.20)
    return x / (np.max(np.abs(x)) + 1e-9)


def ev_shell_drop(rng, dur=0.030) -> np.ndarray:
    """Bossolo espulso che cade a terra: serve alla ricarica dei revolver."""
    n = nsamp(dur)
    x = fir_bp(white(n, rng), 2200, 5200) * decay(n, dur * 0.12, 0.0002)
    x += 0.5 * np.sin(2 * np.pi * 1800 * np.arange(n) / SR) * decay(n, dur * 0.15)
    return x / (np.max(np.abs(x)) + 1e-9)


EVENTS = {
    "click": lambda r, kind="click": ev_click(r),
    "click_hi": lambda r, kind="click_hi": ev_click(r, bright=6200, ping=4300),
    "click_lo": lambda r, kind="click_lo": ev_click(r, bright=2800, ping=1900),
    "thunk": lambda r, kind="thunk": ev_thunk(r),
    "thunk_big": lambda r, kind="thunk_big": ev_thunk(r, f=70, dur=0.110),
    "slide": lambda r, kind="slide": ev_slide(r),
    "slide_fast": lambda r, kind="slide_fast": ev_slide(r, dur=0.055, lo=1400, hi=3400),
    "shell": lambda r, kind="shell": ev_shell(r),
    "spring": lambda r, kind="spring": ev_spring(r),
    "drop": lambda r, kind="drop": ev_shell_drop(r),
}


def reload_timeline(style: str, dur: float, empty: bool, mag: int):
    """La sequenza meccanica della ricarica, in frazioni della durata vera.

    `dur` viene dalla scheda dell'arma: il suono non può durare più dell'animazione,
    altrimenti si sente il caricatore che entra dopo che il fucile ha già sparato.
    """
    ev = []
    if style == "pistol":
        if empty:
            ev += [(0.02, "slide_fast", 0.5)]                 # carrello indietro, resta aperto
        ev += [(0.06, "click_lo", 0.55)]                      # sgancio caricatore
        ev += [(0.16, "slide_fast", 0.45)]                    # caricatore che scivola fuori
        ev += [(0.55 * dur, "thunk", 0.75)]                   # caricatore dentro
        ev += [(0.58 * dur, "click", 0.6)]
        if empty:
            ev += [(0.72 * dur, "slide", 0.55), (0.80 * dur, "click_hi", 0.95),
                   (0.84 * dur, "spring", 0.30)]
        else:
            ev += [(0.80 * dur, "slide", 0.45), (0.86 * dur, "click", 0.8)]
    elif style == "revolver":
        ev += [(0.05 * dur, "click", 0.7)]                     # sblocco tamburo
        ev += [(0.14 * dur, "slide", 0.5)]                     # tamburo che si apre
        for i in range(6):                                     # sei bossoli che escono
            ev += [(0.22 * dur + i * 0.035, "drop", 0.45)]
        ev += [(0.62 * dur, "thunk", 0.6)]                     # speedloader
        ev += [(0.68 * dur, "click_hi", 0.9)]
        ev += [(0.86 * dur, "click", 0.85)]                    # tamburo che si chiude
        ev += [(0.90 * dur, "click_lo", 0.5)]
    elif style == "magnum":
        ev += [(0.04 * dur, "click", 0.7)]
        ev += [(0.12 * dur, "slide", 0.5)]
        for i in range(7):
            ev += [(0.20 * dur + i * 0.030, "drop", 0.40)]
        for i in range(7):                                     # cartucce una a una
            ev += [(0.42 * dur + i * 0.028, "shell", 0.75)]
        ev += [(0.86 * dur, "click", 0.9), (0.90 * dur, "click_lo", 0.55)]
    elif style == "smg":
        if empty:
            ev += [(0.02, "slide_fast", 0.5)]
        ev += [(0.05, "click_lo", 0.55)]
        ev += [(0.14, "slide", 0.55)]                          # caricatore lungo che esce
        ev += [(0.52 * dur, "thunk", 0.85), (0.56 * dur, "click", 0.6)]
        if empty:
            ev += [(0.74 * dur, "slide", 0.6), (0.84 * dur, "click_hi", 1.0)]
        else:
            ev += [(0.82 * dur, "click", 0.85)]
    elif style == "rifle":
        if empty:
            ev += [(0.02, "slide_fast", 0.55)]
        ev += [(0.05, "click_lo", 0.6)]
        ev += [(0.15, "slide", 0.55)]
        ev += [(0.50 * dur, "thunk", 0.9), (0.55 * dur, "click", 0.7)]
        if empty:
            ev += [(0.70 * dur, "slide", 0.6), (0.82 * dur, "click_hi", 1.0),
                   (0.86 * dur, "spring", 0.35)]
        else:
            ev += [(0.80 * dur, "click", 0.8)]
    elif style == "sniper":
        ev += [(0.04, "click_lo", 0.6)]
        ev += [(0.12, "slide", 0.5)]
        ev += [(0.42 * dur, "thunk", 0.85), (0.46 * dur, "click", 0.7)]
        ev += [(0.66 * dur, "slide", 0.6)]                     # otturatore
        ev += [(0.78 * dur, "click_hi", 1.0)]
        if empty:
            ev += [(0.88 * dur, "click_hi", 0.7)]
    elif style == "bolt":
        ev += [(0.03, "click_lo", 0.6)]
        ev += [(0.10, "slide", 0.55)]
        ev += [(0.34 * dur, "thunk", 0.95), (0.38 * dur, "click", 0.7)]
        ev += [(0.56 * dur, "click_hi", 0.9)]                  # maniglia dell'otturatore su
        ev += [(0.68 * dur, "slide", 0.75)]                    # indietro
        ev += [(0.80 * dur, "slide", 0.75)]                    # avanti
        ev += [(0.88 * dur, "click_hi", 1.0)]                  # giù e chiuso
    elif style == "pump":
        # una cartuccia alla volta: `mag` cartucce distribuite, poi la pompa
        first, last = 0.06, 0.72 * dur
        step = (last - first) / max(1, mag - 1)
        for i in range(mag):
            t = first + i * step
            ev += [(t, "shell", 0.8), (t + 0.030, "click_lo", 0.45)]
        ev += [(0.78 * dur, "slide", 0.85), (0.88 * dur, "thunk", 0.5)]
    elif style == "auto_shotgun":
        ev += [(0.04, "click_lo", 0.5)]
        ev += [(0.14, "slide", 0.6)]
        for i in range(4):                                     # cartucce nel tubo
            ev += [(0.30 + i * 0.055, "shell", 0.7)]
        ev += [(0.68 * dur, "slide", 0.8), (0.80 * dur, "click_hi", 0.9)]
        if empty:
            ev += [(0.90 * dur, "click_hi", 0.8)]
    elif style == "lmg":
        ev += [(0.03, "click_lo", 0.6)]
        ev += [(0.10, "slide", 0.5)]                           # coperchio
        for i in range(10):                                    # nastro che scorre
            ev += [(0.20 + i * 0.035, "click_lo", 0.35)]
        ev += [(0.58 * dur, "thunk_big", 0.95)]                # coperchio che sbatte
        ev += [(0.64 * dur, "click", 0.8)]
        ev += [(0.78 * dur, "slide", 0.7)]                     # maniglione
        ev += [(0.88 * dur, "click_hi", 1.0)]
    return [(min(t, dur - 0.02), kind, amp) for t, kind, amp in ev]


# ---------------------------------------------------------------- i suoni

def make_shot(d: dict, rng, last: bool = False) -> np.ndarray:
    """Uno sparo: cinque strati sovrapposti, più la coda del muro."""
    tail_tau = d["tail"]
    dur = 0.055 + tail_tau * 3.2
    n = nsamp(dur)
    t = np.arange(n) / SR
    buf = np.zeros(n)

    # 1. crepitio: il fronte supersonico, secco e brevissimo
    crack = fir_hp(white(n, rng), d["crack_hp"]) * decay(n, 0.0022, 0.0002)
    crack /= np.max(np.abs(crack)) + 1e-9
    buf += d["crack"] * crack

    # 2. corpo: l'esplosione nella canna, con la risonanza del tubo
    blast = fir_lp(white(n, rng), d["body_cut"]) * decay(n, d["body_tau"])
    blast /= np.max(np.abs(blast)) + 1e-9
    ring = np.sin(2 * np.pi * d["body_f"] * t) * decay(n, d["body_tau"] * 0.7)
    body = soft_clip(blast * 0.85 + ring * 0.35 * d["body_q"])
    body /= np.max(np.abs(body)) + 1e-9
    buf += d["body"] * body

    # 3. tonfo: la spinta sul petto, una sinusoide che scende
    thump = glide(n, d["thump_f0"], d["thump_f1"], 0.7) * decay(n, d["thump_tau"])
    buf += d["thump"] * thump / (np.max(np.abs(thump)) + 1e-9)

    # 4. meccanica: otturatore/carrello che sbatte subito dopo
    mech = ev_click(rng, bright=5200, ping=3600)
    place(buf, mech, d["mech_delay"], d["mech"] * 0.9)
    place(buf, ev_click(rng, bright=3800, ping=2400), d["mech_delay"] + 0.011, d["mech"] * 0.45)

    # 5. silenziata: il moderatore non fa crepitio, fa uno sbuffo
    if d.get("puff"):
        puff = fir_lp(white(n, rng), 1500) * decay(n, 0.030)
        buf += d["puff"] * puff / (np.max(np.abs(puff)) + 1e-9)

    # le pompe: le palle che escono insieme fanno una "spruzzata" di rumore in più
    if d.get("pellets"):
        spray = fir_bp(white(n, rng), 1200, 5200) * decay(n, 0.045)
        buf += 0.30 * spray / (np.max(np.abs(spray)) + 1e-9)

    # 6. coda: il ritorno dei muri
    ir = room_ir(tail_tau * 0.6, rng, brightness=4200 if not d.get("puff") else 2000)
    wet = convolve_fft(buf.copy(), ir)
    wet /= np.max(np.abs(wet)) + 1e-9
    buf = buf + d["tail_mix"] * wet[: n] if len(wet) >= n else buf

    # 7. ultimo colpo: l'otturatore resta indietro, si sente il click del fermo
    if last:
        lock = ev_click(rng, bright=5600, ping=4100)
        place(buf, lock, 0.055, 0.55)
        place(buf, ev_click(rng, bright=3000, ping=1700), 0.085, 0.30)

    return finish(buf, d["level"])


def make_reload(d: dict, rng, dur: float, empty: bool) -> np.ndarray:
    """Ricarica: timeline meccanica sulla durata VERA della scheda."""
    n = nsamp(dur + 0.05)
    buf = np.zeros(n)
    for at, kind, amp in reload_timeline(d["reload"], dur, empty, d.get("mag", 10)):
        # ogni click è diverso dall'altro: il metallo non è mai identico
        sig = EVENTS[kind](rng)
        place(buf, sig, at, amp * rng.uniform(0.88, 1.06))

    # il locale: un filo di ritorno, corto (le mani sono vicine)
    ir = room_ir(0.10, rng, brightness=3500)
    wet = convolve_fft(buf.copy(), ir)
    wet /= np.max(np.abs(wet)) + 1e-9
    buf += 0.10 * wet[:n]

    # deve finire ESATTAMENTE con la ricarica: niente coda oltre il tempo
    buf = buf[: nsamp(dur)]
    return finish(buf, d.get("level", -2.0) - 2.0)


def make_draw(d: dict, rng) -> np.ndarray:
    """Estrarre l'arma: stoffa, cinghia, e il ferro che si assesta."""
    n = nsamp(0.34)
    buf = np.zeros(n)
    cloth = fir_bp(white(n, rng), 400, 2600) * decay(n, 0.075, 0.012)
    buf += 0.35 * cloth / (np.max(np.abs(cloth)) + 1e-9)
    place(buf, ev_slide(rng, 0.12, 700, 2200), 0.05, 0.30)
    place(buf, ev_click(rng, 3600, 0.014, 2600), 0.16, 0.45)
    place(buf, ev_thunk(rng, 120, 0.06), 0.20, 0.35)
    place(buf, ev_click(rng, 2800, 0.010, 1900), 0.255, 0.28)
    return finish(buf, d.get("level", -1.0) - 5.0)


def make_empty(d: dict, rng) -> np.ndarray:
    """Colpo a vuoto: il percussore che batte sul nulla."""
    n = nsamp(0.13)
    buf = np.zeros(n)
    place(buf, ev_click(rng, 6200, 0.012, 4400), 0.004, 0.85)
    place(buf, ev_click(rng, 3400, 0.010, 2200), 0.020, 0.35)
    place(buf, ev_spring(rng, 0.06, 1700), 0.030, 0.18)
    return finish(buf, d.get("level", -1.0) - 4.0)


def make_drop(d: dict, rng) -> np.ndarray:
    """L'arma che cade: ferro sul cemento. Un colpo secco, un rimbalzo, la canna che stride."""
    dur = 0.55
    n = nsamp(dur)
    buf = np.zeros(n)

    # peso dell'arma: le pesanti sbattono più grosso e più a lungo
    heavy = 1.0 if d.get("pellets") or d.get("body_tau", 0.05) > 0.06 else 0.7

    # 1. primo impatto: tonfo + click metallico
    place(buf, ev_thunk(rng, 70 if heavy > 0.9 else 110, 0.10), 0.0, 0.95 * heavy)
    place(buf, ev_click(rng, 4200, 0.016, 3000), 0.004, 0.75)
    place(buf, ev_click(rng, 2600, 0.012, 1800), 0.013, 0.45)

    # 2. rimbalzo, più leggero e più acuto
    place(buf, ev_thunk(rng, 120, 0.06), 0.105 + rng.uniform(0, 0.03), 0.42)
    place(buf, ev_click(rng, 5200, 0.012, 3800), 0.115 + rng.uniform(0, 0.03), 0.55)

    # 3. il ferro che si assesta: attrito corto
    place(buf, ev_slide(rng, 0.14, 900, 2800), 0.19, 0.22)
    place(buf, ev_click(rng, 3400, 0.010, 2200), 0.30 + rng.uniform(0, 0.04), 0.18)

    # 4. il locale: un filo di ritorno
    ir = room_ir(0.12, rng, brightness=3500)
    wet = convolve_fft(buf.copy(), ir)
    wet /= np.max(np.abs(wet)) + 1e-9
    buf += 0.14 * wet[:n]

    return finish(buf, -4.0 if heavy > 0.9 else -5.5)


def make_pickup(d: dict, rng) -> np.ndarray:
    """Raccogliere: la mano che afferra (stoffa), il ferro che si assesta, un click."""
    n = nsamp(0.30)
    buf = np.zeros(n)

    cloth = fir_bp(white(n, rng), 350, 2400) * decay(n, 0.055, 0.010)
    buf += 0.45 * cloth / (np.max(np.abs(cloth)) + 1e-9)
    place(buf, ev_click(rng, 3600, 0.012, 2400), 0.075, 0.55)
    place(buf, ev_thunk(rng, 130, 0.05), 0.090, 0.30)
    place(buf, ev_spring(rng, 0.05, 1800), 0.150, 0.12)

    return finish(buf, -6.0)


def make_swing(rng) -> np.ndarray:
    """Il fendente: aria che si apre, non un rumore bianco."""
    n = nsamp(0.30)
    t = np.arange(n) / SR
    src = white(n, rng)
    # il filtro si sposta: 600 Hz all'inizio, 2600 a metà, 700 alla fine
    lo = fir_bp(src, 400, 1400)
    mi = fir_bp(src, 1200, 3200)
    hi = fir_bp(src, 2400, 6000)
    w = np.zeros(n)
    ramp = t / (n / SR)
    w += lo * np.clip(1.0 - ramp * 2.0, 0, 1)
    w += mi * np.sin(np.pi * ramp) ** 1.2
    w += hi * np.clip((ramp - 0.45) * 2.2, 0, 1) * 0.7
    env = np.sin(np.pi * np.clip(ramp * 1.15, 0, 1)) ** 1.6
    buf = w * env
    buf /= np.max(np.abs(buf)) + 1e-9
    # un soffio di stoffa all'inizio del movimento
    cloth = fir_bp(white(n, rng), 300, 1200) * decay(n, 0.05, 0.02)
    buf += 0.12 * cloth / (np.max(np.abs(cloth)) + 1e-9)
    return finish(buf, -6.0)


def make_hit(rng) -> np.ndarray:
    """Il fendente che entra: tonfo sordo + un anello corto di metallo."""
    n = nsamp(0.22)
    buf = np.zeros(n)
    place(buf, ev_thunk(rng, 110, 0.12), 0.0, 0.9)
    place(buf, ev_click(rng, 2600, 0.020, 1500), 0.004, 0.5)
    return finish(buf, -4.0)


# ---------------------------------------------------------------- lettura delle schede

BLOCK_RE = re.compile(r"public static readonly WeaponDef \w+ = new WeaponDef\s*\{(.*?)\n        \};", re.S)


def field(body: str, name: str, default=None):
    m = re.search(rf"\b{name}\s*=\s*([^,\n/]+)", body)
    return m.group(1).strip() if m else default


def parse_weapons() -> list[dict]:
    src = WEAPONS_CS.read_text(encoding="utf-8")
    out = []
    for m in BLOCK_RE.finditer(src):
        body = m.group(1)

        def num(name, default=None):
            v = field(body, name, None)
            if v is None:
                return default
            v = v.rstrip("f").strip()
            try:
                return float(v)
            except ValueError:
                return default

        out.append(dict(
            id=field(body, "Id").strip('"'),
            name=field(body, "Name").strip('"'),
            cls=field(body, "Class").strip('"'),
            magazine=int(num("Magazine", 0)),
            cycle=num("Cycle", 0.25),
            reload_tactical=num("ReloadTactical", 2.0),
            reload_empty=num("ReloadEmpty", 2.5),
            silenced="Silenced = true" in body,
            melee="Melee = true" in body,
        ))
    return out


# ---------------------------------------------------------------- main

def main() -> int:
    ap = argparse.ArgumentParser(description="Genera i suoni delle armi (procedurali)")
    ap.add_argument("--preview", action="store_true", help="scrive un foglio di controllo PNG")
    args = ap.parse_args()

    weapons = parse_weapons()
    if not weapons:
        print("ERRORE: nessuna arma letta da Weapons.cs", file=sys.stderr)
        return 1

    missing = [w["id"] for w in weapons if w["id"] not in DESIGN]
    if missing:
        print(f"ERRORE: schede sonore mancanti per {missing}", file=sys.stderr)
        return 1

    index = []
    rows = []
    for w in weapons:
        d = dict(DESIGN[w["id"]])
        d["mag"] = w["magazine"]
        rng = np.random.default_rng(SEED + sum(map(ord, w["id"])))
        folder = OUT / w["id"]

        files = {}
        if w["melee"]:
            files["swing"] = make_swing(rng)
            files["hit"] = make_hit(rng)
            files["draw"] = make_draw(d, rng)
            files["drop"] = make_drop(d, rng)
            files["pickup"] = make_pickup(d, rng)
        else:
            files["fire"] = make_shot(d, rng)
            files["fire_last"] = make_shot(d, rng, last=True)
            files["reload"] = make_reload(d, rng, w["reload_tactical"], empty=False)
            files["reload_empty"] = make_reload(d, rng, w["reload_empty"], empty=True)
            files["draw"] = make_draw(d, rng)
            files["empty"] = make_empty(d, rng)
            files["drop"] = make_drop(d, rng)      # quando cade a terra
            files["pickup"] = make_pickup(d, rng)  # quando la raccogli

        for name, sig in files.items():
            write_wav(folder / f"{name}.wav", sig)

        index.append(dict(id=w["id"], name=w["name"], cls=w["cls"],
                          reload_tactical=w["reload_tactical"],
                          reload_empty=w["reload_empty"],
                          files={k: round(len(v) / SR, 3) for k, v in files.items()}))

        fire = files.get("fire") if "fire" in files else files["swing"]
        rows.append((w["id"], len(fire) / SR, rms_db(fire), centroid(fire)))

    (OUT / "index.json").write_text(json.dumps(
        dict(seeded=SEED, sample_rate=SR, weapons=index), ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"{'arma':10s} {'durata':>7s} {'RMS':>7s} {'centroide':>10s}")
    print("-" * 40)
    for wid, dur, rms, cen in rows:
        print(f"{wid:10s} {dur:6.2f}s {rms:6.1f}dB {cen:9.0f}Hz")
    n_files = sum(len(f["files"]) for f in index)
    print(f"\n{n_files} file in {OUT.relative_to(ROOT)} · {len(index)} armi · {SR} Hz mono 16 bit")

    if args.preview:
        make_preview(index)
    return 0


def rms_db(x: np.ndarray) -> float:
    return 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-9)


def centroid(x: np.ndarray) -> float:
    spec = np.abs(np.fft.rfft(x))
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    return float((spec * freqs).sum() / (spec.sum() + 1e-9))


def make_preview(index: list[dict]) -> None:
    """Un foglio di controllo: forma d'onda di ogni sparo, per vedere che nessuno clipa."""
    from PIL import Image, ImageDraw

    cols, w, h, pad = 4, 420, 96, 8
    rows = (len(index) + cols - 1) // cols
    img = Image.new("RGB", (cols * (w + pad) + pad, rows * (h + pad + 16) + pad), (18, 20, 24))
    dr = ImageDraw.Draw(img)
    for i, entry in enumerate(index):
        wid = entry["id"]
        name = "fire" if "fire" in entry["files"] else "swing"
        with wave.open(str(OUT / wid / f"{name}.wav"), "rb") as wf:
            raw = np.frombuffer(wf.readframes(wf.getnframes()), dtype="<i2").astype(float) / 32768.0
        x = (i % cols) * (w + pad) + pad
        y = (i // cols) * (h + pad + 16) + pad
        dr.text((x, y), f"{wid} · {name} · {len(raw)/SR:.2f}s", fill=(200, 205, 210))
        step = max(1, len(raw) // w)
        for px in range(w):
            chunk = raw[px * step:(px + 1) * step]
            if len(chunk) == 0:
                break
            lo, hi = float(chunk.min()), float(chunk.max())
            dr.line((x + px, y + 16 + h / 2 * (1 - hi), x + px, y + 16 + h / 2 * (1 - lo)),
                    fill=(220, 90, 80))
        dr.rectangle((x, y + 12, x + w, y + 16 + h), outline=(60, 65, 72))
    PREVIEW.mkdir(parents=True, exist_ok=True)
    path = PREVIEW / "weapon-sounds.png"
    img.save(path)
    print(f"anteprima: {path.relative_to(ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
