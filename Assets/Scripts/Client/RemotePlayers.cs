using System.Collections.Generic;
using Falezia.Net;
using Falezia.Sim;
using UnityEngine;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// Gli ALTRI giocatori: una capsula per id, interpolata fra gli snapshot (i pacchetti
/// arrivano a 64 Hz, il render a 144: senza interpolazione si vedrebbero a scatti).
/// La vita è una barra sopra la testa, come per i bersagli della stanza debug.
/// </summary>
public class RemotePlayers : MonoBehaviour
{
    private sealed class Avatar
    {
        public GameObject Root;
        public Transform Body;
        public Transform Head;
        public Transform Bar;
        public Renderer BarRenderer;
        public Vector3 FromPos;
        public Vector3 ToPos;
        public float FromYaw, ToYaw;
        public float T;
        public bool Alive = true;

        // audio: l'ultimo valore visto dei contatori dello snapshot (8 bit, girano)
        public bool Heard;
        public int SeenShots, SeenReloads;

        // corpo dell'operatore: sostituisce le capsule appena il modello è caricato
        public OperatorView Operator;
        public PlayerState State;
        public Renderer BodyRenderer, HeadRenderer;
    }

    private readonly Dictionary<int, Avatar> _avatars = new();
    private static readonly Color TeamNorth = new(0.24f, 0.55f, 0.85f);
    private static readonly Color TeamSouth = new(0.86f, 0.38f, 0.22f);

    public void Apply(Snapshot snap, int selfId)
    {
        var alive = new HashSet<int>();

        foreach (var p in snap.State.Players.Values)
        {
            if (p.Id == selfId) continue;
            alive.Add(p.Id);

            if (!_avatars.TryGetValue(p.Id, out var a))
            {
                a = Create(p.Id, p.Team);
                _avatars[p.Id] = a;
            }

            a.FromPos = a.Body.position;
            a.ToPos = new Vector3(p.Pos.X, p.Pos.Y - Constants.Height * 0.5f, p.Pos.Z);
            a.FromYaw = a.Body.eulerAngles.y;
            a.ToYaw = p.Yaw * Mathf.Rad2Deg + 180f;
            a.T = 0f;
            a.Alive = p.Alive;
            a.State = p;
            a.Root.SetActive(true);

            // --- audio spaziale: gli eventi arrivano dai contatori, non si indovinano
            int shots = p.Weapon.Shots & 0xFF;
            int reloads = p.Weapon.ReloadStarts & 0xFF;
            if (!a.Heard)
            {
                a.Heard = true;
                a.SeenShots = shots;
                a.SeenReloads = reloads;
            }
            else
            {
                var def = Arsenal.Get(p.Weapon.Slot);
                Vector3 at = new Vector3(p.Pos.X, p.Pos.Y + Constants.Height * 0.5f - 0.25f, p.Pos.Z);

                int newShots = (shots - a.SeenShots) & 0xFF;
                if (newShots is > 0 and < 32 && p.Alive)
                {
                    if (def.Melee) SpatialAudio.I?.PlaySwing(def, at);
                    else SpatialAudio.I?.PlayShot(def, at);
                }

                if (((reloads - a.SeenReloads) & 0xFF) is > 0 and < 32 && p.Alive)
                    SpatialAudio.I?.PlayReload(def, at, empty: false);

                a.SeenShots = shots;
                a.SeenReloads = reloads;
            }

            // corpo dell'operatore: appena è pronto, le capsule grigie spariscono
            if (a.Operator != null && a.Operator.Ready)
            {
                if (a.BodyRenderer != null && a.BodyRenderer.enabled) a.BodyRenderer.enabled = false;
                if (a.HeadRenderer != null && a.HeadRenderer.enabled) a.HeadRenderer.enabled = false;
            }

            float h = Mathf.Clamp01(p.Health / PlayerState.MaxHealth);
            a.Bar.localScale = new Vector3(Mathf.Max(0.001f, 0.5f * h), 0.05f, 1f);
            if (a.BarRenderer != null)
                a.BarRenderer.material.color = Color.Lerp(Color.red, new Color(0.35f, 0.85f, 0.45f), h);
        }

        foreach (var kv in _avatars)
            if (!alive.Contains(kv.Key)) kv.Value.Root.SetActive(false);
    }

    private Avatar Create(int id, int team)
    {
        var root = new GameObject($"Giocatore {id}");
        root.transform.SetParent(transform, false);

        // il corpo vero (scheletro + animazioni procedurali); all'inizio resta nascosto
        var op = root.AddComponent<OperatorView>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "corpo";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.48f, Constants.Height * 0.5f, 0.48f);
        body.transform.localPosition = new Vector3(0, Constants.Height * 0.5f, 0);
        Object.Destroy(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial =
            Fx.Make(team == 1 ? TeamNorth : TeamSouth, 0.35f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "testa";
        head.transform.SetParent(root.transform, false);
        head.transform.localScale = Vector3.one * 0.26f;
        head.transform.localPosition = new Vector3(0, Constants.Height - 0.13f, 0);
        Object.Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().sharedMaterial = Fx.Make(new Color(0.78f, 0.66f, 0.55f), 0.3f);

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "vita";
        bar.transform.SetParent(root.transform, false);
        bar.transform.localPosition = new Vector3(0, Constants.Height + 0.22f, 0);
        bar.transform.localScale = new Vector3(0.5f, 0.05f, 0.02f);
        Object.Destroy(bar.GetComponent<Collider>());

        return new Avatar
        {
            Root = root,
            Body = root.transform,
            Head = head.transform,
            Bar = bar.transform,
            BarRenderer = bar.GetComponent<Renderer>(),
            BodyRenderer = body.GetComponent<Renderer>(),
            HeadRenderer = head.GetComponent<Renderer>(),
            Operator = op
        };
    }

    /// <summary>Posa interpolata di un giocatore remoto (piedi e imbardata Unity, in gradi).</summary>
    public bool TryGetPose(int id, out Vector3 feet, out float yawDegrees)
    {
        if (_avatars.TryGetValue(id, out var a) && a.Root.activeSelf)
        {
            feet = a.Body.position;
            yawDegrees = a.Body.eulerAngles.y;
            return true;
        }
        feet = Vector3.zero;
        yawDegrees = 0f;
        return false;
    }

    private void Update()
    {
        // animazione procedurale: gira a frame rate, non a 32 Hz di snapshot
        foreach (var kv in _avatars)
        {
            var a = kv.Value;
            if (a.Operator == null || a.State == null) continue;
            if (!a.Operator.Ready) continue;
            if (a.BodyRenderer != null && a.BodyRenderer.enabled) a.BodyRenderer.enabled = false;
            if (a.HeadRenderer != null && a.HeadRenderer.enabled) a.HeadRenderer.enabled = false;
            a.Operator.Apply(a.State, Time.deltaTime);
        }

        UpdateInterpolation();
    }

    private void UpdateInterpolation()
    {
        float k = 1f - Mathf.Exp(-18f * Time.deltaTime);   // inseguimento morbido
        foreach (var a in _avatars.Values)
        {
            if (!a.Root.activeSelf) continue;
            a.Body.position = Vector3.Lerp(a.Body.position, a.ToPos, k);
            float yaw = Mathf.LerpAngle(a.Body.eulerAngles.y, a.ToYaw, k);
            a.Body.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
