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
    private static IMemory BuildScopedMemory(IScope<PostRecord, Guid> scope, Guid authorId)
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var registry = new ScopeBuilder<Guid>()
            .Register(scope)
            .Build();
        return new ScopeMemory<Guid>(map.Build(), registry, authorId);
    }

    private static async Task<IMemory> BuildWithPosts(IScope<PostRecord, Guid> scope, Guid authorId, params PostRecord[] posts)
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new PostRecordId());
        var inner = map.Build();

        var branch = inner.Branch();
        foreach (var post in posts)
            await branch.Save(post);
        await branch.Commit();

        var registry = new ScopeBuilder<Guid>()
            .Register(scope)
            .Build();
        return new ScopeMemory<Guid>(inner, registry, authorId);
    }

    [Fact]
    public async Task Aggregate_AllOf_ExcludesOutOfScopeRecords()
    {
        var author1 = Guid.NewGuid();
        var author2 = Guid.NewGuid();
        var post1   = new PostRecord(Guid.NewGuid(), author1, "Hello", 0, new HashSet<Guid>(), DateTime.UtcNow);
        var post2   = new PostRecord(Guid.NewGuid(), author2, "World", 0, new HashSet<Guid>(), DateTime.UtcNow);

        var memory = await BuildWithPosts(new AuthorScope(), author1, post1, post2);

        var results = await memory.Aggregate<PostRecord>()
            .From(new AllOf<PostRecord>())
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(post1.PostId, results[0].PostId);
    }

    [Fact]
    public async Task Aggregate_AllOf_IncludesInScopeRecords()
    {
        var author = Guid.NewGuid();
        var post1  = new PostRecord(Guid.NewGuid(), author, "A", 0, new HashSet<Guid>(), DateTime.UtcNow);
        var post2  = new PostRecord(Guid.NewGuid(), author, "B", 0, new HashSet<Guid>(), DateTime.UtcNow);

        var memory = await BuildWithPosts(new AuthorScope(), author, post1, post2);

        var results = await memory.Aggregate<PostRecord>()
            .From(new AllOf<PostRecord>())
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Vault_Load_ReturnsNotFound_WhenOutOfScope()
    {
        var author1 = Guid.NewGuid();
        var author2 = Guid.NewGuid();
        var post    = new PostRecord(Guid.NewGuid(), author2, "Not mine", 0, new HashSet<Guid>(), DateTime.UtcNow);

        var memory = await BuildWithPosts(new AuthorScope(), author1, post);

        var result = await memory.Vault<PostRecord>().Load(post.PostId);

        Assert.True(result.IsT1);
    }

    // Scope that restricts visibility to posts by a specific author
    private sealed class AuthorScope : IScope<PostRecord, Guid>
    {
        public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;
    }

    // Scope with LINQ expression for SQL-pushdown backends
    private sealed class AuthorLinqScope : IScope<PostRecord, Guid>
    {
        public bool Includes(PostRecord post, Guid authorId) => post.AuthorId == authorId;

        public OneOf<Expression<Func<PostRecord, bool>>, None> AsLinq(Guid authorId)
            => (Expression<Func<PostRecord, bool>>)(p => p.AuthorId == authorId);
    }

    [Fact]
    public async Task Aggregate_AllOf_WithLinqScope_ExcludesOutOfScopeRecords()
    {
        var author1 = Guid.NewGuid();
        var author2 = Guid.NewGuid();
        var post1   = new PostRecord(Guid.NewGuid(), author1, "Mine",     0, new HashSet<Guid>(), DateTime.UtcNow);
        var post2   = new PostRecord(Guid.NewGuid(), author2, "Not mine", 0, new HashSet<Guid>(), DateTime.UtcNow);

        var memory = await BuildWithPosts(new AuthorLinqScope(), author1, post1, post2);

        var results = await memory.Aggregate<PostRecord>()
            .From(new AllOf<PostRecord>())
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(post1.PostId, results[0].PostId);
    }
}
