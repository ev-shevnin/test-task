using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TestTask.Core.Database;
using TestTask.Core.Database.Entities;
using TestTask.Core.Enums;
using TestTask.Core.Messaging;
using TestTask.Core.Models;

namespace TestTask.Core.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks", CreateTask);
        app.MapGet("/tasks", GetTasks);
        app.MapPut("/tasks/{id:guid}/complete", CompleteTask);
        app.MapDelete("/tasks/{id:guid}", DeleteTask);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request,
        TaskDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            return Results.BadRequest("Title не может быть пустым и должен содержать не более 200 символов");

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Priority = request.Priority ?? Priority.Medium,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/tasks/{task.Id}", task.ToDto());
    }

    private static async Task<IResult> GetTasks(
        TaskDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tasks = await dbContext.Tasks
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TaskDto(t.Id, t.Title, t.IsCompleted, t.CreatedAt, t.CompletedAt, t.Priority))
            .ToListAsync(cancellationToken);

        return Results.Ok(tasks);
    }

    private static async Task<IResult> CompleteTask(
        Guid id,
        TaskDbContext dbContext,
        IRabbitMqService rabbitMqService,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FindAsync([id], cancellationToken);

        if (task is null)
            return Results.NotFound();

        if (task.IsCompleted)
            return Results.Conflict("Задача уже завершена");

        task.IsCompleted = true;
        task.CompletedAt = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict("Задача уже была завершена параллельным запросом");
        }

        // HACK: публикация не атомарна с сохранением в БД.
        // Если приложение упадёт между SaveChanges и публикацией, то событие потеряется.
        // В проде нужен Outbox с идемпотентным ключом для обработки дублей.
        await rabbitMqService.PublishTaskCompletedAsync(task);

        return Results.Ok(task.ToDto());
    }

    private static async Task<IResult> DeleteTask(
        Guid id,
        TaskDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FindAsync([id], cancellationToken);

        if (task is null)
            return Results.NotFound();

        // HACK: удаляем без проверки статуса. Если задача завершена и событие
        // ещё обрабатывается консюмером, он получит taskId которого уже нет в БД.
        // В проде стоит обратить внимание.
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
