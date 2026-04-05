using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

public sealed class FileMemory : IMemory
{
    private readonly DirectoryInfo directory;
    private readonly ConcurrentDictionary<Type, object> catalogs;
    private readonly ConcurrentDictionary<Type, object> mutables;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal FileMemory(
        string directory,
        ConcurrentDictionary<Type, object> catalogs,
        ConcurrentDictionary<Type, object> mutables,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.directory = new DirectoryInfo(directory);
        this.catalogs  = catalogs;
        this.mutables  = mutables;
        this.sources   = sources;
    }

    internal DirectoryInfo Directory => directory;

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
        return ((ISynopsis<TResult, TQuery, DirectoryInfo>)source).Build(directory);
    }

    public ITransaction Begin() => new FileTransaction(this);

    internal FileMutableCatalog<TResult> GetFileMutableCatalog<TResult>()
    {
        if (!catalogs.TryGetValue(typeof(TResult), out var catalog) || catalog is not FileMutableCatalog<TResult> c)
            throw new InvalidOperationException($"No FileMutableCatalog<{typeof(TResult).Name}> registered.");
        return c;
    }

    internal FileMutable<TResult> GetFileMutable<TResult>()
    {
        if (!mutables.TryGetValue(typeof(TResult), out var mutable) || mutable is not FileMutable<TResult> m)
            throw new InvalidOperationException($"No FileMutable<{typeof(TResult).Name}> registered.");
        return m;
    }

    internal ISynopsis<TResult, TQuery, DirectoryInfo> GetSource<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return (ISynopsis<TResult, TQuery, DirectoryInfo>)source;
    }
}
