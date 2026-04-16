using Apia.Ram;
using Apia.Scope;
using Apia.Scoped;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples.PolicyFeed;

public sealed class PolicyFeedTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IMemory BuildRawMemory()
    {
        var map = new RamMemoryMap();
        map.Register(new RamEntities<PostRecord>(p => p.PostId));
        return map.Build();
    }

    private static IMemory Scoped(IMemory raw, UserContext ctx) =>
        raw.WithPolicy(ctx, p => p.With<PostRecord>(
            read:   (post, c) => c.IsAdmin || post.AuthorId == c.UserId,
            write:  (post, c) => post.AuthorId == c.UserId,
            delete: (post, c) => c.IsAdmin || post.AuthorId == c.UserId));

    private static async Task<(PostRecord Own, PostRecord Foreign)> SeedTwoPosts(
        IMemory memory, Guid ownerId, Guid foreignerId)
    {
        var own     = new PostRecord(Guid.NewGuid(), ownerId,    "My post",    0, new HashSet<Guid>(), DateTime.UtcNow);
        var foreign = new PostRecord(Guid.NewGuid(), foreignerId, "Other post", 0, new HashSet<Guid>(), DateTime.UtcNow);
        await memory.Entities<PostRecord>().Save(own);
        await memory.Entities<PostRecord>().Save(foreign);
        return (own, foreign);
    }

    // ── Entity-level policy tests ─────────────────────────────────────────────

    [Fact]
    public async Task All_ReturnsOnlyOwnPosts()
    {
        var raw   = BuildRawMemory();
        var ctx   = new UserContext(Guid.NewGuid());
        var other = Guid.NewGuid();
        var (own, _) = await SeedTwoPosts(raw, ctx.UserId, other);

        var posts = await Scoped(raw, ctx).Entities<PostRecord>().All().ToListAsync();

        Assert.Single(posts);
        Assert.Equal(own.PostId, posts[0].PostId);
    }

    [Fact]
    public async Task All_ReturnsAllPosts_WhenAdmin()
    {
        var raw   = BuildRawMemory();
        var admin = new UserContext(Guid.NewGuid(), IsAdmin: true);
        await SeedTwoPosts(raw, Guid.NewGuid(), Guid.NewGuid());

        var posts = await Scoped(raw, admin).Entities<PostRecord>().All().ToListAsync();

        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public async Task Load_ReturnsNotFound_ForForeignPost()
    {
        var raw = BuildRawMemory();
        var ctx = new UserContext(Guid.NewGuid());
        var (_, foreign) = await SeedTwoPosts(raw, ctx.UserId, Guid.NewGuid());

        var result = await Scoped(raw, ctx).Entities<PostRecord>().Load(foreign.PostId);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task Save_Throws_WhenWritingForeignPost()
    {
        var raw = BuildRawMemory();
        var ctx = new UserContext(Guid.NewGuid());
        var (_, foreign) = await SeedTwoPosts(raw, ctx.UserId, Guid.NewGuid());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Scoped(raw, ctx).Entities<PostRecord>().Save(foreign with { Content = "Hacked!" }));
    }

    [Fact]
    public async Task Delete_Throws_WhenDeletingForeignPost()
    {
        var raw = BuildRawMemory();
        var ctx = new UserContext(Guid.NewGuid());
        var (_, foreign) = await SeedTwoPosts(raw, ctx.UserId, Guid.NewGuid());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Scoped(raw, ctx).Entities<PostRecord>().Delete(foreign.PostId));
    }

    // ── View stream — entity-level filtering ──────────────────────────────────

    [Fact]
    public async Task Feed_FiltersViaEntityPolicy_WhenNotRegisteredAsOrigin()
    {
        // MyFeedViewStream is not registered as a backend origin, so ShallowViewStream
        // falls back to Query(query, scopedMemory). Inside, PolicyEnforcedEntities.All()
        // filters to readable posts. The stream's own query.Context check passes through
        // all readable results (context is null — not injected without registration).
        var raw = BuildRawMemory();
        var ctx = new UserContext(Guid.NewGuid());
        var (own, _) = await SeedTwoPosts(raw, ctx.UserId, Guid.NewGuid());

        var feed = await new MyFeedViewStream(Scoped(raw, ctx))
            .From(new MyFeedQuery(20))
            .ToListAsync();

        Assert.Single(feed);
        Assert.Equal(own.PostId, feed[0].PostId);
    }

    // ── View stream — context injection via ScopedQuery ───────────────────────

    [Fact]
    public async Task Feed_InjectsContext_ViaScopedQuery_WhenRegisteredAsOrigin()
    {
        // When the stream is registered as a backend origin, PolicyMemory wraps it in
        // PolicyAwareViewStream. Because MyFeedQuery : IScopedQuery<UserContext>, the
        // UserContext is injected into the query before the inner stream runs. The inner
        // stream receives the context-bearing query and filters using query.Context directly,
        // against the raw (unscoped) backend — no PolicyEnforcedEntities involved here.
        var map = new RamMemoryMap();
        map.Register(new RamEntities<PostRecord>(p => p.PostId));
        map.Register<PostRecord, MyFeedQuery>(new MyFeedOrigin());
        var raw = map.Build();

        var ctx  = new UserContext(Guid.NewGuid());
        var (own, _) = await SeedTwoPosts(raw, ctx.UserId, Guid.NewGuid());

        var scoped = Scoped(raw, ctx);
        var stream = scoped.TryViewStream<PostRecord, MyFeedQuery>().AsT0;
        var feed   = await stream.From(new MyFeedQuery(20)).ToListAsync();

        Assert.Single(feed);
        Assert.Equal(own.PostId, feed[0].PostId);
    }

    // ── Private origin used in context-injection test ─────────────────────────

    /// <summary>
    /// Registers MyFeedViewStream as the backend source for (PostRecord, MyFeedQuery).
    /// PolicyMemory detects MyFeedQuery : IScopedQuery&lt;UserContext&gt; and wraps the
    /// returned stream in PolicyAwareViewStream, which injects the context.
    /// </summary>
    private sealed class MyFeedOrigin : IViewStreamOrigin<PostRecord, MyFeedQuery, IMemory>
    {
        public IViewStream<PostRecord, MyFeedQuery> From(IMemory memory)
            => new InnerFeedStream(memory);
    }

    private sealed class InnerFeedStream(IMemory memory) : IViewStream<PostRecord, MyFeedQuery>
    {
        public async IAsyncEnumerable<PostRecord> From(MyFeedQuery query)
        {
            var count = 0;
            await foreach (var post in memory.Entities<PostRecord>().All())
            {
                if (count >= query.Limit) yield break;
                if (query.Context is null || query.Context.IsAdmin || post.AuthorId == query.Context.UserId)
                {
                    yield return post;
                    count++;
                }
            }
        }
    }
}
