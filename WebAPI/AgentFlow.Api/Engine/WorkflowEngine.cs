using System.Text.Json;
using AgentFlow.Api.Agents;
using AgentFlow.Api.Models;
using AgentFlow.Api.Services;

namespace AgentFlow.Api.Engine;

public class WorkflowEngine
{
    private readonly WorkflowRepository _repo;
    private readonly AgentRegistry _registry;
    private readonly ILogger<WorkflowEngine> _log;
    private readonly int _maxParallel;
    private readonly int _defaultTimeout;

    public WorkflowEngine(WorkflowRepository repo, AgentRegistry registry,
        IConfiguration cfg, ILogger<WorkflowEngine> log)
    {
        _repo = repo; _registry = registry; _log = log;
        _maxParallel = int.TryParse(cfg["Engine:MaxParallelPerWave"], out var mp) ? mp : 4;
        _defaultTimeout = int.TryParse(cfg["Engine:DefaultTimeoutSeconds"], out var t) ? t : 120;
    }

    public async Task ExecuteAsync(Guid runId, CancellationToken ct)
    {
        var run = await _repo.GetRunAsync(runId) ?? throw new InvalidOperationException("Run not found");
        var wf  = await _repo.GetWorkflowAsync(run.WorkflowId) ?? throw new InvalidOperationException("Workflow not found");
        var defs = wf.Definition.Steps.ToDictionary(s => s.Id);

        await _repo.UpdateRunStatusAsync(runId, "running");

        while (!ct.IsCancellationRequested)
        {
            var steps = await _repo.GetStepsAsync(runId);
            run = (await _repo.GetRunAsync(runId))!;

            if (steps.Any(s => s.Status == "waiting_approval"))
            {
                await _repo.UpdateRunStatusAsync(runId, "waiting_approval");
                _log.LogInformation("Run {Run} waiting for approval.", runId);
                return;
            }

            var failed = steps.FirstOrDefault(s => s.Status == "failed");
            if (failed is not null)
            {
                await _repo.UpdateRunStatusAsync(runId, "failed", failed.Error);
                return;
            }

            var completed = steps.Where(s => s.Status is "succeeded" or "skipped").Select(s => s.StepId).ToHashSet();

            if (steps.All(s => s.Status is "succeeded" or "skipped"))
            {
                await _repo.UpdateRunStatusAsync(runId, "succeeded");
                _log.LogInformation("Run {Run} succeeded.", runId);
                return;
            }

            var ready = steps
                .Where(s => s.Status == "pending")
                .Where(s => (defs[s.StepId].DependsOn ?? new()).All(completed.Contains))
                .ToList();

            var toRun = new List<StepRun>();
            foreach (var sr in ready)
            {
                var def = defs[sr.StepId];
                if (!string.IsNullOrWhiteSpace(def.When))
                {
                    var ctx = ToJsonElement(run.Context);
                    var v = ExpressionEvaluator.Resolve(def.When!, ctx);
                    if (!IsTruthy(v))
                    {
                        await _repo.UpdateStepAsync(sr.Id, status: "skipped", finish: true, logs: $"Skipped: when={def.When}");
                        continue;
                    }
                }
                toRun.Add(sr);
            }

            if (toRun.Count == 0 && ready.Count > 0) continue;

            if (toRun.Count == 0)
            {
                await _repo.UpdateRunStatusAsync(runId, "failed", "No runnable steps; check DAG or dependencies.");
                return;
            }

            using var sem = new SemaphoreSlim(_maxParallel);
            var tasks = toRun.Select(sr => RunStepAsync(sr, defs[sr.StepId], runId, ct, sem)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task RunStepAsync(StepRun sr, StepDefinitionJson def, Guid runId, CancellationToken ct, SemaphoreSlim sem)
    {
        await sem.WaitAsync(ct);
        try
        {
            if ((def.Type ?? "agent").Equals("approval", StringComparison.OrdinalIgnoreCase))
            {
                await _repo.UpdateStepAsync(sr.Id, status: "waiting_approval", start: true);
                return;
            }

            var run = (await _repo.GetRunAsync(runId))!;
            var ctx = ToJsonElement(run.Context);

            var resolved = ExpressionEvaluator.ResolveObject(def.Input, ctx);
            var inputDict = resolved switch
            {
                Dictionary<string, object?> d => d,
                JsonElement je when je.ValueKind == JsonValueKind.Object => je.EnumerateObject()
                    .ToDictionary(p => p.Name, p => (object?)p.Value),
                _ => new Dictionary<string, object?>()
            };

            var rec = _registry.GetRecord(def.Agent ?? "") ?? throw new InvalidOperationException($"Agent '{def.Agent}' not registered.");
            var impl = _registry.GetImplementation(rec.Type) ?? throw new InvalidOperationException($"Agent type '{rec.Type}' has no implementation.");

            await _repo.UpdateStepAsync(sr.Id, status: "running", input: inputDict, start: true);

            var logs = new List<string>();
            var retry = def.Retry ?? new RetryJson(3, 1000);
            var timeout = def.TimeoutSeconds ?? _defaultTimeout;

            try
            {
                var output = await RetryPolicy.RunAsync(async attempt =>
                {
                    await _repo.UpdateStepAsync(sr.Id, attempt: attempt);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    var agentCtx = new AgentContext
                    {
                        AgentId = rec.Id,
                        Config = rec.Config,
                        Input = inputDict,
                        WorkflowContext = run.Context,
                        Log = msg => logs.Add($"[{DateTime.UtcNow:HH:mm:ss}] {msg}"),
                        Ct = cts.Token
                    };

                    return await impl.ExecuteAsync(agentCtx);
                }, retry.MaxAttempts, retry.BaseDelayMs,
                   (attempt, ex) => logs.Add($"[retry {attempt}] {ex.Message}"),
                   ct);

                var outputDict = NormalizeToDictionary(output);
                await _repo.UpdateStepAsync(sr.Id, status: "succeeded", output: outputDict,
                    logs: string.Join('\n', logs), finish: true);

                var latest = (await _repo.GetRunAsync(runId))!;
                latest.Context[sr.StepId] = new Dictionary<string, object?> { ["output"] = outputDict };
                await _repo.UpdateRunContextAsync(runId, latest.Context);
            }
            catch (Exception ex)
            {
                logs.Add($"ERROR: {ex.Message}");
                await _repo.UpdateStepAsync(sr.Id, status: "failed", error: ex.Message,
                    logs: string.Join('\n', logs), finish: true);
            }
        }
        finally
        {
            sem.Release();
        }
    }

    private static Dictionary<string, object?> NormalizeToDictionary(object? o)
    {
        if (o is null) return new();
        if (o is Dictionary<string, object?> d) return d;
        if (o is string s) return new() { ["text"] = s };

        var json = JsonSerializer.Serialize(o);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            // IMPORTANT: .Clone() detaches each child from the JsonDocument's memory,
            // which is disposed at the end of this method. Without it, downstream
            // serialization throws ObjectDisposedException.
            return doc.RootElement
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => (object?)p.Value.Clone());
        }

        return new() { ["value"] = o };
    }

    private static JsonElement ToJsonElement(Dictionary<string, object?> ctx)
    {
        var json = JsonSerializer.Serialize(ctx);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static bool IsTruthy(object? v) => v switch
    {
        null => false,
        bool b => b,
        string s => !string.IsNullOrWhiteSpace(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
        JsonElement je => je.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(je.GetString()),
            JsonValueKind.Number => je.GetDouble() != 0,
            _ => true
        },
        _ => true
    };
}
