namespace Apia;

public interface IMemoryMap
{
    /// <summary>Register a catalog of records addressable by Guid.</summary>
    void Register<TResult>(IMutableCatalog<TResult> catalog);

    /// <summary>Register a single mutable record — settings, config, state.</summary>
    void Register<TResult>(IMutable<TResult> mutable);

    /// <summary>Build the IMemory. Call after all registrations are done.</summary>
    IMemory Build();
}
