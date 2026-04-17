using Apia;

namespace Apia.Tests.Record;

public sealed class UserRecordId : IIdentity<UserRecord>
{
    public Guid Of(UserRecord entity) => entity.UserId;
}
