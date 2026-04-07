namespace Apia;

public enum Connector { None, And, Or }

/// <summary>A node in a typed filter tree for queries over T.</summary>
public abstract record FilterNode<T>(Connector Connector);

/// <summary>A single condition over a field of T, identified by a typed selector.</summary>
public abstract record ConditionNode<T>(
    Connector        Connector,
    Func<T, object?> Selector,
    bool             Negated = false) : FilterNode<T>(Connector);

/// <summary>A grouped set of filter nodes combined as a logical unit.</summary>
public sealed record GroupNode<T>(
    Connector                    Connector,
    IReadOnlyList<FilterNode<T>> Nodes) : FilterNode<T>(Connector);

/// <summary>A sort direction applied to a single field of T.</summary>
public sealed record OrderNode<T>(Func<T, object?> Selector, bool Descending);
