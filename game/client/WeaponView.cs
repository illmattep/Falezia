using Falezia.Sim;
using Godot;

/// <summary>
/// Viewmodel in prima persona: carica il GLB dell'arma (art/weapons) sotto la camera,
/// riproduce le animazioni cotte in Blender e aggiunge rinculo e mira.
/// Lo stato VERO delle munizioni è nella simulazione: qui si leggono solo i contatori.
///
/// Le armi non stanno tutte allo stesso modo in mano: una pistola è corta e vicina,
/// un fucile a otturatore è lungo e pesante. La posa (hip e mira) viene da
/// <see cref="Pose"/>, il mirino si allinea da solo sul socket_aim del modello.
/// </summary>
public partial class WeaponView : Node3D
{
    /// <summary>Come un'arma sta in mano: distanza, scala, inclinazione.</summary>
    private readonly struct Pose
    {
        public readonly Vector3 Hip;
        public readonly Vector3 HipRot;
        public readonly float Scale;
        public readonly float AdsZ;      // dove sta l'IMPUGNATURA in mira, in metri davanti all'occhio
        public readonly float AdsFov;    // campo visivo in mira: i cecchini hanno l'ottica
        public readonly float Kick;

        public Pose(Vector3 hip, Vector3 hipRot, float scale, float adsZ, float kick, float adsFov)
        {
            Hip = hip;
            HipRot = hipRot;
            Scale = scale;
            AdsZ = adsZ;
            Kick = kick;
            AdsFov = adsFov;
        }
    }

    // pistola: la posa della Standard, che l'utente ha già approvato. Per le armi lunghe
    // l'impugnatura sta più lontana dall'occhio: se si tenesse a 20 cm si vedrebbe solo
    // il castello (errore già visto: il Blitz in mira era un muro grigio).
    private static readonly Pose Pistol = new(new Vector3(0.172f, -0.146f, -0.272f), new Vector3(2.0f, -8.0f, 1.5f), 1.00f, -0.225f, 0.55f, 58f);
    private static readonly Pose Smg = new(new Vector3(0.145f, -0.19f, -0.285f), new Vector3(1.8f, -7.0f, 1.2f), 0.93f, -0.185f, 0.5f, 62f);
    private static readonly Pose Shotgun = new(new Vector3(0.145f, -0.205f, -0.3f), new Vector3(1.6f, -6.5f, 1.0f), 0.95f, -0.175f, 0.95f, 64f);
    private static readonly Pose Rifle = new(new Vector3(0.145f, -0.205f, -0.295f), new Vector3(1.8f, -7.0f, 1.2f), 0.93f, -0.17f, 0.7f, 60f);
    private static readonly Pose Sniper = new(new Vector3(0.15f, -0.21f, -0.29f), new Vector3(1.4f, -6.0f, 1.0f), 0.96f, -0.165f, 1.1f, 42f);
    private static readonly Pose Lmg = new(new Vector3(0.15f, -0.215f, -0.29f), new Vector3(1.6f, -6.5f, 1.0f), 0.95f, -0.17f, 0.8f, 62f);
    // il coltello si tiene di taglio: lama verso l'interno e in alto, così si VEDE.
    // Con la lama dritta davanti alla camera si vedrebbe solo la punta.
    private static readonly Pose Knife = new(new Vector3(0.120f, -0.215f, -0.300f), new Vector3(22.0f, -52.0f, 14.0f), 1.00f, -0.355f, 0.30f, 76f);

    private Camera3D _cam;
    private Node3D _holder;
    private AnimationPlayer _anim;
    private WeaponSlot _slot = WeaponSlot.Standard;
    private string _playing = "";
    private float _animLeft;
    private int _seenShots, _seenReloads, _seenDry;
    /// <summary>Al primo Sync dopo un cambio d'arma i contatori si ADOTTANO, non si azzerano.</summary>
    private bool _adopt;
    private float _kickBack, _kickVel, _kickPitch;
    private float _ads;
    private bool _loggedAnim;
    private bool _loggedMuzzle;

    private Pose _pose = Pistol;
    private bool _melee;
    private Vector3 _adsPos = new(0f, -0.082f, -0.225f);

    public void Setup(Camera3D cam)
    {
        _cam = cam;
        _holder = new Node3D { Name = "ArmaInMano" };
        AddChild(_holder);
        Equip(WeaponSlot.Standard);
    }

