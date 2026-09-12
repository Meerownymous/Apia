using Apia.Postgres;
using Apia.Tests.Entities;
using Apia.Tests.Identity;
using Marten;
using NSubstitute;
using Xunit;

namespace Apia.Tests.Postgres;

/// <summary>
/// The Postgres write path, guarded without a database. A save and a delete once built a value carrying
/// the call and never invoked it, so the session never heard of the change and the commit flushed
/// nothing. These tests ask the session what it was actually told.
/// </summary>
public sealed class PostgresWriteTests
{
    [Fact]
    public async Task Commit_TellsTheSession_ToStoreTheSavedEntity()
    {
        var session = Substitute.For<IDocumentSession>();
        var user = new User(Guid.NewGuid(), "Miro");
        var branch = new PostgresBranch(session, new ExampleIdentities(), new Overrides(), Substitute.For<IBranches>());
        await branch.Save(user);
        await branch.Commit();

        session.Received().Store(user);
    }

    [Fact]
    public async Task Commit_TellsTheSession_ToDeleteTheRemovedEntity()
    {
        var session = Substitute.For<IDocumentSession>();
        var id = Guid.NewGuid();
        var branch = new PostgresBranch(session, new ExampleIdentities(), new Overrides(), Substitute.For<IBranches>());
        await branch.Delete<User>(id);
        await branch.Commit();

        session.Received().Delete<User>(id);
    }

    [Fact]
    public async Task Commit_SavesTheSession()
    {
        var session = Substitute.For<IDocumentSession>();
        var branch = new PostgresBranch(session, new ExampleIdentities(), new Overrides(), Substitute.For<IBranches>());
        await branch.Save(new User(Guid.NewGuid(), "Miro"));
        await branch.Commit();

        await session.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commit_TellsTheSessionNothing_WhenNothingWasStaged()
    {
        var session = Substitute.For<IDocumentSession>();
        var branch = new PostgresBranch(session, new ExampleIdentities(), new Overrides(), Substitute.For<IBranches>());
        await branch.Commit();

        session.DidNotReceive().Store(Arg.Any<User>());
    }
}
