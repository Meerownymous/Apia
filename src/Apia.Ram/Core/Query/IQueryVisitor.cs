namespace Apia;

public interface IQueryVisitor<T>
{
    void OnCondition(ConditionNode node);
    void OnGroup(GroupNode node);
    void OnOrder(OrderNode order);
    void OnPage(int skip, int take);
}
