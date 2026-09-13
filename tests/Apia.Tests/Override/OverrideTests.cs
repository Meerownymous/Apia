using Apia.Ram;
using Apia.Scope;
using Apia.Tests.Entities;
using Apia.Tests.Identity;
using Apia.Tests.Query;
using Apia.Tests.Scoping;
using Xunit;

namespace Apia.Tests.Override;

/// <summary>
/// A backend-specific implementation of one query, supplied where the memory is composed. A use case
/// asks the same query either way and never names the override.
/// </summary>
public sealed class OverrideTests
{
    [Fact]
    public async Task Aggregate_Results_ComeFromTheOverride_WhenOneWasGiven()
    {
        var answer = new Post(Guid.NewGuid(), Guid.NewGuid(), "from the backend", 0, DateTime.UtcNow);
        var memory = new RamMemory(new ExampleIdentities(), new Overrides().With(new HandWrittenAllPosts(answer)));
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "from the store", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(answer, (await memory.Aggregate(new AllPosts()).ToListAsync()).Single());
    }

    [Fact]
    public async Task Aggregate_Results_ComeFromTheQuery_WhenNoOverrideWasGiven()
    {
        var memory = new RamMemory(new ExampleIdentities(), new Overrides());
        var stored = new Post(Guid.NewGuid(), Guid.NewGuid(), "from the store", 0, DateTime.UtcNow);
        var branch = memory.Branch();
        await branch.Save(stored);
        await branch.Commit();

        Assert.Equal(stored, (await memory.Aggregate(new AllPosts()).ToListAsync()).Single());
    }

    [Fact]
    public async Task Projection_Result_ComesFromTheOverride_WhenOneWasGiven()
    {
        var memory = new RamMemory(new ExampleIdentities(), new Overrides().With(new HandWrittenPostCount(41)));

        Assert.Equal(41, await memory.Projection(new PostCount(Guid.NewGuid())));
    }

    [Fact]
    public async Task Projection_Result_ComesFromTheOverride_WhenTheScopedMemoryWasGivenOne()
        => Assert.Equal(
            41,
            await new ScopeMemory<Guid>(
                    new RamMemory(new ExampleIdentities(), new Overrides()),
                    new Overrides().With(new HandWrittenPostCount(41)),
                    new Scopes<Guid>().With(new AuthorScope()),
                    Guid.NewGuid())
                .Projection(new PostCount(Guid.NewGuid())));

    [Fact]
    public async Task Projection_Result_ComesFromTheQuery_WhenTheScopedMemoryWasGivenNoOverride()
        => Assert.Equal(
            0,
            await new ScopeMemory<Guid>(
                    new RamMemory(new ExampleIdentities(), new Overrides().With(new HandWrittenPostCount(41))),
                    new Overrides(),
                    new Scopes<Guid>().With(new AuthorScope()),
                    Guid.NewGuid())
                .Projection(new PostCount(Guid.NewGuid())));
}
