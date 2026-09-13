namespace Apia;

/// <summary>
/// The outcome of a commit whose branch read an entity that changed underneath it, naming every read
/// that went stale, of whatever entity type the branch read.
/// </summary>
public sealed record Stale(IReadOnlyCollection<Changed> Changes);
