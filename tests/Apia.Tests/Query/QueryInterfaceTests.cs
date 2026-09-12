using Xunit;

namespace Apia.Tests.Query;

/// <summary>
/// The query interfaces themselves: one member each, answering from a memory they are handed, and a
/// result type the memory's own signature takes from the query, so that a mismatched pair is a build
/// error rather than something a user discovers.
/// </summary>
public sealed class QueryInterfaceTests
{
    [Fact]
    public void AggregateQuery_ProducesItsResultsFromAMemory()
        => Assert.Equal(
            typeof(IMemory),
            typeof(IAggregateQuery<>).GetMethods().Single().GetParameters().Single().ParameterType);

    [Fact]
    public void ProjectionQuery_ProducesItsResultFromAMemory()
        => Assert.Equal(
            typeof(IMemory),
            typeof(IProjectionQuery<>).GetMethods().Single().GetParameters().Single().ParameterType);

    [Fact]
    public void Aggregate_StreamsTheTypeItsQueryDeclares()
    {
        var aggregate = typeof(IMemory).GetMethods().Single(member => member.Name == nameof(IMemory.Aggregate));
        Assert.Equal(
            aggregate.GetParameters().Single().ParameterType.GetGenericArguments().Single(),
            aggregate.ReturnType.GetGenericArguments().Single());
    }

    [Fact]
    public void Projection_ReturnsTheTypeItsQueryDeclares()
    {
        var projection = typeof(IMemory).GetMethods().Single(member => member.Name == nameof(IMemory.Projection));
        Assert.Equal(
            projection.GetParameters().Single().ParameterType.GetGenericArguments().Single(),
            projection.ReturnType.GetGenericArguments().Single());
    }
}
