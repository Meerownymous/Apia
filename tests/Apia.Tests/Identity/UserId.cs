using Apia.Tests.Records;

namespace Apia.Tests.Identity;

public sealed class UserId : IIdentity<User>
{
    public Guid Of(User entity) => entity.UserId;
}
