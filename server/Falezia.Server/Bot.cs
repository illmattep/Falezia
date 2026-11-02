using System.Numerics;
using Falezia.Sim;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Server;

/// <summary>
/// UN BOT CHE GIOCA L'OBIETTIVO. Non è un'IA da vendere: è lo strumento che permette di
/// vedere il ciclo del round funzionare senza dieci persone collegate.
///
/// Sa fare tre cose: camminare sulle **rotte verificate** della mappa (quelle di
/// `game/maps/calanera.json`, già usate dai test), tenere premuto il gesto quando serve
/// (aprire, prelevare, avviare, rimuovere), e sparare al nemico più vicino con un minimo
/// di tremolio nella mira. Non usa la rete: è un giocatore come gli altri dentro la simulazione.
/// </summary>
public sealed class Bot
{
    public int Id { get; }
    public int Team { get; }
    /// <summary>Contatore dei colpi già risolti, come per i client.</summary>
    public int SeenShots;

    private int _tick;
    private Route _route;
    private int _wp;
    private Vec3 _goal;
    private bool _hasGoal;
    private float _stuck;
    private float _lastRepath;

    public Bot(int id, int team)
    {
        Id = id;
        Team = team;
    }

    /// <summary>La zona assegnata: Nord va alla Grotta (Est), Sud alla Galleria (Ovest).</summary>
    public int Zone => Team == 1 ? Objective.ZoneEast : Objective.ZoneWest;

    public void ForgetRoute()
    {
        _route = null;
        _wp = 0;
        _hasGoal = false;
        _stuck = 0f;
    }

    public InputCmd Think(SimState s, MatchState m, WorldState w, float dt)
    {
        var cmd = new InputCmd { Tick = ++_tick };
        var me = s.Get(Id);
        if (me == null || !me.Alive) return cmd;

        if (!TryPlan(s, me, m, w, out var goal, out float stop, out bool interact))
        {
            return cmd;
        }

        float distance = Horizontal(me.Pos, goal);

        // ---------------------------------------------------------- movimento
        if (distance > stop)
        {
            var next = NextWaypoint(me, w, goal, dt);
            cmd.Yaw = YawTo(me.Pos, next);
            cmd.Move = new Vector2(0f, 1f);
            cmd.Sprint = distance > 14f && !Objective.IsCarrier(m, Id);
            // se è incastrato su un gradino o un muretto, prova a saltare invece di spingere
            if (_stuck > 1.0f) cmd.Jump = true;
        }
        else
        {
            cmd.Move = Vector2.Zero;
            cmd.Yaw = YawTo(me.Pos, goal);
        }

        if (interact && distance <= stop + 0.6f) cmd.Interact = true;

        // ---------------------------------------------------------- combattimento
        var enemy = NearestEnemy(s);
        if (enemy != null && !cmd.Interact)
        {
            float ed = Horizontal(me.Pos, enemy.Pos);
            if (ed < 30f)
            {
                // mira al petto con un tremolio lento: non è un cecchino
                float jitter = MathF.Sin(_tick * 0.07f + Id * 1.7f) * 0.035f;
                cmd.Yaw = YawTo(me.Pos, enemy.Pos) + jitter;
                cmd.Pitch = PitchTo(me, enemy);
                cmd.Fire = ed < 26f && (_tick % 5) < 2;      // fronti di salita, non tenere premuto
            }
        }

        return cmd;
    }

    // ------------------------------------------------------------------ piano

    /// <summary>Cosa devo fare adesso: dove andare, quando fermarmi, se tenere premuto E.</summary>
    private bool TryPlan(SimState s, PlayerState me, MatchState m, WorldState w, out Vec3 goal, out float stop, out bool interact)
    {
        goal = me.Pos;
        stop = 1.2f;
        interact = false;

        var cassa = Objective.FindPoint(w, "cassa");
        if (!cassa.HasValue) return false;

        switch (m.Core)
        {
            case CorePhase.Closed:
                goal = cassa.Value;
                stop = Objective.CassaRadius - 0.5f;
                interact = true;
                return true;

            case CorePhase.Opened:
                goal = cassa.Value;
                stop = 1.0f;
                interact = true;
                return true;

            case CorePhase.Dropped:
                goal = m.CorePos;
                stop = 1.0f;
                interact = true;
                return true;

            case CorePhase.Carried:
                if (m.CoreOwner == Id)
                {
                    goal = ZonePoint(w) ?? cassa.Value;
                    stop = 1.2f;
                    interact = true;                       // in zona: avvia l'estrazione
                    return true;
                }
                var carrier = s.Get(m.CoreOwner);
                if (carrier == null) return false;

                if (m.CoreTeam == Team)
                {
                    // Scorta: non si segue il portatore a naso, si va alla STESSA zona.
                    // Seguirlo dritto significa incastrarsi su un muro che lui ha aggirato.
                    goal = ZoneFor(w, m.CoreTeam) ?? carrier.Pos;
                    stop = 6f;
                    return true;
                }

                goal = carrier.Pos;                        // intercetta: qui la linea retta va bene
                stop = 3f;
                return true;

            case CorePhase.Siege:
                goal = m.CorePos;
                stop = 1.6f;
                interact = Team != m.CoreTeam;             // gli assaltanti rimuovono il Nucleo
                return true;
        }
        return false;
    }

