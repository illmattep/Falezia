# FALEZIA

**Tattico 5v5 · Estrazione · Round senza respawn · Godot 4.x · Server dedicati**

> Mappa: **Calanera** — città costiera italiana immaginaria.
> Modalità principale: **Extract & Secure**, primo a **5 estrazioni**.
> Titolo di lavoro. Alternativi: *Presidio*, *Testa di Ponte*, *Scalo*.

---

## Scheda sintetica

| Voce | Valore |
|---|---|
| Giocatori | **5v5** (max 4v4 nel formato compresso) |
| Round | Senza respawn, **stato abbattuto + rianimazione** |
| Durata partita | **Primo a 5 estrazioni**, 5–9 round, **~40–55 min** (tetto 60 min) |
| Durata round | Tipica 5:00, **tetto 8:00** |
| Attacco/difesa | **Emergente**: chi prende il Nucleo diventa il difensore |
| Classi | **Nessuna classe, nessun eroe.** Tutti attingono allo stesso arsenale e alle stesse **5 specializzazioni**, scelte prima della partita e **pubbliche** |
| Loadout | **Max 5** per giocatore, uno scelto durante il caricamento, **bloccato per tutta la partita** |
| Consumabili | **3 slot**: 1 dal loadout (gratis ogni round), 2 acquistati in Buy Phase |
| Economia | **Cassa comune** con approvazione di squadra |
| Attrezzatura | **Si conserva se sopravvivi**, si ricompra se muori. Il **kit permanente non si perde mai** |
| Progressione | **Nessuna funzionale.** Tutto disponibile dal **livello 1**. Lo shop vende **solo estetica** |
| Mappa | 240 × 220 m, pianta **a croce con anello**, 5 zone |
| Motore | **Da decidere** — Godot 4 o **Flax**. Verifica in corso: vedi doc 08, *"Scelta del motore"* |
| Server | **Dedicati headless**, mai hostati dai giocatori |
| Target basso | **GTX 970 · 1080p · preset basso · 60 fps** |
| Target alto | **GTX 1070 Ti · 1080p · preset alto · 120 fps** |
| Target di sviluppo | Graybox: ≤ 200 draw call, ≤ 300 k triangoli, nessuna texture |

---

## I 5 pilastri

1. **L'impegno è irreversibile.** Nessun respawn. Ogni morte pesa sul round *e* sull'economia.
2. **L'attacco/difesa nasce dall'azione.** Nessun ruolo assegnato: chi arriva primo al centro difende, l'altro assalta alla Ready or Not.
3. **La squadra è un'entità economica.** La cassa è comune, le spese d'élite si approvano, e il rendiconto è pubblico.
4. **L'informazione è la risorsa più preziosa.** Prelievo pubblico, destinazione segreta. Sensori = ping, mai wallhack. **Anche il loadout è pubblico.**
5. **Ogni morte deve essere meritata.** Ogni trappola letale ha un tell. Ogni sconfitta ha una spiegazione.

---

## Le decisioni bloccate

