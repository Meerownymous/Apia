namespace Apia;

/// <summary>A readable field of a typed item.</summary>
public interface IField<in T>
{
    /// <summary>The value of this field in the given item.</summary>
    object? Value(T item);
}
