# Apia

A storage abstraction library for .NET 9. Apia gives use cases a single interface — `IMemory` — through which they read and write data, without knowing or caring which backend stores it.

---

## The core idea

Business logic should not be coupled to storage infrastructure. A use case that posts to a user feed, registers a new account, or computes a report should be expressible in terms of records and queries — not SQL statements, file paths, or Cosmos DB change feeds.

Apia provides three primitive storage concepts:

| Abstraction | What it models |
|---|---|
| `IEntities<T>` | A catalog of records, each identified by a `Guid` |
| `IVault<T>` | A single record — settings, app state, a singleton |
| `IViews<T, TQuery>` | A read projection that streams results for a query |

All three are accessed through `IMemory`. Use cases receive `IMemory` as a dependency and compose storage operations from it:

```csharp
public sealed class PostToFeedUseCase(IMemory memory)
{
    public async Task<OneOf<PostRecord, Conflict<PostRecord>>> Execute(Guid userId, string content)
    {
        var post = new PostRecord(Guid.NewGuid(), userId, content, LikeCount: 0,
                                  LikedByUserIds: ImmutableHashSet<Guid>.Empty,
                                  CreatedAt: DateTime.UtcNow);
        return await memory.Entities<PostRecord>().Save(post);
    }
}
```

The use case does not reference any backend. It works identically against in-memory, file-based, or PostgreSQL storage — and against any future backend that implements `IMemory`.

---

## Backends

Three backends ship out of the box:

| Backend | When to use |
|---|---|
| `Apia.Ram` | Tests, prototypes, single-process in-memory state |
| `Apia.File` | Small apps, CLIs, dev environments, offline-capable tools |
| `Apia.Postgres` | Production, multi-instance deployments, relational queries |

Every backend is registered through `IMemoryMap` and produces an `IMemory`:

```csharp
// In tests
var map = new RamMemoryMap();
map.Register(new RamEntities<PostRecord>(p => p.PostId));
map.Register(new RamEntities<UserRecord>(u => u.UserId));
var memory = map.Build();

// In production — swap the map, nothing else changes
var map = new PostgresMemoryMap(connectionString);
map.Register(new PostgresEntities<PostRecord>(p => p.PostId));
map.Register(new PostgresEntities<UserRecord>(u => u.UserId));
var memory = map.Build();
```

The same use cases run against both.

---

## Use case reusability

Because use cases depend only on `IMemory`, they are backend-agnostic by construction. This has two practical consequences.

**Testing without a database.** Every use case can be tested with `RamMemoryMap`. No mocks, no test containers, no network. Tests are fast and deterministic.

```csharp
[Fact]
public async Task PostAppearsInFeed()
{
    var map = new RamMemoryMap();
    map.Register(new RamEntities<PostRecord>(p => p.PostId));
    map.Register(new RamEntities<UserRecord>(u => u.UserId));
    map.Register(new UserFeedSynopsisStream());
    var memory = map.Build();

    var user = new UserRecord(Guid.NewGuid(), "alice");
    await memory.Entities<UserRecord>().Save(user);
    await new CreatePostUseCase(memory).Execute(user.UserId, "Hello, world");

    var feed = await memory
        .Views<UserPostSummaryProjection, UserFeedQuery>()
        .Query(new UserFeedQuery(user.UserId, Limit: 10))
        .ToListAsync();

    Assert.Single(feed);
    Assert.Equal("Hello, world", feed[0].Content);
}
```

**Incremental backend migration.** A use case written today against `RamMemoryMap` runs on PostgreSQL tomorrow without changing a single line of business logic.

---

## Projections

Read projections are expressed as `ISynopsisStream<TResult, TQuery, TContext>`. The synopsis is registered with the memory map and built lazily at query time — the backend injects its context (`IMemory` for RAM, a Marten `IDocumentSession` for Postgres).

