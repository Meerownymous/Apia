# Apia: A Storage Boundary That Keeps Your Use Cases Clean

*How an explicit `IMemory` abstraction lets you ship features on day one and optimize specific bottlenecks later — without touching business logic*

---

## The Problem You Have Already Felt

Write a use case. Something real: posting to a feed, registering a user, liking a comment. Now count how many infrastructure concepts bleed into it.

There is probably a `DbContext`. Maybe an `IQueryable<T>` with a `.Include()` chain that only makes sense against a relational database. Perhaps a connection string somewhere, or a migration that must be applied before the test will run. Clean Architecture says business logic should depend on abstractions, not infrastructure. In practice, `IRepository<T>` almost always leaks: LINQ expression trees are SQL in disguise, navigation properties encode schema decisions, and `SaveChangesAsync` ties you to a unit-of-work model that most domain use cases do not need to know about.

The cost is concrete. You cannot run a use case test without a running database, a Docker container, or a carefully orchestrated mock that drifts from the real implementation the moment you add an index. Swapping a backend — say, from Postgres to a document store — is theoretically possible but practically never done, because the IQueryable expressions that live in your repository layer do not survive the journey.

Apia is one answer to this problem. It is a small .NET library that gives use cases a single interface — `IMemory` — through which all storage goes, without exposing any backend detail whatsoever.

---

## One Interface for All Storage

`IMemory` is deliberately small:

```csharp
public interface IMemory
{
    IEntities<TResult> Entities<TResult>() where TResult : notnull;
    IVault<TResult> Vault<TResult>() where TResult : notnull;
    IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull;
    IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull;
    ITransaction Begin();
}
```

A use case receives `IMemory` as a constructor parameter and composes all its storage operations from it. This is the whole contract. No backend type, no connection string, no session object leaks through.

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

This use case runs identically against an in-memory store, a file-based store, and a Postgres database. It will run identically against any future backend that implements `IMemory`.

The interface offers four access patterns, each covering a different storage shape.

---

## The Three Primitives

### IEntities — a catalog with built-in optimistic concurrency

`IEntities<T>` manages a collection of records each identified by a `Guid`. Three methods do the work:

- `All()` returns `IAsyncEnumerable<T>` — a lazy, cancellable stream of every record.
- `Load(Guid id)` returns `OneOf<T, NotFound>` — no null, no `KeyNotFoundException`. The caller handles absence explicitly.
- `Save(T record)` returns `OneOf<T, Conflict<T>>` — optimistic concurrency baked into the signature itself. There is no separate `Update` method.

The `Conflict<T>` case exposes both `Current` (what is in storage now) and `Attempted` (what the caller tried to save). The caller decides what to do: retry with a fresh load, merge the values, or surface the conflict to the user. Nothing throws. The `LikePostUseCase` shows all three outcomes in one realistic path:

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

### IVault — singletons without ceremony

`IVault<T>` stores a single record — application settings, a feature flag snapshot, global counters. The same `OneOf<T, NotFound>` and `OneOf<T, Conflict<T>>` pattern applies, so the calling code looks identical to entity code. There is no special API for singletons; they simply have no `Guid`.

### IViewStream — computed projections on read

`IViewStream<TResult, TSeed>` takes a *seed* — a typed query descriptor — and streams results:

```csharp
public interface IViewStream<out TResult, in TSeed> where TSeed : notnull
{
    IAsyncEnumerable<TResult> Build(TSeed seed);
}
```

The seed type is the interesting design decision, and it deserves its own section.

---

## Two Minutes to a Working Test

Every backend is registered through an `IMemoryMap` and produces an `IMemory`. Here is the same entity setup for tests and production:

```csharp
// Tests — entirely in-process
var map = new RamMemoryMap();
map.Register(new RamEntities<PostRecord>(p => p.PostId));
map.Register(new RamEntities<UserRecord>(u => u.UserId));
var memory = map.Build();

// Production — swap the map, nothing else changes
var map = new PostgresMemoryMap(connectionString);
map.Register(new PostgresEntities<PostRecord>(p => p.PostId));
map.Register(new PostgresEntities<UserRecord>(u => u.UserId));
var memory = map.Build();
```

`RamMemoryMap` runs entirely in-process. No Docker, no TestContainers, no database setup script. Tests are fast and deterministic by construction — not by careful orchestration. Here is a full projection test that verifies a cross-entity view with users, posts, and comments — and requires nothing beyond the process itself:

```csharp
[Fact]
public async Task BuildsSynopsis()
{
    var map = new RamMemoryMap();
    map.Register(new RamEntities<PostRecord>(p => p.PostId));
    map.Register(new RamEntities<CommentRecord>(c => c.CommentId));
    map.Register(new RamEntities<UserRecord>(u => u.UserId));
    map.Register(new UserFeedSynopsisStream());
    var memory = map.Build();

    UserRecord user1 = new(Guid.NewGuid(), "Miro");
    UserRecord user2 = new(Guid.NewGuid(), "Ralph");
    PostRecord post  = new(Guid.NewGuid(), user1.UserId, "Great Unittest discovered",
                           LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
    CommentRecord comment = new(Guid.NewGuid(), post.PostId, user2.UserId,
                                "My cat's breath smells like cat food", DateTime.Now);

    await memory.Entities<UserRecord>().Save(user1);
    await memory.Entities<UserRecord>().Save(user2);
    await memory.Entities<PostRecord>().Save(post);
    await memory.Entities<CommentRecord>().Save(comment);

    var feed = await memory
        .ViewStream<UserPostSummaryView, UserFeedQuery>()
        .Build(new(user1.UserId, Limit: 20))
        .ToListAsync();
}
```

