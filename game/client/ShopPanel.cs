using Falezia.Sim;
using Godot;

/// <summary>
/// LO SHOP (tasto B) — pannello unico di tutto il gioco.
///
/// Struttura, dal modello di Valorant:
///   · testata: titolo, regola, soldi;
///   · colonna CATEGORIE a sinistra (pistole, mitragliette, pompe, assalti, cecchini,
///     pesanti, mischia) con il riepilogo dell'arsenale in fondo;
///   · griglia di SCHEDE al centro: ritratto, nome, classe, danno/cadenza/ricarica, prezzo;
///   · DETTAGLI a destra: barre, tempi (mira, ricarica tattica e a vuoto), ottica,
///     tipo di fuoco, prezzo e pulsante d'acquisto.
///
/// Tutto è **pannelli pieni** con bordo sottile: leggibile sopra qualunque scena, senza
/// dipendere da shader o copie dello schermo. La scala segue il viewport, quindi la UI
/// sta dentro lo schermo in 4:3, 16:9 e 21:9.
/// </summary>
public partial class ShopPanel : Control
{
    private static ShopPanel _open;
    public static bool IsOpen => _open != null && IsInstanceValid(_open);

    private readonly LoadoutState _loadout;
    private readonly System.Action<WeaponSlot> _onEquip;
    private readonly System.Action _onClosed;

    private Label _money;
    private Label _feedback;
    private Label _loadoutLine;
    private GridContainer _grid;
    private VBoxContainer _detail;
    private readonly System.Collections.Generic.List<Button> _tabButtons = new();

    private WeaponSlot _held;
    private WeaponSlot? _selected;
    private int _category;

    private static readonly (string Name, string[] Ids)[] Categories =
    {
        ("PISTOLE", new[] { "standard", "hush", "chatter", "snub", "hammer" }),
        ("MITRAGLIETTE", new[] { "cadence", "scrap", "blitz" }),
        ("FUCILLI A POMPA", new[] { "choke", "sweeper" }),
        ("FUCILLI D'ASSALTO", new[] { "anvil", "service" }),
        ("CECCHINI", new[] { "vantage", "spotter", "longshot" }),
        ("ARMI PESANTI", new[] { "heavy" }),
        ("MISCHIA", new[] { "melee" }),
    };

    public static ShopPanel Open(Node parent, LoadoutState loadout, WeaponSlot held,
                                 System.Action<WeaponSlot> onEquip, System.Action onClosed = null)
    {
        if (IsOpen) { _open.Refresh(held); return _open; }
        var layer = new CanvasLayer { Layer = 40, Name = "Shop" };
        parent.AddChild(layer);
        var panel = new ShopPanel(loadout, onEquip, onClosed) { _held = held };
        layer.AddChild(panel);
        _open = panel;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        return panel;
    }

    public ShopPanel(LoadoutState loadout, System.Action<WeaponSlot> onEquip, System.Action onClosed)
    {
        _loadout = loadout;
        _onEquip = onEquip;
        _onClosed = onClosed;
    }

    public static void CloseIfOpen()
    {
        if (IsOpen) _open.Close();
    }

    public void Close()
    {
        if (_open == this) _open = null;
        var layer = GetParent();
        if (layer is CanvasLayer) layer.QueueFree();
        else QueueFree();
        _onClosed?.Invoke();
    }

    public override void _ExitTree()
    {
        if (_open == this) _open = null;
    }

    public override void _Ready()
    {
        // riempie lo schermo: ancore, dimensione esplicita E dimensione minima.
        // Con le sole ancore, in alcune configurazioni il pannello restava 0×0 e si
        // vedevano solo i testi (i ColorRect non hanno dimensione propria): il minimo
        // pari allo schermo rende impossibile quel caso.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var vp = GetViewportRect().Size;
        CustomMinimumSize = vp;
        Size = vp;
        MouseFilter = MouseFilterEnum.Stop;

        UiKit.UpdateScale(GetViewport());
        Build();
        _selected = _held;
        RebuildGrid();
        ShowDetail(_held);
        Refresh(_held);
    }

