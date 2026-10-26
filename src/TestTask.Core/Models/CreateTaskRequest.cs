using TestTask.Core.Enums;

namespace TestTask.Core.Models;

public sealed record CreateTaskRequest(
    string Title,
    Priority? Priority);
