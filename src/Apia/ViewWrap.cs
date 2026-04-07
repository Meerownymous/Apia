namespace Apia;

public interface IViewStreamLink<out TView>
{
    IViewStream<TView, TSeed> From<TSeed>(TSeed seed) where TSeed : notnull;
}

public sealed class AsViewStreamLink<TView>(IMemory memory)
{
    IViewStream<TView, TSeed> From<TSeed>(TSeed seed) where TSeed : notnull
    {
        return memory.ViewStream<TView, TSeed>();
    }
}