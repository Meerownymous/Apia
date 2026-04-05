namespace Apia.File;

public sealed class BufferingMemory : IMemory
{
    private readonly FileMemory source;
    private readonly List<Func<Task>> operations;

    public BufferingMemory(FileMemory source, List<Func<Task>> operations)
    {
        this.source     = source;
        this.operations = operations;
    }

    public IEntities<TResult> Entities<TResult>()
        => new BufferingEntities<TResult>(source.GetFileEntities<TResult>(), operations);

    public IVault<TResult> Vault<TResult>()
        => new BufferingVault<TResult>(source.GetFileVault<TResult>(), operations);

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
        => source.GetSource<TResult, TQuery>().Build(source.Directory);

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
    //=> source.GetSource<TResult, TQuery>().Build(source.Directory);
        => throw new NotImplementedException();
        

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
