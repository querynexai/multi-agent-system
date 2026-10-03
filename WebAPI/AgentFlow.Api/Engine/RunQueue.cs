using System.Threading.Channels;

namespace AgentFlow.Api.Engine;

public class RunQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    public ValueTask EnqueueAsync(Guid runId) => _channel.Writer.WriteAsync(runId);
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

public class RunWorker : BackgroundService
{
    private readonly RunQueue _queue;
    private readonly WorkflowEngine _engine;
    private readonly ILogger<RunWorker> _log;

    public RunWorker(RunQueue queue, WorkflowEngine engine, ILogger<RunWorker> log)
    { _queue = queue; _engine = engine; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var runId in _queue.ReadAllAsync(stoppingToken))
        {
            try { await _engine.ExecuteAsync(runId, stoppingToken); }
            catch (Exception ex) { _log.LogError(ex, "Run {Run} crashed.", runId); }
        }
    }
}