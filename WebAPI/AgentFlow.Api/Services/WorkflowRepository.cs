using System.Text.Json;
using AgentFlow.Api.Data;
using AgentFlow.Api.Models;
using Dapper;

namespace AgentFlow.Api.Services;

public class WorkflowRepository
{
    private readonly Db _db;
    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);

    public WorkflowRepository(Db db) => _db = db;

    // -------- Agents --------
    public async Task<IReadOnlyList<AgentRecord>> ListAgentsAsync()
    {
        using var c = _db.Open();
        var rows = await c.QueryAsync<(string id, string name, string type, string? description, string config, DateTime created_at)>(
            "SELECT id,name,type,description,config::text,created_at FROM agents ORDER BY created_at DESC");
        return rows.Select(r => new AgentRecord(r.id, r.name, r.type, r.description,
            JsonSerializer.Deserialize<Dictionary<string, object?>>(r.config, J) ?? new(), r.created_at)).ToList();
    }

    public async Task UpsertAgentAsync(AgentRecord a)
    {
        using var c = _db.Open();
        await c.ExecuteAsync("""
            INSERT INTO agents (id, name, type, description, config)
            VALUES (@Id, @Name, @Type, @Description, @Config::jsonb)
            ON CONFLICT (id) DO UPDATE
              SET name=EXCLUDED.name, type=EXCLUDED.type,
                  description=EXCLUDED.description, config=EXCLUDED.config
            """,
            new { a.Id, a.Name, a.Type, a.Description, Config = JsonSerializer.Serialize(a.Config, J) });
    }

    // -------- Workflows --------
    public async Task<IReadOnlyList<WorkflowRecord>> ListWorkflowsAsync()
    {
        using var c = _db.Open();
        var rows = await c.QueryAsync<(string id, string name, string? description, string definition, DateTime created_at, DateTime updated_at)>(
            "SELECT id,name,description,definition::text,created_at,updated_at FROM workflows ORDER BY updated_at DESC");
        return rows.Select(r => new WorkflowRecord(r.id, r.name, r.description,
            JsonSerializer.Deserialize<WorkflowDefinitionJson>(r.definition, J)!, r.created_at, r.updated_at)).ToList();
    }

    public async Task<WorkflowRecord?> GetWorkflowAsync(string id)
    {
        using var c = _db.Open();
        var r = await c.QuerySingleOrDefaultAsync<(string id, string name, string? description, string definition, DateTime created_at, DateTime updated_at)?>(
            "SELECT id,name,description,definition::text,created_at,updated_at FROM workflows WHERE id=@id", new { id });
        if (r is null) return null;
        return new WorkflowRecord(r.Value.id, r.Value.name, r.Value.description,
            JsonSerializer.Deserialize<WorkflowDefinitionJson>(r.Value.definition, J)!,
            r.Value.created_at, r.Value.updated_at);
    }

    public async Task UpsertWorkflowAsync(WorkflowDefinitionJson def)
    {
        using var c = _db.Open();
        await c.ExecuteAsync("""
            INSERT INTO workflows (id, name, description, definition)
            VALUES (@Id, @Name, @Description, @Definition::jsonb)
            ON CONFLICT (id) DO UPDATE
              SET name=EXCLUDED.name, description=EXCLUDED.description,
                  definition=EXCLUDED.definition, updated_at=NOW()
            """,
            new { def.Id, def.Name, def.Description, Definition = JsonSerializer.Serialize(def, J) });
    }

    // -------- Runs --------
    public async Task<Guid> CreateRunAsync(string workflowId, Dictionary<string, object?>? input, IEnumerable<StepDefinitionJson> steps)
    {
        var id = Guid.NewGuid();
        using var c = _db.Open();
        using var tx = c.BeginTransaction();

        await c.ExecuteAsync("""
            INSERT INTO workflow_runs (id, workflow_id, status, input, context)
            VALUES (@id, @wf, 'queued', @input::jsonb, '{}'::jsonb)
            """,
            new { id, wf = workflowId, input = JsonSerializer.Serialize(input ?? new(), J) }, tx);

        foreach (var s in steps)
        {
            await c.ExecuteAsync("""
                INSERT INTO step_runs (id, run_id, step_id, name, type, status)
                VALUES (@id, @run, @sid, @name, @type, 'pending')
                """,
                new { id = Guid.NewGuid(), run = id, sid = s.Id, name = s.Name, type = s.Type ?? "agent" }, tx);
        }

        tx.Commit();
        return id;
    }

    public async Task<WorkflowRun?> GetRunAsync(Guid id)
    {
        using var c = _db.Open();
        var r = await c.QuerySingleOrDefaultAsync<(Guid id, string workflow_id, string status, string? input, string context, string? error, DateTime started_at, DateTime? finished_at)?>(
            "SELECT id,workflow_id,status,input::text,context::text,error,started_at,finished_at FROM workflow_runs WHERE id=@id", new { id });
        if (r is null) return null;
        return new WorkflowRun(r.Value.id, r.Value.workflow_id, r.Value.status,
            r.Value.input is null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(r.Value.input, J),
            JsonSerializer.Deserialize<Dictionary<string, object?>>(r.Value.context, J) ?? new(),
            r.Value.error, r.Value.started_at, r.Value.finished_at);
    }

    public async Task<IReadOnlyList<WorkflowRun>> ListRunsAsync(int take = 50)
    {
        using var c = _db.Open();
        var rows = await c.QueryAsync<(Guid id, string workflow_id, string status, string? input, string context, string? error, DateTime started_at, DateTime? finished_at)>(
            "SELECT id,workflow_id,status,input::text,context::text,error,started_at,finished_at FROM workflow_runs ORDER BY started_at DESC LIMIT @take",
            new { take });
        return rows.Select(r => new WorkflowRun(r.id, r.workflow_id, r.status,
            r.input is null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(r.input, J),
            JsonSerializer.Deserialize<Dictionary<string, object?>>(r.context, J) ?? new(),
            r.error, r.started_at, r.finished_at)).ToList();
    }

    public async Task<IReadOnlyList<StepRun>> GetStepsAsync(Guid runId)
    {
        using var c = _db.Open();
        var rows = await c.QueryAsync<(Guid id, Guid run_id, string step_id, string name, string type, string status, int attempt, string? input, string? output, string? error, string? logs, DateTime? started_at, DateTime? finished_at)>(
            "SELECT id,run_id,step_id,name,type,status,attempt,input::text,output::text,error,logs,started_at,finished_at FROM step_runs WHERE run_id=@runId ORDER BY started_at NULLS LAST, name",
            new { runId });
        return rows.Select(r => new StepRun(r.id, r.run_id, r.step_id, r.name, r.type, r.status, r.attempt,
            r.input  is null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(r.input,  J),
            r.output is null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(r.output, J),
            r.error, r.logs, r.started_at, r.finished_at)).ToList();
    }

    public async Task UpdateRunStatusAsync(Guid id, string status, string? error = null)
    {
        using var c = _db.Open();
        await c.ExecuteAsync("""
            UPDATE workflow_runs
            SET status=@status, error=COALESCE(@error, error),
                finished_at = CASE WHEN @status IN ('succeeded','failed','cancelled') THEN NOW() ELSE finished_at END
            WHERE id=@id
            """, new { id, status, error });
    }

    public async Task UpdateRunContextAsync(Guid id, Dictionary<string, object?> ctx)
    {
        using var c = _db.Open();
        await c.ExecuteAsync("UPDATE workflow_runs SET context=@ctx::jsonb WHERE id=@id",
            new { id, ctx = JsonSerializer.Serialize(ctx, J) });
    }

    public async Task UpdateStepAsync(Guid stepId,
        string? status = null, int? attempt = null, Dictionary<string, object?>? input = null,
        Dictionary<string, object?>? output = null, string? error = null, string? logs = null,
        bool start = false, bool finish = false)
    {
        using var c = _db.Open();
        var sets = new List<string>();
        if (status  is not null) sets.Add("status=@status");
        if (attempt is not null) sets.Add("attempt=@attempt");
        if (input   is not null) sets.Add("input=@input::jsonb");
        if (output  is not null) sets.Add("output=@output::jsonb");
        if (error   is not null) sets.Add("error=@error");
        if (logs    is not null) sets.Add("logs=@logs");
        if (start)   sets.Add("started_at=NOW()");
        if (finish)  sets.Add("finished_at=NOW()");
        if (sets.Count == 0) return;

        await c.ExecuteAsync($"UPDATE step_runs SET {string.Join(",", sets)} WHERE id=@stepId", new
        {
            stepId, status, attempt,
            input  = input  is null ? null : JsonSerializer.Serialize(input,  J),
            output = output is null ? null : JsonSerializer.Serialize(output, J),
            error, logs
        });
    }

    public async Task<StepRun?> GetStepAsync(Guid runId, string stepId)
    {
        var all = await GetStepsAsync(runId);
        return all.FirstOrDefault(s => s.StepId == stepId);
    }
}
