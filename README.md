# Apia

A storage abstraction library for .NET 9. Apia gives use cases a single interface — `IMemory` — through which they read and write data, without knowing or caring which backend stores it.

---

## The core idea

Business logic should not be coupled to storage infrastructure. A use case that posts to a user feed, registers a new account, or computes a report should be expressible in terms of records and queries — not SQL statements, file paths, or Cosmos DB change feeds.

Apia provides three read operations on `IMemory`:

| Method | What it does |
|---|---|
| `Aggregate<T>(query)` | Streams multiple results for a query over entities of type `T` |
| `Projection<T>(query)` | Returns a single computed result for a query |
| `Vault<T>().Load(id)` | Reads a single entity by `Guid` |

All three are accessed through `IMemory`. Use cases receive `IMemory` as a dependency and compose storage operations from it:

```csharp
public interface IMemory
{
    IAsyncEnumerable<T> Aggregate<T>(object query);
    Task<T> Projection<T>(object query);
    IVault<T> Vault<T>();
    IBranch Branch();
}
```

Use cases read through `Aggregate`, `Projection`, and `Vault`. They write through a `Branch` — a unit of work that stages changes and flushes them atomically on `Commit`.

The use case does not reference any backend. It works identically against in-memory, file-based, or PostgreSQL storage — and against any future backend that implements `IMemory`.

---

## Backends

Three backends ship out of the box:

| Backend | When to use |
|---|---|
| `Apia.Ram` | Tests, prototypes, single-process in-memory state |
| `Apia.File` | Small apps, CLIs, dev environments, offline-capable tools |
| `Apia.Postgres` | Production, multi-instance deployments, relational queries |

Every backend implements `IMemoryMap` and produces an `IMemory`. Entity types and query sources are registered before calling `Build`:

```csharp
// In tests
var map = new RamMemoryMap();
map.RegisterStore<PostRecord>(new PostRecordId());
map.RegisterStore<UserRecord>(new UserRecordId());
var memory = map.Build();

// In production — swap the map, nothing else changes
var map = new PostgresMemoryMap(connectionString);
map.RegisterStore<PostRecord>(new PostRecordId());
map.RegisterStore<UserRecord>(new UserRecordId());
var memory = map.Build();
```

`IIdentity<T>` tells the store how to extract the entity's `Guid`:

```csharp
public sealed class PostRecordId : IIdentity<PostRecord>
{
    public Guid Of(PostRecord entity) => entity.PostId;
}
```

The same use cases run against both.

---

## Reading with Aggregate and Vault

### AllOf\<T\> — stream all entities

```csharp
await foreach (var post in memory.Aggregate<PostRecord>(new AllOf<PostRecord>()))
    Console.WriteLine(post.Content);
```

### LinqQuery\<T\> — filter with a predicate

```csharp
var userPosts = memory.Aggregate<PostRecord>(
    new LinqQuery<PostRecord>(p => p.AuthorId == userId));
```

SQL-capable backends translate the predicate to a `WHERE` clause; others compile and apply it in-process.

### Vault — load a single entity by id

```csharp
var result = await memory.Vault<UserRecord>().Load(userId);

result.Match(
    user     => Console.WriteLine($"Found: {user.Username}"),
    notFound => Console.WriteLine("No such user")
);
```

`Load` returns `OneOf<T, NotFound>` — no `null`, no `KeyNotFoundException`.

---

## Writing through a Branch

Mutations go through `IBranch`. A branch stages `Save` and `Delete` operations and flushes them when `Commit` is called:

```csharp
public interface IBranch
{
    IAsyncEnumerable<T> Aggregate<T>(object query);
    Task<T> Projection<T>(object query);

    Task Save<T>(T entity);
    Task Delete<T>(Guid id);

    Task Commit();
}
```

```csharp
public sealed class RegisterUserUseCase(IMemory memory)
{
    public async Task<UserRecord> Execute(string username)
    {
        var user = new UserRecord(Guid.NewGuid(), username);
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();
        return user;
    }
}
```

