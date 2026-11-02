#!/usr/bin/env python3
"""Calanera raccontata (docs/30): area per area, edificio per edificio, dal blockout.
Uso: python3 tools/calanera2/racconto.py   → riscrive docs/30-calanera-racconto.md
Il testo delle aree sta in tools/calanera2/racconto_testo.md ({{T:Area}} = tabella degli edifici).
"""
import sys, collections, hashlib, math
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools' / 'mapgen'))
import calanera as c
R=c.rect
# aree aperte con nome (metà nord) — stesse coordinate di north_half/center/grotta/galleria
NORTH = [
 ("Porto Vecchio", R(-20,20,-99,-82)), ("Porto Vecchio (uscita est)", R(20,24,-95,-87)),
 ("Salita del Porto", R(-4,4,-82,-70)), ("Largo", R(-13,13,-70,-52)), ("Arco", R(-3,3,-52,-44)),
 ("Via del Mercato", R(-3,3,-44,-34)), ("Via del Mercato", R(-3,9,-34,-28)), ("Via del Mercato", R(3,9,-28,-18)),
 ("Passaggio Ovest del Largo", R(-34,-13,-64,-59)), ("Passaggio Est del Largo", R(13,30,-64,-59)),
 ("Banchina", R(24,46,-99,-84)), ("Varco della Dogana", R(36,42,-84,-74)), ("Cortile della Dogana", R(28,54,-74,-52)),
 ("Discesa al Molo", R(46,52,-52,-40)), ("Molo", R(46,82,-44,-30)), ("Terrazza del Molo", R(58,72,-56,-44)),
 ("Piazzetta del Forno", R(-58,-30,-74,-56)), ("Passaggio del Piazzale", R(-62,-54,-56,-44)), ("Piazzale della Galleria", R(-82,-56,-44,-30)),
]
for r in c.path([(-22,-91),(-33,-91),(-33,-74)],4): NORTH.append(("Via Stretta", r))
CENTER = [("Piazza Vecchia", R(-20,20,-18,18)), ("Mercato coperto Est", R(20,36,-7,7)), ("Cortile Est", R(36,52,-9,9)),
          ("Mercato coperto Ovest", c.rot_rect(R(20,36,-7,7))), ("Cortile Ovest", c.rot_rect(R(36,52,-9,9)))]
SOUTH_NAMES = {"Porto Vecchio":"Forte San Verano","Porto Vecchio (uscita est)":"Forte San Verano (uscita ovest)",
 "Salita del Porto":"Salita del Forte","Largo":"Largo del Forte","Arco":"Arco Sud","Via del Mercato":"Via del Mercato Sud",
 "Passaggio Ovest del Largo":"Passaggio Est del Largo Sud","Passaggio Est del Largo":"Passaggio Ovest del Largo Sud",
 "Banchina":"Spalti","Varco della Dogana":"Porta delle Mura","Cortile della Dogana":"Mura","Discesa al Molo":"Discesa alla Marina",
 "Molo":"Marina","Terrazza del Molo":"Bastione della Marina","Piazzetta del Forno":"Piazzetta del Lungomare",
 "Passaggio del Piazzale":"Passaggio del Piazzale Sud","Piazzale della Galleria":"Piazzale della Grotta","Via Stretta":"Lungomare"}
AREAS = list(NORTH) + [(SOUTH_NAMES[n], c.rot_rect(r)) for n,r in NORTH] + CENTER
data, boxes = c.build()
K = c.KINDS
lots = [b for b in boxes if K[b.kind] in ("plaster","plaster_ochre","stone","concrete") and b.y0==0 and (b.y1-b.y0) in (6.2,9.0,11.8)]
def facing(b):
    out=[]
    for side in ("N","S","E","W"):
        for name,(x0,x1,z0,z1) in AREAS:
            if side=="N" and abs(z1-b.z0)<1e-6: ov=min(x1,b.x1)-max(x0,b.x0)
            elif side=="S" and abs(z0-b.z1)<1e-6: ov=min(x1,b.x1)-max(x0,b.x0)
            elif side=="W" and abs(x1-b.x0)<1e-6: ov=min(z1,b.z1)-max(z0,b.z0)
            elif side=="E" and abs(x0-b.x1)<1e-6: ov=min(z1,b.z1)-max(z0,b.z0)
            else: continue
            if ov>0.5: out.append((side,name,round(ov,1)))
    return out
