using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

public sealed class RamMemory : IMemory
{
    private readonly ConcurrentDictionary<Type, object> catalogs;
    private readonly ConcurrentDictionary<Type, object> mutables;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal RamMemory(
        ConcurrentDictionary<Type, object> catalogs,
        ConcurrentDictionary<Type, object> mutables,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.catalogs = catalogs;
        this.mutables = mutables;
        this.sources  = sources;
    }

    public IMutableCatalog<TResult> Catalog<TResult>()
    {
        if (!catalogs.TryGetValue(typeof(TResult), out var catalog))
            throw new InvalidOperationException($"No IMutableCatalog<{typeof(TResult).Name}> registered.");
        return (IMutableCatalog<TResult>)catalog;
    }

    public IMutable<TResult> Mutable<TResult>()
    {
        if (!mutables.TryGetValue(typeof(TResult), out var mutable))
            throw new InvalidOperationException($"No IMutable<{typeof(TResult).Name}> registered.");
        return (IMutable<TResult>)mutable;
    }

    public IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, IMemory>)source).Build(this);
    }

    public ITransaction Begin() => new RamTransaction(this);

    internal RamMutableCatalog<TResult> RawCatalog<TResult>()
    {
        if (!catalogs.TryGetValue(typeof(TResult), out var catalog) || catalog is not RamMutableCatalog<TResult> raw)
            throw new InvalidOperationException($"No RamMutableCatalog<{typeof(TResult).Name}> registered.");
        return raw;
    }

    internal RamMutable<TResult> RawMutable<TResult>()
    {
        if (!mutables.TryGetValue(typeof(TResult), out var mutable) || mutable is not RamMutable<TResult> raw)
            throw new InvalidOperationException($"No RamMutable<{typeof(TResult).Name}> registered.");
        return raw;
    }
}
