namespace Apia.File;

public sealed record Versioned<T>(T Record, uint Version);
