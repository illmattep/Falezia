using System;
using System.Globalization;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// IL MENU PRINCIPALE.
///
/// Prima non c'era: il gioco partiva e ti trovavi in mappa. Qui invece si entra davvero —
/// si sceglie di allenarsi contro i bot o di connettersi a un server, si sistemano le
/// impostazioni, si esce.
///
/// Sta sopra il gioco già costruito e lo congela (<c>Time.timeScale = 0</c>), con la scena
/// sfocata dietro: costa zero cambiare scena e si vede subito com'è il gioco.
///
/// Gli avvii automatici (pannello, foto, controllo input) saltano il menu: se c'è un flag
/// sulla riga di comando si va dritti in partita, come prima.
/// </summary>
public sealed class MainMenu : MonoBehaviour
{
    /// <summary>True mentre il menu è aperto: lo legge PlayerRig per non far muovere il giocatore.</summary>
    public static bool Open { get; private set; }

    private enum Page { Home, Connect, Settings }

    private Page _page = Page.Home;
    private string _host = "127.0.0.1";
    private string _port = "27015";
    private string _error = "";

    private GUIStyle _field;
    private float _stylesFor = -1f;
    private float _shownAt;

    // controllo qualità: -faleziaMenuShot /tmp/menu.png fotografa il menu e chiude.
    // È così che si guarda l'interfaccia senza aprire l'editor a mano.
    private string _shotPath;

