using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // Stored as text ("Todo", "InProgress", "Done") so the table is readable
        // and reordering enum values can't silently corrupt data.
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        // Supports the common "tasks in project X with status Y" query.
        builder.HasIndex(t => new { t.ProjectId, t.Status });

        builder.HasData(TaskTrackerSeedData.Tasks);
    }
}
