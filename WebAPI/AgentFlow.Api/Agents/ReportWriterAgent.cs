using System.Text.Json;
using AgentFlow.Api.Services;

namespace AgentFlow.Api.Agents;

public class ReportWriterAgent : IAgent
{
    private readonly GroqService _llm;
    public ReportWriterAgent(GroqService llm) => _llm = llm;
    public string Type => "report-writer";

    public async Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var tone  = ctx.Input.TryGetValue("tone",  out var t)  ? t?.ToString()  : "executive";
        var title = ctx.Input.TryGetValue("title", out var ti) ? ti?.ToString() : "Report";
        var analysis = ctx.Input.TryGetValue("analysis", out var a) ? a
            : throw new InvalidOperationException("report-writer requires input.analysis");

        var body = analysis is JsonElement je ? je.GetRawText() : JsonSerializer.Serialize(analysis);
        if (body.Length > 12000) body = body[..12000] + "\n...[truncated]";

        var system =
            "You are a professional report writer. Produce a clean Markdown report with the title, " +
            "an executive summary, key findings, and recommended actions. " +
            $"Tone: {tone}. No preamble, only the report.";

        ctx.Log($"ReportWriter tone={tone}, title={title}, model={_llm.Model}");
        var md = await _llm.ChatAsync(system, $"Title: {title}\n\nAnalysis:\n{body}", ctx.Ct);
        return new { markdown = md };
    }
}