area_lots=collections.defaultdict(list); internal=[]
for b in lots:
    f=facing(b)
    if not f: internal.append(b); continue
    tot=collections.Counter()
    for s,n,ov in f: tot[n]+=ov
    main=tot.most_common(1)[0][0]
    area_lots[main].append((b,f))

def floors(h): return {6.2:2, 9.0:3, 11.8:4}[round(h,1)]
"""Scrive docs/30-calanera-racconto.md: la mappa raccontata area per area, edificio per edificio,
partendo dal blockout (tools/mapgen/calanera.py). I dati (misure, piani, facciate) vengono dal
blockout; il carattere (usi, colori, dettagli) dalle regole di zona scritte qui sotto."""

def H(*parts):
    return int(hashlib.md5("|".join(map(str, parts)).encode()).hexdigest()[:8], 16)

def pick(lst, *key):
    return lst[H(*key) % len(lst)]

def fnum(v):
    s = f"{v:.1f}".rstrip('0').rstrip('.')
    return s.replace('.', ',')

SIDE_IT = {"N": "nord", "S": "sud", "E": "est", "W": "ovest"}

# ------------------------------------------------------------------ temi di zona
# colori sempre desaturati (D4): il saturo è delle squadre (ciano/arancio), dell'obiettivo (giallo), del pericolo (rosso)
NORD_INTONACI = ["crema", "bianco calce", "sabbia chiara", "grigio perla", "rosa antico spento", "avorio", "camoscio", "grigio tortora"]
NORD_PERSIANE = ["verde bottiglia", "grigio ardesia", "salvia", "legno naturale schiarito", "verde bosco", "grigio piombo"]
SUD_INTONACI = ["ocra", "terra di Siena chiara", "rosa salmone spento", "sabbia calda", "albicocca spento", "ocra rossa chiara", "camoscio caldo"]
SUD_PERSIANE = ["verde oliva", "marrone castagna", "rosso ossido scuro", "ardesia", "verde salvia scuro", "noce"]