    public WeaponSlot Slot => _slot;

    /// <summary>
    /// Campo visivo in mira: parte da quello della classe (mire metalliche) e si chiude
    /// con l'ingrandimento dell'ottica. 3× Vantage, 4× Spotter, 6× Longshot.
    /// </summary>
    public float AdsFov
    {
        get
        {
            var def = Arsenal.Get(_slot);
            float baseFov = _pose.AdsFov;
            if (def.Magnification <= 1.05f) return baseFov;
            float half = Mathf.DegToRad(baseFov) * 0.5f;
            return 2f * Mathf.RadToDeg(Mathf.Atan(Mathf.Tan(half) / def.Magnification));
        }
    }

    /// <summary>La posa giusta per la classe dell'arma (doc 11: pistola, SMG, pompa, AR, cecchino, LMG).</summary>
    private static Pose PoseFor(WeaponDef d)
    {
        if (d.Melee) return Knife;
        return d.Profile switch
        {
            "smg" => Smg,
            "shotgun" => Shotgun,
            "rifle" => d.Id == "heavy" ? Lmg : Rifle,
            "sniper" => Sniper,
            _ => Pistol
        };
    }

    /// <summary>Carica (o ricarica) il modello dell'arma nel viewmodel.</summary>
    public void Equip(WeaponSlot slot)
    {
        _slot = slot;
        if (_holder != null)
        {
            foreach (var c in _holder.GetChildren()) c.QueueFree();
        }
        _anim = null;
        _playing = "";
        _seenShots = _seenReloads = _seenDry = 0;
        _adopt = true;   // l'arma che riprendi in mano ha già i suoi contatori avanti
        _loggedAnim = false;
        _loggedMuzzle = false;
        _ads = 0f;
        _kickBack = _kickVel = _kickPitch = 0f;

        var def = Arsenal.Get(slot);
        _pose = PoseFor(def);
        _melee = def.Melee;

        string path = $"res://assets/weapons/{def.Id}/{def.Id}.glb";
        var scene = ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;
        if (scene == null)
        {
            GD.PushWarning($"WeaponView: {path} non importato. Apri l'editor una volta per importare il GLB.");
            _adsPos = Pistol.Hip;
            return;
        }

        var inst = scene.Instantiate<Node3D>();
        inst.Name = def.Id;
        inst.Scale = Vector3.One * _pose.Scale;
        _holder.AddChild(inst);

        // l'arma in prima persona NON proietta ombre: è attaccata alla camera, la sua
        // ombra finiva sul muro come una macchia puntinata dietro al mirino
        NoShadow(inst);
        _anim = FindAnim(inst);

        // il mirino si allinea sul socket_aim: la tacca di mira finisce sulla linea di tiro
        // a qualunque arma, senza numeri inventati arma per arma
        Vector3 aim = Vector3.Zero;
        if (FindNamed(inst, "socket_aim") is Node3D aimNode) aim = aimNode.Position * _pose.Scale;
        // il punto da portare sull'asse della camera non è la MIRA ma l'OCCHIO: la mira
        // di un fucile sta quasi alla volata, e allineando quella l'arma finiva dietro
        // l'obiettivo (si vedeva il fucile tagliato e le tacche "in mezzo alle scatole")
        var eyeNode = FindNamed(inst, "socket_eye") as Node3D;
        // la mira si allinea sul socket_aim; il coltello no: si limita ad alzarsi
        // in mira l'impugnatura sta dove dice la posa, e il mirino si allinea da solo:
        // solo X e Y vengono dal socket_aim, la distanza no (altrimenti un'arma con
        // l'ottica molto avanti finirebbe incollata all'occhio)
        // niente scostamento: la linea di mira del modello deve cadere ESATTAMENTE
        // sull'asse della camera, che è dove la simulazione spara. Con 4 mm di scarto
        // il mirino stava sotto il centro di quasi un grado e si sparava alto.
        if (def.Melee)
            _adsPos = _pose.Hip + new Vector3(-0.03f, 0.035f, -0.055f);
        else if (eyeNode != null)
        {
            // X e Y dall'occhio (la linea di mira cade sull'asse della camera), Z dalla
            // posa della classe: l'occhio da solo terrebbe il fucile a 15 cm e in uno
            // schermo stretto il castello coprirebbe mezza visuale.
            var e = -eyeNode.Position * _pose.Scale;
            // 3 cm in più di distanza: con l'occhio "giusto" l'arma resta incollata
            // alla faccia e su uno schermo stretto il castello copre mezza visuale
            _adsPos = new Vector3(e.X, e.Y, Mathf.Min(e.Z, _pose.AdsZ) - 0.03f);
        }
        else _adsPos = new Vector3(-aim.X, -aim.Y, _pose.AdsZ);

        if (_anim != null)
        {
            // le clip si chiamano come in Blender: idle, fire, fire_last, reload, reload_empty, draw
            if (_anim.HasAnimation("draw"))
            {
                _anim.Play("draw");
                _playing = "draw";
                _animLeft = 0.45f;
            }
            else
            {
                Play("idle", 0.1f);
                _anim.Stop();
            }
        }
        Layout();
    }

