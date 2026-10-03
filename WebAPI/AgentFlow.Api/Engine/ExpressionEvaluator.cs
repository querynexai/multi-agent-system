using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentFlow.Api.Engine;

public static class ExpressionEvaluator
{
    private static readonly Regex Token = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);

    public static object? Resolve(string template, JsonElement context)
    {
        var full = Token.Match(template);
        if (full.Success && full.Index == 0 && full.Length == template.Length)
            return Lookup(full.Groups[1].Value, context);

        return Token.Replace(template, m =>
        {
            var v = Lookup(m.Groups[1].Value, context);
            return v switch
            {
                null => "",
                string s => s,
                JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString()!,
                JsonElement je => je.GetRawText(),
                _ => JsonSerializer.Serialize(v)
            };
        });
    }

    public static object? ResolveObject(object? node, JsonElement context)
    {
        switch (node)
        {
            case null: return null;
            case string s: return Resolve(s, context);
            case JsonElement je:
                return je.ValueKind switch
                {
                    JsonValueKind.String => Resolve(je.GetString()!, context),
                    JsonValueKind.Object => je.EnumerateObject().ToDictionary(p => p.Name, p => ResolveObject(p.Value, context)),
                    JsonValueKind.Array  => je.EnumerateArray().Select(e => ResolveObject(e, context)).ToList(),
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                    JsonValueKind.True   => true,
                    JsonValueKind.False  => false,
                    _ => null
                };
            case IDictionary<string, object?> dict:
                return dict.ToDictionary(kv => kv.Key, kv => ResolveObject(kv.Value, context));
            default: return node;
        }
    }

    private static object? Lookup(string path, JsonElement ctx)
    {
        var parts = path.Split('.');
        if (!ctx.TryGetProperty(parts[0], out var cur)) return null;
        for (int i = 1; i < parts.Length; i++)
        {
            if (cur.ValueKind == JsonValueKind.Object && cur.TryGetProperty(parts[i], out var next)) cur = next;
            else return null;
        }
        return cur.ValueKind switch
        {
            JsonValueKind.String => cur.GetString(),
            JsonValueKind.Number => cur.TryGetInt64(out var l) ? l : cur.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array or JsonValueKind.Object => cur,
            _ => null
        };
    }
}