namespace Apia;

/// <summary>An entity a branch read and another branch has written since, named by its type and its id.</summary>
public sealed record Changed(Type EntityType, Guid Id);
