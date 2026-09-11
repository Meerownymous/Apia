using Apia.Tests.Records;

namespace Apia.Tests.Identity;

public sealed class PostId : IIdentity<Post>
{
    public Guid Of(Post entity) => entity.PostId;
}
