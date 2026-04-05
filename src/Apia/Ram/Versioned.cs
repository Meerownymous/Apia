namespace Apia.Ram;

internal sealed record Versioned<T>(T Record, uint Version);
