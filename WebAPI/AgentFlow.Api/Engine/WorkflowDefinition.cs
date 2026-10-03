using AgentFlow.Api.Models;

namespace AgentFlow.Api.Engine;

public static class WorkflowDefinitionExtensions
{
    public static StepDefinitionJson? Find(this WorkflowDefinitionJson def, string id)
        => def.Steps.FirstOrDefault(s => s.Id == id);
}
