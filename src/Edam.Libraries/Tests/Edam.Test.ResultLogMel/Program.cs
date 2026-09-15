// BL-4.1 runtime verification: the additive MEL bridge on ResultLog must forward
// logged entries to an injected Microsoft.Extensions.Logging logger.
using Edam.Diagnostics;
using Microsoft.Extensions.Logging;

var factory = new CapturingFactory();
var log = new ResultLog();
log.UseLogging(factory);          // bind via ILoggerFactory (composition-root path)
log.Failed("bridge works");       // should forward to MEL at Error/Critical level

if (factory.Entries.Count == 0)
{
   Console.WriteLine("FAIL: no MEL entry captured (bridge did not forward).");
   return 1;
}

var (msg, level, _) = factory.Entries[0];
bool ok = msg == "bridge works" && level >= LogLevel.Warning;
Console.WriteLine(ok
   ? $"PASS: MEL bridge forwarded (level={level}, msg=\"{msg}\")"
   : $"FAIL: forwarded but unexpected (level={level}, msg=\"{msg}\")");
return ok ? 0 : 1;

sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
{
   private readonly List<(string msg, LogLevel level, Exception? ex)> _sink;
   public CapturingLogger(List<(string msg, LogLevel level, Exception? ex)> sink) => _sink = sink;
   public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
   public bool IsEnabled(LogLevel logLevel) => true;
   public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
      Func<TState, Exception?, string> formatter)
      => _sink.Add((formatter(state, exception), logLevel, exception));
   private sealed class NullScope : IDisposable
   {
      public static readonly NullScope Instance = new();
      public void Dispose() { }
   }
}

sealed class CapturingFactory : ILoggerFactory, ILoggerProvider
{
   public List<(string msg, LogLevel level, Exception? ex)> Entries { get; } = new();
   public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);
   public void AddProvider(ILoggerProvider provider) { }
   public void Dispose() { }
}
