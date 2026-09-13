# Apia

A storage abstraction for .NET 9. A use case is written against one container — `IMemory` — and neither
knows nor cares which backend holds the entities. Swapping the storage technique, and faking it wholesale
in tests, is a change of composition and nothing else.

---

## The read surface

```csharp
public interface IMemory
{
    IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query);
    Task<T>             Projection<T>(IProjectionQuery<T> query);
    IVault<T>           Vault<T>();
    IBranch             Branch();
}

public interface IVault<T>
{
    Task<OneOf<T, NotFound>> Entity(Guid id);
    IAsyncEnumerable<T>      All();
    IAsyncEnumerable<T>      Matching(Expression<Func<T, bool>> condition);
}
```

`Entity` returns `OneOf<T, NotFound>`: no `null`, no `KeyNotFoundException`. Both outcomes are unpacked
by `Match`, so the absent case is answered where the read happens:

```csharp
var greeting = (await memory.Vault<User>().Entity(id)).Match(
    user => $"Hello, {user.Username}",
    _    => "Nobody here by that id");
```

`Matching` is the single channel through which a backend pushes filtering into its own query language.

---

## Composing a memory

A memory is finished the moment it is constructed. There is no build step to forget. Its whole
configuration is the identities of the stored types, which are backend-neutral and reused everywhere:

```csharp
var identities = new Identities().With(new UserId()).With(new PostId()).With(new CommentId());

var prototype  = new RamMemory(identities, new Overrides());
var onDisk     = new FileMemory("/var/lib/myapp", identities, new Overrides());
var inPostgres = new PostgresMemory(documentStore, identities, new Overrides());
```

An identity says where an entity keeps its id:

```csharp
public sealed class PostId : IIdentity<Post>
{
    public Guid Of(Post entity) => entity.PostId;
}
```

Starting on `RamMemory` costs nothing, and moving to disk or to SQL touches the composition and no use
case.

---

## Queries

A query states the type it returns and carries its own implementation, reading through whatever memory
it is handed. Nothing is registered, so forgetting a registration is not a class of bug that exists,
and the compiler rejects a query paired with the wrong result type.

```csharp
public interface IAggregateQuery<T>  { IAsyncEnumerable<T> Results(IMemory memory); }
public interface IProjectionQuery<T> { Task<T> Result(IMemory memory); }
```

An aggregate returns many results; a projection returns exactly one computed result.

```csharp
public sealed record UserFeed(Guid UserId, int Limit) : IAggregateQuery<UserPostSummary>
{
    public async IAsyncEnumerable<UserPostSummary> Results(IMemory memory)
    {
        await foreach (var summary in
            (await memory.Vault<User>().Entity(UserId)).Match(
                author => Summaries(author, memory),
                _      => AsyncEnumerable.Empty<UserPostSummary>()))
            yield return summary;
    }

    private async IAsyncEnumerable<UserPostSummary> Summaries(User author, IMemory memory)
    {
        var posts = await memory.Vault<Post>().All().Where(post => post.AuthorId == UserId).ToListAsync();
        var comments = await memory.Vault<Comment>().All().ToListAsync();
        foreach (var post in posts.OrderByDescending(post => post.CreatedAt).Take(Limit))
            yield return new UserPostSummary(
                PostId: post.PostId,
                AuthorName: author.Username,
                Content: post.Content,
                LikeCount: post.LikeCount,
                CommentCount: comments.Count(comment => comment.PostId == post.PostId),
                CreatedAt: post.CreatedAt);
    }
}

await foreach (var summary in memory.Aggregate(new UserFeed(userId, 20)))
    Console.WriteLine(summary.Content);
```

An unknown author yields no entries, which is what `Match` states at the point of the read. The query
above is the one the contract suite asks on every backend, in
[`tests/Apia.Tests/Query/UserFeed.cs`](tests/Apia.Tests/Query/UserFeed.cs).

The same query text runs unchanged on Ram, on File and on Postgres.

---

## Backend overrides

When a backend can answer a query better than the generic path, the composition may supply an override.
It is chosen where the application is wired together, never in a use case, and a use case never sees its
type. A missing override is the normal case and falls through to the query's own implementation.

