namespace Apia.Ram.Query;

internal interface IRamCondition<T>
{
    bool Matches(T item);
}
