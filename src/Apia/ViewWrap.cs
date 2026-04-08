namespace Apia;

/// <summary>A link from IMemory to a typed view stream, scoped to a specific result type.</summary>
public interface IViewStreamLink<out TView>
{
    /// <summary>The view stream for the given seed, resolved from IMemory.</summary>
    IViewStream<TView, TSeed> From<TSeed>(TSeed seed) where TSeed : notnull;
}

/// <summary>A view stream link backed by an IMemory instance.</summary>
public sealed class AsViewStreamLink<TView>(IMemory memory) : IViewStreamLink<TView>
{
    public IViewStream<TView, TSeed> From<TSeed>(TSeed seed) where TSeed : notnull
        => memory.ViewStream<TView, TSeed>();
}
