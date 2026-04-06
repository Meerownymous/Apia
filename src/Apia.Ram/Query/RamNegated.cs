namespace Apia.Ram.Query;

internal sealed class RamNegated<T>(IRamCondition<T> inner) : IRamCondition<T>
{
    public bool Matches(T item) => !inner.Matches(item);
}
