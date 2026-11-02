using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// HUD E NEGOZIO.
///
/// Regole di stile (Valorant, ma sobrio): bianco al 90%, testo piccolo, niente pannelli
/// invadenti, un solo accento rosso. In basso a sinistra la vita, in basso a destra
/// munizioni e crediti, in alto al centro solo il timer del round. Il resto dello schermo
/// resta al gioco: la scena deve respirare.
///
/// I dati sono quelli veri della simulazione: vita, munizioni, crediti, fase, punteggio.
/// Il negozio si apre con B e compra solo in fase di acquisto (online).
/// </summary>
public class Hud : MonoBehaviour
{
    public static bool MenuOpen;
    public static bool PauseOpen;

    /// <summary>Impostazioni rapide dal menu di pausa.</summary>
    public static float Sensitivity = 1f;
    public static int FovOffset = 0;
    public static bool ShowPerf;

    private static bool _perf;
    private float _fps;

    private const float TextAlpha = 0.90f;

    private PlayerRig _rig;
    private Texture2D _white;
    private Font _fBody, _fBodyBold, _fCond, _fCondBold;

    private GUIStyle _tiny, _small, _body, _num, _numSub, _title, _btn, _card, _cardSub, _center;
    private static bool _debug;
    private int _cat;
    private string _toast = "";
    private float _toastLeft;

    private static readonly (string Name, WeaponSlot[] Slots)[] Categories =
    {
        ("PISTOLE", new[] { WeaponSlot.Standard, WeaponSlot.Hush, WeaponSlot.Chatter, WeaponSlot.Snub, WeaponSlot.Hammer }),
        ("MITRAGLIETTE", new[] { WeaponSlot.Cadence, WeaponSlot.Scrap, WeaponSlot.Blitz }),
        ("POMPE", new[] { WeaponSlot.Choke, WeaponSlot.Sweeper }),
        ("ASSALTI", new[] { WeaponSlot.Anvil, WeaponSlot.Service }),
        ("CECCHINI", new[] { WeaponSlot.Vantage, WeaponSlot.Spotter, WeaponSlot.Longshot }),
        ("PESANTI", new[] { WeaponSlot.Heavy }),
        ("MISCHIA", new[] { WeaponSlot.Melee }),
    };

    public void Init(PlayerRig rig)
    {
        _rig = rig;
        _white = new Texture2D(1, 1);
        _white.SetPixel(0, 0, Color.white);
        _white.Apply();

        _fBody = Resources.Load<Font>("Fonts/Barlow-Regular");
        _fBodyBold = Resources.Load<Font>("Fonts/Barlow-SemiBold");
        _fCond = Resources.Load<Font>("Fonts/BarlowCondensed-Medium");
        _fCondBold = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
    }

    public static void ToggleMenu()
    {
        MenuOpen = !MenuOpen;
        if (MenuOpen) UiBlur.I.Invalidate();
        Cursor.lockState = MenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = MenuOpen;
    }

    public static void ToggleDebug() => _debug = !_debug;
    public static void TogglePerf() => _perf = !_perf;

    /// <summary>Modalità foto: apre direttamente la pagina grafica della pausa.</summary>
    public static void SetPausePage(int page) => _pausePage = page;

    public static void TogglePause()
    {
        PauseOpen = !PauseOpen;
        if (PauseOpen) UiBlur.I.Invalidate();
        if (!PauseOpen) _pausePage = 0;
        MenuOpen = false;
        Cursor.lockState = PauseOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = PauseOpen;
    }

    /// <summary>Modalità foto: il negozio e il pannello debug si aprono da soli.</summary>
    public static void SetMenuPhotos(bool open)
    {
        MenuOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    public static void SetDebugPhotos(bool on) => _debug = on;

    /// <summary>Scala tipografica: tutto è pensato su 1080p e cresce con l'altezza.</summary>
    private static float S => Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.2f);

    private static Color White(float a = TextAlpha) => new(1f, 1f, 1f, a);
    private static Color Dim(float a = 0.62f) => new(0.86f, 0.89f, 0.93f, a);

    private float _styleScale = -1f;

