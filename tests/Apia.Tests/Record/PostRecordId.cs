using Apia;

namespace Apia.Tests.Record;

public sealed class PostRecordId : IIdentity<PostRecord>
{
    public string Of(PostRecord entity) => entity.PostId.ToString();
}
