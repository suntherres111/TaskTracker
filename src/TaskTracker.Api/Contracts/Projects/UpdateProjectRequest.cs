using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Api.Contracts.Projects;

public class UpdateProjectRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
