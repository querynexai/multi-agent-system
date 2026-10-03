using AgentFlow.Api.Services;

namespace AgentFlow.Api.Agents;

public class LlmAgent : IAgent
{
    private readonly GroqService _llm;
    public LlmAgent(GroqService llm) => _llm = llm;
    public string Type => "llm";

    public async Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var system = (ctx.Config.TryGetValue("system", out var s) ? s?.ToString() : null)
                     ?? "You are a precise assistant. Respond concisely.";
        var promptField = ctx.Config.TryGetValue("promptField", out var pf) ? pf?.ToString() : null ?? "prompt";
        var prompt = ctx.Input.TryGetValue(promptField, out var p) ? p?.ToString()
                     : string.Join("\n", ctx.Input.Select(kv => $"{kv.Key}: {kv.Value}"));

        ctx.Log($"LLM invoke (model={_llm.Model}). Prompt length={prompt?.Length ?? 0}");
        var text = await _llm.ChatAsync(system, prompt ?? "", ctx.Ct);
        return new { text };
    }
}