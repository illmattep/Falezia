"""Asset hero per la rifinitura di Calanera (Meshy 7.1, testo + texture PBR).

Uso:
    python3 tools/calanera2/rifinitura_meshy.py [id ...]

Salva i GLB in art/map-assets/generated/rifinitura/<id>.glb (fuori da Assets:
la QA e la conversione in prefab avvengono prima dell'import in Unity).
"""
import json, os, socket, sys, time, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "art", "map-assets", "generated", "rifinitura")
BLENDER = ("127.0.0.1", 9876)

STILE = (
    "Stylized game prop, Valorant-like art direction: clean readable silhouette, "
    "matte PBR with subtle hand-painted wear, no photorealism, no text, no logos. "
    "Single object, no ground plane, no background, no shadow, no baked lighting. "
)

PROPS = {
    "fontana_piazza": (
        "Old Mediterranean stone fountain for a harbor piazza: octagonal basin, central carved "
        "pillar with a spout and a small upper bowl, weathered limestone, algae and water stains "
        "at the base. Real-world scale 3.2 x 3.2 x 2.6 m. " + STILE),
    "banco_pesce": (
        "Wooden fish market stall with a striped ochre and cream canvas awning, ice trough, "
        "several wooden fish crates, a hanging brass scale and a small chalkboard. Real-world "
        "scale 3.0 x 1.4 x 2.4 m. " + STILE),
    "barca_pesca": (
        "Small wooden Mediterranean fishing dinghy, faded blue and white paint, two oars, "
        "coiled rope, a couple of cork floats and a folded net inside. Real-world scale "
        "4.2 x 1.6 x 0.9 m. " + STILE),
    "bitta_ormeggio": (
        "Cast-iron harbor mooring bollard, fluted octagonal base, mushroom-shaped top, thick "
        "rust and salt crust, worn edges. Real-world scale 0.35 x 1.0 x 0.35 m. " + STILE),
    "lampione_ghisa": (
        "Cast-iron street lamp, ornate fluted base, slender shaft, glass lantern with a small "
        "crown, weathered black paint and rust, warm glass. Real-world scale 0.5 x 3.6 x 0.5 m. " + STILE),
    "tavolino_caffe": (
        "Small wrought-iron cafe table with a white marble top and two matching bistro chairs, "
        "painted dark green, worn edges, folded newspaper on the table. Real-world scale "
        "1.3 x 1.3 x 0.8 m. " + STILE),
    "boa_pesca": (
        "Weathered fishing buoy, orange and white bands, rope ring and a short frayed rope, "
        "salt stains and chips. Real-world scale 0.4 x 0.9 x 0.4 m. " + STILE),
    "barile_legno": (
        "Oak wine barrel with three iron hoops, weathered staves, small chalk mark, damp base. "
        "Real-world scale 0.7 x 0.9 x 0.7 m. " + STILE),
    "cannone_forte": (
        "Old coastal cannon, dark iron barrel on a wooden carriage with iron wheels and a rope "
        "breeching, rust and salt, no cannonballs. Real-world scale 2.4 x 1.2 x 1.4 m. " + STILE),
    "rete_pesca": (
        "Pile of fishing nets on the ground, tangled green and orange netting, cork floats and "
        "two faded buoys caught inside, coiled salt-stained rope on top. Real-world scale "
        "2.0 x 0.6 x 1.4 m. " + STILE),
    "tombino_ghisa": (
        "Ornate cast-iron manhole cover, circular, radial ribs and a central boss, worn edges, "
        "rust in the recesses. Real-world scale 0.7 x 0.06 x 0.7 m. " + STILE),
    "portale_pietra": (
        "Carved limestone portal for a Mediterranean palazzo: arched door frame with a keystone, "
        "moulded jambs and a small cornice, weathered edges. Real-world scale 2.4 x 3.2 x 0.45 m. " + STILE),
}

def blender(cmd, timeout=600, **params):
    s = socket.create_connection(BLENDER, timeout=timeout)
    try:
        s.sendall(json.dumps({"type": cmd, "params": params}).encode())
        buf = b""
        while True:
            chunk = s.recv(65536)
            if not chunk: break
            buf += chunk
            try: return json.loads(buf)
            except json.JSONDecodeError: pass
        raise RuntimeError(f"risposta non JSON: {buf[:300]!r}")
    finally:
        s.close()

def wait(task_id, kind, label, every=20, limit=2400):
    t0 = time.time(); last = None
    while time.time() - t0 < limit:
        r = blender("poll_meshy_job_status", task_id=task_id, kind=kind, timeout=120)
        res = r.get("result") or r
        st, pr = res.get("status"), res.get("progress")
        line = f"{label}: {st} {pr}% ({int(time.time()-t0)} s)"
        if line != last: print(line, flush=True); last = line
        if st == "SUCCEEDED": return res
        if st in ("FAILED", "CANCELED") or res.get("error"): raise RuntimeError(str(res)[:500])
        time.sleep(every)
    raise TimeoutError(label)

def generate(ident):
    prompt = PROPS[ident]
    os.makedirs(OUT, exist_ok=True)
    print(f"== {ident} ({len(prompt)} char)", flush=True)
    created = blender("create_meshy_job", text_prompt=prompt, timeout=180)
    if created.get("status") != "success": raise RuntimeError(str(created)[:500])
    res = created["result"]; preview = res["task_id"]
    wait(preview, res["kind"], f"{ident} preview")
    refined = blender("refine_meshy_job", preview_task_id=preview, timeout=180)
    if refined.get("status") != "success": raise RuntimeError(str(refined)[:500])
    res = refined["result"]
    final = wait(res["task_id"], res["kind"], f"{ident} texture")
    url = final["model_url"]
    path = os.path.join(OUT, ident + ".glb")
    urllib.request.urlretrieve(url, path)
    meta = {k: final.get(k) for k in ("status", "progress", "model_url")}
    meta.update(id=ident, prompt=prompt, preview_task_id=preview, texture_task_id=res["task_id"],
                bytes=os.path.getsize(path))
    with open(os.path.join(OUT, ident + ".json"), "w") as f: json.dump(meta, f, indent=1)
    print(f"== {ident} pronto: {os.path.getsize(path)/1e6:.1f} MB", flush=True)

if __name__ == "__main__":
    ids = sys.argv[1:] or list(PROPS.keys())
    ok, bad = [], []
    for ident in ids:
        try:
            generate(ident); ok.append(ident)
        except Exception as e:
            print(f"!! {ident}: {e}", flush=True); bad.append(ident)
    print("FATTI", ok, flush=True)
    print("FALLITI", bad, flush=True)
