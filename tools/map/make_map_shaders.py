#!/usr/bin/env python3
"""Shader della mappa, derivati dallo shader Lit di URP installato nel progetto.

Perché: URP/Lit accende normal map, maschera, occlusione, dettaglio e alpha-test con
keyword "shader_feature". Su materiali creati da codice quelle varianti non arrivano
nella build (stesso problema dei fori dei proiettili con URP/Unlit, vedi ai/STATE.md):
risultato, strade a specchio e niente rilievo. Qui le stesse passate di Lit,
con le funzioni ACCESE da #define: nessuna keyword da perdere.

Uso:  python3 tools/map/make_map_shaders.py   (rilancialo se aggiorni URP)
Esce in Assets/Resources/Shaders/MapLit.shader. (L'erba non ne ha bisogno: è geometria opaca.)
"""
import glob
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..", "..")
OUT = os.path.join(ROOT, "Assets", "Resources", "Shaders")

VARIANTS = {
    "MapLit": {
        "name": "Falezia/MapLit",
        "defines": ["_NORMALMAP", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP", "_DETAIL_MULX2"],
        "cull": "Back",
        "tags": None,
        "doc": "Superfici della mappa: normal map, maschera (R metallo · G occlusione · A levigatezza) e variazione larga sempre accese.",
    },
}


def find_lit(argv):
    if len(argv) > 1:
        return argv[1]
    hits = glob.glob(os.path.join(ROOT, "Library", "PackageCache", "com.unity.render-pipelines.universal@*", "Shaders", "Lit.shader"))
    if not hits:
        raise SystemExit("Lit.shader di URP non trovato: apri il progetto in Unity una volta, o passa il percorso")
    return hits[0]


def convert(src: str, v: dict) -> str:
    out = []
    lines = src.splitlines()
    for i, line in enumerate(lines):
        s = line.strip()
        if i == 0 and s.startswith("Shader "):
            out.append(f"// {v['doc']}")
            out.append("// GENERATO da tools/map/make_map_shaders.py partendo da URP/Lit: non modificare a mano.")
            out.append(f'Shader "{v["name"]}"')
            continue
        if s.startswith("#pragma shader_feature"):
            continue                                   # le funzioni si accendono qui sotto, con #define
        if s.startswith("CustomEditor"):
            continue
        if s.startswith("Cull[_Cull]"):
            out.append(line.replace("Cull[_Cull]", f"Cull {v['cull']}"))
            continue
        if s.startswith("Blend[_SrcBlend]"):
            out.append(line[: len(line) - len(line.lstrip())] + "Blend One Zero")
            continue
        if s.startswith("ZWrite[_ZWrite]"):
            out.append(line.replace("ZWrite[_ZWrite]", "ZWrite On"))
            continue
        if s.startswith("AlphaToMask[_AlphaToMask]"):
            out.append(line.replace("AlphaToMask[_AlphaToMask]", "AlphaToMask Off"))
            continue
        if v["tags"] and v["tags"][0] in line:
            out.append(line.replace(v["tags"][0], v["tags"][1]))
            continue
        out.append(line)
        if s == "HLSLPROGRAM":
            pad = line[: len(line) - len(line.lstrip())]
            for d in v["defines"]:
                out.append(f"{pad}#define {d} 1")
    return "\n".join(out) + "\n"


def main(argv):
    lit = find_lit(argv)
    src = open(lit, encoding="utf-8").read()
    os.makedirs(OUT, exist_ok=True)
    for file, v in VARIANTS.items():
        path = os.path.join(OUT, file + ".shader")
        with open(path, "w", encoding="utf-8") as f:
            f.write(convert(src, v))
        print("scritto", os.path.relpath(path, ROOT))


if __name__ == "__main__":
    main(sys.argv)
