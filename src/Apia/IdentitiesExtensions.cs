namespace Apia;

/// <summary>Fluent composition of <see cref="IIdentities"/> out of <see cref="IdentitiesWith{TEntity}"/>.</summary>
public static class IdentitiesExtensions
{
    /// <summary>These identities, extended by the given one.</summary>
    public static IIdentities With<T>(this IIdentities identities, IIdentity<T> identity) where T : notnull
        => new IdentitiesWith<T>(identities, identity);
}
