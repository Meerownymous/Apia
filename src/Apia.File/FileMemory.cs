using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>File-backed IMemory that stores records as JSON files on disk, keyed by type.</summary>
public sealed class FileMemory : IMemory
{
    private readonly DirectoryInfo directory;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    public FileMemory(
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

    public IEntities<TEntity> Entities<TEntity>() where TEntity : notnull
    {
        if (!entities.TryGetValue(typeof(TEntity), out var entry))
            throw new InvalidOperationException($"No IEntities<{typeof(TEntity).Name}> registered.");
        return (IEntities<TEntity>)entry;
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No IVault<{typeof(TResult).Name}> registered.");
        return (IVault<TResult>)vault;
    }

    public OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT0(
            ((IViewStreamOrigin<TResult, TQuery, IMemory>)source).From(this));
    }

    public OneOf.OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT0(
            ((IViewOrigin<TResult, TQuery, IMemory>)source).Assemble(this));
    }

    public ITransaction Begin()
        => new FileTransaction(directory, entities, vaults, sources);
}
