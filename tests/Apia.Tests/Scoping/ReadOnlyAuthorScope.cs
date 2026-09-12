using System.Linq.Expressions;
using Apia.Scope;
using Apia.Tests.Entities;
using OneOf;

namespace Apia.Tests.Scoping;

/// <summary>An author who may read their own posts and change none of them.</summary>
public sealed class ReadOnlyAuthorScope : IScope<Post, Guid>
{
    public bool Includes(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanWrite(Post entity, Guid authorId) => false;

    public bool CanDelete(Post entity, Guid authorId) => false;

    public OneOf<Expression<Func<Post, bool>>, None> Condition(Guid authorId) => new None();
}
