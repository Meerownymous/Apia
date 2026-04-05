using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

public sealed class FileMemory : IMemory
{
    private readonly DirectoryInfo directory;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal FileMemory(
        string directory,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.directory = new DirectoryInfo(directory);
        this.entities  = entities;
        this.vaults    = vaults;
        this.sources   = sources;
    }

    internal DirectoryInfo Directory => directory;

    public IEntities<TResult> Entities<TResult>()
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry))
            throw new InvalidOperationException($"No IEntities<{typeof(TResult).Name}> registered.");
        return (IEntities<TResult>)entry;
    }

    public IVault<TResult> Vault<TResult>()
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No IVault<{typeof(TResult).Name}> registered.");
        return (IVault<TResult>)vault;
    }

    public IViews<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, DirectoryInfo>)source).Build(directory);
    }

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
        => throw new NotImplementedException();

    public ITransaction Begin() => new FileTransaction(this);

    internal FileEntities<TResult> GetFileEntities<TResult>()
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not FileEntities<TResult> c)
            throw new InvalidOperationException($"No FileEntities<{typeof(TResult).Name}> registered.");
        return c;
    }

    internal FileVault<TResult> GetFileVault<TResult>()
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault) || vault is not FileVault<TResult> v)
            throw new InvalidOperationException($"No FileVault<{typeof(TResult).Name}> registered.");
        return v;
    }

    internal ISynopsis<TResult, TQuery, DirectoryInfo> GetSource<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return (ISynopsis<TResult, TQuery, DirectoryInfo>)source;
    }
}
