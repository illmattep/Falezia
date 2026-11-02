using Unity.Profiling;
using UnityEngine;

/// <summary>
/// PRESTAZIONI, con F8.
///
/// Un pannello piccolo in alto a destra, sotto la minimappa, con quello che serve per
/// capire se il gioco sta nel budget del doc 07 (≤ 900 draw call, ≤ 1,2 M triangoli,
/// 60 fps su GTX 970):
/// - fps e tempo del fotogramma: medio, peggiore e "1% low" sugli ultimi 2 s, più il grafico;
/// - CPU (thread principale e di render) e GPU, da FrameTimingManager;
/// - draw call, SetPass, batch, triangoli e vertici, dai contatori del profiler;
/// - memoria e allocazioni per fotogramma (il GC che scatta è uno scatto in partita).
///
/// Alcuni contatori di render esistono solo nell'editor e nelle build "development":
/// dove mancano si vede un trattino, non un numero inventato.
/// Si installa da solo (non tocca la scena) e vive fra un caricamento e l'altro.
/// </summary>
public class PerfOverlay : MonoBehaviour
{
    public static bool Visible { get; private set; }

    private const int History = 240;                // ~2 s a 120 fps
    private readonly float[] _frames = new float[History];
    private int _head, _count;
    private float _avgMs, _worstMs, _lowMs, _fps;
    private float _refresh;

    private ProfilerRecorder _drawCalls, _setPass, _batches, _tris, _verts, _gcUsed, _sysUsed, _gcAlloc, _mainThread;
    private readonly FrameTiming[] _timing = new FrameTiming[1];
    private double _cpuMain, _cpuRender, _gpu;

