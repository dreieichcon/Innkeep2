using System.Threading.Channels;
using Serilog.Core;
using Serilog.Events;

namespace Innkeep2.Services.Shared;

/// <summary>
/// Forwards log events to a slow sink on a background thread, so a stalled sink (e.g. a webhook)
/// never blocks the code that is logging. Events are dropped if the queue is full.
/// </summary>
public sealed class BackgroundSink : ILogEventSink, IDisposable
{
    private readonly ILogEventSink _inner;
    private readonly Channel<LogEvent> _channel = Channel.CreateBounded<LogEvent>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
    private readonly Task _worker;

    public BackgroundSink(ILogEventSink inner)
    {
        _inner = inner;
        _worker = Task.Run(ProcessAsync);
    }

    public void Emit(LogEvent logEvent) => _channel.Writer.TryWrite(logEvent);

    private async Task ProcessAsync()
    {
        await foreach (var logEvent in _channel.Reader.ReadAllAsync())
        {
            try
            {
                _inner.Emit(logEvent);
            }
            catch
            {
                // Logging must never take the application down.
            }
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _worker.Wait(TimeSpan.FromSeconds(2));
        (_inner as IDisposable)?.Dispose();
    }
}
