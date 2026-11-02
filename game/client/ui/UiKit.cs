using Falezia.Sim;
using Godot;

/// <summary>
/// IL KIT DELLA UI. Un solo posto per colori, misure, pannelli di vetro e controlli:
/// negozio, HUD, menu e impostazioni devono sembrare lo stesso gioco.
///
/// · **Scala**: tutto nasce su un disegno di 1600×900 e viene moltiplicato per la
///   scala del viewport (fra 0,75× e 1,5×), così la UI sta dentro lo schermo sia in
///   4:3 sia in 21:9, senza elementi tagliati.
/// · **Vetro**: i pannelli usano <see cref="GlassPanel"/>; con le impostazioni alte
///   sfocano la scena dietro, con quelle basse sono solo trasparenti.
/// </summary>
public static class UiKit
{
    // ---------------------------------------------------------------- colori
    public static readonly Color Ink = new(0.94f, 0.96f, 0.98f);
    public static readonly Color Dim = new(0.55f, 0.61f, 0.68f);
    public static readonly Color Accent = new(0.98f, 0.29f, 0.34f);   // corallo: selezione, acquisto
    public static readonly Color Cyan = new(0.36f, 0.79f, 0.85f);     // dati, barre
    public static readonly Color Good = new(0.52f, 0.86f, 0.62f);
    public static readonly Color Bad = new(0.95f, 0.45f, 0.40f);
    public static readonly Color Gold = new(0.95f, 0.80f, 0.45f);
    public static readonly Color Line = new(1f, 1f, 1f, 0.07f);

    public static readonly Color GlassDeep = new(0.030f, 0.048f, 0.065f, 0.88f);
    public static readonly Color GlassSoft = new(0.085f, 0.115f, 0.145f, 0.55f);
    public static readonly Color CardBg = new(0.085f, 0.115f, 0.145f, 0.72f);
    public static readonly Color CardSel = new(0.145f, 0.215f, 0.255f, 0.85f);

    /// <summary>Scala corrente del disegno (1 = 1600×900).</summary>
    public static float Scale = 1f;

    /// <summary>True se le impostazioni grafiche permettono il blur dietro i pannelli.</summary>
    public static bool Blur => GraphicsSettings.UiBlur;

    /// <summary>Aggiorna la scala dal viewport. Da chiamare all'apertura di un pannello.</summary>
    public static void UpdateScale(Viewport vp)
    {
        var size = vp?.GetVisibleRect().Size ?? new Vector2(1600, 900);
        Scale = Mathf.Clamp(Mathf.Min(size.X / 1600f, size.Y / 900f), 0.72f, 1.6f);
    }

    /// <summary>Un valore di disegno (px a 1600×900) nella scala corrente.</summary>
    public static int Px(float basePx) => Mathf.RoundToInt(basePx * Scale);
    public static float F(float basePx) => basePx * Scale;

    // ---------------------------------------------------------------- testo
    public static Label Text(string s, int size, Color c, bool bold = false)
    {
        var l = new Label { Text = s, MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeFontSizeOverride("font_size", Px(size));
        l.AddThemeColorOverride("font_color", c);
        if (bold) l.AddThemeConstantOverride("outline_size", 0);
        return l;
    }

    public static Label Title(string s, int size = 30) => Text(s, size, Ink);
    public static Label Body(string s, int size = 13) => Text(s, size, Ink);
    public static Label Caption(string s, int size = 11) => Text(s, size, Dim);
    public static Label Hint(string s, int size = 10) => Text(s, size, Dim);

    public static Label Tag(string s, Color c)
    {
        var l = Text(s.ToUpperInvariant(), 9, c);
        l.AddThemeConstantOverride("outline_size", 0);
        return l;
    }

    // ---------------------------------------------------------------- struttura
    public static Control Spacer() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    public static Control SpacerV() => new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };

    public static ColorRect Divider() => new() { Color = Line, CustomMinimumSize = new Vector2(0, 1) };

