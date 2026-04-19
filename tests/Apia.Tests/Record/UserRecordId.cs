using Apia;

namespace Apia.Tests.Record;

public sealed class UserRecordId : IIdentity<UserRecord>
{
    public string Of(UserRecord entity) => entity.UserId.ToString();
}
