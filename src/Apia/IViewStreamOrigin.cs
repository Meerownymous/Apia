namespace Apia;

public interface IViewStreamOrigin<out TResult, in TSeed, in TContext> where TSeed : notnull
{
    /// <summary>
    /// Build a views projection bound to the given context.
    /// TContext is injected by the memory at query time —
    /// Ram passes IMemory, File passes DirectoryInfo, Postgres passes (IMemory, IDocumentSession).
    /// </summary>
    IViewStream<TResult, TSeed> Grow(TContext context);
}
