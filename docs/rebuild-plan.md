# Rebuild plan

Settled in the design session of 2026-09-11 and landed in full. This is kept as the record of
what was decided and why; the shapes below are the shapes in the source tree, and the decisions
themselves live on in CONTEXT.md, docs/adr/ and claude.md.

The starting point was a solution that did not compile: one `CS1961` in `src/Apia/IAggregateSource.cs`,
in the core project, so nothing downstream built. Two of three backends had no tests at all, and the
Postgres write path was a silent no-op.

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

var ram = new RamMemory(identities, new Overrides());
var pg  = new PostgresMemory(documentStore, identities, new Overrides().With(new PostgresUserFeed(documentStore)));
```

## Landed

All four steps below are in the source tree.

### 1. Heal and clear out — landed

Pure subtraction: the variance error removed so the solution builds, the truncate-then-write in
`FileEntityStore` replaced by a pending file and a rename, `src/Apia.Scoped/` and the parallel policy
mechanism and `Conflict.cs` deleted, dead `<see cref>` tags removed and documentation generation
switched on so they cannot come back, the readme corrected and the blog article marked stale.

### 2. Core and Ram — landed

`IAggregateQuery<T>` / `IProjectionQuery<T>`, `IIdentities` and `IOverrides` as decorators with fluent
extension methods, `StagedVault` so a branch reads its own writes, versions in every store, `Stale` on
commit, `IScope` with `Condition` and no default implementations — with one deviation from the plan
above: rollback on Ram is delivered by resolving every staged change to its id before the first store
is written, rather than by snapshotting and restoring. Applying already-resolved changes in process
cannot fail, so the snapshot had nothing left to protect, the entity vocabulary, and the
contract suite as a `[SkippableTheory]` over a `ClassData` backend provider.

### 3. File — landed

Shrunk to `FileEntityStore` plus `FileStores` and `FileMemory`. One write per type per commit, rename
atomicity per type, and the note that atomicity across types is not reachable on this medium written
into `FileMemory`'s own documentation. Joined the contract suite.

### 4. Postgres — landed

Rewritten against the Marten session: `PostgresEntityStore` writes into the session and
`PostgresBranch` saves it, so a commit is one transaction. A substituted `IDocumentSession` guards the
class of bug that shipped — a call built into a value and never invoked — without Docker; the contract
entry runs wherever `APIA_POSTGRES_CONNECTION` is set, and is reported as skipped by name where it is
not.
