namespace Apia.Ram;

public sealed record Versioned<T>(T Record, uint Version);
