using System.Text.Json;
using AgentFlow.Api.Services;

namespace AgentFlow.Api.Agents;

public class AnalyzerAgent : IAgent
{
    private readonly GroqService _llm;
    public AnalyzerAgent(GroqService llm) => _llm = llm;
    public string Type => "analyzer";

    public async Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var system = ctx.Config.TryGetValue("system", out var s) ? s?.ToString()
            : "You are a data analyst. Given data, produce (1) a one-paragraph executive summary and " +
              "(2) 3-5 bullet highlights. Be specific with numbers.";

        var useBulk = ctx.Config.TryGetValue("bulk", out var b)
            && b is not null
            && bool.TryParse(b.ToString(), out var bulkFlag) && bulkFlag;
        // Default: analyzer uses bulk model unless explicitly disabled.
        var useBulkModel = !ctx.Config.ContainsKey("bulk") || useBulk;

        var payload = ctx.Input.TryGetValue("data", out var d) ? d : ctx.Input;
        var prompt = payload is JsonElement je ? je.GetRawText() : JsonSerializer.Serialize(payload);
        if (prompt.Length > 12000) prompt = prompt[..12000] + "\n...[truncated]";

        ctx.Log($"Analyzer invoking LLM (model={ (useBulkModel ? _llm.BulkModel : _llm.Model) }).");
        var text = await _llm.ChatAsync(system, prompt, ctx.Ct, bulk: useBulkModel);

        var bullets = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("- ") || l.StartsWith("* "))
            .Select(l => l[2..])
            .ToList();

        return new { text, bullets };
    }
}