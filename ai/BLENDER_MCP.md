# BLENDER_MCP · come lavorare in Blender da Claude Desktop

> Scritto il 28 ottobre 2026 da Claude Code, dopo aver sistemato il collegamento.
> Leggilo tutto prima della prima chiamata a Blender: le trappole sotto sono costate ore.

---

## 1. Come è collegato (lo stato vero, verificato)

```
Claude Desktop ──stdio──▶ server MCP "blender" ──TCP 127.0.0.1:9876──▶ addon in Blender ──▶ bpy
```

| Pezzo | Cosa è | Dove |
|---|---|---|
| **Addon** | "MCP for Blender" **1.8** (Siddharth Ahuja), protocollo **15**. Parte da solo con Blender (`blendermcp_auto_start_server`) | `~/.config/blender/5.2/scripts/addons/blender_mcp.py` · pannello **N › MCP for Blender** |
| **Server MCP** | Pacchetto AUR `blender-mcp-git` ("MCP for Blender" 1.30), avviato da un launcher con un venv suo | `~/.local/share/mcp-for-blender/run` |
| **Config** | voce `mcpServers.blender` → il launcher | `~/.config/Claude/claude_desktop_config.json` |
| **Blender** | 5.2.2 LTS, da `/usr/bin/blender` | |

### Cosa era rotto (non rifarlo)

- **L'estensione "Blender" della directory di Claude** (`ant.dir.gh.blender.blender-mcp`, di Blender
  Lab) **non è compatibile** con questo addon: manda `{"type":"execute",…}` e l'addon risponde
  `Unknown command type`. Nei log sembrava "connessa" solo perché il server partiva. **È disattivata:
  non riattivarla.** Vuole un altro addon (da lab.blender.org) e, sulla stessa porta 9876, fa a pugni
  con questo.
- **`/usr/bin/mcp-for-blender` lanciato direttamente si pianta**: in `~/.local` c'è `mcp` **2.3.0**
  (installato con pip), dove `FastMCP` non esiste più. Il launcher usa un venv con `mcp<2` e
  `python -s`, che ignora `~/.local`. **Non aggiornare `mcp` nel venv a 2.x.**

## 2. Gli strumenti

| Strumento | A cosa serve |
|---|---|
| `get_addon_status` | **Chiamalo una volta all'inizio**: versione, compatibilità addon/server, librerie e generatori attivi |
| `get_scene_info` | Testo: oggetti, posizioni, misure, salute delle mesh. `query="casa"` filtra per nome, `root="X"` mostra una gerarchia |
| `execute_blender_code` | Python dentro Blender (`bpy`, `bmesh`, `mathutils`). **Ti torna solo ciò che stampi con `print`** |
| `look` | Un'immagine: `mode` = viewport · camera · angles (più lati, inquadra da solo) · frames; `shading` = solid · material · rendered · wireframe · xray |
| `generate_3d` | Un modello da testo o immagine (Meshy è configurato con la chiave di Matteo). Aspetta al massimo 45 s, poi ti dà un handle da richiamare |
| `search_assets` / `import_asset` | Poly Haven, Sketchfab, Poly Pizza |

In ogni chiamata, `user_prompt` vuole **le parole di Matteo testuali**, non un tuo riassunto.

### Integrazioni accese e spente (pannello N › MCP for Blender)

- **Meshy: acceso**, con la chiave, modello 7.1. Ogni generazione **costa crediti**.
- **Poly Haven, Sketchfab, Hyper3D, Hunyuan, Tripo: spenti.** Il doc 29 usa Poly Haven per i
  materiali PBR CC0: se ti serve, chiedi a Matteo di spuntarlo, oppure scarica le texture fuori da
  Blender (ambientCG / Poly Haven via HTTP) e caricale con `bpy`.

## 3. Regole di lavoro che funzionano

1. **Passi piccoli e numeri prima delle immagini.** Stampa conteggi (poligoni per materiale, quote,
   bounding box) e verifica su quelli; il render serve per l'occhio, non per trovare i bug (doc 24).
2. **Le chiamate lunghe bloccano Blender.** L'addon esegue sul thread principale: un export da più
   di ~90 edifici in una chiamata lo pianta (doc 24). Spezza il lavoro e seleziona i lotti **per
   posizione**, mai per indice.
3. **Script nel repo, non incollati.** Scrivi gli script in `tools/calanera2/` (cartella prevista dal
   doc 29, non esiste ancora: creala) e passali a `execute_blender_code` letti dal file. Così si
   rigenera tutto e resta nella storia di git. `/tmp/fz/` è roba vecchia e si cancella al riavvio.
4. **Render per verificare**: EEVEE è veloce (`scene.render.engine = 'BLENDER_EEVEE'`),
   `bpy.ops.render.render(write_still=True)` su un PNG, poi aprilo; oppure `look` con
   `shading="rendered"`. Cycles solo per i render finali.
5. **Salva spesso** (`bpy.ops.wm.save_mainfile()`), e prima di aprire un altro `.blend` controlla
   `bpy.data.is_dirty`: aprire un file butta via la scena non salvata.
6. **Meshy** (doc 29): solo **modelli** (arredi, pezzi unici) e **immagini**, con prompt **realistici**,
   ottimizzati a **≤ 4k triangoli**. I modelli escono a ~200–450k triangoli: decima, controlla, poi
   esporta. Muri e suolo **no**, quelli sono materiali PBR + shader master.

### Ripiego se gli strumenti MCP non compaiono

L'addon risponde anche a un socket grezzo: un JSON `{"type": …, "params": {…}}`, risposta JSON
senza terminatore. Da shell:

```python
import socket, json
def blender(t, **p):
    s = socket.create_connection(("127.0.0.1", 9876), timeout=600)
    s.sendall(json.dumps({"type": t, "params": p}).encode())
    buf = b""
    while chunk := s.recv(65536):
        buf += chunk
        try: return json.loads(buf)
        except json.JSONDecodeError: pass
blender("execute_code", code="import bpy; print(len(bpy.data.objects))")
```

Comandi utili: `get_scene_info`, `execute_code {code}`, `get_viewport_screenshot {filepath, max_size}`,
`get_meshy_status`, `create_meshy_job`, `poll_meshy_job_status`, `import_generated_asset_meshy`.

## 4. Diagnosi rapida

| Sintomo | Causa probabile | Rimedio |
|---|---|---|
| Nessuno strumento `blender` in Claude | Server non partito | `~/.config/Claude/logs/mcp-server-blender.log` |
| `Connection refused` su 9876 | Blender chiuso o addon spento | Apri Blender; pannello N › MCP for Blender › Start |
| `Unknown command type` | Qualcosa usa il protocollo Lab | Estensione "Blender" di Claude: deve restare **spenta** |
| `No module named 'mcp.server.fastmcp'` | Lanciato senza launcher | Config deve puntare a `~/.local/share/mcp-for-blender/run` |
| Blender non risponde | Chiamata troppo grossa | Aspetta; poi spezza il lavoro (regola 2) |

## 5. Dove siamo

Nella sessione di Claude Code del 28/10 in Blender c'era solo un **test** (un forziere Meshy
texturizzato a mano), salvato in `~/forziere_valorant.blend`. **Non fa parte della mappa**: puoi
aprire i file della mappa senza preoccuparti.

Per la mappa valgono `docs/29-workflow-mappa.md` (pipeline attuale), `docs/24-stato-mappa.md`
(handoff), `docs/23-bibbia-artistica-calanera.md` e `ai/STATE.md`.