```csharp
public sealed class PostgresUserFeed(IDocumentStore store) : IAggregateOverride<UserFeed, UserPostSummary>
{
    // one statement, joined and grouped in the database, for the user and the limit the query names
    public IAsyncEnumerable<UserPostSummary> Results(UserFeed query, IMemory memory) => ...;
}

var memory = new PostgresMemory(
    documentStore,
    identities,
    new Overrides().With(new PostgresUserFeed(documentStore)));
```

An override is handed the query it answers, which is how it learns what was asked. A query that carries
values therefore states them as values anyone holding it can read — the record above — because a value
kept in a private field is reachable only by the query's own implementation.

The constraint tying a query to the type it returns lives on `With`, so a wiring mistake is a build
error rather than a production one. Asking a query no override was supplied for is not a failure: it
runs its own implementation, which is what every query does until a project has a reason to do
otherwise.

An override reads past the vault, and therefore past any scope. That is accepted and written down in
[ADR-0001](docs/adr/0001-backend-overrides-bypass-scopes.md): registering one for a query that touches
scope-protected entities is a security decision, not only a performance decision.

---

## Branches

A branch is a unit of work, and it is a memory you can read.

```csharp
public interface IBranch
{
    IMemory Memory();
    Task Save<T>(T entity);
    Task Delete<T>(Guid id);
    Task<OneOf<Committed, Stale>> Commit();
}
```

`Memory()` hands back a memory whose vaults answer from what the branch staged, layered over what is
committed — so writing and then reading inside one unit of work returns what was just written, and any
query you already have runs inside the branch unchanged.

```csharp
var branch = memory.Branch();
await branch.Save(post with { LikeCount = post.LikeCount + 1 });

// reads the like it just staged
await foreach (var summary in branch.Memory().Aggregate(new UserFeed(userId, 20))) { }

await branch.Commit();
```

### Stale commits

A commit reports `Stale` when an entity the branch read by id changed underneath it since the read.
The outcome is in the return type rather than thrown, so handling it is something the compiler reminds
you about:

```csharp
(await branch.Commit()).Match(
    committed => Log("saved"),
    stale     => Log(
        "someone else got there first: "
        + string.Join(", ", stale.Changes.Select(changed => $"{changed.EntityType.Name} {changed.Id}"))));
```

`Stale` carries a `Changed` per read that went stale, each naming the entity type and the id, so a
branch that read a user and a post is told about both rather than about the first one noticed.

Nothing is written when a commit reports `Stale`: the entity keeps the value the other branch gave it.

A read by id is what a commit compares. Streaming a type through `All` or `Matching` notes nothing, so
a branch that walked a type commits even when an entity it streamed changed meanwhile.

A commit compares the versions it read against the versions the stores hold and then writes, which
catches a branch that read before another branch committed. It is not a lock: two commits running at
the very same instant can both pass the comparison, and the later write wins.

### Commit atomicity, per backend

Every staged change is resolved to the id it will be written under before any store is touched, so an
entity that cannot be identified costs a commit nothing rather than half of it. Beyond that, the medium
decides:

| Backend | What a commit guarantees |
|---|---|
| `Apia.Ram` | All or nothing. Applying resolved changes in process cannot fail |
| `Apia.File` | Each entity type's file is written once and replaced by a rename, so an interrupted write cannot empty or half-write a type. Atomicity **across** entity types is not reachable on this medium and is not attempted |
| `Apia.Postgres` | All or nothing. A commit is one Marten transaction across every type it touches |

Each row is held to its word by a test: the contract suite asserts on every backend that a commit which
throws part-way leaves nothing of it stored, `FileWriteTests` that a failed write leaves the earlier
entity where it was, and `PostgresWriteTests` that a commit reaches the session once and saves it once.

---

## Scopes

A scope decides what is visible, writable and deletable for one filter value — the id of the signed-in
user, say. Every scope states all three rules: "anything you can see, you can change" is a decision
someone made rather than a default nobody read.

