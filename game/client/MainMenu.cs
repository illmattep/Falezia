using Godot;

/// <summary>Menu principale. Ogni voce incompleta lo dice in faccia, senza vergogna.</summary>
public partial class MainMenu : Control
{
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GraphicsSettings.ApplyGlobal();
        BuildUi();
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.055f, 0.06f, 0.07f), AnchorsPreset = (int)LayoutPreset.FullRect };
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 80);
        margin.AddThemeConstantOverride("margin_top", 70);
        AddChild(margin);

        var col = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        margin.AddChild(col);

        var title = new Label { Text = "FALEZIA" };
        title.AddThemeFontSizeOverride("font_size", 64);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.93f, 0.95f));
        col.AddChild(title);

        var sub = new Label
        {
            Text = "tattico 5v5 · estrazione · round senza respawn",
            Modulate = new Color(0.55f, 0.6f, 0.66f)
        };
        col.AddChild(sub);

        var sign = new Label
        {
            Text = "dal latino falesia — con la Z, come sull'insegna del molo",
            Modulate = new Color(0.45f, 0.49f, 0.55f)
        };
        sign.AddThemeFontSizeOverride("font_size", 12);
        col.AddChild(sign);

        var banner = new Label
        {
            Text = "PROTOTIPO M0 — IN COSTRUZIONE",
            Modulate = new Color(0.95f, 0.72f, 0.3f)
        };
        banner.AddThemeFontSizeOverride("font_size", 18);
        col.AddChild(banner);

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 28) });

        col.AddChild(MenuButton("GIOCA  (online)", "collega al server dedicato · mappa Calanera", () => Go("res://client/client.tscn"), true));
        col.AddChild(MenuButton("STANZA DEBUG", "Calanera in graybox, teleport, cronometro, scenari", () => Go("res://client/debug_room.tscn"), true));
        col.AddChild(MenuButton("IMPOSTAZIONI", "grafica, schermo, latenza · anche in partita con F10", () => SettingsPanel.Open(this), true));
        col.AddChild(MenuButton("ARSENALE", "IN COSTRUZIONE — 15 armi già progettate", null, false));
        col.AddChild(MenuButton("ESCI", "", () => GetTree().Quit(), true));

        var spacer = new Control { CustomMinimumSize = new Vector2(0, 40) };
        col.AddChild(spacer);

        var footer = new Label
        {
            Text = "server: 127.0.0.1:27015   ·   build: M0 " + DateTime.Now.ToString("yyyy-MM-dd") +
                   "\nGodot 4.7.2 .NET · simulazione a passo fisso 64 Hz · predizione client",
            Modulate = new Color(0.4f, 0.44f, 0.5f)
        };
        col.AddChild(footer);
    }

    private Control MenuButton(string text, string note, Action action, bool enabled)
    {
        var row = new VBoxContainer();

        var b = new Button
        {
            Text = "  " + text,
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(480, 46),
            Disabled = !enabled,
            Flat = false
        };
        if (action != null) b.Pressed += action;
        row.AddChild(b);

        if (!string.IsNullOrEmpty(note))
        {
            var n = new Label
            {
                Text = "      " + note,
                Modulate = enabled ? new Color(0.5f, 0.55f, 0.6f) : new Color(0.65f, 0.5f, 0.3f)
            };
            n.AddThemeFontSizeOverride("font_size", 12);
            row.AddChild(n);
        }
        row.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        return row;
    }

    private void Go(string path) => GetTree().ChangeSceneToFile(path);
}
