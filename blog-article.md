# Apia: A Storage Boundary That Keeps Your Use Cases Clean

*How a single `IMemory` abstraction lets a use case ignore where its entities live — and lets the
composition answer that question later, per query, per backend.*

---

## The Problem You Have Already Felt

Write a use case. Something real: posting to a feed, registering a user, liking a comment. Now count
how many infrastructure concepts bleed into it.

There is probably a `DbContext`. Maybe an `IQueryable<T>` with an `.Include()` chain that only makes
sense against a relational database. Perhaps a connection string somewhere, or a migration that must be
applied before the test will run. Clean Architecture says business logic should depend on abstractions.
In practice `IRepository<T>` leaks: LINQ expression trees are SQL in disguise, navigation properties
encode schema decisions, and `SaveChangesAsync` ties the caller to a unit-of-work model most domain
code has no opinion about.

The cost is concrete. A use case test needs a running database, a container, or a mock that drifts from
the real implementation the moment an index appears. Swapping a backend is theoretically possible and
practically never done, because the expressions living in the repository layer do not survive the
journey.

Apia answers this with one container. A use case is written against `IMemory`, and nothing about the
storage technique reaches it.

---

## One Interface for All Storage

`IMemory` has four members:

```csharp
public interface IMemory
{
    IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query);
    Task<T>             Projection<T>(IProjectionQuery<T> query);
    IVault<T>           Vault<T>();
    IBranch             Branch();
}
```

`Vault<T>()` reads the entities of one type. `Aggregate` and `Projection` ask a query — many results,
or exactly one computed result. `Branch()` opens a unit of work. There is no session object, no
connection, no backend type in the signature, and nothing here is registered anywhere.

A use case takes the memory and composes everything from it:

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
```

This runs unchanged in process, on disk and against Postgres.

---

## Reading: The Vault

```csharp
public interface IVault<T>
{
    Task<OneOf<T, NotFound>> Entity(Guid id);
    IAsyncEnumerable<T>      All();
    IAsyncEnumerable<T>      Matching(Expression<Func<T, bool>> condition);
}
```

`Entity` returns `OneOf<T, NotFound>`. There is no `null` in the library and no
`KeyNotFoundException`; the absent case is a value the caller unpacks with `Match`, which is how the
compiler makes sure it was considered:

```csharp
var greeting = (await memory.Vault<User>().Entity(id)).Match(
    user => $"Hello, {user.Username}",
    _    => "Nobody here by that id");
```

`All` streams a type. `Matching` takes a condition and is the single channel through which a backend
pushes filtering into its own query language: the RAM store evaluates it in process, the file store
over what it has read, the Postgres store hands it to Marten. A caller that writes
`Matching(post => post.LikeCount > 3)` gets the same answer everywhere and gets it from the database
where there is one.

---

## The Central Design Decision: A Query Carries Its Own Implementation

The tempting path for a storage abstraction is a universal query language — `IQueryable<T>`, OData, a
custom LINQ provider — one query API over every backend. These leak at scale. The expression
`posts.OrderByDescending(p => p.CreatedAt).Take(10)` is cheap over a list in memory. Translated by a
document database provider it can become a full partition scan with client-side sorting. It compiles,
it runs, and it says nothing about what it costs.

Apia takes the opposite position. A query is an object that states what is asked and answers it:

```csharp
public interface IAggregateQuery<T>  { IAsyncEnumerable<T> Results(IMemory memory); }
public interface IProjectionQuery<T> { Task<T> Result(IMemory memory); }
```

The implementation is written in C#, against the memory it is handed, using the vault:

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
```

Asking it is one call:

```csharp
await foreach (var summary in memory.Aggregate(new UserFeed(userId, 20)))
    Console.WriteLine(summary.Content);
```

Three properties follow from the shape.

**The result type is bound at compile time.** `IMemory.Aggregate<T>` takes its `T` from the query, so a
query paired with the wrong result type is a build error.

**Nothing is registered.** An earlier version of this library kept a registry mapping a query type to
an implementation, and every forgotten registration was a failure discovered at runtime. The registry
is gone. A query that no backend has an opinion about answers itself, which is the default path and the
one every query takes until a project has a reason for something else. The decision and what it cost
are written down in [ADR-0002](docs/adr/0002-queries-carry-their-own-implementation.md).

