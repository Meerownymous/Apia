using System.Linq.Expressions;
using Apia.Scope;
using OneOf;

namespace Apia.Tests.Record;

public sealed record AuthorScope : IScope<PostRecord, Guid>
{
    public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;
}

public sealed record AuthorLinqScope : IScope<PostRecord, Guid>
{
    public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;

    public OneOf<Expression<Func<PostRecord, bool>>, None> AsLinq(Guid authorId)
        => (Expression<Func<PostRecord, bool>>)(p => p.AuthorId == authorId);
}
