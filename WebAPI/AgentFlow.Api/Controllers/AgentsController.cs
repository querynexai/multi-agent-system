using AgentFlow.Api.Agents;
using AgentFlow.Api.Models;
using AgentFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentFlow.Api.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly WorkflowRepository _repo;
    private readonly AgentRegistry _registry;

    public AgentsController(WorkflowRepository repo, AgentRegistry registry)
    {
        _repo = repo;
        _registry = registry;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentRecord>>> List()
        => Ok(await _repo.ListAgentsAsync());

    [HttpGet("types")]
    public ActionResult<AgentTypesResponse> Types()
        => Ok(new AgentTypesResponse(_registry.AvailableTypes));

    [HttpPost]
    public async Task<ActionResult<AgentRecord>> Create([FromBody] CreateAgentRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Id))   return BadRequest("id is required");
        if (string.IsNullOrWhiteSpace(input.Type)) return BadRequest("type is required");

        var record = new AgentRecord(
            input.Id, input.Name, input.Type, input.Description,
            input.Config ?? new(), DateTime.UtcNow);

        await _repo.UpsertAgentAsync(record);
        _registry.RegisterRecord(record);
        return CreatedAtAction(nameof(List), new { id = record.Id }, record);
    }
}