Calling `Branch()` multiple times gives independent units of work. Each `Commit` is an atomic flush of its staged operations. `IBranch` also exposes `Aggregate` and `Projection` — pass a query to read within the same unit of work.

---

## Custom aggregate sources

For queries that span multiple entity types or need domain-specific filtering, implement `IAggregateSource<TResult, TQuery>` and register it with the memory map.

A custom query type carries its parameters and implements `IQuery<TSelf>` (the self-seed pattern):

```csharp
public record UserFeedQuery(Guid UserId, int Limit) : IQuery<UserFeedQuery>
{
    public UserFeedQuery Seed() => this;
}
```

The source receives the query and an `IMemory` it can use to pull data from any registered store:

```csharp
public sealed class UserFeedProjection : IAggregateSource<UserPostSummaryView, UserFeedQuery>
{
    public async IAsyncEnumerable<UserPostSummaryView> From(IQuery<UserFeedQuery> query, IMemory memory)
    {
        var q = query.Seed();

        var author = await memory.Vault<UserRecord>().Load(q.UserId);
        if (author.IsT1) yield break;

        var userPosts = new List<PostRecord>();
        await foreach (var post in memory.Aggregate<PostRecord>(
                           new LinqQuery<PostRecord>(p => p.AuthorId == q.UserId)))
            userPosts.Add(post);

        var commentCounts = new Dictionary<Guid, int>();
        await foreach (var comment in memory.Aggregate<CommentRecord>(new AllOf<CommentRecord>()))
            if (userPosts.Any(p => p.PostId == comment.PostId))
                commentCounts[comment.PostId] = commentCounts.GetValueOrDefault(comment.PostId) + 1;

        foreach (var post in userPosts.OrderByDescending(p => p.CreatedAt).Take(q.Limit))
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserPostSummaryView(
                PostId:       post.PostId,
                AuthorName:   author.AsT0.Username,
                Content:      post.Content,
                LikeCount:    post.LikeCount,
                CommentCount: commentCount,
                CreatedAt:    post.CreatedAt
            );
        }
    }
}
```

Register it alongside the stores:

```csharp
map.RegisterStore<UserRecord>(new UserRecordId());
map.RegisterStore<PostRecord>(new PostRecordId());
map.RegisterStore<CommentRecord>(new CommentRecordId());
map.RegisterQuery<UserPostSummaryView, UserFeedQuery>(new UserFeedProjection());
```

| Registration method | Registers |
|---|---|
| `RegisterStore<T>(IIdentity<T>)` | A mutable entity store for type `T` |
| `RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery>)` | A multi-result query source |
| `RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery>)` | A single-result projection source |

A Postgres-native variant of the same projection can use SQL joins and indexes while sharing the same `UserFeedQuery` and `UserPostSummaryView` types. Callers do not change.

---

## Use case reusability

Because use cases depend only on `IMemory`, they are backend-agnostic by construction. This has two practical consequences.

**Testing without a database.** Every use case can be tested with `RamMemoryMap`. No mocks, no test containers, no network. Tests are fast and deterministic.

```csharp
[Fact]
public async Task PostAppearsInFeed()
{
    var map = new RamMemoryMap();
    map.RegisterStore<UserRecord>(new UserRecordId());
    map.RegisterStore<PostRecord>(new PostRecordId());
    map.RegisterStore<CommentRecord>(new CommentRecordId());
    map.RegisterQuery<UserPostSummaryView, UserFeedQuery>(new UserFeedProjection());
    var memory = map.Build();

    var user = new UserRecord(Guid.NewGuid(), "alice");
    var branch = memory.Branch();
    await branch.Save(user);
    await branch.Commit();

    await new CreatePostUseCase(memory).Execute(user.UserId, "Hello, world");

    var feed = await memory
        .Aggregate<UserPostSummaryView>(new UserFeedQuery(user.UserId, Limit: 10))
        .ToListAsync();

    Assert.Single(feed);
    Assert.Equal("Hello, world", feed[0].Content);
}
```

