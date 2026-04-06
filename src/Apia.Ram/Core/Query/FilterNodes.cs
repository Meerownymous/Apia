namespace Apia;

public enum Connector { None, And, Or }

public abstract record FilterNode(Connector Connector);

public abstract record ConditionNode(
    Connector Connector,
    string    Field,
    bool      Negated = false) : FilterNode(Connector);

public sealed record GroupNode(
    Connector                 Connector,
    IReadOnlyList<FilterNode> Nodes) : FilterNode(Connector);

public sealed record OrderNode(string Field, bool Descending);
