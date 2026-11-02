using System.Collections.Generic;
using Falezia.Net;
using Falezia.Sim;
using UnityEngine;
using UnityEngine.Rendering;
#if FALEZIA_URP
using UnityEngine.Rendering.Universal;
#endif
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// IL GIOCATORE IN PRIMA PERSONA. Legge l'input, lo manda al server (se online) e lo
/// applica in locale per la predizione: la simulazione avanza a passi fissi di 1/64 s.
///
/// Online: lo snapshot del server corregge la posizione se lo scarto supera la soglia,
/// aggiorna vita/round e muove gli altri giocatori. Offline: tutto in locale (stanza
/// di prova), senza round.
/// </summary>
public class PlayerRig : MonoBehaviour
{
    private const float SnapDistance = 0.35f;

    private PlayerState _p;
    private WorldState _world;
    private Camera _cam;
    private Camera _viewmodelCamera;
    private CombatFx _fx;
    private Hud _hud;
    private NetClient _net;
    private RemotePlayers _remotes;
    private WeaponView _view;
    private WeaponAudio _audio;
    private int _seenLastShots;        // ultimo colpo del caricatore: suono diverso
    private int _seenDry;              // click a vuoto
    private bool _wasReloading;        // per fermare il suono se la ricarica si interrompe
    private ReflectionProbe _weaponProbe;  // riflessi locali su armi e mani
    private float _probeTimer;

    private double _acc;
    private float _yaw, _pitch;
    private bool _pendingFire;
    private int _seenShots, _seenReloads;
    private int _seenFxShots;          // contatore separato: lo consuma il viewmodel
    private int _tick;
    private int _localId = GameRoot.PlayerId;
    private float _adsFov = 60f;
    private float _deathTimer;

    // rinculo: la canna sale, poi la mira rientra esattamente dov'era
    private float _kickPitch, _kickYaw;
    private float _sinceShot = 99f;
    private const float KickHold = 0.10f;      // quanto resta su prima di rientrare
    private const float KickRecovery = 16f;    // gradi al secondo di rientro
    private ShellCasings _casings;
    private DroppedWeapons _ground;
    private OperatorView _selfBody;
    private bool _thirdPerson;
    private float _orbit;

    /// <summary>Debug in terza persona (Alt): la camera gira da sola attorno al giocatore.</summary>
    public bool ThirdPerson => _thirdPerson;

    public WeaponView View => _view;

    /// <summary>Soldi infiniti (stanza di prova): lo decide la simulazione, non l'HUD.</summary>
    public bool Unlimited => GameRoot.Sim.UnlimitedMoney;

    private static readonly List<GroundWeapon> NoDrops = new();

    /// <summary>
    /// Le armi a terra: offline sono quelle della simulazione locale, online arrivano
    /// dentro lo snapshot (il server è l'autorità anche su cosa c'è sul pavimento).
    /// </summary>
    public IReadOnlyList<GroundWeapon> Drops =>
        Online ? (LastSnapshot?.State.Drops ?? (IReadOnlyList<GroundWeapon>)NoDrops) : GameRoot.Sim.Drops;

    /// <summary>L'arma a terra più vicina, per il suggerimento "F raccogli".</summary>
    public GroundWeapon NearbyDrop
    {
        get
        {
            GroundWeapon best = null;
            float bestD = 4f;                       // 2 m
            var list = Drops;
            for (int i = 0; i < list.Count; i++)
            {
                var g = list[i];
                float dx = g.Pos.X - _p.Pos.X, dz = g.Pos.Z - _p.Pos.Z, dy = g.Pos.Y - _p.Pos.Y;
                float d = dx * dx + dz * dz + dy * dy * 0.35f;
                if (d < bestD) { bestD = d; best = g; }
            }
            return best;
        }
    }

    /// <summary>Modalità foto: tiene la mira premuta senza mouse.</summary>
    public bool PhotoAds;

    /// <summary>Modalità foto: tiene premuto il grilletto (per vedere rinculo e bossoli).</summary>
    public bool PhotoFire;

    /// <summary>Modalità foto: movimento imposto (x = laterale, y = avanti).</summary>
    [System.NonSerialized] public System.Numerics.Vector2? PhotoMove;

