using AgentFlow.Api.Engine;
using AgentFlow.Api.Models;
using AgentFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentFlow.Api.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly WorkflowRepository _repo;
    private readonly RunQueue _queue;

    public WorkflowsController(WorkflowRepository repo, RunQueue queue)
    {
        _repo = repo;
        _queue = queue;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowRecord>>> List()
        => Ok(await _repo.ListWorkflowsAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowRecord>> Get(string id)
    {
        var wf = await _repo.GetWorkflowAsync(id);
        return wf is null ? NotFound() : Ok(wf);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionJson>> Save([FromBody] WorkflowDefinitionJson def)
    {
        if (string.IsNullOrWhiteSpace(def.Id))            return BadRequest("id is required");
        if (def.Steps is null || def.Steps.Count == 0)    return BadRequest("steps required");

        var ids = def.Steps.Select(s => s.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)          return BadRequest("duplicate step ids");

        foreach (var s in def.Steps)
        {
            foreach (var d in s.DependsOn ?? new())
                if (!ids.Contains(d))
                    return BadRequest($"step '{s.Id}' depends on unknown '{d}'");
        }

        await _repo.UpsertWorkflowAsync(def);
        return CreatedAtAction(nameof(Get), new { id = def.Id }, def);
    }

    [HttpPost("{id}/run")]
    public async Task<ActionResult<RunCreatedResponse>> Run(string id, [FromBody] RunWorkflowRequest? body)
    {
        var wf = await _repo.GetWorkflowAsync(id);
        if (wf is null) return NotFound();

        var runId = await _repo.CreateRunAsync(
            id, body?.Input ?? new Dictionary<string, object?>(), wf.Definition.Steps);
        await _queue.EnqueueAsync(runId);

        return AcceptedAtAction("Get", "Runs", new { id = runId }, new RunCreatedResponse(runId));
    }
}