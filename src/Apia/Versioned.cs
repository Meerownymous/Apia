namespace Apia;

/// <summary>A stored entity together with the version it is stored at.</summary>
public readonly record struct Versioned<T>(T Entity, Guid Version) where T : notnull;
