using System.Linq.Expressions;
using Apia.Scope;
using Apia.Tests.Records;
using OneOf;

namespace Apia.Tests.Scoping;

/// <summary>The author rule again, stated as a condition a backend can push into its own query language.</summary>
public sealed class ConditionedAuthorScope : IScope<Post, Guid>
{
    public bool Includes(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanWrite(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanDelete(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public OneOf<Expression<Func<Post, bool>>, None> Condition(Guid authorId)
        => OneOf<Expression<Func<Post, bool>>, None>.FromT0(post => post.AuthorId == authorId);
}
