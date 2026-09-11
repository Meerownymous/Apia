using Apia.Tests.Records;
using OneOf;

namespace Apia.Tests.Identity;

/// <summary>
/// The identities of the example entity types, declared once and composed into every backend. Note is
/// deliberately absent: it is the entity a commit cannot identify.
/// </summary>
public sealed class ExampleIdentities : IIdentities
{
    private readonly IIdentities identities =
        new Identities()
            .With(new UserId())
            .With(new PostId())
            .With(new CommentId())
            .With(new MeasurementId());

    public OneOf<IIdentity<T>, None> Identity<T>() where T : notnull => identities.Identity<T>();
}
