namespace Apia;

public record Conflict<T>(T Current, T Attempted);