    private const string VersionText = "ALPHA · M0";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Skip()) return;

        var go = new GameObject("MainMenu");
        DontDestroyOnLoad(go);
        go.AddComponent<MainMenu>();
    }

    /// <summary>Gli avvii da script non devono incontrare un menu: il pannello si aspetta un client in partita.</summary>
    private static bool Skip()
    {
        foreach (var a in Environment.GetCommandLineArgs())
            if (a is "-faleziaOnline" or "-faleziaShots" or "-faleziaProbe"
                or "-faleziaCheckInput" or "-faleziaSkipMenu")
                return true;
        return false;
    }

    private void Awake()
    {
        _host = PlayerPrefs.GetString("net.host", "127.0.0.1");
        _port = PlayerPrefs.GetString("net.port", "27015");
        _shownAt = Time.unscaledTime;
        GameSettings.Load();
        SetOpen(true);
        UiBlur.I.Invalidate();

        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-faleziaMenuShot" && i + 1 < args.Length) _shotPath = args[i + 1];
            if (args[i] == "-faleziaMenuPage" && i + 1 < args.Length)
            {
                if (Enum.TryParse<Page>(args[i + 1], true, out var page)) _page = page;
            }
        }
    }

    private void OnDestroy() => Open = false;

    private void SetOpen(bool open)
    {
        Open = open;

        // NON si mette Time.timeScale a zero: la camera del giocatore si posiziona nei suoi
        // Update, e con il tempo fermo resterebbe all'origine — cioè dentro il pavimento,
        // con il fondo del menu nero. L'input è già bloccato da PlayerRig (MainMenu.Open).
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void Update()
    {
        if (!Open) return;

        if (_shotPath != null)
        {
            StartCoroutine(CaptureMenu());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _page != Page.Home)
        {
            _page = Page.Home;
            _error = "";
        }
    }

    /// <summary>Fotografa il menu dopo un paio di frame (serve al controllo qualità) e chiude.</summary>
    private System.Collections.IEnumerator CaptureMenu()
    {
        string path = _shotPath;
        _shotPath = null;

        yield return new WaitForSecondsRealtime(0.8f);
        yield return new WaitForEndOfFrame();

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"menu: schermata salvata in {path}");

        yield return new WaitForSecondsRealtime(1.0f);
        Application.Quit();
    }

    // ------------------------------------------------------------------ disegno

    private void OnGUI()
    {
        if (!Open) return;

        UiTheme.Ensure();
        float s = UiTheme.S;

        // --- fondo: la scena sfocata, poi il velo
        UiBlur.I.Refresh();
        if (UiBlur.I.Suppress) return;      // questo frame serve alla cattura, non si disegna
        var full = new Rect(0, 0, Screen.width, Screen.height);
        var blurred = UiBlur.I.Result;
        if (blurred != null) GUI.DrawTexture(full, blurred, ScaleMode.StretchToFill, false);
        else UiTheme.Fill(full, UiTheme.Ink);

        UiTheme.Veil(0.48f);

        float pad = 60f * s;
        float railW = 270f * s;
        float contentX = pad + railW + 48f * s;

        // il pannello non occupa tutto: a destra si deve vedere la scena sfocata che respira
        float contentW = Mathf.Min(Mathf.Max(420f * s, Screen.width - contentX - pad), 780f * s);

        // --- contenuto (calcolato subito: serve anche al piede)
        float top = Screen.height * 0.16f;
        float panelH = Mathf.Min(Screen.height - top - 120f * s, 520f * s);
        var panel = new Rect(contentX, top - 8f * s, contentW, panelH);

        // --- marchio
        var mark = new Rect(pad, top - 8f * s, railW, 46f * s);
        GUI.Label(mark, UiTheme.Spaced("FALEZIA", "\u2009"), UiTheme.H1);

        // la sbarra rossa accanto al marchio respira: è l'unica animazione del menu
        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 1.6f);
        UiTheme.Fill(new Rect(pad, mark.yMax + 2f * s, 64f * s * pulse, 2f * s),
                     new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.9f));

        GUI.Label(new Rect(pad, mark.yMax + 12f * s, railW, 18f * s),
                  "TATTICO 5v5 · ESTRAZIONE", UiTheme.Tiny);

        // --- navigazione
        float navY = Screen.height * 0.42f;
        DrawNav(new Rect(pad + 6f * s, navY, railW - 20f * s, 34f * s), "GIOCA", Page.Home, s);
        DrawNav(new Rect(pad + 6f * s, navY + 44f * s, railW - 20f * s, 34f * s), "CONNETTI", Page.Connect, s);
        DrawNav(new Rect(pad + 6f * s, navY + 88f * s, railW - 20f * s, 34f * s), "IMPOSTAZIONI", Page.Settings, s);

        if (UiTheme.NavItem(new Rect(pad + 6f * s, navY + 152f * s, railW - 20f * s, 30f * s), "ESCI", false))
            Quit();

        // --- piede
        GUI.Label(new Rect(pad, Screen.height - 54f * s, railW + 200f * s, 18f * s),
                  $"{VersionText} · calanera", UiTheme.Tiny);
        GUI.Label(new Rect(panel.xMax - 320f * s, panel.yMax - 30f * s, 300f * s, 18f * s),
                  "ESC · indietro", UiTheme.Tiny);

        // --- contenuto
        UiTheme.Panel(panel, 0.9f);

        float inset = 36f * s;
        var area = new Rect(panel.x + inset, panel.y + inset, panel.width - inset * 2f, panel.height - inset * 2f);

        switch (_page)
        {
            case Page.Home: HomePage(area, s); break;
            case Page.Connect: ConnectPage(area, s); break;
            case Page.Settings: SettingsPage(area, s); break;
        }
    }

    private void DrawNav(Rect r, string text, Page page, float s)
    {
        if (UiTheme.NavItem(r, text, _page == page)) Switch(page);
    }

    /// <summary>Il rettangolo scuro dietro un campo di testo, con la riga sottile.</summary>
    private static void FieldBackground(Rect r)
    {
        UiTheme.Fill(r, new Color(0f, 0f, 0f, 0.35f));
        UiTheme.Border(r, UiTheme.LineStrong);
    }

    private bool Switch(Page page)
    {
        _page = page;
        _error = "";
        return true;
    }

    // ------------------------------------------------------------------ pagine

    private void HomePage(Rect a, float s)
    {
        UiTheme.SectionLabel(new Rect(a.x, a.y, a.width, 16f * s), "IL GIOCO");

        GUI.Label(new Rect(a.x, a.y + 22f * s, a.width, 52f * s), "CALANERA", UiTheme.H1);

        float y = a.y + 88f * s;
        Text(new Rect(a.x, y, a.width * 0.86f, 22f * s),
             "Due squadre, una cassa al centro, un Nucleo da portare fuori.");
        Text(new Rect(a.x, y + 26f * s, a.width * 0.86f, 22f * s),
             "Chi lo prende diventa il difensore. Chi lo perde lo deve andare a riprendere.");

        float by = y + 78f * s;
        float bh = 52f * s, gap = 12f * s;
        float bw = 190f * s;
        if (UiTheme.Button(new Rect(a.x, by, bw, bh), "GIOCA", primary: true))
            PlayOnline();

        if (UiTheme.Button(new Rect(a.x + bw + gap, by, bw, bh), "ALLENAMENTO"))
            StartGame();

        if (UiTheme.Button(new Rect(a.x + (bw + gap) * 2f, by, 210f * s, bh), "SERVER…"))
        {
            _page = Page.Connect;
            _error = "";
        }

        Text(new Rect(a.x, by + bh + 14f * s, a.width * 0.9f, 20f * s),
             "GIOCA entra in coda sul server: si parte quando la lobby è completa. L'allenamento è offline coi bot.");

        // --- i tre numeri che descrivono il gioco
        float cy = a.yMax - 96f * s;
        UiTheme.Divider(a.x, cy - 18f * s, a.width);

        float cw = a.width / 3f;
        Chip(new Rect(a.x, cy, cw, 60f * s), "5v5", "senza respawn");
        Chip(new Rect(a.x + cw, cy, cw, 60f * s), "120 s", "di assedio");
        Chip(new Rect(a.x + cw * 2f, cy, cw, 60f * s), "5", "specializzazioni");
    }

    private void Chip(Rect r, string big, string small)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 30f * UiTheme.S), big, UiTheme.H2);
        GUI.Label(new Rect(r.x, r.y + 34f * UiTheme.S, r.width, 18f * UiTheme.S), small, UiTheme.Small);
    }

    private void ConnectPage(Rect a, float s)
    {
        UiTheme.SectionLabel(new Rect(a.x, a.y, a.width, 16f * s), "SESSIONE");

        GUI.Label(new Rect(a.x, a.y + 22f * s, a.width, 44f * s), "DOVE GIOCARE", UiTheme.H2);

        float y = a.y + 92f * s;
        Text(new Rect(a.x, y, a.width * 0.9f, 40f * s),
             "L'indirizzo del server dedicato. In locale, dopo ./run.sh dev, è 127.0.0.1:27015.");

        y += 56f * s;
        float fieldW = 320f * s, portW = 130f * s;

        GUI.Label(new Rect(a.x, y, fieldW, 16f * s), "HOST", UiTheme.Tiny);
        GUI.Label(new Rect(a.x + fieldW + 16f * s, y, portW, 16f * s), "PORTA", UiTheme.Tiny);

        var hostRect = new Rect(a.x, y + 20f * s, fieldW, 38f * s);
        var portRect = new Rect(a.x + fieldW + 16f * s, y + 20f * s, portW, 38f * s);
        FieldBackground(hostRect);
        FieldBackground(portRect);

        _host = GUI.TextField(hostRect, _host, 64, FieldStyle());
        _port = GUI.TextField(portRect, _port, 5, FieldStyle());

        y += 82f * s;
        if (UiTheme.Button(new Rect(a.x, y, 220f * s, 48f * s), "CONNETTI", primary: true))
            TryConnect();

        if (UiTheme.Button(new Rect(a.x + 236f * s, y, 180f * s, 48f * s), "INDIETRO"))
            Switch(Page.Home);

        if (_error.Length > 0)
        {
            var st = new GUIStyle(UiTheme.Small);
            st.normal.textColor = UiTheme.Bad;
            GUI.Label(new Rect(a.x, y + 62f * s, a.width, 20f * s), _error, st);
        }

        Text(new Rect(a.x, a.max.y - 60f * s, a.width * 0.95f, 40f * s),
             "Se il server non risponde si gioca in locale: nessun blocco, nessun errore bloccante.");
    }

    private void SettingsPage(Rect a, float s)
    {
        UiTheme.SectionLabel(new Rect(a.x, a.y, a.width, 16f * s), "IMPOSTAZIONI");
        GUI.Label(new Rect(a.x, a.y + 22f * s, a.width, 44f * s), "VIDEO E CONTROLLO", UiTheme.H2);

        float y = a.y + 86f * s;
        float rowH = 33f * s;

        int d;

        d = Stepper(new Rect(a.x, y, a.width, rowH), "RISOLUZIONE", $"{GameSettings.Mode.x}×{GameSettings.Mode.y}");
        if (d != 0) { GameSettings.StepResolution(d); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "FINESTRA", GameSettings.Fullscreen ? "SCHERMO INTERO" : "FINESTRA");
        if (d != 0) { GameSettings.Fullscreen = !GameSettings.Fullscreen; ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "QUALITÀ", QualityName());
        if (d != 0) { GameSettings.Quality = Wrap(GameSettings.Quality + d, QualitySettings.names.Length); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "OMBRE", GameSettings.ShadowName);
        if (d != 0) { GameSettings.Shadows = Wrap(GameSettings.Shadows + d, 4); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "ANTI-ALIASING", GameSettings.AaName);
        if (d != 0) { GameSettings.AntiAliasing = NextAa(GameSettings.AntiAliasing, d); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "SCALA RENDERING", $"{Mathf.RoundToInt(GameSettings.RenderScale * 100f)}%");
        if (d != 0) { GameSettings.RenderScale = Mathf.Clamp(GameSettings.RenderScale + 0.05f * d, 0.5f, 1f); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "CAMPO VISIVO", $"{GameSettings.Fov}°");
        if (d != 0) { GameSettings.Fov = Mathf.Clamp(GameSettings.Fov + 5 * d, 60, 120); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "SENSIBILITÀ", $"{GameSettings.Sensitivity}");
        if (d != 0) { GameSettings.Sensitivity = Mathf.Clamp(GameSettings.Sensitivity + 5 * d, 10, 400); ApplySettings(); }

        y += rowH;
        d = Stepper(new Rect(a.x, y, a.width, rowH), "VOLUME", $"{GameSettings.MasterVolume}%");
        if (d != 0) { GameSettings.MasterVolume = Mathf.Clamp(GameSettings.MasterVolume + 5 * d, 0, 100); ApplySettings(); }

        y += rowH + 18f * s;
        if (UiTheme.Button(new Rect(a.x, y, 180f * s, 40f * s), "INDIETRO"))
            Switch(Page.Home);

        Text(new Rect(a.x + 200f * s, y + 8f * s, a.width - 200f * s, 24f * s),
             "Le modifiche si applicano subito e restano al prossimo avvio.");
    }

    private string QualityName()
    {
        var names = QualitySettings.names;
        if (names.Length == 0) return "—";

        string name = names[Mathf.Clamp(GameSettings.Quality, 0, names.Length - 1)].ToUpperInvariant();
        return name switch
        {
            "VERY LOW" => "MINIMA",
            "LOW" => "BASSA",
            "MEDIUM" => "MEDIA",
            "HIGH" => "ALTA",
            "VERY HIGH" => "MOLTO ALTA",
            _ => name
        };
    }

    private static int Wrap(int v, int count) => count <= 0 ? 0 : (v % count + count) % count;

    private static int NextAa(int current, int dir)
    {
        int[] steps = { 0, 2, 4, 8 };
        int i = Array.IndexOf(steps, current);
        if (i < 0) i = 2;
        i = Mathf.Clamp(i + dir, 0, steps.Length - 1);
        return steps[i];
    }

    private void ApplySettings()
    {
        GameSettings.Apply();
        GameSettings.Save();
    }

    // ------------------------------------------------------------------ azioni

    /// <summary>GIOCA: contatta il server e si mette in coda (matchmaking).</summary>
    private void PlayOnline()
    {
        var root = FindAnyObjectByType<GameRoot>();
        if (root == null) { _error = "nessuna scena di gioco caricata"; return; }

        string host = PlayerPrefs.GetString("net.host", "127.0.0.1");
        string portText = PlayerPrefs.GetString("net.port", "27015");
        if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
            port = 27015;

        Debug.Log($"menu: matchmaking su {host}:{port}");
        root.ConnectOnline(host, port);
        StartGame();
    }

    private void StartGame()
    {
        SetOpen(false);
        Destroy(gameObject, 0.2f);
        Debug.Log("menu: partita avviata");
    }

    private void TryConnect()
    {
        string host = (_host ?? "").Trim();
        if (host.Length == 0) { _error = "scrivi l'indirizzo del server"; return; }
        if (!int.TryParse(_port, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port)
            || port < 1 || port > 65535)
        {
            _error = "la porta deve essere un numero fra 1 e 65535";
            return;
        }

        var root = FindAnyObjectByType<GameRoot>();
        if (root == null) { _error = "nessuna scena di gioco caricata"; return; }

        PlayerPrefs.SetString("net.host", host);
        PlayerPrefs.SetString("net.port", port.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        root.ConnectOnline(host, port);
        StartGame();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ controlli

    private GUIStyle FieldStyle()
    {
        if (_field != null && Mathf.Abs(_stylesFor - UiTheme.S) < 0.01f) return _field;

        _stylesFor = UiTheme.S;
        float s = UiTheme.S;

        _field = new GUIStyle(GUI.skin.textField)
        {
            font = UiTheme.FontBodyBold,
            fontSize = Mathf.RoundToInt(15 * s),
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(Mathf.RoundToInt(12 * s), 8, 6, 6),
            border = new RectOffset(0, 0, 0, 0)
        };
        _field.normal.textColor = UiTheme.Text;
        _field.focused.textColor = Color.white;

        // il fondo lo disegniamo noi: un background bianco di Unity coprirebbe il vetro
        _field.normal.background = null;
        _field.focused.background = null;
        _field.hover.background = null;
        _field.active.background = null;
        return _field;
    }

    /// <summary>Riga con etichetta a sinistra e frecce a destra. Restituisce -1, 0 o +1.</summary>
    private int Stepper(Rect r, string label, string value)
    {
        float s = UiTheme.S;

        GUI.Label(new Rect(r.x, r.y, r.width * 0.45f, r.height), label, UiTheme.Body);

        var left = new Rect(r.xMax - 72f * s, r.y + 1f * s, 26f * s, r.height - 2f * s);
        var right = new Rect(r.xMax - 26f * s, r.y + 1f * s, 26f * s, r.height - 2f * s);

        // il valore finisce prima delle frecce: "SCHERMO INTERO" non ci finisce sopra
        float valueX = r.x + r.width * 0.40f;
        GUI.Label(new Rect(valueX, r.y, left.x - valueX - 10f * s, r.height), value, UiTheme.Right);

        int d = 0;
        if (UiTheme.Button(left, "‹")) d = -1;
        if (UiTheme.Button(right, "›")) d = 1;
        return d;
    }

    private static void Text(Rect r, string text)
    {
        var st = new GUIStyle(UiTheme.Small) { wordWrap = true };
        GUI.Label(r, text, st);
    }
}
