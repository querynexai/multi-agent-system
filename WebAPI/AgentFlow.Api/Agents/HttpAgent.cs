using System.Text.Json;

namespace AgentFlow.Api.Agents;

public class HttpAgent : IAgent
{
    private readonly HttpClient _http;
    public HttpAgent(IHttpClientFactory f) => _http = f.CreateClient("agents");
    public string Type => "http";

    public async Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var url    = ctx.Input.TryGetValue("url", out var u) ? u?.ToString()
            : throw new InvalidOperationException("http agent requires input.url");
        var method = (ctx.Input.TryGetValue("method", out var m) ? m?.ToString() : "GET")!.ToUpperInvariant();
        var body   = ctx.Input.TryGetValue("body", out var b) ? b : null;

        using var req = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null && method != "GET")
        {
            var json = body is JsonElement je ? je.GetRawText() : JsonSerializer.Serialize(body);
            req.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        ctx.Log($"{method} {url}");
        using var res = await _http.SendAsync(req, ctx.Ct);
        var text = await res.Content.ReadAsStringAsync(ctx.Ct);
        ctx.Log($"HTTP {(int)res.StatusCode} ({text.Length} bytes)");
        return new { status = (int)res.StatusCode, body = text };
    }
}