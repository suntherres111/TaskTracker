using System.ComponentModel.DataAnnotations;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Api.Contracts.Tasks;

public class UpdateTaskStatusRequest
{
    // Nullable so a missing value fails [Required] instead of silently becoming Todo (0).
    [Required]
    public TaskItemStatus? Status { get; set; }
}
