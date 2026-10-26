namespace TestTask.Core.EnvironmentParser;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class EnvironmentNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    /// <summary>
    /// Must implement IEnvironmentParser interface
    /// </summary>
    public Type? ParserType { get; set; }
}