    private void EnsureStyles()
    {
        UiTheme.Ensure();

        // i font arrivano dal tema: un posto solo da cambiare
        _fBody = UiTheme.FontBody;
        _fBodyBold = UiTheme.FontBodyBold;
        _fCond = UiTheme.FontCond;
        _fCondBold = UiTheme.FontCondBold;

        if (_num != null && Mathf.Abs(_styleScale - UiTheme.S) < 0.005f) return;
        _styleScale = UiTheme.S;

        float s = S;
        _num = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * s), font = _fCondBold, alignment = TextAnchor.MiddleRight };
        _num.normal.textColor = White();
        _numSub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), font = _fCond };
        _numSub.normal.textColor = Dim(0.72f);
        _body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(16 * s), font = _fBodyBold };
        _body.normal.textColor = White();
        _small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13 * s), font = _fBody };
        _small.normal.textColor = Dim(0.72f);
        _tiny = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(11 * s), font = _fBody };
        _tiny.normal.textColor = Dim(0.45f);
        _title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * s), font = _fCondBold };
        _title.normal.textColor = White();
        _card = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), font = _fBodyBold };
        _card.normal.textColor = White();
        _cardSub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13 * s), font = _fBody };
        _cardSub.normal.textColor = Dim(0.62f);
        _btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14 * s), font = _fBodyBold, alignment = TextAnchor.MiddleCenter };
        _btn.normal.textColor = White(0.85f);
        _btn.hover.textColor = Color.white;
        _center = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * s), font = _fCondBold, alignment = TextAnchor.MiddleCenter };
        _center.normal.textColor = White();
    }

    private static string PhaseName(RoundPhase p) => p switch
    {
        RoundPhase.Warmup => "SELEZIONE",
        RoundPhase.Buy => "ACQUISTI",
        RoundPhase.Live => "ROUND",
        RoundPhase.RoundEnd => "FINE ROUND",
        _ => "FINE PARTITA"
    };

    private void OnGUI()
    {
        if (_rig == null) return;

        // il menu principale è un'altra schermata: sotto non si vede né HUD né mirino.
        // Lo stesso vale per la coda e per il brief: lì il gioco non è ancora cominciato,
        // e mirino, munizioni e minimappa davanti alla scelta dell'operatore sono rumore.
        // (la pausa resta disegnata: da lì si può uscire anche mentre si aspetta)
        if (MainMenu.Open) return;
        if ((DraftScreen.Open || MatchFlow.BlocksPlay) && !PauseOpen) return;

        UiBlur.I.Refresh();
        if (UiBlur.I.Suppress) return;      // frame riservato alla cattura del fondo

        EnsureStyles();

        if (_toastLeft > 0f) _toastLeft -= Time.deltaTime;

        var p = _rig.State;
        var def = Arsenal.Get(p.Weapon.Slot);

        if (_perf) DrawPerf();

        _scoreboard = Input.GetKey(KeyCode.Tab) && !MenuOpen && !PauseOpen && !MainMenu.Open;

        DrawRound();
        DrawObjective();
        DrawCrosshair(p);
        DrawVitals(p);
        DrawWeapon(p, def);
        if (!MenuOpen && !PauseOpen) DrawPickupHint();
        if (_rig.ThirdPerson) DrawThirdPersonHint();
        DrawScoreboard();
        DrawToast(p);
        if (_debug) DrawDebug(p, def);
        if (MenuOpen)
        {
            DrawBlur();
            DrawMenu();
        }
        if (PauseOpen) DrawPause();
    }

    // ---------------------------------------------------------------- round

    private void DrawRound()
    {
        if (!_rig.Online) return;

        var m = _rig.Match;
        float s = S;
        int secs = Mathf.CeilToInt(m.TimeLeft);

        var box = new Rect(Screen.width * 0.5f - 150f * s, 14f * s, 300f * s, 26f * s);
        GUI.Label(box, $"{m.ScoreNorth}   {secs / 60}:{secs % 60:00}   {m.ScoreSouth}", _center);

        var sub = new GUIStyle(_tiny) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(box.x, box.yMax - 2f * s, box.width, 16f * s),
                  $"{PhaseName(m.Phase).ToLowerInvariant()} · round {m.Round}", sub);
    }

    /// <summary>
    /// L'obiettivo del round: dov'è il Nucleo e cosa devo fare. Sta sotto il timer perché
    /// è la seconda informazione più importante dopo il tempo che resta.
    /// </summary>
    private void DrawObjective()
    {
        if (!_rig.Online) return;

        var m = _rig.Match;
        float s = S;

        string line = m.Core switch
        {
            CorePhase.Closed => $"CASSA CHIUSA · tieni E per aprirla · {m.OpenProgress * 100f:0}%",
            CorePhase.Opened => "CASSA APERTA · tieni E per prelevare il Nucleo",
            CorePhase.Dropped => "NUCLEO A TERRA · tieni E per raccoglierlo",
            CorePhase.Carried when m.CoreOwner == _rig.LocalId => "PORTI IL NUCLEO · portalo alla Grotta o alla Galleria",
            CorePhase.Carried => $"NUCLEO ADDOSSO A {m.CoreOwner}",
            CorePhase.Siege => $"ASSEDIO · {Mathf.CeilToInt(m.SiegeLeft)}s · " +
                               (m.ExtractZone == Objective.ZoneEast ? "GROTTA" : "GALLERIA"),
            _ => ""
        };

        if (line.Length == 0) return;

        var box = new Rect(Screen.width * 0.5f - 260f * s, 42f * s, 520f * s, 18f * s);
        GUI.Label(box, line, _center);

        // barra del gesto in corso (prelievo, avvio, rimozione): 3 s o 5 s
        if (m.Channel > 0.05f)
        {
            float full = m.Core == CorePhase.Siege ? Objective.RemoveSeconds : Objective.PickupSeconds;
            float t = Mathf.Clamp01(m.Channel / full);
            var bar = new Rect(Screen.width * 0.5f - 120f * s, 62f * s, 240f * s, 5f * s);
            GUI.color = new Color(0f, 0f, 0f, 0.34f);
            GUI.DrawTexture(bar, _white);
            GUI.color = new Color(1f, 0.28f, 0.34f, 0.95f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * t, bar.height), _white);
            GUI.color = Color.white;
        }
    }

    // ---------------------------------------------------------------- scoreboard

    private static bool _scoreboard;

    /// <summary>
    /// La classifica (Tab). In un gioco senza respawn sapere chi è ancora in piedi e con
    /// quanti soldi è informazione, non decorazione: sta su un pannello di vetro, due colonne.
    /// </summary>
    private void DrawScoreboard()
    {
        if (!_scoreboard || _rig == null) return;

        float s = S;
        var players = _rig.Online && _rig.LastSnapshot != null
            ? _rig.LastSnapshot.State.Players.Values
            : GameRoot.Sim.Players.Values;

        float w = Mathf.Min(Screen.width - 80f * s, 820f * s);
        float h = Mathf.Min(Screen.height - 120f * s, 460f * s);
        var panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        UiTheme.Panel(panel, 0.94f);
        UiTheme.Fill(new Rect(panel.x, panel.y, panel.width, 3f * s), UiTheme.Accent);

        var m = _rig.Match;
        var head = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(30 * s), alignment = TextAnchor.MiddleCenter };
        head.normal.textColor = Bone;
        GUI.Label(new Rect(panel.x, panel.y + 16f * s, panel.width, 36f * s),
                  $"{m.ScoreNorth}   —   {m.ScoreSouth}", head);

        var sub = new GUIStyle(_tiny) { alignment = TextAnchor.MiddleCenter };
        sub.normal.textColor = Dim();
        GUI.Label(new Rect(panel.x, panel.y + 50f * s, panel.width, 18f * s),
                  $"{PhaseName(m.Phase)} · ROUND {m.Round} · {(m.Core == CorePhase.Siege ? $"ASSEDIO {Mathf.CeilToInt(m.SiegeLeft)}s" : CorePhaseName(m.Core))}", sub);

        float top = panel.y + 82f * s;
        float colW = (panel.width - 48f * s) * 0.5f;
        float rowH = 30f * s;

        DrawTeamColumn(new Rect(panel.x + 24f * s, top, colW, panel.height - 100f * s), 1, players, s, rowH);
        DrawTeamColumn(new Rect(panel.x + 24f * s + colW + 24f * s - 24f * s, top, colW, panel.height - 100f * s), 2, players, s, rowH);
    }

    private void DrawTeamColumn(Rect r, int team, ICollection<PlayerState> players, float s, float rowH)
    {
        var head = new GUIStyle(_body) { fontSize = Mathf.RoundToInt(13 * s) };
        head.normal.textColor = team == 1 ? UiTheme.Cool : UiTheme.Accent;
        GUI.Label(new Rect(r.x, r.y, r.width, 20f * s), team == 1 ? "NORD" : "SUD", head);

        float y = r.y + 26f * s;
        int shown = 0;

        foreach (var p in players)
        {
            if (p.Team != team) continue;
            if (shown >= 8) break;

            var row = new Rect(r.x, y, r.width, rowH);
            UiTheme.Divider(row.x, row.yMax - 1f, row.width, 0.06f);

            if (!p.Alive) UiTheme.Fill(new Rect(row.x, row.y, 2f * s, row.height - 2f * s), new Color(1f, 1f, 1f, 0.10f));
            else UiTheme.Fill(new Rect(row.x, row.y, 2f * s, row.height - 2f * s), team == 1 ? UiTheme.Cool : UiTheme.Accent);

            var name = new GUIStyle(_small) { alignment = TextAnchor.MiddleLeft };
            name.normal.textColor = p.Alive ? Bone : Dim(0.45f);
            string me = _rig.Online && p.Id == _rig.LocalId ? " · TU" : "";
            GUI.Label(new Rect(row.x + 10f * s, row.y, row.width * 0.42f, row.height - 2f * s),
                      $"OPERATORE {p.Id}{me}", name);

            var spec = new GUIStyle(_tiny) { alignment = TextAnchor.MiddleLeft };
            spec.normal.textColor = p.Spec == Specialization.None ? Dim(0.35f) : Dim(0.8f);
            GUI.Label(new Rect(row.x + row.width * 0.40f, row.y, row.width * 0.30f, row.height - 2f * s),
                      p.Spec == Specialization.None ? "—" : Draft.Name(p.Spec), spec);

            var kd = new GUIStyle(_small) { alignment = TextAnchor.MiddleRight };
            kd.normal.textColor = p.Alive ? Bone : Dim(0.45f);
            GUI.Label(new Rect(row.x + row.width * 0.70f, row.y, row.width * 0.28f, row.height - 2f * s),
                      $"{p.Kills}/{p.Deaths}", kd);

            y += rowH;
            shown++;
        }
    }

    private static string CorePhaseName(CorePhase c) => c switch
    {
        CorePhase.Closed => "CASSA CHIUSA",
        CorePhase.Opened => "CASSA APERTA",
        CorePhase.Carried => "NUCLEO IN MOTO",
        CorePhase.Dropped => "NUCLEO A TERRA",
        CorePhase.Siege => "ASSEDIO",
        _ => ""
    };

    // ---------------------------------------------------------------- mirino

    private void DrawCrosshair(PlayerState p)
    {
        if (p.Weapon.AdsT > 0.55f) return;   // in mira si usano le mire dell'arma

        float s = S;
        float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
        float gap = (4f + p.Weapon.Spread * 2.1f) * s;
        float len = 6.5f * s;
        float th = Mathf.Max(1f, Mathf.Round(1.5f * s));

        void Tick(float x, float y, float w, float h)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x - 1f, y - 1f, w + 2f, h + 2f), _white);
            GUI.color = White(0.92f);
            GUI.DrawTexture(new Rect(x, y, w, h), _white);
        }

        Tick(cx - gap - len, cy - th * 0.5f, len, th);
        Tick(cx + gap, cy - th * 0.5f, len, th);
        Tick(cx - th * 0.5f, cy - gap - len, th, len);
        Tick(cx - th * 0.5f, cy + gap, th, len);
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- vita

    private void DrawVitals(PlayerState p)
    {
        float s = S;
        float x = 34f * s;
        float barY = Screen.height - 46f * s;
        float hp = Mathf.Clamp01(p.Health / PlayerState.MaxHealth);

        // numero: il riquadro è più alto del corpo, altrimenti IMGUI taglia i glifi
        var num = new GUIStyle(_num) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(32 * s) };
        GUI.Label(new Rect(x, barY - 44f * s, 140f * s, 44f * s), Mathf.CeilToInt(p.Health).ToString(), num);

        float w = 148f * s, h = Mathf.Max(2f, 3f * s);
        GUI.color = new Color(0f, 0f, 0f, 0.32f);
        GUI.DrawTexture(new Rect(x, barY, w, h), _white);
        GUI.color = hp > 0.35f ? White(0.92f) : new Color(1f, 0.33f, 0.36f, 0.95f);
        GUI.DrawTexture(new Rect(x, barY, w * hp, h), _white);
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- arma

    private void DrawWeapon(PlayerState p, WeaponDef def)
    {
        float s = S;
        float right = Screen.width - 34f * s;
        float barY = Screen.height - 46f * s;

        var mag = new GUIStyle(_num) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(38 * s) };
        GUI.Label(new Rect(right - 130f * s, barY - 50f * s, 130f * s, 50f * s), p.Weapon.Ammo.ToString(), mag);

        var res = new GUIStyle(_numSub) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(17 * s) };
        GUI.Label(new Rect(right - 226f * s, barY - 36f * s, 92f * s, 28f * s), p.Weapon.Reserve.ToString(), res);

        var cred = new GUIStyle(_small) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(12 * s) };
        int funds = Economy.TeamFunds(_rig.Match, p.Team);
        string money = _rig.Unlimited ? "$ ∞" : $"${p.Credits}  ·  cassa ${funds}";
        GUI.Label(new Rect(right - 300f * s, barY + 2f * s, 300f * s, 18f * s), money, cred);

        var name = new GUIStyle(_tiny) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(10 * s) };
        GUI.Label(new Rect(right - 260f * s, barY - 68f * s, 260f * s, 16f * s), def.Name.ToUpperInvariant(), name);
    }

    /// <summary>Terza persona di debug: si dice che è attiva, sennò sembra un difetto.</summary>
    private void DrawThirdPersonHint()
    {
        float s = S;
        var st = new GUIStyle(_tiny)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(11 * s),
        };
        st.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.55f);
        GUI.Label(new Rect(Screen.width * 0.5f - 200f * s, 46f * s, 400f * s, 20f * s),
                  "TERZA PERSONA · ALT PER TORNARE IN PRIMA", st);
    }

    /// <summary>Quando c'è un'arma a terra a portata: "F raccogli" e quanto ha dentro.</summary>
    private void DrawPickupHint()
    {
        var drop = _rig.NearbyDrop;
        if (drop == null) return;

        var def = Arsenal.Get(drop.Slot);
        float s = S;
        var st = new GUIStyle(_body)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(14 * s),
        };
        st.normal.textColor = Bone;

        GUI.Label(new Rect(Screen.width * 0.5f - 240f * s, Screen.height * 0.5f + 74f * s, 480f * s, 26f * s),
                  $"F · RACCOGLI {def.Name.ToUpperInvariant()}   {drop.Ammo}/{drop.Reserve}", st);
    }

    // ---------------------------------------------------------------- messaggi

    private void DrawToast(PlayerState p)
    {
        if (_toastLeft > 0f && !string.IsNullOrEmpty(_toast))
        {
            var st = new GUIStyle(_body) { alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = White(Mathf.Clamp01(_toastLeft) * TextAlpha);
            GUI.Label(new Rect(0, Screen.height * 0.70f, Screen.width, 24f * S), _toast, st);
        }

        if (!p.Alive)
        {
            var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(22 * S) };
            st.normal.textColor = White(0.9f);
            GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 30 * S), "ABBATTUTO", st);
            var sub = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * 0.42f + 26 * S, Screen.width, 20 * S), "aspetti il prossimo round", sub);
        }
    }

    private void Say(string text)
    {
        _toast = text;
        _toastLeft = 2.2f;
    }

    // ---------------------------------------------------------------- debug

    private void DrawDebug(PlayerState p, WeaponDef def)
    {
        float s = S;
        float w = 250f * s, h = 92f * s;
        var r = new Rect(18f * s, 18f * s, w, h);

        GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.45f);
        GUI.DrawTexture(r, _white);
        GUI.color = new Color(1f, 0.27f, 0.33f, 1f);
        GUI.DrawTexture(new Rect(r.x, r.y, 2f, h), _white);
        GUI.color = Color.white;

        var st = new GUIStyle(_tiny) { fontSize = Mathf.RoundToInt(12 * s) };
        st.normal.textColor = White(0.82f);
        GUI.Label(new Rect(r.x + 10f * s, r.y + 6f * s, w - 14f * s, h - 10f * s),
            $"pos {p.Pos.X:F1} {p.Pos.Y:F1} {p.Pos.Z:F1}\n" +
            $"cono {p.Weapon.Spread:F2}° · serie {p.Weapon.ShotIndex} · {def.RoundsPerMinute:F0} c/min\n" +
            $"ammo {p.Weapon.Ammo}/{p.Weapon.Reserve} · {(_rig.Online ? "online" : "locale")} · colpi {p.Weapon.Shots}\n" +
            $"{Screen.width}×{Screen.height} · F7 chiude",
            st);
    }

    // ---------------------------------------------------------------- sfondo sfocato

    /// <summary>
    /// Il menu non è trasparente sul gioco: è una copia della scena resa in un
    /// RenderTexture a bassa risoluzione e ridisegnata a tutto schermo. Rimpicciolire e
    /// ringrandire è il blur più economico che esista, e non richiede pass URP.
    /// </summary>
    /// <summary>
    /// Il fondo dei pannelli: la scena sfocata (UiBlur, due passate) più un velo scuro.
    /// Lo stesso vetro del menu principale: se cambia il blur, cambia per tutti.
    /// </summary>
    private void DrawBlur()
    {
        var full = new Rect(0, 0, Screen.width, Screen.height);

        UiBlur.I.Refresh();
        var blurred = UiBlur.I.Result;
        if (blurred != null)
            GUI.DrawTexture(full, blurred, ScaleMode.StretchToFill, false);
        else
            UiTheme.Fill(full, UiTheme.Ink);

        UiTheme.Fill(full, new Color(0.01f, 0.02f, 0.03f, 0.52f));
    }

    // ---------------------------------------------------------------- pausa

    private static int _pausePage;

    private void DrawPause()
    {
        float s = S;
        DrawBlur();

        float w = 430f * s;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.16f;

        var title = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(34 * s), alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = Bone;
        GUI.Label(new Rect(x, y, w, 42f * s), UiTheme.Spaced(_pausePage == 0 ? "PAUSA" : "GRAFICA"), title);

        UiTheme.Fill(new Rect(x + w * 0.5f - 30f * s, y + 46f * s, 60f * s, 2f * s), Red);

        if (_pausePage == 0) DrawPauseMain(x, y + 70f * s, w, s);
        else DrawPauseGraphics(x, y + 70f * s, w, s);

        GUI.Label(new Rect(x, Screen.height - 70f * s, w, 20f * s),
                  "Esc chiude · B apre il negozio · F8 performance", _tiny);
    }

    private void DrawPauseMain(float x, float y, float w, float s)
    {
        float bh = 40f * s, gap = 10f * s;
        var btn = new GUIStyle(_btn) { fontSize = Mathf.RoundToInt(14 * s), alignment = TextAnchor.MiddleCenter };
        btn.normal.textColor = Bone;

        if (UiTheme.Button(new Rect(x, y, w, bh), "RIPRENDI", primary: true)) TogglePause();
        if (UiTheme.Button(new Rect(x, y + bh + gap, w, bh), "GRAFICA")) _pausePage = 1;
        if (UiTheme.Button(new Rect(x, y + (bh + gap) * 2f, w, bh), "ESCI DAL GIOCO")) Quit();
    }

    private void DrawPauseGraphics(float x, float y, float w, float s)
    {
        float rowH = 42f * s;
        float row = 0f;

        void Row(string label, string value, System.Action minus, System.Action plus)
        {
            var r = new Rect(x, y + row * rowH, w, rowH);
            UiTheme.Divider(r.x, r.yMax - 1f, r.width, 0.07f);

            GUI.Label(new Rect(r.x + 2f * s, r.y, r.width * 0.44f, r.height - 4f * s), label, UiTheme.Small);
            GUI.Label(new Rect(r.x + r.width * 0.42f, r.y, r.width * 0.34f, r.height - 4f * s), value, UiTheme.Right);

            var left = new Rect(r.xMax - 76f * s, r.y + 3f * s, 30f * s, r.height - 10f * s);
            var right = new Rect(r.xMax - 38f * s, r.y + 3f * s, 30f * s, r.height - 10f * s);
            if (UiTheme.Button(left, "‹")) minus();
            if (UiTheme.Button(right, "›")) plus();

            row++;
        }

        var m = GameSettings.Fullscreen ? GameSettings.NativeSize : GameSettings.Mode;
        Row("PRESET", QualityPresets.Names[GameSettings.Preset],
            () => { GameSettings.Preset = Mathf.Max(0, GameSettings.Preset - 1); GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.Preset = Mathf.Min(3, GameSettings.Preset + 1); GameSettings.Apply(); GameSettings.Save(); });

        Row("RISOLUZIONE", GameSettings.Fullscreen ? $"{m.x} × {m.y} (NATIVA)" : $"{m.x} × {m.y}",
            () => GameSettings.StepResolution(-1),
            () => GameSettings.StepResolution(1));

        Row("SCHERMO", GameSettings.Fullscreen ? "SCHERMO INTERO" : "FINESTRA",
            () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; GameSettings.Apply(); GameSettings.Save(); });

        Row("QUALITÀ", QualitySettings.names.Length > 0
                ? QualitySettings.names[Mathf.Clamp(GameSettings.Quality, 0, QualitySettings.names.Length - 1)].ToUpperInvariant()
                : "—",
            () => { GameSettings.Quality = Mathf.Max(0, GameSettings.Quality - 1); GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.Quality = Mathf.Min(QualitySettings.names.Length - 1, GameSettings.Quality + 1); GameSettings.Apply(); GameSettings.Save(); });

        Row("V-SYNC", GameSettings.VSync ? "ON" : "OFF",
            () => { GameSettings.VSync = !GameSettings.VSync; GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.VSync = !GameSettings.VSync; GameSettings.Apply(); GameSettings.Save(); });

        Row("ANTI-ALIASING", GameSettings.AaName,
            () => { GameSettings.AntiAliasing = GameSettings.AntiAliasing switch { 8 => 4, 4 => 2, 2 => 0, _ => 8 }; GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.AntiAliasing = GameSettings.AntiAliasing switch { 0 => 2, 2 => 4, 4 => 8, _ => 0 }; GameSettings.Apply(); GameSettings.Save(); });

        Row("OMBRE", GameSettings.ShadowName,
            () => { GameSettings.Shadows = Mathf.Max(0, GameSettings.Shadows - 1); GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.Shadows = Mathf.Min(3, GameSettings.Shadows + 1); GameSettings.Apply(); GameSettings.Save(); });

        Row("SCALA RENDERING", $"{Mathf.RoundToInt(GameSettings.RenderScale * 100f)} %",
            () => { GameSettings.RenderScale = Mathf.Max(0.6f, GameSettings.RenderScale - 0.05f); GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.RenderScale = Mathf.Min(1f, GameSettings.RenderScale + 0.05f); GameSettings.Apply(); GameSettings.Save(); });

        Row("CAMPO VISIVO", GameSettings.Fov.ToString(),
            () => { GameSettings.Fov = Mathf.Clamp(GameSettings.Fov - 5, 70, 110); GameSettings.Save(); },
            () => { GameSettings.Fov = Mathf.Clamp(GameSettings.Fov + 5, 70, 110); GameSettings.Save(); });

        Row("VOLUME", GameSettings.MasterVolume + " %",
            () => { GameSettings.MasterVolume = Mathf.Clamp(GameSettings.MasterVolume - 5, 0, 100); GameSettings.Apply(); GameSettings.Save(); },
            () => { GameSettings.MasterVolume = Mathf.Clamp(GameSettings.MasterVolume + 5, 0, 100); GameSettings.Apply(); GameSettings.Save(); });

        Row("DETTAGLI MAPPA", GameSettings.MapDetail ? "ON" : "OFF",
            () => { GameSettings.MapDetail = !GameSettings.MapDetail; MapBuilder.SetDetail(GameSettings.MapDetail); GameSettings.Save(); },
            () => { GameSettings.MapDetail = !GameSettings.MapDetail; MapBuilder.SetDetail(GameSettings.MapDetail); GameSettings.Save(); });

        Row("MINIMAPPA", "ZOOM " + GameSettings.MinimapZoomPercent + " %",
            () => { GameSettings.MinimapZoom = Mathf.Max(0, GameSettings.MinimapZoom - 1); GameSettings.Save(); },
            () => { GameSettings.MinimapZoom = Mathf.Min(GameSettings.MinimapZoomSteps.Length - 1, GameSettings.MinimapZoom + 1); GameSettings.Save(); });

        Row("SENSIBILITÀ", GameSettings.Sensitivity + " %",
            () => { GameSettings.Sensitivity = Mathf.Clamp(GameSettings.Sensitivity - 5, 20, 300); GameSettings.Save(); },
            () => { GameSettings.Sensitivity = Mathf.Clamp(GameSettings.Sensitivity + 5, 20, 300); GameSettings.Save(); });

        float by = y + row * rowH + 12f * s;
        float bh = 38f * s;

        if (UiTheme.Button(new Rect(x, by, w, bh), "INDIETRO")) _pausePage = 0;

        GUI.Label(new Rect(x, by + bh + 8f * s, w, 18f * s),
                  GameSettings.Fullscreen
                      ? "a schermo intero si usa la risoluzione del monitor · per alleggerire: SCALA RENDERING"
                      : "risoluzione minima 1280 × 720", _tiny);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- performance

    private void DrawPerf()
    {
        float s = S;
        float dt = Mathf.Max(1e-5f, Time.unscaledDeltaTime);
        _fps = Mathf.Lerp(_fps <= 0f ? 1f / dt : _fps, 1f / dt, 0.06f);

        var st = new GUIStyle(_tiny) { fontSize = Mathf.RoundToInt(12 * s), alignment = TextAnchor.UpperRight };
        st.normal.textColor = new Color(0.85f, 0.95f, 0.85f, 0.85f);

        long mem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        var p = _rig.State;
        // sotto la minimappa, che occupa l'angolo in alto a destra
        GUI.Label(new Rect(Screen.width - 240f * s, Mathf.Max(12f * s, Minimap.BottomY + 8f * s), 226f * s, 90f * s),
            $"{_fps:F0} fps · {dt * 1000f:F1} ms" + "\n" +
            $"mem {mem} MB · {p.Pos.X:F0} {p.Pos.Z:F0}" + "\n" +
            $"{( _rig.Online ? "online" : "locale")} · {(Hud.MenuOpen ? "negozio" : "gioco")}",
            st);
    }

    // ---------------------------------------------------------------- negozio

    // Palette Valorant: navy di fondo, rosso #FF4655, off-white #ECE8E1.
    // La tavolozza non vive più qui: è quella di UiTheme, così menu, pausa e HUD
    // non possono divergere. Questi nomi restano perché il resto del file li usa.
    private static Color Ink => UiTheme.PanelColor;
    private static Color Panel => UiTheme.PanelColor;
    private static Color PanelHi => UiTheme.PanelLight;
    private static Color Line => UiTheme.Line;
    private static Color Red => UiTheme.Accent;
    private static Color Bone => UiTheme.Text;

    private int _selected;

    private void DrawMenu()
    {
        float s = S;
        float w = Screen.width, h = Screen.height;

        DrawBlur();

        float pad = Mathf.Round(36f * s);
        float topH = Mathf.Round(88f * s);
        float railW = Mathf.Round(214f * s);
        float detailW = Mathf.Round(330f * s);

        DrawMenuTop(pad, topH, s, w);

        var slots = Categories[_cat].Slots;
        if (_selected >= slots.Length) _selected = 0;

        // --- colonna delle categorie
        float railY = topH + pad * 0.6f;
        for (int i = 0; i < Categories.Length; i++)
        {
            var r = new Rect(pad, railY + i * 46f * s, railW, 40f * s);
            bool on = i == _cat;
            GUI.color = on ? PanelHi : new Color(1f, 1f, 1f, 0.02f);
            GUI.DrawTexture(r, _white);
            if (on)
            {
                GUI.color = Red;
                GUI.DrawTexture(new Rect(r.x, r.y, 3f * s, r.height), _white);
            }
            GUI.color = on ? Bone : new Color(Bone.r, Bone.g, Bone.b, 0.55f);
            var label = new GUIStyle(_btn)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(15 * s),
                padding = new RectOffset(Mathf.RoundToInt(18 * s), 0, 0, 0),
            };
            label.normal.textColor = GUI.color;
            if (GUI.Button(r, Categories[i].Name, label) && _cat != i)
            {
                _cat = i;
                _selected = 0;
            }
            GUI.color = Color.white;
        }

        // --- schede
        float gridX = pad + railW + pad * 0.8f;
        float gridW = w - gridX - detailW - pad * 1.6f;
        int cols = gridW > 900f * s ? 3 : 2;
        float gap = 14f * s;
        float cw = (gridW - gap * (cols - 1)) / cols;
        float gridY = topH + pad * 0.6f;

        // le schede riempiono l'altezza disponibile, come nel menu di Valorant
        int rows = Mathf.CeilToInt(slots.Length / (float)cols);
        float availH = h - gridY - pad * 1.6f;
        float ch = Mathf.Clamp((availH - gap * (rows - 1)) / Mathf.Max(1, rows), 150f * s, 360f * s);

        for (int i = 0; i < slots.Length; i++)
        {
            int col = i % cols, row = i / cols;
            var r = new Rect(gridX + col * (cw + gap), gridY + row * (ch + gap), cw, ch);
            DrawCard(r, slots[i], i == _selected, s);
        }

        // --- pannello dettagli
        if (slots.Length > 0)
        {
            var dr = new Rect(w - pad - detailW, topH + pad * 0.6f, detailW, h - topH - pad * 1.8f);
            DrawDetail(dr, slots[_selected], s);
        }

        var hint = new GUIStyle(_tiny) { fontSize = Mathf.RoundToInt(11 * s) };
        hint.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.45f);
        GUI.Label(new Rect(pad, h - pad * 1.5f, w - pad * 2f, 16f * s),
            _rig.Online
                ? "B chiude · l'acquisto lo conferma il server"
                : "B chiude · offline: hai già tutto l'arsenale", hint);
    }

    private void DrawMenuTop(float pad, float topH, float s, float w)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.04f);
        GUI.DrawTexture(new Rect(0, 0, w, topH), _white);
        GUI.color = Line;
        GUI.DrawTexture(new Rect(0, topH, w, 1f), _white);
        GUI.color = Color.white;

        var title = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(34 * s) };
        title.normal.textColor = Bone;
        GUI.Label(new Rect(pad, 16f * s, 420f * s, 40f * s), "ARMERIA", title);

        var sub = new GUIStyle(_tiny) { fontSize = Mathf.RoundToInt(11 * s) };
        sub.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.5f);
        string phase = _rig.Online
            ? $"{PhaseName(_rig.Match.Phase)} · {Mathf.CeilToInt(_rig.Match.TimeLeft)} s · round {_rig.Match.Round}"
            : "stanza di prova · armi sbloccate";
        GUI.Label(new Rect(pad + 2f * s, 50f * s, 560f * s, 18f * s), phase.ToUpperInvariant(), sub);

        var money = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(30 * s), alignment = TextAnchor.MiddleRight };
        money.normal.textColor = Bone;
        GUI.Label(new Rect(w - pad - 320f * s, 16f * s, 320f * s, 40f * s),
                  _rig.Unlimited ? "$ ∞" : $"$ {_rig.State.Credits}", money);

        var hint = new GUIStyle(_tiny) { fontSize = Mathf.RoundToInt(10 * s), alignment = TextAnchor.MiddleRight };
        hint.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.45f);
        GUI.Label(new Rect(w - pad - 320f * s, 48f * s, 320f * s, 16f * s),
                  _rig.Unlimited ? "stanza di prova" : $"tuoi · cassa squadra ${Economy.TeamFunds(_rig.Match, _rig.State.Team)}",
                  hint);
    }

    private void DrawCard(Rect r, WeaponSlot slot, bool selected, float s)
    {
        var d = Arsenal.Get(slot);
        bool afford = _rig.CanAfford(slot);

        GUI.color = selected ? PanelHi : Panel;
        GUI.DrawTexture(r, _white);
        if (selected)
        {
            GUI.color = Red;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f * s), _white);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f * s, r.width, 2f * s), _white);
            GUI.DrawTexture(new Rect(r.x, r.y, 2f * s, r.height), _white);
            GUI.DrawTexture(new Rect(r.xMax - 2f * s, r.y, 2f * s, r.height), _white);
        }
        GUI.color = Color.white;

        var pic = Thumb(d.Id);
        if (pic != null)
        {
            float ph = r.height * 0.56f;
            float pw = ph * 2f;
            GUI.color = new Color(1f, 1f, 1f, afford ? 0.95f : 0.35f);
            GUI.DrawTexture(new Rect(r.center.x - pw * 0.5f, r.y + r.height * 0.12f, pw, ph), pic, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        var name = new GUIStyle(_card) { fontSize = Mathf.RoundToInt(19 * s) };
        name.normal.textColor = afford ? Bone : new Color(Bone.r, Bone.g, Bone.b, 0.5f);
        GUI.Label(new Rect(r.x + 14f * s, r.yMax - 54f * s, r.width - 28f * s, 26f * s), d.Name, name);

        var price = new GUIStyle(_card) { fontSize = Mathf.RoundToInt(17 * s), alignment = TextAnchor.MiddleRight };
        price.normal.textColor = afford ? Bone : Red;
        GUI.Label(new Rect(r.x + 14f * s, r.yMax - 54f * s, r.width - 28f * s, 26f * s),
            d.Price <= 0 ? "GRATIS" : "$" + d.Price, price);

        var cls = new GUIStyle(_cardSub) { fontSize = Mathf.RoundToInt(11 * s) };
        cls.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.5f);
        GUI.Label(new Rect(r.x + 14f * s, r.yMax - 28f * s, r.width - 28f * s, 18f * s), d.Class.ToUpperInvariant(), cls);

        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) _selected = IndexOf(slot);
    }

    private int IndexOf(WeaponSlot slot)
    {
        var slots = Categories[_cat].Slots;
        for (int i = 0; i < slots.Length; i++) if (slots[i] == slot) return i;
        return 0;
    }

    private void DrawDetail(Rect r, WeaponSlot slot, float s)
    {
        var d = Arsenal.Get(slot);
        bool afford = _rig.CanAfford(slot);

        GUI.color = Panel;
        GUI.DrawTexture(r, _white);
        GUI.color = Red;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f * s), _white);
        GUI.color = Color.white;

        float x = r.x + 20f * s;
        float w = r.width - 40f * s;

        var name = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(26 * s) };
        name.normal.textColor = Bone;
        GUI.Label(new Rect(x, r.y + 18f * s, w, 34f * s), d.Name.ToUpperInvariant(), name);

        var cls = new GUIStyle(_cardSub) { fontSize = Mathf.RoundToInt(12 * s) };
        cls.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.55f);
        GUI.Label(new Rect(x, r.y + 50f * s, w, 18f * s), d.Class.ToUpperInvariant(), cls);

        var pic = Thumb(d.Id);
        if (pic != null)
        {
            float ph = r.width * 0.36f;
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(r.center.x - ph, r.y + 74f * s, ph * 2f, ph), pic, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        float y = r.y + 74f * s + r.width * 0.36f + 14f * s;
        float lh = 36f * s;

        StatRow(new Rect(x, y, w, lh), "DANNO", Mathf.Clamp01(d.ShotDamage / 90f),
                d.ShotDamage.ToString(), s);
        StatRow(new Rect(x, y + lh, w, lh), "CADENZA", Mathf.Clamp01(d.RoundsPerMinute / 700f),
                $"{d.RoundsPerMinute:F0}", s);
        StatRow(new Rect(x, y + lh * 2f, w, lh), "CARICATORE", Mathf.Clamp01(d.Magazine / 40f),
                $"{d.Magazine}", s);
        StatRow(new Rect(x, y + lh * 3f, w, lh), "PORTATA", Mathf.Clamp01(d.FalloffEnd / 120f),
                $"{d.FalloffEnd:F0} m", s);
        StatRow(new Rect(x, y + lh * 4f, w, lh), "RICARICA", Mathf.Clamp01(1f - d.ReloadTactical / 4f),
                $"{d.ReloadTactical:F1} s", s);

        // riquadro d'acquisto in fondo, come Valorant
        var btn = new Rect(r.x + 20f * s, r.yMax - 62f * s, r.width - 40f * s, 42f * s);
        GUI.color = !afford ? new Color(1f, 1f, 1f, 0.08f) : Red;
        GUI.DrawTexture(btn, _white);
        GUI.color = Color.white;

        var bt = new GUIStyle(_btn)
        {
            fontSize = Mathf.RoundToInt(15 * s),
            alignment = TextAnchor.MiddleCenter,
        };
        bt.normal.textColor = Bone;
        bt.hover.textColor = Color.white;

        string text = !_rig.CanBuy ? "NON IN ACQUISTO" : afford ? $"ACQUISTA · {(d.Price <= 0 ? "GRATIS" : "$" + d.Price)}" : "CREDITI INSUFFICIENTI";
        GUI.enabled = _rig.CanBuy && afford;
        if (GUI.Button(btn, text, bt))
        {
            var outcome = _rig.Buy(slot);
            Say(_rig.Online ? $"{d.Name}: richiesta inviata alla squadra" : outcome.Text);
        }
        GUI.enabled = true;
    }

    private void StatRow(Rect r, string label, float value, string text, float s)
    {
        var st = new GUIStyle(_cardSub) { fontSize = Mathf.RoundToInt(11 * s) };
        st.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.55f);
        GUI.Label(new Rect(r.x, r.y, 140f * s, 20f * s), label, st);

        var val = new GUIStyle(_cardSub) { fontSize = Mathf.RoundToInt(11 * s), alignment = TextAnchor.MiddleRight };
        val.normal.textColor = Bone;
        GUI.Label(new Rect(r.x, r.y, r.width, 20f * s), text, val);

        float barY = r.y + 20f * s;
        float barW = r.width;
        GUI.color = new Color(1f, 1f, 1f, 0.08f);
        GUI.DrawTexture(new Rect(r.x, barY, barW, 3f * s), _white);
        GUI.color = Red;
        GUI.DrawTexture(new Rect(r.x, barY, barW * Mathf.Clamp01(value), 3f * s), _white);
        GUI.color = Color.white;
    }

    private static readonly Dictionary<string, Texture2D> Thumbs = new();

    /// <summary>Miniatura dell'arma dai PNG in StreamingAssets/weapons/ui (caricata una volta).</summary>
    private static Texture2D Thumb(string id)
    {
        if (Thumbs.TryGetValue(id, out var t)) return t;
        Thumbs[id] = null;

        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "weapons", "ui", id + ".png");
        if (!System.IO.File.Exists(path)) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(System.IO.File.ReadAllBytes(path))) return null;
        tex.wrapMode = TextureWrapMode.Clamp;
        Thumbs[id] = tex;
        return tex;
    }
}
