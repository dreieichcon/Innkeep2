using Serilog.Core;
using Serilog.Events;

namespace Innkeep2.Services.Shared;

/// <summary>
/// Forwards log events to a sink that can be replaced at runtime. Events are dropped while no sink is set.
/// </summary>
public sealed class SwitchableSink : ILogEventSink, IDisposable
{
    private volatile ILogEventSink? _inner;

    public void Emit(LogEvent logEvent) => _inner?.Emit(logEvent);

    /// <summary>
    /// Replaces the target sink and disposes the previous one.
    /// </summary>
    public void Replace(ILogEventSink? sink)
    {
        var previous = Interlocked.Exchange(ref _inner, sink);
        (previous as IDisposable)?.Dispose();
    }

    public void Dispose() => Replace(null);
}
