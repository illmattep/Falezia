using System.Collections.Generic;
using Falezia.Net;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// LA SELEZIONE PERSONAGGIO (doc 10). Compare nel riscaldamento e sparisce al fischio
/// d'inizio del round 1, quando la scelta si blocca per tutta la partita.
///
/// Non è un eroe: sono **cinque specializzazioni** che danno tempo o informazione, e
/// l'arma che dichiari di voler usare. Tutto è **pubblico**: chi ti sta di fronte vede
/// cosa hai scelto e può rispondere. È informazione, non identità.
///
/// Il pannello si costruisce da solo all'avvio della scena: non c'è niente da agganciare
/// nella scena, così non tocca il lavoro di chi monta il GameRoot.
/// </summary>
public sealed class DraftScreen : MonoBehaviour
{
    /// <summary>True mentre il pannello è aperto: lo legge PlayerRig per fermare l'input.</summary>
    public static bool Open { get; private set; }

    private static readonly Specialization[] Options =
    {
        Specialization.Decifratore,
        Specialization.Rianimatore,
        Specialization.Portatore,
        Specialization.Ingegnere,
        Specialization.Ricognitore
    };

    private PlayerRig _rig;
    private Texture2D _dim, _panel, _accent, _line;
    private GUIStyle _title, _sub, _cardTitle, _cardBody, _tiny, _label, _accentText, _chip, _chipOn;
    private float _builtScale = -1f;
    private bool _wasOpen;

    private Specialization _spec = Specialization.None;
    private int _intent;
    private bool _loaded;

