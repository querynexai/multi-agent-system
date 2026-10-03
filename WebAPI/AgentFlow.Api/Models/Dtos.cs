namespace AgentFlow.Api.Models;

// ---------- Requests ----------

/// <summary>Body for POST /api/workflows/{id}/run. Free-form context for the workflow.</summary>
public class RunWorkflowRequest
{
    public Dictionary<string, object?> Input { get; set; } = new();
}

/// <summary>Body for POST /api/runs/{runId}/steps/{stepId}/approve.</summary>
public class ApprovalRequest
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

/// <summary>Body for POST /api/agents. Mirrors AgentRecord but kept separate for contract stability.</summary>
public class CreateAgentRequest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public Dictionary<string, object?> Config { get; set; } = new();
}

// ---------- Responses ----------

public record RunDetailResponse(WorkflowRun Run, IReadOnlyList<StepRun> Steps);

public record AgentTypesResponse(IEnumerable<string> Implementations);

public record RunCreatedResponse(Guid RunId);

public record ApprovalResponse(bool Resumed);

public record HealthResponse(bool Ok, DateTime Time);