using System.Reflection;
using Xunit;

namespace Apia.Tests.Override;

/// <summary>
/// The override interfaces themselves, and the <c>With</c> that supplies one: the query an override
/// answers is constrained to the query interface returning the override's own result type, so a pair
/// that does not match is a build error rather than something a user discovers.
/// </summary>
public sealed class OverrideInterfaceTests
{
    [Fact]
    public void AggregateOverride_AnswersAQueryReturningItsOwnResultType()
        => Assert.Equal(
            typeof(IAggregateQuery<>).MakeGenericType(typeof(IAggregateOverride<,>).GetGenericArguments()[1]),
            typeof(IAggregateOverride<,>).GetGenericArguments()[0].GetGenericParameterConstraints().Single());

    [Fact]
    public void ProjectionOverride_AnswersAQueryReturningItsOwnResultType()
        => Assert.Equal(
            typeof(IProjectionQuery<>).MakeGenericType(typeof(IProjectionOverride<,>).GetGenericArguments()[1]),
            typeof(IProjectionOverride<,>).GetGenericArguments()[0].GetGenericParameterConstraints().Single());

    [Fact]
    public void With_TakesOnlyAnOverrideOfAQueryReturningTheSameResultType()
        => Assert.Empty(
            typeof(OverridesExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(supplying =>
                    supplying.GetGenericArguments()[0].GetGenericParameterConstraints().Single()
                        .GetGenericArguments().Single() != supplying.GetGenericArguments()[1])
                .Select(supplying => supplying.GetParameters().Last().ParameterType.Name));
}
