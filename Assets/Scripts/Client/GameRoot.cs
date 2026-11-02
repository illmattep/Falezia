using System.Collections;
using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// IL PUNTO DI PARTENZA (Unity). Mette in scena ciò che serve per giocare:
/// mappa dai volumi della simulazione, sole con ombre, giocatore in prima persona,
/// HUD e negozio. La simulazione è la STESSA del progetto Godot: `Falezia.Sim` è
/// C# puro, non sa niente del motore.
///
/// Uso: nuova scena vuota → GameObject vuoto → componente GameRoot → Play.
/// </summary>
public class GameRoot : MonoBehaviour
{
    public static WorldState World;
    public static SimState Sim;
    public const int PlayerId = 1;

    [Header("Mondo")]
    public string mapFile = "calanera.json";
    public bool useTestWorld = false;

    [Header("Mappa vestita (nuova pipeline)")]
    [Tooltip("Carica la scena Calanera (blockout + arte dello spawn) al posto del vecchio kit GLB.")]
    public bool useCalaneraScene = true;
    public string calaneraScene = "Calanera";

    [Header("Rete")]
    public bool online = false;
    public string host = "127.0.0.1";
    public int port = 27015;

    [Header("Luce")]
    public Color sunColor = new(1.00f, 0.90f, 0.74f);
    public float sunIntensity = 1.12f;

    public static PlayerRig Player;

    private NetClient _net;

    /// <summary>
    /// Collega questo client a un server già in esecuzione. La usa il menu (e prima
    /// <c>-faleziaOnline</c>): il giocatore esiste già, quindi la rete gli viene agganciata
    /// dopo. Se il server non risponde si resta in locale, senza bloccare niente.
    /// </summary>
    public void ConnectOnline(string serverHost, int serverPort)
    {
        if (_net == null) _net = gameObject.AddComponent<NetClient>();
        host = serverHost;
        port = serverPort;
        online = true;
        _net.Connect(serverHost, serverPort);
        if (Player != null) Player.AttachNetwork(_net);
    }

    private void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        Application.targetFrameRate = 0;

        GameSettings.Load();
        GameSettings.Apply();
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance = 90f;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowCascade4Split = new Vector3(0.08f, 0.22f, 0.48f);
        QualitySettings.antiAliasing = 4;

        // la mappa nuova è una scena: il vecchio kit GLB (props.json) non si carica più
        MapProps.Enabled = !useCalaneraScene;

        World = useTestWorld ? WorldState.MakeTestWorld() : UnityMapFiles.Load(mapFile);
        Sim = new SimState();

        var start = World.SpawnsNorth.Count > 0 ? World.SpawnsNorth[0] : World.FallbackFeet;
        Simulation.Spawn(Sim, PlayerId, start);
        FaceMapCenter(Sim.Get(PlayerId), start);

        if (useCalaneraScene)
        {
            yield return LoadCalaneraRoutine();
        }
        else
        {
            var mapRoot = MapBuilder.Build(World, transform);
            if (!MapBuilder.UseKit)
            {
                // sistemi procedurali del vecchio livello: con il kit non devono esistere
                MapBands.Build(World, mapRoot);
            }
        }
        BuildLight();
        if (!useCalaneraScene) BuildGround();

        ApplyCommandLine();

        if (online) ConnectOnline(host, port);
        NetClient net = _net;

        // il corpo degli operatori si carica in sottofondo: al primo avversario è già pronto
        OperatorView.Preload();

        var go = new GameObject("Giocatore");
        Player = go.AddComponent<PlayerRig>();
        Player.Init(Sim.Get(PlayerId), World, net);

        // controllo qualità degli operatori: tre manichini davanti alla camera, uno screenshot
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaOperatorProbe") >= 0)
            gameObject.AddComponent<OperatorProbe>();

        // giocando online si passa dalla coda: "in coda → match trovato → brief"
        if (online && Player != null) gameObject.AddComponent<MatchFlow>().Init(Player);

