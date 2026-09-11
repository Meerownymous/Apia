namespace Apia;

/// <summary>The identity given for entities of type T, refusing a type no identity was given for.</summary>
public sealed class GivenIdentity<T>(IIdentities identities) : IIdentity<T> where T : notnull
{
    public Guid Of(T entity)
        => identities.Identity<T>().Match(
            identity => identity.Of(entity),
            _ => throw new InvalidOperationException(
                $"No identity was given for {typeof(T).Name}. Add one to the identities the memory was composed from."));
}
