using OneOf;

namespace Apia;

/// <summary>Identities extended by the identity of one further entity type.</summary>
public sealed class IdentitiesWith<TEntity>(IIdentities inner, IIdentity<TEntity> identity)
    : IIdentities where TEntity : notnull
{
    public OneOf<IIdentity<T>, None> Identity<T>() where T : notnull
        => identity is IIdentity<T> given
            ? OneOf<IIdentity<T>, None>.FromT0(given)
            : inner.Identity<T>();
}
