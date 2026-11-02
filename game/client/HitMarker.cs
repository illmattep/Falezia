using Godot;

/// <summary>
/// Il segno del colpo a segno: quattro trattini attorno al mirino che lampeggiano.
/// Bianco quando prendi, rosso quando abbatti. Si spegne da solo in un decimo di secondo.
/// </summary>
public partial class HitMarker : Control
{
    private float _life;
    private bool _kill;
    private float _size = 9f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = 0; AnchorTop = 0; AnchorRight = 1; AnchorBottom = 1;
        OffsetLeft = 0; OffsetTop = 0; OffsetRight = 0; OffsetBottom = 0;
    }

    /// <summary>Un colpo a segno: <paramref name="kill"/> se ha abbattuto il bersaglio.</summary>
    public void Flash(bool kill)
    {
        _life = kill ? 0.22f : 0.13f;
        _kill = kill;
        _size = kill ? 13f : 9f;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_life <= 0f) return;
        _life -= (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_life <= 0f) return;
        var c = GetViewportRect().Size * 0.5f;
        var col = _kill ? new Color("#FF6B4A") : new Color("#F2F4F7");
        col.A = Mathf.Clamp(_life * 8f, 0f, 1f);

        float inner = 7f, outer = inner + _size;
        for (int i = 0; i < 4; i++)
        {
            float sx = i < 2 ? -1f : 1f;
            float sy = i % 2 == 0 ? -1f : 1f;
            DrawLine(c + new Vector2(sx * inner, sy * inner), c + new Vector2(sx * outer, sy * outer), col, 2f, true);
        }
    }
}
