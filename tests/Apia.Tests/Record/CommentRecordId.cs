using Apia;

namespace Apia.Tests.Record;

public sealed class CommentRecordId : IIdentity<CommentRecord>
{
    public Guid Of(CommentRecord entity) => entity.CommentId;
}
