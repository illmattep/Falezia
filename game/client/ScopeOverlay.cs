using Falezia.Sim;
using Godot;

/// <summary>
/// L'OTTICA, disegnata sopra il mondo: maschera nera col buco e reticolo per i
/// cannocchiali (Vantage 3×, Spotter 4×, Longshot 6×), puntino rosso per le armi
/// col dot (Cadence, Blitz). Tutto in un solo shader (`scope.gdshader`).
///
/// Il mirino normale si nasconde quando l'ottica è in faccia: il reticolo prende
/// il suo posto, altrimenti si vedrebbero due mirini sovrapposti.
/// </summary>
public partial class ScopeOverlay : Control
{
    private ColorRect _rect;
    private ShaderMaterial _mat;
    private float _shown;          // 0 = niente ottica, 1 = tutta
    private float _radius = 420f;
    private float _time;           // il reticolo "respira": due sinusoidi lente

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        GetViewport().SizeChanged += UpdateViewport;

        _rect = new ColorRect { Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore };
        _rect.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_rect);

        var shader = ResourceLoader.Exists("res://assets/shaders/scope.gdshader")
            ? GD.Load<Shader>("res://assets/shaders/scope.gdshader")
            : null;
        if (shader != null)
        {
            _mat = new ShaderMaterial { Shader = shader };
            _rect.Material = _mat;
        }

        UpdateViewport();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
    }

    private void UpdateViewport()
    {
        var size = GetViewportRect().Size;
        _radius = Mathf.Min(size.X, size.Y) * 0.42f;
        _mat?.SetShaderParameter("viewport", size);
        _radius = Mathf.Clamp(_radius, 120f, 700f);
    }

    /// <summary>
    /// Aggiorna l'ottica: <paramref name="adsT"/> è quanto sei in mira,
    /// <paramref name="spreadPx"/> allarga il vuoto del reticolo col cono vero.
    /// </summary>
    public void Set(float adsT, WeaponDef def, float spreadPx)
    {
        bool scope = def.Scoped;
        bool dot = def.DotSight;
        if (!scope && !dot)
        {
            _shown = 0f;
            Visible = false;
            return;
        }

        // la maschera entra solo a mira quasi completa: prima sarebbe un telo nero
        float target = scope ? Mathf.SmoothStep(0.55f, 0.92f, adsT) : Mathf.SmoothStep(0.35f, 0.75f, adsT);
        _shown = Mathf.MoveToward(_shown, target, 0.16f);
        Visible = _shown > 0.01f;
        if (!Visible || _mat == null) return;

        _mat.SetShaderParameter("alpha", _shown);
        // 5× e oltre: reticolo con le tacche di profondità (mildot), sotto: croce semplice
        _mat.SetShaderParameter("reticle", scope ? (def.Magnification >= 5f ? 2f : 1f) : 3f);
        _mat.SetShaderParameter("radius", _radius);
        _mat.SetShaderParameter("spread_px", scope ? Mathf.Clamp(spreadPx, 0f, 90f) : 0f);
        _mat.SetShaderParameter("viewport", GetViewportRect().Size);

        // il respiro: impercettibile a occhio, ma il reticolo non è incollato allo schermo
        float amp = scope ? 1.9f : 0.0f;
        var sway = new Vector2(
            (Mathf.Sin(_time * 1.13f) * 1.0f + Mathf.Sin(_time * 2.71f) * 0.35f) * amp,
            (Mathf.Cos(_time * 0.87f) * 0.8f + Mathf.Sin(_time * 3.10f) * 0.25f) * amp);
        _mat.SetShaderParameter("sway", sway);
    }
}
