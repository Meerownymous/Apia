using Apia.Tests.Record;

namespace Apia.TestAssets.Records;

public sealed record UserFeedQuery(Guid UserId, int Limit) : Query<UserPostSummaryView>;
