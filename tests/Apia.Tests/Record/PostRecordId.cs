using Apia;

namespace Apia.Tests.Record;

public sealed class PostRecordId : IIdentity<PostRecord>
{
    public Guid Of(PostRecord entity) => entity.PostId;
}
