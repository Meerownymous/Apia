namespace Apia.File;

internal sealed record Versioned<T>(T Record, uint Version);
