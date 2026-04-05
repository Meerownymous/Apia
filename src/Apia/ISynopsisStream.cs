namespace Apia;

public interface ISynopsisStream<TResult, TQuery, in TContext> where TQuery : Query<TResult>
{
    /// <summary>
    /// Build a views projection bound to the given context.
    /// TContext is injected by the memory at query time —
    /// Ram passes IMemory, File passes DirectoryInfo, Postgres passes (IMemory, IDocumentSession).
    /// </summary>
    IViewStream<TResult, TQuery> Build(TContext context);
}
