using Apia.Scope;
using Apia.Tests.Backend;
using Apia.Tests.Entities;
using Apia.Tests.Query;
using Apia.Tests.Scoping;
using Xunit;

namespace Apia.Tests.Contract;

/// <summary>
/// The promises a use case relies on, exercised through the memory on every backend. A backend that
/// cannot run here is named as skipped rather than silently absent.
/// </summary>
public sealed class MemoryTests
{
    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsTheEntity_WhenSavedAndCommitted(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        Assert.Equal(
            user,
            (await memory.Vault<User>().Entity(user.UserId))
                .Match(found => found, _ => throw new InvalidOperationException("NotFound")));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsNotFound_WhenSavedAndNotCommitted(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        await memory.Branch().Save(user);

        Assert.True((await memory.Vault<User>().Entity(user.UserId)).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsNotFound_WhenDeleted(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var saving = memory.Branch();
        await saving.Save(user);
        await saving.Commit();
        var deleting = memory.Branch();
        await deleting.Delete<User>(user.UserId);
        await deleting.Commit();

        Assert.True((await memory.Vault<User>().Entity(user.UserId)).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsTheLaterEntity_WhenSavedTwice(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var first = memory.Branch();
        await first.Save(user);
        await first.Commit();
        var second = memory.Branch();
        await second.Save(user with { Username = "Ralph" });
        await second.Commit();

        Assert.Equal(
            "Ralph",
            (await memory.Vault<User>().Entity(user.UserId))
                .Match(found => found.Username, _ => throw new InvalidOperationException("NotFound")));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_All_StreamsEveryEntity(IBackend backend)
    {
        var memory = backend.Memory();
        var branch = memory.Branch();
        await branch.Save(new User(Guid.NewGuid(), "Miro"));
        await branch.Save(new User(Guid.NewGuid(), "Ralph"));
        await branch.Commit();

        Assert.Equal(2, (await memory.Vault<User>().All().ToListAsync()).Count);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Matching_StreamsOnlyEntitiesSatisfyingTheCondition(IBackend backend)
    {
        var memory = backend.Memory();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "liked", 7, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "unliked", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            "liked",
            (await memory.Vault<Post>().Matching(post => post.LikeCount > 3).ToListAsync()).Single().Content);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Aggregate_Results_AreTheSameOnEveryBackend(IBackend backend)
    {
        var memory = backend.Memory();
        var author = new User(Guid.NewGuid(), "Miro");
        var post = new Post(Guid.NewGuid(), author.UserId, "Great unit test discovered", 1, DateTime.UtcNow);
        var branch = memory.Branch();
        await branch.Save(author);
        await branch.Save(post);
        await branch.Save(new Comment(Guid.NewGuid(), post.PostId, Guid.NewGuid(), "Mine smells like cat food", DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            new UserPostSummary(post.PostId, "Miro", post.Content, 1, 1, post.CreatedAt),
            (await memory.Aggregate(new UserFeed(author.UserId, 20)).ToListAsync()).Single());
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Aggregate_Results_AreEmpty_WhenTheAuthorWroteNothing(IBackend backend)
    {
        var memory = backend.Memory();
        var author = new User(Guid.NewGuid(), "Miro");
        var branch = memory.Branch();
        await branch.Save(author);
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Empty(await memory.Aggregate(new UserFeed(author.UserId, 20)).ToListAsync());
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Aggregate_Results_AreEmpty_WhenTheAuthorIsNotStored(IBackend backend)
    {
        var memory = backend.Memory();
        var unstoredAuthor = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), unstoredAuthor, "written by nobody the memory holds", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Empty(await memory.Aggregate(new UserFeed(unstoredAuthor, 20)).ToListAsync());
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Projection_Result_IsTheSameOnEveryBackend(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "one", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), author, "two", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(2, await memory.Projection(new PostCount(author)));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_ReadsItsOwnStagedSave(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var branch = memory.Branch();
        await branch.Save(user);

        Assert.Equal(
            user,
            (await branch.Memory().Vault<User>().Entity(user.UserId))
                .Match(found => found, _ => throw new InvalidOperationException("NotFound")));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_ReadsItsOwnStagedDelete(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var saving = memory.Branch();
        await saving.Save(user);
        await saving.Commit();
        var deleting = memory.Branch();
        await deleting.Delete<User>(user.UserId);

        Assert.True((await deleting.Memory().Vault<User>().Entity(user.UserId)).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_ReadsItsOwnStagedSave_ThroughAQuery(IBackend backend)
    {
        var memory = backend.Memory();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "staged", 0, DateTime.UtcNow));

        Assert.Single(await branch.Memory().Aggregate(new AllPosts()).ToListAsync());
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_CountsItsOwnStagedSaves_ThroughAQuery(IBackend backend)
    {
        var author = Guid.NewGuid();
        var branch = backend.Memory().Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "staged", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), author, "also staged", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));

        Assert.Equal(2, await branch.Memory().Projection(new PostCount(author)));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsNotFound_WhenTheCommitFailed(IBackend backend)
    {
        var memory = backend.Memory();
        var measurement = new Measurement(Guid.NewGuid(), 21.5);
        var branch = memory.Branch();
        await branch.Save(measurement);
        await branch.Save(new Note(Guid.NewGuid(), "an entity no identity was given for"));
        try { await branch.Commit(); } catch (InvalidOperationException) { }

        Assert.True((await memory.Vault<Measurement>().Entity(measurement.MeasurementId)).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Commit_ReportsStale_WhenAnEntityItReadChangedSince(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var seeding = memory.Branch();
        await seeding.Save(user);
        await seeding.Commit();
        var reading = memory.Branch();
        await reading.Memory().Vault<User>().Entity(user.UserId);
        var meddling = memory.Branch();
        await meddling.Save(user with { Username = "Ralph" });
        await meddling.Commit();
        await reading.Save(user with { Username = "Bart" });

        Assert.True((await reading.Commit()).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Commit_ReportsCommitted_WhenNothingItReadChanged(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var seeding = memory.Branch();
        await seeding.Save(user);
        await seeding.Commit();
        var reading = memory.Branch();
        await reading.Memory().Vault<User>().Entity(user.UserId);
        await reading.Save(user with { Username = "Bart" });

        Assert.True((await reading.Commit()).Match(_ => true, _ => false));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsNotFound_WhenOutsideTheScope(IBackend backend)
    {
        var memory = backend.Memory();
        var post = new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow);
        var branch = memory.Branch();
        await branch.Save(post);
        await branch.Commit();

        Assert.True(
            (await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), Guid.NewGuid())
                .Vault<Post>()
                .Entity(post.PostId))
            .Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_All_StreamsOnlyEntitiesInsideTheScope(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), author, "also mine", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            2,
            (await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), author)
                .Vault<Post>()
                .All()
                .ToListAsync())
            .Count);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Aggregate_Results_StreamOnlyEntitiesInsideTheScope(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            "mine",
            (await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), author)
                .Aggregate(new AllPosts())
                .ToListAsync())
            .Single().Content);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Aggregate_Results_AreEmpty_WhenTheAuthorsPostsAreOutsideTheScope(IBackend backend)
    {
        var memory = backend.Memory();
        var reader = Guid.NewGuid();
        var stranger = new User(Guid.NewGuid(), "Ralph");
        var branch = memory.Branch();
        await branch.Save(stranger);
        await branch.Save(new Post(Guid.NewGuid(), stranger.UserId, "someone else's", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), reader, "mine", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Empty(
            await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), reader)
                .Aggregate(new UserFeed(stranger.UserId, 20))
                .ToListAsync());
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_StreamsOnlyEntitiesInsideTheScope_ThroughAQuery(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var seeding = memory.Branch();
        await seeding.Save(new Post(Guid.NewGuid(), author, "mine, committed", 0, DateTime.UtcNow));
        await seeding.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await seeding.Commit();
        var branch =
            new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), author).Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine, staged", 0, DateTime.UtcNow));

        Assert.Equal(2, (await branch.Memory().Aggregate(new AllPosts()).ToListAsync()).Count);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Branch_Memory_CountsOnlyEntitiesInsideTheScope_ThroughAQuery(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var seeding = memory.Branch();
        await seeding.Save(new Post(Guid.NewGuid(), stranger, "someone else's", 0, DateTime.UtcNow));
        await seeding.Save(new Post(Guid.NewGuid(), stranger, "also someone else's", 0, DateTime.UtcNow));
        await seeding.Commit();
        var branch =
            new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), author).Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine, staged", 0, DateTime.UtcNow));

        Assert.Equal(0, await branch.Memory().Projection(new PostCount(stranger)));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Projection_Result_CountsNothing_WhenTheEntitiesAreOutsideTheScope(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), stranger, "someone else's", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), stranger, "also someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            0,
            await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), author)
                .Projection(new PostCount(stranger)));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_All_PushesTheScopeCondition_WhenTheScopeStatesOne(IBackend backend)
    {
        var memory = backend.Memory();
        var author = Guid.NewGuid();
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), author, "mine", 0, DateTime.UtcNow));
        await branch.Save(new Post(Guid.NewGuid(), Guid.NewGuid(), "someone else's", 0, DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            "mine",
            (await new ScopeMemory<Guid>(memory, new Overrides(), new Scopes<Guid>().With(new ConditionedAuthorScope()), author)
                .Vault<Post>()
                .All()
                .ToListAsync())
            .Single().Content);
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsNotFound_WhenTheBranchDeletedWhatItHadSaved(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Delete<User>(user.UserId);
        await branch.Commit();

        Assert.True((await memory.Vault<User>().Entity(user.UserId)).Match(_ => false, _ => true));
    }

    [SkippableTheory]
    [ClassData(typeof(Backends))]
    public async Task Vault_Entity_ReturnsTheEntity_WhenTheBranchSavedWhatItHadDeleted(IBackend backend)
    {
        var memory = backend.Memory();
        var user = new User(Guid.NewGuid(), "Miro");
        var seeding = memory.Branch();
        await seeding.Save(user);
        await seeding.Commit();
        var branch = memory.Branch();
        await branch.Delete<User>(user.UserId);
        await branch.Save(user with { Username = "Ralph" });
        await branch.Commit();

        Assert.Equal(
            "Ralph",
            (await memory.Vault<User>().Entity(user.UserId))
                .Match(found => found.Username, _ => throw new InvalidOperationException("NotFound")));
    }
}