| # | Decisione |
|---|---|
| 1 | Primo a **5 estrazioni**, tetto di sicurezza partita **60 min** |
| 2 | Round: tetto **8:00**, sospeso durante un'estrazione attiva |
| 3 | Buy Phase: **40 s massimo**, avvio anticipato a 5/5 pronti |
| 4 | Cassa: **15 s** a mani / **10 s** con Kit Decodifica / **15 s** col Robot (10 s con Ingegnere) |
| 5 | La cassa, una volta aperta, **resta aperta** |
| 6 | Prelievo: **3 s**, annunciato pubblicamente. Destinazione **non** rivelata |
| 7 | Il Nucleo **non torna mai alla cassa**: resta dove cade |
| 8 | Assedio: **120 s**. Rimozione del Nucleo: 5 s |
| 9 | **Overclock**: consumabile **durevole**, si conserva finché non lo usi, 120 s → **90 s** |
| 10 | **Lockdown**: uno per round, attivato **dopo il prelievo**, chiude fisicamente un accesso |
| 11 | Il **Robot** è un **deployabile**: si piazza, si muove, è distruttibile |
| 12 | Sensori = **ping temporaneo**, mai marcatore live |
| 13 | Trappole letali: **tell audiovisivo obbligatorio**, distruttibili e disattivabili |
| 14 | Si vende **solo** ciò che si compra nella Buy Phase corrente (anti-stampa soldi) |
| 15 | Nucleo **non** mostrato sul HUD nemico: solo l'annuncio del prelievo |
| 16 | Rimozione del Nucleo durante l'assedio → **chiude subito il round** |
| 17 | Netcode a **passo fisso, simulazione pura**, server autoritativo |
| 18 | **Max 5 loadout**, uno scelto durante il caricamento e **bloccato per la partita** |
| 19 | Il loadout è **pubblico**: entrambe le squadre lo vedono prima del round 1 |
| 20 | **5 specializzazioni condivise**, mai esclusive, **senza malus**: il costo è l'opportunità. Nessuna dà informazione o potenza, solo **tempo** |
| 21 | **3 slot consumabili**: 1 dal loadout (si rinnova ogni round), 2 acquistati in Buy Phase |
| 22 | **Zero progressione funzionale:** armi, ottiche, attachment e specializzazioni sono **tutte disponibili dal livello 1**. Lo shop vende **solo estetica** |
| 23 | **Nessuno sfondamento e nessun cuneo.** La mappa è **statica**: le porte si aprono e si chiudono, e **niente le blocca**. L'ingresso è flash + peek + pulizia |
| 24 | **15 armi + coltello**, tutte dal livello 1. **Piastre a punti effettivi** (+20 / +40), **acquistabili** — non gratuite. Il riferimento è **senza piastra** |
| 25 | **L'unico attachment è l'ottica** (gratuita). Niente canne, impugnature o laser: **un'arma ha un carattere, e l'ottica non lo cambia** |

Le voci 12, 15 e 16 erano proposte mie, non tue: sono marcate come *rivedibili* nei documenti.

---

## Indice dei documenti

| Doc | Contenuto |
|---|---|
| [00 · Panoramica](docs/00-panoramica.md) | **Il gioco e l'architettura in dieci minuti — leggi questo per primo** |
| [01 · Visione e pilastri](docs/01-visione-e-pilastri.md) | Identità, cosa il gioco è e cosa non è, riferimenti |
| [02 · La mappa](docs/02-mappa.md) | Croce + anello, 5 zone, tempi di percorrenza, regole di level design |
| [03 · Svolgimento del round](docs/03-svolgimento-del-round.md) | Le 9 fasi, i timer, le condizioni di fine round |
| [04 · Economia e acquisti](docs/04-economia-e-acquisti.md) | Cassa comune, approvazioni, prezzi, curva anti-snowball |
| [05 · Combattimento e movimento](docs/05-combattimento-movimento.md) | Danni, piastre, DBNO, movimento, rumore, porte, trappole |
| [06 · UX e interfaccia](docs/06-ux-interfaccia.md) | HUD, Buy Phase UI, rendiconto, accessibilità |
| [07 · Artstyle e performance](docs/07-artstyle-e-performance.md) | Direzione artistica, budget GPU, kit modulari |
| [08 · Netcode e server](docs/08-netcode-e-server.md) | Passo fisso, predizione, rewind, architettura server |
| [09 · Roadmap e rischi](docs/09-roadmap-e-rischi.md) | Milestone, rischi, cosa tagliare |
| [10 · Loadout e progressione](docs/10-loadout-e-progressione.md) | 5 loadout, 5 specializzazioni, shop e chest |
| [11 · Armi](docs/11-armi.md) | 15 armi con scheda completa, coltello, TTK, ottiche, analisi del roster |
| [12 · Brief artistico armi](docs/12-brief-artistico-armi.md) | Descrizione estetica per il render: silhouette, materiali, usura, marchi |
| [13 · Piano di sviluppo](docs/13-piano-di-sviluppo.md) | Sforzo, fasi, gate, costi, criteri di kill, tagli |
| [14 · Account e backend](docs/14-account-e-backend.md) | Identità, token, GDPR, ban, stack consigliato |

