using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>
/// Compose a file-backed IMemory. One JSON file per record type in the given directory.
/// Synopsis sources receive DirectoryInfo as context.
/// </summary>
public sealed class FileMemoryMap(string directory) : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> catalogs = new();
    private readonly ConcurrentDictionary<Type, object> mutables = new();
    private readonly ConcurrentDictionary<(Type, Type), object> sources = new();

    /// <inheritdoc/>
    public void Register<TResult>(IMutableCatalog<TResult> catalog)
        => catalogs[typeof(TResult)] = catalog;

    /// <inheritdoc/>
    public void Register<TResult>(IMutable<TResult> mutable)
        => mutables[typeof(TResult)] = mutable;

    /// <summary>Register a synopsis source. TContext is DirectoryInfo for File.</summary>
    public void Register<TResult, TQuery>(ISynopsis<TResult, TQuery, DirectoryInfo> source)
        where TQuery : Query<TResult>
        => sources[(typeof(TResult), typeof(TQuery))] = source;

    /// <inheritdoc/>
    public IMemory Build() => new FileMemory(directory, catalogs, mutables, sources);
}
