using System.Collections.Generic;
#if FALEZIA_GLTF
using System.IO;
using System.Threading.Tasks;
using GLTFast;
#endif
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// L'ARMA IN PRIMA PERSONA.
///
/// Con il pacchetto **glTFast** installato (`com.unity.cloud.gltfast`) carica i GLB
/// degli armamenti da StreamingAssets/weapons e suona le 7 clip cotte in Blender
/// (idle, fire, fire_last, reload, reload_empty, draw, holster).
///
/// Senza il pacchetto il gioco funziona lo stesso: l'arma non si vede in 3D, ma
/// munizioni, animazioni di stato e tutto il resto restano. La define
/// `FALEZIA_GLTF` arriva da sola dall'asmdef (versionDefine): appena installi il
/// pacchetto, il codice si attiva senza toccare niente.
///
/// La rotazione di 180° su Y serve perché la canna è esportata verso -Z (glTF),
/// mentre in Unity la camera guarda +Z.
/// </summary>
public class WeaponView : MonoBehaviour
{
    [Header("Posizione")]
    public Vector3 hipPosition = new(0.246f, -0.150f, 0.322f);
    public Vector3 hipRotation = new(2f, 184f, 1.5f);
    public Vector3 adsPosition = new(0f, -0.075f, 0.24f);
    private Vector3 _adsBase;
    private bool _adsCached;
    public Vector3 adsRotation = new(0f, 180f, 0f);
    public float scale = 0.80f;

    private readonly Dictionary<string, GameObject> _cache = new();
    private GameObject _current;
    private Animation _anim;
    private WeaponSlot _slot = WeaponSlot.Standard;
    private float _ads;
    private float _kickBack, _kickUp;      // contraccolpo visivo dell'arma
    private Transform _eye;             // socket_eye del modello (posa vera, non di riposo)
    private Transform _sockR, _sockL, _sockMuzzle, _mag;   // punti delle mani, volata, caricatore

    /// <summary>Impugnatura dell'arma in mano (dove va la mano destra in prima persona).</summary>
    public Transform SocketHandR => _current != null ? _sockR : null;
    /// <summary>Guardamano, o sotto l'impugnatura per le pistole (mano sinistra).</summary>
    public Transform SocketHandL => _current != null ? _sockL : null;
    public Transform SocketMuzzle => _current != null ? _sockMuzzle : null;
    /// <summary>Il caricatore animato dalle clip di ricarica: la mano sinistra lo segue.</summary>
    public Transform Magazine => _current != null ? _mag : null;

    /// <summary>Avanzamento della clip di ricarica in corso, 0..1; -1 se non si sta ricaricando.</summary>
    public float ReloadPhase
    {
        get
        {
            if (_anim == null || (_playing != "reload" && _playing != "reload_empty")) return -1f;
            if (!_anim.IsPlaying(_playing)) return -1f;
            var st = _anim[_playing];
            return st == null ? -1f : Mathf.Clamp01(st.normalizedTime);
        }
    }

    public bool IsMelee => Arsenal.Get(_slot).Melee;
    public float adsDistance = 0.34f;   // quanto sta avanti l'occhio in mira (si adatta alla lunghezza)
    private bool _loading;
    private string _playing = "";
    private bool _warned;

    /// <summary>Stato dell'animazione e posa del nodo radice del modello (diagnostica).</summary>
    public string PoseInfo
    {
        get
        {
            if (_current == null) return "modello assente";
            var root = _eye != null ? _eye.parent : _current.transform;
            string playing = _anim == null ? "no anim" : (_anim.isPlaying ? "in corso" : "ferma");
            return $"{_playing} {playing} · radice {root.localEulerAngles.x:F0}/{root.localEulerAngles.y:F0}/{root.localEulerAngles.z:F0}";
        }
    }

    /// <summary>Distanza occhio-arma in mira, per classe (metri).</summary>
    private static float AdsDistanceFor(WeaponSlot slot) => slot switch
    {
        WeaponSlot.Standard or WeaponSlot.Hush or WeaponSlot.Chatter or WeaponSlot.Snub or WeaponSlot.Hammer => 0.16f,
        WeaponSlot.Cadence or WeaponSlot.Scrap or WeaponSlot.Blitz => 0.17f,
        WeaponSlot.Choke or WeaponSlot.Sweeper => 0.18f,
        WeaponSlot.Anvil or WeaponSlot.Service => 0.19f,
        WeaponSlot.Vantage or WeaponSlot.Spotter or WeaponSlot.Longshot => 0.18f,
        WeaponSlot.Heavy => 0.19f,
        _ => 0.22f,
    };

    /// <summary>Dove sta il socket dell'occhio nello spazio della camera (diagnostica).</summary>
    public Vector3 EyeInCamera =>
        _eye == null || transform.parent == null
            ? Vector3.zero
            : transform.parent.InverseTransformPoint(_eye.position);

