using System.Collections.Concurrent;
using System.Linq.Expressions;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// A Postgres-backed entity origin that filters <see cref="IEntities{TRecord}.All"/> at the
/// SQL level via Marten's LINQ provider. The context is baked in at construction time, which
/// matches per-request DI patterns: create one instance per user scope and register it in
/// <see cref="PostgresMemoryMap"/>.
///
/// Subclass and implement <see cref="IdOf"/> and <see cref="ScopeFilter"/>:
/// <code>
/// // Domain types:
/// public sealed record UserCtx(Guid UserId, bool IsAdmin);
/// public sealed record Post(Guid Id, Guid AuthorId, string Body);
///
/// // Scoped entity origin — one instance per request:
/// public sealed class ScopedPostEntities(UserCtx ctx)
///     : ScopedPostgresEntities&lt;Post, UserCtx&gt;(ctx)
/// {
///     protected override Guid IdOf(Post p) =&gt; p.Id;
///     protected override Expression&lt;Func&lt;Post, bool&gt;&gt; ScopeFilter(UserCtx ctx)
///         =&gt; post =&gt; ctx.IsAdmin || post.AuthorId == ctx.UserId;
/// }
///
/// // Per-request wiring — no WithPolicy needed, filtering is at the database level:
/// var map = new PostgresMemoryMap(connectionString);
/// map.Register(new ScopedPostEntities(currentUser));
/// var memory = map.Build();
///
/// // All() executes: SELECT * FROM posts WHERE author_id = @userId (or no WHERE for admins)
/// var myPosts = memory.Entities&lt;Post&gt;().All();
/// </code>
///
/// <para>
/// Load, Save, and Delete are not scoped — they operate by ID and are access-checked at the
/// <see cref="PolicyEntities{TRecord,TContext}"/> level when
/// <see cref="Scope.MemoryExtensions.WithPolicy{TContext}"/> is also applied.
/// </para>
/// </summary>
public abstract class PostgresScopedEntities<TRecord, TContext>
    : IEntitiesOrigin<TRecord, (IMemory Memory, IDocumentSession Session)>
    where TRecord : notnull
{
    private readonly TContext context;

    /// <summary>Binds the origin to the given context for the lifetime of this instance.</summary>
    protected PostgresScopedEntities(TContext context) => this.context = context;

    /// <summary>Extracts the Guid key from a record.</summary>
    protected abstract Guid IdOf(TRecord record);

    /// <summary>
    /// Returns a LINQ expression that Marten translates to a SQL WHERE clause.
    /// Called once per <see cref="Bind"/>; the expression is reused for all <c>All()</c> calls
    /// on the returned <see cref="IEntities{TRecord}"/>.
    /// </summary>
    protected abstract Expression<Func<TRecord, bool>> ScopeFilter(TContext context);

    /// <inheritdoc/>
    public IEntities<TRecord> Bind((IMemory Memory, IDocumentSession Session) ctx)
        => new BoundEntities(ctx.Session, IdOf, ScopeFilter(context));

    private sealed class BoundEntities(
        IDocumentSession session,
        Func<TRecord, Guid> idOf,
        Expression<Func<TRecord, bool>> filter) : IEntities<TRecord>
    {
        private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

        /// <inheritdoc/>
        public Guid IdOf(TRecord record) => idOf(record);

        /// <summary>Streams only records matching the scope filter — pushed to SQL by Marten.</summary>
        public async IAsyncEnumerable<TRecord> All()
        {
            await foreach (var record in session.Query<TRecord>().Where(filter).ToAsyncEnumerable())
                yield return record;
        }

        /// <inheritdoc/>
        public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
        {
            var record = await session.LoadAsync<TRecord>(id);
            if (record is null)
                return new NotFound();
            var version = await LoadVersion(id);
            loadedVersions[id] = version;
            return record;
        }

        /// <inheritdoc/>
        public async Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
        {
            var id              = idOf(record);
            var currentVersion  = await LoadVersion(id);
            var expectedVersion = loadedVersions.GetValueOrDefault(id, 0u);
            if (currentVersion > 0 && currentVersion != expectedVersion)
            {
                var current  = await session.LoadAsync<TRecord>(id);
                var conflict = new Conflict<TRecord>(current!, record);
                return OneOf<TRecord, Conflict<TRecord>>.FromT1(conflict);
            }
            session.Store(record);
            session.Store(new ApiaVersion(VersionId(id), typeof(TRecord).Name, id, currentVersion + 1));
            return OneOf<TRecord, Conflict<TRecord>>.FromT0(record);
        }

        /// <inheritdoc/>
        public Task Delete(Guid id)
        {
            session.Delete<TRecord>(id);
            session.Delete<ApiaVersion>(VersionId(id));
            loadedVersions.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        private async Task<uint> LoadVersion(Guid id)
        {
            var doc = await session.LoadAsync<ApiaVersion>(VersionId(id));
            return doc?.Version ?? 0u;
        }

        private static Guid VersionId(Guid recordId)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(typeof(TRecord).Name)
                .Concat(recordId.ToByteArray())
                .ToArray();
            return new Guid(System.Security.Cryptography.MD5.HashData(bytes));
        }
    }
}
