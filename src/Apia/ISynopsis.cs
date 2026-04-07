namespace Apia;

public interface ISynopsis<TView, in TSeed, in TSource> where TSeed : notnull
{
    /// <summary>
    /// Build a views projection bound to the given context.
    /// TContext is injected by the memory at query time —
    /// Ram passes IMemory, File passes DirectoryInfo, Postgres passes (IMemory, IDocumentSession).
    /// </summary>
    IView<TView, TSeed> Build(TSource source);
}