    /// <summary>L'arma in mano è pronta? (il GLB si carica in modo asincrono)</summary>
    public bool Ready => _current != null;

    public void Equip(WeaponSlot slot)
    {
        _slot = slot;
        var def = Arsenal.Get(slot);

        // l'arma di prima sparisce SUBITO, anche se la nuova non è ancora caricata
        if (_current != null) _current.SetActive(false);
        _current = null;
        _anim = null;
        _eye = null;
        _sockR = _sockL = _sockMuzzle = _mag = null;
        _playing = "";

        if (_cache.TryGetValue(def.Id, out var cached))
        {
            _current = cached;
            _current.SetActive(true);
            _anim = _current.GetComponentInChildren<Animation>();
            _eye = FindDeep(_current.transform, "socket_eye");   // l'occhio di QUESTA arma, non dell'ultima caricata
            _sockR = FindDeep(_current.transform, "socket_hand_r");
            _sockL = FindDeep(_current.transform, "socket_hand_l");
            _sockMuzzle = FindDeep(_current.transform, "socket_muzzle");
            _mag = FindDeep(_current.transform, "mag");
            adsDistance = AdsDistanceFor(_slot);
            Play("idle", 0.1f);
            return;
        }

#if FALEZIA_GLTF
        Request(def.Id, true);
        // alla prima arma si mettono in coda anche tutte le altre: niente attese al cambio
        if (!_preloaded)
        {
            _preloaded = true;
            foreach (var d in Arsenal.All) Request(d.Id, false);
        }
#else
        if (!_warned)
        {
            _warned = true;
            Debug.Log("WeaponView: installa com.unity.cloud.gltfast per il viewmodel 3D");
        }
#endif
    }

#if FALEZIA_GLTF
    // I GLB si caricano UNO ALLA VOLTA da una coda. Prima c'era un solo flag `_loading`:
    // se cambiavi arma durante un caricamento la richiesta andava persa, e il modello appena
    // caricato restava visibile anche se non era quello in mano (HUD "STANDARD" con l'Anvil in mano).
    private readonly List<string> _queue = new();
    private readonly HashSet<string> _missing = new();
    private bool _preloaded;

    private void Request(string id, bool first)
    {
        if (_cache.ContainsKey(id) || _missing.Contains(id)) return;
        _queue.Remove(id);
        if (first) _queue.Insert(0, id); else _queue.Add(id);
        Pump();
    }

