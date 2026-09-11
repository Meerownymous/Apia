using OneOf;

namespace Apia;

/// <summary>
/// The identities of the stored entity types. Backend-neutral: the same collection composes a memory
/// on every backend.
/// </summary>
public interface IIdentities
{
    /// <summary>The identity given for entities of type T, or <see cref="None"/> when none was given.</summary>
    OneOf<IIdentity<T>, None> Identity<T>() where T : notnull;
}
