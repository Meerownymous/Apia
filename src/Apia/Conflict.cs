namespace Apia;

/// <summary>Two conflicting versions of the same record: the current persisted value and the attempted update.</summary>
public record Conflict<T>(T Current, T Attempted);
