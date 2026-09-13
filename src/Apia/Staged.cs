namespace Apia;

/// <summary>
/// The changes staged on a branch for entities of one type, and what it read by id. A later entry in
/// <c>Saved</c> replaces an earlier one carrying the same id; staging a removal drops any entry already
/// saved under that id, and saving after a removal outweighs it, so the later call wins either way
/// round.
/// <para>
/// <c>Read</c> names the ids the store answered with an entity and the version it answered at;
/// <c>Absent</c> names the ids it answered nothing for. Absence is its own memory rather than a version
/// no entity could carry, so that an id read as absent is remembered as read at all and a commit can
/// tell an arrival underneath the branch from an entity that never moved.
/// </para>
/// </summary>
public sealed record Staged<T>(
    List<T> Saved,
    HashSet<Guid> Removed,
    Dictionary<Guid, Guid> Read,
    HashSet<Guid> Absent) where T : notnull;
