using Apia.Tests.Records;

namespace Apia.Tests.Identity;

public sealed class CommentId : IIdentity<Comment>
{
    public Guid Of(Comment entity) => entity.CommentId;
}