The test exercises the full stack — entity storage, cross-entity join, projection — without a network call or a container port.

---

## The Central Design Decision: Explicit Query Objects, Not a Universal Query Language

This is where Apia makes its most deliberate trade-off, and the one most worth understanding.

### The tempting alternative

The obvious path for a storage abstraction is a universal query language: `IQueryable<T>`, OData, a custom LINQ provider. These claim a single query API across all backends. The reality is leaky abstractions at scale. A LINQ expression like `posts.OrderByDescending(p => p.CreatedAt).Take(10)` is efficient over an in-memory list. The same expression translated by a Cosmos DB provider can become a full partition scan with client-side sorting — it compiles and it is silent about the cost. Abstract query languages hide where the performance lives.

### What Apia does instead

Each view gets one explicitly designed query object. That object derives from a base record:

```csharp
public abstract record Query<TResult>;
```

The type parameter `TResult` binds the query to its result type at compile time. You cannot accidentally pass a `UserFeedQuery` to a view that returns `InvoiceSummary`. The concrete query for a user feed looks like this:

```csharp
public sealed record UserFeedQuery(Guid UserId, int Limit) : Query<UserPostSummaryView>;
```

This record is not a predicate. It is not a filter tree. It is a specification of business intent: *give me this user's feed, capped at this many items*. The query object carries exactly the parameters the view needs — nothing more.

The memory map stores views keyed by `(typeof(TResult), typeof(TSeed))`. Each query type resolves to exactly one registered synopsis. Simple implementations and optimized implementations are different types; they coexist without any conditional logic in the use cases.

### C# as the query language for simple implementations

The "simple path" in Apia is explicit: for simple views, C# itself is the query language. A `RamSynopsisStream` receives an `IMemory` and a query object, iterates entity collections with `await foreach`, filters in memory, and uses standard LINQ-to-Objects for sorting and limiting:

```csharp
public sealed class UserFeedSynopsisStream()
    : RamSynopsisStream<UserPostSummaryView, UserFeedQuery>(Query)
{
    private static async IAsyncEnumerable<UserPostSummaryView> Query(
        IMemory memory, UserFeedQuery query)
    {
        var posts    = memory.Entities<PostRecord>();
        var comments = memory.Entities<CommentRecord>();
        var users    = memory.Entities<UserRecord>();

        var userPosts = new List<PostRecord>();
        await foreach (var post in posts.All())
        {
            if (post.AuthorId == query.UserId)
                userPosts.Add(post);
        }

        var commentCounts = new Dictionary<Guid, int>();
        await foreach (var comment in comments.All())
        {
            if (userPosts.Any(p => p.PostId == comment.PostId))
                commentCounts[comment.PostId] =
                    commentCounts.GetValueOrDefault(comment.PostId) + 1;
        }

        var userResult = await users.Load(query.UserId);
        if (userResult.IsT1) yield break;
        var author = userResult.AsT0;

        foreach (var post in userPosts.OrderByDescending(p => p.CreatedAt).Take(query.Limit))
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserPostSummaryView(
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

This is readable, debuggable, and testable on day one. There is no SQL to write, no mapping configuration, no schema migration. The feature ships.

The explicit query object is what makes optimization possible later. When this full-scan becomes the bottleneck, the object's shape already tells you exactly what the optimized version needs to do: indexed lookup by `UserId`, ordered by `CreatedAt`, capped by `Limit`. The query object is the contract between the use case and however the view is implemented.

---

## Phase 2: Optimization Without Regression

When a production load test reveals that `UserFeedSynopsisStream` is performing a full scan over a table with 500,000 posts, the response does not touch the use case. It does not change the `UserFeedQuery` record. It does not change `UserPostSummaryView`. It writes a new synopsis implementation.

`ISynopsisStream<TResult, TSeed, TContext>` is the interface all synopsis sources implement:

```csharp
public interface ISynopsisStream<out TResult, in TSeed, in TContext> where TSeed : notnull
{
    // TContext is injected by the memory at query time:
    // Ram     passes  IMemory
    // File    passes  DirectoryInfo
    // Postgres passes (IMemory, IDocumentSession)
    IViewStream<TResult, TSeed> Grow(TContext context);
}
```

The `TContext` type parameter is the only thing that changes across backends. A Postgres-optimized synopsis implements `ISynopsisStream<UserPostSummaryView, UserFeedQuery, (IMemory, IDocumentSession)>` and uses a Marten LINQ query or raw SQL tuned for this exact access pattern. The context injects the Marten session at query time; the synopsis uses it to run a single indexed query instead of iterating all rows.

Swap the registration in the Postgres memory map. The use case that calls `memory.ViewStream<UserPostSummaryView, UserFeedQuery>()` is unchanged. The test suite continues to run against the simple RAM implementation. The optimized path runs only in the environments where you register it.

This composability extends further. Because the `TContext` pattern decouples the synopsis from any single backend, you can build a mixed `IMemory` where different entity types live in different stores. `UserRecord` entities backed by Postgres, `SessionRecord` entities backed by RAM — ephemeral by design. A single `IMemory` instance can compose backends at the registration level, and the use cases are none the wiser.

---

## Transactions

`IMemory.Begin()` returns an `ITransaction` that is also an `IAsyncDisposable`. The pattern is direct:

```csharp
await using var tx = memory.Begin();
var txMemory = tx.Memory();

