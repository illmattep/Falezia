using Godot;

/// <summary>
/// Mirino. UNA classe, un profilo per classe d'arma.
/// Il centro si calcola dal RECT DEL VIEWPORT, non da Size: un Control figlio
/// di un CanvasLayer non viene disposto da un contenitore, quindi Size resta 0
/// e il mirino finirebbe in alto a sinistra.
/// </summary>
public partial class Crosshair : Control
{
    public struct Profile
    {
        public float Gap;
        public float Length;
        public float Thickness;
        public bool Dot;
        public float DotSize;
        public Color Color;
    }

    private Profile _p;
    private float _spread;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetViewport().SizeChanged += QueueRedraw;
        SetProfile("rifle");
    }

    public void SetProfile(string weaponClass)
    {
        _p = For(weaponClass);
        QueueRedraw();
    }

    public void SetSpread(float pixels)
    {
        if (Mathf.IsEqualApprox(_spread, pixels)) return;
        _spread = pixels;
        QueueRedraw();
    }

    /// <summary>I profili. Verranno spostati nel dato dell'arma (doc 11) quando esisterà.</summary>
    public static Profile For(string weaponClass)
    {
        var white = new Color(0.95f, 0.97f, 1f, 0.92f);
        var p = new Profile { Gap = 6f, Length = 10f, Thickness = 2f, Dot = false, DotSize = 2f, Color = white };
        switch (weaponClass)
        {
            case "pistol": p.Gap = 9f; p.Length = 7f; p.Dot = true; break;
            case "smg": p.Gap = 7f; p.Length = 7f; break;
            case "shotgun": p.Gap = 18f; p.Length = 6f; break;
            case "rifle": p.Gap = 6f; p.Length = 10f; break;
            case "sniper": p.Gap = 4f; p.Length = 15f; p.Thickness = 1f; p.Dot = true; break;
            case "knife": p.Gap = 12f; p.Length = 6f; break;
        }
        return p;
    }

    public override void _Draw()
    {
        // Verità assoluta: il centro dello schermo, in coordinate canvas.
        var c = GetViewportRect().Size * 0.5f;
        float g = _p.Gap + _spread;
        float t = _p.Thickness;
        var outline = new Color(0f, 0f, 0f, 0.78f);

        void Arm(Vector2 a, Vector2 b)
        {
            DrawLine(a, b, outline, t + 2f);
            DrawLine(a, b, _p.Color, t);
        }

        Arm(c + new Vector2(-g - _p.Length, 0f), c + new Vector2(-g, 0f));
        Arm(c + new Vector2(g, 0f), c + new Vector2(g + _p.Length, 0f));
        Arm(c + new Vector2(0f, -g - _p.Length), c + new Vector2(0f, -g));
        Arm(c + new Vector2(0f, g), c + new Vector2(0f, g + _p.Length));

        if (_p.Dot)
        {
            float h = _p.DotSize * 0.5f;
            DrawRect(new Rect2(c - new Vector2(h + 1f, h + 1f), new Vector2(_p.DotSize + 2f, _p.DotSize + 2f)), outline);
            DrawRect(new Rect2(c - new Vector2(h, h), new Vector2(_p.DotSize, _p.DotSize)), _p.Color);
        }
    }
}
