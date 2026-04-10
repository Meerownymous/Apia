using OneOf;

namespace Apia;

/// <summary>
/// An entity store backed by IMemory alone, executable on any backend without registration.
/// All operations delegate to the IEntities&lt;TRecord&gt; registered in IMemory.
/// </summary>
public abstract class ShallowEntities<TRecord> : IEntities<TRecord>
    where TRecord : notnull
{
    private readonly IMemory memory;

    /// <summary>Initialises the entity store with the IMemory it will delegate to.</summary>
    protected ShallowEntities(IMemory memory) => this.memory = memory;

    /// <inheritdoc/>
    public abstract Guid IdOf(TRecord record);

    /// <inheritdoc/>
    public Task<OneOf<TRecord, NotFound>> Load(Guid id)
        => memory.Entities<TRecord>().Load(id);

    /// <inheritdoc/>
    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
        => memory.Entities<TRecord>().Save(record);

    /// <inheritdoc/>
    public Task Delete(Guid id)
        => memory.Entities<TRecord>().Delete(id);

    /// <inheritdoc/>
    public IAsyncEnumerable<TRecord> All()
        => memory.Entities<TRecord>().All();
}
