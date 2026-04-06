namespace Apia.Ram.Core;

public sealed record Ambiguous<TResult>(IReadOnlyList<TResult> Candidates);