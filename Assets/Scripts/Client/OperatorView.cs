using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Falezia.Sim;
using UnityEngine;
#if FALEZIA_GLTF
using GLTFast;
#endif

/// <summary>
/// IL CORPO DELL'OPERATORE in terza persona: gli altri giocatori, e te stesso con Alt.
///
/// Il modello (`StreamingAssets/characters/operator/operator.glb`) è ricavato da una
/// scansione con `tools/character/rig_operator.py`: 20 000 triangoli, 19 ossi più le dita.
/// Lo scheletro lo gestisce <see cref="OperatorRig"/> (lato riconosciuto dalla posizione,
/// IK a due ossa, mani orientate con assi misurati).
///
/// **L'arma comanda, le mani seguono.** Prima arma e mani venivano messe ciascuna per conto
/// suo (l'arma su un'"ancora" vicino alla mano, le mani su punti fissi davanti al petto), e
/// non si incontravano mai. Ora, a ogni fotogramma:
///   1. il busto si gira (fucili: spalla sinistra avanti) e si piega con la mira, su e giù;
///   2. l'arma si mette sulla linea di mira, con l'impugnatura in un punto che dipende dal tipo
///      (fucile alla spalla, pistola a braccia tese, coltello basso) e dalla mira (ADS);
///      la direzione della canna si ricava dai SOCKET dell'arma (impugnatura → volata), quindi
///      vale anche per i modelli con la radice inclinata;
///   3. le mani vanno ESATTAMENTE su `socket_hand_r` (impugnatura) e `socket_hand_l` (guardamano,
///      o sotto l'impugnatura per le pistole) con l'IK; se il guardamano è fuori portata
///      (cecchini lunghi) l'arma arretra lungo la canna finché la mano ci arriva.
///
/// Animazioni **procedurali** dallo stato della simulazione: passo (avanti, indietro, di lato,
/// la fase avanza con la distanza), accovacciato (il bacino scende davvero: i piedi restano a
/// terra), in aria, ricarica (la mano sinistra va al caricatore, alla tasca e torna, per la
/// durata vera della scheda), sparo (contraccolpo), mira, morte (le ginocchia cedono e il corpo
/// cade all'indietro).
/// </summary>
public class OperatorView : MonoBehaviour
{
    private const string ModelPath = "characters/operator/operator.glb";

    /// <summary>
    /// Il corpo dell'operatore si mostra solo quando il rig è allineato al gioco.
    /// Con lo scheletro nuovo (Rigify) la posa procedurale lo spezza: finché non è
    /// adattata, meglio il viewmodel dell'arma senza corpo che un corpo a schegge.
    /// </summary>
    public static bool UseBody;

    // ---------------------------------------------------------------- caricamento condiviso
    private static GameObject _template;
    private static bool _loading;
    private static readonly Dictionary<string, GameObject> WeaponTemplates = new();
    private static readonly HashSet<string> WeaponsLoading = new();

    public static bool TemplateReady => _template != null;

    /// <summary>Il modello caricato (nascosto): lo usano anche le braccia in prima persona.</summary>
    public static GameObject Template => _template;

    /// <summary>Carica il modello una volta sola (chiamato all'avvio: niente attesa al primo avversario).</summary>
    public static void Preload()
    {
#if FALEZIA_GLTF
        if (_template != null || _loading) return;
        _loading = true;
        _ = LoadTemplate();
#else
        Debug.Log("OperatorView: installa com.unity.cloud.gltfast per il corpo degli operatori");
#endif
    }

#if FALEZIA_GLTF
    private static async Task LoadTemplate()
    {
        string path = Path.Combine(Application.streamingAssetsPath, ModelPath);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"OperatorView: {path} non trovato");
            _loading = false;
            return;
        }

        var gltf = new GltfImport();
        if (!await gltf.Load(path))
        {
            Debug.LogWarning("OperatorView: import del modello fallito");
            _loading = false;
            return;
        }

        var holder = new GameObject("Operatore (modello)");
        holder.SetActive(false);
        Object.DontDestroyOnLoad(holder);
        var inst = new GameObjectInstantiator(gltf, holder.transform);
        await gltf.InstantiateMainSceneAsync(inst);
        GltfMaterials.ToUrp(holder);       // shader glTF → URP: altrimenti pelle e vestiti piatti

        foreach (var r in holder.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.updateWhenOffscreen = true;          // il corpo esce dallo schermo spesso
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        _template = holder;
        _loading = false;
        Debug.Log($"OperatorView: modello pronto ({holder.GetComponentsInChildren<Transform>(true).Length} nodi)");
    }
