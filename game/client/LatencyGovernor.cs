using Godot;

/// <summary>
/// BASSA LATENZA — ciò che si può fare in Godot senza NVIDIA Reflex.
///
/// Reflex riduce la latenza soprattutto in un modo: impedisce che la CPU prepari
/// fotogrammi più in fretta di quanto la GPU li consumi, perché quei fotogrammi in coda
/// sono input vecchi che aspettano di essere mostrati. Qui si ottiene lo stesso effetto
/// con un limite di FPS calcolato da solo:
///
///  * vsync attivo  → limite a (refresh − 3): si resta appena sotto il refresh e la coda
///                    del vsync non si riempie mai (è il trucco "auto-cap" dei driver);
///  * vsync spento  → se la GPU è il collo di bottiglia, limite al 94% di ciò che la GPU
///                    regge: la GPU non è mai satura e non si forma la coda.
///
/// Quello che NON si può fare senza l'SDK: il "just-in-time" dell'input dentro il driver
/// e il Reflex Boost. Il limite dell'utente, se più basso, vince sempre.
/// </summary>
public partial class LatencyGovernor : Node
{
    private const double Window = 0.5;   // secondi di media
    private double _acc;
    private double _gpuSum, _cpuSum;
    private int _frames;
    private int _applied = -1;
    private Rid _vp;

    public int CurrentCap => _applied;
    public double GpuMs { get; private set; }
    public double CpuMs { get; private set; }
    public string Mode { get; private set; } = "spento";

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _vp = GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(_vp, true);
        GraphicsSettings.Changed += OnChanged;
        OnChanged();
    }

    public override void _ExitTree()
    {
        GraphicsSettings.Changed -= OnChanged;
        RenderingServer.ViewportSetMeasureRenderTime(_vp, false);
        Engine.MaxFps = GraphicsSettings.MaxFps;   // la scena successiva riparte dal limite dell'utente
    }

    private void OnChanged()
    {
        _applied = -1;
        _gpuSum = _cpuSum = 0;
        _frames = 0;
        Apply(Target(0, 0));
    }

    public override void _Process(double delta)
    {
        if (!GraphicsSettings.LowLatency) return;

        _gpuSum += RenderingServer.ViewportGetMeasuredRenderTimeGpu(_vp);
        _cpuSum += RenderingServer.ViewportGetMeasuredRenderTimeCpu(_vp)
                 + Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        _frames++;
        _acc += delta;
        if (_acc < Window) return;

        GpuMs = _gpuSum / _frames;
        CpuMs = _cpuSum / _frames;
        _acc = 0;
        _gpuSum = _cpuSum = 0;
        _frames = 0;
        Apply(Target(GpuMs, CpuMs));
    }

    private int Target(double gpuMs, double cpuMs)
    {
        int user = GraphicsSettings.MaxFps;   // 0 = illimitato
        if (!GraphicsSettings.LowLatency)
        {
            Mode = "spento";
            return user;
        }

        int cap = 0;
        if (GraphicsSettings.VSync != 0)
        {
            int screen = DisplayServer.WindowGetCurrentScreen();
            double hz = DisplayServer.ScreenGetRefreshRate(screen);
            if (hz > 30) cap = (int)System.Math.Floor(hz) - 3;
            Mode = cap > 0 ? $"vsync −3 ({cap})" : "vsync (refresh ignoto)";
        }
        else if (gpuMs > 0.1 && gpuMs > cpuMs * 1.1)
        {
            // la GPU è il collo di bottiglia: le si lascia il 6% di margine
            cap = System.Math.Max(30, (int)(1000.0 / (gpuMs * 1.06)));
            Mode = $"GPU-bound ({cap})";
        }
        else
        {
            Mode = gpuMs > 0.1 ? "CPU-bound, nessun limite" : "misura GPU non disponibile";
        }

        if (user > 0 && (cap == 0 || user < cap)) return user;
        return cap;
    }

    private void Apply(int cap)
    {
        // isteresi: non inseguire il rumore della misura
        if (_applied >= 0 && System.Math.Abs(cap - _applied) <= 3 && (cap == 0) == (_applied == 0)) return;
        _applied = cap;
        Engine.MaxFps = cap;
    }
}
