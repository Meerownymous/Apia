using System.Linq.Expressions;
using Apia.Scope;
using Apia.Tests.Entities;
using OneOf;

namespace Apia.Tests.Scoping;

/// <summary>
/// An author who may read and change their own posts and remove none of them. The write rule and the
/// delete rule are separate statements, so one may permit what the other refuses.
/// </summary>
public sealed class IndelibleAuthorScope : IScope<Post, Guid>
{
    public bool Includes(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanWrite(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public bool CanDelete(Post entity, Guid authorId) => false;

    public OneOf<Expression<Func<Post, bool>>, None> Condition(Guid authorId) => new None();
}
