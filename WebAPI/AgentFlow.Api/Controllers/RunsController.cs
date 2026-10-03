using AgentFlow.Api.Engine;
using AgentFlow.Api.Models;
using AgentFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentFlow.Api.Controllers;

[ApiController]
[Route("api/runs")]
public class RunsController : ControllerBase
{
    private readonly WorkflowRepository _repo;
    private readonly RunQueue _queue;

    public RunsController(WorkflowRepository repo, RunQueue queue)
    {
        _repo = repo;
        _queue = queue;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowRun>>> List([FromQuery] int take = 50)
        => Ok(await _repo.ListRunsAsync(take));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RunDetailResponse>> Get(Guid id)
    {
        var run = await _repo.GetRunAsync(id);
        if (run is null) return NotFound();

        var steps = await _repo.GetStepsAsync(id);
        return Ok(new RunDetailResponse(run, steps));
    }

    [HttpPost("{id:guid}/steps/{stepId}/approve")]
    public async Task<ActionResult<ApprovalResponse>> Approve(
        Guid id, string stepId, [FromBody] ApprovalRequest body)
    {
        var step = await _repo.GetStepAsync(id, stepId);
        if (step is null) return NotFound();
        if (step.Status != "waiting_approval")
            return BadRequest($"step is '{step.Status}', not waiting_approval");

        if (body.Approved)
        {
            await _repo.UpdateStepAsync(step.Id, status: "succeeded",
                output: new() { ["approved"] = true, ["comment"] = body.Comment ?? "" },
                logs: $"Approved: {body.Comment}", finish: true);
            await _queue.EnqueueAsync(id);
            return Ok(new ApprovalResponse(true));
        }
        else
        {
            await _repo.UpdateStepAsync(step.Id, status: "failed",
                error: body.Comment ?? "Rejected",
                logs: $"Rejected: {body.Comment}", finish: true);
            await _queue.EnqueueAsync(id);
            return Ok(new ApprovalResponse(false));
        }
    }
}