    /// <summary>Modalità foto: cambio d'arma senza tastiera.</summary>
    public void PhotoEquip(WeaponSlot slot)
    {
        if (Online) return;
        Simulation.Equip(GameRoot.Sim, _localId, slot);
        _view.Equip(slot);
        _audio.SetWeapon(Arsenal.Get(slot));
    }
    public PlayerState State => _p;
    /// <summary>Id del giocatore locale (lo assegna il server al primo snapshot).</summary>
    public int LocalId => _localId;
    /// <summary>Ultimo snapshot ricevuto (null offline): la minimappa ci legge i compagni.</summary>
    public Snapshot LastSnapshot { get; private set; }
    public RemotePlayers Remotes => _remotes;
    public MatchState Match { get; private set; } = new();
    public bool Online => _net != null && _net.Online;

    /// <summary>
    /// Secondi dall'ultimo snapshot del server (infinito se non ne è arrivato nessuno):
    /// serve a distinguere "sto aspettando la lobby" da "il server non risponde".
    /// </summary>
    public float SecondsSinceSnapshot => Online ? _net.SinceSnapshot : 0f;

    /// <summary>Aggancia la rete dopo l'avvio: lo usa il menu quando ti connetti a un server.</summary>
    public void AttachNetwork(NetClient net)
    {
        _net = net;
        // online i soldi non sono più infiniti: li decide il server
        if (GameRoot.Sim != null) GameRoot.Sim.UnlimitedMoney = false;
    }

    /// <summary>True se ho io il Nucleo: si cammina, niente sprint (doc 03).</summary>
    public bool CarryingCore => Match.Core == CorePhase.Carried && Match.CoreOwner == LocalId;
    public bool CanBuy => !Online || Match.Phase == RoundPhase.Buy;

    public void Init(PlayerState p, WorldState world, NetClient net)
    {
        _p = p;
        _world = world;
        _net = net;

        var camGo = new GameObject("Camera") { tag = "MainCamera" };
        camGo.transform.SetParent(transform, false);
        _cam = camGo.AddComponent<Camera>();
        if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
        _cam.fieldOfView = 85f;
        _cam.nearClipPlane = 0.02f;
        _cam.farClipPlane = 440f;
        EnablePostFx(_cam);
        BuildViewmodelCamera(camGo.transform);
        BuildWeaponProbe(camGo.transform);

        var viewGo = new GameObject("Arma");
        viewGo.transform.SetParent(camGo.transform, false);
        _view = viewGo.AddComponent<WeaponView>();
        _audio = viewGo.AddComponent<WeaponAudio>();
        _audio.Init();

        // le braccia dell'operatore in prima persona, con le mani sui punti dell'arma
        camGo.AddComponent<FirstPersonArms>().Init(this, _view);

        // offline è la stanza di prova: soldi infiniti e fase di acquisto sempre aperta.
        // Online decide il server, e i soldi sono quelli veri (cassa di squadra compresa).
        GameRoot.Sim.UnlimitedMoney = !Online;
        if (!Online)
        {
            Match.Phase = RoundPhase.Buy;
            Match.TimeLeft = 9999f;
            Match.Round = 1;
        }

        Simulation.Equip(GameRoot.Sim, _localId, WeaponSlot.Standard);
        _view.Equip(WeaponSlot.Standard);
        _audio.SetWeapon(Arsenal.Standard);
        _audio.Draw();

        _fx = gameObject.AddComponent<CombatFx>();
        _fx.Init();

        _casings = gameObject.AddComponent<ShellCasings>();
        _casings.Init(world);

        _remotes = gameObject.AddComponent<RemotePlayers>();

        // audio spaziale del mondo (spari e ricariche degli altri, armi che cadono)
        // il corpo del giocatore locale: si vede solo in terza persona (Alt).
        // Non è figlio di questo transform, che è la CAMERA: deve stare nel mondo.
        var selfGo = new GameObject("Corpo del giocatore");
        _selfBody = selfGo.AddComponent<OperatorView>();
        selfGo.SetActive(false);

        SpatialAudio.Ensure(transform);

        _ground = gameObject.AddComponent<DroppedWeapons>();
        _ground.Init(this);

        _hud = gameObject.AddComponent<Hud>();
        _hud.Init(this);

        gameObject.AddComponent<Minimap>().Init(this, world);

        _yaw = p.Yaw;
        _pitch = p.Pitch;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Riflessi dell'arma: sonda locale a 4 Hz, costo basso, niente cielo piatto sui metalli.
        if (_weaponProbe != null && Time.unscaledTime - _probeTimer > 0.25f)
        {
            _probeTimer = Time.unscaledTime;
            _weaponProbe.RenderProbe();
        }

        // anche il draft ferma l'input: si scegli col mouse, non si corre in giro
        bool ui = Hud.MenuOpen || Hud.PauseOpen || DraftScreen.Open || MainMenu.Open || MatchFlow.BlocksPlay;

        if (!ui && _p.Alive)
        {
            float sens = 2.2f * (_cam.fieldOfView / 85f) * 0.35f * (GameSettings.Sensitivity / 100f);
            _yaw += Input.GetAxisRaw("Mouse X") * sens;
            // mouse su = guardare in su: la convenzione della simulazione ha pitch > 0 verso l'alto
            _pitch = Mathf.Clamp(_pitch + Input.GetAxisRaw("Mouse Y") * sens, -85f, 85f);

            if (Input.GetMouseButtonDown(0)) _pendingFire = true;
            if (Input.GetKeyDown(KeyCode.G)) DropWeapon();       // lascia l'arma in mano
            if (Input.GetKeyDown(KeyCode.F)) PickupWeapon();     // raccogli quella a terra
            // 1 = la TUA primaria, 2 = la tua secondaria, 3 = il coltello.
            // Prima erano cablati su Anvil/Standard e la simulazione, non trovando l'arma in
            // cache, ne creava una nuova: premere 1 dopo aver comprato un fucile ti metteva
            // in mano un Anvil fantasma.
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);
        }

