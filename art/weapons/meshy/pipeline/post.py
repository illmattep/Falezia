"""
FALEZIA · ultimo passo dopo build.py: aggiunge socket_eye (richiesto dal client Unity) e scrive <id>.json.
socket_eye sta sulla linea di mira, 16 cm dietro la tacca: come nei modelli di arsenal2,
il WeaponView di Unity porta QUEL punto sull'asse della camera in mira.
Uso: python3 -I post.py <id> [<id> ...]
"""
import json, os, struct, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from glbinfo import load  # noqa: E402

EYE_BEHIND = 0.16


def write_glb(path, js, b):
    j = json.dumps(js, separators=(",", ":")).encode()
    while len(j) % 4: j += b" "
    while len(b) % 4: b += b"\0"
    with open(path, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, 28 + len(j) + len(b)))
        f.write(struct.pack("<II", len(j), 0x4E4F534A)); f.write(j)
        f.write(struct.pack("<II", len(b), 0x004E4942)); f.write(b)


def post(wid):
    path = os.path.join(HERE, "out", wid, wid + ".glb")
    js, b = load(path)
    names = {n.get("name"): i for i, n in enumerate(js["nodes"])}
    aim = js["nodes"][names["socket_aim"]]["translation"]
    eye = [0.0, aim[1], aim[2] + EYE_BEHIND]
    if "socket_eye" in names:
        js["nodes"][names["socket_eye"]]["translation"] = eye
    else:
        js["nodes"].append({"name": "socket_eye", "translation": eye})
        root = js["scenes"][0]["nodes"][0]
        js["nodes"][root]["children"].append(len(js["nodes"]) - 1)
    # ogni clip deve durare quanto dice la scheda (Unity riscala "fire" sulla sua lunghezza)
    import numpy as np
    from glbinfo import acc
    lens = json.load(open(os.path.join(HERE, "tmp", wid + "_report.json")))["anim_lengths"]
    b = bytearray(b)
    for a in js.get("animations", []):
        want = lens[a["name"]]
        for smp in a["samplers"]:
            t = acc(js, bytes(b), smp["input"]).ravel().astype(np.float32)
            if t[-1] < want - 1e-4:
                v = acc(js, bytes(b), smp["output"]).astype(np.float32)
                t2 = np.append(t, np.float32(want)); v2 = np.vstack([v, v[-1:]])
                for arr, key in ((t2, "input"), (v2, "output")):
                    while len(b) % 4: b += b"\0"
                    js["bufferViews"].append({"buffer": 0, "byteOffset": len(b), "byteLength": arr.nbytes})
                    b += arr.tobytes()
                    old = js["accessors"][smp[key]]
                    newa = {"bufferView": len(js["bufferViews"]) - 1, "componentType": 5126, "count": int(arr.shape[0]), "type": old["type"]}
                    if key == "input": newa["min"] = [float(arr.min())]; newa["max"] = [float(arr.max())]
                    js["accessors"].append(newa); smp[key] = len(js["accessors"]) - 1
    js["buffers"][0]["byteLength"] = len(b)
    write_glb(path, js, b)

    rep = json.load(open(os.path.join(HERE, "tmp", wid + "_report.json")))
    sockets = dict(rep["sockets"]); sockets["socket_eye"] = [round(v, 4) for v in eye]
    data = {
        "id": wid,
        "class": rep["class"],
        "source": "meshy",
        "tris": rep["tris"]["viewmodel"],
        "tris_tp": rep["tris"]["tp"],
        "size_mm": rep["size_mm"],
        "size_mm_doc12": rep["doc_mm"],
        "material": wid + "_mat",
        "parts": list(rep["parts_tris"].keys()),
        "sockets": sockets,
        "animations": {k: {"length_s": v} for k, v in rep["anim_lengths"].items()},
    }
    json.dump(data, open(os.path.join(HERE, "out", wid, wid + ".json"), "w"), indent=2, ensure_ascii=False)
    print(wid, "ok", eye)


if __name__ == "__main__":
    for w in sys.argv[1:]:
        post(w)