        if (PhotoMode.Directory() != null)
        {
            var photo = go.AddComponent<PhotoMode>();
            photo.Begin(Player);
        }
        else if (PhotoMode.CheckRequested)
        {
            var photo = go.AddComponent<PhotoMode>();
            photo.BeginCheck(Player);
        }
    }

    /// <summary>
    /// CARICA LA MAPPA NUOVA. La scena `Calanera` contiene il blockout (collider e volumi) e
    /// l'arte dello spawn; si aggiunge alla scena Main. La luce e il post della scena si spengono:
    /// li costruisce GameRoot, così l'atmosfera è una sola e la camera non cambia.
    /// </summary>
    private IEnumerator LoadCalaneraRoutine()
    {
        if (!Application.CanStreamedLevelBeLoaded(calaneraScene))
        {
            Debug.LogError($"GameRoot: la scena '{calaneraScene}' non è nei Build Settings");
            MapProps.Ready = true;
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(calaneraScene, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"GameRoot: load di '{calaneraScene}' rifiutato");
            MapProps.Ready = true;
            yield break;
        }
        while (!op.isDone) yield return null;

        // il nome della scena in build può non coincidere: prova nome, path e ultima caricata
        var sc = SceneManager.GetSceneByName(calaneraScene);
        if (!sc.IsValid()) sc = SceneManager.GetSceneByPath("Assets/Scenes/Calanera.unity");
        if (!sc.IsValid()) sc = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        if (!sc.IsValid() || !sc.isLoaded)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                names.Append(SceneManager.GetSceneAt(i).name).Append(' ');
            Debug.LogError($"GameRoot: '{calaneraScene}' non si è caricata (scene: {names})");
            MapProps.Ready = true;
            yield break;
        }

        int nodi = 0;
        foreach (var root in sc.GetRootGameObjects())
        {
            nodi++;
            if (root.name == "Sole" || root.name == "Post") root.SetActive(false);
            else if (root.name == "Spawn e punti") Destroy(root);
        }

        foreach (var b in Object.FindObjectsByType<BlockoutBox>(FindObjectsInactive.Exclude))
            if (b.kind == "team_north" || b.kind == "team_south")
            { var r0 = b.GetComponent<MeshRenderer>(); if (r0 != null) r0.enabled = false; }

        // la pelle arriva con la scena: PhotoMode non deve aspettare il vecchio props.json
        MapProps.Ready = true;
        Debug.Log($"GameRoot: Calanera caricata dalla scena ({nodi} radici, collider del blockout attivi)");
    }

    /// <summary>
    /// `-faleziaOnline 127.0.0.1:27015` avvia il client già collegato al server dedicato,
    /// senza toccare la scena. Utile per le prove e per i lanci da script.
    /// </summary>
    private void ApplyCommandLine()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != "-faleziaOnline") continue;
            online = true;
            var parts = args[i + 1].Split(':');
            host = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out var p)) port = p;
        }
    }

    /// <summary>
    /// Guarda verso il centro della mappa invece che verso il muro alle spalle.
    /// Convenzione della simulazione: yaw 0 → avanti = -Z, quindi yaw = atan2(-dx, -dz).
    /// </summary>
    private static void FaceMapCenter(PlayerState p, Vec3 from)
    {
        var center = MapCenter();
        var d = new Vec3(center.X - from.X, 0f, center.Z - from.Z);
        if (d.LengthSquared() < 0.01f) return;
        p.Yaw = (float)System.Math.Atan2(-d.X, -d.Z);
    }

    private static Vec3 MapCenter()
    {
        var sum = new Vec3(0f, 0f, 0f);
        int n = 0;
        foreach (var s in World.SpawnsNorth) { sum += s; n++; }
        foreach (var s in World.SpawnsSouth) { sum += s; n++; }
        return n == 0 ? new Vec3(0f, 0f, 0f) : sum / n;
    }

    private void BuildLight()
    {
        var sun = new GameObject("Sole").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.00f, 0.965f, 0.915f);
        sun.intensity = 0.84f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.92f;
        sun.shadowBias = 0.045f;
        sun.shadowNormalBias = 0.35f;
        // sole da ovest, più basso: ombre lunghe e facciate leggibili (CalaneraLook)
        sun.transform.rotation = Quaternion.Euler(33f, 103f, 0f);
        RenderSettings.sun = sun;

        // riempimento freddo senza ombre (stessa luce della scena, CalaneraLook)
        var fill = new GameObject("Riempimento");
        fill.transform.SetParent(sun.transform, false);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.62f, 0.72f, 0.86f);
        fl.intensity = 0.18f;
        fl.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(18f, -78f, 0f);

        // lens flare del sole: stesso asset del look della mappa
        var flare = Resources.Load<LensFlareDataSRP>("LensFlare/SunLensFlare");
        if (flare != null)
        {
            var lf = sun.gameObject.AddComponent<LensFlareComponentSRP>();
            lf.lensFlareData = flare;
            lf.intensity = 0.55f;
            lf.scale = 1f;
            lf.maxAttenuationDistance = 220f;
            lf.attenuationByLightShape = false;
            lf.useOcclusion = false;
            lf.allowOffScreen = false;
        }

        BuildSky();
        // ambiente a tre colori: cielo fresco, rimbalzo caldo del selciato, terra scura
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.50f, 0.60f, 0.73f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.62f, 0.59f);
        RenderSettings.ambientGroundColor = new Color(0.33f, 0.31f, 0.29f);
        RenderSettings.ambientIntensity = 0.70f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.735f, 0.775f, 0.805f);
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 360f;
        // riflessi: cielo ovunque, più intensi sulle armi e sui metalli
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 1.15f;

        BuildPostProcessing();
    }

    /// <summary>Cielo: panorama 4096×2048 della mappa (lo stesso file del client Godot).</summary>
    private void BuildSky()
    {
        // cielo procedurale con nuvole in movimento: nessuna texture, si anima da solo
        var shader = Shader.Find("Skybox/Falezia Nuvole");
        if (shader != null)
        {
            var mat = new Material(shader) { name = "Cielo Nuvole" };
            var sun = RenderSettings.sun;
            Vector3 toSun = sun != null ? -sun.transform.forward : new Vector3(0.55f, 0.42f, 0.2f);
            mat.SetVector("_SunDir", toSun.normalized);
            mat.SetColor("_SunColor", sunColor);
            mat.SetColor("_Horizon", new Color(0.745f, 0.815f, 0.875f));
            mat.SetColor("_Zenith", new Color(0.115f, 0.265f, 0.545f));
            mat.SetColor("_Ground", new Color(0.265f, 0.270f, 0.265f));
            mat.SetColor("_CloudLight", new Color(0.92f, 0.94f, 0.97f));
            mat.SetColor("_CloudDark", new Color(0.66f, 0.71f, 0.78f));
            // nuvole rade e grandi, bordo netto (CalaneraLook)
            mat.SetFloat("_Cover", 0.56f);
            mat.SetFloat("_Scale", 1.50f);
            mat.SetFloat("_Softness", 0.05f);
            mat.SetFloat("_Speed", 0.006f);
            RenderSettings.skybox = mat;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.60f, 0.73f);
            return;
        }

        var sky = Resources.Load<Material>("Sky/Cielo");
        if (sky == null)
        {
            Debug.LogWarning("GameRoot: nessun cielo disponibile");
            return;
        }
        RenderSettings.skybox = sky;
    }

    /// <summary>Volume globale con il profilo (tonemap ACES, bloom, vignetta).</summary>
    private void BuildPostProcessing()
    {
        var profile = Resources.Load<UnityEngine.Rendering.VolumeProfile>("Falezia-Volume");
        if (profile == null)
        {
            Debug.LogWarning("GameRoot: profilo volume assente (Assets/Resources/Falezia-Volume)");
            return;
        }
        Debug.Log("GameRoot: volume attivo, componenti = " + profile.components.Count);

        var go = new GameObject("Post");
        go.transform.SetParent(transform, false);
        var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    /// <summary>Pavimento d'appoggio: la mappa ha il suo, ma sotto il vuoto serve un fondo.</summary>
    private void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Mare";
        ground.transform.SetParent(transform, false);
        ground.transform.position = new Vector3(0f, -1.05f, 0f);
        ground.transform.localScale = new Vector3(60f, 1f, 60f);
        ground.GetComponent<Renderer>().sharedMaterial = Fx.Make(new Color(0.31f, 0.40f, 0.45f), 0.75f);
        Destroy(ground.GetComponent<Collider>());
    }
}
