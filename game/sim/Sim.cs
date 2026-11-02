using System;
using System.Collections.Generic;
using System.Numerics;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>
    /// LA SIMULAZIONE. Funzioni pure, passo fisso esplicito, zero dipendenze dal motore.
    /// Il client la esegue in predizione, il server in autorità, e il replay la riesegue
    /// identica per la riconciliazione. È lo stesso codice, sempre.
    /// </summary>
    public static class Simulation
    {
        private struct MoveResult
        {
            public Vec3 Pos;
            public bool Floor, Ceil, WallX, WallZ;
        }

        public static void Spawn(SimState state, int id, Vec3 feetPos)
        {
            var p = state.GetOrCreate(id);
            p.Pos = feetPos + new Vec3(0, Constants.Height * 0.5f, 0);
            p.Vel = Vec3.Zero;
            p.Yaw = 0;
            p.Pitch = 0;
            p.OnFloor = false;
            p.Crouching = false;
            p.Stamina = Constants.StaminaMax;
            p.JumpCd = 0;
            p.Weapon = new WeaponState();
            p.Weapon.Refill(Arsenal.Standard);
            p.Cache = new WeaponState[Arsenal.Count];
        }

        /// <summary>
        /// Arma NUOVA di zecca: caricatore pieno e contatori a zero. È quello che succede
        /// quando la compri o quando spawni. <see cref="Equip"/> invece è il CAMBIO in mano,
        /// che si porta dietro le munizioni.
        /// </summary>
        public static void Give(SimState state, int id, WeaponSlot slot)
        {
            var p = state.Get(id);
            if (p == null) return;
            int i = (int)slot;
            if (i < 0 || i >= p.Cache.Length) return;

            var fresh = new WeaponState();
            fresh.Refill(Arsenal.Get(slot));
            p.Cache[i] = fresh;
            p.Weapon = fresh;
        }

        /// <summary>
        /// Imposta la modalità, se l'arma la ammette. Non c'è più un tasto per cambiarla:
        /// ogni arma ha UN comportamento (raffica, automatica o semiautomatica) e basta.
        /// La funzione resta per il server e per i test.
        /// </summary>
        public static bool SetMode(SimState state, int id, FireMode mode)
        {
            var p = state.Get(id);
            if (p == null) return false;
            var d = Arsenal.Get(p.Weapon.Slot);
            if ((d.Modes & mode) == 0 || p.Weapon.Mode == mode) return false;
            p.Weapon.Mode = mode;
            p.Weapon.BurstLeft = 0;
            p.Weapon.FireBuffer = 0f;
            return true;
        }

        /// <summary>
        /// Rimette in piedi un giocatore per il round nuovo: vita piena, munizioni piene,
        /// armi nuove (si ricompra), stamina e stato del movimento azzerati.
        /// </summary>
        public static void Respawn(SimState state, int id, Vec3 feet)
        {
            var p = state.Get(id);
            if (p == null) return;

            p.Pos = feet + new Vec3(0f, Constants.Height * 0.5f, 0f);
            p.Vel = Vec3.Zero;
            p.Health = PlayerState.MaxHealth;
            p.Alive = true;
            p.Stamina = Constants.StaminaMax;
            p.JumpCd = 0f;
            p.OnFloor = false;
            p.Crouching = false;

            // armi nuove e pistola di partenza: al round si ricompra tutto
            p.Cache = new WeaponState[Arsenal.Count];
            Give(state, id, WeaponSlot.Standard);
        }

        /// <summary>
        /// INIZIO ROUND, stile Valorant: chi è SOPRAVVISSUTO tiene le armi che aveva e ritrova
        /// i caricatori pieni; chi è morto riparte dalla Standard. È qui che si azzera
        /// `BoughtThisBuy`: da questo momento nessuna vecchia arma è più "di questa fase",
        /// quindi comprare qualcosa di nuovo la lascerà a terra invece di venderla.
        /// </summary>
        public static void StartRound(SimState state, int id, Vec3 feet, bool keepLoadout)
        {
            var p = state.Get(id);
            if (p == null) return;

            p.Pos = feet + new Vec3(0f, Constants.Height * 0.5f, 0f);
            p.Vel = Vec3.Zero;
            p.Health = PlayerState.MaxHealth;
            p.Alive = true;
            p.Stamina = Constants.StaminaMax;
            p.JumpCd = 0f;
            p.OnFloor = false;
            p.Crouching = false;

            if (!keepLoadout)
            {
                p.Cache = new WeaponState[Arsenal.Count];
                Give(state, id, WeaponSlot.Standard);
                return;
            }

            for (int i = 0; i < p.Cache.Length; i++)
            {
                var w = p.Cache[i];
                if (w == null) continue;
                var d = Arsenal.Get((WeaponSlot)i);
                w.Ammo = d.Magazine;                // si riparte con i caricatori pieni
                w.Reserve = d.Reserve;
                w.Reloading = false;
                w.ReloadT = 0f;
                w.FireCd = 0f;
                w.FireBuffer = 0f;
                w.BurstLeft = 0;
                w.AdsT = 0f;
                w.ShotIndex = 0;
                w.BoughtThisBuy = false;            // non è più vendibile: è roba del round scorso
                w.BoughtRound = 0;
                w.PaidFromTeam = false;
            }

            var held = Shop.HeldPrimary(p) ?? Shop.HeldSecondary(p) ?? WeaponSlot.Standard;
            Equip(state, id, held);
        }

        /// <summary>
        /// Mette in mano un'arma SENZA rimettere in cache quella che avevi: dopo un drop
        /// l'arma che avevi in mano non deve tornare in tasca.
        /// </summary>
        private static void EquipAfterDrop(PlayerState p, WeaponSlot slot)
        {
            int i = (int)slot;
            if (i < 0 || i >= p.Cache.Length) return;

            var cached = p.Cache[i];
            if (cached != null)
            {
                p.Weapon = cached;
                p.Weapon.FireCd = 0f;
                p.Weapon.FireBuffer = 0f;
                p.Weapon.BurstLeft = 0;
                return;
            }

            var fresh = new WeaponState();
            fresh.Refill(Arsenal.Get(slot));
            p.Weapon = fresh;
        }

        /// <summary>
        /// Lascia a terra un'arma della cache, con le SUE munizioni. Se era quella in mano si
        /// passa a quella che resta (l'altra arma, poi il coltello).
        /// </summary>
        public static GroundWeapon DropFromCache(SimState state, WorldState world, PlayerState p,
                                                 WeaponSlot slot, bool throwForward)
        {
            int i = (int)slot;
            if (p == null || i < 0 || i >= p.Cache.Length) return null;

            var w = p.Cache[i];
            if (w == null) return null;
            p.Cache[i] = null;

            var g = SpawnDrop(state, p, w, throwForward);
            if (p.Weapon.Slot == slot)
            {
                var next = Shop.HeldPrimary(p) ?? Shop.HeldSecondary(p) ?? WeaponSlot.Melee;
                EquipAfterDrop(p, next);
            }
            return g;
        }

        /// <summary>Il tasto G: l'arma in mano parte in avanti e cade. Il coltello resta.</summary>
        public static bool DropCurrent(SimState state, WorldState world, int id)
        {
            var p = state.Get(id);
            if (p == null || !p.Alive) return false;
            if (Arsenal.Get(p.Weapon.Slot).Melee) return false;
            return DropFromCache(state, world, p, p.Weapon.Slot, throwForward: true) != null;
        }

        /// <summary>
        /// Morendo le armi cadono dove sei (la Standard gratis no: non la vuole nessuno).
        /// Chi passa le raccoglie: è così che un round povero si ribalta.
        /// </summary>
        public static void DropOnDeath(SimState state, WorldState world, PlayerState p)
        {
            if (p == null) return;
            for (int i = 0; i < p.Cache.Length; i++)
            {
                var w = p.Cache[i];
                if (w == null) continue;
                var d = Arsenal.Get((WeaponSlot)i);
                if (d.Melee || d.Price <= 0) continue;
                p.Cache[i] = null;

                var g = SpawnDrop(state, p, w, throwForward: false);
                if (g == null) continue;
                // un po' di dispersione: due armi non finiscono una dentro l'altra
                g.Vel = new Vec3(i % 2 == 0 ? 0.7f : -0.7f, 0.9f, 0.35f);
            }
        }

        private static GroundWeapon SpawnDrop(SimState state, PlayerState p, WeaponState w, bool throwForward)
        {
            var fwd = Arsenal.AimForward(p.Yaw, 0f);
            var eye = new Vec3(p.Pos.X, p.Pos.Y + Constants.Height * 0.5f - 0.25f, p.Pos.Z);

            // "leggermente avanti": parte dalla mano, un filo più in basso
            var pos = throwForward
                ? eye + fwd * 0.35f - new Vec3(0f, 0.30f, 0f)
                : new Vec3(p.Pos.X, p.Pos.Y - Constants.Height * 0.5f + 0.25f, p.Pos.Z) + fwd * 0.25f;

            var g = new GroundWeapon
            {
                Id = state.NextDropId(),
                Slot = w.Slot,
                Ammo = w.Ammo,
                Reserve = w.Reserve,
                BoughtThisBuy = w.BoughtThisBuy,
                BoughtRound = w.BoughtRound,
                PaidFromTeam = w.PaidFromTeam,
                Mode = w.Mode,
                Pos = pos,
                // "leggermente avanti": ~1,2 m di volo, non un tiro da bocce. Il ferro pesa.
                Vel = throwForward ? fwd * 2.2f + new Vec3(0f, 1.25f, 0f) : Vec3.Zero,
                Spin = 0.8f,
                OwnerId = p.Id,
            };
            state.Drops.Add(g);
            return g;
        }

        /// <summary>
        /// Fisica delle armi a terra: gravità, un rimbalzo, poi si posano. Quando toccano terra
        /// `LandTick` viene scritto: è l'evento che fa suonare il ferro che sbatte.
        /// </summary>
        public static void StepDrops(SimState state, WorldState world, float dt = Constants.Dt)
        {
            for (int i = state.Drops.Count - 1; i >= 0; i--)
            {
                var g = state.Drops[i];
                if (g.Settled) continue;

                g.Vel.Y -= Constants.Gravity * dt;
                g.Pos += g.Vel * dt;
                g.Spin += dt * 2.2f;

                var probe = Trace.Ray(world, g.Pos + new Vec3(0f, 0.6f, 0f), -Vec3.UnitY, 1.6f);
                if (probe.Hit && g.Pos.Y <= probe.Point.Y + 0.03f)
                {
                    g.Pos.Y = probe.Point.Y + 0.03f;
                    if (MathF.Abs(g.Vel.Y) > 2.4f)
                        g.Vel = new Vec3(g.Vel.X * 0.35f, -g.Vel.Y * 0.28f, g.Vel.Z * 0.35f);  // un rimbalzo solo
                    else
                    {
                        g.Vel = Vec3.Zero;
                        g.Settled = true;
                        g.LandTick = state.Tick;
                    }
                }

                if (g.Pos.Y < -3f) state.Drops.RemoveAt(i);      // caduta fuori dal mondo
            }
        }

        /// <summary>
        /// Raccoglie l'arma a terra più vicina (entro 2 m). Lo slot occupato si libera con la
        /// stessa regola del negozio: venduta se comprata in questa fase, altrimenti a terra.
        /// </summary>
        public static bool Pickup(SimState state, WorldState world, MatchState match, int id, out WeaponSlot slot)
        {
            slot = WeaponSlot.Standard;
            var p = state.Get(id);
            if (p == null || !p.Alive) return false;

            GroundWeapon best = null;
            float bestD = 4f;                       // 2 m al quadrato
            for (int i = 0; i < state.Drops.Count; i++)
            {
                var g = state.Drops[i];
                float dx = g.Pos.X - p.Pos.X, dz = g.Pos.Z - p.Pos.Z;
                float dy = g.Pos.Y - p.Pos.Y;
                float d = dx * dx + dz * dz + dy * dy * 0.35f;
                if (d < bestD) { bestD = d; best = g; }
            }
            if (best == null) return false;

            slot = best.Slot;
            state.Drops.Remove(best);
            Shop.ReplaceForPickup(state, match, world, p, best.Slot);

            var w = new WeaponState();
            w.Refill(Arsenal.Get(best.Slot));
            w.Ammo = best.Ammo;
            w.Reserve = best.Reserve;
            w.BoughtThisBuy = best.BoughtThisBuy;
            w.BoughtRound = best.BoughtRound;
            w.PaidFromTeam = best.PaidFromTeam;
            w.Mode = best.Mode;
            p.Cache[(int)best.Slot] = w;
            Equip(state, id, best.Slot);
            return true;
        }

        /// <summary>A fine round il campo si pulisce: le armi a terra non passano il round.</summary>
        public static void ClearDrops(SimState state) => state.Drops.Clear();

        /// <summary>Equipaggia un'arma (cambio in mano). Ricarica i contatori degli eventi.</summary>
        public static void Equip(SimState state, int id, WeaponSlot slot)
        {
            var p = state.Get(id);
            if (p == null) return;

            // metti via l'arma in mano con le sue munizioni, e riprendi quella che avevi
            int i = (int)slot;
            if (i < 0 || i >= p.Cache.Length) return;
            p.Cache[(int)p.Weapon.Slot] = p.Weapon;

            var cached = p.Cache[i];
            if (cached != null)
            {
                p.Weapon = cached;
                p.Weapon.FireCd = 0f;       // il cambio non azzera la cadenza, ma non la conserva
                p.Weapon.FireBuffer = 0f;
                p.Weapon.BurstLeft = 0;
                return;
            }

            p.Weapon = new WeaponState();
            p.Weapon.Refill(Arsenal.Get(slot));
        }

        /// <summary>
        /// Mette in mano SOLO quello che il giocatore ha davvero (il coltello c'è sempre).
        /// È la porta del client: <see cref="Equip"/> invece è permissiva e crea l'arma se manca,
        /// comodo per test e stanza di prova — ma è così che un tasto cablato male ti metteva
        /// in mano un Anvil mai comprato.
        /// </summary>
        public static bool TryEquip(SimState state, int id, WeaponSlot slot)
        {
            var p = state.Get(id);
            if (p == null) return false;

            int i = (int)slot;
            if (i < 0 || i >= p.Cache.Length) return false;
            if (!Arsenal.Get(slot).Melee && p.Cache[i] == null) return false;   // non ce l'hai
            if (p.Weapon.Slot == slot) return false;                             // già in mano

            Equip(state, id, slot);
            return true;
        }

        /// <summary>
        /// Logica dell'arma: cadenza, caricatore, ricarica tattica/a vuoto, ADS.
        /// Gli eventi visivi viaggiano come contatori, così il client non deve indovinare.
        /// </summary>
        public static void ApplyWeapon(SimState state, int id, bool fire, bool reload, bool ads, float dt = Constants.Dt)
        {
            var p = state.Get(id);
            if (p == null) return;
            var w = p.Weapon;
            var d = Arsenal.Get(w.Slot);

            // Il resto che avanza sotto il tick si porta al colpo successivo: con il solo
            // azzeramento, a 64 Hz la cadenza vera era il multiplo di tick successivo (una
            // SMG da 837 RPM ne faceva 640). Il pavimento a -dt evita che l'attesa si accumuli.
            w.FireCd = MathF.Max(-dt, w.FireCd - dt);

            // mira: interpolazione lineare verso il valore richiesto.
            // Il COLTELLO non ha mirino: niente ADS, mai — né zoom, né chiusura del cono,
            // né mirino nascosto. Il destro col coltello in mano non fa nulla.
            float step = dt / MathF.Max(0.01f, d.AdsTime);
            w.AdsT = d.Melee ? 0f
                : ads ? MathF.Min(1f, w.AdsT + step)
                : MathF.Max(0f, w.AdsT - step);

            // il cono si richiude da solo e si riapre se ti muovi: è la stessa formula
            // di client, server e test (Arsenal.SpreadAt)
            w.Bloom = MathF.Max(0f, w.Bloom - d.BloomDecay * dt);
            float hspeed = MathF.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Z * p.Vel.Z);

            // --- dispersione: la SERIE di colpi. I primi N colpi sono puliti, poi il cono
            // si apre a ogni colpo (stile CS/Valorant) e si richiude quando smetti.
            w.SinceShot += dt;
            if (w.SinceShot >= d.SpreadReset) w.ShotIndex = 0;
            w.Spread = Arsenal.SpreadAt(d, hspeed, p.OnFloor, p.Crouching, w.AdsT, w.ShotIndex);
            w.Bloom = MathF.Max(0f, w.Spread - Arsenal.SpreadAt(d, hspeed, p.OnFloor, p.Crouching, w.AdsT, 0));

            // --- grilletto -------------------------------------------------------
            // `fire` è lo STATO del grilletto, non il fronte: il click lo ricava la
            // simulazione (FireLatch), così tenere premuto funziona anche in rete.
            bool click = fire && !w.FireLatch;
            w.FireLatch = fire;
            w.BurstTimer = MathF.Max(0f, w.BurstTimer - dt);

            // --- raffica: un click fa partire la raffica, e TENENDO PREMUTO le raffiche
            // si susseguono (3 colpi, pausa, 3 colpi...). Nessun tasto per cambiare modo.
            bool burstWeapon = w.Mode == FireMode.Burst;
            if (burstWeapon)
            {
                if (click) w.BurstTimer = 0f;      // un click nuovo apre subito la raffica
                if (fire && w.BurstLeft <= 0 && w.BurstTimer <= 0f && w.Ammo > 0)
                    w.BurstLeft = System.Math.Max(1, d.BurstCount);
            }

            // --- click in anticipo (semi) e grilletto tenuto (auto)
            if (click || (w.Mode == FireMode.Auto && fire))
            {
                w.FireBuffer = d.FireBuffer;
            }
            else
            {
                w.FireBuffer = MathF.Max(0f, w.FireBuffer - dt);
            }

            if (w.Reloading)
            {
                w.FireBuffer = 0f;      // ricaricando non si spara
                w.BurstLeft = 0;
                w.ReloadT -= dt;
                if (w.ReloadT <= 0f)
                {
                    int need = d.Magazine - w.Ammo;
                    int take = Math.Min(need, w.Reserve);
                    w.Ammo += take;
                    w.Reserve -= take;
                    w.Reloading = false;
                    w.ReloadT = 0f;
                }
                return;
            }

            // il coltello non ha munizioni e non si ricarica: un colpo per click, e basta
            if (d.Melee)
            {
                if (w.FireBuffer <= 0f || w.FireCd > 0f) return;
                w.FireCd += d.Cycle;      // porta avanti il resto sotto il tick
                w.FireBuffer = 0f;
                w.ShotSpread = 0f;
                w.ShotSeed++;
                w.Shots++;
                w.Ammo = d.Magazine;
                w.Reserve = d.Reserve;
                return;
            }

            if (reload && w.Ammo < d.Magazine && w.Reserve > 0)
            {
                w.FireBuffer = 0f;
                w.BurstLeft = 0;
                w.Reloading = true;
                w.ReloadWasEmpty = w.Ammo == 0;
                w.ReloadT = w.ReloadWasEmpty ? d.ReloadEmpty : d.ReloadTactical;
                w.ReloadStarts++;
                return;
            }

            // si spara se c'è una raffica aperta o un click in coda
            if (w.FireBuffer <= 0f && w.BurstLeft <= 0) return;

            if (w.Ammo <= 0)
            {
                // a vuoto scatta subito: metterlo in coda non avrebbe senso
                w.DryFires++;
                w.FireCd = 0.30f;
                w.FireBuffer = 0f;
                w.BurstLeft = 0;
                w.ShotIndex = 0;
                return;
            }

            if (w.FireCd > 0f) return;        // il click resta in coda: spara appena è pronto

            w.Ammo--;
            w.FireCd += d.Cycle;      // porta avanti il resto sotto il tick
            w.FireBuffer = 0f;

            // il colpo parte col cono di ADESSO (la serie è ancora quella prima del colpo)
            w.ShotSpread = w.Spread;
            w.ShotSeed++;

            w.Shots++;
            if (w.Ammo == 0) w.ShotsLast++;

            // il colpo entra nella serie: il prossimo cono è più largo
            w.ShotIndex++;
            w.SinceShot = 0f;
            w.Spread = Arsenal.SpreadAt(d, hspeed, p.OnFloor, p.Crouching, w.AdsT, w.ShotIndex);
            w.Bloom = MathF.Max(0f, w.Spread - Arsenal.SpreadAt(d, hspeed, p.OnFloor, p.Crouching, w.AdsT, 0));

            // raffica: un colpo se ne va dalla coda. Finita la raffica parte la pausa.
            if (w.BurstLeft > 0)
            {
                w.BurstLeft--;
                if (w.BurstLeft <= 0) w.BurstTimer = d.BurstReset;
            }
        }

        /// <summary>Avanza lo stato di un tick e applica gli input ricevuti.</summary>
        public static void Step(SimState state, IReadOnlyDictionary<int, InputCmd> inputs, WorldState world, float dt = Constants.Dt)
        {
            state.Tick++;
            foreach (var kv in inputs)
            {
                ApplyInput(state, kv.Key, kv.Value, world, dt);
                ApplyWeapon(state, kv.Key, kv.Value.Fire, kv.Value.Reload, kv.Value.Ads, dt);
            }
        }

        /// <summary>
        /// Avanza UN giocatore senza toccare il tick. È quello che serve al replay
        /// della riconciliazione: riapplicare gli input non confermati.
        /// </summary>
        public static void ApplyInput(SimState state, int id, InputCmd cmd, WorldState world, float dt = Constants.Dt)
        {
            var p = state.Get(id);
            if (p == null) return;
            StepPlayer(p, cmd, world, dt);
        }

        private static void StepPlayer(PlayerState p, InputCmd cmd, WorldState world, float dt)
        {
            p.Yaw = cmd.Yaw;
            p.Pitch = Math.Clamp(cmd.Pitch, -1.55f, 1.55f);
            p.Interacting = cmd.Interact;
            p.JumpCd = MathF.Max(0f, p.JumpCd - dt);

            var halfStand = new Vec3(Constants.Radius, Constants.Height * 0.5f, Constants.Radius);
            var halfCrouch = new Vec3(Constants.Radius, Constants.CrouchHeight * 0.5f, Constants.Radius);

            // --- accovacciato: si abbassa subito, si rialza solo se c'è spazio
            bool wantCrouch = cmd.Crouch && p.OnFloor;
            if (p.Crouching && !wantCrouch)
            {
                var raised = p.Pos + new Vec3(0, (Constants.Height - Constants.CrouchHeight) * 0.5f, 0);
                if (!OverlapsWorld(raised, halfStand, world))
                {
                    p.Pos = raised;
                    p.Crouching = false;
                }
                // altrimenti resta accovacciato: c'è un soffitto basso
            }
            else if (wantCrouch && !p.Crouching)
            {
                p.Pos.Y -= (Constants.Height - Constants.CrouchHeight) * 0.5f;
                p.Crouching = true;
            }

            var half = p.Crouching ? halfCrouch : halfStand;

            // --- velocità massima consentita da questo input.
            // Il peso dell'arma conta (doc 11): le pistole non rallentano quasi, il Longshot
            // toglie il 12%, il coltello dà l'8% in più. In mira vale AdsMoveScale.
            var wd = Arsenal.Get(p.Weapon.Slot);
            bool moving = cmd.Move.Length() > 0.01f;
            float maxSpeed = Constants.WalkSpeed * wd.MoveScale;
            bool sprinting = false;
            if (p.Crouching)
            {
                maxSpeed = Constants.CrouchSpeed * wd.MoveScale;
            }
            else if (cmd.Ads)
            {
                maxSpeed = Constants.WalkSpeed * wd.AdsMoveScale;
            }
            else if (cmd.Sprint && moving && p.Stamina > 0f)
            {
                maxSpeed = Constants.SprintSpeed * wd.MoveScale;
                sprinting = true;
            }

            // --- stamina
            if (sprinting && p.OnFloor)
                p.Stamina = MathF.Max(0f, p.Stamina - dt);
            else
                p.Stamina = MathF.Min(Constants.StaminaMax, p.Stamina + Constants.StaminaMax / Constants.StaminaRecovery * dt);

            // --- direzione desiderata, in spazio mondo (-Z è avanti)
            float sy = MathF.Sin(p.Yaw), cy = MathF.Cos(p.Yaw);
            var forward = new Vec3(-sy, 0f, -cy);
            var right = new Vec3(cy, 0f, -sy);
            var wish = right * cmd.Move.X + forward * cmd.Move.Y;
            if (wish.LengthSquared() > 1e-6f) wish = Vec3.Normalize(wish);
            float wishSpeed = maxSpeed * Math.Clamp(cmd.Move.Length(), 0f, 1f);

            // --- attrito e accelerazione
            if (p.OnFloor)
            {
                p.Vel = Friction(p.Vel, dt);
                p.Vel = Accelerate(p.Vel, wish, wishSpeed, Constants.GroundAccel, dt);
            }
            else
            {
                // controllo aereo limitato: niente bhop, niente air-strafe
                p.Vel = Accelerate(p.Vel, wish, MathF.Min(wishSpeed, Constants.MaxAirWishSpeed), Constants.AirAccel, dt);
            }

            // --- salto
            if (cmd.Jump && p.OnFloor && p.JumpCd <= 0f)
            {
                p.Vel.Y = Constants.JumpVelocity;
                p.OnFloor = false;
                p.JumpCd = Constants.JumpCooldown;
            }

            // --- gravità / aggancio al suolo
            if (p.OnFloor)
                p.Vel.Y = -2f;   // piccola spinta verso il basso: serve a RESTARE a terra
            else
                p.Vel.Y -= Constants.Gravity * dt;

            // --- movimento e collisioni, un asse alla volta
            bool wasOnFloor = p.OnFloor;
            bool jumped = p.Vel.Y > 0f;
            var delta = p.Vel * dt;
            var r = Move(p.Pos, half, delta, world);

            // --- gradini: se un muro basso ci ha fermato, si prova a salirci sopra.
            // Senza questo, la simulazione (solo AABB, niente rampe) non sa fare le scale.
            if (wasOnFloor && !jumped && (r.WallX || r.WallZ))
            {
                if (TryStepUp(p.Pos, half, delta, world, out var stepped)
                    && HorizontalDist2(stepped.Pos, p.Pos) > HorizontalDist2(r.Pos, p.Pos) + 1e-8f)
                    r = stepped;
            }

            // --- aggancio in discesa: scendendo una scala si resta a terra invece di
            // "saltellare" gradino per gradino (e di perdere il controllo a ogni alzata)
            if (wasOnFloor && !jumped && !r.Floor)
            {
                var probe = Move(r.Pos, half, new Vec3(0f, -Constants.StepHeight, 0f), world);
                if (probe.Floor)
                {
                    r.Pos = probe.Pos;
                    r.Floor = true;
                }
            }

            p.Pos = r.Pos;
            p.OnFloor = r.Floor;
            if (r.Floor && p.Vel.Y < 0f) p.Vel.Y = 0f;
            if (r.Ceil && p.Vel.Y > 0f) p.Vel.Y = 0f;
            if (r.WallX) p.Vel.X = 0f;
            if (r.WallZ) p.Vel.Z = 0f;

            // --- caduto fuori dal mondo
            if (p.Pos.Y < Constants.WorldFloorY)
            {
                p.Pos = world.FallbackFeet + new Vec3(0f, Constants.Height * 0.5f, 0f);
                p.Vel = Vec3.Zero;
                p.Crouching = false;
            }
        }

        // ------------------------------------------------------------------ movimento

        private static Vec3 Friction(Vec3 vel, float dt)
        {
            float speed = MathF.Sqrt(vel.X * vel.X + vel.Z * vel.Z);
            if (speed < 0.01f) return new Vec3(0f, vel.Y, 0f);
            float drop = speed * Constants.Friction * dt;
            float newSpeed = MathF.Max(speed - drop, 0f);
            float s = newSpeed / speed;
            return new Vec3(vel.X * s, vel.Y, vel.Z * s);
        }

        private static Vec3 Accelerate(Vec3 vel, Vec3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.LengthSquared() < 1e-8f || wishSpeed <= 0f) return vel;
            float current = vel.X * wishDir.X + vel.Z * wishDir.Z;
            float add = wishSpeed - current;
            if (add <= 0f) return vel;
            float accelSpeed = MathF.Min(accel * wishSpeed * dt, add);
            return new Vec3(vel.X + wishDir.X * accelSpeed, vel.Y, vel.Z + wishDir.Z * accelSpeed);
        }

        private static MoveResult Move(Vec3 pos, Vec3 half, Vec3 delta, WorldState world)
        {
            var r = new MoveResult { Pos = pos };

            r.Pos.X += delta.X;
            Resolve(ref r, half, world, 0, delta.X);

            r.Pos.Z += delta.Z;
            Resolve(ref r, half, world, 2, delta.Z);

            r.Pos.Y += delta.Y;
            Resolve(ref r, half, world, 1, delta.Y);

            return r;
        }

        /// <summary>
        /// Risolve la penetrazione su UN asse, usando la DIREZIONE del movimento.
        /// Usare il centro della scatola per decidere "pavimento o soffitto" è sbagliato:
        /// un giocatore in piedi ha il centro sopra quello del pavimento, quindi verrebbe
        /// classificato come "soffitto". La direzione non mente.
        /// </summary>
        private static void Resolve(ref MoveResult r, Vec3 half, WorldState world, int axis, float delta)
        {
            if (MathF.Abs(delta) < 1e-9f) return;

            // solo le scatole vicine al volume spazzato da questo asse (stesso ordine della lista)
            var sweep = new Vec3(axis == 0 ? MathF.Abs(delta) : 0f, 0f, axis == 2 ? MathF.Abs(delta) : 0f);
            var near = world.Query(r.Pos - half - sweep - new Vec3(0.05f), r.Pos + half + sweep + new Vec3(0.05f));
            var boxes = world.Boxes;
            for (int n = 0; n < near.Count; n++)
            {
                var b = boxes[near[n]];
                if (!Overlaps(r.Pos, half, b)) continue;

                if (axis == 0)
                {
                    r.Pos.X = delta > 0f ? b.Min.X - half.X : b.Max.X + half.X;
                    r.WallX = true;
                }
                else if (axis == 2)
                {
                    r.Pos.Z = delta > 0f ? b.Min.Z - half.Z : b.Max.Z + half.Z;
                    r.WallZ = true;
                }
                else
                {
                    if (delta < 0f)
                    {
                        // stava scendendo: era SOPRA la scatola, va spinto in su sul tetto
                        r.Pos.Y = b.Max.Y + half.Y;
                        r.Floor = true;
                    }
                    else
                    {
                        // stava salendo: era SOTTO la scatola, va spinto in giù sul fondo
                        r.Pos.Y = b.Min.Y - half.Y;
                        r.Ceil = true;
                    }
                }
            }
        }

        /// <summary>
        /// Tolleranza di contatto (0,1 mm). Chi sta IN PIEDI su una scatola ha i piedi
        /// "esattamente" sul suo tetto, ma in virgola mobile 0,165 + 0,9 − 0,9 può dare
        /// 0,16499999: senza tolleranza il giocatore risulterebbe compenetrato nel gradino
        /// su cui poggia e verrebbe spinto di lato. Un contatto non è una compenetrazione.
        /// </summary>
        public const float ContactSkin = 1e-4f;

        private static bool Overlaps(Vec3 pos, Vec3 half, Box b)
        {
            var min = pos - half;
            var max = pos + half;
            var bMin = b.Min;
            var bMax = b.Max;
            return !(max.X <= bMin.X + ContactSkin || min.X >= bMax.X - ContactSkin
                  || max.Y <= bMin.Y + ContactSkin || min.Y >= bMax.Y - ContactSkin
                  || max.Z <= bMin.Z + ContactSkin || min.Z >= bMax.Z - ContactSkin);
        }

        /// <summary>Vero se il volume è in collisione con QUALSIASI scatola del mondo.</summary>
        private static bool OverlapsWorld(Vec3 pos, Vec3 half, WorldState world)
        {
            var near = world.Query(pos - half, pos + half);
            var boxes = world.Boxes;
            for (int n = 0; n < near.Count; n++)
                if (Overlaps(pos, half, boxes[near[n]])) return true;
            return false;
        }

        /// <summary>
        /// Gradino: su di StepHeight, avanti in orizzontale, giù fino al pavimento.
        /// Riesce solo se si atterra su qualcosa: niente "gradini" nel vuoto.
        /// </summary>
        private static bool TryStepUp(Vec3 pos, Vec3 half, Vec3 delta, WorldState world, out MoveResult result)
        {
            result = default;
            var up = Move(pos, half, new Vec3(0f, Constants.StepHeight, 0f), world);
            float raised = up.Pos.Y - pos.Y;
            if (raised < 0.01f) return false;                  // soffitto basso: niente gradino

            var across = Move(up.Pos, half, new Vec3(delta.X, 0f, delta.Z), world);
            var down = Move(across.Pos, half, new Vec3(0f, -(raised + 0.01f), 0f), world);
            if (!down.Floor) return false;

            result = down;
            result.WallX = across.WallX;
            result.WallZ = across.WallZ;
            return true;
        }

        private static float HorizontalDist2(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }
    }
}
