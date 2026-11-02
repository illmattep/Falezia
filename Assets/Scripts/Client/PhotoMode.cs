using System;
using System.Collections.Generic;
using System.IO;
using Falezia.Sim;
using UnityEngine;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// MODALITÀ FOTO — controllo qualità della grafica senza mani sul mouse.
///
/// Si attiva da riga di comando: <c>./Falezia.x86_64 -faleziaShots /tmp/shots</c>.
/// Il giocatore viene teletrasportato in punti presi dalla mappa (i "points" del JSON),
/// la camera viene tenuta ferma (niente caduta) e ogni inquadratura viene salvata in PNG.
/// Alla fine il gioco si chiude da solo. Serve a verificare mappa, ombre, armi e UI.
/// </summary>
public class PhotoMode : MonoBehaviour
{
    public const string Flag = "-faleziaShots";
    public const string CheckFlag = "-faleziaCheckInput";

    private struct Shot
    {
        public string Name;
        public Vec3 Eye;
        public float Yaw;
        public float Pitch;
        public bool Shop;
        public bool DebugPanel;
        public int Fire;
        public bool Ads;
        public bool FireHeld;
        public bool Pause;
        public int PausePage;
        public WeaponSlot? Weapon;
    }

    private readonly List<Shot> _shots = new();
    private string _dir;
    private PlayerRig _rig;
    private int _index;
    private float _wait;
    private int _phase;          // 0 = posa, 1 = scatta, 2 = respiro (la UI non cambia prima dello scatto)
    private Shot _current;
    private bool _done;

