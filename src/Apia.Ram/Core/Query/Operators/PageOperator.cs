namespace Apia;

public static partial class FilterExtensions
{
    public static Query<T> Page<T>(this Query<T> query, int number, int size)
    {
        query.Skip = (number - 1) * size;
        query.Take = size;
        return query;
    }
}
