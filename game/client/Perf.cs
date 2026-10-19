using Godot;

/// <summary>
/// Performance in una riga. I numeri sono quelli del budget del doc 07:
/// draw call e primitivi sono i due che decidono se la GTX 970 regge.
/// </summary>
public static class Perf
{
    public static string Line()
    {
        double fps = Engine.GetFramesPerSecond();
        double frame = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double phys = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;
        double draws = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        double prims = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        double vram = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / (1024.0 * 1024.0);
        return $"fps     {fps,4:F0}\n" +
               $"frame   {frame,5:F2} ms\n" +
               $"fisica  {phys,5:F2} ms\n" +
               $"draw    {draws,4:F0}\n" +
               $"prim    {prims,7:F0}\n" +
               $"vram    {vram,5:F0} MB";
    }

    /// <summary>Versione compatta per l'HUD in partita.</summary>
    public static string Short()
    {
        double fps = Engine.GetFramesPerSecond();
        double frame = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double draws = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        return $"fps {fps:F0} · {frame:F2} ms · draw {draws:F0}";
    }
}
