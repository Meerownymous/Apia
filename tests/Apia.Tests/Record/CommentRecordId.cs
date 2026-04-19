using Apia;

namespace Apia.Tests.Record;

public sealed class CommentRecordId : IIdentity<CommentRecord>
{
    public string Of(CommentRecord entity) => entity.CommentId.ToString();
}
