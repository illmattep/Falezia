using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// MINIMAPPA, in alto a destra, stile Valorant.
///
/// - **Dentro un cerchio con il bordo**, su un fondo scuro velato: non galleggia sulla scena.
/// - **Fissa e verticale**: non gira con la camera. È ruotata (a scatti di 90°) in modo
///   che lo spawn della TUA squadra stia in basso: Nord e Sud vedono la mappa capovolta
///   l'una rispetto all'altra, e per entrambe "avanti" è verso l'alto.
/// - **Zoom 20 / 40 / 60 %** (Esc → GRAFICA → MINIMAPPA): la mappa si ingrandisce
///   attorno a te, che resti al centro del riquadro; i compagni fuori restano sul bordo.
/// - **Sfondo trasparente**: si vedono solo i corridoi e i rialzi, calcolati dalla
///   collisione vera (<see cref="MinimapBake"/>), non dalla grafica.
/// - **Scritte piccole**: la cassa, le estrazioni, gli spawn e i nomi delle zone.
/// - **Tu e i compagni**: tu con il cono della visuale, i compagni con la direzione,
///   i compagni a terra con una croce. Gli avversari non ci sono: la minimappa non dà
///   informazioni che non avresti (regole A4 e C4).
///
/// Disegnata in IMGUI come il resto dell'HUD, con gli stessi font.
/// </summary>
public class Minimap : MonoBehaviour
{
    /// <summary>Bordo inferiore della minimappa sullo schermo (px GUI): l'HUD ci mette sotto le prestazioni.</summary>
    public static float BottomY { get; private set; }

    // ---- misure su 1080p, scalate con l'altezza dello schermo come l'HUD
    private const float Diameter = 300f;     // il cerchio, su 1080p
    private const float RingWidth = 2.5f;    // spessore del bordo, su 1080p
    private const float MarginTop = 20f, MarginRight = 24f;
    private const float CropPad = 1.5f;    // metri attorno all'area calpestabile

    // ---- colori. Il saturo resta all'informazione di gioco (regola D4): squadre e obiettivo
    private static readonly Color SelfColor = new(1.00f, 0.86f, 0.30f, 1f);
    private static readonly Color TeamNorth = new(0.18f, 0.61f, 0.90f, 1f);   // team_north della mappa
    private static readonly Color TeamSouth = new(0.91f, 0.39f, 0.18f, 1f);   // team_south della mappa
    private static readonly Color Objective = new(0.95f, 0.76f, 0.19f, 1f);   // objective della mappa
    private static readonly Color Down = new(0.80f, 0.82f, 0.85f, 0.70f);
    private static readonly Color Ink = new(1f, 1f, 1f, 0.92f);
    private static readonly Color InkDim = new(0.90f, 0.92f, 0.95f, 0.62f);
    private static readonly Color Shadow = new(0f, 0f, 0f, 0.75f);

    private enum Kind { Box, Extraction, Spawn, Callout }

    /// <summary>
    /// Il JSON della mappa dà nome ai punti ma non il tipo: le estrazioni e gli spawn si
    /// riconoscono dal nome (glossario). La cassa invece è la scatola "objective".
    /// </summary>
    private static readonly Dictionary<string, Kind> KnownPoints = new()
    {
        { "Grotta del Levante", Kind.Extraction },
        { "Galleria Vecchia", Kind.Extraction },
        { "Porto Vecchio", Kind.Spawn },
        { "Forte San Verano", Kind.Spawn },
        { "Cassa", Kind.Box },   // punto di teleport accanto alla cassa: la cassa vera la dà la scatola
    };

    private struct Poi
    {
        public Kind Kind;
        public string Label;
        public Vector3 Pos;
        public int Order;
    }

    private PlayerRig _rig;
    private WorldState _world;
    private Texture2D _map, _arrow, _dot, _cone, _diamond, _hex, _cross;
    private Vector2 _cropCenter;        // centro del ritaglio nel mondo (X, Z)
    private Vector2 _cropSize;          // metri (X, Z)
    private readonly List<Poi> _pois = new();
    private readonly List<Rect> _placed = new();
    private Font _fCond, _fCondBold;
    private GUIStyle _label, _callout;
    private float _styleScale;

