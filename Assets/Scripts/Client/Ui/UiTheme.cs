using UnityEngine;

/// <summary>
/// IL LINGUAGGIO VISIVO DELL'INTERFACCIA.
///
/// Un posto solo per colori, caratteri, spaziature e controlli. Tutto quello che si vede
/// — menu, pausa, HUD, negozio, draft — passa da qui, così l'interfaccia è coerente e
/// cambiarla è cambiare un numero.
///
/// Stile: tattico, sobrio, un solo accento rosso. Il **colore saturo resta informazione di
/// gioco** (squadre, vita, danni): l'interfaccia intorno è Navy quasi nero, bianco al 90%,
/// righe da un pixel e mai un pannello pieno dove il gioco deve respirare.
/// </summary>
public static class UiTheme
{
    // ------------------------------------------------------------------ colori

    public static readonly Color Ink = new(0.035f, 0.047f, 0.067f, 1f);
    public static readonly Color InkSoft = new(0.055f, 0.070f, 0.094f, 1f);
    public static readonly Color PanelColor = new(0.055f, 0.070f, 0.094f, 0.88f);
    public static readonly Color PanelLight = new(0.10f, 0.125f, 0.16f, 0.92f);
    public static readonly Color Line = new(1f, 1f, 1f, 0.10f);
    public static readonly Color LineStrong = new(1f, 1f, 1f, 0.22f);

    public static readonly Color Text = new(0.93f, 0.95f, 0.97f, 0.95f);
    public static readonly Color Dim = new(0.62f, 0.68f, 0.75f, 0.85f);
    public static readonly Color Faint = new(0.62f, 0.68f, 0.75f, 0.45f);

    /// <summary>L'unico accento: il rosso di Falezia. Sui bottoni primari, sulla selezione, sui numeri critici.</summary>
    public static readonly Color Accent = new(1f, 0.275f, 0.333f, 1f);
    public static readonly Color AccentDim = new(1f, 0.275f, 0.333f, 0.16f);
    /// <summary>Il secondo colore esiste solo per l'informazione neutra (timbri, link, dati).</summary>
    public static readonly Color Cool = new(0.35f, 0.80f, 0.92f, 1f);
    public static readonly Color Good = new(0.24f, 0.82f, 0.52f, 1f);
    public static readonly Color Bad = new(1f, 0.38f, 0.38f, 1f);

    // ------------------------------------------------------------------ caratteri

    public static Font FontBody { get; private set; }
    public static Font FontBodyBold { get; private set; }
    public static Font FontCond { get; private set; }
    public static Font FontCondBold { get; private set; }

    /// <summary>Scala tipografica: tutto è pensato su 1080p e cresce con l'altezza.</summary>
    public static float S { get; private set; } = 1f;

    public static GUIStyle H1, H2, H3, Body, Small, Tiny, LabelStyle, Center, Right;
    public static GUIStyle Btn, BtnPrimary, BtnGhost, BtnHover, BtnDisabled;
    public static GUIStyle Nav, NavOn, NavHover, Section;

    private static Texture2D _white;
    private static float _built = -1f;

    public static Texture2D White
    {
        get
        {
            if (_white != null) return _white;
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            return _white;
        }
    }

    /// <summary>Va chiamata all'inizio di ogni OnGUI: ricostruisce solo quando cambia la scala.</summary>
    public static void Ensure()
    {
        float s = Mathf.Clamp(Screen.height / 1080f, 0.70f, 2.4f);
        if (H1 != null && Mathf.Abs(s - _built) < 0.005f) return;

        _built = s;
        S = s;

        if (FontCondBold == null) LoadFonts();

        H1 = Style(FontCondBold, 56, Text);
        H2 = Style(FontCondBold, 30, Text);
        H3 = Style(FontCondBold, 20, Text);
        Body = Style(FontBodyBold, 15, Text);
        Small = Style(FontBody, 13, Dim);
        Tiny = Style(FontBody, 11, Faint);
        LabelStyle = Style(FontBodyBold, 12, Text);
        Center = Style(FontCondBold, 18, Text, TextAnchor.MiddleCenter);
        Right = Style(FontBodyBold, 14, Text, TextAnchor.MiddleRight);

        Btn = Style(FontBodyBold, 14, Text, TextAnchor.MiddleCenter);
        BtnHover = Style(FontBodyBold, 14, Color.white, TextAnchor.MiddleCenter);
        BtnDisabled = Style(FontBodyBold, 14, Faint, TextAnchor.MiddleCenter);
        BtnPrimary = Style(FontBodyBold, 14, Color.white, TextAnchor.MiddleCenter);
        BtnGhost = Style(FontBody, 13, Dim, TextAnchor.MiddleCenter);
        Nav = Style(FontCondBold, 24, Dim, TextAnchor.MiddleLeft);
        NavOn = Style(FontCondBold, 24, Text, TextAnchor.MiddleLeft);
        NavHover = Style(FontCondBold, 24, Text, TextAnchor.MiddleLeft);
        Section = Style(FontBody, 11, new Color(Accent.r, Accent.g, Accent.b, 0.9f));
    }

