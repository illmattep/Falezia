using Godot;

/// <summary>
/// IL VETRO DELLA UI. Un rettangolo arrotondato che, con le impostazioni grafiche
/// alte, mostra la scena dietro **sfocata**, e con quelle basse è solo trasparente.
///
/// È il mattone di tutti i pannelli (negozio, HUD, menu): <see cref="UiKit.Panel"/>
/// lo crea già configurato, qui c'è solo il nodo che tiene aggiornati i parametri
/// dello shader (dimensione in pixel, raggio, blur) quando il pannello si ridimensiona.
/// </summary>
public partial class GlassPanel : ColorRect
{
    public float Radius = 12f;
    public float Blur = 2.2f;
    public Color Tint = new(0.035f, 0.055f, 0.075f, 0.86f);
    public float BorderWidth = 1f;
    public float Sheen = 0.05f;

    private ShaderMaterial _mat;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        if (ResourceLoader.Exists("res://assets/shaders/glass.gdshader"))
        {
            var shader = GD.Load<Shader>("res://assets/shaders/glass.gdshader");
            _mat = new ShaderMaterial { Shader = shader };
            Material = _mat;
        }

        Resized += Apply;
        Apply();
    }

    public void Apply()
    {
        if (_mat == null) return;
        _mat.SetShaderParameter("size", Size);
        _mat.SetShaderParameter("radius", Radius);
        _mat.SetShaderParameter("blur", Blur);
        _mat.SetShaderParameter("tint", Tint);
        _mat.SetShaderParameter("border_width", BorderWidth);
        _mat.SetShaderParameter("sheen", Sheen);
    }

    /// <summary>Raggio in pixel, aggiornato dopo un cambio di layout.</summary>
    public GlassPanel With(float radius, float blur, Color tint)
    {
        Radius = radius;
        Blur = blur;
        Tint = tint;
        Apply();
        return this;
    }
}