```csharp
public sealed class UserFeedSynopsisStream()
    : RamSynopsisStream<UserPostSummaryProjection, UserFeedQuery>(Query)
{
    private static async IAsyncEnumerable<UserPostSummaryProjection> Query(
        IMemory memory, UserFeedQuery query)
    {
        var posts    = memory.Entities<PostRecord>();
        var comments = memory.Entities<CommentRecord>();
        var users    = memory.Entities<UserRecord>();

        var userPosts = new List<PostRecord>();
        await foreach (var post in posts.All())
            if (post.AuthorId == query.UserId)
                userPosts.Add(post);

        var commentCounts = new Dictionary<Guid, int>();
        await foreach (var comment in comments.All())
            if (userPosts.Any(p => p.PostId == comment.PostId))
                commentCounts[comment.PostId] = commentCounts.GetValueOrDefault(comment.PostId) + 1;

        var userResult = await users.Load(query.UserId);
        if (userResult.IsT1) yield break;
        var author = userResult.AsT0;

        foreach (var post in userPosts.OrderByDescending(p => p.CreatedAt).Take(query.Limit))
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserPostSummaryProjection(
                PostId:       post.PostId,
                AuthorName:   author.Username,
                Content:      post.Content,
                LikeCount:    post.LikeCount,
                CommentCount: commentCount,
                CreatedAt:    post.CreatedAt
            );
        }
    }
}
```

A `PostgresSynopsisStream` variant of the same projection can use SQL joins and indexes while sharing the same `UserFeedQuery` and `UserPostSummaryProjection` types. Callers do not change.

---

## Transactions

```csharp
await using var tx = memory.Begin();
var txMemory = tx.Memory();

await new DeductCreditsUseCase(txMemory).Execute(userId, amount);
await new RecordPurchaseUseCase(txMemory).Execute(userId, itemId);

await tx.Commit(); // both operations persist atomically, or neither does
```

`DisposeAsync` without `Commit` rolls back. Use cases receive the transactional `IMemory` and are unaware they are inside a transaction.

---

## Optimistic concurrency

`Save` does not throw on conflict. It returns a discriminated union:

```csharp
var result = await memory.Entities<PostRecord>().Save(updatedPost);

result.Switch(
    saved    => Console.WriteLine($"Saved {saved.PostId}"),
    conflict => Console.WriteLine($"Conflict — current: {conflict.Current.Content}")
);
```

`Load` returns `OneOf<T, NotFound>` — no `null`, no `KeyNotFoundException`:

```csharp
var result = await memory.Entities<PostRecord>().Load(postId);

if (result.IsT1)
    return Results.NotFound();

var post = result.AsT0;
```

The caller decides what to do with a conflict: retry, merge, or surface it to the user.

---

## Staged development

Apia is designed for teams that want to ship quickly and optimize deliberately.

**Stage 1 — standard stores.** Start with `RamMemoryMap` in tests and `FileMemoryMap` or `PostgresMemoryMap` in production. Write all use cases against `IMemory`. Performance is predictable and sufficient for most early workloads.

**Stage 2 — targeted optimization.** When profiling reveals a bottleneck — a projection that full-scans a collection, an entity store on a hot path — replace that specific registration with a specialized implementation. A `PostgresSynopsisStream` for a slow view; a custom `IEntities<T>` backed by Redis for a high-throughput catalog. Everything else stays unchanged.

**Stage 3 — cross-cutting instrumentation.** Because every store interaction goes through `IEntities<T>`, `IVault<T>`, and `IViewStream<T, TQuery>`, measuring decorators, caching layers, and audit logs can wrap any backend uniformly:

```csharp
public sealed class TimedEntities<T>(IEntities<T> inner, IMetrics metrics) : IEntities<T>
{
    public async Task<OneOf<T, Conflict<T>>> Save(T record)
    {
        using var _ = metrics.Time($"entities.{typeof(T).Name}.save");
        return await inner.Save(record);
    }
    // ...
}
```

