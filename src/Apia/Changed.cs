namespace Apia;

/// <summary>
/// An entity a branch read and another branch has written since, named by its type and its id. A
/// record rather than a record struct for the reason <see cref="Stale"/> gives: a default value would
/// name no type.
/// </summary>
public sealed record Changed(Type EntityType, Guid Id);
