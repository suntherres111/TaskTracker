using System.ComponentModel.DataAnnotations;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Api.Contracts.Tasks;

public class CreateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime? DueDateUtc { get; set; }

    /// <summary>Optional; defaults to Todo. Accepts "Todo", "InProgress" or "Done".</summary>
    public TaskItemStatus? Status { get; set; }
}
