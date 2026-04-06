using Apia.Query;

namespace Apia;

public interface IConditionBuilder<T>
{
    Query<T> Build(FilterOp op, object? value, bool ignoreCase);
}