```csharp
public sealed class AuthorScope : IScope<Post, Guid>
{
    public bool Includes(Post entity, Guid authorId)  => entity.AuthorId == authorId;
    public bool CanWrite(Post entity, Guid authorId)  => entity.AuthorId == authorId;
    public bool CanDelete(Post entity, Guid authorId) => entity.AuthorId == authorId;

    public OneOf<Expression<Func<Post, bool>>, None> Condition(Guid authorId)
        => (Expression<Func<Post, bool>>)(post => post.AuthorId == authorId);
}

var scoped = new ScopeMemory<Guid>(
    memory, new Overrides(), new Scopes<Guid>().With(new AuthorScope()), signedInUserId);
```

A query's own implementation reads through the vault, so the scope holds inside it. `Condition` lets a
backend push the rule into its own query language instead of fetching everything and discarding most of
it. An id outside the scope reads as `NotFound`, indistinguishable from a genuine miss.

A branch taken from a scoped memory refuses what the scope does not permit: a save of an entity
`CanWrite` denies, and a removal of an entity `CanDelete` denies, each throwing
`UnauthorizedAccessException` at the moment it is staged rather than at the commit. A removal is checked
against the entity as the store holds it, so an entity the scope hides is exactly the one a removal
cannot reach.

`ScopeMemory` is not a complete boundary: a backend override reads past it. See
[ADR-0001](docs/adr/0001-backend-overrides-bypass-scopes.md).

---

## Backends

| Backend | When to use |
|---|---|
| `Apia.Ram` | Tests, prototypes, single-process state |
| `Apia.File` | Small apps, CLIs, dev environments, offline tools |
| `Apia.Postgres` | Production, multi-instance deployments, relational queries |

Marten decides for itself which member of an entity carries its id, so a document store handed to
`PostgresMemory` must be configured to agree with the identities. Apia does not configure Marten.

---

## Testing

Every use case can be exercised against `RamMemory`: no mocks, no containers, no network. A use case
takes the memory and hands the commit's outcome on, so its caller answers the stale case too:

```csharp
public sealed class CreatePost(IMemory memory)
{
    public async Task<OneOf<Committed, Stale>> Execute(Guid authorId, string content)
    {
        var branch = memory.Branch();
        await branch.Save(new Post(Guid.NewGuid(), authorId, content, LikeCount: 0, DateTime.UtcNow));
        return await branch.Commit();
    }
}

[Fact]
public async Task PostAppearsInFeed()
{
    var memory = new RamMemory(
        new Identities().With(new UserId()).With(new PostId()).With(new CommentId()),
        new Overrides());
    var user = new User(Guid.NewGuid(), "alice");
    var branch = memory.Branch();
    await branch.Save(user);
    await branch.Commit();

    await new CreatePost(memory).Execute(user.UserId, "Hello, world");

    Assert.Single(await memory.Aggregate(new UserFeed(user.UserId, 10)).ToListAsync());
}
```

The library's own suite works the same way: one contract suite runs through the memory against every
backend, so a backend that does not actually write cannot be green. A backend this machine cannot reach
is reported as skipped by name, so a green local run cannot be mistaken for full coverage. Set
`APIA_POSTGRES_CONNECTION` to include Postgres; CI sets it against a Postgres service.

Every promise this readme makes is one the suite keeps. The reads, the queries, the overrides, the
branch overlay, the stale outcome and what a scope makes visible are asked on Ram, on File and on
Postgres in [`tests/Apia.Tests/Contract/MemoryTests.cs`](tests/Apia.Tests/Contract/MemoryTests.cs); what
a scope refuses to write or to remove is held in
[`tests/Apia.Tests/Scope/ScopeTests.cs`](tests/Apia.Tests/Scope/ScopeTests.cs).

---

## Vocabulary

The word for the thing that is stored is **entity**. The full glossary — memory, vault, store, branch,
commit, stale, aggregate, projection, override, scope, filter — lives in [CONTEXT.md](CONTEXT.md).

---

## Installation

The projects are consumed from source. No package is published.

---

## License

MIT. See [LICENSE](LICENSE).