    private void Pump()
    {
        if (_loading || _queue.Count == 0) return;
        string next = _queue[0];
        _queue.RemoveAt(0);
        _ = Load(next);
    }
#endif

#if FALEZIA_GLTF
    private async Task Load(string id)
    {
        _loading = true;
        string path = Path.Combine(Application.streamingAssetsPath, "weapons", id, id + ".glb");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"WeaponView: {path} non trovato");
            _missing.Add(id);
            _loading = false;
            Pump();
            return;
        }

        var gltf = new GltfImport();
        bool ok = await gltf.Load(path);
        if (!ok)
        {
            Debug.LogWarning($"WeaponView: import fallito per {id}");
            _missing.Add(id);
            _loading = false;
            Pump();
            return;
        }

        var holder = new GameObject(id);
        holder.SetActive(false);                      // nascosto finché non è l'arma in mano
        holder.transform.SetParent(transform, false);
        holder.transform.localScale = Vector3.one;   // la scala si applica sull'holder della camera, una volta sola

        var instantiator = new GameObjectInstantiator(gltf, holder.transform);
        await gltf.InstantiateMainSceneAsync(instantiator);
        GltfMaterials.ToUrp(holder);       // shader glTF → URP (armi e mani del viewmodel)

        if (PlayerRig.ViewmodelLayer >= 0)
            foreach (var t in holder.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = PlayerRig.ViewmodelLayer;

        foreach (var r in holder.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        var anim = holder.GetComponentInChildren<Animation>(true);
        if (anim == null) Debug.LogWarning($"WeaponView: {id} senza clip di animazione");

        if (Probe) Dump(id, holder);

        _cache[id] = holder;
        _loading = false;

        if (Arsenal.Get(_slot).Id == id && _current == null) Equip(_slot);
        Pump();
    }
#endif

    /// <summary>Diagnostica: `-faleziaProbe` stampa come è messa l'arma rispetto alla camera.</summary>
    public static bool Probe => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaProbe") >= 0;

    private void Dump(string id, GameObject holder)
    {
        transform.localPosition = hipPosition;
        transform.localRotation = Quaternion.Euler(hipRotation);
        transform.localScale = Vector3.one * scale;

        var cam = GetComponentInParent<Camera>();
        var camT = cam != null ? cam.transform : transform.parent;

        var eye = FindDeep(holder.transform, "socket_eye");
        var muzzle = FindDeep(holder.transform, "socket_muzzle");
        var aim = FindDeep(holder.transform, "socket_aim");

        string Local(Transform t) => t == null
            ? "assente"
            : camT.InverseTransformPoint(t.position).ToString("F3");

        Debug.Log($"[PROBE] {id}\n" +
                  $"  camera avanti = {camT.forward.x:F2} {camT.forward.y:F2} {camT.forward.z:F2}  (in mondo)\n" +
                  $"  arma rot (mondo) = {transform.eulerAngles.x:F1} {transform.eulerAngles.y:F1} {transform.eulerAngles.z:F1}  scala {transform.localScale.x:F2}\n" +
                  $"  socket_eye    (spazio camera) = {Local(eye)}\n" +
                  $"  socket_muzzle (spazio camera) = {Local(muzzle)}\n" +
                  $"  socket_aim    (spazio camera) = {Local(aim)}\n" +
                  $"  clip idle = {(_anim != null && _anim.GetClip("idle") != null ? "sì" : "NO")}");

        var anims = holder.GetComponentsInChildren<Animation>(true);
        Debug.Log($"[PROBE] {id}: {anims.Length} componenti Animation");
        foreach (var a in anims)
        {
            var names = new System.Text.StringBuilder();
            foreach (AnimationState st in a)
            {
                if (names.Length > 0) names.Append(',');
                names.Append(st.name);
            }
            Debug.Log($"[PROBE]   '{a.gameObject.name}' clip={a.GetClipCount()} [{names}]");
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all) if (t.name == name) return t;
        return null;
    }

    /// <summary>Fa partire una clip. Se non esiste, resta l'idle (niente errori).</summary>
    public void Play(string clip, float speed = 1f)
    {
        if (_anim == null || _playing == clip) return;
        if (_anim.GetClip(clip) == null) return;

        var state = _anim[clip];
        state.speed = speed;
        // l'idle gira in loop: con "Once" l'arma si fermava sull'ultimo fotogramma o
        // tornava alla posa di riposo (quella di "draw"), cioè inclinata di ~19°
        state.wrapMode = clip == "idle" ? WrapMode.Loop : WrapMode.Once;
        _anim.Play(clip);
        _playing = clip;
    }

    public void TriggerFire(float cycle)
    {
        Play("fire", 1f);
        if (_anim != null && _anim.GetClip("fire") is { } clip && clip.length > 0.01f)
            _anim["fire"].speed = Mathf.Clamp(clip.length / Mathf.Max(0.05f, cycle * 0.9f), 0.6f, 3f);
    }

    public void TriggerReload(bool empty) => Play(empty ? "reload_empty" : "reload", 1f);

    public void SetAds(float t) => _ads = Mathf.Clamp01(t);

    /// <summary>Contraccolpo: l'arma va indietro e su, poi rientra (indipendente dalla camera).</summary>
    public void Kick(float upDegrees)
    {
        _kickBack = Mathf.Min(0.05f, _kickBack + 0.016f);
        _kickUp = Mathf.Min(6f, _kickUp + upDegrees * 1.6f);
    }

    private void LateUpdate()
    {
        var rot = Quaternion.Slerp(Quaternion.Euler(hipRotation), Quaternion.Euler(adsRotation), _ads);
        var ads = adsPosition;


        // contraccolpo: si somma alla posa e rientra da solo
        _kickBack = Mathf.MoveTowards(_kickBack, 0f, 0.22f * Time.deltaTime);
        _kickUp = Mathf.MoveTowards(_kickUp, 0f, 42f * Time.deltaTime);
        rot *= Quaternion.Euler(-_kickUp, 0f, 0f);

        transform.localRotation = rot;
        transform.localPosition = Vector3.Lerp(hipPosition, ads, _ads) + Vector3.back * _kickBack;

        // IN MIRA L'ARMA NON SI CENTRA. Resta di fianco, come in mano a una persona vera:
        // si avvicina appena al centro (30%) e la visuale è la camera che zooma sul FOV
        // dell'ottica. Centrare l'arma sullo schermo è la cosa che fa "gioco di 15 anni fa".
        if (_ads > 0.001f)
        {
            // la posa di mira NON si accumula fra fotogrammi: si parte sempre da quella base
            if (!_adsCached) { _adsBase = adsPosition; _adsCached = true; }
            var inward = new Vector3(-_adsBase.x * 0.30f, 0f, 0f);
            transform.localPosition = Vector3.Lerp(hipPosition, _adsBase + inward, _ads) + Vector3.back * _kickBack;
        }
        transform.localScale = Vector3.one * scale;

        // se l'animazione si ferma (o se il modello è appena comparso) si torna all'idle:
        // senza questo l'arma resta nella posa di riposo, che è quella di "draw" (inclinata)
        if (_anim != null && !_anim.isPlaying && _playing != "idle")
        {
            _playing = "";
            Play("idle", 0.1f);
        }
    }
}