**Incremental backend migration.** A use case written today against `RamMemoryMap` runs on PostgreSQL tomorrow without changing a single line of business logic.

---

## Staged development

Apia is designed for teams that want to ship quickly and optimize deliberately.

**Stage 1 — standard stores.** Start with `RamMemoryMap` in tests and `FileMemoryMap` or `PostgresMemoryMap` in production. Write all use cases against `IMemory`. Performance is predictable and sufficient for most early workloads.

**Stage 2 — targeted optimization.** When profiling reveals a bottleneck — an aggregate source that full-scans a collection, an entity store on a hot path — replace that specific registration with a specialized implementation. A Postgres-native aggregate source for a slow query; a custom `IEntityStore<T>` backed by Redis for a high-throughput catalog. Everything else stays unchanged.

**Stage 3 — cross-cutting instrumentation.** Because every read goes through `IMemory.Aggregate`, `IMemory.Projection`, and `IVault<T>`, measuring decorators, caching layers, and audit logs can wrap any backend uniformly:

```csharp
public sealed class TimedAggregateSource<T>(IAggregateSource<T> inner, IMetrics metrics) : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From(object query)
    {
        using var _ = metrics.Time($"aggregate.{typeof(T).Name}");
        return inner.From(query);
    }
}
```

The use cases that call `memory.Aggregate<T>(query)` do not know the decorator is there.

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
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();
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
        var author = await memory.Vault<UserRecord>().Load(authorId);
        if (author.IsT1)
            return new NotFound();

        var post = new PostRecord(
            PostId:         Guid.NewGuid(),
            AuthorId:       authorId,
            Content:        content,
            LikeCount:      0,
            LikedByUserIds: ImmutableHashSet<Guid>.Empty,
            CreatedAt:      DateTime.UtcNow);

        var branch = memory.Branch();
        await branch.Save(post);
        await branch.Commit();
        return post;
    }
}
```

### Like a post

```csharp
public sealed class LikePostUseCase(IMemory memory)
{
    public async Task<OneOf<PostRecord, NotFound>> Execute(Guid postId, Guid userId)
    {
        var loaded = await memory.Vault<PostRecord>().Load(postId);
        if (loaded.IsT1)
            return new NotFound();

        var updated = loaded.AsT0 with
        {
            LikeCount      = loaded.AsT0.LikeCount + 1,
            LikedByUserIds = loaded.AsT0.LikedByUserIds.Add(userId)
        };

        var branch = memory.Branch();
        await branch.Save(updated);
        await branch.Commit();
        return updated;
    }
}
```

### Add a comment

```csharp
public sealed class AddCommentUseCase(IMemory memory)
{
    public async Task<OneOf<CommentRecord, NotFound>> Execute(Guid postId, Guid authorId, string text)
    {
        var post = await memory.Vault<PostRecord>().Load(postId);
        if (post.IsT1)
            return new NotFound();

        var comment = new CommentRecord(Guid.NewGuid(), postId, authorId, text, DateTime.UtcNow);
        var branch = memory.Branch();
        await branch.Save(comment);
        await branch.Commit();
        return comment;
    }
}
```

### Read the feed

```csharp
public sealed class GetUserFeedUseCase(IMemory memory)
{
    public IAsyncEnumerable<UserPostSummaryView> Execute(Guid userId, int limit)
        => memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(userId, limit));
}
```

---

## Who this is for

Apia is a good fit for teams that:

- Write automated tests and want them fast — `RamMemoryMap` makes every use case testable without a running database
- Value use case portability — the same business logic runs in a CLI tool, a web API, a background worker, and a test harness
- Prefer explicit error modeling — `OneOf<T, NotFound>` eliminates silent null returns and exception-based control flow
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
dotnet add package Apia.Scoped    # scope-filtered memory
```
