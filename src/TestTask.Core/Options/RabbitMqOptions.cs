using TestTask.Core.EnvironmentParser;

namespace TestTask.Core.Options;

public sealed class RabbitMqOptions
{
    [EnvironmentName("RABBITMQ_HOST")]
    public string Host { get; set; } = "localhost";

    [EnvironmentName("RABBITMQ_PORT")]
    public int Port { get; set; } = 5672;

    [EnvironmentName("RABBITMQ_USER")]
    public string User { get; set; } = "guest";

    [EnvironmentName("RABBITMQ_PASSWORD")]
    public string Password { get; set; } = "guest";
}