**A query reads through whatever memory it is handed.** This is what makes a branch's staged changes
and a scope's rules reach inside a query's own implementation, without the query knowing either exists.

---

## Composing a Memory

A memory is finished when its constructor returns. Its whole configuration is the identities of the
stored types — where each entity keeps its id — which are backend-neutral:

```csharp
public sealed class PostId : IIdentity<Post>
{
    public Guid Of(Post entity) => entity.PostId;
}

var identities = new Identities().With(new UserId()).With(new PostId()).With(new CommentId());

var prototype  = new RamMemory(identities, new Overrides());
var onDisk     = new FileMemory("/var/lib/myapp", identities, new Overrides());
var inPostgres = new PostgresMemory(documentStore, identities, new Overrides());
```

The same identities compose all three. Starting a project on `RamMemory` costs nothing, and moving it
to disk or to Postgres is an edit in the composition. No use case changes, no query changes, no test
changes.

There is no `Build()` step to forget, because there is nothing to resolve at build time any more — the
two-phase construction existed to break a circular dependency that ADR-0002 removed. That history is in
[ADR-0003](docs/adr/0003-memory-is-constructed-not-built.md).

---

## Optimizing One Query: Backend Overrides

`UserFeed` above reads every post and every comment and filters in process. For a prototype and for
most workloads, that is the right amount of machinery. When production says otherwise, the composition
supplies an override for that one query:

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

The use case still calls `memory.Aggregate(new UserFeed(userId, 20))`. It never names the override and
cannot tell which implementation answered. The tests keep running against `RamMemory`, where the query
answers itself.

An override is handed the query object, which is how it learns what was asked. That has a consequence
for how a query is written: the values it asks with are readable by anyone holding it — the `record`
above — because a value in a private field is reachable only by the query's own implementation. This is
the one place where the project's design rules allow a record to carry behaviour, and the reason is
exactly this.

The constraint tying a query to its result type lives on the `With` method, so supplying an override
for the wrong result type is a build error. Supplying none is not an error at all.

One thing an override does not do is respect a scope. It reads past the vault, and therefore past every
rule a scope states. That is accepted and written down in
[ADR-0001](docs/adr/0001-backend-overrides-bypass-scopes.md): supplying an override for a query that
touches scope-protected entities is a security decision as much as a performance decision.

---

## Branches: The Unit of Work

```csharp
public interface IBranch
{
    IMemory Memory();
    Task Save<T>(T entity);
    Task Delete<T>(Guid id);
    Task<OneOf<Committed, Stale>> Commit();
}
```

`Memory()` is what makes a branch usable from inside a use case. It hands back a memory whose vaults
answer from what the branch staged, layered over what is committed. Writing and then reading inside one
unit of work returns what was written, and any query already written runs inside the branch with no
change:

```csharp
var branch = memory.Branch();
await branch.Save(post with { LikeCount = post.LikeCount + 1 });

// reads the like it just staged
await foreach (var summary in branch.Memory().Aggregate(new UserFeed(userId, 20))) { }

await branch.Commit();
```

Nothing reaches a store until `Commit`.

### When someone else got there first

`Commit` returns `OneOf<Committed, Stale>`. A branch that read an entity by id, and committed after
another branch had written that entity, is told so:

```csharp
(await branch.Commit()).Match(
    committed => Log("saved"),
    stale     => Log(
        "someone else got there first: "
        + string.Join(", ", stale.Changes.Select(changed => $"{changed.EntityType.Name} {changed.Id}"))));
```

`Stale` carries a `Changed` for every read that went stale, each naming the entity type and the id. A
branch that read a user and a post hears about both. Nothing is written when a commit reports `Stale`:
the entities keep the values the other branch gave them, and the caller decides whether to re-read,
merge, or tell a person.

Reading by id is what a commit compares. Streaming a type through `All` or `Matching` notes nothing, so
walking a type does not turn every entity in it into a reason for a stale commit.

This is optimistic concurrency and it is not a lock. Two commits running at the very same instant can
both pass the version comparison, and the later write wins.

