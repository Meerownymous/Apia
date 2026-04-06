namespace Apia.Ram.Core;

public interface IMemoryTmp
{
    IEntitiesTmp<TEntity> Entities<TEntity>() where TEntity : notnull;
    IVault<TContent> Vault<TContent>()    where TContent : notnull;
    IViewStreamTmp<TView, TQueryTarget> Views<TView, TQueryTarget>()    where TView : notnull;
    IViewTmp<TView> View<TView>()     where TView : notnull;
    ITransaction Begin();
}
