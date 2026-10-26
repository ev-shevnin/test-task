using TestTask.Core.Database.Entities;
using TestTask.Core.Enums;

namespace TestTask.Core.Models;

public sealed record TaskDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority);

public static class TaskEntityExtensions
{
    public static TaskDto ToDto(this TaskEntity t) =>
        new(t.Id, t.Title, t.IsCompleted, t.CreatedAt, t.CompletedAt, t.Priority);
}