    private static void NoShadow(Node n)
    {
        if (n is MeshInstance3D mi) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        foreach (var c in n.GetChildren()) NoShadow(c);
    }

    private static AnimationPlayer FindAnim(Node n)
    {
        if (n is AnimationPlayer ap) return ap;
        foreach (var c in n.GetChildren())
        {
            if (FindAnim(c) is AnimationPlayer found) return found;
        }
        return null;
    }

    /// <summary>Il finestrino d'estrazione del bossolo, in spazio mondo.</summary>
    public Vector3 EjectPort()
    {
        if (_holder != null && FindNamed(_holder, "socket_eject") is Node3D socket)
            return socket.GlobalPosition;

        // ripiego: sopra e a destra del vivo di canna, dove sta il finestrino
        if (_cam == null) return Vector3.Zero;
        return _cam.GlobalPosition
             - _cam.GlobalBasis.Z * 0.45f
             + _cam.GlobalBasis.X * 0.12f
             + _cam.GlobalBasis.Y * 0.02f;
    }

    /// <summary>
    /// Il vivo di canna in spazio mondo: serve alle tracce dei proiettili, che devono
    /// partire da lì e non dal centro della camera. Se il GLB non ha il socket si
    /// ripiega su un punto davanti alla camera, dove la canna finisce davvero.
    /// </summary>
    public Vector3 Muzzle()
    {
        if (_holder != null && FindNamed(_holder, "socket_muzzle") is Node3D socket && _cam != null)
        {
            // un socket sballato (modello riportato male) disegnerebbe una traccia da
            // un'altra parte del mondo: si accetta solo se sta vicino alla camera
            var p = socket.GlobalPosition;
            if (p.DistanceTo(_cam.GlobalPosition) < 2.5f)
            {
                if (!_loggedMuzzle)
                {
                    _loggedMuzzle = true;
                    GD.Print("[arma] vivo di canna: socket_muzzle del GLB");
                }
                return p;
            }
        }

        if (_cam == null) return Vector3.Zero;
        if (!_loggedMuzzle)
        {
            _loggedMuzzle = true;
            GD.PushWarning("[arma] socket_muzzle assente: traccia dal ripiego davanti alla camera");
        }
        return _cam.GlobalPosition
             - _cam.GlobalBasis.Z * 0.55f    // avanti
             + _cam.GlobalBasis.X * 0.07f    // verso il fianco
             - _cam.GlobalBasis.Y * 0.06f;   // appena sotto la linea di mira
    }

    private static Node FindNamed(Node n, string name)
    {
        if (n.Name == name) return n;
        foreach (var c in n.GetChildren())
            if (FindNamed(c, name) is Node found) return found;
        return null;
    }

    /// <summary>Allinea il modello alla camera: posa hip, mira e rinculo.</summary>
    private void Layout()
    {
        if (_holder == null) return;
        Vector3 pos = _pose.Hip.Lerp(_adsPos, _ads);
        // il coltello non si raddrizza in mira: la lama resta di taglio, si alza e basta
        Vector3 rot = _melee ? _pose.HipRot : _pose.HipRot.Lerp(Vector3.Zero, _ads);
        pos.Z += _kickBack * _pose.Scale;
        pos.Y -= _kickPitch * 0.0015f;
        rot.X -= _kickPitch;
        _holder.Position = pos;
        _holder.RotationDegrees = rot;
    }

