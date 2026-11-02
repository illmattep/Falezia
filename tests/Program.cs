using System.Numerics;
using System.Text.Json;
using Falezia.Net;
using Falezia.Sim;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Tests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Console.WriteLine();
        Console.WriteLine("=========== TEST M0 — simulazione (fuori da Godot) ===========");

        Weapons();
        WeaponsSpread();
        ArsenalTable();
        AutomaticAndPellets();
        ShopLegacy();
        MatchBrief();
        EconomyAndDrops();
        FireModes();
        TimeToKill();
        CombatDamage();
        CombatZones();
        CombatWalls();
        CombatSurfaceNormal();
        EquipKeepsCounters();
        CombatRespawn();
        CombatMelee();
        CombatPlayers();
        MatchRounds();
        ObjectiveRound();
        FireBuffer();
        Raytrace();
        Gravity();
        WalkSpeed();
        SprintSpeed();
        CrouchSpeed();
        JumpHeight();
        NoAirStrafe();
        WallCollision();
        StandOnCrate();
        Determinism();
        ProtocolInput();
        ProtocolSnapshot();
        SimHasNoGodotDependency();

        Console.WriteLine("----------- gradini e contatto --------------------------------");
        StepUpStairs();
        StepTooHigh();
        StepDownStaysOnFloor();
        GridQueryMatchesBruteForce();

        Console.WriteLine("----------- mappa: Calanera (doc 02) --------------------------");
        CalaneraMap();

        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine($"  passati: {_passed}    falliti: {_failed}");
        Console.WriteLine();
        return _failed == 0 ? 0 : 1;
    }

    // ============================================================ COMBATTIMENTO

    private static void CombatDamage()
    {
        var world = WorldState.MakeTestWorld();
        var targets = new System.Collections.Generic.List<TargetState>();
        var t = new TargetState { Id = 7, Feet = new Vec3(0f, 0f, 8f) };
        targets.Add(t);

        var def = Arsenal.Standard;
        // dal petto, a 8 m: danno da scheda a quella distanza
        var hit = Combat.Shoot(world, targets, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), def);
        int atteso = Arsenal.DamageAt(def, 8f);
        Check("combat: colpo al petto a 8 m = danno da scheda",
            hit.Hit && hit.Target == 7 && hit.Zone == HitZone.Torso && hit.Damage == atteso,
            $"zona={hit.Zone} danno={hit.Damage} atteso={atteso}");
        Check("combat: la vita scende del danno vero",
            MathF.Abs(t.Health - (TargetState.MaxHealth - atteso)) < 0.01f, $"vita={t.Health}");
    }

    private static void CombatZones()
    {
        var world = WorldState.MakeTestWorld();
        var targets = new System.Collections.Generic.List<TargetState>();
        var t = new TargetState { Id = 1, Feet = new Vec3(0f, 0f, 8f) };
        targets.Add(t);
        var def = Arsenal.Standard;

        var head = Combat.Shoot(world, targets, new Vec3(0f, 1.72f, 0f), new Vec3(0f, 0f, 1f), def);
        Check("combat: tiro alla testa riconosciuto", head.Zone == HitZone.Head, $"{head.Zone}");
        Check("combat: testa ×2.2 sul danno da scheda",
            head.Damage == (int)MathF.Round(Arsenal.DamageAt(def, 8f) * Combat.HeadshotMultiplier),
            $"{head.Damage}");

        var legs = Combat.Shoot(world, targets, new Vec3(0f, 0.30f, 0f), new Vec3(0f, 0f, 1f), def);
        Check("combat: tiro alle gambe riconosciuto", legs.Zone == HitZone.Legs, $"{legs.Zone}");
        Check("combat: gambe valgono meno del petto", legs.Damage < head.Damage, $"{legs.Damage} vs {head.Damage}");
    }

    private static void CombatWalls()
    {
        var world = new WorldState();
        world.AddBox(new Vec3(-30f, -1f, -30f), new Vec3(60f, 1f, 60f));   // pavimento
        world.AddBox(new Vec3(-2f, 0f, 4f), new Vec3(4f, 3f, 0.3f));      // muro a 4 m
        var targets = new System.Collections.Generic.List<TargetState>
        {
            new() { Id = 3, Feet = new Vec3(0f, 0f, 8f) }
        };
        var hit = Combat.Shoot(world, targets, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), Arsenal.Standard);
        Check("combat: il muro ferma il colpo (niente danni attraverso)",
            hit.Hit && hit.Target == -1 && hit.Damage == 0 && targets[0].Health == TargetState.MaxHealth,
            $"target={hit.Target} danno={hit.Damage}");

        var miss = Combat.Shoot(world, targets, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 1f, 0f), Arsenal.Standard);
        Check("combat: colpo in aria non tocca nessuno", miss.Target < 0, $"target={miss.Target}");
    }

    /// <summary>
    /// Il colpo riporta la normale della faccia colpita: i fori sui muri si appoggiano
    /// sul muro vero (un colpo sul pavimento lascia il buco per terra, non in piedi).
    /// </summary>
    private static void CombatSurfaceNormal()
    {
        var world = new WorldState();
        world.AddBox(new Vec3(-30f, -1f, -30f), new Vec3(60f, 1f, 60f));   // pavimento
        world.AddBox(new Vec3(-2f, 0f, 4f), new Vec3(4f, 3f, 0.3f));      // muro a 4 m

        var floor = Combat.Shoot(world, null, new Vec3(0f, 1.5f, 0f), new Vec3(0.2f, -1f, 0.1f), Arsenal.Standard);
        Check("impatto: il pavimento restituisce una normale verso l'alto",
            floor.Target == -1 && floor.Normal.Y > 0.85f,
            $"target={floor.Target} normale=({floor.Normal.X:F2},{floor.Normal.Y:F2},{floor.Normal.Z:F2})");

        var wall = Combat.Shoot(world, null, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), Arsenal.Standard);
        Check("impatto: il muro restituisce una normale orizzontale",
            wall.Target == -1 && MathF.Abs(wall.Normal.Z) > 0.95f && MathF.Abs(wall.Normal.Y) < 0.05f,
            $"normale=({wall.Normal.X:F2},{wall.Normal.Y:F2},{wall.Normal.Z:F2})");

        Check("impatto: il punto è sulla faccia, non dentro il muro",
            MathF.Abs(wall.Point.Z - 4f) < 0.02f, $"z={wall.Point.Z:F3}");
    }

    /// <summary>
    /// I contatori degli eventi vivono NELL'ARMA: quando la riprendi in mano sono già
    /// avanti. È la ragione per cui il client li adotta invece di azzerarli — azzerarli
    /// faceva partire uno sparo fantasma al primo 1/2 (il bug segnalato).
    /// </summary>
    private static void EquipKeepsCounters()
    {
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];
        Simulation.Give(s, 1, WeaponSlot.Standard);
        Simulation.Give(s, 1, WeaponSlot.Melee);

        Simulation.Equip(s, 1, WeaponSlot.Standard);   // in mano la pistola
        p.Weapon.Shots = 4;
        p.Weapon.ReloadStarts = 2;
        Simulation.Equip(s, 1, WeaponSlot.Melee);      // la mette via usata
        Check("cambio arma: l'arma lasciata tiene i suoi contatori",
            p.Cache[(int)WeaponSlot.Standard]?.Shots == 4,
            $"cachati={(p.Cache[(int)WeaponSlot.Standard]?.Shots ?? -1)}");
        Simulation.Equip(s, 1, WeaponSlot.Standard);
        Check("cambio arma: tornando indietro i contatori sono ancora avanti (il client li adotta)",
            p.Weapon.Shots == 4 && p.Weapon.ReloadStarts == 2,
            $"colpi={p.Weapon.Shots} ricariche={p.Weapon.ReloadStarts}");
    }

    /// <summary>
    /// I colpi contro i GIOCATORI (server): testa ×2,2, muro che blocca, morte che
    /// toglie dal round. È la funzione che rende possibile la vittoria per eliminazione.
    /// </summary>
    private static void CombatPlayers()
    {
        var world = new WorldState();
        world.AddBox(new Vec3(-30f, -1f, -30f), new Vec3(60f, 1f, 60f));   // pavimento

        var s = new SimState();
        Simulation.Spawn(s, 1, new Vec3(0f, 0f, 0f));
        Simulation.Spawn(s, 2, new Vec3(0f, 0f, -8f));
        var shooter = s.Get(1);
        var target = s.Get(2);

        // colpo al petto
        var body = Combat.ShootPlayers(world, s, 1, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, -1f), Arsenal.Standard);
        Check("giocatori: il colpo al petto toglie vita",
            body.Target == 2 && body.Damage > 0 && target.Health < PlayerState.MaxHealth,
            $"danno={body.Damage} vita={target.Health}");

        // colpo alla testa: ×2,2 sulla scheda
        target.Health = PlayerState.MaxHealth;
        var head = Combat.ShootPlayers(world, s, 1, new Vec3(0f, 1.75f, 0f), new Vec3(0f, 0f, -1f), Arsenal.Standard);
        Check("giocatori: la testa vale ×2,2",
            head.Zone == HitZone.Head &&
            head.Damage == (int)MathF.Round(Arsenal.DamageAt(Arsenal.Standard, head.Distance) * Combat.HeadshotMultiplier),
            $"zona={head.Zone} danno={head.Damage}");

        // il muro ferma il colpo
        var walled = new WorldState();
        walled.AddBox(new Vec3(-30f, -1f, -30f), new Vec3(60f, 1f, 60f));
        walled.AddBox(new Vec3(-3f, 0f, -4f), new Vec3(6f, 3f, 0.3f));
        target.Health = PlayerState.MaxHealth;
        var blocked = Combat.ShootPlayers(walled, s, 1, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, -1f), Arsenal.Standard);
        Check("giocatori: il muro blocca il colpo",
            blocked.Target == -1 && target.Health == PlayerState.MaxHealth,
            $"target={blocked.Target} vita={target.Health}");

        // il colpo che uccide toglie il giocatore dal round
        target.Health = 10f;
        var kill = Combat.ShootPlayers(world, s, 1, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, -1f), Arsenal.Anvil);
        Check("giocatori: a vita zero si è fuori dal round",
            kill.Killed && !target.Alive && target.Health == 0f && target.Deaths == 1,
            $"ucciso={kill.Killed} vivo={target.Alive} morti={target.Deaths}");

        // nessuno spara a se stesso
        var self = Combat.ShootPlayers(world, s, 1, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), Arsenal.Anvil);
        Check("giocatori: chi spara non colpisce se stesso", self.Target != 1, $"target={self.Target}");
    }

    /// <summary>La partita: fasi, punteggio, metà campo, crediti.</summary>
    private static void MatchRounds()
    {
        var m = new MatchState();
        Match.Reset(m);
        Check("partita: si parte dal riscaldamento", m.Phase == RoundPhase.Warmup && m.Round == 1);

        // il riscaldamento porta agli acquisti, gli acquisti al round
        Match.Tick(m, Match.WarmupSeconds + 0.01f);
        Check("partita: dopo il riscaldamento si compra", m.Phase == RoundPhase.Buy,
            $"fase={m.Phase}");
        Match.Tick(m, Match.BuySeconds + 0.01f);
        Check("partita: dopo gli acquisti il round è vivo", m.Phase == RoundPhase.Live,
            $"fase={m.Phase} tempo={m.TimeLeft:F1}");

        // fine round: punteggio e crediti
        Match.EndRound(m, 1);
        Check("partita: il Nord vince il round", m.ScoreNorth == 1 && m.Phase == RoundPhase.RoundEnd);
        Check("partita: il vincitore prende 3000, il perdente 1900",
            Match.Reward(m, 1) == Match.WinReward && Match.Reward(m, 2) == Match.LossBase,
            $"{Match.Reward(m, 1)} / {Match.Reward(m, 2)}");

        // sconfitte di fila: il bonus cresce
        m.Phase = RoundPhase.Live;
        Match.EndRound(m, 1);
        int second = Match.Reward(m, 2);
        m.Phase = RoundPhase.Live;
        Match.EndRound(m, 1);
        int third = Match.Reward(m, 2);
        Check("partita: il bonus sconfitta cresce fino a 2900",
            second == 2400 && third == 2900, $"{second} / {third}");

        // il round successivo riparte dagli acquisti
        m.Phase = RoundPhase.RoundEnd;
        Match.NextRound(m);
        Check("partita: il round dopo riparte dagli acquisti",
            m.Phase == RoundPhase.Buy && m.Round == 2, $"fase={m.Phase} round={m.Round}");

        // a 13 round vinti la partita finisce
        var fast = new MatchState();
        Match.Reset(fast);
        fast.Phase = RoundPhase.Live;
        fast.ScoreNorth = 12;
        Match.EndRound(fast, 1);
        Check("partita: a 13 round vinti è finita",
            fast.Phase == RoundPhase.MatchEnd && Match.MatchOver(fast), $"fase={fast.Phase}");

        DraftSelection();
    }

    /// <summary>La selezione personaggio: pubblica nel riscaldamento, bloccata dal round 1.</summary>
    private static void DraftSelection()
    {
        var m = new MatchState();
        Match.Reset(m);
        var p = new PlayerState { Id = 1 };

        Check("draft: nel riscaldamento si può scegliere", Match.CanDraft(m));
        Check("draft: una specializzazione valida viene accettata",
            Match.TryDraft(m, p, Specialization.Decifratore, (byte)WeaponSlot.Anvil)
            && p.Spec == Specialization.Decifratore && p.Intent == (byte)WeaponSlot.Anvil,
            $"spec={p.Spec} intento={p.Intent}");
        Check("draft: una specializzazione inventata viene rifiutata",
            !Match.TryDraft(m, p, (Specialization)99, 0) && p.Spec == Specialization.Decifratore);

        Match.Tick(m, Match.WarmupSeconds + 0.01f);
        Check("draft: al via del round 1 è chiuso",
            m.Phase == RoundPhase.Buy && !Match.CanDraft(m), $"fase={m.Phase}");
        Check("draft: dopo la chiusura la scelta non cambia più",
            !Match.TryDraft(m, p, Specialization.Portatore, 0) && p.Spec == Specialization.Decifratore);

        // il protocollo: la scelta viaggia in 3 byte e torna indietro uguale
        var wire = Protocol.EncodeDraft(Specialization.Ricognitore, (byte)WeaponSlot.Cadence);
        Check("draft: la scelta viaggia nel protocollo",
            Protocol.TryDecodeDraft(wire, out var spec2, out var intent2)
            && spec2 == Specialization.Ricognitore && intent2 == (byte)WeaponSlot.Cadence);

        // e soprattutto: è PUBBLICA, cioè sta nello snapshot che vede chiunque
        var snapshot = Protocol.EncodeSnapshot(1, 1, 1, new[] { p }, m);
        bool publicOk = Protocol.TryDecodeSnapshot(snapshot, out var decoded)
                        && decoded.State.Get(1) is { } seen
                        && seen.Spec == Specialization.Decifratore;
        Check("draft: la scelta è pubblica nello snapshot", publicOk);

        Check("draft: ogni specializzazione ha un nome e un effetto",
            Draft.IsValid(Specialization.Decifratore) && Draft.IsValid(Specialization.Ricognitore)
            && !Draft.IsValid(Specialization.None)
            && Draft.Name(Specialization.Portatore) == "PORTATORE"
            && Draft.Effect(Specialization.Ingegnere).Contains("Robot"));
    }

    private static void CombatRespawn()
    {
        var t = new TargetState { Id = 9, Feet = Vec3.Zero };
        bool killed = t.Apply((int)TargetState.MaxHealth);
        Check("combat: a vita zero il bersaglio muore", killed && !t.Alive, $"vita={t.Health}");
        Check("combat: da morto non si può uccidere due volte", !t.Apply(50));

        t.TryRespawn(TargetState.RespawnSeconds * 0.5f);
        Check("combat: la rinascita non è immediata", !t.Alive);
        t.TryRespawn(TargetState.RespawnSeconds);
        Check("combat: dopo 4 s torna in piedi con vita piena",
            t.Alive && t.Health == TargetState.MaxHealth, $"vita={t.Health}");
    }

    private static void CombatMelee()
    {
        var world = WorldState.MakeTestWorld();
        var targets = new System.Collections.Generic.List<TargetState>
        {
            new() { Id = 5, Feet = new Vec3(0f, 0f, 1.2f) }
        };
        var cut = Combat.Melee(world, targets, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), Arsenal.Melee);
        Check("combat: il coltello arriva a 1,2 m", cut.Target == 5 && cut.Damage > 0, $"danno={cut.Damage}");

        var far = new TargetState { Id = 6, Feet = new Vec3(0f, 0f, 3f) };
        var list2 = new System.Collections.Generic.List<TargetState> { far };
        var no = Combat.Melee(world, list2, new Vec3(0f, 1.2f, 0f), new Vec3(0f, 0f, 1f), Arsenal.Melee);
        Check("combat: il coltello non arriva a 3 m", no.Target < 0, $"target={no.Target}");
    }

    /// <summary>
    /// IL CICLO DELL'OBIETTIVO (doc 03): aprire, prelevare, trasportare, avviare, resistere.
    /// È il test che dice se il round è un round o solo un deathmatch.
    /// </summary>
    private static void ObjectiveRound()
    {
        string path = FindMap();
        var w = path != null ? MapLoader.Parse(File.ReadAllText(path)) : WorldState.MakeTestWorld();
        var cassa = Objective.FindPoint(w, "cassa");
        var grotta = Objective.FindPoint(w, "grotta");
        var galleria = Objective.FindPoint(w, "galleria");

        Check("obiettivo: la mappa ha cassa ed estrazioni",
            cassa.HasValue && grotta.HasValue && galleria.HasValue,
            $"cassa={cassa?.ToString() ?? "no"} grotta={grotta?.ToString() ?? "no"} galleria={galleria?.ToString() ?? "no"}");
        if (!cassa.HasValue || !grotta.HasValue) return;

        // ---------------------------------------------------- apertura e prelievo
        var s = new SimState();
        var m = new MatchState();
        Match.Reset(m);
        m.Phase = RoundPhase.Live;
        m.TimeLeft = 300f;

        Simulation.Spawn(s, 1, new Vec3(cassa.Value.X, cassa.Value.Y + 0.1f, cassa.Value.Z));
        var nord = s.Get(1);
        nord.Team = 1;
        nord.Interacting = true;

        RunObjective(s, m, w, Objective.OpenSeconds + 0.1f);
        Check("obiettivo: tenendo E la cassa si apre",
            m.Core == CorePhase.Opened && m.OpenProgress >= 0.99f,
            $"core={m.Core} apertura={m.OpenProgress:F2}");

        RunObjective(s, m, w, Objective.PickupSeconds + 0.1f);
        Check("obiettivo: il prelievo mette il Nucleo in mano",
            m.Core == CorePhase.Carried && m.CoreOwner == 1 && m.CoreTeam == 1,
            $"core={m.Core} portatore={m.CoreOwner}");

        // ------------------------------------------------------------ trasporto
        nord.Pos = new Vec3(grotta.Value.X, grotta.Value.Y + 0.9f, grotta.Value.Z);
        RunObjective(s, m, w, Objective.PlantSeconds + 0.1f);
        Check("obiettivo: in zona, tenendo E, parte l'assedio",
            m.Core == CorePhase.Siege && m.ExtractZone == Objective.ZoneEast && m.SiegeLeft > 100f,
            $"core={m.Core} zona={m.ExtractZone} assedio={m.SiegeLeft:F0}");

        float before = m.TimeLeft;
        Match.Tick(m, 5f);
        Check("obiettivo: durante l'assedio il timer del round è sospeso",
            Math.Abs(m.TimeLeft - before) < 0.01f, $"{before:F1} → {m.TimeLeft:F1}");

        RunObjective(s, m, w, Objective.SiegeSeconds + 0.3f);
        Check("obiettivo: l'assedio resistito fa vincere chi estrae",
            m.Phase == RoundPhase.RoundEnd && m.LastWinner == 1,
            $"fase={m.Phase} vincitore={m.LastWinner}");

        // ------------------------------------------------------- rimozione nemica
        var s2 = new SimState();
        var m2 = new MatchState();
        Match.Reset(m2);
        m2.Phase = RoundPhase.Live;
        m2.TimeLeft = 300f;
        m2.Core = CorePhase.Siege;
        m2.CoreTeam = 1;
        m2.ExtractZone = Objective.ZoneEast;
        m2.SiegeLeft = 60f;
        m2.CorePos = grotta.Value;

        Simulation.Spawn(s2, 2, new Vec3(grotta.Value.X, grotta.Value.Y + 0.1f, grotta.Value.Z));
        var sud = s2.Get(2);
        sud.Team = 2;
        sud.Interacting = true;

        RunObjective(s2, m2, w, Objective.RemoveSeconds - 0.5f);
        Check("obiettivo: la rimozione non è istantanea",
            m2.Phase == RoundPhase.Live, $"fase={m2.Phase} canale={m2.Channel:F1}");

        RunObjective(s2, m2, w, 1.0f);
        Check("obiettivo: chi rimuove il Nucleo chiude subito il round",
            m2.Phase == RoundPhase.RoundEnd && m2.LastWinner == 2,
            $"fase={m2.Phase} vincitore={m2.LastWinner}");

        // ------------------------------------------------------ Nucleo che cade
        var s3 = new SimState();
        var m3 = new MatchState();
        Match.Reset(m3);
        m3.Phase = RoundPhase.Live;
        m3.TimeLeft = 300f;
        m3.Core = CorePhase.Carried;
        m3.CoreOwner = 1;
        m3.CoreTeam = 1;

        Simulation.Spawn(s3, 1, new Vec3(10f, 0.1f, 10f));
        var carrier = s3.Get(1);
        carrier.Team = 1;
        carrier.Alive = false;

        RunObjective(s3, m3, w, 0.2f);
        Check("obiettivo: se il portatore cade il Nucleo resta a terra",
            m3.Core == CorePhase.Dropped && m3.CoreOwner == -1
            && Math.Abs(m3.CorePos.X - carrier.Pos.X) < 0.5f,
            $"core={m3.Core} pos={m3.CorePos}");

        // ---------------------------------------------------------- round nullo
        var m4 = new MatchState();
        Match.Reset(m4);
        m4.Phase = RoundPhase.Live;
        m4.TimeLeft = 0.05f;
        Match.Tick(m4, 0.1f);
        Check("obiettivo: nessuna estrazione entro il tempo = round nullo",
            m4.Phase == RoundPhase.RoundEnd && m4.LastWinner == 0
            && Match.Reward(m4, 1) == Match.NullReward && Match.Reward(m4, 2) == Match.NullReward,
            $"fase={m4.Phase} vincitore={m4.LastWinner} premi={Match.Reward(m4, 1)}/{Match.Reward(m4, 2)}");

        // ------------------------------------------- il Decifratore dimezza il tempo
        var s5 = new SimState();
        var m5 = new MatchState();
        Match.Reset(m5);
        m5.Phase = RoundPhase.Live;
        m5.TimeLeft = 300f;

        Simulation.Spawn(s5, 3, new Vec3(cassa.Value.X, cassa.Value.Y + 0.1f, cassa.Value.Z));
        var dec = s5.Get(3);
        dec.Team = 1;
        dec.Spec = Specialization.Decifratore;
        dec.Interacting = true;

        RunObjective(s5, m5, w, Objective.OpenSecondsDecifratore + 0.1f);
        Check("obiettivo: il Decifratore apre in metà tempo",
            m5.Core == CorePhase.Opened, $"core={m5.Core} apertura={m5.OpenProgress:F2}");
    }

    private static void RunObjective(SimState s, MatchState m, WorldState w, float seconds)
    {
        const float dt = 0.1f;
        for (float t = 0f; t < seconds; t += dt) Objective.Step(s, m, w, dt);
    }

    private static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { _passed++; Console.WriteLine($"  OK    {name}"); }
        else { _failed++; Console.WriteLine($"  FAIL  {name}   {detail}"); }
    }

    private static float HSpeed(PlayerState p) => MathF.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Z * p.Vel.Z);

    private static void Run(SimState s, WorldState w, InputCmd cmd, int ticks)
    {
        var d = new Dictionary<int, InputCmd> { [1] = cmd };
        for (int i = 0; i < ticks; i++) Simulation.Step(s, d, w);
    }

    // ------------------------------------------------------------------ test

    private static void Weapons()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];

        Check("armi: si parte con la Standard 17/51",
            p.Weapon.Slot == WeaponSlot.Standard && p.Weapon.Ammo == 17 && p.Weapon.Reserve == 51,
            $"{p.Weapon.Ammo}/{p.Weapon.Reserve}");

        // un secondo di click (uno ogni due tick): la cadenza di 0,21 s dà 5 colpi
        for (int i = 0; i < 64; i++)
            Simulation.ApplyWeapon(s, 1, i % 2 == 0, false, false);
        Check("armi: cadenza 0,21 s = 5 colpi/s", p.Weapon.Shots == 5, $"colpi={p.Weapon.Shots}");
        Check("armi: munizioni scalate", p.Weapon.Ammo == 12, $"caricatore={p.Weapon.Ammo}");

        // ricarica tattica (colpo in canna): 1.55 s
        Simulation.ApplyWeapon(s, 1, false, true, false);
        Check("armi: ricarica tattica avviata", p.Weapon.Reloading && !p.Weapon.ReloadWasEmpty);
        for (int i = 0; i < 130; i++) Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("armi: ricarica tattica completa", p.Weapon.Ammo == 17 && p.Weapon.Reserve == 46,
            $"{p.Weapon.Ammo}/{p.Weapon.Reserve}");

        // ricarica a vuoto: 2.37 s (stessa durata dell'animazione reload_empty)
        p.Weapon.Ammo = 0;
        Simulation.ApplyWeapon(s, 1, false, true, false);
        Check("armi: ricarica a vuoto = scheda (2,0 s)",
            p.Weapon.ReloadWasEmpty && MathF.Abs(p.Weapon.ReloadT - Arsenal.Standard.ReloadEmpty) < 0.02f,
            $"t={p.Weapon.ReloadT:F2}");
        p.Weapon.Reloading = false;
        p.Weapon.ReloadT = 0;

        // colpo a vuoto: nessun colpo sparato, scatta il dry fire
        p.Weapon.Ammo = 0;
        int dryBefore = p.Weapon.DryFires;
        Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("armi: colpo a vuoto", p.Weapon.DryFires == dryBefore + 1 && p.Weapon.Shots == 5,
            $"dry={p.Weapon.DryFires} colpi={p.Weapon.Shots}");

        // l'ultimo colpo del caricatore alza ShotsLast (animazione fire_last)
        p.Weapon.Ammo = 1;
        p.Weapon.FireCd = 0;
        Simulation.ApplyWeapon(s, 1, false, false, false);   // rilascio del grilletto
        Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("armi: ultimo colpo segnalato", p.Weapon.ShotsLast == 1 && p.Weapon.Ammo == 0,
            $"last={p.Weapon.ShotsLast}");

        // cambio arma
        Simulation.Equip(s, 1, WeaponSlot.Hush);
        Check("armi: cambio alla Hush 15/45",
            p.Weapon.Slot == WeaponSlot.Hush && p.Weapon.Ammo == 15 && p.Weapon.Reserve == 45,
            $"{p.Weapon.Ammo}/{p.Weapon.Reserve}");
        Check("armi: Hush silenziata", Arsenal.Get(WeaponSlot.Hush).Silenced);
        Check("armi: danno pieno entro 15 m", Arsenal.DamageAt(Arsenal.Standard, 10f) == 25);
        Check("armi: danno cala oltre la fascia",
            Arsenal.DamageAt(Arsenal.Standard, 30f) < Arsenal.Standard.Damage,
            $"{Arsenal.DamageAt(Arsenal.Standard, 30f)}");
    }

    private static void WeaponsSpread()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];
        var std = Arsenal.Standard;
        var hush = Arsenal.Hush;

        // fermo in piedi: il cono è quello della scheda
        p.Vel = Vector3.Zero;
        p.OnFloor = true;
        Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("spread: fermo = SpreadStill",
            MathF.Abs(p.Weapon.Spread - std.SpreadStill) < 0.01f,
            $"{p.Weapon.Spread:F3} vs {std.SpreadStill:F3}");

        // camminando il cono si apre
        p.Vel = new Vector3(Constants.WalkSpeed, 0, 0);
        Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("spread: in movimento si apre",
            MathF.Abs(p.Weapon.Spread - std.SpreadMove) < 0.02f && p.Weapon.Spread > std.SpreadStill * 3f,
            $"{p.Weapon.Spread:F2}° (fermo {std.SpreadStill:F2}°)");

        // in mira si richiude, anche in movimento
        p.Vel = Vector3.Zero;
        for (int i = 0; i < 24; i++) Simulation.ApplyWeapon(s, 1, false, false, true, 0.02f);
        Check("spread: in mira si chiude",
            MathF.Abs(p.Weapon.Spread - std.SpreadAds) < 0.01f && p.Weapon.AdsT > 0.99f,
            $"{p.Weapon.Spread:F3}° ads={p.Weapon.AdsT:F2}");

        // accovacciato è ancora più stretto
        float adsSpread = p.Weapon.Spread;
        p.Crouching = true;
        Simulation.ApplyWeapon(s, 1, false, false, true, 0.001f);
        Check("spread: accovacciato è più stretto",
            p.Weapon.Spread < adsSpread && MathF.Abs(p.Weapon.Spread - adsSpread * 0.8f) < 0.01f,
            $"{p.Weapon.Spread:F3} vs {adsSpread:F3}");
        p.Crouching = false;

        // in aria il cono si spalanca: niente tiri al volo
        p.OnFloor = false;
        Simulation.ApplyWeapon(s, 1, false, false, true, 0.001f);
        Check("spread: in aria si spalanca",
            p.Weapon.Spread > adsSpread * 1.5f, $"{p.Weapon.Spread:F2}°");
        p.OnFloor = true;

        // il colpo usa il cono di adesso e poi apre il bloom
        p.Weapon.FireCd = 0;
        p.Weapon.ShotSeed = 0;
        Simulation.ApplyWeapon(s, 1, true, false, true, 0.001f);
        Check("spread: il colpo registra il suo cono",
            MathF.Abs(p.Weapon.ShotSpread - adsSpread) < 0.01f && p.Weapon.ShotSeed == 1,
            $"cono colpo {p.Weapon.ShotSpread:F3}° seme {p.Weapon.ShotSeed}");
        Check("spread: il PRIMO colpo è pulito (nessuna apertura)",
            p.Weapon.Bloom <= 0.001f && MathF.Abs(p.Weapon.Spread - adsSpread) < 0.01f,
            $"bloom {p.Weapon.Bloom:F3}° cono {p.Weapon.Spread:F3}°");

        // la serie: i primi colpi restano puliti, poi il cono si apre di molto
        float clean = p.Weapon.Spread;
        p.Weapon.FireCd = 0;
        Simulation.ApplyWeapon(s, 1, false, false, true);
        Simulation.ApplyWeapon(s, 1, true, false, true);
        p.Weapon.FireCd = 0;
        Simulation.ApplyWeapon(s, 1, false, false, true);
        Simulation.ApplyWeapon(s, 1, true, false, true);
        float mid = p.Weapon.Spread;
        for (int i = 0; i < 6; i++)
        {
            p.Weapon.FireCd = 0;
            Simulation.ApplyWeapon(s, 1, false, false, true);
            Simulation.ApplyWeapon(s, 1, true, false, true);
        }
        float late = p.Weapon.Spread;
        Check("spread: i primi colpi sono precisi, poi il cono si apre molto",
            late > clean * 6f && mid < late * 0.5f,
            $"primo {clean:F3}° · terzo {mid:F3}° · nono {late:F3}°");

        // e la serie rientra col silenzio
        for (int i = 0; i < 64; i++) Simulation.ApplyWeapon(s, 1, false, false, true);
        Check("spread: dopo il silenzio la serie riparte da zero",
            p.Weapon.ShotIndex == 0 && MathF.Abs(p.Weapon.Spread - adsSpread) < 0.01f,
            $"serie {p.Weapon.ShotIndex} cono {p.Weapon.Spread:F3}°");

        // cadenza dichiarata dalle schede
        // +20% sulla scheda del 26 ott 2026: 240 → 288, 220 → 264
        Check("armi: cadenza Standard 288 RPM", MathF.Abs(std.RoundsPerMinute - 288f) < 1f,
            $"{std.RoundsPerMinute:F0}");
        Check("armi: cadenza Hush 264 RPM", MathF.Abs(hush.RoundsPerMinute - 263.7f) < 1f,
            $"{hush.RoundsPerMinute:F0}");

        // La cadenza dichiarata è quella VERA: a 64 Hz il resto sotto il tick viaggia
        // nel colpo dopo, altrimenti 837 RPM diventavano 640 (il multiplo di tick).
        bool cadenceAccurate = true;
        string cadenceDetail = "";
        foreach (var slot in new[] { WeaponSlot.Chatter, WeaponSlot.Blitz, WeaponSlot.Scrap })
        {
            var cad = new SimState();
            Simulation.Spawn(cad, 1, new Vector3(0, 1, 0));
            var cadP = cad.Players[1];
            Simulation.Equip(cad, 1, slot);
            int ticks = (int)MathF.Round(1f * Constants.TickRate);
            for (int i = 0; i < ticks; i++) Simulation.ApplyWeapon(cad, 1, true, false, false);

            var d = Arsenal.Get(slot);
            float expected = 1f / d.Cycle;      // colpi attesi in 1 s a cadenza di scheda
            bool ok = MathF.Abs((cadP.Weapon.Shots - 1) - expected) <= 1.5f;
            cadenceAccurate &= ok;
            cadenceDetail += $"{(ok ? "" : "!")}{d.Name} {cadP.Weapon.Shots}colpi/{expected:F1} ";
        }
        Check("armi: a 64 Hz la cadenza vera è quella della scheda", cadenceAccurate, cadenceDetail.Trim());
        Check("armi: l'Hush è più preciso della Standard",
            hush.SpreadAds < std.SpreadAds && hush.SpreadMove < std.SpreadMove,
            $"{hush.SpreadAds:F3} vs {std.SpreadAds:F3}");

        // dispersione: dentro il cono, diversa a ogni colpo, ripetibile
        var fwd = Arsenal.AimForward(0.3f, 0.1f);
        float maxDeg = 2f;
        float worst = 0f;
        var seen = new System.Collections.Generic.HashSet<int>();
        for (int seed = 0; seed < 200; seed++)
        {
            var d = Arsenal.ShotDirection(0.3f, 0.1f, maxDeg, seed);
            float ang = MathF.Atan2(Vector3.Cross(fwd, d).Length(), Vector3.Dot(fwd, d)) * 180f / MathF.PI;
            worst = MathF.Max(worst, ang);
            seen.Add((int)(d.X * 100000f));
        }
        Check("colpo: la traiettoria resta dentro il cono", worst <= maxDeg + 0.01f, $"peggiore {worst:F3}°");
        Check("colpo: ogni seme dà una traiettoria diversa", seen.Count > 150, $"{seen.Count} distinte");
        Check("colpo: stesso seme = stessa traiettoria",
            Arsenal.ShotDirection(0.3f, 0.1f, 2f, 7) == Arsenal.ShotDirection(0.3f, 0.1f, 2f, 7));
        Check("colpo: cono nullo = mira esatta",
            Vector3.Distance(Arsenal.ShotDirection(0.3f, 0.1f, 0f, 7), fwd) < 1e-5f);
    }

    /// <summary>
    /// La tabella del doc 11, riga per riga. Se un numero cambia, il test dice quale arma
    /// e quale campo: l'arsenale si legge da qui, non a occhio nel codice.
    /// </summary>
    private static void ArsenalTable()
    {
        Check("arsenale: 17 slot", Arsenal.Count == 17, $"{Arsenal.Count}");

        var ids = new System.Collections.Generic.HashSet<string>();
        bool allNamed = true;
        foreach (var d in Arsenal.All)
        {
            allNamed &= !string.IsNullOrWhiteSpace(d.Id) && !string.IsNullOrWhiteSpace(d.Name)
                        && !string.IsNullOrWhiteSpace(d.Class) && ids.Add(d.Id);
        }
        Check("arsenale: id unici e schede compilate", allNamed && ids.Count == Arsenal.Count, $"{ids.Count} id");

        // nome, danno per palla, caricatore, riserva, RPM, ricarica tattica, ricarica a vuoto
        (WeaponSlot slot, string name, int dmg, int mag, int res, float rpm, float rt, float re)[] doc =
        {
            (WeaponSlot.Standard, "Standard", 25, 17, 51, 288f, 1.5f, 2.0f),
            (WeaponSlot.Hush, "Hush", 22, 15, 45, 264f, 1.7f, 2.2f),
            (WeaponSlot.Chatter, "Chatter", 18, 20, 60, 742f, 1.8f, 2.4f),
            (WeaponSlot.Snub, "Snub", 50, 6, 18, 216f, 3.0f, 3.0f),
            (WeaponSlot.Hammer, "Hammer", 70, 7, 21, 108f, 2.1f, 2.8f),
            (WeaponSlot.Cadence, "Cadence", 20, 30, 90, 626f, 2.2f, 2.9f),
            (WeaponSlot.Scrap, "Scrap", 17, 20, 60, 720f, 2.0f, 2.6f),
            (WeaponSlot.Blitz, "Blitz", 16, 30, 90, 837f, 2.4f, 3.1f),
            (WeaponSlot.Choke, "Choke", 11, 6, 24, 66f, 3.6f, 3.6f),
            (WeaponSlot.Sweeper, "Sweeper", 10, 7, 21, 132f, 2.6f, 3.4f),
            (WeaponSlot.Anvil, "Anvil", 30, 30, 90, 649f, 2.6f, 3.3f),
            (WeaponSlot.Service, "Service", 25, 30, 90, 735f, 2.3f, 3.0f),
            (WeaponSlot.Vantage, "Vantage", 60, 20, 60, 132f, 2.8f, 3.5f),
            (WeaponSlot.Spotter, "Spotter", 75, 10, 30, 144f, 3.0f, 3.8f),
            (WeaponSlot.Longshot, "Longshot", 150, 5, 20, 48f, 3.2f, 4.0f)
        };

        foreach (var row in doc)
        {
            var d = Arsenal.Get(row.slot);
            bool ok = d.Name == row.name && d.Damage == row.dmg && d.Magazine == row.mag
                      && d.Reserve == row.res && MathF.Abs(d.RoundsPerMinute - row.rpm) <= 2f
                      && MathF.Abs(d.ReloadTactical - row.rt) < 0.001f
                      && MathF.Abs(d.ReloadEmpty - row.re) < 0.001f;
            Check($"arsenale: {row.name} come da scheda", ok,
                $"{d.Damage}dmg {d.Magazine}/{d.Reserve} {d.RoundsPerMinute:F0}rpm {d.ReloadTactical:F1}/{d.ReloadEmpty:F1}s");
        }

        // il cono di ogni arma: in mira è il più stretto, in movimento il più largo
        bool cone = true, buffer = true;
        foreach (var d in Arsenal.All)
        {
            if (d.Melee) continue;
            cone &= d.SpreadAds < d.SpreadStill && d.SpreadStill < d.SpreadMove
                    && d.SpreadAdsMove < d.SpreadMove && d.BloomMax > 0f && d.BloomDecay > 0f;
            buffer &= d.FireBuffer > 0f && d.FireBuffer <= d.Cycle * 0.6f + 0.001f;
        }
        Check("arsenale: cono in mira < fermo < movimento", cone);
        Check("arsenale: il click in anticipo non supera mezzo ciclo", buffer);

        // chi è automatico e chi no (doc 11: dove non è scritto, è semiautomatica)
        bool automatic =
            Arsenal.Chatter.Automatic && Arsenal.Scrap.Automatic
            && Arsenal.Blitz.Automatic && Arsenal.Anvil.Automatic
            && Arsenal.Heavy.Automatic;
        bool semi =
            !Arsenal.Standard.Automatic && !Arsenal.Hush.Automatic && !Arsenal.Snub.Automatic
            && !Arsenal.Hammer.Automatic && !Arsenal.Choke.Automatic && !Arsenal.Sweeper.Automatic
            && !Arsenal.Vantage.Automatic && !Arsenal.Spotter.Automatic && !Arsenal.Longshot.Automatic;
        bool burst = Arsenal.Cadence.Modes == FireMode.Burst && Arsenal.Cadence.BurstCount == 3;
        bool rifles = Arsenal.Anvil.Automatic && Arsenal.Service.Automatic;   // due assalti full-auto
        Check("arsenale: full-auto, raffica e semi come da scheda", automatic && semi && burst && rifles);

        // peso dell'arma: il Longshot è il più lento, il coltello il più veloce
        float slowest = 1f, fastest = 1f;
        foreach (var d in Arsenal.All)
        {
            slowest = MathF.Min(slowest, d.MoveScale);
            fastest = MathF.Max(fastest, d.MoveScale);
        }
        Check("arsenale: il peso si sente", slowest < 0.9f && fastest > 1.0f && Arsenal.Melee.MoveScale == fastest,
            $"lento {slowest:F2} veloce {fastest:F2}");

        // munizioni e danni delle pompe: la scheda dà il TOTALE della cartuccia
        Check("pompe: Choke 12 palle = 132 a bruciapelo",
            Arsenal.Choke.Pellets == 12 && Arsenal.Choke.ShotDamage == 132, $"{Arsenal.Choke.ShotDamage}");
        Check("pompe: Sweeper 10 palle = 100 a bruciapelo",
            Arsenal.Sweeper.Pellets == 10 && Arsenal.Sweeper.ShotDamage == 100, $"{Arsenal.Sweeper.ShotDamage}");
        Check("pompe: Choke a 6 m fa ~90", MathF.Abs(Arsenal.ShotDamageAt(Arsenal.Choke, 6f) - 90f) <= 12f,
            $"{Arsenal.ShotDamageAt(Arsenal.Choke, 6f)}");
        Check("pompe: Choke a 12 m fa ~45", MathF.Abs(Arsenal.ShotDamageAt(Arsenal.Choke, 12f) - 45f) <= 10f,
            $"{Arsenal.ShotDamageAt(Arsenal.Choke, 12f)}");
        Check("pompe: oltre i 20 m è finita", Arsenal.ShotDamageAt(Arsenal.Choke, 20f) <= 18,
            $"{Arsenal.ShotDamageAt(Arsenal.Choke, 20f)}");

        // il coltello non ha munizioni e non si ricarica
        var s = new SimState();
        WorldState.MakeTestWorld();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        Simulation.Equip(s, 1, WeaponSlot.Melee);
        var p = s.Players[1];
        for (int i = 0; i < 6; i++)
        {
            Simulation.ApplyWeapon(s, 1, true, false, false);
            Simulation.ApplyWeapon(s, 1, false, false, false);
            for (int t = 0; t < 40; t++) Simulation.ApplyWeapon(s, 1, false, false, false);
        }
        Check("coltello: 55 danni e nessun colpo a vuoto",
            p.Weapon.Shots >= 5 && p.Weapon.DryFires == 0 && Arsenal.Melee.Damage == 55,
            $"colpi={p.Weapon.Shots} a vuoto={p.Weapon.DryFires}");
    }

    /// <summary>
    /// Fuoco automatico vs semiautomatico, e i pallini delle pompe: le due cose che
    /// distinguono davvero le armi quando il giocatore tiene premuto.
    /// </summary>
    private static void AutomaticAndPellets()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];

        // Chatter 742 RPM: un secondo di grilletto giù sono ~13 colpi, non uno
        Simulation.Equip(s, 1, WeaponSlot.Chatter);
        for (int i = 0; i < 64; i++) Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("automatica: Chatter ~742 RPM in un secondo di grilletto",
            p.Weapon.Shots >= 12 && p.Weapon.Shots <= 14 && p.Weapon.Ammo == 20 - p.Weapon.Shots,
            $"colpi={p.Weapon.Shots} residui={p.Weapon.Ammo}");

        // Standard semiautomatica: tenere premuto NON fa fuoco automatico
        Simulation.Equip(s, 1, WeaponSlot.Standard);
        for (int i = 0; i < 64; i++) Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("semiautomatica: tenere premuto = un colpo", p.Weapon.Shots == 1, $"colpi={p.Weapon.Shots}");

        // Blitz 837 RPM: in un secondo non finisce il caricatore, ma una quindicina di colpi
        Simulation.Equip(s, 1, WeaponSlot.Blitz);
        for (int i = 0; i < 64; i++) Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("automatica: Blitz ~837 RPM", p.Weapon.Shots >= 13 && p.Weapon.Shots <= 15,
            $"colpi={p.Weapon.Shots}");

        // pompa: un colpo per click, dodici palle nella stessa direzione di base
        Simulation.Equip(s, 1, WeaponSlot.Choke);
        Simulation.ApplyWeapon(s, 1, false, false, false);
        Simulation.ApplyWeapon(s, 1, true, false, false);
        Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("pompa: un click = una cartuccia", p.Weapon.Shots == 1 && p.Weapon.Ammo == 5,
            $"colpi={p.Weapon.Shots} ammo={p.Weapon.Ammo}");

        var seen = new System.Collections.Generic.HashSet<int>();
        var fwd = Arsenal.AimForward(0f, 0f);
        float worst = 0f;
        for (int i = 0; i < Arsenal.Choke.Pellets; i++)
        {
            var d = Arsenal.PelletDirection(0f, 0f, 1.5f, 42, i, Arsenal.Choke.Pellets);
            float ang = MathF.Atan2(Vector3.Cross(fwd, d).Length(), Vector3.Dot(fwd, d)) * 180f / MathF.PI;
            worst = MathF.Max(worst, ang);
            seen.Add((int)(d.X * 100000f) * 31 + (int)(d.Y * 100000f));
        }
        Check("pompa: dodici palle diverse dentro il cono",
            seen.Count == 12 && worst <= 1.51f, $"{seen.Count} distinte, peggiore {worst:F2}°");
        Check("pompa: le palle sono ripetibili",
            Arsenal.PelletDirection(0f, 0f, 1.5f, 42, 5, 12) == Arsenal.PelletDirection(0f, 0f, 1.5f, 42, 5, 12));
    }

    /// <summary>
    /// Lo shop e i tre posti dell'arsenale: si parte con la Standard e il coltello,
    /// una sola primaria e una sola secondaria, i soldi si scalano del prezzo.
    /// </summary>
    private static void ShopLegacy()
    {
        var l = new LoadoutState();
        Check("shop: si parte con la Standard in mano e il coltello",
            l.Secondary == WeaponSlot.Standard && l.Primary == null
            && l.Owns(WeaponSlot.Standard) && l.Owns(WeaponSlot.Melee)
            && l.DeployWeapon == WeaponSlot.Standard,
            $"secondaria={l.Secondary} primaria={l.Primary}");
        Check("shop: si parte senza primaria", l.ForSlotKey(1) == null && l.ForSlotKey(2) == WeaponSlot.Standard);

        l.Money = 5000;
        var r1 = l.Buy(WeaponSlot.Cadence);
        Check("shop: comprare una primaria la mette in mano",
            r1 == BuyResult.Bought && l.Primary == WeaponSlot.Cadence && l.Money == 4100,
            $"{r1} ${l.Money}");

        var r2 = l.Buy(WeaponSlot.Blitz);
        Check("shop: una sola primaria, la nuova consuma la vecchia",
            r2 == BuyResult.Bought && l.Primary == WeaponSlot.Blitz && l.Money == 3100
            && !l.Owns(WeaponSlot.Cadence),
            $"primaria={l.Primary} ${l.Money} vecchia={(l.Owns(WeaponSlot.Cadence) ? "ancora tua" : "consumata")}");

        var r3 = l.Buy(WeaponSlot.Hush);
        Check("shop: una sola secondaria, la Standard resta gratis",
            r3 == BuyResult.Bought && l.Secondary == WeaponSlot.Hush && l.Money == 2700
            && l.Owns(WeaponSlot.Standard),
            $"secondaria={l.Secondary} ${l.Money}");

        var r4 = l.Buy(WeaponSlot.Hush);
        Check("shop: ricomprare un'arma già tua non costa niente",
            r4 == BuyResult.Equipped && l.Money == 2700, $"{r4} ${l.Money}");

        var r5 = l.Buy(WeaponSlot.Melee);
        Check("shop: il coltello non si compra", r5 == BuyResult.IsMelee);

        l.Money = 100;
        var r6 = l.Buy(WeaponSlot.Longshot);
        Check("shop: senza soldi non si compra",
            r6 == BuyResult.NotEnough && !l.Owns(WeaponSlot.Longshot) && l.Primary == WeaponSlot.Blitz,
            $"{r6}");

        l.Unlimited = true;
        l.Money = 0;
        var r7 = l.Buy(WeaponSlot.Longshot);
        Check("shop: in debug i soldi sono illimitati",
            r7 == BuyResult.Bought && l.Primary == WeaponSlot.Longshot && l.Money == 0,
            $"{r7} ${l.Money}");
        Check("shop: con una primaria si spawna con quella",
            l.DeployWeapon == WeaponSlot.Longshot, $"{l.DeployWeapon}");

        // la rotella gira solo fra gli slot che hai
        var seen = new System.Collections.Generic.HashSet<WeaponSlot>();
        var cur = l.Secondary;
        for (int i = 0; i < 6; i++)
        {
            cur = l.NextHeld(cur, 1);
            seen.Add(cur);
        }
        Check("shop: la rotella gira primaria, secondaria e coltello",
            seen.Count == 3 && seen.Contains(l.Primary.Value) && seen.Contains(l.Secondary)
            && seen.Contains(WeaponSlot.Melee), $"{seen.Count} slot");
    }

    /// <summary>
    /// Modi di fuoco: la raffica da tre colpi, il selettore, e chi non ce l'ha.
    /// </summary>
    private static void FireModes()
    {
        var s = new SimState();
        WorldState.MakeTestWorld();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];

        // Cadence: raffica da 3. Non c'è nessun selettore: è il suo comportamento.
        Simulation.Equip(s, 1, WeaponSlot.Cadence);
        Check("raffica: la Cadence è a raffica (3 colpi), senza selettore",
            p.Weapon.Mode == FireMode.Burst && Arsenal.Cadence.Modes == FireMode.Burst
            && Arsenal.Cadence.BurstCount == 3);

        // un click = tre colpi, alla cadenza dell'arma (il grilletto si molla subito:
        // la raffica continua da sola grazie alla coda)
        Simulation.ApplyWeapon(s, 1, true, false, false);
        for (int i = 0; i < 96; i++) Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("raffica: un click sono TRE colpi", p.Weapon.Shots == 3, $"colpi={p.Weapon.Shots}");

        // TENENDO PREMUTO la raffica si ripete (era il difetto: serviva ripremere)
        for (int i = 0; i < 160; i++) Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("raffica: tenendo premuto le raffiche si susseguono", p.Weapon.Shots >= 6,
            $"colpi={p.Weapon.Shots}");

        // rilasciando, la raffica in corso finisce e non ne parte un'altra
        for (int i = 0; i < 160; i++) Simulation.ApplyWeapon(s, 1, false, false, false);
        int stopped = p.Weapon.Shots;
        for (int i = 0; i < 160; i++) Simulation.ApplyWeapon(s, 1, false, false, false);
        Check("raffica: mollando il grilletto non parte un'altra raffica",
            p.Weapon.Shots == stopped, $"colpi={p.Weapon.Shots} (fermo a {stopped})");

        // la pistola resta semiautomatica: tenere premuto non spara
        Simulation.Equip(s, 1, WeaponSlot.Standard);
        int before = p.Weapon.Shots;
        for (int i = 0; i < 120; i++) Simulation.ApplyWeapon(s, 1, true, false, false);
        Check("semi: la Standard spara un colpo per click",
            p.Weapon.Shots == before + 1, $"colpi={p.Weapon.Shots - before}");

        // cambiare arma NON riempie il caricatore: le munizioni restano nell'arma
        int afterShots = p.Weapon.Ammo;
        Simulation.Equip(s, 1, WeaponSlot.Melee);
        Simulation.Equip(s, 1, WeaponSlot.Standard);
        Check("armi: cambiare arma non riempie il caricatore",
            p.Weapon.Ammo == afterShots, $"prima={afterShots} dopo={p.Weapon.Ammo}");

        // ottiche: chi ce l'ha, quanto ingrandisce
        Check("ottiche: i cecchini hanno il cannocchiale",
            Arsenal.Longshot.Scoped && Arsenal.Spotter.Scoped && Arsenal.Vantage.Scoped
            && Arsenal.Longshot.Magnification > Arsenal.Spotter.Magnification
            && Arsenal.Spotter.Magnification > Arsenal.Vantage.Magnification,
            $"{Arsenal.Longshot.Magnification}× {Arsenal.Spotter.Magnification}× {Arsenal.Vantage.Magnification}×");
        Check("ottiche: Cadence e Blitz hanno il punto rosso",
            Arsenal.Cadence.DotSight && Arsenal.Blitz.DotSight && !Arsenal.Cadence.Scoped);
        Check("ottiche: la Standard ha le mire metalliche",
            !Arsenal.Standard.Scoped && !Arsenal.Standard.DotSight && Arsenal.Standard.Magnification == 1f);
    }

    /// <summary>
    /// I tempi della tabella del doc 11: quanti colpi servono per uccidere (senza piastra)
    /// e quanto ci mettono a uscire, calcolati dalla cadenza. Se qualcuno cambia un ciclo,
    /// qui si vede subito quale arma è uscita dalla fascia 0,25–0,45 s (era 0,30–0,55).
    /// </summary>
    private static void TimeToKill()
    {
        // arma, colpi per uccidere senza piastra, tempo dichiarato dalla tabella
        (WeaponSlot slot, int shots, float seconds)[] table =
        {
            (WeaponSlot.Chatter, 6, 0.40f),
            (WeaponSlot.Blitz, 7, 0.43f),
            (WeaponSlot.Snub, 2, 0.28f),
            (WeaponSlot.Cadence, 5, 0.38f),
            (WeaponSlot.Hammer, 2, 0.56f),
            (WeaponSlot.Anvil, 4, 0.28f),
            (WeaponSlot.Service, 4, 0.25f),
            (WeaponSlot.Scrap, 6, 0.42f),
            (WeaponSlot.Vantage, 2, 0.45f),
            (WeaponSlot.Spotter, 2, 0.42f),
            (WeaponSlot.Standard, 4, 0.63f),
            (WeaponSlot.Hush, 5, 0.91f),
            (WeaponSlot.Sweeper, 2, 0.45f)
        };

        foreach (var row in table)
        {
            var d = Arsenal.Get(row.slot);
            float t = (row.shots - 1) * d.Cycle;
            Check($"tempi: {d.Name} {row.shots} colpi in {row.seconds:F2}s",
                MathF.Abs(t - row.seconds) <= 0.02f, $"{t:F2}s (cadenza {d.RoundsPerMinute:F0})");
        }

        // le armi che uccidono con un colpo non hanno tempo di raffica
        Check("tempi: Choke e Longshot uccidono col primo colpo",
            Arsenal.Choke.ShotDamage >= 100 && Arsenal.Longshot.Damage >= 100,
            $"{Arsenal.Choke.ShotDamage} / {Arsenal.Longshot.Damage}");

        // il caricatore si svuota nei tempi dichiarati nelle schede
        Check("tempi: il Chatter svuota in 1,5 s",
            MathF.Abs((Arsenal.Chatter.Magazine - 1) * Arsenal.Chatter.Cycle - 1.54f) <= 0.10f,
            $"{(Arsenal.Chatter.Magazine - 1) * Arsenal.Chatter.Cycle:F2}s");
    }

    private static void FireBuffer()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        var p = s.Players[1];

        // un click vero è un fronte: premo e rilascio (è quello che manda il client)
        void Click()
        {
            Simulation.ApplyWeapon(s, 1, true, false, false);
            Simulation.ApplyWeapon(s, 1, false, false, false);
        }

        void Ticks(int n, bool fire = false)
        {
            for (int i = 0; i < n; i++) Simulation.ApplyWeapon(s, 1, fire, false, false);
        }

        // a freddo il colpo parte subito
        Click();
        Check("grilletto: click a freddo spara subito", p.Weapon.Shots == 1, $"colpi={p.Weapon.Shots}");

        // click 40 ms in anticipo: resta in coda e spara appena l'arma è pronta
        int before = p.Weapon.Shots;
        p.Weapon.FireCd = 0.04f;
        Click();
        Check("grilletto: il click in anticipo entra in coda",
            p.Weapon.Shots == before && p.Weapon.FireBuffer > 0f,
            $"colpi={p.Weapon.Shots} coda={p.Weapon.FireBuffer:F3}");

        Ticks(8);
        Check("grilletto: spara appena l'arma è pronta", p.Weapon.Shots == before + 1,
            $"colpi={p.Weapon.Shots}");

        // click troppo in anticipo: fuori dalla finestra, si scarta (niente colpi in ritardo)
        before = p.Weapon.Shots;
        p.Weapon.FireCd = 0.18f;
        Click();
        Ticks(24);
        Check("grilletto: click fuori finestra scartato", p.Weapon.Shots == before,
            $"colpi={p.Weapon.Shots}");

        // fuoco tenuto: una semiauto spara UNA volta, non diventa automatica
        Simulation.Give(s, 1, WeaponSlot.Standard);
        p.Weapon.FireCd = 0f;
        Ticks(64, fire: true);
        Check("grilletto: tenere premuto non è fuoco automatico", p.Weapon.Shots == 1,
            $"colpi={p.Weapon.Shots}");

        // spam: 16 click al secondo per un secondo → la cadenza tiene, 4 colpi
        Simulation.Give(s, 1, WeaponSlot.Standard);
        for (int i = 0; i < 64; i++)
            Simulation.ApplyWeapon(s, 1, i % 4 == 0, false, false);
        Check("grilletto: 16 click/s danno 5 colpi", p.Weapon.Shots == 5, $"colpi={p.Weapon.Shots}");
        Check("grilletto: nessun colpo in più nel caricatore",
            p.Weapon.Ammo == 12, $"caricatore={p.Weapon.Ammo}");

        // la finestra non è infinita: un click 300 ms prima non arma il colpo successivo
        Simulation.Give(s, 1, WeaponSlot.Standard);
        p.Weapon.FireCd = 0.30f;
        Click();
        Ticks(8);
        Check("grilletto: la coda scade dopo la finestra",
            p.Weapon.FireBuffer == 0f && p.Weapon.Shots == 0,
            $"coda={p.Weapon.FireBuffer:F3} colpi={p.Weapon.Shots}");

        // con il caricatore vuoto il colpo a vuoto scatta subito, non va in coda
        p.Weapon.Ammo = 0;
        p.Weapon.FireCd = 0.05f;
        int dry = p.Weapon.DryFires;
        Click();
        Check("grilletto: a vuoto scatta subito", p.Weapon.DryFires == dry + 1,
            $"a vuoto={p.Weapon.DryFires}");
    }

    private static void Raytrace()
    {
        var w = WorldState.MakeTestWorld();
        var eye = new Vector3(0, 1.2f, 0);

        // muro lungo a x -10..-4, z -6..6: sparando a ovest si colpisce la faccia est a 4 m
        var hit = Trace.Ray(w, eye, new Vector3(-1, 0, 0), 100f);
        Check("raggio: colpisce il muro a 4 m",
            hit.Hit && MathF.Abs(hit.Distance - 4f) < 0.01f,
            $"{hit.Distance:F2} m");
        Check("raggio: punto e normale dell'impatto",
            MathF.Abs(hit.Point.X + 4f) < 0.01f && hit.Normal == Vector3.UnitX,
            $"punto {hit.Point.X:F2} normale {hit.Normal.X:F0}");

        // verso l'alto: cielo, nessun impatto
        Check("raggio: verso l'alto non colpisce niente",
            !Trace.Ray(w, eye, new Vector3(0, 1, 0), 100f).Hit);

        // verso il basso: il pavimento a y = 0, 1,2 m sotto
        var floor = Trace.Ray(w, eye, new Vector3(0, -1, 0), 100f);
        Check("raggio: colpisce il pavimento",
            floor.Hit && MathF.Abs(floor.Distance - 1.2f) < 0.01f,
            $"{floor.Distance:F2} m");

        // dentro un volume il raggio non si ferma addosso a se stesso
        Check("raggio: chi è dentro un volume non lo colpisce",
            !Trace.Ray(w, new Vector3(-7, 1.2f, 0), new Vector3(-1, 0, 0), 100f).Hit);

        // una cassa più vicina del muro vince
        var crate = Trace.Ray(w, new Vector3(3, 0.6f, 9.5f), new Vector3(0, 0, -1), 100f);
        Check("raggio: prende la cassa più vicina",
            crate.Hit && MathF.Abs(crate.Distance - 2f) < 0.05f, $"{crate.Distance:F2} m");

        // oltre la distanza massima non c'è impatto
        Check("raggio: oltre la gittata niente impatto",
            !Trace.Ray(w, eye, new Vector3(-1, 0, 0), 2f).Hit);
    }

    private static void Gravity()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 5, 0));
        Run(s, w, new InputCmd(), 180);
        var p = s.Players[1];
        Check("gravità: atterra", p.OnFloor, $"on_floor={p.OnFloor} y={p.Pos.Y:F3}");
        Check("gravità: centro a metà altezza", MathF.Abs(p.Pos.Y - Constants.Height * 0.5f) < 0.05f,
            $"y={p.Pos.Y:F3} atteso {Constants.Height * 0.5f:F3}");
    }

    private static void WalkSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 128);
        float v = HSpeed(s.Players[1]);
        Check("passo: ~4.2 m/s", MathF.Abs(v - Constants.WalkSpeed) < 0.25f, $"{v:F3}");
    }

    private static void SprintSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1), Sprint = true }, 48);
        float v = HSpeed(s.Players[1]);
        Check("sprint: ~6.5 m/s", MathF.Abs(v - Constants.SprintSpeed) < 0.35f, $"{v:F3}");
    }

    private static void CrouchSpeed()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1), Crouch = true }, 128);
        var p = s.Players[1];
        Check("accovacciato: stato attivo", p.Crouching);
        float v = HSpeed(p);
        Check("accovacciato: ~2.2 m/s", MathF.Abs(v - Constants.CrouchSpeed) < 0.25f, $"{v:F3}");
    }

    private static void JumpHeight()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd(), 30);
        float baseY = s.Players[1].Pos.Y;
        Simulation.ApplyInput(s, 1, new InputCmd { Jump = true }, w);
        float peak = s.Players[1].Pos.Y;
        for (int i = 0; i < 60; i++)
        {
            Simulation.ApplyInput(s, 1, new InputCmd(), w);
            peak = MathF.Max(peak, s.Players[1].Pos.Y);
        }
        float h = peak - baseY;
        Check("salto: altezza 0.8–1.2 m", h > 0.8f && h < 1.2f, $"{h:F3} m");
    }

    private static void NoAirStrafe()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 64);
        Simulation.ApplyInput(s, 1, new InputCmd { Jump = true }, w);
        float before = HSpeed(s.Players[1]);
        for (int i = 0; i < 22; i++)
            Simulation.ApplyInput(s, 1, new InputCmd { Move = new Vector2(1, 0) }, w);
        float after = HSpeed(s.Players[1]);
        Check("nessun air-strafe: guadagno < 0.8 m/s", after - before < 0.8f,
            $"prima {before:F3} dopo {after:F3}");
    }

    private static void WallCollision()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(-3.0f, 0.2f, 0));
        Run(s, w, new InputCmd { Move = new Vector2(-1, 0) }, 180);
        var p = s.Players[1];
        Check("muro: non penetra", p.Pos.X >= -3.75f, $"x={p.Pos.X:F3}");
        Check("muro: si ferma al contatto", MathF.Abs(p.Pos.X - (-3.7f)) < 0.1f, $"x={p.Pos.X:F3} atteso -3.700");
    }

    private static void StandOnCrate()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(3.25f, 4.0f, 6.25f));
        Run(s, w, new InputCmd(), 180);
        var p = s.Players[1];
        float expected = 1.0f + Constants.Height * 0.5f;
        Check("cassa: atterra sopra", MathF.Abs(p.Pos.Y - expected) < 0.06f,
            $"y={p.Pos.Y:F3} atteso {expected:F3}");
    }

    private static void Determinism()
    {
        var a = ScriptedRun(12345);
        var b = ScriptedRun(12345);
        Check("determinismo: due run identiche",
            Vector3.Distance(a.Players[1].Pos, b.Players[1].Pos) < 1e-6f,
            $"a={a.Players[1].Pos} b={b.Players[1].Pos}");

        var c = ScriptedRun(999);
        Check("determinismo: seed diverso → risultato diverso",
            Vector3.Distance(a.Players[1].Pos, c.Players[1].Pos) > 1e-4f);
    }

    private static SimState ScriptedRun(int seed)
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.2f, 0));
        var rng = new Random(seed);
        var d = new Dictionary<int, InputCmd>();
        for (int i = 0; i < 300; i++)
        {
            d[1] = new InputCmd
            {
                Move = new Vector2((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)),
                Yaw = (float)(rng.NextDouble() * Math.PI * 2 - Math.PI),
                Pitch = (float)(rng.NextDouble() * 2 - 1),
                Jump = rng.NextDouble() < 0.08,
                Sprint = rng.NextDouble() < 0.5,
                Crouch = rng.NextDouble() < 0.1,
                Ads = rng.NextDouble() < 0.1
            };
            Simulation.Step(s, d, w);
        }
        return s;
    }

    private static void ProtocolInput()
    {
        var c = new InputCmd
        {
            Tick = 4242,
            Move = new Vector2(-0.5f, 0.75f),
            Yaw = 1.234f,
            Pitch = -0.567f,
            Jump = true,
            Sprint = true,
            Ads = true,
            Fire = true,
            Reload = true
        };
        bool ok = Protocol.TryDecodeInput(Protocol.EncodeInput(c), out var d);
        Check("protocollo input: decodifica", ok);
        if (!ok) return;
        Check("protocollo input: tick", d.Tick == 4242);
        Check("protocollo input: move", Vector2.Distance(d.Move, c.Move) < 1e-5f);
        Check("protocollo input: yaw/pitch", MathF.Abs(d.Yaw - c.Yaw) < 1e-5f && MathF.Abs(d.Pitch - c.Pitch) < 1e-5f);
        Check("protocollo input: flag", d.Jump && d.Sprint && d.Ads && !d.Crouch);
        Check("protocollo input: fuoco e ricarica", d.Fire && d.Reload);
    }

    private static void ProtocolSnapshot()
    {
        var s = new SimState();
        var p = s.GetOrCreate(3);
        p.Pos = new Vector3(1.5f, 2.5f, -3.5f);
        p.Vel = new Vector3(0.1f, 0.2f, 0.3f);
        p.Yaw = 0.9f;
        p.Pitch = -0.2f;
        p.Stamina = 7.5f;
        p.JumpCd = 0.05f;
        p.OnFloor = true;
        p.Crouching = true;

        bool ok = Protocol.TryDecodeSnapshot(
            Protocol.EncodeSnapshot(77, 33, 3, s.Players.Values), out var snap);
        Check("protocollo snapshot: decodifica", ok);
        if (!ok) return;
        Check("protocollo snapshot: tick/ack", snap.Tick == 77 && snap.Ack == 33);
        Check("protocollo snapshot: your_id", snap.YourId == 3);
        var rp = snap.State.Players[3];
        Check("protocollo snapshot: posizione", Vector3.Distance(rp.Pos, p.Pos) < 1e-5f);
        Check("protocollo snapshot: velocità", Vector3.Distance(rp.Vel, p.Vel) < 1e-5f);
        Check("protocollo snapshot: flag", rp.OnFloor && rp.Crouching);
        Check("protocollo snapshot: stamina", MathF.Abs(rp.Stamina - 7.5f) < 1e-5f);

        // armi a terra, cassa di squadra e contatori degli eventi viaggiano nello snapshot:
        // senza questi byte il client non può sentire gli spari degli altri né mostrare la cassa
        s.Drops.Add(new GroundWeapon
        {
            Id = 7, Slot = WeaponSlot.Anvil, Ammo = 21, Reserve = 63,
            Pos = new Vector3(4f, 0.03f, -2f), Spin = 1.1f, Settled = true
        });
        p.Weapon.Shots = 42;
        p.Weapon.ReloadStarts = 3;
        var match = new MatchState { Phase = RoundPhase.Buy, Round = 4, ScoreNorth = 2, ScoreSouth = 1,
                                     TeamFundsNorth = 4100, TeamFundsSouth = 2200 };

        bool ok2 = Protocol.TryDecodeSnapshot(
            Protocol.EncodeSnapshot(90, 41, 3, s.Players.Values, match, s.Drops), out var snap2);
        Check("protocollo snapshot: armi a terra, cassa e contatori", ok2
            && snap2.State.Drops.Count == 1
            && snap2.State.Drops[0].Slot == WeaponSlot.Anvil
            && snap2.State.Drops[0].Ammo == 21 && snap2.State.Drops[0].Reserve == 63
            && MathF.Abs(snap2.State.Drops[0].Pos.X - 4f) < 1e-4f
            && snap2.State.Drops[0].Settled && snap2.State.Drops[0].Id == 7
            && snap2.Match.TeamFundsNorth == 4100 && snap2.Match.TeamFundsSouth == 2200
            && snap2.State.Players[3].Weapon.Shots == 42
            && snap2.State.Players[3].Weapon.ReloadStarts == 3,
            ok2 ? $"drops={snap2.State.Drops.Count} casse={snap2.Match.TeamFundsNorth}/{snap2.Match.TeamFundsSouth} colpi={snap2.State.Players[3].Weapon.Shots}" : "decode fallito");
    }

    // ------------------------------------------------------------- gradini

    /// <summary>Un mondo minimo: pavimento + una scala di <paramref name="steps"/> alzate da 17 cm verso -Z.</summary>
    private static WorldState StairWorld(int steps, float rise = 0.17f)
    {
        var w = new WorldState();
        w.AddBox(new Vector3(-20, -1, -40), new Vector3(40, 1, 60));
        for (int i = 0; i < steps; i++)
            w.AddBox(new Vector3(-2, 0, -2 - (i + 1) * 0.30f), new Vector3(4, rise * (i + 1), 0.30f));
        // pianerottolo in cima
        w.AddBox(new Vector3(-2, 0, -2 - steps * 0.30f - 4f), new Vector3(4, rise * steps, 4f));
        return w;
    }

    private static void StepUpStairs()
    {
        var w = StairWorld(10);
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 0.1f, 0));
        Run(s, w, new InputCmd(), 30);
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 96);    // 1,5 s in avanti (-Z): finisce sul pianerottolo
        var p = s.Players[1];
        float feet = p.Pos.Y - Constants.Height * 0.5f;
        Check("gradini: sale una scala da 17 cm camminando", MathF.Abs(feet - 1.70f) < 0.03f && p.OnFloor,
            $"piedi y={feet:F3} atteso 1.700, a terra={p.OnFloor}");
    }

    private static void StepTooHigh()
    {
        // il gradino da 40 cm della stanza di test: si sale solo saltando
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(-1f, 0.1f, 12f));
        Run(s, w, new InputCmd(), 30);
        Run(s, w, new InputCmd { Move = new Vector2(0, 1) }, 96);
        var p = s.Players[1];
        float feet = p.Pos.Y - Constants.Height * 0.5f;
        Check("gradini: 40 cm non si salgono camminando", feet < 0.05f && p.Pos.Z >= 10.29f,
            $"piedi y={feet:F3} z={p.Pos.Z:F3}");
    }

    private static void StepDownStaysOnFloor()
    {
        var w = StairWorld(10);
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1.8f, -6.0f));        // sul pianerottolo
        Run(s, w, new InputCmd(), 30);
        int airborne = 0;
        var d = new Dictionary<int, InputCmd> { [1] = new InputCmd { Move = new Vector2(0, -1) } };
        for (int i = 0; i < 160; i++)
        {
            Simulation.Step(s, d, w);
            if (!s.Players[1].OnFloor) airborne++;
        }
        float feet = s.Players[1].Pos.Y - Constants.Height * 0.5f;
        Check("gradini: in discesa si resta a terra", airborne <= 2 && feet < 0.02f,
            $"tick in aria={airborne} piedi y={feet:F3}");
    }

    private static void GridQueryMatchesBruteForce()
    {
        var w = WorldState.MakeTestWorld();
        var rng = new Random(7);
        for (int i = 0; i < 300; i++)
            w.AddBox(new Vector3((float)rng.NextDouble() * 100 - 50, 0, (float)rng.NextDouble() * 100 - 50),
                     new Vector3((float)rng.NextDouble() * 9 + 0.1f, 2, (float)rng.NextDouble() * 9 + 0.1f));
        bool ok = true;
        for (int t = 0; t < 2000 && ok; t++)
        {
            var c = new Vector3((float)rng.NextDouble() * 120 - 60, 1, (float)rng.NextDouble() * 120 - 60);
            var h = new Vector3((float)rng.NextDouble() * 3, 1, (float)rng.NextDouble() * 3);
            var got = new HashSet<int>(w.Query(c - h, c + h));
            for (int i = 0; i < w.Boxes.Count; i++)
            {
                var b = w.Boxes[i];
                bool touches = !(c.X + h.X < b.Min.X || c.X - h.X > b.Max.X || c.Z + h.Z < b.Min.Z || c.Z - h.Z > b.Max.Z);
                if (touches && !got.Contains(i)) { ok = false; break; }
            }
        }
        Check("griglia: nessuna scatola vicina dimenticata", ok);
    }

    // ------------------------------------------------------------- mappa

    private static string FindMap()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var f = Path.Combine(dir.FullName, "game", "maps", "calanera.json");
                if (File.Exists(f)) return f;
                dir = dir.Parent;
            }
        }
        return null;
    }

    /// <summary>
    /// Cammina con la VERA simulazione da un punto a una serie di waypoint (x, z),
    /// come farebbe un giocatore che tiene premuto W e gira il mouse.
    /// </summary>
    private static (bool Ok, float Seconds, PlayerState P) Walk(WorldState w, Vector3 feet, (float X, float Z)[] wps, float maxSeconds)
    {
        var s = new SimState();
        Simulation.Spawn(s, 1, feet);
        var d = new Dictionary<int, InputCmd> { [1] = new InputCmd() };
        for (int i = 0; i < 64; i++) Simulation.Step(s, d, w);
        float t = 0f;
        foreach (var wp in wps)
        {
            while (true)
            {
                var p = s.Players[1];
                float dx = wp.X - p.Pos.X, dz = wp.Z - p.Pos.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) < 0.4f) break;
                d[1] = new InputCmd { Move = new Vector2(0, 1), Yaw = MathF.Atan2(-dx, -dz) };
                Simulation.Step(s, d, w);
                t += Constants.Dt;
                if (t > maxSeconds) return (false, t, s.Players[1]);
            }
        }
        return (true, t, s.Players[1]);
    }

    private static void CalaneraMap()
    {
        string path = FindMap();
        WorldState w = null;
        try { if (path != null) w = MapLoader.Parse(File.ReadAllText(path)); }
        catch (Exception e) { Console.WriteLine("        " + e.Message); }
        Check("mappa: calanera.json si carica", w != null, path ?? "file non trovato (game/maps/calanera.json)");
        if (w == null) return;

        Check("mappa: contenuto completo",
            w.Boxes.Count > 500 && w.SpawnsNorth.Count == 5 && w.SpawnsSouth.Count == 5
            && w.Points.Any(p => p.Name == "Grotta del Levante") && w.Points.Any(p => p.Name == "Galleria Vecchia")
            && w.Kinds.Contains("objective"),
            $"scatole={w.Boxes.Count} spawn={w.SpawnsNorth.Count}+{w.SpawnsSouth.Count} punti={w.Points.Count}");

        // tutti e 10 gli spawn poggiano su un pavimento libero
        var s = new SimState();
        int id = 1;
        foreach (var f in w.SpawnsNorth.Concat(w.SpawnsSouth)) Simulation.Spawn(s, id++, f);
        var idle = s.Players.Keys.ToDictionary(k => k, k => new InputCmd());
        for (int i = 0; i < 64; i++) Simulation.Step(s, idle, w);
        bool landed = s.Players.Values.All(p => p.OnFloor && MathF.Abs(p.Pos.Y - Constants.Height * 0.5f) < 0.05f);
        Check("mappa: i 10 spawn poggiano a terra", landed);

        // Le ROTTE vengono dal file della mappa: il generatore (tools/mapgen) le ha trovate sul
        // grafo di percorribilità e le ha già percorse con il suo porting Python del movimento.
        // Qui si ripercorrono con la simulazione VERA: se la mappa o Sim.cs cambiano in modo
        // incompatibile, il test lo dice. Così i test non contengono coordinate da aggiornare a mano.
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        int ring = 0, total = 0;
        if (doc.RootElement.TryGetProperty("routes", out var routes))
        {
            foreach (var rt in routes.EnumerateArray())
            {
                total++;
                string name = rt.GetProperty("name").GetString();
                var start = rt.GetProperty("from");
                var feet = new Vector3(start[0].GetSingle(), start[1].GetSingle(), start[2].GetSingle());
                var wps = rt.GetProperty("waypoints").EnumerateArray()
                    .Select(p => (p[0].GetSingle(), p[1].GetSingle())).ToArray();
                float expected = rt.GetProperty("walk_s").GetSingle();
                bool avoids = rt.GetProperty("avoids_center").GetBoolean();
                if (avoids) ring++;

                var res = Walk(w, feet, wps, expected * 1.5f + 10f);
                string detail = res.Ok
                    ? $"{res.Seconds:F1} s (generatore {expected:F1} s)"
                    : $"bloccato a x={res.P.Pos.X:F1} z={res.P.Pos.Z:F1}";
                bool good = res.Ok;
                if (good && rt.TryGetProperty("end_feet_y", out var ey))
                {
                    float feetY = res.P.Pos.Y - Constants.Height * 0.5f;
                    good = MathF.Abs(feetY - ey.GetSingle()) < 0.06f;
                    detail += $" · piedi a {feetY:F2} m";
                }
                // la simulazione C# e il porting Python devono dare lo stesso tempo (±10%)
                if (good) good = MathF.Abs(res.Seconds - expected) <= expected * 0.10f + 0.5f;
                Check($"rotta: {name}", good, detail);
            }
        }
        // D1: almeno le 4 rotte spawn → estrazione che NON passano dalla piazza
        Check("mappa: anello (D1) — rotte senza piazza nel file", ring >= 4, $"{ring} su {total}");

        // rete di sicurezza: chi cade fuori dal mondo rientra al molo, non in piazza
        var fs = new SimState();
        Simulation.Spawn(fs, 1, new Vector3(0, Constants.WorldFloorY - 5f, 0));
        Simulation.ApplyInput(fs, 1, new InputCmd(), w);
        var fp = fs.Players[1];
        Check("mappa: caduta fuori dal mondo → rientro al molo",
            Vector3.Distance(fp.Pos - new Vector3(0, Constants.Height * 0.5f, 0), w.FallbackFeet) < 0.5f,
            $"pos={fp.Pos}");
    }

    /// <summary>
    /// IL BRIEF (prima del round 1): un minuto al massimo, si chiude prima se hanno scelto
    /// tutti. È la fase `Warmup`, che il server apre quando la lobby si riempie.
    /// </summary>
    private static void MatchBrief()
    {
        var s = new SimState();
        Simulation.Spawn(s, 1, new Vector3(0, 1, 0));
        Simulation.Spawn(s, 2, new Vector3(2, 1, 0));
        var m = new MatchState();
        Match.Reset(m);

        Check("brief: dura un minuto", m.Phase == RoundPhase.Warmup && MathF.Abs(m.TimeLeft - 60f) < 0.01f,
            $"{m.Phase} {m.TimeLeft:F1}s");

        Check("brief: senza scelte resta aperto", !Match.AllDrafted(s, new[] { 1, 2 }));

        Match.TryDraft(m, s.Get(1), Specialization.Decifratore, 0);
        Check("brief: basta uno che non ha scelto", !Match.AllDrafted(s, new[] { 1, 2 }));

        Match.TryDraft(m, s.Get(2), Specialization.Ricognitore, 0);
        Check("brief: con tutti pronti si può chiudere", Match.AllDrafted(s, new[] { 1, 2 }));

        Match.EndBrief(m);
        Check("brief: chiuso → si va agli acquisti",
            m.Phase == RoundPhase.Buy && MathF.Abs(m.TimeLeft - Match.BuySeconds) < 0.01f,
            $"{m.Phase} {m.TimeLeft:F1}s");

        Check("brief: dopo la chiusura non si drafa più",
            !Match.TryDraft(m, s.Get(1), Specialization.Portatore, 0));

        // allo scadere del minuto si va avanti lo stesso, anche se qualcuno non ha scelto
        var m2 = new MatchState();
        Match.Reset(m2);
        for (int i = 0; i < (int)(60f / Constants.Dt) + 2; i++) Match.Tick(m2, Constants.Dt);
        Check("brief: allo scadere del minuto si passa agli acquisti",
            m2.Phase != RoundPhase.Warmup, $"{m2.Phase}");

        // chi non blocca in tempo la prende lo stesso: una specializzazione per posto in coda,
        // sempre la stessa (la simulazione resta riproducibile)
        Check("brief scaduto: chi non ha scelto prende una specializzazione",
            Match.AutoDraft(s, new[] { 1, 2 }) == 0, "avevano già scelto tutti");

        var s3 = new SimState();
        Simulation.Spawn(s3, 1, Vector3.Zero);
        Simulation.Spawn(s3, 3, Vector3.Zero);
        int auto = Match.AutoDraft(s3, new[] { 1, 3 });
        Check("brief scaduto: la scelta automatica è valida e stabile",
            auto == 2 && Draft.IsValid(s3.Get(1).Spec) && s3.Get(1).Spec == Draft.Suggested(1)
                 && s3.Get(3).Spec == Draft.Suggested(3) && s3.Get(1).Spec != s3.Get(3).Spec,
            $"{Draft.Name(s3.Get(1).Spec)} / {Draft.Name(s3.Get(3).Spec)}");

        var m3 = new MatchState();
        Match.Reset(m3);                                              // brief ancora aperto
        var s4 = new SimState();
        Simulation.Spawn(s4, 1, Vector3.Zero);
        Match.TryDraft(m3, s4.Get(1), Specialization.Portatore, 0);
        Match.AutoDraft(s4, new[] { 1 });                              // non deve sovrascrivere
        Check("brief scaduto: chi ha bloccato non viene toccato",
            s4.Get(1).Spec == Specialization.Portatore, $"{s4.Get(1).Spec}");
    }

    /// <summary>
    /// IL CICLO DELLE ARMI, stile Valorant: istanze con le loro munizioni, ricompera,
    /// vendita di quello che hai preso in questa fase, drop di quello che ti portavi
    /// dal round prima, armi a terra con fisica, raccolta e cassa di squadra.
    /// </summary>
    private static void EconomyAndDrops()
    {
        var w = WorldState.MakeTestWorld();
        var s = new SimState();
        var m = new MatchState { Phase = RoundPhase.Buy, Round = 1, TeamFundsNorth = 5000, TeamFundsSouth = 5000 };
        var feet = new Vector3(0f, 1f, 0f);
        Simulation.Spawn(s, 1, feet);
        var p = s.Get(1);
        p.Team = 1;
        p.Credits = Economy.PersonalStart;

        // 1. comprare una primaria: paga la CASSA (sopra i $300), il fondo personale resta
        var r1 = Shop.Buy(s, m, w, 1, WeaponSlot.Cadence);
        Check("negozio: primaria comprata dalla cassa",
            r1.Result == ShopResult.Bought && r1.FromTeam && r1.Price == 900
            && m.TeamFundsNorth == 4100 && p.Credits == 1000
            && Shop.HeldPrimary(p) == WeaponSlot.Cadence && p.Cache[(int)WeaponSlot.Cadence].Ammo == 30,
            $"{r1.Result} cassa {m.TeamFundsNorth} personali {p.Credits}");

        // 1b. IL TASTO 1: mette in mano la TUA primaria, e non una cablata.
        // (il difetto vero: dopo aver comprato un fucile, premere 1 dava un Anvil mai comprato)
        Check("armi: dopo l'acquisto la primaria è quella comprata",
            Shop.HeldPrimary(p) == WeaponSlot.Cadence && p.Weapon.Slot == WeaponSlot.Cadence,
            $"in mano {p.Weapon.Slot}");

        Simulation.TryEquip(s, 1, WeaponSlot.Melee);       // coltello in mano
        Check("armi: il tasto 1 riporta in mano la primaria comprata",
            Simulation.TryEquip(s, 1, WeaponSlot.Cadence) && p.Weapon.Slot == WeaponSlot.Cadence,
            $"in mano {p.Weapon.Slot}");
        Check("armi: non si mette in mano quello che non hai",
            !Simulation.TryEquip(s, 1, WeaponSlot.Anvil) && p.Weapon.Slot == WeaponSlot.Cadence,
            $"in mano {p.Weapon.Slot}");

        // 2. RICOMPRARE la stessa arma: munizioni piene, si paga di nuovo
        p.Cache[(int)WeaponSlot.Cadence].Ammo = 4;
        p.Cache[(int)WeaponSlot.Cadence].Reserve = 9;
        var r2 = Shop.Buy(s, m, w, 1, WeaponSlot.Cadence);
        Check("negozio: ricomprare la stessa arma ridà i proiettili",
            r2.Result == ShopResult.Refilled && p.Cache[(int)WeaponSlot.Cadence].Ammo == 30
            && p.Cache[(int)WeaponSlot.Cadence].Reserve == 90 && m.TeamFundsNorth == 3200,
            $"{r2.Result} ammo {p.Cache[(int)WeaponSlot.Cadence].Ammo} cassa {m.TeamFundsNorth}");

        // 3. sostituire un'arma comprata in QUESTA fase: si VENDE (rimborso intero)
        var r3 = Shop.Buy(s, m, w, 1, WeaponSlot.Anvil);
        Check("negozio: la sostituita comprata in questa fase si vende",
            r3.Result == ShopResult.Bought && r3.Refund == 900 && !r3.OldDropped
            && m.TeamFundsNorth == 2700 && p.Cache[(int)WeaponSlot.Cadence] == null
            && Shop.HeldPrimary(p) == WeaponSlot.Anvil && s.Drops.Count == 0,
            $"rimborso {r3.Refund} cassa {m.TeamFundsNorth} drops {s.Drops.Count}");

        // 4. passato il round: chi sopravvive tiene le armi, con i caricatori pieni
        m.Round = 2;
        m.Phase = RoundPhase.RoundEnd;
        Simulation.StartRound(s, 1, feet, keepLoadout: true);
        m.Phase = RoundPhase.Buy;
        var anvil = p.Cache[(int)WeaponSlot.Anvil];
        Check("round: chi sopravvive tiene l'arma, caricatore pieno e non più vendibile",
            anvil != null && anvil.Ammo == 30 && anvil.Reserve == 90 && !anvil.BoughtThisBuy,
            $"ammo {anvil?.Ammo}/{anvil?.Reserve} vendibile {anvil?.BoughtThisBuy}");

        // 5. la sostituita portata dal round prima NON si vende: si lascia a terra
        var r5 = Shop.Buy(s, m, w, 1, WeaponSlot.Service);
        Check("negozio: la sostituita del round scorso si lascia a terra",
            r5.Result == ShopResult.Bought && r5.OldDropped && r5.Refund == 0
            && s.Drops.Count == 1 && s.Drops[0].Slot == WeaponSlot.Anvil && s.Drops[0].Ammo == 30
            && Shop.HeldPrimary(p) == WeaponSlot.Service,
            $"drop {s.Drops.Count} rimborso {r5.Refund}");

        // 6. G: l'arma in mano parte in avanti, con le sue munizioni
        p.Cache[(int)WeaponSlot.Service].Ammo = 7;
        float z0 = p.Pos.Z;
        bool dropped = Simulation.DropCurrent(s, w, 1);
        var flying = s.Drops[s.Drops.Count - 1];
        Check("drop (G): l'arma parte in avanti con le sue munizioni",
            dropped && s.Drops.Count == 2 && flying.Slot == WeaponSlot.Service && flying.Ammo == 7
            && flying.Vel.Z < 0f && p.Cache[(int)WeaponSlot.Service] == null,
            $"drops {s.Drops.Count} velZ {flying.Vel.Z:F2}");

        // 7. cade, rimbalza e si posa: l'evento che fa suono è LandTick
        for (int i = 0; i < 240; i++) Simulation.StepDrops(s, w);
        Check("drop: cade a terra e si posa (una volta sola)",
            flying.Settled && flying.LandTick >= 0
            && flying.Pos.Y > -0.1f && flying.Pos.Y < 1.5f
            && flying.Pos.Z < z0 + 0.5f,
            $"posata {flying.Settled} tick {flying.LandTick} y {flying.Pos.Y:F2} z {flying.Pos.Z:F2} (da {z0:F2})");

        // 8. la raccolta riporta l'arma IN MANO con le munizioni che aveva
        // (l'Anvil è finito vicino: lo sposto, la raccolta prende la più vicina)
        s.Drops[0].Pos += new Vector3(8f, 0f, 0f);
        p.Pos = flying.Pos + new Vector3(0f, Constants.Height * 0.5f, 0.4f);
        bool picked = Simulation.Pickup(s, w, m, 1, out var pickedSlot);
        Check("raccolta (F): l'arma torna in mano con le sue munizioni",
            picked && pickedSlot == WeaponSlot.Service && s.Drops.Count == 1
            && Shop.HeldPrimary(p) == WeaponSlot.Service && p.Weapon.Ammo == 7,
            $"raccolta {picked} slot {pickedSlot} ammo {p.Weapon.Ammo}");

        // 9. la seconda arma a terra (l'Anvil) resta lì
        Check("raccolta: le altre armi a terra restano dove sono",
            s.Drops.Count == 1 && s.Drops[0].Slot == WeaponSlot.Anvil);

        // 10. fuori dalla fase di acquisto non si compra
        m.Phase = RoundPhase.Live;
        var r10 = Shop.Buy(s, m, w, 1, WeaponSlot.Blitz);
        Check("negozio: fuori dalla fase di acquisto non si compra",
            r10.Result == ShopResult.NotBuyPhase, $"{r10.Result}");
        m.Phase = RoundPhase.Buy;

        // 11. cassa vuota: paga il fondo personale
        var s2 = new SimState();
        var m2 = new MatchState { Phase = RoundPhase.Buy, Round = 1, TeamFundsNorth = 0 };
        Simulation.Spawn(s2, 2, feet);
        var p2 = s2.Get(2);
        p2.Team = 1;
        p2.Credits = Economy.PersonalStart;
        var r11 = Shop.Buy(s2, m2, w, 2, WeaponSlot.Hush);
        Check("economia: con la cassa vuota paga il fondo personale",
            r11.Result == ShopResult.Bought && !r11.FromTeam && p2.Credits == 600
            && Shop.HeldSecondary(p2) == WeaponSlot.Hush,
            $"{r11.Result} personali {p2.Credits}");

        // 12. senza soldi da nessuna parte: rifiutato, e non cambia niente
        p2.Credits = 100;
        var r12 = Shop.Buy(s2, m2, w, 2, WeaponSlot.Hammer);
        Check("economia: senza soldi l'acquisto è rifiutato",
            r12.Result == ShopResult.NotEnough && Shop.HeldSecondary(p2) == WeaponSlot.Hush,
            $"{r12.Result}");

        // 13. morendo le armi cadono dove sei (la Standard gratis no)
        p2.Cache[(int)WeaponSlot.Hush] = null;
        Simulation.Give(s2, 2, WeaponSlot.Anvil);
        int dropsBefore = s2.Drops.Count;
        Simulation.DropOnDeath(s2, w, p2);
        Check("morte: le armi cadono a terra, la Standard gratis resta",
            s2.Drops.Count > dropsBefore
            && s2.Drops.Exists(g => g.Slot == WeaponSlot.Anvil)
            && !s2.Drops.Exists(g => g.Slot == WeaponSlot.Standard));

        // 14. chi muore riparte dalla Standard
        Simulation.StartRound(s2, 2, feet, keepLoadout: false);
        Check("round: chi è morto riparte dalla pistola",
            Shop.HeldPrimary(s2.Get(2)) == null && Shop.HeldSecondary(s2.Get(2)) == WeaponSlot.Standard);

        // 15. la cassa di squadra: i premi entrano lì, non nelle tasche
        var m3 = new MatchState();
        Economy.AddTeamFunds(m3, 1, Match.WinReward);
        Check("economia: il premio di round entra nella cassa di squadra",
            m3.TeamFundsNorth == Economy.TeamStartFunds + Match.WinReward
            && m3.TeamFundsSouth == Economy.TeamStartFunds,
            $"{m3.TeamFundsNorth}/{m3.TeamFundsSouth}");

        // 16. fine round: il campo si pulisce
        Simulation.ClearDrops(s);
        Check("round: le armi a terra non passano il round", s.Drops.Count == 0);

        // 17. il COLTELLO non mira: il destro non fa niente (niente zoom, niente cono chiuso)
        var s4 = new SimState();
        Simulation.Spawn(s4, 4, feet);
        var p4 = s4.Get(4);
        Simulation.TryEquip(s4, 4, WeaponSlot.Melee);
        for (int i = 0; i < 40; i++) Simulation.ApplyWeapon(s4, 4, false, false, true);
        Check("coltello: non si mira",
            p4.Weapon.Slot == WeaponSlot.Melee && p4.Weapon.AdsT == 0f,
            $"in mano {p4.Weapon.Slot} · ads {p4.Weapon.AdsT:F2}");

        // 18. comprare mette SUBITO in mano la nuova arma, qualunque cosa avessi
        var s5 = new SimState();
        var m5 = new MatchState { Phase = RoundPhase.Buy, Round = 1 };
        Simulation.Spawn(s5, 5, feet);
        var p5 = s5.Get(5);
        p5.Team = 1;
        Simulation.TryEquip(s5, 5, WeaponSlot.Melee);      // coltello in mano
        Shop.Buy(s5, m5, w, 5, WeaponSlot.Anvil);
        bool primaryInHand = p5.Weapon.Slot == WeaponSlot.Anvil;
        Shop.Buy(s5, m5, w, 5, WeaponSlot.Hush);           // compro una pistola col fucile in mano
        Check("negozio: l'arma comprata entra subito in mano",
            primaryInHand && p5.Weapon.Slot == WeaponSlot.Hush
            && Shop.HeldPrimary(p5) == WeaponSlot.Anvil,
            $"dopo il fucile {primaryInHand} · ora {p5.Weapon.Slot}");
    }

    /// <summary>
    /// Il cancello architetturale: questo file compila solo se sim/ e net/
    /// non hanno dipendenze dal motore. Se qualcuno aggiunge "using Godot",
    /// il progetto di test non compila più e la CI si ferma.
    /// </summary>
    private static void SimHasNoGodotDependency()
    {
        Check("architettura: sim/ e net/ compilano senza Godot", true);
    }
}