await new DeductCreditsUseCase(txMemory).Execute(userId, amount);
await new RecordPurchaseUseCase(txMemory).Execute(userId, itemId);

await tx.Commit(); // both operations persist atomically, or neither does
```

`txMemory` is an `IMemory`. The use cases that receive it do not know they are inside a transaction; they call the same `Entities<T>().Save()` and `Load()` as always. Disposing without calling `Commit()` rolls back. The transaction boundary is expressed in the composition layer, not inside the use cases.

---

## Cross-Cutting Concerns: The Decorator Pattern

Because every storage interaction flows through three narrow interfaces — `IEntities<T>`, `IVault<T>`, `IViewStream<T, TSeed>` — decorators are uniformly applicable. Timing, caching, audit logging, and circuit-breaking wrap any backend without modifying a single use case:

```csharp
public sealed class TimedEntities<T>(IEntities<T> inner, IMetrics metrics) : IEntities<T>
{
    public async Task<OneOf<T, Conflict<T>>> Save(T record)
    {
        using var _ = metrics.Time($"entities.{typeof(T).Name}.save");
        return await inner.Save(record);
    }
    // ... All(), Load(), Delete() follow the same pattern
}
```

Registered in the map:

```csharp
map.Register(new TimedEntities<PostRecord>(postgresEntities, metrics));
```

The use cases that call `memory.Entities<PostRecord>()` do not know the decorator is there. Measurements are added to one registration; they appear on every call through that store. The same pattern applies to distributed caches over hot paths, read-through caches in front of slow projections, or audit decorators around vaults that store sensitive configuration.

---

## Honest Trade-offs

Apia is not a query engine. The simple path — iterating `All()` and filtering in C# — is appropriate for early workloads and testing. It is not appropriate for complex cross-entity aggregations on tables with millions of rows where the simple path is never fast enough. If your domain is relational from day one and requires sophisticated ad-hoc queries, the phase-1 path may always underperform.

The Postgres backend uses Marten, which stores records as JSONB documents. This is a deliberate choice that enables schema-free iteration. Teams that need hand-crafted tables, composite primary keys, or fine-grained index strategies will find the Postgres backend constraining.

If your codebase already has a deep Entity Framework Core layer with a significant migration history and a well-established repository pattern, the cost of replacing it may substantially outweigh the benefit. Apia is most at home in greenfield applications or in bounded contexts where clean seams already exist.

Finally, the optimistic concurrency model requires callers to handle `Conflict<T>`. Teams accustomed to last-write-wins semantics — where `SaveChangesAsync` simply overwrites — need to update their mental model and write explicit conflict resolution paths. This is deliberate: conflicts that are silently lost are bugs deferred, not bugs prevented. But it is friction, and it should be acknowledged as such.

---

## Conclusion: Ship, Measure, Optimize

The pattern Apia supports has three phases.

**Phase 1:** Write use cases against `IMemory`. Express views as explicit query objects in C#. Use `RamMemoryMap` in tests; use `FileMemoryMap` or `PostgresMemoryMap` in production with the simple synopsis implementations. The feature ships with predictable performance and a fast, reliable test suite.

**Phase 2:** Profile the production system. Find the bottleneck — a specific view, a specific entity store on a hot path. Write one optimized synopsis or one specialized `IEntities<T>` implementation for that specific registration. Swap it in. The use case does not change.

**Phase 3:** Add decorators at the map level for timing, caching, and observability. The use cases remain unchanged.

The linchpin of this design is the explicit query object. It is the reason phase 2 is possible. A raw predicate or a universal query language obscures what a view needs. An explicit `UserFeedQuery(Guid UserId, int Limit)` says exactly what the business requires, serves as the contract between the use case and any implementation, and allows the simple and the optimized to coexist without conflict.

The three narrow interfaces — `IEntities`, `IVault`, `IViewStream` — are what make decorators, tests, and backend composition trivially composable. Keep them narrow, keep the use cases ignorant of backends, and the architecture remains flexible at the points where flexibility has real value.

```
dotnet add package Apia
dotnet add package Apia.Ram
dotnet add package Apia.File
dotnet add package Apia.Postgres
```

Examples are in the test suite under `tests/Apia.Tests/Examples/`.
