namespace AgentFlow.Api.Agents;

public class AgentContext
{
    public string AgentId { get; init; } = "";
    public Dictionary<string, object?> Config { get; init; } = new();
    public Dictionary<string, object?> Input { get; init; } = new();
    public Dictionary<string, object?> WorkflowContext { get; init; } = new();
    public Action<string> Log { get; init; } = _ => { };
    public CancellationToken Ct { get; init; }
}