using Apia;
using OneOf;

namespace Apia.File;

public sealed class FileTransaction : ITransaction
{
    private readonly List<Func<Task>> operations = new();
    private volatile bool committed;
    private readonly BufferingMemory bufferingMemory;

    internal FileTransaction(FileMemory source)
    {
        bufferingMemory = new BufferingMemory(source, operations);
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