    // vista corrente
    private Vector2 _up = new(0f, 1f), _right = new(1f, 0f);   // direzioni del mondo (X, Z) che vanno in su e a destra
    private float _angle;           // rotazione della texture in GUI, gradi in senso orario
    private float _ppm;             // pixel per metro
    private Vector2 _screenCenter;  // centro del riquadro sullo schermo
    private Vector2 _viewCenter;    // punto del mondo (X, Z) al centro del riquadro: il giocatore
    private Rect _panel;            // quadrato che contiene il cerchio, sullo schermo
    private float _radiusPx;        // raggio del cerchio sullo schermo
    private Material _disc;         // disegna la mappa DENTRO il cerchio (IMGUI non ritaglia a cerchio)
    private float _rasterMinX, _rasterMinZ, _rasterW, _rasterH;   // estensione della texture, metri

    public void Init(PlayerRig rig, WorldState world)
    {
        // la minimappa non deve MAI rompere il giocatore: se qualcosa va storto si spegne e lo dice
        try { Build(rig, world); }
        catch (System.Exception e)
        {
            Debug.LogError("Minimap: disattivata, " + e);
            _map = null;
            enabled = false;
        }
    }

    private void Build(PlayerRig rig, WorldState world)
    {
        _rig = rig;
        _world = world;
        _fCond = Resources.Load<Font>("Fonts/BarlowCondensed-Medium");
        _fCondBold = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var r = MinimapBake.Bake(world);
        _map = new Texture2D(r.Width, r.Height, TextureFormat.RGBA32, true)
        {
            name = "Minimappa",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 0
        };
        // SetPixelData riempie SOLO il livello 0; i mipmap li calcola Apply.
        // (LoadRawTextureData voleva anche i mipmap e lanciava un'eccezione: minimappa invisibile)
        _map.SetPixelData(r.Rgba, 0);
        _map.Apply(true, true);
        _rasterMinX = r.MinX; _rasterMinZ = r.MinZ;
        _rasterW = r.Width * r.Cell; _rasterH = r.Height * r.Cell;

        float x0 = r.ReachMinX - CropPad, x1 = r.ReachMaxX + CropPad;
        float z0 = r.ReachMinZ - CropPad, z1 = r.ReachMaxZ + CropPad;
        _cropCenter = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
        _cropSize = new Vector2(x1 - x0, z1 - z0);
        Debug.Log($"Minimap: {r.Width}×{r.Height}, {r.WalkableCells} celle calpestabili, {sw.ElapsedMilliseconds} ms");

        _arrow = Icons.Arrow(64);
        _dot = Icons.Dot(48);
        _cone = Icons.Cone(128, 90f);
        _diamond = Icons.Diamond(32);
        _hex = Icons.Hex(48);
        _cross = Icons.Cross(32);

        var shader = Resources.Load<Shader>("Shaders/MinimapDisc");
        if (shader == null) shader = Shader.Find("Falezia/MinimapDisc");
        if (shader != null) _disc = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        else Debug.LogWarning("Minimap: shader Falezia/MinimapDisc assente, la mappa resta senza cerchio");

        CollectPois();
    }

