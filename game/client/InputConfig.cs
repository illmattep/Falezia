using Godot;

/// <summary>
/// Input dell'FPS. Un solo posto dove si decide come il mouse parla col gioco.
/// </summary>
public static class InputConfig
{
    /// <summary>
    /// Raw input: niente cursore, niente fusione degli eventi.
    /// `UseAccumulatedInput = false` fa arrivare OGNI movimento così com'è,
    /// invece di accorpare più eventi in uno solo: è ciò che serve alla mira.
    /// </summary>
    public static void FpsDefaults()
    {
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public static bool Captured => Input.MouseMode == Input.MouseModeEnum.Captured;

    public static void Capture() => Input.MouseMode = Input.MouseModeEnum.Captured;

    public static void Release() => Input.MouseMode = Input.MouseModeEnum.Visible;

    public static void Toggle()
    {
        if (Captured) Release();
        else Capture();
    }
}