    private Vec3? ZonePoint(WorldState w) => ZoneFor(w, Team);

    /// <summary>La zona di estrazione di una squadra: Nord → Grotta (Est), Sud → Galleria (Ovest).</summary>
    private static Vec3? ZoneFor(WorldState w, int team)
        => Objective.FindPoint(w, team == 1 ? "grotta" : "galleria");

    // ------------------------------------------------------------- navigazione

    /// <summary>
    /// Il prossimo punto da raggiungere. Sceglie la rotta verificata che costa meno
    /// (distanza dall'inizio + distanza dalla fine all'obiettivo) e la segue waypoint per waypoint.
    /// Se non c'è una rotta sensata, va dritto: meglio che restare fermi.
    /// </summary>
    private Vec3 NextWaypoint(PlayerState me, WorldState w, Vec3 goal, float dt)
    {
        _lastRepath += dt;

        if (!_hasGoal || Horizontal(_goal, goal) > 3f)
        {
            _goal = goal;
            _hasGoal = true;
            _route = null;
        }

        if (_route == null && _lastRepath > 0.5f)
        {
            _lastRepath = 0f;
            _route = BestRoute(w, me.Pos, goal, out _wp);
        }

        // incastrato contro un muro? salta il waypoint e riprova
        if (me.Vel.LengthSquared() < 0.04f) _stuck += dt; else _stuck = 0f;
        if (_stuck > 1.2f)
        {
            _stuck = 0f;
            _wp++;
            _route = null;
        }

        if (_route == null || _route.Waypoints.Count == 0) return goal;

        while (_wp < _route.Waypoints.Count &&
               Horizontal(me.Pos, _route.Waypoints[_wp]) < 1.5f)
            _wp++;

        return _wp < _route.Waypoints.Count ? _route.Waypoints[_wp] : goal;
    }

    private Route BestRoute(WorldState w, Vec3 from, Vec3 goal, out int startWp)
    {
        startWp = 0;
        Route best = null;
        float bestCost = float.MaxValue;

        foreach (var r in w.Routes)
        {
            if (r.Waypoints.Count == 0) continue;

            float toStart = Horizontal(from, r.From);
            if (toStart > 12f) continue;                     // troppo lontana per essere la mia

            float cost = toStart + Horizontal(r.Waypoints[^1], goal);
            if (cost >= bestCost) continue;
            bestCost = cost;
            best = r;
        }

        if (best == null) return null;

        // entra nella rotta dal waypoint più vicino: se sono già a metà strada, non torno indietro
        float nearest = float.MaxValue;
        for (int i = 0; i < best.Waypoints.Count; i++)
        {
            float d = Horizontal(from, best.Waypoints[i]);
            if (d >= nearest) continue;
            nearest = d;
            startWp = i;
        }

        // se il primo waypoint utile è dietro le spalle, meglio partire da zero
        return best;
    }

    private PlayerState NearestEnemy(SimState s)
    {
        var me = s.Get(Id);
        if (me == null || !me.Alive) return null;

        PlayerState best = null;
        float bestD = float.MaxValue;
        foreach (var p in s.Players.Values)
        {
            if (!p.Alive || p.Team == Team) continue;
            float d = Horizontal(me.Pos, p.Pos);
            if (d >= bestD) continue;
            bestD = d;
            best = p;
        }
        return best;
    }

    // ------------------------------------------------------------------ matematica

    private static float Horizontal(Vec3 a, Vec3 b)
    {
        float dx = a.X - b.X, dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>La convenzione della simulazione: avanti = -Z, yaw cresce verso destra.</summary>
    private static float YawTo(Vec3 from, Vec3 to)
        => MathF.Atan2(-(to.X - from.X), -(to.Z - from.Z));

    private static float PitchTo(PlayerState me, PlayerState enemy)
    {
        float eye = me.Pos.Y + Constants.Height * 0.5f - 0.2f;
        float target = enemy.Pos.Y + Constants.Height * 0.2f;
        float d = Horizontal(me.Pos, enemy.Pos);
        return MathF.Atan2(target - eye, MathF.Max(0.5f, d));
    }
}
