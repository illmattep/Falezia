"""Genera con Meshy i due arredi del Porto Vecchio (spawn Nord).

Il protocollo dell'addon "MCP for Blender" parla su 127.0.0.1:9876 con JSON
{"type": ..., "params": ...} e risposta JSON senza terminatore.

Uso:
    python3 tools/calanera2/meshy_spawn.py [id ...]

Scarica i GLB in art/map-assets/generated/<id>.glb e scrive <id>.json con i
dati della richiesta (per rifare la stessa generazione senza indovinare).
"""
import json
import os
import socket
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "art", "map-assets", "generated")
BLENDER = ("127.0.0.1", 9876)

STILE = (
    "Stylized game prop, Valorant-like art direction: clean readable forms, "
    "matte PBR, subtle hand-painted wear, no photorealism. Single object, "
    "no ground plane, no text, no shadow, no background, no baked lighting."
)

PROMPTS = {
    "rete_porto": (
        "Pile of fishing nets on the ground, tangled green and orange netting, cork "
        "floats and two small faded buoys caught inside, salt-stained rope coiled on "
        "top. Real-world scale 2.0 x 0.6 x 1.4 m. " + STILE
    ),
    "casse_pesce": (
        "Stack of weathered light-wood fish crates with iron straps, crushed ice and a "
        "few fish inside, two crates slightly offset and one lid open. Real-world scale "
        "2.5 x 1.0 x 2.5 m. " + STILE
    ),
}


def blender(kind_name, timeout=600, **params):
    s = socket.create_connection(BLENDER, timeout=timeout)
    try:
        s.sendall(json.dumps({"type": kind_name, "params": params}).encode())
        buf = b""
        while True:
            chunk = s.recv(65536)
            if not chunk:
                break
            buf += chunk
            try:
                return json.loads(buf)
            except json.JSONDecodeError:
                pass
        raise RuntimeError(f"risposta non JSON: {buf[:400]!r}")
    finally:
        s.close()


def wait(task_id, kind, label, every=20, limit=1800):
    t0 = time.time()
    last = None
    while time.time() - t0 < limit:
        r = blender("poll_meshy_job_status", task_id=task_id, kind=kind, timeout=120)
        res = r.get("result") or r
        status = res.get("status")
        progress = res.get("progress")
        line = f"{label}: {status} {progress}% ({int(time.time() - t0)} s)"
        if line != last:
            print(line, flush=True)
            last = line
        if status == "SUCCEEDED":
            return res
        if status in ("FAILED", "CANCELED") or res.get("error"):
            raise RuntimeError(f"{label}: {res}")
        time.sleep(every)
    raise TimeoutError(label)


def generate(ident, preview_id=None):
    prompt = PROMPTS[ident]
    os.makedirs(OUT, exist_ok=True)
    print(f"== {ident}: {len(prompt)} caratteri", flush=True)
    if preview_id:
        kind = "t2p"
        print("riprendo il preview", preview_id, flush=True)
    else:
        created = blender("create_meshy_job", text_prompt=prompt, timeout=180)
        print("create:", json.dumps(created)[:400], flush=True)
        if created.get("status") != "success":
            raise RuntimeError(json.dumps(created))
        res = created["result"]
        preview_id, kind = res["task_id"], res["kind"]

    wait(preview_id, kind, f"{ident} preview")
    refined = blender("refine_meshy_job", preview_task_id=preview_id, timeout=180)
    print("refine:", json.dumps(refined)[:400], flush=True)
    if refined.get("status") != "success":
        raise RuntimeError(json.dumps(refined))
    res = refined["result"]
    final = wait(res["task_id"], res["kind"], f"{ident} texture")

    url = final["model_url"]
    path = os.path.join(OUT, ident + ".glb")
    print("scarico", url, "->", path, flush=True)
    urllib.request.urlretrieve(url, path)
    meta = {k: final.get(k) for k in ("status", "progress", "model_url")}
    meta.update(id=ident, prompt=prompt, preview_task_id=preview_id,
                texture_task_id=res["task_id"], bytes=os.path.getsize(path))
    with open(os.path.join(OUT, ident + ".json"), "w") as f:
        json.dump(meta, f, indent=1)
    print(f"== {ident} pronto: {os.path.getsize(path) / 1e6:.1f} MB", flush=True)


if __name__ == "__main__":
    argv = sys.argv[1:]
    preview = None
    if "--preview" in argv:
        i = argv.index("--preview")
        preview = argv[i + 1]
        del argv[i:i + 2]
    ids = argv or ["rete_porto", "casse_pesce"]
    for ident in ids:
        generate(ident, preview_id=preview if ident == ids[0] else None)