    /// <summary>Un pannello di vetro con dentro un margine e un contenitore verticale.</summary>
    public static VBoxContainer Panel(out GlassPanel glass, float radius = 16f, Color? tint = null,
                                      int pad = 18)
    {
        var t = tint ?? GlassDeep;
        // col blur la tinta deve lasciar passare lo sfondo (62%): con l'88% il vetro
        // sarebbe quasi nero e il blur non si vedrebbe
        if (Blur) t = new Color(t.R, t.G, t.B, Mathf.Min(t.A, 0.62f));
        else t = new Color(t.R, t.G, t.B, Mathf.Max(t.A, 0.93f));

        glass = new GlassPanel
        {
            Radius = F(radius),
            Blur = Blur ? 2.2f : 0f,
            Tint = t,
            BorderWidth = 1f
        };

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", Px(pad));
        margin.AddThemeConstantOverride("margin_right", Px(pad));
        margin.AddThemeConstantOverride("margin_top", Px(pad));
        margin.AddThemeConstantOverride("margin_bottom", Px(pad));
        glass.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", Px(8));
        margin.AddChild(box);

        glass.SetMeta("content", box);
        return box;
    }

    /// <summary>Un contenitore che porta il pannello di vetro e restituisce il contenuto.</summary>
    public static VBoxContainer PanelInto(Control parent, float radius = 16f, Color? tint = null,
                                          int pad = 18, Control.SizeFlags h = Control.SizeFlags.ExpandFill,
                                          Control.SizeFlags v = Control.SizeFlags.ExpandFill)
    {
        var holder = new Control { SizeFlagsHorizontal = h, SizeFlagsVertical = v };
        parent.AddChild(holder);
        var box = Panel(out var glass, radius, tint, pad);
        glass.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        holder.AddChild(glass);
        return box;
    }

