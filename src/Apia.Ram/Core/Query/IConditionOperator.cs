namespace Apia;

public interface IConditionOperator<T>
{
    Query<T> Apply(IConditionBuilder<T> builder);
}
