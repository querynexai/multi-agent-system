using AgentFlow.Api.Models;

namespace AgentFlow.Api.Agents;

public class AgentRegistry
{
    private readonly Dictionary<string, IAgent> _byType;
    private readonly Dictionary<string, AgentRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public AgentRegistry(IEnumerable<IAgent> agents)
        => _byType = agents.ToDictionary(a => a.Type, StringComparer.OrdinalIgnoreCase);

    public void RegisterRecord(AgentRecord rec) => _records[rec.Id] = rec;
    public IEnumerable<AgentRecord> Records => _records.Values;
    public AgentRecord? GetRecord(string id) => _records.TryGetValue(id, out var r) ? r : null;
    public IAgent? GetImplementation(string type) => _byType.TryGetValue(type, out var a) ? a : null;
    public IEnumerable<string> AvailableTypes => _byType.Keys;
}
