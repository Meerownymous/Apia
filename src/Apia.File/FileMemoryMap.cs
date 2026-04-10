using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>
/// Compose a file-backed IMemory. One JSON file per record type in the given directory.
/// Synopsis sources receive IMemory as context — identical to the Ram convention.
/// </summary>
public sealed class FileMemoryMap(string directory) : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> entities = new();
    private readonly ConcurrentDictionary<Type, object> vaults   = new();
    private readonly ConcurrentDictionary<(Type, Type), object> sources = new();

    /// <inheritdoc/>
    public void Register<TResult>(IEntities<TResult> e) where TResult : notnull
        => entities[typeof(TResult)] = e;

    /// <inheritdoc/>
    public void Register<TResult>(IVault<TResult> vault) where TResult : notnull
        => vaults[typeof(TResult)] = vault;

    /// <summary>Register a synopsis source. Context is IMemory — the same interface used for Ram.</summary>
    public void Register<TResult, TQuery>(IViewStreamOrigin<TResult, TQuery, IMemory> source)
        where TQuery : Query<TResult>
        => sources[(typeof(TResult), typeof(TQuery))] = source;

    /// <inheritdoc/>
    public IMemory Build() => new FileMemory(directory, entities, vaults, sources);
}
