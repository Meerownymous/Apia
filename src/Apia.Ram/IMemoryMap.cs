namespace Apia;

public interface IMemoryMapTmp
{
    /// <summary>Register a catalog of records addressable by Guid.</summary>
    void Register<TResult>(IEntitiesTmp<TResult> entities);

    /// <summary>Register a single record — settings, config, state.</summary>
    void Register<TResult>(IVault<TResult> vault);

    /// <summary>Build the IMemory. Call after all registrations are done.</summary>
    IMemoryTmp Build();
}
