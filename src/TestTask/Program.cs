using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using TestTask.Core;
using TestTask.Core.Endpoints;

namespace TestTask;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDatabase();
        builder.Services.AddRabbitMq();

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.Configure<HostOptions>(options =>
            options.ShutdownTimeout = TimeSpan.FromSeconds(5));

        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.ApplyMigrations();

        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapTaskEndpoints();

        app.Run();
    }
}
