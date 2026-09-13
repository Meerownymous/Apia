namespace Apia;

/// <summary>
/// The outcome of a commit whose branch read an entity that changed underneath it, naming every read
/// that went stale, of whatever entity type the branch read.
/// <para>
/// A record rather than a record struct, unlike the other outcomes: the default value of a struct
/// carrying a collection would hold no collection at all, and a stale outcome naming nothing is the
/// silence this type exists to end.
/// </para>
/// </summary>
public sealed record Stale(IReadOnlyCollection<Changed> Changes);