    // ---------------------------------------------------------------- controlli
    public static Button Action(string text, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, Px(40)),
            FocusMode = Control.FocusModeEnum.None
        };
        b.AddThemeFontSizeOverride("font_size", Px(13));
        Style(b, primary);
        return b;
    }

    public static Button Tab(string text, bool on)
    {
        var b = new Button
        {
            Text = text,
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = on,
            CustomMinimumSize = new Vector2(0, Px(38)),
            FocusMode = Control.FocusModeEnum.None
        };
        b.AddThemeFontSizeOverride("font_size", Px(12));
        Style(b, false);
        return b;
    }

    /// <summary>Stile di un bottone: pieno per l'azione principale, vetro per il resto.</summary>
    public static void Style(Button b, bool primary)
    {
        var normal = Flat(primary ? Accent : new Color(1, 1, 1, 0.06f), Px(6),
                          primary ? Accent : new Color(1, 1, 1, 0.09f));
        var hover = Flat(primary ? new Color(1.0f, 0.42f, 0.46f, 1f) : new Color(1, 1, 1, 0.12f),
                         Px(6), primary ? Accent : new Color(1, 1, 1, 0.18f));
        var pressed = Flat(primary ? new Color(0.86f, 0.20f, 0.25f, 1f) : new Color(1, 1, 1, 0.18f),
                           Px(6), Accent);
        var disabled = Flat(new Color(1, 1, 1, 0.03f), Px(6), new Color(1, 1, 1, 0.05f));

        b.AddThemeStyleboxOverride("normal", normal);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", pressed);
        b.AddThemeStyleboxOverride("disabled", disabled);
        b.AddThemeColorOverride("font_color", primary ? new Color(0.12f, 0.03f, 0.04f) : Ink);
        b.AddThemeColorOverride("font_hover_color", primary ? new Color(0.10f, 0.02f, 0.03f) : Colors.White);
        b.AddThemeColorOverride("font_pressed_color", primary ? new Color(0.10f, 0.02f, 0.03f) : Ink);
        b.AddThemeColorOverride("font_disabled_color", new Color(1, 1, 1, 0.28f));
    }

    /// <summary>Pannello pieno della UI: fondo notturno, bordo sottile, angoli morbidi.</summary>
    public static StyleBoxFlat PanelStyle(float alpha = 0.97f)
    {
        var s = Flat(new Color(0.062f, 0.082f, 0.102f, alpha), Px(7), new Color(1, 1, 1, 0.07f));
        s.ContentMarginLeft = Px(12);
        s.ContentMarginRight = Px(12);
        s.ContentMarginTop = Px(10);
        s.ContentMarginBottom = Px(10);
        return s;
    }

    /// <summary>Scheda di un'arma: piena, con bordo d'accento quando è selezionata.</summary>
    public static StyleBoxFlat CardStyle(bool selected)
    {
        var s = Flat(selected ? new Color(0.115f, 0.165f, 0.200f, 1f) : new Color(0.080f, 0.102f, 0.125f, 1f),
                     Px(6), selected ? Accent : new Color(1, 1, 1, 0.06f), selected ? 2 : 1);
        s.ContentMarginLeft = Px(0);
        s.ContentMarginRight = Px(0);
        s.ContentMarginTop = Px(0);
        s.ContentMarginBottom = Px(0);
        return s;
    }

    public static StyleBoxFlat Flat(Color bg, int radius, Color border, int borderWidth = 1)
    {
        var s = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = Px(10),
            ContentMarginRight = Px(10),
            ContentMarginTop = Px(4),
            ContentMarginBottom = Px(4)
        };
        s.SetBorderWidthAll(borderWidth);
        return s;
    }

    /// <summary>Il tipo di fuoco in una parola, per schede, dettagli e HUD.</summary>
    public static string FireLabel(WeaponDef def)
        => def.Melee ? "mischia"
         : def.Modes == FireMode.Burst ? $"raffica ×{def.BurstCount}"
         : def.Automatic ? "automatica" : "semiautomatica";

    /// <summary>Una barra con etichetta e valore: la usano negozio e HUD.</summary>
    public static Control Stat(string label, float value, float max, string shown, Color? color = null)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", Px(2));

        var top = new HBoxContainer();
        top.AddChild(Caption(label, 10));
        top.AddChild(Spacer());
        top.AddChild(Text(shown, 11, Ink));
        box.AddChild(top);

        var track = new HBoxContainer { CustomMinimumSize = new Vector2(0, Px(5)) };
        float t = Mathf.Clamp(value / Mathf.Max(0.001f, max), 0f, 1f);
        var on = new ColorRect { Color = color ?? Cyan, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        on.SizeFlagsStretchRatio = Mathf.Max(0.02f, t);
        var off = new ColorRect
        {
            Color = new Color(1, 1, 1, 0.07f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        off.SizeFlagsStretchRatio = Mathf.Max(0.02f, 1f - t);
        track.AddChild(on);
        track.AddChild(off);
        box.AddChild(track);
        return box;
    }

    /// <summary>Riga "chiave · valore" per i dati che non hanno una barra.</summary>
    public static Control Row(string key, string value, Color? valueColor = null)
    {
        var row = new HBoxContainer();
        row.AddChild(Caption(key));
        row.AddChild(Spacer());
        row.AddChild(Text(value, 11, valueColor ?? Ink));
        return row;
    }

    /// <summary>Il testo di una statistica dell'arma, usato nelle schede del negozio.</summary>
    public static string StatsLine(WeaponDef def)
    {
        if (def.Melee) return $"{def.Damage} danni · mischia silenziosa";
        string shots = def.Pellets > 1 ? $"{def.ShotDamage}×{def.Pellets}" : $"{def.ShotDamage}";
        return $"{shots} danni · {def.RoundsPerMinute:F0} c/min\n" +
               $"{def.Magazine}/{def.Reserve} colpi · ric. {def.ReloadTactical:F1}/{def.ReloadEmpty:F1} s";
    }
}
