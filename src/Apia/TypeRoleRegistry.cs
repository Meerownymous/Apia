namespace Apia;

/// <summary>Tracks the single role (Vault / Aggregate / Projection) each type may occupy.</summary>
public sealed class TypeRoleRegistry
{
    private readonly Dictionary<Type, string> roles = new();

    public void ClaimVault<T>()      => Claim<T>("vault");
    public void ClaimAggregate<T>()  => Claim<T>("aggregate");
    public void ClaimProjection<T>() => Claim<T>("projection");

    private void Claim<T>(string role)
    {
        var type = typeof(T);
        if (roles.TryGetValue(type, out var existing) && existing != role)
            throw new InvalidOperationException(
                $"{type.Name} is already registered as {existing} and cannot also be registered as {role}.");
        roles[type] = role;
    }
}