    public override void _Process(double delta)
    {
        // rete di sicurezza: se il viewport cambia (finestra ridimensionata) il pannello
        // lo segue anche quando il layout ad ancore non viene ricalcolato
        var vp = GetViewportRect().Size;
        if (Size != vp) { CustomMinimumSize = vp; Size = vp; }
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.B || k.Keycode == Key.Escape))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    // ------------------------------------------------------------------ costruzione

    private void Build()
    {
        var backdrop = new ColorRect { Color = new Color(0.030f, 0.042f, 0.055f, 0.96f) };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        int m = UiKit.Px(24);
        margin.AddThemeConstantOverride("margin_left", m);
        margin.AddThemeConstantOverride("margin_right", m);
        margin.AddThemeConstantOverride("margin_top", UiKit.Px(18));
        margin.AddThemeConstantOverride("margin_bottom", UiKit.Px(16));
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", UiKit.Px(12));
        margin.AddChild(root);

        root.AddChild(Header());
        root.AddChild(Body());
        root.AddChild(Footer());
    }

    private Control Header()
    {
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", UiKit.Px(14));

        var title = new VBoxContainer();
        title.AddThemeConstantOverride("separation", 0);
        title.AddChild(UiKit.Title("NEGOZIO", 32));
        title.AddChild(UiKit.Caption("un'arma per slot · quello che compri resta tuo fino alla fine del round", 11));
        head.AddChild(title);
        head.AddChild(UiKit.Spacer());

        var moneyBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        moneyBox.AddThemeConstantOverride("separation", 0);
        _money = UiKit.Text("$0", 30, UiKit.Gold);
        _money.HorizontalAlignment = HorizontalAlignment.Right;
        moneyBox.AddChild(_money);
        var hint = UiKit.Caption("B per chiudere", 10);
        hint.HorizontalAlignment = HorizontalAlignment.Right;
        moneyBox.AddChild(hint);
        head.AddChild(moneyBox);
        return head;
    }

    private Control Body()
    {
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", UiKit.Px(12));

        // ---- colonna categorie
        var cats = Panel(new Vector2(UiKit.Px(196), 0));
        _cats = cats.Box;
        _cats.AddChild(UiKit.Caption("CATEGORIE", 10));
        _tabButtons.Clear();
        for (int i = 0; i < Categories.Length; i++)
        {
            int index = i;
            var b = UiKit.Tab(Categories[i].Name, i == _category);
            b.CustomMinimumSize = new Vector2(0, UiKit.Px(34));
            b.Pressed += () =>
            {
                _category = index;
                RebuildGrid();
                RebuildTabs();
            };
            _tabButtons.Add(b);
            _cats.AddChild(b);
        }
        _cats.AddChild(UiKit.SpacerV());
        _cats.AddChild(UiKit.Divider());
        _loadoutLine = UiKit.Text("", 11, UiKit.Ink);
        _loadoutLine.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cats.AddChild(_loadoutLine);
        row.AddChild(cats.Root);

        // ---- griglia delle schede
        var center = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        center.AddThemeConstantOverride("separation", UiKit.Px(8));
        var head = new HBoxContainer();
        head.AddChild(UiKit.Caption("ARSENALE", 10));
        head.AddChild(UiKit.Spacer());
        head.AddChild(UiKit.Caption("click = dettagli · doppio click = acquista", 10));
        center.AddChild(head);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        center.AddChild(scroll);

        _grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _grid.AddThemeConstantOverride("h_separation", UiKit.Px(10));
        _grid.AddThemeConstantOverride("v_separation", UiKit.Px(10));
        scroll.AddChild(_grid);
        row.AddChild(center);

        // ---- pannello dettagli
        var detail = Panel(new Vector2(UiKit.Px(348), 0));
        _detail = detail.Box;
        row.AddChild(detail.Root);

        return row;
    }

    private VBoxContainer _cats;

    private (Control Root, VBoxContainer Box) Panel(Vector2 minSize)
    {
        var panel = new PanelContainer { CustomMinimumSize = minSize };
        panel.AddThemeStyleboxOverride("panel", UiKit.PanelStyle());
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", UiKit.Px(14));
        margin.AddThemeConstantOverride("margin_right", UiKit.Px(14));
        margin.AddThemeConstantOverride("margin_top", UiKit.Px(12));
        margin.AddThemeConstantOverride("margin_bottom", UiKit.Px(12));
        panel.AddChild(margin);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", UiKit.Px(6));
        margin.AddChild(box);
        return (panel, box);
    }

    private void RebuildTabs()
    {
        for (int i = 0; i < _tabButtons.Count; i++) _tabButtons[i].ButtonPressed = i == _category;
    }

    private Control Footer()
    {
        var foot = new HBoxContainer();
        foot.AddThemeConstantOverride("separation", UiKit.Px(14));
        _feedback = UiKit.Text("scegli una categoria, poi una scheda: a destra trovi i numeri e l'acquisto",
                               11, UiKit.Dim);
        _feedback.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _feedback.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foot.AddChild(_feedback);
        foot.AddChild(UiKit.Hint("1/2/3 armi · rotella · B negozio · F8 debug · F10 impostazioni", 10));
        return foot;
    }

    // ------------------------------------------------------------------ schede

    private void RebuildGrid()
    {
        if (_grid == null) return;
        foreach (var c in _grid.GetChildren())
        {
            _grid.RemoveChild(c);
            c.QueueFree();
        }

        foreach (var id in Categories[_category].Ids)
        {
            var slot = FindSlot(id);
            if (slot == null) continue;
            _grid.AddChild(Card(slot.Value));
        }
    }

    private static WeaponSlot? FindSlot(string id)
    {
        for (int i = 0; i < Arsenal.All.Length; i++)
            if (Arsenal.All[i].Id == id) return (WeaponSlot)i;
        return null;
    }

    private Control Card(WeaponSlot slot)
    {
        var def = Arsenal.Get(slot);
        bool owned = _loadout.Owns(slot);
        bool equipped = def.SlotKind != LoadoutSlot.Melee && _loadout.Get(def.SlotKind) == slot;
        bool selected = _selected.HasValue && _selected.Value == slot;

        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(UiKit.Px(208), UiKit.Px(172)),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop
        };
        card.AddThemeStyleboxOverride("panel", UiKit.CardStyle(selected));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", UiKit.Px(10));
        margin.AddThemeConstantOverride("margin_right", UiKit.Px(10));
        margin.AddThemeConstantOverride("margin_top", UiKit.Px(8));
        margin.AddThemeConstantOverride("margin_bottom", UiKit.Px(8));
        card.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", UiKit.Px(2));
        margin.AddChild(box);

        box.AddChild(Thumbnail(def, new Vector2(0, UiKit.Px(66))));

        var nameRow = new HBoxContainer();
        nameRow.AddChild(UiKit.Text(def.Name, 14, UiKit.Ink));
        nameRow.AddChild(UiKit.Spacer());
        if (equipped) nameRow.AddChild(UiKit.Tag("in mano", UiKit.Good));
        else if (owned) nameRow.AddChild(UiKit.Tag("tua", UiKit.Cyan));
        box.AddChild(nameRow);

        box.AddChild(UiKit.Text(def.Class.ToUpperInvariant(), 9, UiKit.Dim));

        var stats = UiKit.Text(UiKit.StatsLine(def), 9, UiKit.Dim);
        stats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(stats);

        var priceRow = new HBoxContainer();
        priceRow.AddChild(UiKit.Text(def.Price <= 0 ? "GRATIS" : "$" + def.Price.ToString("N0"), 14,
                                     owned ? UiKit.Dim : UiKit.Gold));
        priceRow.AddChild(UiKit.Spacer());
        if (!def.Melee) priceRow.AddChild(UiKit.Tag(UiKit.FireLabel(def), UiKit.Dim));
        box.AddChild(priceRow);

        foreach (var c in box.GetChildren()) IgnoreMouse(c);

        card.GuiInput += e =>
        {
            if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.DoubleClick) OnPick(slot);
                else
                {
                    _selected = slot;
                    ShowDetail(slot);
                    RebuildGrid();
                    Refresh(_held);
                }
            }
        };
        card.MouseEntered += () =>
        {
            _selected = slot;
            ShowDetail(slot);
        };
        return card;
    }

    private static void IgnoreMouse(Node n)
    {
        if (n is Control c) c.MouseFilter = MouseFilterEnum.Ignore;
        foreach (var child in n.GetChildren()) IgnoreMouse(child);
    }

    private TextureRect Thumbnail(WeaponDef def, Vector2 size)
    {
        var tex = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = size,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        try
        {
            string path = $"res://assets/weapons/ui/{def.Id}.png";
            if (ResourceLoader.Exists(path)) tex.Texture = GD.Load<Texture2D>(path);
        }
        catch { /* senza ritratto la scheda resta col solo testo */ }
        return tex;
    }

    // ------------------------------------------------------------------ dettagli

    private void ShowDetail(WeaponSlot slot)
    {
        if (_detail == null) return;
        foreach (var c in _detail.GetChildren())
        {
            _detail.RemoveChild(c);
            c.QueueFree();
        }

        var def = Arsenal.Get(slot);
        bool owned = _loadout.Owns(slot);
        bool equipped = def.SlotKind != LoadoutSlot.Melee && _loadout.Get(def.SlotKind) == slot;
        bool affordable = _loadout.Unlimited || _loadout.Money >= def.Price;

        _detail.AddChild(Thumbnail(def, new Vector2(0, UiKit.Px(92))));
        _detail.AddChild(UiKit.Title(def.Name.ToUpperInvariant(), 22));
        _detail.AddChild(UiKit.Caption(def.Class + $" · {UiKit.FireLabel(def)}", 11));
        _detail.AddChild(UiKit.Divider());

        if (def.Melee)
        {
            _detail.AddChild(UiKit.Stat("DANNO", def.Damage, 120f, $"{def.Damage}"));
            _detail.AddChild(UiKit.Row("PORTATA", "1,75 m"));
            _detail.AddChild(UiKit.Row("VELOCITÀ", "+8%"));
        }
        else
        {
            _detail.AddChild(UiKit.Stat("DANNO", def.ShotDamage, 100f,
                def.Pellets > 1 ? $"{def.ShotDamage} × {def.Pellets}" : $"{def.ShotDamage}"));
            _detail.AddChild(UiKit.Stat("CADENZA", def.RoundsPerMinute, 700f, $"{def.RoundsPerMinute:F0} c/min"));
            float precision = Mathf.Clamp(1f - def.SpreadAds / 2.5f, 0f, 1f);
            _detail.AddChild(UiKit.Stat("PRECISIONE", precision, 1f, $"{def.SpreadAds:F2}°"));
            _detail.AddChild(UiKit.Stat("CARICATORE", def.Magazine, 40f, $"{def.Magazine}/{def.Reserve}"));

            _detail.AddChild(UiKit.Divider());
            _detail.AddChild(UiKit.Row("TEMPO DI MIRA", $"{def.AdsTime:F2} s"));
            _detail.AddChild(UiKit.Row("RICARICA · tattica", $"{def.ReloadTactical:F2} s"));
            _detail.AddChild(UiKit.Row("RICARICA · a vuoto", $"{def.ReloadEmpty:F2} s"));
            _detail.AddChild(UiKit.Row("OTTICA", def.OpticName));
        }

        _detail.AddChild(UiKit.SpacerV());
        _detail.AddChild(UiKit.Divider());

        var priceRow = new HBoxContainer();
        priceRow.AddChild(UiKit.Text(def.Price <= 0 ? "GRATIS" : "$" + def.Price.ToString("N0"), 22, UiKit.Gold));
        priceRow.AddChild(UiKit.Spacer());
        if (equipped) priceRow.AddChild(UiKit.Tag("in mano", UiKit.Good));
        else if (owned) priceRow.AddChild(UiKit.Tag("già tua", UiKit.Cyan));
        _detail.AddChild(priceRow);

        string label;
        bool enabled = true;
        if (equipped) { label = "IN MANO"; enabled = false; }
        else if (owned) label = "EQUIPAGGIA";
        else if (def.Price <= 0) label = "PRENDI";
        else if (!affordable) { label = "SOLDI INSUFFICIENTI"; enabled = false; }
        else label = "ACQUISTA";

        var buy = UiKit.Action(label, true);
        buy.Disabled = !enabled;
        buy.Pressed += () => OnPick(slot);
        _detail.AddChild(buy);
    }

    // ------------------------------------------------------------------ acquisto

    public void Refresh(WeaponSlot held)
    {
        _held = held;
        if (_money == null) return;

        _money.Text = _loadout.Unlimited ? "∞" : "$" + _loadout.Money.ToString("N0");
        _money.AddThemeColorOverride("font_color", _loadout.Unlimited ? UiKit.Good : UiKit.Gold);

        string Name(WeaponSlot? s) => s.HasValue ? Arsenal.Get(s.Value).Name : "—";
        _loadoutLine.Text =
            $"IN MANO\n{Arsenal.Get(held).Name}\n\nPRIMARIA\n{Name(_loadout.Primary)}\n\nSECONDARIA\n{Name(_loadout.Secondary)}";
    }

    private void OnPick(WeaponSlot slot)
    {
        var result = _loadout.Buy(slot);
        var def = Arsenal.Get(slot);

        _feedback.Text = result switch
        {
            BuyResult.Bought => $"{def.Name} comprata · −${def.Price:N0}" +
                                (def.IsPrimary ? "  (la primaria che avevi viene consumata)"
                                               : "  (la secondaria che avevi viene consumata)"),
            BuyResult.Equipped => $"{def.Name} è già tua · messa in mano",
            BuyResult.NotEnough => $"non bastano i soldi per {def.Name} (${def.Price:N0})",
            BuyResult.IsMelee => "il coltello è sempre con te, non si compra",
            BuyResult.NotForSale => "questa arma non è in vendita",
            _ => "acquisto non possibile"
        };
        _feedback.AddThemeColorOverride("font_color",
            result == BuyResult.NotEnough ? UiKit.Bad
            : result == BuyResult.Bought ? UiKit.Good : UiKit.Dim);

        if (result is BuyResult.Bought or BuyResult.Equipped)
        {
            _onEquip?.Invoke(slot);
            _held = slot;
            _selected = slot;
        }
        RebuildGrid();
        ShowDetail(_selected ?? _held);
        Refresh(_held);
    }
}
