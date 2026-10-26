using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace TestTask.Tests.IntegrationTests;

public class CompleteTask : IAsyncLifetime
{
    private const string RabbitUser = "testuser";
    private const string RabbitPassword = "testpass";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPassword)
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());
        
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RABBITMQ_HOST", _rabbit.Hostname);
        Environment.SetEnvironmentVariable("RABBITMQ_PORT", _rabbit.GetMappedPublicPort(5672).ToString());
        Environment.SetEnvironmentVariable("RABBITMQ_USER", RabbitUser);
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", RabbitPassword);

        _factory = new WebApplicationFactory<Program>();
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CompleteTask_ShouldPublishMessageToRabbitMq()
    {
        // Arrange
        await using var connection = await new ConnectionFactory
        {
            HostName = _rabbit.Hostname,
            Port = _rabbit.GetMappedPublicPort(5672),
            UserName = RabbitUser,
            Password = RabbitPassword
        }.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "task.events",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var queue = await channel.QueueDeclareAsync();
        await channel.QueueBindAsync(queue.QueueName, "task.events", "task.completed");

        var createResponse = await _client.PostAsJsonAsync("/tasks", new { title = "Тестовая задача" });
        
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        
        var taskId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetString();

        // Act
        var completeResponse = await _client.PutAsync($"/tasks/{taskId}/complete", content: null);

        // Assert
        completeResponse.IsSuccessStatusCode.Should().BeTrue();
        
        var message = await channel.BasicGetAsync(queue.QueueName, autoAck: true);
        message.Should().NotBeNull();

        var payload = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(message!.Body.Span));
        payload.GetProperty("taskId").GetString().Should().Be(taskId);
        payload.GetProperty("title").GetString().Should().Be("Тестовая задача");
        payload.GetProperty("priority").GetString().Should().Be("Medium");
    }
    
    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask());

        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("RABBITMQ_HOST", null);
        Environment.SetEnvironmentVariable("RABBITMQ_PORT", null);
        Environment.SetEnvironmentVariable("RABBITMQ_USER", null);
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", null);
    }
}
