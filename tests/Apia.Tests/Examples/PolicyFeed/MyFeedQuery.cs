using Apia.Scope;
using Apia.Tests.Record;

namespace Apia.Tests.Examples.PolicyFeed;

public record MyFeedQuery(int Limit) : ScopedQuery<MyFeedQuery, PostRecord, UserContext>
{
    public UserContext Context { get; init; }
    public override MyFeedQuery WithContext(UserContext ctx) => this with { Context = ctx };
}
