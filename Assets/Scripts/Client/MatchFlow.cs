using Falezia.Sim;
using UnityEngine;

/// <summary>
/// LA CODA E IL "MATCH FOUND", lato client.
///
/// Quando premi **GIOCA** il client contatta il server e si mette in coda. Finché la lobby
/// non è completa si vede `IN CODA · n/N` (i numeri arrivano nello snapshot: il client non
/// conta niente da solo). Appena il server apre la partita compare **MATCH FOUND** per un
/// attimo, e poi il testimone passa a <see cref="DraftScreen"/>: il brief con i cinque
/// operatori, che si chiude da solo alla fase di acquisti.
///
/// Qui c'è solo il pezzo che manca al resto: la coda. Il brief è già suo.
/// </summary>
public class MatchFlow : MonoBehaviour
{
    private enum Stage { Queue, Found, Done }

    private Stage _stage = Stage.Queue;
    private float _stageTime;
    private PlayerRig _rig;

    private GUIStyle _title, _sub, _hint, _dots;
    private float _stylesFor = -1f;

    /// <summary>Il giocatore è fermo ai comandi (coda o "match found")? Lo legge PlayerRig.</summary>
    public static bool BlocksPlay { get; private set; }

    public const float FoundSeconds = 1.6f;

    public void Init(PlayerRig rig)
    {
        _rig = rig;
        _stage = Stage.Queue;
        BlocksPlay = true;

        // controllo qualità: `-faleziaFlowProbe <file> <secondi>` salva uno screenshot e chiude
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-faleziaFlowProbe");
        if (i >= 0 && i + 1 < args.Length)
        {
            _shotPath = args[i + 1];
            if (i + 2 < args.Length && float.TryParse(args[i + 2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float t)) _shotAt = t;
        }
    }

    private string _shotPath;
    private float _shotAt = 2.5f;

    private void LateUpdate()
    {
        if (string.IsNullOrEmpty(_shotPath) || Time.timeSinceLevelLoad < _shotAt) return;
        string path = _shotPath;
        _shotPath = null;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? "/tmp");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[FLUSSO] screenshot in {path}");
        Invoke(nameof(QuitAfterShot), 0.6f);
    }

    private void QuitAfterShot() => Application.Quit();

    private void OnDestroy() => BlocksPlay = false;

    private void Update()
    {
        if (_rig == null) return;
        _stageTime += Time.deltaTime;

        var snap = _rig.LastSnapshot;
        bool inMatch = snap != null && snap.InMatch;
        var phase = _rig.Match.Phase;

        switch (_stage)
        {
            case Stage.Queue:
                // la lobby si è riempita: il server ci ha messi in partita (brief)
                if (inMatch) { _stage = Stage.Found; _stageTime = 0f; Debug.Log("[CODA] match trovato"); }
                break;

            case Stage.Found:
                if (_stageTime >= FoundSeconds) { _stage = Stage.Done; Debug.Log("[CODA] al brief"); }
                break;

            case Stage.Done:
                break;
        }

        // il pannello blocca i comandi solo finché è visibile
        BlocksPlay = _stage != Stage.Done;
    }

    private void OnGUI()
    {
        if (_rig == null || _stage == Stage.Done) return;
        EnsureStyles();

        float s = UiTheme.S;
        UiTheme.Veil(0.66f);

        float w = Mathf.Min(Screen.width * 0.5f, 620f * s);
        float h = 250f * s;
        var panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        UiTheme.Panel(panel);

        var r = new Rect(panel.x + 26f * s, panel.y + 22f * s, panel.width - 52f * s, panel.height - 44f * s);

        if (_stage == Stage.Queue) DrawQueue(r, s);
        else DrawFound(r, s);
    }

    private void DrawQueue(Rect r, float s)
    {
        var snap = _rig.LastSnapshot;
        int have = snap?.QueueCount ?? 0;
        int need = Mathf.Max(1, snap?.QueueNeeded ?? 1);

        // "in coda" e "il server non risponde" non sono la stessa cosa: con UDP il socket
        // resta aperto anche se dall'altra parte non c'è nessuno, quindi si guarda l'età
        // dell'ultimo snapshot, non lo stato della connessione.
        bool mute = _rig.SecondsSinceSnapshot > 3f;

        GUI.Label(new Rect(r.x, r.y, r.width, 40f * s), mute ? "IN ATTESA DEL SERVER" : "IN CODA", _title);
        GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 22f * s),
                  mute ? "nessuno risponde: la lobby non è ancora aperta, riprovo…"
                       : "il server sa che sei pronto: aspetta che la lobby si riempia", _sub);

        var row = new Rect(r.x, r.y + 92f * s, r.width, 56f * s);

        if (mute)
        {
            var bar = new Rect(r.x, r.y + 152f * s, r.width, 5f * s);
            UiTheme.Fill(bar, UiTheme.Line);
            float sweep = Mathf.Repeat(Time.time * 0.5f, 1f);
            UiTheme.Fill(new Rect(bar.x + bar.width * sweep * 0.7f, bar.y, bar.width * 0.3f, bar.height),
                         UiTheme.Accent);
        }
        else
        {
            float barW = r.width;
            var bar = new Rect(r.x, r.y + 152f * s, barW, 5f * s);
            UiTheme.Fill(bar, UiTheme.Line);
            UiTheme.Fill(new Rect(bar.x, bar.y, barW * Mathf.Clamp01(have / (float)need), bar.height), UiTheme.Accent);
        }

        // il conteggio a sinistra e i puntini che pulsano a destra: prima i puntini
        // finivano attaccati al numero ("1 / 3.."), che sembrava un refuso
        GUI.Label(row, mute ? "—" : $"{have} / {need}", _title);
        int dots = Mathf.FloorToInt(Time.time * 2f) % 3 + 1;
        GUI.Label(row, new string('·', dots), _dots);

        GUI.Label(new Rect(r.x, r.yMax - 22f * s, r.width, 20f * s),
                  mute ? "Esc per la pausa · controlla che il server sia acceso"
                       : "Esc per la pausa · si parte appena c'è posto per tutti", _hint);
    }

    private void DrawFound(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y + 34f * s, r.width, 52f * s), "MATCH FOUND", _title);
        GUI.Label(new Rect(r.x, r.y + 96f * s, r.width, 22f * s),
                  "lobby completa · si passa al brief", _sub);
        GUI.Label(new Rect(r.x, r.y + 130f * s, r.width, 22f * s),
                  "scegli il tuo operatore", _hint);
    }

    private void EnsureStyles()
    {
        UiTheme.Ensure();
        if (_title != null && Mathf.Approximately(_stylesFor, UiTheme.S)) return;
        _stylesFor = UiTheme.S;
        float s = UiTheme.S;

        _title = new GUIStyle(UiTheme.H1) { fontSize = Mathf.RoundToInt(32 * s) };
        _title.normal.textColor = UiTheme.Text;

        _sub = new GUIStyle(UiTheme.Small) { fontSize = Mathf.RoundToInt(13 * s), wordWrap = true };
        _sub.normal.textColor = UiTheme.Dim;

        _hint = new GUIStyle(UiTheme.Small) { fontSize = Mathf.RoundToInt(11 * s) };
        _hint.normal.textColor = UiTheme.Faint;

        _dots = new GUIStyle(_title) { alignment = TextAnchor.MiddleRight };
        _dots.normal.textColor = UiTheme.Accent;
    }
}
