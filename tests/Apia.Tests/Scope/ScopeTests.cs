using Apia.Ram;
using Apia.Scope;
using Apia.Tests.Entities;
using Apia.Tests.Identity;
using Apia.Tests.Scoping;
using Xunit;

namespace Apia.Tests.Scope;

/// <summary>What a scope permits to be written and removed, which every scope states for itself.</summary>
public sealed class ScopeTests
{
    [Fact]
    public async Task Branch_Save_Throws_WhenTheScopeRefusesTheWrite()
    {
        var author = Guid.NewGuid();
        var branch =
            new ScopeMemory<Guid>(
                    new RamMemory(new ExampleIdentities(), new Overrides()),
                    new Overrides(),
                    new Scopes<Guid>().With(new ReadOnlyAuthorScope()),
                    author)
                .Branch();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await branch.Save(new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow)));
    }

    [Fact]
    public async Task Branch_Save_Stages_WhenTheScopePermitsTheWrite()
    {
        var author = Guid.NewGuid();
        var post = new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow);
        var branch =
            new ScopeMemory<Guid>(
                    new RamMemory(new ExampleIdentities(), new Overrides()),
                    new Overrides(),
                    new Scopes<Guid>().With(new AuthorScope()),
                    author)
                .Branch();
        await branch.Save(post);
        await branch.Commit();

        Assert.Single(await branch.Memory().Vault<Post>().All().ToListAsync());
    }

    [Fact]
    public async Task Branch_Delete_Throws_WhenTheScopeRefusesTheRemoval()
    {
        var author = Guid.NewGuid();
        var post = new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow);
        var memory = new RamMemory(new ExampleIdentities(), new Overrides());
        var seeding = memory.Branch();
        await seeding.Save(post);
        await seeding.Commit();
        var branch =
            new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new ReadOnlyAuthorScope()), author)
                .Branch();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await branch.Delete<Post>(post.PostId));
    }

    [Fact]
    public async Task Branch_Delete_Throws_WhenTheEntityIsOutsideTheScope()
    {
        var post = new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow);
        var memory = new RamMemory(new ExampleIdentities(), new Overrides());
        var seeding = memory.Branch();
        await seeding.Save(post);
        await seeding.Commit();
        var branch =
            new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), Guid.NewGuid())
                .Branch();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await branch.Delete<Post>(post.PostId));
    }

    [Fact]
    public async Task Branch_Save_Stages_WhenTheScopeRefusesOnlyTheRemoval()
    {
        var author = Guid.NewGuid();
        var post = new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow);
        var branch =
            new ScopeMemory<Guid>(
                    new RamMemory(new ExampleIdentities(), new Overrides()),
                    new Overrides(),
                    new Scopes<Guid>().With(new IndelibleAuthorScope()),
                    author)
                .Branch();
        await branch.Save(post);
        await branch.Commit();

        Assert.Single(await branch.Memory().Vault<Post>().All().ToListAsync());
    }

    [Fact]
    public async Task Branch_Delete_Throws_WhenTheScopeRefusesOnlyTheRemoval()
    {
        var author = Guid.NewGuid();
        var post = new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow);
        var memory = new RamMemory(new ExampleIdentities(), new Overrides());
        var seeding = memory.Branch();
        await seeding.Save(post);
        await seeding.Commit();
        var branch =
            new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new IndelibleAuthorScope()), author)
                .Branch();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await branch.Delete<Post>(post.PostId));
    }
}
