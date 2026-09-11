namespace Apia;

/// <summary>
/// The changes staged on a branch for entities of one type, and the versions it read by id. A later
/// entry in <c>Saved</c> replaces an earlier one carrying the same id; staging a removal drops any
/// entry already saved under that id, and saving after a removal outweighs it, so the later call wins
/// either way round.
/// </summary>
public sealed record Staged<T>(
    List<T> Saved,
    HashSet<Guid> Removed,
    Dictionary<Guid, Guid> Read) where T : notnull;
