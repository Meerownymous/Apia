namespace Apia;

/// <summary>
/// Thrown when Save() detects that the record was modified by another process
/// since the last Load(). Retry the Load-Modify-Save cycle.
/// </summary>
public sealed class ConcurrentModificationException(Type type, Guid id)
    : Exception($"{type.Name} with id {id} was modified by another process.");