#endif

    // ---------------------------------------------------------------- stato
    private OperatorRig _rig;
    private bool _built;
    private float _gait, _crouchT, _reloadT = -1f, _reloadDur, _kick, _dead, _airT;
    private float _speedSmoothed;
    private int _seenShots, _seenReloads;
    private bool _heard;
    private string _weaponId;
    private GameObject _weapon;
    private Transform _sHandR, _sHandL, _sMuzzle, _sMag;
    private float _fallSide;
    private static int _bodies;

    public bool Ready => _built;

    // ---------------------------------------------------------------- costruzione

    private void Update()
    {
        if (_built || _template == null) return;
        Build();
    }

    private void Build()
    {
        var body = Instantiate(_template, transform, false);
        body.name = "corpo";
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;
        body.SetActive(true);
        foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;

        _rig = OperatorRig.Bind(body, transform);
        if (_rig == null)
        {
            Debug.LogWarning("OperatorView: ossa non trovate nel modello");
            Destroy(body);
            enabled = false;
            return;
        }
        // da che parte cade: deterministico per oggetto, così due corpi non cadono uguali
        _fallSide = (_bodies++ & 1) == 0 ? 1f : -1f;
        _built = true;
    }

    // ---------------------------------------------------------------- animazione

    /// <summary>Aggiorna posa e arma dallo stato del giocatore (snapshot o simulazione locale).</summary>
    public void Apply(PlayerState p, float dt)
    {
        if (!_built || p == null) return;
        var def = Arsenal.Get(p.Weapon.Slot);

        // --- eventi: sparo e ricarica (contatori a 8 bit, come nello snapshot)
        int shots = p.Weapon.Shots & 0xFF;
        int reloads = p.Weapon.ReloadStarts & 0xFF;
        if (!_heard)
        {
            _heard = true;
            _seenShots = shots;
            _seenReloads = reloads;
        }
        else
        {
            if (((shots - _seenShots) & 0xFF) is > 0 and < 32) _kick = 1f;
            if (((reloads - _seenReloads) & 0xFF) is > 0 and < 32)
            {
                _reloadT = 0f;
                _reloadDur = Mathf.Max(0.3f, p.Weapon.ReloadWasEmpty ? def.ReloadEmpty : def.ReloadTactical);
            }
            _seenShots = shots;
            _seenReloads = reloads;
        }

        // --- movimento nello spazio del personaggio
        var vel = new Vector3(p.Vel.X, 0f, p.Vel.Z);
        if (!p.Alive) vel = Vector3.zero;
        float fwd = Vector3.Dot(vel, transform.forward);
        float side = Vector3.Dot(vel, transform.right);
        float speed = vel.magnitude;
        _speedSmoothed = Mathf.Lerp(_speedSmoothed, speed, 1f - Mathf.Exp(-8f * dt));
        _crouchT = Mathf.MoveTowards(_crouchT, p.Crouching ? 1f : 0f, dt * 6f);
        _airT = Mathf.MoveTowards(_airT, p.OnFloor || !p.Alive ? 0f : 1f, dt * 5f);
        _dead = Mathf.MoveTowards(_dead, p.Alive ? 0f : 1f, dt * (p.Alive ? 4f : 1.5f));

        // la fase del passo avanza con la DISTANZA, non col tempo: niente piedi che slittano
        float stride = Mathf.Lerp(0.85f, 1.55f, Mathf.InverseLerp(2.5f, 6.5f, _speedSmoothed));
        _gait += (_speedSmoothed / stride) * Mathf.PI * 2f * dt;
        if (_gait > Mathf.PI * 2f) _gait -= Mathf.PI * 2f;
        float dirSign = fwd < -0.25f * Mathf.Max(0.01f, speed) ? -1f : 1f;      // all'indietro il passo si inverte
        float sideK = speed > 0.1f ? Mathf.Clamp(side / speed, -1f, 1f) : 0f;
        float amp = Mathf.Clamp01(_speedSmoothed / 6.5f) * (1f - _airT);
        float swing = Mathf.Sin(_gait) * dirSign;
        float liftL = Mathf.Max(0f, Mathf.Sin(_gait + 0.5f));
        float liftR = Mathf.Max(0f, Mathf.Sin(_gait + 3.64f));

        // --- ricarica: avanza con la durata vera dell'arma
        if (_reloadT >= 0f)
        {
            _reloadT += dt;
            if (_reloadT >= _reloadDur || !p.Alive) _reloadT = -1f;
        }
        float rlPhase = _reloadT < 0f ? -1f : Mathf.Clamp01(_reloadT / _reloadDur);
        float rl = rlPhase < 0f ? 0f : Mathf.Sin(rlPhase * Mathf.PI);
        _kick = Mathf.Max(0f, _kick - dt * 7f);

        var r = _rig;
        r.ResetPose();

        // --- gambe
        float crouch = _crouchT;
        r.Rotate(r.LegL.Thigh, swing * 26f * amp - 52f * crouch - 30f * _airT, 0f, sideK * Mathf.Sin(_gait) * 9f * amp);
        r.Rotate(r.LegR.Thigh, -swing * 26f * amp - 52f * crouch - 18f * _airT, 0f, -sideK * Mathf.Sin(_gait) * 9f * amp);
        r.Rotate(r.LegL.Shin, liftL * 34f * amp + 66f * crouch + 45f * _airT, 0f, 0f);
        r.Rotate(r.LegR.Shin, liftR * 34f * amp + 66f * crouch + 30f * _airT, 0f, 0f);
        r.Rotate(r.LegL.Foot, -8f * amp * swing + 22f * crouch, 0f, 0f);
        r.Rotate(r.LegR.Foot, 8f * amp * swing + 22f * crouch, 0f, 0f);
        r.Rotate(r.Pelvis, -6f * crouch, -swing * 4f * amp, 0f);

        if (_dead > 0.001f)
        {
            DeathPose(_dead);
            if (_weapon != null) _weapon.SetActive(false);
            return;
        }

        r.Ground();

        // --- busto: si gira per i fucili e si piega con la mira
        bool pistol = IsPistol(p.Weapon.Slot), melee = def.Melee;
        bool longGun = !pistol && !melee;
        float pitch = Mathf.Clamp(p.Pitch * Mathf.Rad2Deg, -70f, 70f);
        float twist = longGun ? 14f : pistol ? 4f : 8f;
        float breath = Mathf.Sin(Time.time * 1.7f) * 0.35f;
        float kick = _kick;
        r.Rotate(r.Spine, 3f + 8f * crouch - pitch * 0.35f - kick * 1.5f, twist, 0f);
        r.Rotate(r.Chest, breath + 4f * crouch - pitch * 0.40f, twist, 0f);
        r.Rotate(r.Neck, -2f * crouch - pitch * 0.12f, -twist * 0.9f, 0f);
        r.Rotate(r.Head, -4f * crouch - pitch * 0.10f, -twist * 0.9f, 0f);

        // --- l'arma sulla linea di mira
        Quaternion aim = transform.rotation * Quaternion.Euler(-pitch, 0f, 0f);
        Vector3 D = aim * Vector3.forward, U = aim * Vector3.up, R = aim * Vector3.right;
        float ads = Mathf.Clamp01(p.Weapon.AdsT);

        bool haveWeapon = UpdateWeapon(def) && _weapon != null && _sHandR != null;
        Vector3 sR = r.Right.Upper != null ? r.Right.Upper.position : transform.position + Vector3.up * 1.4f;
        Vector3 sL = r.Left.Upper != null ? r.Left.Upper.position : transform.position + Vector3.up * 1.4f;
        Vector3 mid = (sR + sL) * 0.5f;

        Vector3 grip;
        if (melee) grip = sR + D * 0.30f - U * 0.30f + R * 0.04f;
        else if (pistol) grip = mid + D * Mathf.Lerp(0.42f, 0.46f, ads) + U * Mathf.Lerp(-0.08f, 0.02f, ads) + R * 0.02f;
        else grip = sR + D * Mathf.Lerp(0.26f, 0.28f, ads) + U * Mathf.Lerp(-0.11f, 0.00f, ads) - R * Mathf.Lerp(0.05f, 0.13f, ads);

        // ricarica: l'arma si avvicina al petto e si inclina per far vedere il caricatore
        grip += (U * 0.05f - D * 0.10f - R * 0.03f) * rl;
        grip -= D * 0.035f * kick;
        Quaternion weaponAim = Quaternion.AngleAxis(-25f * rl, D) * Quaternion.AngleAxis(-kick * 4f, R) * aim;

        if (haveWeapon)
        {
            PlaceWeapon(grip, weaponAim);

            // guardamano fuori portata (fucili lunghi): l'arma arretra lungo la canna
            if (!pistol && !melee && _sHandL != null && r.Left.Upper != null)
            {
                Vector3 wl = LeftWrist(longGun: true);
                float reach = (r.Left.UpperLen + r.Left.ForeLen) * 0.97f;
                float d = Vector3.Distance(wl, r.Left.Upper.position);
                if (d > reach)
                {
                    grip -= (weaponAim * Vector3.forward) * (d - reach);
                    PlaceWeapon(grip, weaponAim);
                }
            }
        }

        // --- braccia
        Vector3 down = -transform.up;
        Vector3 poleR = sR + transform.right * 0.30f + down * 0.55f - D * 0.05f;
        Vector3 poleL = sL - transform.right * 0.30f + down * 0.60f;

        if (haveWeapon)
        {
            WeaponFrame(out var wD, out var wU, out var wR);

            // destra: sull'impugnatura. Il dorso della mano va in avanti e un po' in giù lungo
            // l'impugnatura (le dita poi si chiudono attorno), il palmo guarda l'arma
            Vector3 fR = GripFingers(wD, wU);
            Vector3 wristR = GripWrist(_sHandR.position, fR, wR, 1f);
            r.SolveArm(r.Right, wristR, poleR, -wR);
            r.OrientHand(r.Right, fR, -wR);
            r.Curl(r.Right, melee ? 0.95f : 0.85f);

            // sinistra: guardamano (o sotto l'impugnatura), oppure caricatore e tasca in ricarica
            if (melee)
            {
                Vector3 guard = sL + D * 0.30f - U * 0.32f - R * 0.05f;
                r.SolveArm(r.Left, guard, poleL, R);
                r.OrientHand(r.Left, (D + -U * 0.6f).normalized, R);
                r.Curl(r.Left, 0.7f);
            }
            else
            {
                Vector3 hold = LeftWrist(!pistol);
                Vector3 fL = LeftFingers(!pistol, wD, wU, wR);
                Vector3 pL = LeftPalm(!pistol, wU, wR);
                Vector3 target = hold;
                if (rlPhase >= 0f)
                {
                    Vector3 mag = (_sMag != null ? _sMag.position : _sHandR.position + wD * 0.12f - wU * 0.10f) - wU * 0.05f;
                    // tasca porta-caricatori sul fianco sinistro (nello spazio del corpo: scende con lui)
                    Vector3 pouch = r.Body.TransformPoint(new Vector3(-0.14f, 0.98f, 0.16f));
                    target = ReloadPath(rlPhase, hold, mag, pouch);
                    fL = Vector3.Slerp(fL, (-wU + wD * 0.2f).normalized, rl);
                }
                r.SolveArm(r.Left, target, poleL, pL);
                r.OrientHand(r.Left, fL, pL);
                r.Curl(r.Left, rlPhase >= 0f ? 0.55f : 0.72f);
            }
        }
        else
        {
            // niente modello dell'arma (ancora in caricamento): mani in posizione di tiro
            r.SolveArm(r.Right, grip, poleR, -R);
            r.SolveArm(r.Left, grip + D * 0.22f, poleL, R);
            r.Curl(r.Right, 0.8f);
            r.Curl(r.Left, 0.7f);
        }
    }

    // ---------------------------------------------------------------- mano sinistra

    private Vector3 LeftWrist(bool longGun)
    {
        WeaponFrame(out var d, out var u, out var rr);
        return LeftWristAt(_sHandL.position, longGun, d, u, rr, 1f);
    }

    // ---- presa, condivisa con le braccia in prima persona (FirstPersonArms) ----

    /// <summary>Destra sull'impugnatura: direzione polso → nocche (in avanti, un po' in giù lungo l'impugnatura).</summary>
    public static Vector3 GripFingers(Vector3 d, Vector3 u) => (d * 0.85f - u * 0.50f).normalized;

    /// <summary>Polso destro: dietro il socket lungo la mano, spostato dal lato del dorso (il palmo guarda l'arma).</summary>
    public static Vector3 GripWrist(Vector3 socket, Vector3 fingers, Vector3 r, float scale) =>
        socket - fingers * 0.055f * scale + r * 0.025f * scale;

    /// <summary>
    /// Sinistra. Fucile: sotto il guardamano, palmo in su verso destra, dita che salgono sul
    /// fianco destro. Pistola: a coppa contro le dita della destra, palmo verso destra.
    /// </summary>
    public static Vector3 LeftFingers(bool longGun, Vector3 d, Vector3 u, Vector3 r) =>
        longGun ? (d * 0.60f + r * 0.50f + u * 0.30f).normalized
                : (d * 0.80f - u * 0.45f + r * 0.10f).normalized;

    public static Vector3 LeftPalm(bool longGun, Vector3 u, Vector3 r) =>
        longGun ? (u * 0.80f + r * 0.40f).normalized : r;

    public static Vector3 LeftWristAt(Vector3 socket, bool longGun, Vector3 d, Vector3 u, Vector3 r, float scale)
    {
        Vector3 f = LeftFingers(longGun, d, u, r);
        return longGun
            ? socket - f * 0.060f * scale - LeftPalm(true, u, r) * 0.030f * scale
            : socket - f * 0.055f * scale - r * 0.035f * scale;
    }

    /// <summary>Ricarica: guardamano → caricatore → tasca → caricatore → guardamano.</summary>
    private static Vector3 ReloadPath(float t, Vector3 hold, Vector3 mag, Vector3 pouch)
    {
        if (t < 0.22f) return Vector3.Lerp(hold, mag, Smooth(t / 0.22f));
        if (t < 0.45f) return Vector3.Lerp(mag, pouch, Smooth((t - 0.22f) / 0.23f));
        if (t < 0.70f) return Vector3.Lerp(pouch, mag, Smooth((t - 0.45f) / 0.25f));
        return Vector3.Lerp(mag, hold, Smooth((t - 0.70f) / 0.30f));
    }

    private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    // ---------------------------------------------------------------- morte

    /// <summary>Le ginocchia cedono, poi il corpo cade all'indietro facendo perno sulle caviglie.</summary>
    private void DeathPose(float t)
    {
        var r = _rig;
        float knees = Mathf.Sin(Mathf.Clamp01(t * 1.8f) * Mathf.PI * 0.5f);
        r.Rotate(r.LegL.Thigh, -38f * knees, 0f, 0f);
        r.Rotate(r.LegR.Thigh, -28f * knees, 0f, 0f);
        r.Rotate(r.LegL.Shin, 62f * knees, 0f, 0f);
        r.Rotate(r.LegR.Shin, 48f * knees, 0f, 0f);
        r.Rotate(r.Spine, 6f * knees, 0f, 0f);
        r.Rotate(r.Head, 10f * t, 15f * _fallSide * t, 0f);
        // braccia molli, un po' aperte
        r.Rotate(r.Right.Upper, 0f, 0f, 30f * t);
        r.Rotate(r.Left.Upper, 0f, 0f, -30f * t);
        r.Ground();

        float fall = Smooth(Mathf.Clamp01((t - 0.15f) / 0.85f));
        fall *= fall;                                              // accelera come un corpo che cade
        var rot = Quaternion.AngleAxis(-84f * fall, Vector3.right) * Quaternion.AngleAxis(12f * _fallSide * fall, Vector3.forward);
        var pivot = new Vector3(0f, r.RestAnkleY, -0.05f);
        var body = r.Body;
        body.localPosition = pivot + rot * (body.localPosition - pivot) + new Vector3(0f, -0.04f * fall, -0.05f * fall);
        body.localRotation = rot * body.localRotation;
    }

    // ---------------------------------------------------------------- arma

    private static bool IsPistol(WeaponSlot slot) => slot switch
    {
        WeaponSlot.Standard or WeaponSlot.Hush or WeaponSlot.Chatter or WeaponSlot.Snub or WeaponSlot.Hammer => true,
        _ => false,
    };

    /// <summary>Assi dell'arma ricavati dai socket: avanti = impugnatura → volata.</summary>
    private void WeaponFrame(out Vector3 d, out Vector3 u, out Vector3 r)
    {
        d = (_sMuzzle != null ? _sMuzzle.position - _sHandR.position : -_sHandR.forward).normalized;
        u = _sHandR.up;
        u = (u - Vector3.Dot(u, d) * d).normalized;
        r = Vector3.Cross(u, d);
    }

    /// <summary>Mette l'arma in modo che l'impugnatura stia su <paramref name="grip"/> e la canna guardi lungo <paramref name="aim"/>.</summary>
    private void PlaceWeapon(Vector3 grip, Quaternion aim)
    {
        var w = _weapon.transform;
        Vector3 hr = w.InverseTransformPoint(_sHandR.position);
        Vector3 mz = _sMuzzle != null ? w.InverseTransformPoint(_sMuzzle.position) : hr + Vector3.back;
        Vector3 dl = (mz - hr).normalized;
        Vector3 ul = w.InverseTransformDirection(_sHandR.up);
        var frame = Quaternion.LookRotation(dl, ul);
        w.rotation = aim * Quaternion.Inverse(frame);
        w.position += grip - _sHandR.position;
    }

    /// <summary>Il modello dell'arma in pugno (uno per tipo, condiviso). Falso finché non è caricato.</summary>
    private bool UpdateWeapon(WeaponDef def)
    {
        if (_weaponId != def.Id)
        {
            _weaponId = def.Id;
            if (_weapon != null) Destroy(_weapon);
            _weapon = null;
            _sHandR = _sHandL = _sMuzzle = _sMag = null;
        }

        if (_weapon == null)
        {
            if (!WeaponTemplates.TryGetValue(def.Id, out var tpl))
            {
                if (WeaponsLoading.Add(def.Id)) _ = LoadWeapon(def.Id);
                return false;
            }
            if (tpl == null) return false;                 // arma senza modello

            _weapon = Instantiate(tpl, transform, false);
            _weapon.SetActive(true);
            foreach (var t in _weapon.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            _weapon.transform.localScale = Vector3.one;
            _sHandR = OperatorRig.Find(_weapon.transform, "socket_hand_r");
            _sHandL = OperatorRig.Find(_weapon.transform, "socket_hand_l");
            _sMuzzle = OperatorRig.Find(_weapon.transform, "socket_muzzle");
            _sMag = OperatorRig.Find(_weapon.transform, "mag");
        }
        if (!_weapon.activeSelf) _weapon.SetActive(true);
        return true;
    }

#if FALEZIA_GLTF
    private static async Task LoadWeapon(string id)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "weapons", id, id + ".glb");
        if (!File.Exists(path)) { WeaponTemplates[id] = null; return; }

        var gltf = new GltfImport();
        if (!await gltf.Load(path)) { WeaponTemplates[id] = null; return; }

        var holder = new GameObject($"Arma {id}");
        holder.SetActive(false);
        Object.DontDestroyOnLoad(holder);
        var inst = new GameObjectInstantiator(gltf, holder.transform);
        await gltf.InstantiateMainSceneAsync(inst);
        GltfMaterials.ToUrp(holder);

        // in terza persona l'arma non suona clip: la posa è data dai socket
        foreach (var a in holder.GetComponentsInChildren<Animation>(true))
        {
            a.playAutomatically = false;
            a.Stop();
            a.enabled = false;
        }
        foreach (var r in holder.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        WeaponTemplates[id] = holder;
    }
#else
    private static Task LoadWeapon(string id) { WeaponTemplates[id] = null; return Task.CompletedTask; }
#endif
}
