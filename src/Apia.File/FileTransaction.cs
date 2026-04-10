using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>A buffered file transaction. Saves and deletes are deferred; Commit() flushes them to disk.</summary>
public sealed class FileTransaction : ITransaction
{
    private readonly List<Func<Task>> operations = new();
    private volatile bool committed;
    private readonly BufferingMemory bufferingMemory;

    public FileTransaction(
        DirectoryInfo directory,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        bufferingMemory = new BufferingMemory(directory, entities, vaults, sources, operations);
    }

    public IMemory Memory() => bufferingMemory;

    public async Task Commit()
    {
        foreach (var op in operations)
            await op();
        committed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (!committed)
            operations.Clear();
        return ValueTask.CompletedTask;
    }
}
