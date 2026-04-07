namespace Apia.Ram.Query;

/// <summary>A condition built from a query condition node, dispatching to the appropriate implementation.</summary>
public sealed class RamConditionFor<T> : ICondition<T>
{
    private readonly ICondition<T> inner;

    /// <summary>Creates the condition corresponding to the given node.</summary>
    public RamConditionFor(ConditionNode node)
    {
        ICondition<T> raw = node switch
        {
            EqualsNode   eq => new RamEquals<T>(eq),
            IsAfterNode  a  => new RamIsAfter<T>(a),
            ContainsNode cn => new RamContains<T>(cn),
            _               => throw new NotSupportedException(
                                   $"Node type '{node.GetType().Name}' is not handled by the RAM backend.")
        };
        inner = node.Negated ? new RamNegated<T>(raw) : raw;
    }

    /// <inheritdoc/>
    public bool Matches(T item) => inner.Matches(item);
}
