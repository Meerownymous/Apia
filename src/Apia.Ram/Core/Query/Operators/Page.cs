namespace Apia;

public static partial class QueryExtensions
{
    public static Query<T> Page<T>(this Query<T> query, int number, int size)
        => query.WithSkip((number - 1) * size).WithTake(size);
}
