using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestTask.Core.Database;
using TestTask.Core.EnvironmentParser;
using TestTask.Core.Messaging;
using TestTask.Core.Options;

namespace TestTask.Core;

public static class ServiceExtensions
{
    public static void AddDatabase(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>();
        services.PostConfigureWithEnvironment<DatabaseOptions>();

        services.AddDbContext<TaskDbContext>((sp, opt) =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            opt.UseNpgsql(options.ConnectionString).UseSnakeCaseNamingConvention();
        });
    }

    public static void AddRabbitMq(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>();
        services.PostConfigureWithEnvironment<RabbitMqOptions>();

        services.AddSingleton<IRabbitMqService, RabbitMqService>();
        services.AddHostedService(sp => (RabbitMqService)sp.GetRequiredService<IRabbitMqService>());
    }
    
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<TaskDbContext>();
        if (db.Database.GetPendingMigrations().Any())
            db.Database.Migrate();
    }
}
