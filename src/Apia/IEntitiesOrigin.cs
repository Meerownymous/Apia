namespace Apia;

/// <summary>A factory that produces a context-bound IEntities&lt;TRecord&gt; for a given backend.</summary>
public interface IEntitiesOrigin<TRecord, in TContext> where TRecord : notnull
{
    /// <summary>Build an entities store bound to the given context.</summary>
    IEntities<TRecord> Bind(TContext context);
}
