using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Contracts.Tasks;
using TaskTracker.Api.Services;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Controllers;

// Tasks are created/listed under their project, and addressed directly by id otherwise.
[ApiController]
[Route("api")]
[Produces("application/json")]
// Mutations require a valid JWT (401 otherwise) and a permitted role (403 otherwise):
// Admin or User may create/update tasks; only Admin may delete. Reads opt out with [AllowAnonymous].
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks)
    {
        _tasks = tasks;
    }

    [HttpGet("projects/{projectId:int}/tasks")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetForProject(int projectId, CancellationToken cancellationToken)
    {
        var tasks = await _tasks.GetForProjectAsync(projectId, cancellationToken);
        return tasks is null ? ProjectNotFound(projectId) : Ok(tasks);
    }

    [HttpGet("tasks/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("projects/{projectId:int}/tasks")]
    [Authorize(Policy = AuthorizationPolicies.TaskEditor)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _tasks.CreateAsync(projectId, request, cancellationToken);
        return task is null
            ? ProjectNotFound(projectId)
            : CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("tasks/{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.TaskEditor)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _tasks.UpdateAsync(id, request, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPatch("tasks/{id:int}/status")]
    [Authorize(Policy = AuthorizationPolicies.TaskEditor)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        // [Required] guarantees a value here; invalid enum names are rejected as 400 during JSON binding.
        var task = await _tasks.UpdateStatusAsync(id, request.Status!.Value, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("tasks/{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return await _tasks.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private ObjectResult ProjectNotFound(int projectId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Project not found",
            detail: $"Project {projectId} does not exist.");
}
