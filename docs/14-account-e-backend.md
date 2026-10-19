# 14 · Account e backend

> **Risposta breve: non costruire l'autenticazione da solo.**
> Usa un identity provider, e tieni nel tuo database **solo un ID**. Su Steam, l'account *è* lo
> SteamID e non esiste nessuna password.

---

## 1 · La decisione

| Se… | Allora |
|---|---|
| **Esci su Steam** *(scenario consigliato)* | **Steamworks Auth.** L'account è lo SteamID. Nessun form, nessuna password, nessuna email da gestire |
| Ti serve anche **fuori da Steam** | **Epic Online Services (EOS) Connect** come secondo provider. Gratuito, cross-platform |
| Vuoi partire senza dipendenze esterne | **Nakama** (self-hosted, open source) o **Supabase Auth**. Mai un DB di password scritto a mano |

**Perché non farlo da solo:** costruire l'autenticazione significa possedere la sicurezza, il reset
delle password, la deliverability delle email, il rate limiting, la prevenzione delle frodi e la
responsabilità in caso di violazione. È un progetto dentro il progetto, e non è il tuo mestiere.

---

## 2 · L'architettura, in un diagramma

```
┌──────────┐   1. login (Steam / EOS)    ┌───────────────────┐
│  CLIENT  │ ──────────────────────────► │ IDENTITY PROVIDER │
│          │ ◄────────────────────────── │  (Steam / EOS)    │
│          │   2. session ticket         └───────────────────┘
│          │
│          │   3. ticket                  ┌───────────────────┐
│          │ ──────────────────────────► │   TUO BACKEND     │
│          │ ◄────────────────────────── │  · valida ticket  │
│          │   4. game token (JWT, 10')   │  · crea l'account │
│          │                              │  · Postgres       │
│          │   5. game token              └───────────────────┘
│          │ ──────────────────────────► ┌───────────────────┐
│          │                              │   MATCHMAKING     │
│          │ ◄────────────────────────── │  · MMR + ping     │
│          │   6. {ip, port, match_token} │  · alloca server  │
│          │                              └───────────────────┘
│          │   7. connect + match_token   ┌───────────────────┐
│          │ ──────────────────────────► │  GAME SERVER      │
└──────────┘                              │  valida il token  │
                                          │  server→server    │
                                          └───────────────────┘
```

### Le tre regole che non si violano

1. **Il client non dichiara mai la propria identità.** Manda un *token*, non un nome utente.
2. **Il game server non si fida del client.** Valida il `match_token` con il backend prima di
   ammettere il giocatore in partita.
3. **Il `match_token` è monouso e legato a quella singola partita.** Non deve poter essere condiviso
   o riutilizzato su un altro server.

---

## 3 · I token

| Token | Vita | Chi lo emette | A cosa serve |
|---|---|---|---|
| **Session ticket** | Minuti | Steam / EOS | Prova che il giocatore possiede quell'account |
| **Game token** (JWT) | **10 minuti**, rinnovabile | Il tuo backend | Autentica il giocatore verso matchmaking e backend |
| **Match token** | La durata della partita, **monouso** | Il tuo matchmaking | Ammette il giocatore su *quel* server |

Firma i JWT con **HMAC** e una chiave condivisa fra backend e game server: così la validazione è
locale e non c'è un round-trip per ogni connessione. Ruota la chiave ogni 30 giorni con una finestra
di sovrapposizione.

---

## 4 · Cosa salvi, e cosa non salvi

| Salvi | Non salvi |
|---|---|
| `account_id` (UUID interno) | **Password** (non esistono) |
| `provider_id` (SteamID / EOS ID) | Nome e cognome |
| `display_name` + storico dei cambi | Email (solo se serve per l'assistenza, e solo allora) |
| `created_at`, stato del ban, impostazioni | Dati di pagamento (li gestisce Steam) |
| Inventario cosmetico, statistiche, MMR | IP oltre i **30 giorni** (solo per abusi e ban evasion) |

### GDPR, in cinque punti

1. **Minimizzazione:** se non ti serve, non lo raccogli. L'email non ti serve.
2. **Consenso separato** per la telemetria, distinto dall'accettazione della EULA.
3. **Diritto alla cancellazione:** elimina l'account, **anonimizza** le partite (il match resta per
   le statistiche, senza identità).
