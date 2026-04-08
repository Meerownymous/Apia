using System.Collections.Concurrent;

namespace Apia.File;

/// <summary>IMemory that buffers writes for deferred commit within a FileTransaction.</summary>
public sealed class BufferingMemory : IMemory
{
    private readonly DirectoryInfo directory;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;
    private readonly List<Func<Task>> operations;

    public BufferingMemory(
        DirectoryInfo directory,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources,
        List<Func<Task>> operations)
    {
        this.directory  = directory;
        this.entities   = entities;
        this.vaults     = vaults;
        this.sources    = sources;
        this.operations = operations;
    }

    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not FileEntities<TResult> fileEntities)
            throw new InvalidOperationException($"No FileEntities<{typeof(TResult).Name}> registered.");
        return new BufferingEntities<TResult>(fileEntities, operations);
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault) || vault is not FileVault<TResult> fileVault)
            throw new InvalidOperationException($"No FileVault<{typeof(TResult).Name}> registered.");
        return new BufferingVault<TResult>(fileVault, operations);
    }

    public IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((IViewStreamOrigin<TResult, TSeed, IMemory>)source).Grow(this);
    }

    public IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull
        => throw new NotImplementedException();

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