    private static WeaponSlot[] _primaries;
    private static WeaponSlot[] Primaries
    {
        get
        {
            if (_primaries != null) return _primaries;
            var list = new List<WeaponSlot>();
            foreach (WeaponSlot slot in System.Enum.GetValues(typeof(WeaponSlot)))
            {
                var def = Arsenal.Get(slot);
                if (def != null && def.IsPrimary && def.Price > 0) list.Add(slot);
            }
            _primaries = list.ToArray();
            return _primaries;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("DraftScreen");
        DontDestroyOnLoad(go);
        go.AddComponent<DraftScreen>();
    }

    private void Awake()
    {
        _dim = Solid(new Color(0.01f, 0.02f, 0.03f, 0.55f));
        _panel = Solid(new Color(UiTheme.PanelColor.r, UiTheme.PanelColor.g, UiTheme.PanelColor.b, 0.96f));
        _accent = Solid(UiTheme.Accent);
        _line = Solid(UiTheme.Line);
    }

    private void OnDestroy()
    {
        if (_dim) Destroy(_dim);
        if (_panel) Destroy(_panel);
        if (_accent) Destroy(_accent);
        if (_line) Destroy(_line);
        Open = false;
    }

    private static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void Update()
    {
        if (_rig == null) _rig = FindAnyObjectByType<PlayerRig>();

        // il brief vale solo per chi è già dentro la partita: in coda il fase è ancora
        // Warmup ma la lobby non è completa, e il pannello non deve comparire
        Open = _rig != null && _rig.Online && !MainMenu.Open
               && _rig.Match.Phase == RoundPhase.Warmup
               && _rig.LastSnapshot is { InMatch: true };

        if (Open && _rig.LastSnapshot is { } snap && !_loaded && snap.State.Get(_rig.LocalId) is { } me)
        {
            _spec = me.Spec;
            _intent = me.Intent;
            _loaded = true;
        }

        // il mouse deve essere libero per scegliere, ma solo qui: appena parte il round
        // torna catturato, altrimenti si giocherebbe con il cursore a schermo.
        if (Open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _wasOpen = true;
        }
        else if (_wasOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _wasOpen = false;
            _loaded = false;
        }
    }

    // ------------------------------------------------------------------ disegno

    private void OnGUI()
    {
        if (!Open || _rig == null) return;

        float s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
        if (!Mathf.Approximately(s, _builtScale)) BuildStyles(s);

        float pw = Mathf.Min(Screen.width - 40f, 1240f * s);
        float ph = 560f * s;
        var panel = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
        float pad = 26f * s;

        UiBlur.I.Refresh();
        var backdrop = UiBlur.I.Result;
        if (backdrop != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backdrop, ScaleMode.StretchToFill, false);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _dim);
        GUI.DrawTexture(panel, _panel);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f * s), _accent);

        // ---- testata
        float y = panel.y + pad;
        GUI.Label(new Rect(panel.x + pad, y, pw * 0.5f, 34f * s), "SELEZIONE", _title);

        int secs = Mathf.Max(0, Mathf.CeilToInt(_rig.Match.TimeLeft));
        var clock = new Rect(panel.x + pw - pad - 130f * s, y, 130f * s, 34f * s);
        GUI.Label(clock, $"{secs / 60}:{secs % 60:00}", _accentText);

        // ---- IL BLOCCO: scegliere non basta, si conferma. Senza questo passo un clic per
        // curiosità chiudeva il brief per tutti (il server vede "hanno scelto tutti" e va
        // agli acquisti): il minuto deve valere un minuto.
        var me = _rig.LastSnapshot?.State.Get(_rig.LocalId);
        bool locked = me != null && me.Spec != Specialization.None;
        var lockRect = new Rect(clock.x - 232f * s, y, 220f * s, 34f * s);

        if (locked)
        {
            GUI.Label(lockRect, $"BLOCCATO · {Draft.Name(me.Spec)}", _accentText);
            _spec = me.Spec;      // la luce resta su quello che il server ha davvero registrato
        }
        else
        {
            bool ready = _spec != Specialization.None;
            string label = ready ? $"BLOCCA · {Draft.Name(_spec)}" : "SCEGLI UN OPERATORE";
            if (UiTheme.Button(lockRect, label, primary: ready, enabled: ready))
                _rig.Draft(_spec, (byte)_intent);
        }

        y += 40f * s;
        GUI.Label(new Rect(panel.x + pad, y, pw - pad * 2f, 20f * s),
            "Scegli e BLOCCA: la scelta è pubblica e resta per tutta la partita · nessun eroe, solo tempo e informazione",
            _sub);

        // ---- le cinque specializzazioni
        y += 30f * s;
        float gap = 12f * s;
        float cw = (pw - pad * 2f - gap * 4f) / 5f;
        float ch = 150f * s;

        for (int i = 0; i < Options.Length; i++)
        {
            var spec = Options[i];
            var card = new Rect(panel.x + pad + i * (cw + gap), y, cw, ch);

            bool picked = _spec == spec;
            GUI.DrawTexture(card, picked ? _accent : _line);
            var inner = new Rect(card.x + 2f, card.y + 2f, card.width - 4f, card.height - 4f);
            GUI.DrawTexture(inner, _panel);

            if (GUI.Button(card, GUIContent.none, GUIStyle.none) && !locked) _spec = spec;

            var titleRect = new Rect(card.x + 10f * s, card.y + 12f * s, card.width - 20f * s, 22f * s);
            GUI.Label(titleRect, Draft.Name(spec), picked ? _accentText : _cardTitle);

            var bodyRect = new Rect(card.x + 10f * s, card.y + 40f * s, card.width - 20f * s, ch - 50f * s);
            GUI.Label(bodyRect, Draft.Effect(spec), _cardBody);

            if (picked)
                GUI.Label(new Rect(card.x + 10f * s, card.y + ch - 26f * s, card.width - 20f * s, 18f * s),
                    "SCELTA", _accentText);
        }

        // ---- intento di loadout: dichiari l'arma, non te la regalano
        y += ch + 24f * s;
        GUI.Label(new Rect(panel.x + pad, y, 420f * s, 18f * s), "ARMA CHE INTENDI USARE", _tiny);
        y += 22f * s;

        var prim = Primaries;
        int perRow = 6;
        float bw = (pw - pad * 2f - 8f * s * (perRow - 1)) / perRow;
        float bh = 28f * s;

        for (int i = 0; i < prim.Length; i++)
        {
            int row = i / perRow, col = i % perRow;
            var slot = prim[i];
            var btn = new Rect(panel.x + pad + col * (bw + 8f * s), y + row * (bh + 6f * s), bw, bh);

            bool chosen = _intent == (int)slot;
            UiTheme.Fill(btn, chosen ? new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.18f)
                                     : new Color(UiTheme.PanelColor.r, UiTheme.PanelColor.g, UiTheme.PanelColor.b, 0.75f));
            UiTheme.Border(btn, chosen ? UiTheme.Accent : UiTheme.Line);
            if (GUI.Button(btn, GUIContent.none, GUIStyle.none) && !locked) _intent = (int)slot;
            GUI.Label(new Rect(btn.x + 8f * s, btn.y, btn.width - 16f * s, btn.height),
                      Arsenal.Get(slot).Name, chosen ? _chipOn : _chip);
        }

        // ---- chi ha scelto cosa: il motivo per cui il draft è pubblico
        y += 2f * (bh + 6f * s) + 14f * s;
        GUI.DrawTexture(new Rect(panel.x + pad, y, pw - pad * 2f, 1f), _line);
        y += 10f * s;

        var snap = _rig.LastSnapshot;
        if (snap == null)
        {
            GUI.Label(new Rect(panel.x + pad, y, pw - pad * 2f, 18f * s), "in attesa dello stato della partita…", _tiny);
            return;
        }

        int shown = 0;
        foreach (var p in snap.State.Players.Values)
        {
            var rect = new Rect(panel.x + pad + (shown % 2) * (pw - pad * 2f) * 0.5f, y + (shown / 2) * 20f * s,
                                (pw - pad * 2f) * 0.5f, 18f * s);
            string team = p.Team == 1 ? "NORD" : "SUD";
            string weapon = p.Intent == 0 ? "—" : Arsenal.Get((WeaponSlot)p.Intent).Name;
            string mine = p.Id == _rig.LocalId ? " · tu" : "";
            GUI.Label(rect, $"{team} {p.Id}{mine}   {Draft.Name(p.Spec)}   {weapon}", _label);
            shown++;
            if (shown >= 10) break;
        }
    }

    private void BuildStyles(float s)
    {
        _builtScale = s;

        _title = new GUIStyle(GUI.skin.label)
        {
            font = UiTheme.FontCondBold,
            fontSize = Mathf.RoundToInt(28 * s),
            alignment = TextAnchor.MiddleLeft
        };
        _title.normal.textColor = UiTheme.Text;
        _sub = new GUIStyle(GUI.skin.label)
        { font = UiTheme.FontBody, fontSize = Mathf.RoundToInt(12 * s), normal = { textColor = UiTheme.Dim } };
        _cardTitle = new GUIStyle(GUI.skin.label)
        { font = UiTheme.FontCondBold, fontSize = Mathf.RoundToInt(15 * s) };
        _cardTitle.normal.textColor = UiTheme.Text;
        _cardBody = new GUIStyle(GUI.skin.label)
        { font = UiTheme.FontBody, fontSize = Mathf.RoundToInt(11.5f * s), wordWrap = true, normal = { textColor = UiTheme.Dim } };
        _tiny = new GUIStyle(GUI.skin.label) { font = UiTheme.FontBody, fontSize = Mathf.RoundToInt(11 * s) };
        _tiny.normal.textColor = UiTheme.Faint;
        _label = new GUIStyle(GUI.skin.label) { font = UiTheme.FontBody, fontSize = Mathf.RoundToInt(12 * s) };
        _label.normal.textColor = UiTheme.Text;
        // le armi dichiarate: pastiglie leggibili, non etichette grigie
        _chip = new GUIStyle(GUI.skin.label)
        { font = UiTheme.FontBody, fontSize = Mathf.RoundToInt(11 * s), alignment = TextAnchor.MiddleLeft };
        _chip.normal.textColor = UiTheme.Dim;
        _chipOn = new GUIStyle(_chip) { font = UiTheme.FontBodyBold };
        _chipOn.normal.textColor = UiTheme.Text;
        _accentText = new GUIStyle(GUI.skin.label)
        {
            font = UiTheme.FontCondBold,
            fontSize = Mathf.RoundToInt(17 * s),
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = UiTheme.Accent }
        };
    }
}
