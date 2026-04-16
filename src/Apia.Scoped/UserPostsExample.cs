// =============================================================================
// THIS FILE IS A DESIGN EXAMPLE — not production code.
// It shows how IEntitiesScope is used with RAM and Postgres backends.
// =============================================================================

#if EXAMPLE

using System.Linq.Expressions;
using Apia.Ram;
using Apia.Postgres;
using Marten;

namespace Apia.Scope.Example;

// -----------------------------------------------------------------------------
// Domain
// -----------------------------------------------------------------------------

public record Post(Guid Id, Guid AuthorId, string Title);

// TFilter can be a plain Guid or a richer object — shown here with roles:
public record UserContext(Guid Id, bool IsAdmin = false);

// -----------------------------------------------------------------------------
// THE SCOPE OBJECT — written once, works with every backend
// -----------------------------------------------------------------------------

// Simple ownership (TFilter = Guid):
// public sealed class UserPosts : ILinqEntitiesScope<Post, Guid>
// {
//     public bool Includes(Post post, Guid userId) => post.AuthorId == userId;
//     public Expression<Func<Post, bool>> Filter(Guid userId) => p => p.AuthorId == userId;
// }

// With roles (TFilter = UserContext):
public sealed class UserPosts : ILinqEntitiesScope<Post, UserContext>
{
    // RAM / File: in-process predicate
    public bool Includes(Post post, UserContext ctx)
        => ctx.IsAdmin || post.AuthorId == ctx.Id;

    public bool CanWrite(Post post, UserContext ctx)
        => post.AuthorId == ctx.Id;                  // only owner may write

    public bool CanDelete(Post post, UserContext ctx)
        => post.AuthorId == ctx.Id || ctx.IsAdmin;   // owner or admin may delete

    // Postgres: LINQ expression → SQL WHERE (no full-table scan)
    public Expression<Func<Post, bool>> Filter(UserContext ctx)
        => ctx.IsAdmin
            ? _ => true
            : post => post.AuthorId == ctx.Id;
}

// -----------------------------------------------------------------------------
// Scope builder — created once at app startup, reused across requests
// -----------------------------------------------------------------------------

static readonly ScopeBuilder<UserContext> PostScopes =
    new ScopeBuilder<UserContext>()
        .Register<Post>(new UserPosts());
        // .Register<Comment>(new UserComments())   ← add more types as needed

// =============================================================================
// EXAMPLE 1 — RAM
// =============================================================================

// App startup: build the raw (unscoped) RAM memory
var ramMap = new RamMemoryMap();
ramMap.Register(new RamEntities<Post>(p => p.Id));
IMemory ram = ramMap.Build();

// Per-request: activate scope for the current user
IMemory userRam = ram.Scoped(new UserContext(currentUser.Id), PostScopes);

// Use case — receives IMemory, knows nothing about the backend or scope:
IEntities<Post> posts = userRam.Entities<Post>();

var myPosts = await posts.All().ToListAsync();
//  └─ RAM path: RamScopedEntities.All() → post-filter: UserPosts.Includes(post, ctx)

var foreign = await posts.Load(foreignId);
//  └─ RAM: loads from ConcurrentDictionary → Includes check → NotFound (no leak)

await posts.Save(new Post(Guid.NewGuid(), currentUser.Id, "Hello"));
//  └─ CanWrite(post, ctx): true → RamScopedEntities.Save()

await posts.Save(new Post(Guid.NewGuid(), otherUserId, "Hijack"));
//  └─ CanWrite(post, ctx): false → UnauthorizedAccessException

// Transaction — scope stays active:
await using var tx = userRam.Begin();
await tx.Memory().Entities<Post>().Save(new Post(Guid.NewGuid(), currentUser.Id, "Tx"));
await tx.Commit();

// =============================================================================
// EXAMPLE 2 — Postgres
// =============================================================================

// Postgres entity origin: tells Marten how to extract the record's ID.
public sealed class Posts : PostgresEntities<Post>
{
    protected override Guid IdOf(Post post) => post.Id;
}

// App startup: build the raw (unscoped) Postgres memory
var pgMap = new PostgresMemoryMap(connectionString);
pgMap.Register(new Posts());            // IEntitiesOrigin → Marten session-bound
IMemory pg = pgMap.Build();

// Per-request: IDENTICAL to the RAM setup — same ScopeBuilder, same call
IMemory userPg = pg.Scoped(new UserContext(currentUser.Id), PostScopes);

IEntities<Post> pgPosts = userPg.Entities<Post>();

var myPgPosts = await pgPosts.All().ToListAsync();
//  └─ Postgres path:
//       ScopeAwareEntities detects ILinqEntitiesScope + ILinqFilterableEntities
//       → session.Query<Post>().Where(p => p.AuthorId == ctx.Id)
//       → SQL: SELECT * FROM posts WHERE author_id = @userId   ← no full-table scan ✓
//
//  └─ If BoundPostgresEntities does NOT implement ILinqFilterableEntities yet:
//       fallback to post-filter (same correctness, lower performance on large tables)

await pgPosts.Save(new Post(Guid.NewGuid(), currentUser.Id, "DB post")); // ✓
await pgPosts.Save(new Post(Guid.NewGuid(), otherUserId,    "Hijack"));  // → Unauthorized

// =============================================================================
// SUMMARY
// =============================================================================
//
//  UserPosts         defined once
//  ScopeBuilder      configured once at startup
//  .Scoped(ctx, …)   called per-request, same line for RAM and Postgres
//
//  RAM:      All() → post-filter in-process            (Includes predicate)
//  Postgres: All() → SQL WHERE via ILinqEntitiesScope  (Filter expression)
//            fallback to post-filter if backend not yet optimised
//
//  Load()    → NotFound for out-of-scope records       (no ID enumeration)
//  Save()    → UnauthorizedAccessException when CanWrite is false
//  Delete()  → loads first, checks CanDelete, then deletes
//  Begin()   → transactional memory inherits the scope

#endif
