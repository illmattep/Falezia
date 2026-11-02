using Falezia.Server;

// ---------------------------------------------------------------------------
// FALEZIA · server dedicato
//   dotnet run --project server/Falezia.Server -- --port=27015
//   ./Falezia.Server --port=27015 --map=calanera.json --max=10
// Comandi da console: status · round · kick <id> · quit
// ---------------------------------------------------------------------------

var cfg = ServerConfig.Parse(args);
var server = new GameServer(cfg);

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
    server.Stop();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => server.Stop();

server.Start();

// console di amministrazione su un thread separato: il loop di gioco non si ferma
var console = new Thread(() =>
{
    while (!stop.IsCancellationRequested)
    {
        string line = Console.ReadLine();
        if (line == null) break;
        server.Command(line);
    }
})
{ IsBackground = true, Name = "console" };
console.Start();

server.Run();