    public static string Directory()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == Flag) return args[i + 1];
        return null;
    }

    /// <summary>Avvia la verifica degli assi (non fa foto).</summary>
    public static bool CheckRequested =>
        System.Array.IndexOf(System.Environment.GetCommandLineArgs(), CheckFlag) >= 0;

    public void BeginCheck(PlayerRig rig)
    {
        _rig = rig;
        _checking = true;
        _checkStage = 0;
        _checkWait = 0.8f;   // lascia atterrare il giocatore e sistemare la camera
        _checkStart = rig.transform.position;
        _checkRight = rig.transform.right;
        _checkForward = rig.transform.forward;
    }

    private bool _checking;
    private int _checkStage;
    private float _checkWait;
    private Vector3 _checkStart;
    private Vector3 _checkRight;
    private Vector3 _checkForward;

    public void Begin(PlayerRig rig)
    {
        _rig = rig;
        _dir = Directory();
        if (string.IsNullOrEmpty(_dir)) return;

        System.IO.Directory.CreateDirectory(_dir);
        BuildShots(GameRoot.World);
        _current = _shots[0];
        Apply(_current);
        _wait = 2.0f;   // il viewmodel carica il GLB in modo asincrono: prima non si vedrebbe
    }

    /// <summary>Le inquadrature: punti veri della mappa, sguardo verso il centro dell'azione.</summary>
    private void BuildShots(WorldState world)
    {
        var spawn = world.SpawnsNorth.Count > 0 ? world.SpawnsNorth[0] : world.FallbackFeet;
        var pLargo = Point(world, "Largo", new Vec3(-6f, 0f, -55f));
        var pPiazza = Point(world, "Piazza Vecchia", new Vec3(6f, 0f, -16f));
        var pCassa = Point(world, "Cassa", new Vec3(0f, 0f, -2f));
        var pMercato = Point(world, "Mercato", new Vec3(22f, 0f, 0f));
        var pMolo = Point(world, "Molo", new Vec3(56f, 0f, -38f));
        var pPortico = Point(world, "Porticato", new Vec3(17f, 3.3f, 0f));

        _shots.Add(ShotAt(world, "01-porto", spawn, pLargo, 0f));

        // i due spawn visti VERSO L'ESTERNO: è lì che sta il loro carattere
        _shots.Add(ShotAt(world, "00-porto-retro", spawn, new Vec3(0f, 1.6f, -99f), 0f));

        // la bottega del Largo in primo piano: insegna, tenda, manifesto sulla stessa facciata
        _shots.Add(ShotAt(world, "02b-bottega", new Vec3(-3f, 0f, -49f), new Vec3(-8f, 2.4f, -52f), 2f));

        // Porto dal largo: capannone con tetto, container e gru nella stessa inquadratura
        _shots.Add(ShotAt(world, "00b-porto-largo", new Vec3(6f, 0f, -84f), new Vec3(-13f, 5f, -98f), 9f));
        var spawnSud = world.SpawnsSouth.Count > 0 ? world.SpawnsSouth[0] : world.FallbackFeet;
        _shots.Add(ShotAt(world, "01b-forte", spawnSud, new Vec3(0f, 2.0f, 99f), 0f));
        _shots.Add(ShotAt(world, "02-largo", pLargo, pPiazza, 0f));
        _shots.Add(ShotAt(world, "03-piazza", pPiazza, pCassa, 0f));
        _shots.Add(ShotAt(world, "04-mercato", pMercato, pPortico, -4f));
        _shots.Add(ShotAt(world, "05-molo", pMolo, pPortico, -2f));

        var high = new Vec3(0f, 26f, -52f);
        _shots.Add(new Shot { Name = "06-dall-alto", Eye = high, Yaw = YawTo(high, new Vec3(0f, 0f, -6f)), Pitch = -34f });

        _shots.Add(new Shot
        {
            Name = "07-negozio",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Shop = true,
        });
        _shots.Add(new Shot
        {
            Name = "08-muri",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = -4f,
            Fire = 10,
        });
        _shots.Add(new Shot
        {
            Name = "09-pavimento",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = -55f,
            Fire = 6,
        });
        // muro vicino: un raggio in 16 direzioni, poi ci si mette davanti lungo la NORMALE
        // della faccia colpita (arretrare lungo il raggio lascia dentro la parete)
        {
            var origins = new[] { spawn, pLargo, pPiazza, pMercato, pCassa };
            bool found = false;

            foreach (var origin in origins)
            {
                if (found) break;
                var from = new Vec3(origin.X, origin.Y + 1.55f, origin.Z);

                for (int i = 0; i < 16 && !found; i++)
                {
                    var dir = Arsenal.AimForward(i * Mathf.PI / 8f, 0f);
                    var hit = Trace.Ray(world, from, dir, 40f);
                    if (!hit.Hit || hit.Distance < 1.5f || hit.Distance > 20f) continue;

                    foreach (float back in new[] { 1.2f, 1.8f, 2.6f })
                    {
                        var feet = new Vec3(hit.Point.X + hit.Normal.X * back, origin.Y + 0.1f, hit.Point.Z + hit.Normal.Z * back);
                        if (!IsFree(world, new Vec3(feet.X, feet.Y + 1.55f, feet.Z))) continue;

                        var target = new Vec3(hit.Point.X, feet.Y + 1.55f, hit.Point.Z);
                        _shots.Add(new Shot
                        {
                            Name = "10-muro-vicino",
                            Eye = feet,
                            Yaw = YawTo(feet, target),
                            Pitch = 0f,
                            Fire = 9,
                        });
                        found = true;
                        Debug.Log($"[FALEZIA] muro di prova: {hit.Distance:F1} m, dietro {back:F1} m");
                        break;
                    }
                }
            }
        }

        _shots.Add(new Shot
        {
            Name = "11-mira-pistola",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Ads = true,
        });
        _shots.Add(new Shot
        {
            Name = "13-rinculo",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Weapon = WeaponSlot.Anvil,
            FireHeld = true,
        });
        _shots.Add(new Shot
        {
            Name = "14-pausa",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Pause = true,
        });
        _shots.Add(new Shot
        {
            Name = "15-grafica",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Pause = true,
            PausePage = 1,
        });
        _shots.Add(new Shot
        {
            Name = "12-mira-fucile",
            Eye = pPiazza,
            Yaw = YawTo(pPiazza, pCassa),
            Pitch = 0f,
            Ads = true,
            Weapon = WeaponSlot.Anvil,
        });
        _shots.Add(new Shot
        {
            Name = "10-debug",
            Eye = pLargo,
            Yaw = YawTo(pLargo, pPiazza),
            Pitch = -8f,
            DebugPanel = true,
        });
    }

    private static Shot ShotAt(WorldState world, string name, Vec3 feet, Vec3 lookAt, float pitch)
    {
        var eye = Free(world, new Vec3(feet.X, feet.Y + 1.5f, feet.Z));
        return new Shot
        {
            Name = name,
            Eye = eye,
            Yaw = YawTo(eye, lookAt),
            Pitch = pitch,
        };
    }

    /// <summary>
    /// I punti con nome stanno spesso a ridosso di un muro: qui si cerca il primo spazio
    /// libero intorno (anelli da 1,5 a 9 m) così la foto non finisce dentro un volume.
    /// </summary>
    private static Vec3 Free(WorldState world, Vec3 eye)
    {
        if (IsFree(world, eye)) return eye;

        for (float r = 1.5f; r <= 9f; r += 1.5f)
        {
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI / 8.0;
                var p = new Vec3(eye.X + (float)Math.Cos(a) * r, eye.Y, eye.Z + (float)Math.Sin(a) * r);
                if (IsFree(world, p)) return p;
            }
        }

        return new Vec3(eye.X, eye.Y + 6f, eye.Z);
    }

    /// <summary>
    /// Il posto è libero? `Query` filtra solo in pianta (griglia X/Z), quindi i candidati
    /// vanno ricontrollati in 3D: senza questo ogni punto sembrava occupato.
    /// </summary>
    private static bool IsFree(WorldState world, Vec3 eye)
    {
        var min = new Vec3(eye.X - 0.35f, eye.Y - 0.9f, eye.Z - 0.35f);
        var max = new Vec3(eye.X + 0.35f, eye.Y + 0.35f, eye.Z + 0.35f);

        foreach (int i in world.Query(min, max))
        {
            var b = world.Boxes[i];
            if (b.Min.X < max.X && b.Max.X > min.X &&
                b.Min.Y < max.Y && b.Max.Y > min.Y &&
                b.Min.Z < max.Z && b.Max.Z > min.Z) return false;
        }
        return true;
    }

    private static Vec3 Point(WorldState world, string name, Vec3 fallback)
    {
        foreach (var p in world.Points)
            if (p.Name == name) return p.Pos;
        return fallback;
    }

    private float _mapWait;

    private void Update()
    {
        if (_checking) { TickCheck(); return; }
        if (_rig == null || _done) return;

        // la prima foto aspetta ANCHE la pelle della mappa: senza, gli scatti
        // mostrano il blockout mentre i modelli stanno ancora caricando
        _mapWait += Time.unscaledDeltaTime;
        if (!MapProps.Ready && _mapWait < 60f)
        {
            _wait = 0.3f;
            _rig.Teleport(_current.Eye, _current.Yaw, _current.Pitch);
            return;
        }

        // la prima foto aspetta che il viewmodel abbia finito di caricare il GLB
        if (!_rig.View.Ready)
        {
            _wait = 0.3f;
            _rig.Teleport(_current.Eye, _current.Yaw, _current.Pitch);
            return;
        }

        if (_phase != 2) _rig.Teleport(_current.Eye, _current.Yaw, _current.Pitch);

        _wait -= Time.unscaledDeltaTime;
        if (_wait > 0f) return;

        if (_phase == 0)
        {
            // ora la posa è quella giusta: si spara da qui, non da dove eravamo prima
            if (_current.Fire > 0)
            {
                _rig.TestFire(_current.Fire);
                _phase = 1;
                _wait = 0.01f;      // scatto subito: le scie durano 55 ms
                return;
            }
            _phase = 1;
            _wait = 0.3f;
            return;
        }

        if (_phase == 1)
        {
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_dir, _current.Name + ".png"));
            var cam = Camera.main;
            var eye = _rig.View.EyeInCamera;
            Debug.Log($"[FALEZIA] foto {_current.Name} · ads {_rig.State.Weapon.AdsT:F2} · fov {(cam != null ? cam.fieldOfView : 0f):F0} · occhio {eye.x:F3} {eye.y:F3} {eye.z:F3} · mira {_rig.View.adsDistance:F2} · {_rig.View.PoseInfo}");
            _phase = 2;
            _wait = 0.35f;
            return;
        }

        _index++;
        if (_index >= _shots.Count)
        {
            _done = true;
            Debug.Log("[FALEZIA] foto finite: " + _dir);
            Invoke(nameof(Quit), 0.5f);
            return;
        }

        _current = _shots[_index];
        Apply(_current);
        _phase = 0;
        _wait = 0.55f;
    }

    private void Apply(Shot s)
    {
        Hud.SetMenuPhotos(s.Shop);
        Hud.SetDebugPhotos(s.DebugPanel);
        _rig.PhotoAds = s.Ads;
        _rig.PhotoFire = s.FireHeld;
        Hud.PauseOpen = s.Pause;
        Hud.SetPausePage(s.PausePage);
        if (s.Weapon.HasValue) _rig.PhotoEquip(s.Weapon.Value);
    }

    /// <summary>
    /// Prova strumentata degli assi: tiene premuto "destra" per 0,8 s e poi "avanti",
    /// e misura lo spostamento vero rispetto alla destra della camera. Serve a NON
    /// fidarsi dei segni a occhio.
    /// </summary>
    private void TickCheck()
    {
        _checkWait -= Time.unscaledDeltaTime;
        if (_checkWait > 0f) return;

        switch (_checkStage)
        {
            case 0:
                // la camera è al suo posto solo dopo qualche frame: si misura da qui
                _checkStart = _rig.transform.position;
                _checkRight = _rig.transform.right;
                _checkForward = _rig.transform.forward;
                _rig.PhotoMove = PlayerRig.MoveFor(false, true, false, false);   // tasto D vero
                _checkWait = 0.8f;
                _checkStage = 1;
                break;

            case 1:
                _rig.PhotoMove = null;
                var moved = _rig.transform.position - _checkStart;
                moved.y = 0f;
                float right = Vector3.Dot(moved, _checkRight);
                float fwd = Vector3.Dot(moved, _checkForward);
                Debug.Log($"[ASSI] tasto D → {right:+0.00;-0.00} m verso destra della camera " +
                          $"(avanti {fwd:+0.00;-0.00}) · {(right > 0.2f ? "OK" : "SBAGLIATO")}");

                _checkStart = _rig.transform.position;
                _rig.PhotoMove = PlayerRig.MoveFor(false, false, true, false);   // tasto W vero
                _checkWait = 0.8f;
                _checkStage = 2;
                break;

            case 2:
                _rig.PhotoMove = null;
                var f = _rig.transform.position - _checkStart;
                f.y = 0f;
                Debug.Log($"[ASSI] tasto W → {Vector3.Dot(f, _checkForward):+0.00;-0.00} m avanti " +
                          $"· {(Vector3.Dot(f, _checkForward) > 0.2f ? "OK" : "SBAGLIATO")}");
                // --- su/giù: pitch positivo deve guardare in ALTO (mouse su = +=)
                var feet = new Vec3(_rig.State.Pos.X, 0.1f, _rig.State.Pos.Z);
                _rig.Teleport(feet, 0f, 10f);
                float up = _rig.transform.forward.y;
                _rig.Teleport(feet, 10f, 0f);
                float turned = Vector3.Dot(_rig.transform.forward, Vector3.right);
                _rig.Teleport(feet, 0f, 0f);
                float straight = Vector3.Dot(_rig.transform.forward, Vector3.right);

                Debug.Log($"[ASSI] pitch +10 → sguardo {(up > 0.1f ? "in ALTO" : "in BASSO")} ({up:F2}) " +
                          $"· {(up > 0.1f ? "OK" : "SBAGLIATO")}");
                Debug.Log($"[ASSI] yaw +10 → {(turned < straight ? "verso destra" : "verso sinistra")} " +
                          $"(x {straight:F2} → {turned:F2}) · {(turned < straight ? "OK" : "SBAGLIATO")}");
                Debug.Log("[ASSI] prova finita");
                _checking = false;
                Invoke(nameof(Quit), 0.4f);
                break;
        }
    }

    private void Quit() => Application.Quit();

    private static float YawTo(Vec3 from, Vec3 to)
    {
        float dx = to.X - from.X, dz = to.Z - from.Z;
        return (float)(Math.Atan2(-dx, -dz) * Mathf.Rad2Deg);
    }
}
