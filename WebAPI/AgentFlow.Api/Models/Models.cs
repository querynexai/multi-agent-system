namespace AgentFlow.Api.Models;

public record AgentRecord(
    string Id, string Name, string Type, string? Description,
    Dictionary<string, object?> Config, DateTime CreatedAt);

public record WorkflowRecord(
    string Id, string Name, string? Description,
    WorkflowDefinitionJson Definition, DateTime CreatedAt, DateTime UpdatedAt);

public record WorkflowRun(
    Guid Id, string WorkflowId, string Status,
    Dictionary<string, object?>? Input, Dictionary<string, object?> Context,
    string? Error, DateTime StartedAt, DateTime? FinishedAt);

public record StepRun(
    Guid Id, Guid RunId, string StepId, string Name, string Type, string Status,
    int Attempt, Dictionary<string, object?>? Input, Dictionary<string, object?>? Output,
    string? Error, string? Logs, DateTime? StartedAt, DateTime? FinishedAt);

public record WorkflowDefinitionJson(
    string Id, string Name, string? Description, List<StepDefinitionJson> Steps);

public record StepDefinitionJson(
    string Id, string Name, string? Agent, string? Type,
    List<string>? DependsOn, Dictionary<string, object?>? Input,
    RetryJson? Retry, int? TimeoutSeconds, List<string>? Approvers,
    string? When);

public record RetryJson(int MaxAttempts, int BaseDelayMs);