using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using TestTask.Core.Database.Entities;
using TestTask.Core.Options;

namespace TestTask.Core.Messaging;

public class RabbitMqService(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqService> logger)
    : IRabbitMqService, IHostedService, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rabbit = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = rabbit.Host,
                Port = rabbit.Port,
                UserName = rabbit.User,
                Password = rabbit.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            
            await _channel.ExchangeDeclareAsync(
                exchange: "task.events",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            logger.LogInformation("RabbitMQ подключён, обменник task.events объявлен");
        }
        catch (Exception ex)
        {
            // HACK: нужен retry.
            // Все публикации будут падать silently до перезапуска приложения.
            // В проде нужен Polly (например) с retry + circuit breaker.
            logger.LogError(ex, "Не удалось подключиться к RabbitMQ при запуске");
        }
    }

    public async Task PublishTaskCompletedAsync(TaskEntity task)
    {
        try
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ канал не инициализирован");

            var payload = new
            {
                taskId = task.Id,
                title = task.Title,
                completedAt = task.CompletedAt,
                priority = task.Priority.ToString()
            };

            var body = JsonSerializer.SerializeToUtf8Bytes(payload);

            await _channel.BasicPublishAsync(
                exchange: "task.events",
                routingKey: "task.completed",
                body: body);

            logger.LogInformation(
                "Событие task.completed отправлено в RabbitMQ для задачи {TaskId}", task.Id);
        }
        catch (Exception ex)
        {
            // Fail silently: задача уже сохранена в БД, сообщение RabbitMQ теряем
            logger.LogError(ex,
                "Не удалось отправить событие в RabbitMQ для задачи {TaskId}. " +
                "Задача завершена в БД, событие потеряно", task.Id);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
                await _channel.CloseAsync(cancellationToken);

            if (_connection is not null)
                await _connection.CloseAsync(cancellationToken);
        }
        catch (AlreadyClosedException ex)
        {
            logger.LogWarning(ex, "RabbitMQ соединение уже было закрыто до вызова StopAsync");
        }

        logger.LogInformation("RabbitMQ соединение корректно закрыто");
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
