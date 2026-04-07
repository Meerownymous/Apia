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

    public IEntities<TEntity> Entities<TEntity>()
    {
        if (!entities.TryGetValue(typeof(TEntity), out var entry))
            throw new InvalidOperationException($"No IEntities<{typeof(TEntity).Name}> registered.");
        return (IEntities<TEntity>)entry;
    }

    public IVault<TResult> Vault<TResult>()
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No IVault<{typeof(TResult).Name}> registered.");
        return (IVault<TResult>)vault;
    }

    public IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsisStream<TResult, TSeed, DirectoryInfo>)source).Grow(directory);
    }

    public IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsis<TResult, TSeed, DirectoryInfo>)source).Build(directory);
    }

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
    
    internal ISynopsisStream<TResult, TSeed, DirectoryInfo> GetSource<TResult, TSeed>()
        where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return (ISynopsisStream<TResult, TSeed, DirectoryInfo>)source;
    }
}
