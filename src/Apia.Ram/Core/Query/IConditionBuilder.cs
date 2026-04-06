namespace Apia;

public interface IConditionBuilder<T>
{
    Query<T> Complete(Func<Connector, string, ConditionNode> nodeFactory);
}