        if (Input.GetKeyDown(KeyCode.B) && CanBuy) Hud.ToggleMenu();
        if (Input.GetKeyDown(KeyCode.F7)) Hud.ToggleDebug();
        // F8 = prestazioni (lo gestisce PerfOverlay da solo); F3 resta per abitudine
        if (Input.GetKeyDown(KeyCode.F3)) PerfOverlay.Toggle();
        if (Input.GetKeyDown(KeyCode.Escape)) Hud.TogglePause();

        // ALT = terza persona di debug, con la camera che gira attorno al giocatore
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (alt != _thirdPerson) SetThirdPerson(alt);

        UpdateRecoil();

        var cmd = new InputCmd
        {
            Tick = ++_tick,
            Yaw = (_yaw + _kickYaw) * Mathf.Deg2Rad,
            Pitch = (_pitch + _kickPitch) * Mathf.Deg2Rad,
            Move = ui ? System.Numerics.Vector2.Zero : MoveInput(),
            Jump = !ui && Input.GetKey(KeyCode.Space),
            Sprint = !ui && Input.GetKey(KeyCode.LeftShift) && !CarryingCore,
            Crouch = !ui && Input.GetKey(KeyCode.LeftControl),
            Reload = !ui && Input.GetKey(KeyCode.R),
            Ads = !ui && (Input.GetMouseButton(1) || PhotoAds),
            Fire = !ui && (_pendingFire || Input.GetMouseButton(0) || PhotoFire),
            // E: aprire la cassa, prelevare il Nucleo, avviare l'estrazione, rimuoverlo
            Interact = !ui && Input.GetKey(KeyCode.E)
        };

        if (Online) _net.Send(cmd);

        _acc += Time.deltaTime;
        int guard = 0;
        while (_acc >= Constants.Dt && guard++ < 8)
        {
            _acc -= Constants.Dt;
            if (!_p.Alive) break;
            Simulation.ApplyInput(GameRoot.Sim, _localId, cmd, _world);
            Simulation.ApplyWeapon(GameRoot.Sim, _localId, cmd.Fire, cmd.Reload, cmd.Ads);
            if (!Online) Simulation.StepDrops(GameRoot.Sim, _world);   // online le muove il server
            _pendingFire = false;
        }

        if (Online) ApplySnapshot(_net.TakeLatest());

        if (_thirdPerson && _selfBody != null)
        {
            float halfH = (_p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
            _selfBody.transform.position = new Vector3(_p.Pos.X, _p.Pos.Y - halfH, _p.Pos.Z);
            _selfBody.transform.rotation = Quaternion.Euler(0f, _p.Yaw * Mathf.Rad2Deg + 180f, 0f);
            if (_selfBody.Ready) _selfBody.Apply(_p, Time.deltaTime);
        }

        PlaceCamera();
        UpdateView();
    }

