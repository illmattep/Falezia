using UnityEngine;

/// <summary>
/// LE BRACCIA IN PRIMA PERSONA.
///
/// Sono le braccia dello stesso operatore della terza persona (stesso modello, stesse maniche):
/// dal mesh si tengono SOLO i triangoli che pesano su braccio, avambraccio, mano e dita
/// (<see cref="OperatorRig.KeepOnlyArms"/>), così niente busto né testa davanti alla camera.
///
/// Le mani vanno sui punti dell'arma in mano (`socket_hand_r`, `socket_hand_l`), qualunque
/// cosa faccia l'arma: idle, sparo, ADS, contraccolpo. In ricarica la mano sinistra segue il
/// CARICATORE animato dalla clip dell'arma (nodo `mag`) e poi torna al guardamano.
///
/// Le spalle non si vedono: stanno sotto il bordo dello schermo e, se un bersaglio è troppo
/// lontano (un fucile lungo in ADS), scivolano in avanti invece di lasciare la mano a metà strada.
///
/// Gira DOPO <see cref="WeaponView"/> (ordine di esecuzione), così legge la posa vera
/// dell'arma in quel fotogramma, animazione compresa.
/// </summary>
[DefaultExecutionOrder(1000)]
public class FirstPersonArms : MonoBehaviour
{
    private const float Scale = 0.62f;       // mani più piccole: visibili ma non invadenti

