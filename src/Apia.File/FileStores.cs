using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>The on-disk stores of one memory, one file per entity type, under one directory.</summary>
public sealed class FileStores(string directory, IIdentities identities) : IStores
{
    private readonly ConcurrentDictionary<Type, object> stores = new();

    public IEntityStore<T> Store<T>() where T : notnull
        => (IEntityStore<T>)stores.GetOrAdd(
            typeof(T),
            _ => new FileEntityStore<T>(directory, new GivenIdentity<T>(identities)));
}
