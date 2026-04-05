using Apia;

namespace Apia.Tests.Record;

public sealed record UserFeedQuery(Guid UserId, int Limit) : Query<UserFeedProjection>;
