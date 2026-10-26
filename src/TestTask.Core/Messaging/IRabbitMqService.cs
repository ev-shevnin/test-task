using TestTask.Core.Database.Entities;

namespace TestTask.Core.Messaging;

public interface IRabbitMqService
{
    Task PublishTaskCompletedAsync(TaskEntity task);
}