The use cases that call `memory.Entities<T>()` do not know the decorator is there.

---

## Example: a social feed application

The following use cases cover typical operations in a feed-style application. Each takes only `IMemory`.

### Register a user

```csharp
public sealed class RegisterUserUseCase(IMemory memory)
{
    public async Task<UserRecord> Execute(string username)
    {
        var user = new UserRecord(Guid.NewGuid(), username);
        await memory.Entities<UserRecord>().Save(user);
        return user;
    }
}
```

### Create a post

```csharp
public sealed class CreatePostUseCase(IMemory memory)
{
    public async Task<OneOf<PostRecord, NotFound>> Execute(Guid authorId, string content)
    {
        var author = await memory.Entities<UserRecord>().Load(authorId);
        if (author.IsT1)
            return new NotFound();

        var post = new PostRecord(
            PostId:         Guid.NewGuid(),
            AuthorId:       authorId,
            Content:        content,
            LikeCount:      0,
            LikedByUserIds: ImmutableHashSet<Guid>.Empty,
            CreatedAt:      DateTime.UtcNow);

        await memory.Entities<PostRecord>().Save(post);
        return post;
    }
}
```

### Like a post

```csharp
public sealed class LikePostUseCase(IMemory memory)
{
    public async Task<OneOf<PostRecord, NotFound, Conflict<PostRecord>>> Execute(Guid postId, Guid userId)
    {
        var loaded = await memory.Entities<PostRecord>().Load(postId);
        if (loaded.IsT1)
            return new NotFound();

        var updated = loaded.AsT0 with
        {
            LikeCount      = loaded.AsT0.LikeCount + 1,
            LikedByUserIds = loaded.AsT0.LikedByUserIds.Add(userId)
        };

        var saved = await memory.Entities<PostRecord>().Save(updated);
        if (saved.IsT1)
            return saved.AsT1; // conflict — caller retries with a fresh Load

        return saved.AsT0;
    }
}
```

### Add a comment

```csharp
public sealed class AddCommentUseCase(IMemory memory)
{
    public async Task<OneOf<CommentRecord, NotFound>> Execute(Guid postId, Guid authorId, string text)
    {
        var post = await memory.Entities<PostRecord>().Load(postId);
        if (post.IsT1)
            return new NotFound();

        var comment = new CommentRecord(Guid.NewGuid(), postId, authorId, text, DateTime.UtcNow);
        await memory.Entities<CommentRecord>().Save(comment);
        return comment;
    }
}
```

### Read the feed

```csharp
public sealed class GetUserFeedUseCase(IMemory memory)
{
    public IAsyncEnumerable<UserPostSummaryProjection> Execute(Guid userId, int limit)
        => memory.Views<UserPostSummaryProjection, UserFeedQuery>()
                 .Query(new UserFeedQuery(userId, limit));
}
```

---

## Who this is for

Apia is a good fit for teams that:

- Write automated tests and want them fast — `RamMemoryMap` makes every use case testable without a running database
- Value use case portability — the same business logic runs in a CLI tool, a web API, a background worker, and a test harness
- Prefer explicit error modeling — `OneOf<T, NotFound>` and `OneOf<T, Conflict<T>>` eliminate silent null returns and exception-based control flow
- Expect to grow — starting simple and migrating specific stores to optimized implementations as load increases is a deliberate, supported path

Apia is a less natural fit for teams that:

- Need complex cross-entity relational queries from day one — projections help, but Apia is not a query engine
- Have existing ORM-heavy codebases where the repository pattern is already deeply established
- Require fine-grained database schema control — the Postgres backend uses Marten (document store semantics) rather than hand-crafted tables

---

## Installation

```
dotnet add package Apia
dotnet add package Apia.Ram       # in-memory backend
dotnet add package Apia.File      # file-based backend
dotnet add package Apia.Postgres  # PostgreSQL via Marten
```
