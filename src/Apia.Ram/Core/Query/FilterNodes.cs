namespace Apia.Query;

public enum Connector { None, And, Or }

public enum FilterOp { Equals, IsAfter, Contains }

public abstract record FilterNode(Connector Connector);

public sealed record ConditionNode(
    Connector Connector,
    string    Field,
    FilterOp  Op,
    object?   Value,
    bool      IgnoreCase = false) : FilterNode(Connector);

public sealed record GroupNode(
    Connector              Connector,
    IReadOnlyList<FilterNode> Nodes) : FilterNode(Connector);

public sealed record OrderNode(string Field, bool Descending);
