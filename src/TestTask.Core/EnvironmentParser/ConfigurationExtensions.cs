using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TestTask.Core.EnvironmentParser;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Постконфигурация настроек значениями окружающей среды
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static IServiceCollection PostConfigureWithEnvironment<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        services.PostConfigureAll<TOptions>(PostConfigureWithEnvVariables);
        return services;
    }

    public static object? GetEnvironmentVariable(string variable, Type type, Type? parserType = null)
    {
        var envValue = Environment.GetEnvironmentVariable(variable)?.Trim('"');

        if (string.IsNullOrEmpty(envValue))
            return null;

        if (parserType is not null)
        {
            var parser = (IEnvironmentParser)Activator.CreateInstance(parserType)!;
            return parser.Parse(envValue);
        }

        return
            type == typeof(bool) ? BoolParse(envValue) :
            type == typeof(Uri) ? new Uri(envValue) :
            type == typeof(TimeSpan) ? TimeSpan.Parse(envValue) :
            type.IsEnum ? Enum.Parse(type, envValue, true) :
            Convert.ChangeType(envValue, type);

        bool BoolParse(string value) => value.ToLower() switch
        {
            "true" => true,
            "yes" => true,
            "1" => true,
            "false" => false,
            "no" => false,
            "0" => false,
            _ => throw new InvalidCastException($"Can't cast {value} as boolean")
        };
    }

    public static T? GetEnvironmentVariable<T>(string variable)
    {
        return (T?)GetEnvironmentVariable(variable, typeof(T));
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal static void PostConfigureWithEnvVariables<TOptions>(TOptions options)
        where TOptions : class
    {
        var properties = typeof(TOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(p => Attribute.IsDefined(p, typeof(EnvironmentNameAttribute)))
            .ToList();

        foreach (var property in properties)
        {
            foreach (var variable in property.GetCustomAttributes<EnvironmentNameAttribute>())
            {
                var value = GetEnvironmentVariable(variable.Name, property.PropertyType, variable.ParserType);

                if (value is null)
                    continue;

                property.SetValue(options, value);
            }
        }
    }
}
