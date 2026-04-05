namespace Apia;

public interface ISynopsis<TResult, TQuery, TContext> where TQuery : Query<TResult>
{
    /// <summary>
    /// Build a views projection bound to the given context.
    /// TContext is injected by the memory at query time —
    /// Ram passes IMemory, File passes DirectoryInfo, Postgres passes (IMemory, IDocumentSession).
    /// </summary>
    IViews<TResult, TQuery> Build(TContext context);
}
