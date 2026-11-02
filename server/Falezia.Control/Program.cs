using Falezia.Control;

// ---------------------------------------------------------------------------
// FALEZIA · control plane
//
// Un processo da avviare. Tiene insieme tre cose che altrimenti vivono separate:
//   · il PANNELLO WEB (HttpListener, HTML incorporato, zero pacchetti esterni);
//   · la CODA di matchmaking, con il limite rigido di istanze;
//   · le ISTANZE di gioco, cioè i processi del server dedicato.
//
// Sviluppo:  dotnet run --project server/Falezia.Control
// VPS:       Falezia.Control --bind=0.0.0.0 --public=IP --data=/var/lib/falezia
// ---------------------------------------------------------------------------

var cfg = ControlConfig.Load(args);
var log = new LogHub(cfg.MaxLogLines);
var instances = new InstanceManager(cfg, log);
var matchmaker = new Matchmaker(cfg, instances, log);
var started = DateTime.UtcNow;

log.Info($"avvio · radice {cfg.RepoRoot}");
log.Info($"configurazione in {cfg.ConfigFile}");

if (cfg.AutoBuild)
{
    var (ok, _) = await instances.BuildAsync();
    if (!ok) log.Warn("il server non compila: nessuna nuova partita può partire");
}

if (cfg.AutoStartInstance)
{
    try
    {
        var first = instances.Start();
        log.Info($"pronta l'istanza {first.Id} · UDP {first.Port}");
    }
    catch (Exception e)
    {
        log.Error($"istanza di partenza non avviata: {e.Message}");
    }
}

using var api = new ApiServer(cfg, log, instances, matchmaker, started);
api.Start();

// la coda gira da sola, una volta al secondo
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1000);
        try { matchmaker.Tick(); }
        catch (Exception e) { log.Error($"coda: {e.Message}"); }
    }
});

var quit = new ManualResetEventSlim();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => quit.Set();

PrintBanner(cfg);
quit.Wait();

log.Info("arresto: fermo le istanze");
instances.StopAll();
api.Stop();
await Task.Delay(200);

static void PrintBanner(ControlConfig cfg)
{
    string where = cfg.Bind is "0.0.0.0" or "::" or "*" ? "127.0.0.1" : cfg.Bind;
    Console.WriteLine();
    Console.WriteLine("  ┌────────────────────────────────────────────────────────────────┐");
    Console.WriteLine("  │  FALEZIA · control plane                                       │");
    Console.WriteLine("  ├────────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"  │  PANNELLO  http://{where}:{cfg.Port}/");
    Console.WriteLine($"  │  TOKEN     {cfg.AdminToken}");
    Console.WriteLine($"  │  LIMITE    {(cfg.HardLimitEnabled ? $"ON · {cfg.HardLimitMaxInstances} istanze" : "OFF")}");
    Console.WriteLine($"  │  DATI      {cfg.ConfigFile}");
    Console.WriteLine("  └────────────────────────────────────────────────────────────────┘");
    Console.WriteLine();
}
