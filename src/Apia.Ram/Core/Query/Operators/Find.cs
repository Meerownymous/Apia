using Apia.Ram.Core;

namespace Apia;

public static partial class QueryExtensions
{
    public static IAsyncEnumerable<T> Find<T>(
        this IEntitiesTmp<T>     entities,
        Func<Query<T>, Query<T>> build)
        => entities.Find(build(new Query<T>()));
}