4. **Conservazione dichiarata:** log delle partite **90 giorni**, telemetria **12 mesi** anonimizzata,
   account bannati conservati per la durata del ban + 12 mesi.
5. **Registro dei trattamenti** documentato. Non serve un DPO per uno studio piccolo, serve il registro.

---

## 5 · Creazione dell'account: l'esperienza

L'errore da evitare è mettere un muro davanti al primo avvio.

```
Primo avvio
   └─► si gioca SUBITO come OSPITE (UUID locale, nessuna rete)
         · allenamento, poligono, stanza debug, partite coi bot
         · nessun dato personale, nessun consenso
         └─► "Gioca online"
               └─► UN CLICK: accedi con Steam (o EOS)
                     · l'ospite viene collegato all'account
                     · scegli il nome visualizzato: una volta sola
                     · fatto. Nessun form, nessuna password, nessuna email
```

**Nomi visualizzati:** unici, moderati, modificabili con **30 giorni di attesa**. Il nome non è la
chiave dell'account: la chiave è l'`account_id`, e il nome può cambiare senza toccare niente.

**Un solo account per persona:** niente condivisione, niente trasferimenti. Se un giorno servisse il
"family sharing", si affronta allora — non prima.

---

## 6 · Ban e integrità

| Cosa | Come |
|---|---|
| **Ban** | Sull'`account_id`, con motivo, data e scadenza. Canale di ricorso documentato |
| **Ban hardware** | **No nella v1.** Falsi positivi e carico di assistenza sproporzionato |
| **Prova** | Il replay deterministico: registri gli input e **rigiochi la partita**. È il tuo strumento migliore, e ce l'hai gratis dall'architettura |
| **Evasion** | Correlazione su IP (entro 30 giorni) e dispositivo, **come segnale**, mai come prova automatica |
| **2FA** | Sul portale web dell'account, **non** in gioco: in un FPS rompe il flusso |

---

## 7 · Stack consigliato

| Componente | Scelta | Costo |
|---|---|---|
| Identità | **Steamworks** (+ **EOS** se serve fuori da Steam) | gratuito (Steam: $100 una volta) |
| Backend API | **Go** o **C#** (ASP.NET minimal API) | — |
| Database | **PostgreSQL** | €10–25/mese |
| Cache / code | **Redis** (presenza, code, rate limit) | €10/mese |
| Matchmaking | servizio tuo, sopra Redis | — |
| Hosting | VPS con banda flat (Hetzner, OVH) | €10–40/mese |
| Alternativa "tutto in uno" | **Nakama** self-hosted | gratuito |

**Totale pre-lancio: €30–80 al mese.** Il costo serio non è l'hosting: è il tempo di sviluppo
(≈8 persona-settimane nel piano, Fase 3).

---

## 8 · Cosa fa M0, oggi

**Niente account.** Il prototipo usa una **identità di sviluppo**:

- un nome scelto nel menu, salvato in `user://dev_identity.json`
- il server assegna un `id` numerico al primo pacchetto ricevuto, e lo dimentica quando il client
  va in timeout
- nessuna persistenza, nessun database, nessuna rete oltre la LAN

**Gli account arrivano alla Fase 3**, insieme a matchmaking e backend — non prima.
Mettere l'autenticazione in M0 significherebbe costruire un cancello davanti a un gioco che
non esiste ancora.

---

## 9 · Le tre domande a cui rispondere prima della Fase 3

1. **Steam al lancio, sì o no?** Se sì, l'intero sistema di identità si riduce a Steamworks, e il
   resto del documento diventa molto più corto.
2. **Early Access a pagamento o free-to-play?** Cambia il modello di ricavi (doc 13), non
   l'architettura.
3. **Vuoi le statistiche storiche pubbliche?** Se sì, servono profili pubblici e quindi moderazione
   dei nomi — un costo operativo ricorrente, non una feature una tantum.
