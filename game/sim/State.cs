using System;
using System.Collections.Generic;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>Input di un tick. È l'unica cosa che il client ha il diritto di inviare.</summary>
    public struct InputCmd
    {
        public int Tick;
        public Vector2 Move;      // X = strafe, Y = avanti
        public float Yaw;
        public float Pitch;
        public bool Jump;
        public bool Sprint;
        public bool Crouch;
        public bool Ads;
        public bool Fire;    // semiauto: il client manda solo il fronte di salita
        public bool Reload;
        /// <summary>E tenuto premuto: aprire la cassa, prelevare, avviare, rimuovere il Nucleo.</summary>
        public bool Interact;
    }

    /// <summary>Stato di un giocatore. <see cref="Pos"/> è il CENTRO dell'AABB, non i piedi.</summary>
    public sealed class PlayerState
    {
        public int Id;
        public Vec3 Pos;
        public Vec3 Vel;
        public float Yaw;
        public float Pitch;
        public bool OnFloor;
        public bool Crouching;
        public float Stamina = Constants.StaminaMax;
        public float JumpCd;

        // --- partita
        /// <summary>1 = Nord, 2 = Sud. Le squadre servono ai round, allo spawn e al punteggio.</summary>
        public int Team = 1;
        /// <summary>Vita. A zero si muore: la gestisce il server, il client la legge dallo snapshot.</summary>
        public float Health = MaxHealth;
        public bool Alive = true;
        public int Kills;
        public int Deaths;
        /// <summary>Crediti di partita (economia del round).</summary>
        public int Credits = 800;

        /// <summary>
        /// La specializzazione scelta nel draft (doc 10). È **pubblica**: viaggia nello snapshot
        /// come le posizioni, perché chi la vede può rispondere.
        /// </summary>
        public Specialization Spec = Specialization.None;

        /// <summary>L'arma primaria dichiarata nel draft: un intento, non un regalo (le armi si comprano).</summary>
        public byte Intent;

        /// <summary>Sta tenendo premuto il gesto di interazione (cassa, Nucleo). Lo decide l'input.</summary>
        public bool Interacting;

        public const float MaxHealth = 100f;

        /// <summary>Arma equipaggiata e sue munizioni. Vive nella simulazione, non nel client.</summary>
        public WeaponState Weapon = new WeaponState();

        /// <summary>
        /// Le munizioni restano NELL'ARMA: cambiando arma il caricatore non si riempie da solo
        /// (altrimenti basterebbe premere 1 e 2 per avere munizioni infinite). Una voce per slot,
        /// creata la prima volta che prendi quell'arma in mano.
        /// </summary>
        public WeaponState[] Cache = new WeaponState[Arsenal.Count];

        public void CopyFrom(PlayerState o)
        {
            Id = o.Id; Pos = o.Pos; Vel = o.Vel; Yaw = o.Yaw; Pitch = o.Pitch;
            OnFloor = o.OnFloor; Crouching = o.Crouching; Stamina = o.Stamina; JumpCd = o.JumpCd;
            Team = o.Team; Health = o.Health; Alive = o.Alive; Kills = o.Kills; Deaths = o.Deaths;
            Credits = o.Credits; Spec = o.Spec; Intent = o.Intent;
            Weapon = o.Weapon;
            Cache = (WeaponState[])o.Cache.Clone();
        }

        public PlayerState Clone()
        {
            var p = new PlayerState();
            p.CopyFrom(this);
            return p;
        }
    }

    /// <summary>
    /// Scatola statica del mondo. Niente mesh, niente fisica del motore.
    /// <see cref="Kind"/> è solo l'indice del materiale per chi disegna: la simulazione lo ignora.
    /// </summary>
    public struct Box
    {
        public Vec3 Min;
        public Vec3 Size;
        public byte Kind;
        public Vec3 Max => Min + Size;
        public Vec3 Center => Min + Size * 0.5f;
    }

    /// <summary>Un punto con un nome: spawn, teleport della stanza debug, obiettivi.</summary>
    /// <summary>
    /// Una rotta verificata della mappa: dal punto di partenza, una fila di waypoint (x,z).
    /// La usa il bot per spostarsi senza attraversare i muri, e i test per misurare i tempi.
    /// </summary>
    public sealed class Route
    {
        public string Name = "";
        public Vec3 From;
        public readonly List<Vec3> Waypoints = new();

        /// <summary>La lunghezza percorsa, in metri (solo XZ).</summary>
        public float Length
        {
            get
            {
                float total = 0f;
                var prev = From;
                foreach (var wp in Waypoints)
                {
                    total += new Vec3(wp.X - prev.X, 0f, wp.Z - prev.Z).Length();
                    prev = wp;
                }
                return total;
            }
        }
    }

    public struct NamedPoint
    {
        public string Name;
        public Vec3 Pos;
    }

    /// <summary>
    /// Mondo statico. Client e server costruiscono lo STESSO mondo dagli stessi dati
    /// (la stessa funzione, o lo stesso file di mappa), nello stesso ordine.
    /// </summary>
    public sealed class WorldState
    {
        public readonly List<Box> Boxes = new();

        // --- solo dati di presentazione e di regia: la simulazione non li legge
        public string Name = "test";
        public string[] Kinds = Array.Empty<string>();
        public readonly List<Box> Decor = new();
        public readonly List<Vec3> SpawnsNorth = new();
        public readonly List<Vec3> SpawnsSouth = new();
        public readonly List<NamedPoint> Points = new();
        /// <summary>Le rotte verificate della mappa (game/maps/*.json): le segue il bot.</summary>
        public readonly List<Route> Routes = new();

        /// <summary>Dove rientra chi cade fuori dal mondo (piedi). Non è un respawn: è una rete di sicurezza.</summary>
        public Vec3 FallbackFeet = new(0f, 0.1f, 0f);

        public void AddBox(Vec3 min, Vec3 size, byte kind = 0)
        {
            Boxes.Add(new Box { Min = min, Size = size, Kind = kind });
            _grid = null;
        }

        public static WorldState MakeTestWorld()
        {
            var w = new WorldState();
            w.AddBox(new Vec3(-60, -1, -60), new Vec3(120, 1, 120));   // pavimento
            w.AddBox(new Vec3(-10, 0, -6), new Vec3(6, 3, 12));        // muro lungo (-X)
            w.AddBox(new Vec3(2, 0, 5), new Vec3(2.5f, 1, 2.5f));      // cassa 1 m
            w.AddBox(new Vec3(8, 0, -4), new Vec3(2, 5, 2));           // torre 5 m
            w.AddBox(new Vec3(-4, 0, 8), new Vec3(6, 0.4f, 2));        // gradino 40 cm
            w.AddBox(new Vec3(0, 0, -10), new Vec3(8, 1.1f, 0.4f));    // muretto 1,1 m
            // i punti della stanza debug (piedi)
            w.Points.Add(new NamedPoint { Name = "PIAZZA", Pos = new Vec3(0f, 0f, 3f) });
            w.Points.Add(new NamedPoint { Name = "CASSA", Pos = new Vec3(3.25f, 1.0f, 6.25f) });
            w.Points.Add(new NamedPoint { Name = "TORRE", Pos = new Vec3(9f, 5.0f, -3f) });
            w.Points.Add(new NamedPoint { Name = "MURO", Pos = new Vec3(-7f, 3.0f, 0f) });
            w.Points.Add(new NamedPoint { Name = "GRADINO", Pos = new Vec3(-1f, 0.4f, 9f) });
            w.SpawnsNorth.Add(new Vec3(0f, 0.1f, 3f));
            w.FallbackFeet = new Vec3(0f, 0.1f, 3f);
            return w;
        }

        // ================================================================ griglia
        //
        // Con una mappa vera (centinaia di scatole) provare TUTTE le scatole a ogni asse
        // di ogni tick di ogni giocatore, più i replay della riconciliazione, è spreco.
        // La griglia restituisce solo le scatole vicine, IN ORDINE DI INDICE: lo stesso
        // ordine della lista completa, quindi il risultato è identico bit per bit.
        // Non è thread-safe (usa buffer interni): un WorldState per thread.

        private const float CellSize = 4f;
        private Dictionary<long, int[]> _grid;
        private int[] _stamp = Array.Empty<int>();
        private int _generation;
        private readonly List<int> _scratch = new(64);

        private static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

        private void BuildGrid()
        {
            var cells = new Dictionary<long, List<int>>();
            for (int i = 0; i < Boxes.Count; i++)
            {
                var b = Boxes[i];
                int x0 = (int)MathF.Floor(b.Min.X / CellSize), x1 = (int)MathF.Floor(b.Max.X / CellSize);
                int z0 = (int)MathF.Floor(b.Min.Z / CellSize), z1 = (int)MathF.Floor(b.Max.Z / CellSize);
                for (int cx = x0; cx <= x1; cx++)
                    for (int cz = z0; cz <= z1; cz++)
                    {
                        long k = Key(cx, cz);
                        if (!cells.TryGetValue(k, out var list)) cells[k] = list = new List<int>();
                        list.Add(i);
                    }
            }
            _grid = new Dictionary<long, int[]>(cells.Count);
            foreach (var kv in cells) _grid[kv.Key] = kv.Value.ToArray();
            _stamp = new int[Boxes.Count];
            _generation = 0;
        }

        /// <summary>
        /// Indici delle scatole che POTREBBERO toccare il volume [min, max] (in pianta),
        /// in ordine crescente. La lista restituita è riusata alla chiamata successiva.
        /// </summary>
        public List<int> Query(Vec3 min, Vec3 max)
        {
            if (_grid == null) BuildGrid();
            _scratch.Clear();
            if (++_generation == int.MaxValue)
            {
                Array.Clear(_stamp, 0, _stamp.Length);
                _generation = 1;
            }

            int x0 = (int)MathF.Floor(min.X / CellSize), x1 = (int)MathF.Floor(max.X / CellSize);
            int z0 = (int)MathF.Floor(min.Z / CellSize), z1 = (int)MathF.Floor(max.Z / CellSize);
            for (int cx = x0; cx <= x1; cx++)
                for (int cz = z0; cz <= z1; cz++)
                {
                    if (!_grid.TryGetValue(Key(cx, cz), out var arr)) continue;
                    foreach (int i in arr)
                    {
                        if (_stamp[i] == _generation) continue;
                        _stamp[i] = _generation;
                        _scratch.Add(i);
                    }
                }
            _scratch.Sort();
            return _scratch;
        }
    }

    /// <summary>Stato completo della simulazione. Oggetto puro, serializzabile, clonabile.</summary>
    public sealed class SimState
    {
        public int Tick;
        public readonly Dictionary<int, PlayerState> Players = new();

        /// <summary>
        /// Le armi a terra, con le loro munizioni. Fisica nostra (gravità + traccia sul
        /// pavimento), niente rigidbody: la stessa su client, server e test.
        /// </summary>
        public readonly List<GroundWeapon> Drops = new();

        /// <summary>Stanza di prova: i soldi non finiscono mai. Non viaggia in rete.</summary>
        public bool UnlimitedMoney;

        private int _nextDropId = 1;
        public int NextDropId() => _nextDropId++;

        public PlayerState Get(int id) => Players.TryGetValue(id, out var p) ? p : null;

        public PlayerState GetOrCreate(int id)
        {
            if (!Players.TryGetValue(id, out var p))
            {
                p = new PlayerState { Id = id };
                Players[id] = p;
            }
            return p;
        }

        public SimState Clone()
        {
            var s = new SimState { Tick = Tick, UnlimitedMoney = UnlimitedMoney };
            foreach (var kv in Players) s.Players[kv.Key] = kv.Value.Clone();
            foreach (var d in Drops) s.Drops.Add(d.Copy());
            s._nextDropId = _nextDropId;
            return s;
        }
    }
}
