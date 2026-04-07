using System.Reflection;

namespace Apia.Ram.Query;

/// <summary>A field of a typed item, resolved by name via reflection.</summary>
public sealed class RamField<T>(string name) : IField<T>
{
    private readonly PropertyInfo property =
        typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"Property '{name}' not found on {typeof(T).Name}.");

    /// <inheritdoc/>
    public object? Value(T item) => property.GetValue(item);
}