    private static void LoadFonts()
    {
        FontBody = Resources.Load<Font>("Fonts/Barlow-Regular");
        FontBodyBold = Resources.Load<Font>("Fonts/Barlow-SemiBold");
        FontCond = Resources.Load<Font>("Fonts/BarlowCondensed-Medium");
        FontCondBold = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");

        // senza i font il gioco deve restare leggibile, non sparire
        FontBody ??= Font.CreateDynamicFontFromOSFont("Arial", 14);
        FontBodyBold ??= FontBody;
        FontCondBold ??= FontBodyBold;
        FontCond ??= FontBody;
    }

    private static GUIStyle Style(Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = Mathf.RoundToInt(size * S),
            alignment = align,
            richText = false,
            clipping = TextClipping.Clip,
            wordWrap = false
        };
        st.normal.textColor = color;
        return st;
    }

    // ------------------------------------------------------------------ forme

    public static void Fill(Rect r, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, White);
        GUI.color = prev;
    }

    public static void Border(Rect r, Color color, float w = 1f)
    {
        Fill(new Rect(r.x, r.y, r.width, w), color);
        Fill(new Rect(r.x, r.yMax - w, r.width, w), color);
        Fill(new Rect(r.x, r.y, w, r.height), color);
        Fill(new Rect(r.xMax - w, r.y, w, r.height), color);
    }

    /// <summary>Un pannello "di vetro": navy traslucido sopra il blur, riga da un pixel.</summary>
    public static void Panel(Rect r, float alpha = 0.88f)
    {
        Fill(r, new Color(PanelColor.r, PanelColor.g, PanelColor.b, alpha));
        Border(r, Line);
    }

    /// <summary>Il velo scuro che rende leggibile il testo sopra la scena sfocata.</summary>
    public static void Veil(float alpha = 0.55f)
        => Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.01f, 0.02f, 0.03f, alpha));

    // ------------------------------------------------------------------ controlli

    /// <summary>
    /// Bottone disegnato a mano (l'aspetto di quelli di Unity non si può piegare).
    /// Restituisce true al rilascio del mouse dentro il rettangolo.
    /// </summary>
    public static bool Button(Rect r, string text, bool primary = false, bool enabled = true)
    {
        var e = Event.current;
        bool hover = enabled && r.Contains(e.mousePosition);

        if (primary)
        {
            Fill(r, enabled ? (hover ? new Color(1f, 0.36f, 0.42f) : Accent) : new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            GUI.Label(r, text, BtnPrimary);
        }
        else
        {
            Fill(r, hover ? PanelLight : new Color(PanelColor.r, PanelColor.g, PanelColor.b, 0.55f));
            Border(r, hover ? LineStrong : Line);
            GUI.Label(r, text, !enabled ? BtnDisabled : hover ? BtnHover : Btn);
        }

        // barretta d'accento a sinistra sui bottoni primari: il dettaglio che li fa sembrare "Falezia"
        if (primary) Fill(new Rect(r.x, r.y, 3f * S, r.height), Color.white);

        return enabled && hover && e.type == EventType.MouseUp && e.button == 0
               ? Use(e)
               : false;
    }

    private static bool Use(Event e)
    {
        e.Use();
        return true;
    }

    /// <summary>Una voce di navigazione verticale: barra rossa quando è selezionata.</summary>
    public static bool NavItem(Rect r, string text, bool selected)
    {
        var e = Event.current;
        bool hover = r.Contains(e.mousePosition);

        if (selected)
        {
            Fill(new Rect(r.x - 14f * S, r.y + 4f * S, 3f * S, r.height - 8f * S), Accent);
            GUI.Label(r, text, NavOn);
        }
        else
        {
            GUI.Label(r, text, hover ? NavHover : Nav);
        }

        return hover && e.type == EventType.MouseUp && e.button == 0 ? Use(e) : false;
    }

    /// <summary>Barra di progresso sottile, senza angoli: quella dei tattici.</summary>
    public static void Bar(Rect r, float t, Color fill, Color? back = null)
    {
        Fill(r, back ?? new Color(0f, 0f, 0f, 0.35f));
        t = Mathf.Clamp01(t);
        if (t > 0f) Fill(new Rect(r.x, r.y, r.width * t, r.height), fill);
    }

    /// <summary>Testo con spaziatura larga, per i titoli: IMGUI non sa farlo, si mette il carattere.</summary>
    public static string Spaced(string text, string space = "\u2009")
        => string.Join(space, text.ToCharArray());

    public static void Divider(float x, float y, float width, float alpha = 0.10f)
        => Fill(new Rect(x, y, width, 1f), new Color(1f, 1f, 1f, alpha));

    /// <summary>Un timbro maiuscolo piccolo, per le etichette di sezione.</summary>
    public static void SectionLabel(Rect r, string text)
        => GUI.Label(r, Spaced(text, " "), Section);
}
