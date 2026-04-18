using Apia;

namespace Apia.Tests.Record;

public sealed record UserFeedQuery(Guid UserId, int Limit) : IQuery<UserFeedQuery, UserPostSummaryView>
{
    public UserFeedQuery Seed() => this;
}
