namespace Apia;

/// <summary>A condition that can be evaluated against an item.</summary>
public interface ICondition<in T>
{
    /// <summary>Whether the item satisfies this condition.</summary>
    bool Matches(T item);
}
