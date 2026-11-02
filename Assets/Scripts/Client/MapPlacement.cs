using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// PIAZZARE I MODELLI DENTRO UNITY, col mouse.
///
/// Si avvia il gioco con <c>-faleziaPlace</c>: compare un pannello che lascia scegliere un
/// modello e piazzarlo col click sinistro dove guardi, sulla griglia da 50 cm. Le frecce
/// ruotano, [ ] cambiano modello, F9 apre/chiude il pannello, F10 salva in
/// <c>StreamingAssets/map/props.json</c> — lo stesso file che carica la mappa.
///
/// Perché così: il piazzamento automatico va bene per 1.300 oggetti, ma i ritocchi a mano
/// (una vetrina fuori posto, un lampione dentro un arco) si fanno guardando, non in un CSV.
/// </summary>
public sealed class MapPlacement : MonoBehaviour
{
    public static bool Active;

    private readonly List<MapProps.Item> _items = new();
    private string[] _models = new string[0];
    private int _model;
    private float _yaw;
    private string _status = "";

    private static string JsonPath =>
        Path.Combine(Application.streamingAssetsPath, "map", "props.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaPlace") < 0) return;
        var go = new GameObject("MapPlacement");
        DontDestroyOnLoad(go);
        go.AddComponent<MapPlacement>();
    }

    private void Start()
    {
        _models = CollectModels();
        Load();
        Active = true;
    }

    private static string[] CollectModels()
    {
        var found = new List<string>();
        string root = Path.Combine(Application.streamingAssetsPath, "map");
        if (!Directory.Exists(root)) return found.ToArray();

        foreach (var dir in Directory.GetDirectories(root))
        {
            string cat = Path.GetFileName(dir);
            foreach (var file in Directory.GetFiles(dir, "*.glb"))
                found.Add($"map/{cat}/{Path.GetFileNameWithoutExtension(file)}");
        }
        found.Sort();
        return found.ToArray();
    }

    private void Load()
    {
        _items.Clear();
        if (!File.Exists(JsonPath)) { _status = "props.json assente"; return; }

        try
        {
            var data = JsonUtility.FromJson<MapProps.Data>(File.ReadAllText(JsonPath));
            if (data?.items != null) _items.AddRange(data.items);
            _status = $"{_items.Count} oggetti caricati";
        }
        catch (System.Exception e) { _status = "lettura fallita: " + e.Message; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) Active = !Active;
        if (!Active) return;

        if (Input.GetKeyDown(KeyCode.LeftBracket)) _model = (_model - 1 + _models.Length) % _models.Length;
        if (Input.GetKeyDown(KeyCode.RightBracket)) _model = (_model + 1) % _models.Length;
        if (Input.GetKey(KeyCode.Q)) _yaw -= 45f * Time.unscaledDeltaTime * 4f;
        if (Input.GetKey(KeyCode.E)) _yaw += 45f * Time.unscaledDeltaTime * 4f;
        if (Input.GetKeyDown(KeyCode.R)) _yaw = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                var p = ray.GetPoint(dist);
                p.x = Mathf.Round(p.x * 2f) / 2f;
                p.z = Mathf.Round(p.z * 2f) / 2f;
                Place(p);
            }
        }

        if (Input.GetKeyDown(KeyCode.F10)) Save();
    }

    private void Place(Vector3 p)
    {
        if (_models.Length == 0) { _status = "nessun modello in StreamingAssets/map"; return; }

        var item = new MapProps.Item
        {
            model = _models[_model],
            x = p.x, y = 0f, z = p.z,
            rot = _yaw,
            scale = 1f
        };
        _items.Add(item);
        _status = $"piazzato {item.model} a {p.x:0.0}, {p.z:0.0}";
        _ = MapProps.SpawnOne(item);
    }

    private void Save()
    {
        var sb = new StringBuilder("{\"items\":[");
        for (int i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"model\":\"").Append(it.model).Append("\",")
              .Append("\"x\":").Append(it.x.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"y\":").Append(it.y.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"z\":").Append(it.z.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"rot\":").Append(it.rot.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"scale\":").Append(it.scale.ToString("0.##", CultureInfo.InvariantCulture))
              .Append('}');
        }
        sb.Append("]}");

        try
        {
            File.WriteAllText(JsonPath, sb.ToString());
            _status = $"salvati {_items.Count} oggetti";
        }
        catch (System.Exception e) { _status = "salvataggio fallito: " + e.Message; }
    }

    private void OnGUI()
    {
        if (!Active) return;

        float w = 420f, h = 120f;
        var box = new Rect(20, 20, w, h);
        GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.92f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;

        var st = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        st.normal.textColor = Color.white;

        string model = _models.Length > 0 ? _models[_model] : "—";
        GUI.Label(new Rect(box.x + 12, box.y + 10, box.width - 24, 20),
                  $"PIAZZAMENTO · modello {_model + 1}/{_models.Length}", st);
        GUI.Label(new Rect(box.x + 12, box.y + 32, box.width - 24, 20), model, st);
        GUI.Label(new Rect(box.x + 12, box.y + 54, box.width - 24, 20),
                  $"[ ] modello · Q/E ruota ({_yaw:0}°) · R azzera · click piazza · F10 salva", st);
        GUI.Label(new Rect(box.x + 12, box.y + 80, box.width - 24, 30), _status, st);
    }
}
