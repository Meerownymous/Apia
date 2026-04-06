namespace Apia.Ram.Query;

public interface IRamCondition<in T>
{
    bool Matches(T item);
}
