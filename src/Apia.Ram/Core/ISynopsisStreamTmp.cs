namespace Apia.Ram.Core;

public interface ISynopsisStreamTmp<TResult, TQueryTarget, in TContext>
{
    IViewStreamTmp<TResult, TQueryTarget> Build(TContext context);
}
