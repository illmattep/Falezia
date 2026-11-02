using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Falezia.Control;

/// <summary>Una riga di log, con la sorgente (control, istanza, client) e un numero progressivo.</summary>
public sealed class LogLine
{
    public long Seq { get; set; }
    public string Source { get; set; } = "control";
    public string Level { get; set; } = "info";
    public string Text { get; set; } = "";
    public DateTime At { get; set; } = DateTime.Now;
}

/// <summary>
/// Il registro condiviso: un buffer circolare per la storia recente e un canale per
/// ogni pannello aperto. Il pannello riceve le righe via SSE, quindi non c'è polling.
/// </summary>
public sealed class LogHub
{
    private readonly object _lock = new();
    private readonly List<LogLine> _ring = new();
    private readonly List<Channel<LogLine>> _subs = new();
    private readonly int _cap;
    private long _seq;

    public LogHub(int capacity) => _cap = Math.Max(200, capacity);

    public LogLine Add(string source, string level, string text)
    {
        if (string.IsNullOrEmpty(text)) text = "";
        var line = new LogLine
        {
            Seq = Interlocked.Increment(ref _seq),
            Source = source,
            Level = level,
            Text = text.Length > 2000 ? text[..2000] : text,
            At = DateTime.Now
        };

        lock (_lock)
        {
            _ring.Add(line);
            if (_ring.Count > _cap) _ring.RemoveRange(0, _ring.Count - _cap);
            foreach (var ch in _subs) ch.Writer.TryWrite(line);
        }
        return line;
    }

    public LogLine Info(string text, string source = "control") => Add(source, "info", text);
    public LogLine Warn(string text, string source = "control") => Add(source, "warn", text);
    public LogLine Error(string text, string source = "control") => Add(source, "error", text);

    public List<LogLine> Tail(int count)
    {
        lock (_lock) return _ring.Skip(Math.Max(0, _ring.Count - count)).ToList();
    }

    public ChannelReader<LogLine> Subscribe(out IDisposable subscription)
    {
        var ch = Channel.CreateBounded<LogLine>(new BoundedChannelOptions(2048)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        lock (_lock) _subs.Add(ch);
        subscription = new Unsubscriber(this, ch);
        return ch.Reader;
    }

    private sealed class Unsubscriber(LogHub hub, Channel<LogLine> ch) : IDisposable
    {
        public void Dispose()
        {
            lock (hub._lock) hub._subs.Remove(ch);
            ch.Writer.TryComplete();
        }
    }
}
