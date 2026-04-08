namespace Apia.File;

/// <summary>
/// Abstract synopsis stream for Postgres-backed views.
/// Subclasses pass a query function to the base constructor.
/// IMemory and IDocumentSession are provided at query time via Grow().
/// </summary>
public abstract class FileViewStreamOrigin<TResult, TSeed>(
    Func<IMemory, DirectoryInfo, TSeed, IAsyncEnumerable<TResult>> query)
    : IViewStreamOrigin<TResult, TSeed, (IMemory Memory, DirectoryInfo Directory)>
    where TSeed : notnull
{
    public IViewStream<TResult, TSeed> Grow((IMemory Memory, DirectoryInfo Directory) context)
        => new BoundViewStream(context, query);

    /// <summary>A view stream bound to a specific IMemory and IDocumentSession.</summary>
    private sealed class BoundViewStream(
        (IMemory Memory, DirectoryInfo directory) context,
        Func<IMemory, DirectoryInfo, TSeed, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TSeed>
    {
        public IAsyncEnumerable<TResult> Assemble(TSeed seed)
            => query(context.Memory, context.directory, seed);
    }
}