    /// <summary>Lo stato del server corregge quello locale: posizione, vita, round, altri.</summary>
    private void ApplySnapshot(Snapshot snap)
    {
        if (snap == null) return;

        // il server assegna gli id: al primo snapshot il giocatore locale prende il SUO
        if (snap.YourId != 0 && snap.YourId != _localId)
        {
            GameRoot.Sim.Players.Remove(_localId);
            _localId = snap.YourId;
            var feet = snap.State.Players.TryGetValue(_localId, out var me0)
                ? new Vec3(me0.Pos.X, me0.Pos.Y - Constants.Height * 0.5f, me0.Pos.Z)
                : _world.FallbackFeet;
            Simulation.Spawn(GameRoot.Sim, _localId, feet);
            _p = GameRoot.Sim.Get(_localId);
        }

        Match = snap.Match;
        LastSnapshot = snap;

        if (snap.State.Players.TryGetValue(_net.MyId, out var mine))
        {
            var local = new Vector3(_p.Pos.X, _p.Pos.Y, _p.Pos.Z);
            var server = new Vector3(mine.Pos.X, mine.Pos.Y, mine.Pos.Z);
            if (Vector3.Distance(local, server) > SnapDistance)
            {
                _p.Pos = mine.Pos;
                _p.Vel = mine.Vel;
            }

            _p.Health = mine.Health;
            _p.Alive = mine.Alive;
            if (!mine.Alive) _deathTimer += Time.deltaTime; else _deathTimer = 0f;
            _p.Credits = mine.Credits;
            _p.Team = mine.Team;   // la squadra la decide il server (orienta la minimappa)

            // l'arma in pugno è quella che dice il server (acquisto autoritativo):
            // entra subito in mano, con zoom e voce giusti, appena arriva lo snapshot
            if (mine.Weapon.Slot != _p.Weapon.Slot)
            {
                Simulation.Equip(GameRoot.Sim, _localId, mine.Weapon.Slot);
                ApplyEquipped(mine.Weapon.Slot);
            }
        }

        _remotes.Apply(snap, _net.MyId);
    }

    /// <summary>
    /// Layer riservato all'arma in mano (creato da Falezia > Configura URP).
    /// </summary>
    public static int ViewmodelLayer { get; private set; } = -1;

    /// <summary>
    /// Seconda camera, impilata sulla principale, che disegna SOLO l'arma: cancella la
    /// profondità, quindi il fucile non sparisce più dentro i muri quando ci si avvicina.
    /// </summary>

    /// <summary>
    /// Sonda di riflessione locale per il viewmodel: le armi metalliche riflettono le facciate
    /// vicine, non solo il cielo. 128 px, 4 Hz, senza la capa ViewModel (niente auto-riflesso).
    /// </summary>
    private void BuildWeaponProbe(Transform parent)
    {
        var go = new GameObject("Riflessi Arma");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.size = new Vector3(16f, 10f, 16f);
        probe.resolution = 128;
        probe.hdr = true;
        probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        probe.cullingMask = ViewmodelLayer >= 0 ? ~(1 << ViewmodelLayer) : ~0;
        probe.nearClipPlane = 0.05f;
        probe.farClipPlane = 140f;
        probe.intensity = 1.0f;
        probe.importance = 1;
        _weaponProbe = probe;
    }

    private void BuildViewmodelCamera(Transform parent)
    {
        ViewmodelLayer = LayerMask.NameToLayer("ViewModel");
        if (ViewmodelLayer < 0) return;      // layer assente: si tiene la camera singola

        var go = new GameObject("CameraViewmodel") { layer = ViewmodelLayer };
        go.transform.SetParent(parent, false);

        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = _cam.fieldOfView;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 8f;
        cam.clearFlags = CameraClearFlags.Depth;
        cam.cullingMask = 1 << ViewmodelLayer;
        cam.depth = _cam.depth + 1;
        cam.allowHDR = true;
        cam.allowMSAA = true;

        _cam.cullingMask &= ~(1 << ViewmodelLayer);

#if FALEZIA_URP
        var vmData = cam.GetUniversalAdditionalCameraData();
        vmData.renderType = CameraRenderType.Overlay;
        vmData.renderPostProcessing = false;
        vmData.antialiasing = AntialiasingMode.None;
        vmData.renderShadows = false;
        _cam.GetUniversalAdditionalCameraData().cameraStack.Add(cam);
#endif
        _viewmodelCamera = cam;
    }