THEMES = {
    # nome area: (codice, usi del piano terra, dettagli, nota di carattere)
    "Porto Vecchio": ("PV", ["deposito reti e cassette", "Capitaneria di Porto (ufficio, orologio sulla facciata)", "fabbrica del ghiaccio",
                             "officina nautica con saracinesca", "bar del porto (tavolini sotto la tenda)", "magazzino del pesce", "ufficio spedizioni"],
                      ["insegne dipinte a mano scolorite", "cassette di polistirolo impilate sui davanzali", "reti appese ad asciugare a 3 m",
                       "lampione a braccio sulla facciata", "numeri civici su piastrelle", "cavi elettrici fra le finestre"], "porto"),
    "Porto Vecchio (uscita est)": ("PVE", ["torretta della gru (cabina di manovra)", "magazzino doganale"], ["scaletta in ferro sul fianco", "segnaletica portuale"], "porto"),
    "Salita del Porto": ("SP", ["bottega"], ["lampione"], "via"),
    "Largo": ("LA", ["bar con tavolini e ombrelloni chiusi", "tabaccheria", "barbiere con l'insegna a spirale", "panetteria", "farmacia con la croce spenta", "latteria"],
              ["vasi di gerani sui davanzali", "balconcino in ferro battuto", "orologio pubblico", "persiane socchiuse", "targa della via in marmo"], "piazzetta"),
    "Via del Mercato": ("VM", ["frutta e verdura", "macelleria", "merceria", "drogheria", "cantina con le botti in vetrina", "pasticceria"],
                        ["tende a strisce sopra le vetrine (sopra i 2,3 m)", "cassette di frutta sui davanzali", "cartelli dei prezzi in gesso", "panni stesi fra le finestre"], "via"),
    "Passaggio Ovest del Largo": ("PL", ["androne", "deposito"], ["lampada a muro sotto il sottoportico", "affreschi sbiaditi nell'intradosso"], "sottoportico"),
    "Passaggio Est del Largo": ("PL", ["androne", "deposito"], ["lampada a muro sotto il sottoportico", "graffiti vecchi"], "sottoportico"),
    "Banchina": ("BA", ["magazzino portuale", "rimessa", "ufficio della compagnia di navigazione", "deposito carburante (fusti)"],
                 ["bitte di ghisa lungo il bordo", "anelli d'ormeggio nel muro", "scritte con stencil sui portoni", "lampioni portuali"], "porto"),
    "Varco della Dogana": ("VD", ["corpo di guardia della Dogana", "ufficio pratiche"], ["sbarra a bilico alzata", "garitta"], "dogana"),
    "Cortile della Dogana": ("CD", ["Dogana (portone con lo stemma)", "magazzino sigillato", "pesa pubblica", "uffici con le grate"],
                            ["grate alle finestre del piano terra", "stemma in pietra sopra il portone", "orologio della Dogana", "bandiera ammainata"], "dogana"),
    "Discesa al Molo": ("DM", ["torre delle scale"], ["corrimano a muro"], "via"),
    "Molo": ("MO", ["casa del pescatore", "rimessaggio barche", "osteria del Molo", "deposito attrezzi", "cantiere navale"],
             ["reti e boe appese", "remi appoggiati sopra i 2,5 m", "lanterne", "scafo rovesciato sulla tettoia"], "molo"),
    "Terrazza del Molo": ("TM", ["casa a schiera", "magazzino"], ["vasi sul parapetto", "pergola in ferro senza tralci"], "molo"),
    "Via Stretta": ("VS", ["ingresso di casa", "ciabattino", "portone di androne", "cantina", "piccola cappella votiva"],
                    ["panni stesi a 4,6 m", "edicola votiva con lumino", "scritta di propaganda sbiadita", "vasi di basilico", "contatori a muro"], "vicolo"),
    "Piazzetta del Forno": ("PF", ["forno (la stanza attraversabile)", "falegnameria", "latteria", "deposito della farina", "lavatoio coperto"],
                            ["sacchi di farina dipinti sui muri", "camino fumante del forno", "fascine di legna sopra i 2,5 m", "insegna PANE in ferro"], "piazzetta"),
    "Passaggio del Piazzale": ("PP", ["officina", "deposito"], ["pneumatici appesi", "cartelli stradali"], "strada"),
    "Piazzale della Galleria": ("PG", ["officina meccanica", "distributore di benzina dismesso", "casello dell'ANAS", "deposito dei cantonieri", "trattoria per camionisti"],
                                ["cartello GALLERIA VECCHIA con la sagoma del tunnel", "segnaletica stradale arrugginita", "pompa di benzina anni '60 (dentro il lotto)", "fari stradali"], "strada"),
    "Piazza Vecchia": ("PZ", ["Caffè Centrale sotto il porticato", "Municipio con il balcone d'onore", "palazzo nobiliare con portone", "libreria", "gioielleria con le saracinesche abbassate", "Circolo dei Marinai"],
                       ["cornicioni sagomati", "lesene di pietra agli spigoli", "balconi d'onore in ferro", "bandiere del Comune", "lanterne in ghisa a muro"], "piazza"),
    "Mercato coperto Est": ("ME", ["pescheria", "banchi coperti"], ["teli tesi", "lavagne dei prezzi"], "mercato"),
    "Mercato coperto Ovest": ("MW", ["pescheria", "banchi coperti"], ["teli tesi", "lavagne dei prezzi"], "mercato"),
    "Cortile Est": ("CE", ["casa a corte", "scuderia trasformata in garage", "cappella"], ["pergolato", "panche di pietra"], "cortile"),
    "Cortile Ovest": ("CW", ["casa a corte", "lavanderia", "cappella"], ["pergolato", "panche di pietra"], "cortile"),
    # ---- metà sud: stessa architettura, altra pelle (Forte, pietra, ocra, legno)
    "Forte San Verano": ("FS", ["caserma del presidio", "armeria (portone ferrato)", "corpo di guardia", "magazzino delle polveri", "scuderia", "cappella del Forte", "cucine del presidio"],
                         ["stendardi rosso scuro sulle facciate", "bracieri in ferro sopra i 2,5 m", "feritoie", "stemma della torre", "rastrelliere di legno"], "forte"),
    "Forte San Verano (uscita ovest)": ("FSO", ["torretta di guardia", "postierla"], ["feritoie", "stendardo"], "forte"),
    "Salita del Forte": ("SF", ["bottega"], ["lampione"], "via"),
    "Largo del Forte": ("LF", ["osteria", "bottega del sellaio", "fabbro", "erboristeria", "forno del presidio"],
                        ["pergola di glicine (rami spogli)", "fontanella in ghisa", "insegne in ferro battuto", "orci di terracotta"], "piazzetta"),
    "Arco Sud": ("AS", ["porta del borgo"], ["stemma"], "via"),
    "Via del Mercato Sud": ("VMS", ["bottega dell'olio", "salumeria", "cestaio", "ferramenta", "vinaio"],
                            ["tende color sabbia sopra le vetrine", "cesti appesi sopra i 2,3 m", "corde di peperoncini", "persiane a ventola"], "via"),
    "Passaggio Est del Largo Sud": ("PLS", ["androne", "deposito"], ["volta a botte con lanterna"], "sottoportico"),
    "Passaggio Ovest del Largo Sud": ("PLS", ["androne", "deposito"], ["volta a botte con lanterna"], "sottoportico"),
    "Spalti": ("SPA", ["magazzino del presidio", "polveriera", "alloggi"], ["merli", "cannone su affusto (dentro il lotto)", "palle di cannone impilate"], "forte"),
    "Porta delle Mura": ("PM", ["corpo di guardia", "torre della porta"], ["saracinesca alzata", "stemma"], "forte"),
    "Mura": ("MU", ["caserma", "alloggi degli ufficiali", "cisterna", "cappella di Santa Barbara"],
             ["camminamento con merli", "stendardi", "bracieri", "lapide commemorativa"], "forte"),
    "Discesa alla Marina": ("DMS", ["torre delle scale"], ["corrimano"], "via"),
    "Marina": ("MA", ["cantiere delle barche di legno", "casa del maestro d'ascia", "rimessa", "osteria della Marina"],
               ["scafi in costruzione sul fianco", "trucioli e tavole sopra la tettoia", "lanterne"], "molo"),
    "Bastione della Marina": ("BM", ["casa del guardiano", "magazzino"], ["vasi di agavi", "meridiana"], "molo"),
    "Lungomare": ("LU", ["villino con giardino pensile", "pensione affacciata", "stabilimento chiuso per la stagione", "casa del custode"],
                  ["balaustre in pietra", "bouganville secca sui muri", "persiane a ventola", "fanali"], "vicolo"),
    "Piazzetta del Lungomare": ("PLU", ["frantoio (la stanza attraversabile)", "bottaio", "deposito delle olive", "lavatoio", "cantina sociale"],
                                ["giare di terracotta", "macina in pietra dipinta sul muro", "botti sopra i 2,5 m", "insegna OLIO"], "piazzetta"),
    "Passaggio del Piazzale Sud": ("PPS", ["rimessa dei carri", "stalla"], ["finimenti appesi"], "strada"),
    "Piazzale della Grotta": ("PGR", ["rimessa delle barche", "deposito del sale", "casa dei doganieri", "taverna dei contrabbandieri (chiusa)", "lazzaretto vecchio"],
                              ["cartello GROTTA DEL LEVANTE in ceramica", "fanale di segnalazione", "nasse impilate sopra i 2,5 m"], "strada"),
}

