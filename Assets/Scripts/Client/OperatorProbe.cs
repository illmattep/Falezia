using Falezia.Sim;
using UnityEngine;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// CONTROLLO QUALITÀ DEL CORPO DEGLI OPERATORI (`-faleziaOperatorProbe`).
///
/// Mette tre operatori davanti alla telecamera — fermo col fucile, in camminata con la
/// pistola, accovacciato con la SMG — aspetta che il modello sia caricato e salva uno
/// screenshot, poi chiude. Serve a guardare la posa, l'arma in mano e le proporzioni
/// senza dover organizzare una partita con due client.
///
///     ./build/linux/Falezia.x86_64 -faleziaOperatorProbe -screen-width 960 -screen-height 720
/// </summary>
public class OperatorProbe : MonoBehaviour
{
    private float _t;
    private bool _spawned;
    private int _shot;
    private Vector3 _centre;
    private string _path = "/tmp/op/unity_operator.png";

    private void Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-faleziaOperatorShot");
        if (i >= 0 && i + 1 < args.Length) _path = args[i + 1];
    }

    private void Update()
    {
        _t += Time.deltaTime;

        if (!_spawned && OperatorView.TemplateReady && _t > 0.3f)
        {
            Spawn();
            _spawned = true;
            _t = 0f;
            return;
        }

        if (_spawned && _shot == 0 && _t > 1.2f)
        {
            _shot = 1;
            Shot("_front");
            _t = 0f;
            // secondo scatto dal FIANCO: è l'unico modo per giudicare la posa di un fucile
            var p = GameRoot.Player;
            if (p != null)
            {
                var right = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                var feet = _centre + right * 5.5f;
                float yaw = Mathf.Atan2(-(_centre.x - feet.x), -(_centre.z - feet.z)) * Mathf.Rad2Deg;
                p.Teleport(new Vec3(feet.x, feet.y, feet.z), yaw, -13f);
            }
            return;
        }

        if (_shot == 1 && _t > 0.7f)
        {
            _shot = 2;
            Shot("_side");
            _t = 0f;
            return;
        }

        // terzo scatto: la terza persona di debug (Alt), per vedere il corpo del giocatore
        if (_shot == 2 && _t > 0.9f)
        {
            _shot = 3;
            if (GameRoot.Player != null) GameRoot.Player.SetThirdPerson(true);
            Shot("_third");
            Invoke(nameof(Bye), 0.7f);
        }
    }

    private void Shot(string suffix)
    {
        string path = System.IO.Path.ChangeExtension(_path, null) + suffix + ".png";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? "/tmp");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[OPERATORI] screenshot {path}");
    }

    private void Bye()
    {
        Debug.Log("[OPERATORI] fine");
        Application.Quit();
    }

    private void Spawn()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[OPERATORI] camera assente"); return; }

        // posto fisso e aperto: lo spawn Nord, guardando il centro. Altrimenti i manichini
        // finiscono dentro un cancello e lo screenshot non serve a niente.
        var player = GameRoot.Player;
        if (player != null && GameRoot.World != null && GameRoot.World.SpawnsNorth.Count > 0)
        {
            var s0 = GameRoot.World.SpawnsNorth[0];
            player.Teleport(new Vec3(s0.X, s0.Y, s0.Z), player.State.Yaw * Mathf.Rad2Deg, 0f);
        }

        Vector3 fwd = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 basePos = cam.transform.position + fwd * 3.6f;

        // piedi a terra: la mappa non ha collider, si usa la traccia della simulazione
        var world = GameRoot.World;
        if (world != null)
        {
            var o = new Vec3(basePos.x, basePos.y + 4f, basePos.z);
            var hit = Trace.Ray(world, o, new Vec3(0f, -1f, 0f), 12f);
            if (hit.Hit) basePos.y = hit.Point.Y;
        }

        _centre = basePos;
        // 1. fermo, fucile, verso la camera
        Make(basePos - right * 1.15f, WeaponSlot.Anvil, 0f, false, 0f);
        // 2. in camminata con la pistola
        Make(basePos, WeaponSlot.Standard, Constants.WalkSpeed, false, 1.2f);
        // 3. accovacciato con la SMG
        Make(basePos + right * 1.15f, WeaponSlot.Cadence, 0f, true, 2.4f);

        Debug.Log("[OPERATORI] tre corpi in scena");
    }

    private void Make(Vector3 at, WeaponSlot slot, float speed, bool crouch, float reloadPhase)
    {
        var p = new PlayerState
        {
            Id = 900 + (int)slot,
            Pos = new Vec3(at.x, at.y - 1.3f, at.z),      // piedi a terra: il centro è ~0,9 m sopra
            Vel = new Vec3(0f, 0f, speed),
            Crouching = crouch,
            OnFloor = true,
            Alive = true,
            Health = 100f,
        };
        p.Weapon.Refill(Arsenal.Get(slot));

        // la ricarica è un evento: la faccio partire adesso così la posa si vede
        if (reloadPhase > 0f) p.Weapon.ReloadStarts = 1;

        var go = new GameObject($"Operatore prova {slot}");
        go.transform.position = at;
        // il modello guarda verso +Z: per vederlo in faccia si punta +Z VERSO la camera
        go.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - at, Vector3.up);
        var view = go.AddComponent<OperatorView>();
        go.AddComponent<ProbeDriver>().Bind(view, p, reloadPhase > 0f);
    }

    /// <summary>Tiene vivo lo stato del manichino e lo anima.</summary>
    private class ProbeDriver : MonoBehaviour
    {
        private OperatorView _view;
        private PlayerState _p;
        private float _t;

        public void Bind(OperatorView view, PlayerState p, bool reload)
        {
            _view = view;
            _p = p;
            if (reload) _t = -0.6f;    // la ricarica parte dopo mezzo secondo, così la posa si vede
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_p.Weapon.ReloadStarts == 0 && _t > 0f) _p.Weapon.ReloadStarts = 1;
            if (_view != null && _view.Ready) _view.Apply(_p, Time.deltaTime);
        }
    }
}