### What a commit guarantees, per backend

Every staged change is resolved to the id it will be written under before any store is touched, so an
entity no identity was given for costs a commit nothing rather than half of it. Beyond that, the medium
decides:

| Backend | What a commit guarantees |
|---|---|
| `Apia.Ram` | All or nothing. Applying resolved changes in process cannot fail |
| `Apia.File` | Each entity type's file is written once and replaced by a rename, so an interrupted write cannot empty or half-write a type. Atomicity **across** entity types is not reachable on this medium and is not attempted |
| `Apia.Postgres` | All or nothing. A commit is one Marten transaction across every type it touches |

The file backend's limit is real and is written down. A commit touching two types, failing while
writing the second, leaves the first written. An application that needs cross-type atomicity on that
medium needs a different medium.

---

## Scopes: What This Caller May See

A scope decides what is visible, writable and deletable for one filter value — the id of the signed-in
user, say:

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

Every scope states all three rules. There are no default interface implementations in this library, so
"anything you can see, you can change" is a sentence someone wrote rather than a default nobody read.

`ScopeMemory` is an `IMemory`, so a use case takes it without knowing. An id outside the scope reads as
`NotFound`, indistinguishable from a genuine miss. A query's own implementation reads through the
vault, so the scope holds inside it. A branch taken from a scoped memory throws
`UnauthorizedAccessException` when a save or a removal is staged that the scope does not permit, and it
checks a removal against the entity as the store holds it, because an entity the scope hides is exactly
the one a removal must not reach.

`Condition` is the optional half. Where a scope can state its rule as an expression, the backend pushes
it into its own query language rather than fetching everything and discarding most of it. Where it
cannot, the scope returns `None` and filtering happens in process.

The honest limit, again: an override reads past all of this.

---

## Testing

Every use case can be exercised against `RamMemory`. No mocks, no containers, no network:

```csharp
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

The library holds itself to the same standard. One contract suite runs through `IMemory` against all
three backends: the same assertions, once per storage technique. A backend that does not actually write
cannot be green. An earlier Postgres write path staged changes the session never heard of, and a suite
that only ran in memory had nothing to say about it. A backend the machine cannot reach is reported as
skipped by name, so a green local run is never mistaken for full coverage, and CI runs the suite
against a real Postgres.

The suite asks the same query with an override and without one and compares the two answers, which is
what keeps an optimization from quietly changing a result.

---

## Honest Trade-offs

Apia is not a query engine. A query's own implementation streams entities and filters them in C#. That
is appropriate for early workloads, for tests, and for most of what an application asks. It is not
appropriate for a cross-entity aggregation over millions of rows, which is what overrides are for — and
an override is code someone writes for one backend.

The Postgres backend stores entities as JSONB documents through Marten. Schema-free iteration comes
with it, and so does the constraint: hand-crafted tables, composite keys and fine-grained index
strategies are not what this backend offers. Marten also decides for itself which member carries an
entity's id, so the document store handed to `PostgresMemory` has to be configured to agree with the
identities. Apia does not configure Marten.

Optimistic concurrency means callers handle `Stale`. Teams used to last-write-wins have a mental model
to update and code to write. The library takes the position that a silently lost write is a bug
deferred, and it is friction either way.

`ScopeMemory` states a rule and stops short of being a complete boundary. An override reads past it.

A codebase with a deep EF Core layer, a migration history and an established repository pattern has a
lot to replace for what it would gain. Apia is at home in greenfield applications and in bounded
contexts with clean seams already in place.

There is no published package. The projects are consumed from source.

---

## Where This Leaves You

Write use cases against `IMemory`. Write queries as objects that answer themselves. Start on
`RamMemory`, move to `Apia.File` or `Apia.Postgres` when the application needs it, and supply an
override for the one query that profiling actually names. The use cases do not change at any point in
that sequence, because none of them ever knew where an entity was kept.

The vocabulary — memory, vault, store, branch, commit, stale, aggregate, projection, override, scope,
filter — is in [CONTEXT.md](CONTEXT.md). Every example in this article has a counterpart in the test
project under `tests/Apia.Tests/`, where the contract suite in `Contract/MemoryTests.cs` asks each of
these promises on every backend.
