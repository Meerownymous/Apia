namespace Apia.File;

/// <summary>A record paired with its optimistic-concurrency version number.</summary>
public sealed record Versioned<T>(T Record, uint Version);
