namespace Apia;

public interface ISynopsis<TResult, in TQuery, in TContext> where TQuery : QueryRecord<TResult>
{
    /// <summary>
    /// Build a views projection bound to the given context.
    /// TContext is injected by the memory at query time —
    /// Ram passes IMemory, File passes DirectoryInfo, Postgres passes (IMemory, IDocumentSession).
    /// </summary>
    IView<TResult, TQuery> Build(TContext context);
}