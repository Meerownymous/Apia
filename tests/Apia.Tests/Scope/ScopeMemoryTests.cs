using System.Linq.Expressions;
using Apia;
using Apia.Ram;
using Apia.Scope;
using Apia.Tests.Record;
using OneOf;
using Xunit;

namespace Apia.Tests.Scope;

public sealed class ScopeMemoryTests
{
    [Fact]
    public async Task Aggregate_AllOf_ExcludesOutOfScopeRecords()
    {
        var author1 = Guid.NewGuid();
        var post1   = new PostRecord(Guid.NewGuid(), author1, "Mine", 0, new HashSet<Guid>(), DateTime.UtcNow);
        var map     = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var inner  = map.Build();
        var branch = inner.Branch();
        await branch.Save(post1);
        await branch.Save(new PostRecord(Guid.NewGuid(), Guid.NewGuid(), "Not mine", 0, new HashSet<Guid>(), DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            post1,
            (await new ScopeMemory<Guid>(inner, new ScopeBuilder<Guid>().Register<PostRecord>(new AuthorScope()).Build(), author1)
                .Aggregate<PostRecord>()
                .From(new AllOf<PostRecord>())
                .ToListAsync())
            .Single());
    }

    [Fact]
    public async Task Aggregate_AllOf_IncludesInScopeRecords()
    {
        var author = Guid.NewGuid();
        var map    = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var inner  = map.Build();
        var branch = inner.Branch();
        await branch.Save(new PostRecord(Guid.NewGuid(), author, "A", 0, new HashSet<Guid>(), DateTime.UtcNow));
        await branch.Save(new PostRecord(Guid.NewGuid(), author, "B", 0, new HashSet<Guid>(), DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            2,
            (await new ScopeMemory<Guid>(inner, new ScopeBuilder<Guid>().Register<PostRecord>(new AuthorScope()).Build(), author)
                .Aggregate<PostRecord>()
                .From(new AllOf<PostRecord>())
                .ToListAsync())
            .Count);
    }

    [Fact]
    public async Task Vault_Load_ReturnsNotFound_WhenOutOfScope()
    {
        var post   = new PostRecord(Guid.NewGuid(), Guid.NewGuid(), "Not mine", 0, new HashSet<Guid>(), DateTime.UtcNow);
        var map    = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var inner  = map.Build();
        var branch = inner.Branch();
        await branch.Save(post);
        await branch.Commit();

        Assert.True(
            (await new ScopeMemory<Guid>(inner, new ScopeBuilder<Guid>().Register<PostRecord>(new AuthorScope()).Build(), Guid.NewGuid())
                .Vault<PostRecord>()
                .Load(post.PostId))
            .IsT1);
    }

    [Fact]
    public async Task Aggregate_AllOf_WithLinqScope_ExcludesOutOfScopeRecords()
    {
        var author1 = Guid.NewGuid();
        var post1   = new PostRecord(Guid.NewGuid(), author1, "Mine", 0, new HashSet<Guid>(), DateTime.UtcNow);
        var map     = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var inner  = map.Build();
        var branch = inner.Branch();
        await branch.Save(post1);
        await branch.Save(new PostRecord(Guid.NewGuid(), Guid.NewGuid(), "Not mine", 0, new HashSet<Guid>(), DateTime.UtcNow));
        await branch.Commit();

        Assert.Equal(
            post1,
            (await new ScopeMemory<Guid>(inner, new ScopeBuilder<Guid>().Register<PostRecord>(new AuthorLinqScope()).Build(), author1)
                .Aggregate<PostRecord>()
                .From(new AllOf<PostRecord>())
                .ToListAsync())
            .Single());
    }

    private sealed record AuthorScope : IScope<PostRecord, Guid>
    {
        public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;
    }

    private sealed record AuthorLinqScope : IScope<PostRecord, Guid>
    {
        public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;

        public OneOf<Expression<Func<PostRecord, bool>>, None> AsLinq(Guid authorId)
            => (Expression<Func<PostRecord, bool>>)(p => p.AuthorId == authorId);
    }
}
