namespace Apia.Ram.Query;

public sealed class RamConditionFor<T> : IRamCondition<T>
{
    private readonly IRamCondition<T> inner;

    public RamConditionFor(ConditionNode node)
    {
        IRamCondition<T> raw = node switch
        {
            EqualsNode   eq => new RamEquals<T>(eq),
            IsAfterNode  a  => new RamIsAfter<T>(a),
            ContainsNode cn => new RamContains<T>(cn),
            _               => throw new NotSupportedException(
                                   $"Node type '{node.GetType().Name}' is not handled by the RAM backend.")
        };
        inner = node.Negated ? new RamNegated<T>(raw) : raw;
    }

    public bool Matches(T item) => inner.Matches(item);
}
