using Apia;

namespace Apia.Tests.Record;

public sealed record UserFeedQueryRecord(Guid UserId, int Limit) : QueryRecord<UserPostSummaryProjection>;
