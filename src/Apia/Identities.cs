using OneOf;

namespace Apia;

/// <summary>The identities of no entity type at all. The root a composition extends with <c>With</c>.</summary>
public sealed class Identities : IIdentities
{
    public OneOf<IIdentity<T>, None> Identity<T>() where T : notnull => new None();
}