    private Texture2D _white;
    private Font _font, _fontBold;
    private GUIStyle _label, _value, _title;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<PerfOverlay>() != null) return;
        var go = new GameObject("Prestazioni (F8)");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfOverlay>();
    }

    public static void Toggle() => Visible = !Visible;

    private void OnEnable()
    {
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _verts = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        _gcUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
        _sysUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);

        _white = Texture2D.whiteTexture;
        _font = Resources.Load<Font>("Fonts/BarlowCondensed-Medium");
        _fontBold = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
    }

    private void OnDisable()
    {
        _drawCalls.Dispose(); _setPass.Dispose(); _batches.Dispose(); _tris.Dispose(); _verts.Dispose();
        _gcUsed.Dispose(); _sysUsed.Dispose(); _gcAlloc.Dispose(); _mainThread.Dispose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8)) Toggle();

        float ms = Time.unscaledDeltaTime * 1000f;
        _frames[_head] = ms;
        _head = (_head + 1) % History;
        if (_count < History) _count++;

        if (!Visible) return;

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
        {
            _cpuMain = _timing[0].cpuMainThreadFrameTime;
            _cpuRender = _timing[0].cpuRenderThreadFrameTime;
            _gpu = _timing[0].gpuFrameTime;
        }

        // i numeri si aggiornano 4 volte al secondo: leggibili, non ballano
        _refresh -= Time.unscaledDeltaTime;
        if (_refresh > 0f) return;
        _refresh = 0.25f;

        float sum = 0f, worst = 0f;
        var sorted = new float[_count];
        for (int i = 0; i < _count; i++)
        {
            float f = _frames[i];
            sum += f;
            if (f > worst) worst = f;
            sorted[i] = f;
        }
        System.Array.Sort(sorted);
        _avgMs = _count > 0 ? sum / _count : 0f;
        _worstMs = worst;
        // "1% low": la media del peggior 1% dei fotogrammi, in fps
        int n = Mathf.Max(1, _count / 100);
        float low = 0f;
        for (int i = _count - n; i < _count; i++) low += sorted[Mathf.Max(0, i)];
        _lowMs = low / n;
        _fps = _avgMs > 0f ? 1000f / _avgMs : 0f;
    }

    private void OnGUI()
    {
        if (!Visible || Event.current.type != EventType.Repaint) return;
        GUI.depth = -10;   // sopra l'HUD

        float s = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.2f);
        EnsureStyles(s);

        float w = 270f * s, line = 17f * s, pad = 10f * s;
        int rows = 13;
        float graphH = 46f * s;
        float h = pad * 2 + 22f * s + rows * line + graphH + 8f * s;
        float x = Screen.width - 24f * s - w;
        float y = Mathf.Max(20f * s, Minimap.BottomY + 10f * s);

        Box(new Rect(x, y, w, h), new Color(0.04f, 0.05f, 0.07f, 0.78f));
        Box(new Rect(x, y, 2f * s, h), FpsColor(_fps));

        float cy = y + pad;
        GUI.Label(new Rect(x + pad, cy, w - pad * 2, 22f * s), $"{_fps:F0} FPS", _title);
        var right = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(x + pad, cy, w - pad * 2, 22f * s), "F8 chiude", right);
        cy += 24f * s;

        void Row(string k, string v)
        {
            GUI.Label(new Rect(x + pad, cy, w * 0.5f, line), k, _label);
            GUI.Label(new Rect(x + w * 0.42f, cy, w * 0.58f - pad, line), v, _value);
            cy += line;
        }

        Row("fotogramma", $"{_avgMs:F2} ms  ·  peggiore {_worstMs:F1}");
        Row("1% low", _lowMs > 0f ? $"{1000f / _lowMs:F0} fps ({_lowMs:F1} ms)" : "—");
        Row("CPU principale", Ms(_cpuMain, _mainThread.Valid ? _mainThread.LastValue * 1e-6 : 0));
        Row("CPU render", Ms(_cpuRender, 0));
        Row("GPU", Ms(_gpu, 0));
        Row("draw call", Count(_drawCalls) + "  ·  SetPass " + Count(_setPass));
        Row("batch", Count(_batches));
        Row("triangoli", Big(_tris) + "  ·  vertici " + Big(_verts));
        Row("memoria", Mb(_sysUsed, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()) + "  ·  GC " + Mb(_gcUsed, System.GC.GetTotalMemory(false)));
        Row("GC per fotogramma", _gcAlloc.Valid ? Kb(_gcAlloc.LastValue) : "—");
        Row("risoluzione", $"{Screen.width}×{Screen.height} · scala {GameSettings.RenderScale * 100f:F0}%");
        Row("v-sync · qualità", (QualitySettings.vSyncCount > 0 ? "on" : "off") + " · " +
            (QualitySettings.names.Length > 0 ? QualitySettings.names[QualitySettings.GetQualityLevel()] : "—"));
        Row("dettagli mappa", GameSettings.MapDetail ? "on" : "off");

        // grafico dei tempi: la riga gialla è 16,7 ms (60 fps)
        var g = new Rect(x + pad, cy + 6f * s, w - pad * 2, graphH);
        Box(g, new Color(1f, 1f, 1f, 0.05f));
        float scaleMs = 33.4f;
        int bars = Mathf.Min(_count, 120);
        float bw = g.width / 120f;
        for (int i = 0; i < bars; i++)
        {
            int idx = (_head - bars + i + History) % History;
            float f = _frames[idx];
            float bh = Mathf.Min(1f, f / scaleMs) * g.height;
            Box(new Rect(g.x + i * bw, g.yMax - bh, Mathf.Max(1f, bw - 0.5f), bh), FpsColor(1000f / Mathf.Max(0.01f, f)) * new Color(1, 1, 1, 0.85f));
        }
        Box(new Rect(g.x, g.yMax - g.height * 16.7f / scaleMs, g.width, 1f), new Color(0.95f, 0.76f, 0.19f, 0.8f));
    }

    private static string Ms(double frameTiming, double fallback)
    {
        double v = frameTiming > 0 ? frameTiming : fallback;
        return v > 0 ? $"{v:F2} ms" : "—";
    }

    private static string Count(ProfilerRecorder r) => r.Valid && r.LastValue > 0 ? r.LastValue.ToString("N0") : "—";

    private static string Big(ProfilerRecorder r)
    {
        if (!r.Valid || r.LastValue <= 0) return "—";
        long v = r.LastValue;
        return v >= 1_000_000 ? $"{v / 1e6:F2} M" : v >= 1000 ? $"{v / 1e3:F0} k" : v.ToString();
    }

    private static string Mb(ProfilerRecorder r, long fallback)
    {
        long v = r.Valid && r.LastValue > 0 ? r.LastValue : fallback;
        return $"{v / (1024f * 1024f):F0} MB";
    }

    private static string Kb(long bytes) => bytes <= 0 ? "0" : bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024f:F1} KB";

    private static Color FpsColor(float fps) =>
        fps >= 110f ? new Color(0.45f, 0.85f, 0.55f) :
        fps >= 58f ? new Color(0.86f, 0.89f, 0.93f) :
        fps >= 40f ? new Color(0.95f, 0.76f, 0.19f) : new Color(0.85f, 0.23f, 0.23f);

    private void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, _white);
        GUI.color = old;
    }

    private void EnsureStyles(float s)
    {
        if (_label != null && _label.fontSize == Mathf.RoundToInt(12f * s)) return;
        _label = new GUIStyle(GUI.skin.label) { font = _font, fontSize = Mathf.RoundToInt(12f * s), alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
        _label.normal.textColor = new Color(0.82f, 0.85f, 0.90f, 0.70f);
        _value = new GUIStyle(_label) { font = _fontBold, alignment = TextAnchor.MiddleRight };
        _value.normal.textColor = new Color(0.95f, 0.96f, 0.97f, 0.95f);
        _title = new GUIStyle(_label) { font = _fontBold, fontSize = Mathf.RoundToInt(19f * s) };
        _title.normal.textColor = Color.white;
    }
}
