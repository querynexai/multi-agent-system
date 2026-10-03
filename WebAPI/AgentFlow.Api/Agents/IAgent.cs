namespace AgentFlow.Api.Agents;

/// <summary>
/// Contract every agent must implement.
/// The <see cref="Type"/> string is what workflow steps reference indirectly —
/// via the agent record's <c>type</c> column (agents table), which is matched
/// against this value by <see cref="AgentRegistry"/>.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Unique type identifier for this agent implementation.
    /// Matches <c>agents.type</c> in the database.
    /// Examples: "llm", "data-extractor", "analyzer", "report-writer",
    /// "email-sender", "http".
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Executes the agent.
    /// Return value is normalised to a JSON object and stored as the step's output,
    /// then exposed to downstream steps via <c>${&lt;stepId&gt;.output.&lt;field&gt;}</c>.
    /// Throw to signal failure — the engine will apply the step's retry policy.
    /// </summary>
    Task<object?> ExecuteAsync(AgentContext ctx);
}