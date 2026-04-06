namespace Apia;

public interface ISynopsisStreamTmp<TResult, in TContext>
{
    IViewStreamTmp<TResult> Build(TContext context);
}