def tinte(area, kind):
    sud = kind in ("plaster_ochre", "stone")
    return (SUD_INTONACI if sud else NORD_INTONACI), (SUD_PERSIANE if sud else NORD_PERSIANE)

def materiale(kind, area, key):
    if kind == "concrete":
        return pick(["palazzina del dopoguerra in intonaco grigio cemento con fasce marcapiano chiare",
                     "edificio portuale in cemento a vista dipinto, spigoli smussati, finestre a nastro",
                     "palazzina razionalista in intonaco grigio perla con zoccolo in pietra scura"], key, "c")
    if kind == "stone":
        return pick(["pietra calcarea a conci regolari, cornicione in pietra",
                     "muratura di pietra mista intonacata solo al primo piano",
                     "pietra calcarea con cantonali squadrati e archi a tutto sesto al piano terra"], key, "s")
    intonaci, _ = tinte(area, kind)
    return f"intonaco {pick(intonaci, key, 'i')}"

def finestre(L):
    return max(1, int((L - 1.0) // 2.8))

USO_AREA = {}
# correzioni a mano dove il racconto dell'area lo chiede
USO_FISSO = {
    "PV-10": "Capitaneria di Porto (orologio fermo alle 7:10, salvagente appeso a muro, bandiera)",
    "PV-03": "bar del porto (tavolini sotto la tenda)",
    "FS-03": "corpo di guardia (portone con la garitta)",
}
NOTE = {
    "PV-05": "sopra il cornicione la prima metà dell'**insegna antica FALEZIA**, lettere di ferro arrugginite alte 1,2 m",
    "PV-06": "sopra il cornicione la seconda metà dell'**insegna FALEZIA**",
    "PVE-01": "alla sua base, lato mare, il pilone della **gru** (il traliccio sale oltre il bordo)",
    "FS-04": "al centro della cortina la **porta del soccorso** murata, con lo stemma della torre",
    "FS-05": "sopra i merli lo **stendardo** più grande del Forte",
}
def uso_per(area, usi, key):
    """Gli usi del piano terra non si ripetono nella stessa area finché la lista non è finita."""
    st = USO_AREA.setdefault(area, {"ord": sorted(usi, key=lambda u: H(area, u)), "i": 0})
    u = st["ord"][st["i"] % len(st["ord"])]
    st["i"] += 1
    return u

def edificio(area, i, b, f, theme):
    code, usi, dettagli, char = theme
    key = (round(b.x0, 2), round(b.z0, 2))
    w, d = b.x1 - b.x0, b.z1 - b.z0
    pi = floors(b.y1)
    fronti = [(s, n, ov) for s, n, ov in f]
    main = max(fronti, key=lambda t: t[2])
    L = main[2]
    _, persiane = tinte(area, K[b.kind])
    thin = min(w, d) <= 2.0
    parts = []
    if thin and (abs(b.z0 + 100) < 1e-6 or abs(b.z1 - 100) < 1e-6):
        if area.startswith("Forte") or area in ("Spalti",):
            parts.append(f"tratto di **cortina muraria** del Forte alto {fnum(b.y1)} m: merli, feritoie e camminamento in cima; il corpo vero sta oltre il bordo (solo vista)")
        else:
            parts.append(f"**facciata di magazzino sul mare** alta {fnum(b.y1)} m (il lotto è profondo 1 m: il corpo prosegue oltre il bordo, sopra la banchina, solo vista)")
            parts.append(f"portoni scorrevoli in lamiera {pick(['grigio piombo', 'verde bottiglia scuro', 'ruggine spenta'], key, 'p')}, finestre alte con inferriate")
    elif thin and min(w, d) < 1.5 and L < 4:
        parts.append(f"corpo stretto ({fnum(min(w, d))} m): quinta muraria intonacata con una nicchia cieca e una lapide")
    else:
        if thin:
            parts.append(f"lotto profondo {fnum(min(w, d))} m: **facciata addossata** (il corpo sta nell'isolato o nella roccia dietro)")
        parts.append(materiale(K[b.kind], area, key))
        code_here = f"{code}-{i:02d}"
        uso = USO_FISSO.get(code_here) or uso_per(area, usi, key)
        parts.append(f"piano terra: {uso}")
        nf = finestre(L)
        if pi >= 2:
            parts.append(f"{nf} finestr{'a' if nf == 1 else 'e'} per piano sul fronte {SIDE_IT[main[0]]} con persiane {pick(persiane, key, 'pe')}")
        dett = pick(dettagli, key, "d")
        dett2 = pick(dettagli, key, "d2")
        parts.append(dett if dett == dett2 else f"{dett}; {dett2}")
        if pi >= 3 and H(key, 'b') % 3 == 0:
            parts.append("balcone in ferro al primo piano (sopra i 3,6 m)")
        if pi == 4 and H(key, 'a') % 2 == 0:
            parts.append("altana con pergola sul tetto (skyline)")
    if f"{code}-{i:02d}" in NOTE:
        parts.append(NOTE[f"{code}-{i:02d}"])
    fr = "; ".join(f"{SIDE_IT[s]} su {n} {fnum(ov)} m" for s, n, ov in fronti)
    return (f"| **{code}-{i:02d}** | x {fnum(b.x0)}…{fnum(b.x1)} · z {fnum(b.z0)}…{fnum(b.z1)} | {fnum(w)} × {fnum(d)} m | "
            f"{pi} ({fnum(b.y1)} m) | {fr} | {'; '.join(parts)} |")

def tabella(area):
    L = area_lots.get(area, [])
    if not L:
        return "_Nessun edificio ha qui il suo fronte principale: le facciate che si vedono sono elencate nelle aree vicine._\n"
    theme = THEMES[area]
    rows = ["| Edificio | Lotto | Pianta | Piani | Fronti | Com'è |", "|---|---|---|---|---|---|"]
    for i, (b, f) in enumerate(sorted(L, key=lambda t: (t[0].x0, t[0].z0)), 1):
        rows.append(edificio(area, i, b, f, theme))
    return "\n".join(rows) + "\n"

if __name__ == "__main__":
    import re
    t = open(ROOT / 'tools' / 'calanera2' / 'racconto_testo.md').read()
    cnt = collections.Counter(floors(b.y1) for b in internal)
    t = t.replace("{{N_INTERNI}}", str(len(internal)))
    t = t.replace("{{INTERNI_DETTAGLIO}}", f"{cnt[2]} da 2 piani, {cnt[3]} da 3, {cnt[4]} da 4")
    missing = []
    def rep(m):
        a = m.group(1)
        if a not in THEMES: missing.append(a); return f"_(area {a} senza tema)_"
        return tabella(a)
    t = re.sub(r"\{\{T:([^}]+)\}\}", rep, t)
    open(ROOT / 'docs' / '30-calanera-racconto.md', 'w').write(t)
    used = set(re.findall(r"\{\{T:([^}]+)\}\}", open(ROOT / 'tools' / 'calanera2' / 'racconto_testo.md').read()))
    print("mancanti:", missing, "| aree con lotti non citate:", [a for a in area_lots if a not in used])
    print(len(t), "caratteri")
