namespace Apia;

/// <summary>A link from IMemory to a typed view stream, scoped to a specific result type.</summary>
public interface IViewStreamLink<TView>
{
    /// <summary>The view stream for the given query, resolved from IMemory.</summary>
    IViewStream<TView, TQuery> From<TQuery>(TQuery query) where TQuery : Query<TView>;
}

/// <summary>A view stream link backed by an IMemory instance.</summary>
public sealed class AsViewStreamLink<TView>(IMemory memory) : IViewStreamLink<TView>
{
    /// <inheritdoc/>
    public IViewStream<TView, TQuery> From<TQuery>(TQuery query) where TQuery : Query<TView>
        => memory.ViewStream<TView, TQuery>();
}