    private void CollectPois()
    {
        _pois.Clear();
        int order = 0;

        // la cassa: il centro della scatola "objective"
        int objective = System.Array.IndexOf(_world.Kinds, "objective");
        if (objective >= 0)
            foreach (var b in _world.Boxes)
                if (b.Kind == objective)
                    _pois.Add(new Poi { Kind = Kind.Box, Label = "CASSA", Pos = ToUnity(b.Center), Order = order++ });

        foreach (var p in _world.Points)
        {
            var kind = KnownPoints.TryGetValue(p.Name, out var k) ? k : Kind.Callout;
            if (kind == Kind.Box) continue;
            _pois.Add(new Poi { Kind = kind, Label = p.Name.ToUpperInvariant(), Pos = ToUnity(p.Pos), Order = order++ });
        }

        // priorità nelle sovrapposizioni: cassa, estrazioni, spawn, poi i nomi delle zone
        _pois.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Order.CompareTo(b.Order));
    }

    private static Vector3 ToUnity(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    // ------------------------------------------------------------------ orientamento

    /// <summary>
    /// "In su" = dallo spawn della squadra verso il centro della mappa, agganciato
    /// all'asse più vicino: la mappa resta dritta, lo spawn finisce in basso.
    /// </summary>
    private void UpdateOrientation()
    {
        var spawns = _rig.State.Team == 2 ? _world.SpawnsSouth : _world.SpawnsNorth;
        var dir = new Vector2(0f, 1f);
        if (spawns.Count > 0)
        {
            var s = Vector2.zero;
            foreach (var p in spawns) s += new Vector2(p.X, p.Z);
            s /= spawns.Count;
            var d = _cropCenter - s;
            if (d.sqrMagnitude > 0.01f)
                dir = Mathf.Abs(d.x) > Mathf.Abs(d.y)
                    ? new Vector2(Mathf.Sign(d.x), 0f)
                    : new Vector2(0f, Mathf.Sign(d.y));
        }

        _up = dir;
        _right = new Vector2(dir.y, -dir.x);
        // senza rotazione la texture ha +Z in su; GUI ruota in senso orario
        _angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
    }

    /// <summary>Mondo (X, Z) → schermo GUI (y verso il basso).</summary>
    private Vector2 ToScreen(Vector3 world)
    {
        var d = new Vector2(world.x, world.z) - _viewCenter;
        return _screenCenter + new Vector2(Vector2.Dot(d, _right), -Vector2.Dot(d, _up)) * _ppm;
    }

    /// <summary>Gradi di imbardata Unity (asse Y, 0 = +Z) → rotazione GUI di un'icona che punta in su.</summary>
    private float IconAngle(float yawDegrees)
    {
        float a = yawDegrees * Mathf.Deg2Rad;
        var f = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        var s = new Vector2(Vector2.Dot(f, _right), -Vector2.Dot(f, _up));
        return Mathf.Atan2(s.x, -s.y) * Mathf.Rad2Deg;
    }

    // ------------------------------------------------------------------ disegno

    private void OnGUI()
    {
        if (MainMenu.Open) return;   // il menu principale ha la sua schermata
        if (_rig == null || _map == null || Event.current.type != EventType.Repaint) return;
        if (Hud.MenuOpen || Hud.PauseOpen) { BottomY = 0f; return; }

        // coda e brief: la partita non è ancora cominciata e la mappa non serve a nessuno
        if (DraftScreen.Open || MatchFlow.BlocksPlay) { BottomY = 0f; return; }

        GUI.depth = 2;   // sotto l'HUD
        float s = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.2f);
        EnsureStyles(s);
        UpdateOrientation();

        // un cerchio fisso; a zoom 0 ci starebbe tutta la mappa, lo zoom ingrandisce DENTRO il cerchio
        float d = Diameter * s;
        float right = Screen.width - MarginRight * s, top = MarginTop * s;
        _panel = new Rect(right - d, top, d, d);
        _screenCenter = _panel.center;
        _radiusPx = d * 0.5f;
        BottomY = top + d;

        float fit = d / Mathf.Max(_cropSize.x, _cropSize.y);
        _ppm = fit * GameSettings.MinimapZoomFactor;   // 20/40/60 %
        var me = _rig.transform.position;
        _viewCenter = new Vector2(me.x, me.z);         // tu sempre al centro

        if (_disc != null)
        {
            // tutto nello shader: sfondo scuro, mappa ruotata attorno a te, bordo. Fuori dal
            // cerchio niente. La texture è in Clamp con i bordi trasparenti: oltre la mappa, vuoto
            _disc.SetVector("_Center", new Vector4(_viewCenter.x, _viewCenter.y, 0f, 0f));
            _disc.SetVector("_Axes", new Vector4(_right.x, _right.y, _up.x, _up.y));
            _disc.SetFloat("_Radius", _radiusPx / _ppm);
            _disc.SetVector("_Raster", new Vector4(_rasterMinX, _rasterMinZ, _rasterW, _rasterH));
            _disc.SetFloat("_RingWidth", RingWidth * s / _radiusPx);
            Graphics.DrawTexture(_panel, _map, _disc);
        }
        else
        {
            // ripiego senza shader: la mappa nel quadrato, senza cerchio
            var texRect = new Rect(0, 0, d, d) { center = _screenCenter };
            float h = d * 0.5f / _ppm;
            var uv = new Rect((_viewCenter.x - h - _rasterMinX) / _rasterW, (_viewCenter.y - h - _rasterMinZ) / _rasterH,
                              h * 2f / _rasterW, h * 2f / _rasterH);
            var saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(_angle, _screenCenter);
            GUI.DrawTextureWithTexCoords(texRect, _map, uv, true);
            GUI.matrix = saved;
        }
                DrawPois(s);
        DrawPlayers(s);
    }

    private void EnsureStyles(float s)
    {
        if (_label != null && Mathf.Approximately(_styleScale, s)) return;
        _styleScale = s;
        _label = new GUIStyle(GUI.skin.label)
        {
            font = _fCondBold, fontSize = Mathf.RoundToInt(11f * s),
            alignment = TextAnchor.UpperCenter, wordWrap = false, clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0)
        };
        _callout = new GUIStyle(_label) { font = _fCond, fontSize = Mathf.RoundToInt(10f * s) };
    }

    private void DrawPois(float s)
    {
        _placed.Clear();
        int team = _rig.State.Team;

        foreach (var p in _pois)
        {
            var at = ToScreen(p.Pos);
            float icon = 0f;
            Texture2D tex = null;
            var tint = Ink;

            switch (p.Kind)
            {
                case Kind.Box: tex = _diamond; icon = 11f * s; tint = Objective; break;
                case Kind.Extraction: tex = _hex; icon = 15f * s; break;
                case Kind.Spawn:
                    tint = SpawnTeam(p.Pos) == team ? TeamColor(team) : InkDim;
                    break;
            }

            var style = p.Kind == Kind.Callout ? _callout : _label;
            var content = new GUIContent(p.Label);
            var size = style.CalcSize(content);
            float gap = tex != null ? icon * 0.5f + 1f * s : -size.y * 0.5f;
            var r = new Rect(at.x - size.x * 0.5f, at.y + gap, size.x, size.y);

            // con lo zoom: fuori dal riquadro non si disegna niente
            if (!InCircle(at, icon * 0.5f + 3f * s)) continue;
            bool labelFits = Inside(r);
            // i nomi delle zone cedono il posto a tutto il resto, e non escono dal riquadro
            if (p.Kind == Kind.Callout && (!labelFits || Overlaps(r))) continue;
            _placed.Add(r);

            if (tex != null)
            {
                var c = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(at.x - icon * 0.5f + 1f, at.y - icon * 0.5f + 1f, icon, icon), tex);
                GUI.color = tint;
                GUI.DrawTexture(new Rect(at.x - icon * 0.5f, at.y - icon * 0.5f, icon, icon), tex);
                GUI.color = c;
            }

            if (!labelFits) continue;   // l'icona resta, la scritta tagliata no
            var textColor = p.Kind == Kind.Callout ? InkDim : p.Kind == Kind.Spawn ? tint : Ink;
            ShadowLabel(r, content, style, textColor);
        }
    }

    /// <summary>A che squadra appartiene uno spawn con nome: quella con gli spawn più vicini.</summary>
    private int SpawnTeam(Vector3 pos)
    {
        float dn = Nearest(_world.SpawnsNorth, pos), ds = Nearest(_world.SpawnsSouth, pos);
        return dn <= ds ? 1 : 2;
    }

    private static float Nearest(List<System.Numerics.Vector3> list, Vector3 pos)
    {
        float best = float.MaxValue;
        foreach (var v in list)
            best = Mathf.Min(best, (new Vector2(v.X, v.Z) - new Vector2(pos.x, pos.z)).sqrMagnitude);
        return best;
    }

    private static Color TeamColor(int team) => team == 2 ? TeamSouth : TeamNorth;

    private bool InCircle(Vector2 at, float margin) => (at - _screenCenter).magnitude <= _radiusPx - margin;

    /// <summary>La scritta sta tutta dentro il cerchio, bordo escluso.</summary>
    private bool Inside(Rect r)
    {
        float m = 4f;
        return InCircle(new Vector2(r.xMin, r.yMin), m) && InCircle(new Vector2(r.xMax, r.yMin), m)
            && InCircle(new Vector2(r.xMin, r.yMax), m) && InCircle(new Vector2(r.xMax, r.yMax), m);
    }

    /// <summary>Un compagno fuori dal cerchio resta attaccato al bordo, dalla sua parte.</summary>
    private Vector2 ClampToPanel(Vector2 at, float inset)
    {
        var d = at - _screenCenter;
        float max = _radiusPx - inset;
        return d.magnitude > max ? _screenCenter + d.normalized * max : at;
    }

    private bool Overlaps(Rect r)
    {
        foreach (var o in _placed)
            if (o.Overlaps(r)) return true;
        return false;
    }

    private static void ShadowLabel(Rect r, GUIContent c, GUIStyle st, Color color)
    {
        st.normal.textColor = Shadow;
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), c, st);
        st.normal.textColor = color;
        GUI.Label(r, c, st);
    }

    private void DrawPlayers(float s)
    {
        var me = _rig.State;
        var snap = _rig.LastSnapshot;
        var remotes = _rig.Remotes;

        // compagni (solo online: offline non ce ne sono)
        if (snap != null)
        {
            foreach (var p in snap.State.Players.Values)
            {
                if (p.Id == _rig.LocalId || p.Team != me.Team) continue;

                Vector3 feet;
                float yaw;
                if (remotes == null || !remotes.TryGetPose(p.Id, out feet, out yaw))
                {
                    feet = new Vector3(p.Pos.X, p.Pos.Y, p.Pos.Z);
                    yaw = p.Yaw * Mathf.Rad2Deg + 180f;
                }

                var at = ClampToPanel(ToScreen(feet), 7f * s);
                if (!p.Alive)
                {
                    Icon(_cross, at, 10f * s, 0f, Down);
                    continue;
                }
                Icon(_dot, at, 13f * s, IconAngle(yaw), TeamColor(me.Team));
            }
        }

        // tu, sopra a tutti
        var self = _rig.transform;
        var pos = ToScreen(self.position);
        if (!me.Alive)
        {
            Icon(_cross, pos, 12f * s, 0f, Down);
            return;
        }
        float a = IconAngle(self.eulerAngles.y);
        float cone = 44f * s;
        var saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(a, pos);
        var c = GUI.color;
        GUI.color = new Color(SelfColor.r, SelfColor.g, SelfColor.b, 0.55f);
        GUI.DrawTexture(new Rect(pos.x - cone, pos.y - cone, cone * 2f, cone), _cone);
        GUI.color = c;
        GUI.matrix = saved;
        Icon(_arrow, pos, 17f * s, a, SelfColor);
    }

    private static void Icon(Texture2D tex, Vector2 at, float size, float angle, Color color)
    {
        var saved = GUI.matrix;
        if (angle != 0f) GUIUtility.RotateAroundPivot(angle, at);
        var c = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), tex);
        GUI.color = c;
        GUI.matrix = saved;
    }

    private void OnDestroy()
    {
        if (_disc != null) Destroy(_disc);
        foreach (var t in new[] { _map, _arrow, _dot, _cone, _diamond, _hex, _cross })
            if (t != null) Destroy(t);
    }

    // ------------------------------------------------------------------ icone

    /// <summary>
    /// Icone generate in codice (niente asset da importare). Bianco = parte che prende il
    /// colore, nero = bordo per leggerle sopra qualunque scena. Puntano tutte in su.
    /// </summary>
    private static class Icons
    {
        private static readonly Color Fill = Color.white;
        private static readonly Color Edge = new(0f, 0f, 0f, 0.85f);

        public static Texture2D Arrow(int n) => Make(n, n, (x, y) =>
        {
            // freccia: punta in alto, coda incavata
            if (InArrow(x, y, 0f)) return Fill;
            if (InArrow(x, y, 0.13f)) return Edge;
            return Color.clear;
        });

        private static bool InArrow(float x, float y, float g)
        {
            // triangolo (0, .85) (-.62, -.7) (.62, -.7) meno la tacca (0, -.3)
            float top = 0.85f + g, bottom = -0.70f - g, half = 0.62f + g;
            if (y > top || y < bottom) return false;
            float t = (top - y) / (top - bottom);
            if (Mathf.Abs(x) > half * t) return false;
            float notch = -0.30f - g * 0.5f;
            float notchY = bottom + (notch - bottom) * (1f - Mathf.Abs(x) / Mathf.Max(0.001f, half));
            return y >= notchY;
        }

        public static Texture2D Dot(int n) => Make(n, n, (x, y) =>
        {
            // cerchio con una punta corta che dice dove guarda
            float d = Mathf.Sqrt(x * x + (y + 0.12f) * (y + 0.12f));
            bool tip = y > 0.2f && y < 0.92f && Mathf.Abs(x) < (0.92f - y) * 0.55f;
            bool tipEdge = y > 0.1f && y < 1.0f && Mathf.Abs(x) < (1.0f - y) * 0.62f;
            if (d <= 0.58f || tip) return Fill;
            if (d <= 0.74f || tipEdge) return Edge;
            return Color.clear;
        });

        public static Texture2D Cone(int n, float fov) => Make(n * 2, n, (x, y) =>
        {
            // apice in basso al centro (y = -1), raggio 2 = altezza della texture
            float py = y + 1f;
            float d = Mathf.Sqrt(x * x + py * py) * 0.5f;
            float ang = Mathf.Abs(Mathf.Atan2(x, py) * Mathf.Rad2Deg);
            if (d > 1f || ang > fov * 0.5f) return Color.clear;
            float edge = Mathf.Clamp01((fov * 0.5f - ang) / 6f);
            return new Color(1f, 1f, 1f, (1f - d) * 0.75f * edge);
        }, aspectX: 2f);

        public static Texture2D Diamond(int n) => Make(n, n, (x, y) =>
        {
            float m = Mathf.Abs(x) + Mathf.Abs(y);
            if (m <= 0.72f) return Fill;
            if (m <= 0.98f) return Edge;
            return Color.clear;
        });

        public static Texture2D Hex(int n) => Make(n, n, (x, y) =>
        {
            // esagono vuoto con una freccia in su: "si esce da qui"
            float hx = Mathf.Max(Mathf.Abs(x) * 0.866f + Mathf.Abs(y) * 0.5f, Mathf.Abs(y));
            bool ring = hx <= 0.86f && hx >= 0.64f;
            bool ringEdge = hx <= 0.98f && hx >= 0.52f;
            bool shaft = Mathf.Abs(x) < 0.08f && y > -0.36f && y < 0.18f;
            bool head = y >= 0.06f && y <= 0.40f && Mathf.Abs(x) <= (0.40f - y) * 0.95f;
            if (ring || shaft || head) return Fill;
            bool shaftE = Mathf.Abs(x) < 0.17f && y > -0.45f && y < 0.18f;
            bool headE = y >= -0.04f && y <= 0.52f && Mathf.Abs(x) <= (0.52f - y) * 1.0f;
            if (ringEdge || shaftE || headE) return Edge;
            return Color.clear;
        });

        public static Texture2D Cross(int n) => Make(n, n, (x, y) =>
        {
            float a = Mathf.Abs(x - y) * 0.7071f, b = Mathf.Abs(x + y) * 0.7071f;
            bool inside = Mathf.Abs(x) < 0.8f && Mathf.Abs(y) < 0.8f;
            if (inside && (a < 0.13f || b < 0.13f)) return Fill;
            if (Mathf.Abs(x) < 0.95f && Mathf.Abs(y) < 0.95f && (a < 0.26f || b < 0.26f)) return Edge;
            return Color.clear;
        });

        /// <summary>Texture da una funzione su [-1, 1]² (y in su), con supersampling 4×4.</summary>
        private static Texture2D Make(int w, int h, System.Func<float, float, Color> shade, float aspectX = 1f)
        {
            const int ss = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };
            var px = new Color[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (i + (sx + 0.5f) / ss) / w * 2f - 1f;
                            float v = (j + (sy + 0.5f) / ss) / h * 2f - 1f;
                            var c = shade(u * aspectX, v);
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    px[j * w + i] = a > 0f ? new Color(r / a, g / a, b / a, a / (ss * ss)) : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }
    }
}
