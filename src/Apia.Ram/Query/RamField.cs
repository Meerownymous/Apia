using System.Reflection;

namespace Apia.Ram.Query;

public sealed class RamField<T>(string name)
{
    private readonly PropertyInfo property =
        typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"Property '{name}' not found on {typeof(T).Name}.");

    internal object? Read(T item) => property.GetValue(item);
}
