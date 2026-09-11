# Rebuild plan

Settled in the design session of 2026-09-11. Nothing here has been implemented yet.

The starting point: `Apia.sln` does not compile. One error, `CS1961` in `src/Apia/IAggregateSource.cs:12`
(`in TQuery` is contravariant where it must be invariant), in the core project, so nothing downstream
builds. With that one annotation removed the solution builds clean with no CS warnings and all 18 tests
pass. Two of three backends have no tests at all, and the Postgres write path is a silent no-op.

## Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Breaking changes | Free. No consumers, no published package |
| 2 | Untyped `object query` | A query carries its result type and its own storage-agnostic implementation. Backend overrides are optional and registered where the memory is composed. See ADR-0002 |
| 3 | Scope versus override | Orthogonal. An override reads past every scope and its author is responsible. See ADR-0001 and issue #12 |
| 4 | Reading inside a branch | `IBranch.Memory()` returns a memory whose vaults layer staged changes over committed state |
| 5 | Commit atomicity | Delivered per backend as far as the medium allows, with the limits written down |
| 6 | Concurrent branches | Optimistic. Stores carry versions, `Commit` returns `OneOf<Committed, Stale>`, `Conflict<T>` is replaced |
| 7 | Behaviourless value carriers | Allowed as `record` / `record struct`. Rule amended |
| 8 | Verb-named accessors | `Build` → `Memory`, `Load`/`Get` → `Entity`, `AsLinq` → `Condition`. Bool methods state a fact and stay verbs |
| 9 | The stored thing | **Entity**. `record` leaves the vocabulary, `PostRecord` becomes `Post`. See CONTEXT.md |
| 10 | Default interface implementations | Forbidden. Every scope states its own write and delete policy |
| 11 | The memory map | Gone. A memory is constructed from `Identities` and `Overrides`, both fluent through extension methods over decorators. See ADR-0003 |
| 12 | Tonga | Style only, no dependency. The async gap is tracked in Meerownymous/Tonga#52 |
| 13 | Backend coverage | One contract suite over all three backends, Postgres in CI, a faked `IDocumentSession` as the immediate guard |
| 14 | Dead code | Three scope mechanisms exist and one is alive. The other two go, along with `Conflict.cs`. The blog article is marked stale now and rewritten after the rebuild |
| 15 | Landing | Four pull requests, below |

## Target shape

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
    IAsyncEnumerable<T>      Matching(Expression<Func<T, bool>> predicate);
}

public interface IBranch
{
    IMemory Memory();
    Task Save<T>(T entity);
    Task Delete<T>(Guid id);
    Task<OneOf<Committed, Stale>> Commit();
}

public interface IAggregateQuery<T>  { IAsyncEnumerable<T> Results(IMemory memory); }
public interface IProjectionQuery<T> { Task<T> Result(IMemory memory); }
```

Composition, the same identities across every backend:

```csharp
var identities = new Identities().With(new UserId()).With(new PostId()).With(new CommentId());

var ram = new RamMemory(identities, new RamOverrides());
var pg  = new PostgresMemory(session, identities, new PostgresOverrides().With(new PostgresUserFeed()));
```

## Pull requests

### 1. Heal and clear out

Pure subtraction. Takes no design decision and unblocks everything else.

- Remove `in TQuery` from `IAggregateSource<out T, in TQuery>`, so the solution builds
- Replace the truncate-then-write in `FileEntityStore.WriteUnsafe` with a temp file and `File.Move`.
  `FileMode.Create` empties the file before serialising, so any exception loses the whole type's data
- Delete `src/Apia.Scoped/` (never compiled, references types that do not exist)
- Delete the policy mechanism: `IAccessPolicy`, `AccessPolicy`, `IPolicies`, `Policies`,
  `PolicyMemory`, `PolicyEnforcedBranch`, `PolicyEnforcedVault`. 218 lines, zero callers
- Delete `Conflict.cs`, zero callers
- Remove the dead `<see cref>` tags and switch `GenerateDocumentationFile` on so they cannot come back
- Drop the `Apia.Scoped` line from README, add a staleness note to `blog-article.md`

Exit: solution builds, 18 tests still green, roughly 280 dead lines gone.

### 2. Core and Ram

- The interfaces above, `Identities` / `Overrides` as decorators with fluent extension methods
- Staged overlay vault so a branch reads its own writes
- Versions in `RamEntityStore`, `Stale` on commit, snapshot and restore for rollback
- `IScope`: `AsLinq` → `Condition`, default implementations removed
- Rename `PostRecord`/`UserRecord`/`CommentRecord` to `Post`/`User`/`Comment`
- The contract suite starts here, as a `[Theory]` over a `ClassData` backend provider

### 3. File

Shrinks to `FileEntityStore` plus composition. One write per type per commit, rename atomicity per
type, and a written note that atomicity across types is not reachable on this medium. Joins the
contract suite.

### 4. Postgres

Rewritten against the Marten session. `PostgresBranch.Save` and `Delete` currently build a
`Task<Action>` whose lambda is never invoked, so nothing is ever written. A faked `IDocumentSession`
guards that class of bug without Docker; the contract entry runs in CI.