    /// <summary>
    /// La camera creata a runtime nasce con il post-processing SPENTO: senza questo
    /// il volume (ACES, bloom, vignetta) non si vede.
    /// </summary>
    private static void EnablePostFx(Camera cam)
    {
#if FALEZIA_URP
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        data.renderShadows = true;
        data.dithering = true;
        Debug.Log("PlayerRig: post-processing = " + data.renderPostProcessing + " · AA " + data.antialiasing);
#endif
    }

    /// <summary>
    /// Input di movimento.
    ///
    /// ATTENZIONE ALL'ASSE LATERALE: la simulazione viene da Godot, dove la camera guarda
    /// -Z e la destra è +X. In Unity la camera guarda +Z, quindi una camera che guarda -Z
    /// (yaw 0 della simulazione) ha la destra su -X: l'asse è specchiato. "W/S" (avanti e
    /// indietro, cioè l'asse Z) coincidono; "A/D" va invertito qui, una volta sola, invece
    /// di toccare la simulazione che è la stessa per client, server e test.
    /// </summary>
    private System.Numerics.Vector2 MoveInput()
    {
        if (PhotoMove.HasValue) return PhotoMove.Value;
        return MoveFor(Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.D),
                       Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));
    }

    /// <summary>La mappatura dei tasti, isolata per poterla verificare nei test.</summary>
    public static System.Numerics.Vector2 MoveFor(bool a, bool d, bool w, bool s)
    {
        float strafe = (a ? 1f : 0f) - (d ? 1f : 0f);
        float forward = (w ? 1f : 0f) - (s ? 1f : 0f);
        return new System.Numerics.Vector2(strafe, forward);
    }

    /// <summary>
    /// Spara N colpi dall'arma in pugno senza passare dall'input: serve alla modalità
    /// foto per verificare i fori sui muri e sul pavimento.
    /// </summary>
    public void TestFire(int shots)
    {
        for (int i = 0; i < shots; i++)
            _fx.Shoot(_p, _world, transform.position, _p.Yaw, _p.Pitch);
    }

    /// <summary>
    /// RINCULO. Ogni colpo aggiunge un impulso alla canna (su e di lato, meno in mira);
    /// per 100 ms la mira resta su, poi rientra verso il punto in cui stavi mirando.
    /// L'impulso viaggia dentro l'input, quindi il server vede la stessa mira del client.
    /// </summary>
    private void UpdateRecoil()
    {
        float dt = Time.deltaTime;

        if (_p.Weapon.Shots != _seenShots)
        {
            int fired = Mathf.Max(1, _p.Weapon.Shots - _seenShots);
            _seenShots = _p.Weapon.Shots;

            var def = Arsenal.Get(_p.Weapon.Slot);
            float ads = Mathf.Lerp(1f, 0.72f, _p.Weapon.AdsT);
            float up = def.RecoilUp * ads;
            float side = def.RecoilSide * ads * Random.Range(-1f, 1f);

            _kickPitch = Mathf.Min(18f, _kickPitch + up * Mathf.Min(fired, 3));
            _kickYaw += side * Mathf.Min(fired, 3);
            _sinceShot = 0f;

            if (_view != null) _view.Kick(up);
            if (_casings != null) _casings.Spawn(transform.position, transform.right, _p.Weapon.AdsT);
        }

        _sinceShot += dt;
        if (_sinceShot < KickHold) return;

        _kickPitch = Mathf.MoveTowards(_kickPitch, 0f, KickRecovery * dt);
        _kickYaw = Mathf.MoveTowards(_kickYaw, 0f, KickRecovery * 0.7f * dt);
    }

    /// <summary>
    /// Sposta il giocatore e lo fa guardare in una direzione precisa.
    /// Usato dalla modalità foto (controllo qualità della grafica).
    /// </summary>
    public void Teleport(Vec3 feet, float yawDegrees, float pitchDegrees)
    {
        // la simulazione tiene il CENTRO del giocatore: i piedi vanno convertiti
        float halfH = (_p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
        _p.Pos = new Vec3(feet.X, feet.Y + halfH, feet.Z);
        _p.Vel = System.Numerics.Vector3.Zero;
        _p.Yaw = yawDegrees * Mathf.Deg2Rad;
        _p.Pitch = Mathf.Clamp(pitchDegrees, -85f, 85f) * Mathf.Deg2Rad;
        _yaw = yawDegrees;
        _pitch = Mathf.Clamp(pitchDegrees, -85f, 85f);
        PlaceCamera();
    }

    /// <summary>Entra o esce dalla terza persona: corpo visibile, viewmodel nascosto, orbita azzerata.</summary>
    public void SetThirdPerson(bool on)
    {
        _thirdPerson = on;
        _orbit = 0f;
        if (_selfBody != null) _selfBody.gameObject.SetActive(on);
        if (_view != null) _view.gameObject.SetActive(!on);       // l'arma in mano è del viewmodel
    }

    private void PlaceCamera()
    {
        if (_thirdPerson)
        {
            // camera che gira: 30°/s attorno al giocatore, sempre rivolta al petto
            _orbit += Time.deltaTime * 30f;
            if (_orbit >= 360f) _orbit -= 360f;

            float hh = (_p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
            var centre = new Vector3(_p.Pos.X, _p.Pos.Y - hh + 1.05f, _p.Pos.Z);
            float ang = (_p.Yaw * Mathf.Rad2Deg + 180f + _orbit) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));

            transform.position = centre + dir * 3.4f + Vector3.up * 0.5f;
            transform.LookAt(centre);
            _cam.fieldOfView = GameSettings.Fov;
            if (_viewmodelCamera != null) _viewmodelCamera.fieldOfView = _cam.fieldOfView;
            return;
        }

        float halfH = (_p.Crouching ? Constants.CrouchHeight : Constants.Height) * 0.5f;
        float eyeY = _p.Pos.Y + halfH - 0.25f;
        if (!_p.Alive) eyeY -= Mathf.Min(0.9f, _deathTimer * 0.8f);   // la camera scende

        transform.position = new Vector3(_p.Pos.X, eyeY, _p.Pos.Z);
        transform.rotation = Quaternion.Euler(-_p.Pitch * Mathf.Rad2Deg, _p.Yaw * Mathf.Rad2Deg + 180f, 0f);
        float baseFov = GameSettings.Fov;
        _cam.fieldOfView = Mathf.Lerp(baseFov, _adsFov * (baseFov / 85f), _p.Weapon.AdsT);
        if (_viewmodelCamera != null) _viewmodelCamera.fieldOfView = _cam.fieldOfView;
    }

    private void UpdateView()
    {
        _view.SetAds(_p.Weapon.AdsT);

        if (_p.Weapon.Shots != _seenFxShots)
        {
            _seenFxShots = _p.Weapon.Shots;
            var def = Arsenal.Get(_p.Weapon.Slot);
            _view.TriggerFire(def.Cycle);
            _fx.Shoot(_p, _world, transform.position, _p.Yaw, _p.Pitch);

            bool last = _p.Weapon.ShotsLast != _seenLastShots;
            _seenLastShots = _p.Weapon.ShotsLast;
            if (def.Melee) _audio.MeleeSwing();
            else _audio.Fire(last);
        }

        if (_p.Weapon.ReloadStarts != _seenReloads)
        {
            _seenReloads = _p.Weapon.ReloadStarts;
            _view.TriggerReload(_p.Weapon.ReloadWasEmpty);
            _audio.Reload(_p.Weapon.ReloadWasEmpty);
        }

        // a vuoto: il click secco del percussore
        if (_p.Weapon.DryFires != _seenDry)
        {
            _seenDry = _p.Weapon.DryFires;
            _audio.DryFire();
        }

        // la ricarica finita o interrotta porta via il suo suono con sé
        if (_wasReloading && !_p.Weapon.Reloading) _audio.StopReload();
        _wasReloading = _p.Weapon.Reloading;
    }

    /// <summary>
    /// Compra un'arma. Offline la decidono le regole della simulazione (le stesse del server):
    /// ricompera, vendita della sostituita, drop di quella portata dal round prima.
    /// Online è una RICHIESTA: il server valida e lo snapshot riporta cosa hai in mano.
    /// </summary>
    public BuyOutcome Buy(WeaponSlot slot)
    {
        var def = Arsenal.Get(slot);

        if (Online)
        {
            if (!CanBuy)
                return new BuyOutcome { Slot = slot, Result = ShopResult.NotBuyPhase, Price = def.Price };
            _net.SendBuy(slot);
            return new BuyOutcome { Slot = slot, Result = ShopResult.Bought, Price = def.Price };
        }

        var outcome = Shop.Buy(GameRoot.Sim, Match, _world, _localId, slot);

        // La simulazione ha già messo in mano la nuova arma: qui si aggiorna il viewmodel.
        // (Passare da `Equip` non funzionava: `TryEquip` risponde "già in mano" e non fa
        // niente, quindi compravi un fucile e restavi con il modello vecchio.)
        if (outcome.Ok) ApplyEquipped(slot);
        return outcome;
    }

    /// <summary>Te lo puoi permettere? Conta cassa di squadra + fondo personale (doc 04).</summary>
    public bool CanAfford(WeaponSlot slot)
    {
        if (Unlimited) return true;
        return Shop.CanAfford(GameRoot.Sim, Match, _p, Arsenal.Get(slot).Price);
    }

    /// <summary>G: l'arma in mano parte in avanti e cade a terra (con le sue munizioni).</summary>
    /// <summary>
    /// La scelta del draft (doc 10): specializzazione e arma dichiarata. È pubblica —
    /// la vedono tutti nello snapshot — e si blocca al via del round 1.
    /// </summary>
    public void Draft(Specialization spec, byte intent)
    {
        if (Online) _net.SendDraft(spec, intent);
    }

    public void DropWeapon()
    {
        if (Online) { _net.SendDrop(); return; }
        if (Simulation.DropCurrent(GameRoot.Sim, _world, _localId)) SyncHeld();
    }

    /// <summary>F: raccoglie l'arma a terra più vicina, che porta con sé le sue munizioni.</summary>
    public void PickupWeapon()
    {
        if (Online) { _net.SendPickup(); return; }
        if (!Simulation.Pickup(GameRoot.Sim, _world, Match, _localId, out var slot)) return;

        SyncHeld();
        if (_ground != null) _ground.PlayPickup(slot);
    }

    /// <summary>Dopo un drop o una raccolta l'arma in mano cambia da sola: viewmodel e voce seguono.</summary>
    private void SyncHeld() => ApplyEquipped(_p.Weapon.Slot);

    /// <summary>
    /// 1 = primaria, 2 = secondaria, 3 = coltello — ma solo se quell'arma ce l'hai.
    /// Se lo slot è vuoto non succede niente: niente armi che compaiono dal nulla.
    /// </summary>
    private void EquipSlot(int key)
    {
        WeaponSlot? slot = key switch
        {
            1 => Shop.HeldPrimary(_p),
            2 => Shop.HeldSecondary(_p),
            3 => WeaponSlot.Melee,
            _ => null,
        };
        if (slot.HasValue) Equip(slot.Value);
    }

    /// <summary>
    /// Mette in mano un'arma. Offline solo se è in cache (il coltello c'è sempre);
    /// online è una richiesta: decide il server, e lo snapshot riporta cosa hai in mano.
    /// </summary>
    public void Equip(WeaponSlot slot)
    {
        if (Online)
        {
            _net.SendSwitch(slot);
            return;                              // l'arma la conferma lo snapshot
        }

        // solo quello che hai davvero: niente armi che compaiono dal nulla
        if (!Simulation.TryEquip(GameRoot.Sim, _localId, slot)) return;
        ApplyEquipped(slot);
    }

    /// <summary>Viewmodel, zoom e voce dell'arma che è appena entrata in mano.</summary>
    private void ApplyEquipped(WeaponSlot slot)
    {
        _view.Equip(slot);
        var def = Arsenal.Get(slot);
        _audio.StopReload();               // il caricatore non finisce di entrare in tasca
        _audio.SetWeapon(def);
        _audio.Draw();
        // Zoom stile Valorant: le ottiche ingrandiscono davvero, le mire metalliche
        // danno solo un filo di ingrandimento (1,2×), non un cannocchiale.
        _adsFov = def.Magnification > 1.05f
            ? Mathf.Clamp(85f / def.Magnification, 18f, 70f)
            : 72f;
        _seenShots = _p.Weapon.Shots;
        _seenFxShots = _p.Weapon.Shots;
        _seenReloads = _p.Weapon.ReloadStarts;
    }
}
