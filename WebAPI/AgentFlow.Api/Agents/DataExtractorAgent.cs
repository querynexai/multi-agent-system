using System.Text.Json;
using AgentFlow.Api.Data;
using Dapper;

namespace AgentFlow.Api.Agents;

public class DataExtractorAgent : IAgent
{
    private readonly Db _db;
    public DataExtractorAgent(Db db) => _db = db;
    public string Type => "data-extractor";

    public async Task<object?> ExecuteAsync(AgentContext ctx)
    {
        var query = ctx.Input.TryGetValue("query", out var q) ? q?.ToString()
            : throw new InvalidOperationException("data-extractor requires input.query");

        var trimmed = query.TrimStart();
        if (!trimmed.StartsWith("select", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("with",   StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT/WITH queries are allowed.");
        if (query.Contains(';') && query.TrimEnd().TrimEnd(';').Contains(';'))
            throw new InvalidOperationException("Multi-statement queries are not allowed.");

        var parameters = ctx.Input.TryGetValue("params", out var p) && p is JsonElement je && je.ValueKind == JsonValueKind.Object
            ? je.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.ToString())
            : new Dictionary<string, object?>();

        ctx.Log($"SQL: {query}");
        using var conn = _db.Open();
        var rows = (await conn.QueryAsync(query, parameters)).ToList();
        ctx.Log($"Returned {rows.Count} rows.");
        return new { rows, rowCount = rows.Count };
    }
}