---

## Il codice

Il progetto Godot C# sta in **`game/`**, i test in **`tests/`**.

```
falezia/
├── docs/          design, arte, produzione
├── game/          progetto Godot 4.7 .NET
│   ├── sim/       SIMULAZIONE PURA — zero dipendenze dal motore
│   ├── net/       protocollo binario, puro anch'esso
│   ├── server/    server dedicato headless + console
│   └── client/    menu, gioco, stanza debug
├── tests/         test harness .NET puro — il CANCELLO architetturale
├── ai/            contesto condensato per agenti (leggi ai/README.md)
└── run.sh         test · build · server · game · debug
```

> **Il cancello:** `tests/` compila **solo** `sim/` e `net/`. Se qualcuno aggiunge `using Godot;`
> nella simulazione, il progetto di test non compila più. Il confine architetturale è un errore di
> build, non una linea guida.

```bash
./run.sh test      # 26 test, ~2 secondi, senza Godot
./run.sh build     # compila il progetto Godot
./run.sh server    # server headless con console
./run.sh game      # menu principale
./run.sh debug     # stanza debug
```

### Contesto per AI

La cartella **`ai/`** contiene il pacchetto di contesto per gli agenti — scritto per essere letto in
cinque minuti e per evitare che un modello rifaccia decisioni già prese ed **escluse**.

| File | Cosa contiene |
|---|---|
| **`ai/RULES.md`** | **Gli invarianti**, ognuno con il suo *test di violazione*. Se un cambio li viola, il cambio è sbagliato |
| `ai/CONTEXT.md` | Il gioco in una pagina: round, numeri, mappa |
| `ai/STATE.md` | Cosa esiste nel codice **oggi** e cosa manca |
| `ai/CODE.md` | Architettura, convenzioni, ricette, i cinque errori già fatti |
| `ai/DATA.json` | Gli stessi numeri in forma macchina |
| `ai/GLOSSARY.md` | Il vocabolario, e le parole vietate |

> **Se sei un modello linguistico, leggi `ai/README.md` per primo.**
> Se `ai/` contraddice `docs/`, ha ragione `docs/` — e `ai/` va aggiornato nello stesso commit.

### Repository

Il progetto è un repository git. `.gitignore` esclude `.godot/`, `bin/`, `obj/`: **niente artefatti di
build nel repo**, solo sorgenti, documenti e contesto.

```bash
git log --oneline        # storia
git status               # stato
```

---

## Stato

**Design: completo.**

- [x] Concept, formato, economia, mappa, round
- [x] 5 specializzazioni · 15 armi + coltello · ottiche
- [x] Brief artistico delle 16 armi
- [x] Piano di sviluppo · account e backend
- [ ] Matrice di rianimazione da confermare
- [ ] Scrap da ECO da confermare

**Codice (M0):**

- [x] Simulazione a passo fisso 64 Hz, deterministica — **26/26 test**
- [x] Predizione client + riconciliazione + netgraph
- [x] Server dedicato headless con console e comandi
- [x] Menu principale con stato WIP visibile
- [x] Stanza debug: teleport, ispettore, latenza/loss, bot, dump armi
- [ ] **Far parlare client e server end-to-end** ← prossimo passo
- [ ] Lag compensation
- [ ] Sparo · round · economia
- [ ] Graybox e arte

## Prossimo passo operativo

**Far girare server e client insieme** e misurare la riconciliazione con **200 ms di ping e 2% di
perdita pacchetti**. È il **Gate G1** del piano: se il combattimento non è fluido a quelle condizioni,
non si passa avanti.

```bash
./run.sh server     # in un terminale
./run.sh game       # in un altro
```
