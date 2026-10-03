namespace AgentFlow.Api.Agents;

public class EmailSenderAgent : IAgent
{
    public string Type => "email-sender";

    public Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var to      = ctx.Input.TryGetValue("to", out var t) ? t?.ToString() : "(unset)";
        var subject = ctx.Input.TryGetValue("subject", out var s) ? s?.ToString() : "AgentFlow notification";
        var body    = ctx.Input.TryGetValue("body", out var b) ? b?.ToString() : "";

        ctx.Log($"[EMAIL] to={to}\nsubject={subject}\n---\n{body}");
        return Task.FromResult<object?>(new { sent = true, to, subject, chars = body?.Length ?? 0 });
    }
}