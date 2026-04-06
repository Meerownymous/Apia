namespace Apia;

public interface IMemoryTmp
{
    IEntitiesTmp<TResult>   Entities<TResult>() where TResult : notnull;
    IVault<TResult>         Vault<TResult>()    where TResult : notnull;
    IViewStreamTmp<TResult> Views<TResult>()    where TResult : notnull;
    IViewTmp<TResult>       View<TResult>()     where TResult : notnull;
    ITransaction            Begin();
}