    /// <summary>Legge lo stato della simulazione e fa partire le animazioni giuste.</summary>
    public void Sync(WeaponState w, float delta)
    {
        if (_adopt)
        {
            // il caricatore è già avanti (l'hai usato prima): senza questo il cambio
            // d'arma faceva partire l'animazione di sparo e il rinculo dal nulla
            _adopt = false;
            _seenShots = w.Shots;
            _seenReloads = w.ReloadStarts;
            _seenDry = w.DryFires;
        }

        _ads = Mathf.MoveToward(_ads, w.AdsT, delta * 6f);

        // rinculo: spinta indietro + alzata, ritorno elastico
        _kickVel -= _kickBack * 90f * delta;
        _kickVel *= Mathf.Max(0f, 1f - 9f * delta);
        _kickBack += _kickVel * delta;
        _kickPitch = Mathf.MoveToward(_kickPitch, 0f, delta * 26f);

        if (w.Shots != _seenShots)
        {
            _seenShots = w.Shots;
            var def = Arsenal.Get(w.Slot);
            if (!def.Melee)
            {
                _kickVel += _pose.Kick;
                _kickPitch = Mathf.Min(_kickPitch + def.BloomPerShot * 1.6f, 6.0f);
            }
            Play(w.ShotsLast > 0 ? "fire_last" : "fire", Mathf.Max(0.08f, def.Cycle * 0.9f));
        }
        if (w.ReloadStarts != _seenReloads)
        {
            _seenReloads = w.ReloadStarts;
            var def = Arsenal.Get(w.Slot);
            // la clip viene ADATTATA alla durata vera della ricarica: la scheda decide
            // quanto dura, il modello la segue (le vecchie clip erano più lunghe)
            Play(w.ReloadWasEmpty ? "reload_empty" : "reload",
                 w.ReloadWasEmpty ? def.ReloadEmpty : def.ReloadTactical, fit: true);
        }

        // Scarrellata: se il caricatore è vuoto l'arma RESTA sull'ultimo fotogramma
        // di fire_last (otturatore indietro) invece di tornare a idle. La ricarica
        // riparte da lì con reload_empty e alla fine torna idle.
        var gun = Arsenal.Get(w.Slot);
        bool locked = !gun.Melee && w.Ammo <= 0 && !w.Reloading;
        if (_animLeft > 0f)
        {
            _animLeft -= delta;
            if (_animLeft <= 0f)
            {
                if (locked) _anim?.Pause();
                else Play("idle", 0.05f);
            }
        }
        else if (!locked && _anim != null && !_anim.IsPlaying() && _playing != "idle")
        {
            Play("idle", 0.05f);   // dopo la ricarica si riparte
        }

        Layout();
    }

    private void Play(string name, float fallbackDuration, bool fit = false)
    {
        _playing = name;
        string resolved = Resolve(name);
        if (_anim == null || resolved == null)
        {
            if (!_loggedAnim)
            {
                _loggedAnim = true;
                GD.Print("WeaponView: animazioni disponibili = " +
                         (_anim == null ? "(nessun AnimationPlayer)" : string.Join(", ", _anim.GetAnimationList())));
            }
            _animLeft = fallbackDuration;
            return;
        }

        // fit = la clip dura quanto la ricarica della scheda: si cambia la velocità
        // di riproduzione invece di tagliare l'animazione a metà
        float speed = 1f;
        if (fit)
        {
            float clip = (float)_anim.GetAnimation(resolved).Length;
            if (clip > 0.05f && fallbackDuration > 0.05f) speed = clip / fallbackDuration;
        }
        _anim.Play(resolved, 0.02f, speed);
        _animLeft = Mathf.Max(fallbackDuration, 0.02f);
    }

    /// <summary>
    /// L'import glTF può prefissare le animazioni (es. "standard/fire"): si accetta
    /// prima il nome esatto, poi quello che finisce o contiene la chiave.
    /// </summary>
    private string Resolve(string name)
    {
        if (_anim == null) return null;
        if (_anim.HasAnimation(name)) return name;
        foreach (var a in _anim.GetAnimationList())
        {
            string s = a.ToString();
            if (s.EndsWith("/" + name) || s.EndsWith("|" + name) || s.EndsWith("_" + name))
                return a;
        }
        foreach (var a in _anim.GetAnimationList())
            if (a.ToString().Contains(name)) return a;
        return null;
    }
}
