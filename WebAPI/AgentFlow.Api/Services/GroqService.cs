using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgentFlow.Api.Services;

public class GroqService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _bulkModel;
    private readonly string _apiKey;
    private readonly ILogger<GroqService> _log;

    public string Model => _model;
    public string BulkModel => _bulkModel;
    public bool Enabled => !string.IsNullOrWhiteSpace(_apiKey);

    public GroqService(HttpClient http, IConfiguration cfg, ILogger<GroqService> log)
    {
        _http = http;
        _log = log;

        _model     = cfg["Groq:Model"]     ?? "openai/gpt-oss-120b";
        _bulkModel = cfg["Groq:BulkModel"] ?? "openai/gpt-oss-20b";
        _apiKey    = cfg["Groq:ApiKey"]    ?? "";

        var baseUrl = cfg["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1/";
        _http.BaseAddress = new Uri(baseUrl);
        _http.Timeout = TimeSpan.FromSeconds(90);
    }

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct, bool bulk = false)
    {
        if (!Enabled)
        {
            _log.LogWarning("Groq API key missing — returning stub.");
            return $"[AI disabled] Would have answered:\n{user[..Math.Min(user.Length, 400)]}...";
        }

        var payload = new
        {
            model = bulk ? _bulkModel : _model,
            temperature = 0.3,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user",   content = user }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Groq {(int)res.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}