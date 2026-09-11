using System.Linq.Expressions;
using Apia.Scope;
using Apia.Tests.Records;
using OneOf;

namespace Apia.Tests.Scoping;

/// <summary>A post belongs to the author who wrote it, who may also change and remove it.</summary>
public sealed class AuthorScope : IScope<Post, Guid>
{
    public bool Includes(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanWrite(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanDelete(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public OneOf<Expression<Func<Post, bool>>, None> Condition(Guid authorId) => new None();
}
