namespace Apia;

public interface ISynopsisTmp<TResult, in TContext>
{
    IViewTmp<TResult> Build(TContext context);
}
