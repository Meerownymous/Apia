namespace Apia.DynamoDB;

/// <summary>
/// Maps an entity to a composite DynamoDB key: PK and SK joined by a unit-separator character.
/// Pass an instance of this to DynamoMemoryMap.RegisterStore, or use the pk/sk lambda overload directly.
/// </summary>
public sealed class DynamoIdentity<T>(Func<T, string> pk, Func<T, string> sk) : IIdentity<T>
{
    internal const char Separator = '\x1F';

    public string Of(T entity) => $"{pk(entity)}{Separator}{sk(entity)}";

    internal string Pk(T entity) => pk(entity);
    internal string Sk(T entity) => sk(entity);
}