    /// <summary>
    /// Le braccia sono SPENTE di default: il modello disponibile oggi è una scansione del corpo,
    /// con la pelle a tinta piatta, e in prima persona sembra una mano gigante. Il viewmodel
    /// dell'arma da solo è pulito. Si riaccendono senza ricompilare:
    ///   PlayerPrefs.SetInt("viewmodel.arms", 1)
    /// Quando arriverà un modello di sole mani/guanti, basta girarlo a 1 di default.
    /// </summary>
    public static bool Enabled
    {
        get => PlayerPrefs.GetInt("viewmodel.arms", 1) == 1;   // accese di default, discrete
        set { PlayerPrefs.SetInt("viewmodel.arms", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    private PlayerRig _player;
    private WeaponView _view;
    private Transform _cam;
    private Transform _root;
    private OperatorRig _rig;
    private bool _failed;

    public void Init(PlayerRig player, WeaponView view)
    {
        _player = player;
        _view = view;
        _cam = view != null ? view.transform.parent : transform;
        OperatorView.Preload();
    }

    private void Build()
    {
        if (!Enabled || !OperatorView.UseBody) { _failed = true; return; }

        var template = OperatorView.Template;
        if (template == null || _cam == null) return;

        _root = new GameObject("Braccia (prima persona)").transform;
        _root.SetParent(_cam, false);
        _root.localRotation = Quaternion.identity;
        _root.localScale = Vector3.one * Scale;

        var body = Instantiate(template, _root, false);
        body.name = "braccia";
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;
        body.SetActive(true);

        int layer = PlayerRig.ViewmodelLayer >= 0 ? PlayerRig.ViewmodelLayer : _cam.gameObject.layer;
        foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        _root.gameObject.layer = layer;

        _rig = OperatorRig.Bind(body, _root);
        if (_rig == null || !_rig.KeepOnlyArms())
        {
            Debug.LogWarning("FirstPersonArms: modello non adatto (ossa o mesh non leggibili): niente braccia");
            Destroy(_root.gameObject);
            _root = null;
            _failed = true;
            return;
        }
        foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.updateWhenOffscreen = true;
        }

        // l'occhio del modello sulla camera
        _root.localPosition = -_rig.RestEye * Scale;
    }

    private void LateUpdate()
    {
        if (_failed || _view == null) return;
        if (_root == null) { Build(); if (_root == null) return; }

        var hr = _view.SocketHandR;
        bool show = _view.isActiveAndEnabled && _view.Ready && hr != null &&
                    (_player == null || _player.State == null || _player.State.Alive);
        if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
        if (!show) return;

        var r = _rig;
        r.ResetPose();
        float s = Scale;

        // assi dell'arma dai socket: avanti = impugnatura → volata
        var mz = _view.SocketMuzzle;
        Vector3 d = (mz != null ? mz.position - hr.position : _cam.forward).normalized;
        Vector3 u = hr.up;
        u = (u - Vector3.Dot(u, d) * d).normalized;
        Vector3 rr = Vector3.Cross(u, d);

        // Le spalle stanno sotto gli angoli bassi dello schermo (il mesh delle braccia è staccato
        // dal busto: si possono mettere dove servono). Gomiti in giù: l'avambraccio non si attorciglia.
        r.Right.Upper.position = _cam.TransformPoint(ShoulderR);
        r.Left.Upper.position = _cam.TransformPoint(ShoulderL);
        Vector3 poleR = _cam.TransformPoint(new Vector3(0.70f, -1.00f, 0.00f));
        Vector3 poleL = _cam.TransformPoint(new Vector3(-0.40f, -1.00f, 0.20f));

        // destra: impugnatura
        Vector3 fR = OperatorView.GripFingers(d, u);
        Vector3 wristR = OperatorView.GripWrist(hr.position, fR, rr, s);
        Reach(r.Right, wristR);
        r.SolveArm(r.Right, wristR, poleR, -rr);
        r.OrientHand(r.Right, fR, -rr);
        r.Curl(r.Right, _view.IsMelee ? 0.95f : 0.85f);

        // sinistra
        var hl = _view.SocketHandL;
        if (_view.IsMelee || hl == null)
        {
            // coltello: la sinistra resta fuori dall'inquadratura
            Vector3 low = _cam.TransformPoint(new Vector3(-0.30f, -0.62f, 0.10f));
            Reach(r.Left, low);
            r.SolveArm(r.Left, low, poleL, _cam.right);
            r.Curl(r.Left, 0.6f);
            return;
        }

        bool pistol = Vector3.Distance(hl.position, hr.position) < 0.15f;
        Vector3 fL = OperatorView.LeftFingers(!pistol, d, u, rr);
        Vector3 pL = OperatorView.LeftPalm(!pistol, u, rr);
        Vector3 hold = OperatorView.LeftWristAt(hl.position, !pistol, d, u, rr, s);

        // ricarica: la mano va al caricatore animato e lo accompagna, poi torna
        float ph = _view.ReloadPhase;
        var mag = _view.Magazine;
        Vector3 target = hold;
        float grab = 0f;
        if (ph >= 0f && mag != null)
        {
            grab = Smooth((ph - 0.04f) / 0.14f) * (1f - Smooth((ph - 0.78f) / 0.16f));
            Vector3 atMag = mag.position - u * 0.05f * s;
            target = Vector3.Lerp(hold, atMag, grab);
            fL = Vector3.Slerp(fL, (u + d * 0.2f).normalized, grab);
            pL = Vector3.Slerp(pL, (rr * 0.6f + d * 0.4f).normalized, grab);
        }
        Reach(r.Left, target);
        r.SolveArm(r.Left, target, poleL, pL);
        r.OrientHand(r.Left, fL, pL);
        r.Curl(r.Left, Mathf.Lerp(0.72f, 0.6f, grab));
    }

    private static readonly Vector3 ShoulderR = new(0.36f, -0.50f, -0.02f);
    private static readonly Vector3 ShoulderL = new(-0.36f, -0.50f, -0.02f);

    /// <summary>
    /// Bersaglio fuori portata (fucile lungo, mano sul guardamano lontano): la spalla avanza
    /// IN AVANTI (resta bassa, fuori dallo schermo); se non basta, di lato verso il bersaglio.
    /// </summary>
    private void Reach(OperatorRig.Arm arm, Vector3 target)
    {
        if (arm.Upper == null) return;
        float reach = (arm.UpperLen + arm.ForeLen) * 0.97f;
        Vector3 s0 = arm.Upper.position;
        Vector3 v = s0 - target;
        if (v.magnitude <= reach) return;
        Vector3 side = -v - Vector3.Dot(-v, _cam.up) * _cam.up;
        if (Slide(arm, s0, v, _cam.forward, reach) || Slide(arm, s0, v, side.normalized, reach)) return;
        arm.Upper.position = target + v.normalized * reach;
    }

    private static bool Slide(OperatorRig.Arm arm, Vector3 s0, Vector3 v, Vector3 f, float reach)
    {
        float b = Vector3.Dot(v, f);
        float disc = b * b - (v.sqrMagnitude - reach * reach);
        if (disc < 0f) return false;
        arm.Upper.position = s0 + f * (-b - Mathf.Sqrt(disc));
        return true;
    }

    private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
    }
}
