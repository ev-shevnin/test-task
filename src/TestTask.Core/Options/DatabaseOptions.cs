using TestTask.Core.EnvironmentParser;

namespace TestTask.Core.Options;

public sealed class DatabaseOptions
{
    [EnvironmentName("DB_CONNECTION_STRING")]
    public string ConnectionString { get; set; } = string.Empty;
}
