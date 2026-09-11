namespace Apia;

/// <summary>The storage abstraction an application is written against. One container per application.</summary>
public interface IMemory
{
    /// <summary>The results of a multi-result query over entities of type T.</summary>
    IAsyncEnumerable<T> Aggregate<T>(object query) where T : notnull;

    /// <summary>The single computed result of a query.</summary>
    Task<T> Projection<T>(object query) where T : notnull;

    /// <summary>Read access to the entities of type T.</summary>
    IVault<T> Vault<T>() where T : notnull;

    /// <summary>A new unit of work over this memory.</summary>
    IBranch Branch();
}
