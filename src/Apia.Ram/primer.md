# Apia — Roadmap Primer

## Vision

Apia provides a unified storage abstraction (`IMemory`) for business logic.
Use cases are written once and run against any supported backend without change.
A built-in query language covers the common intersection of all backends — backend-specific
optimizations remain possible via the Synopsis pattern when needed.

---

## Package Structure

```
Apia                    Core — IMemory, IEntities, IVault, IFilter, ...
Apia.Ram                In-memory backend (tests, development)
Apia.File               JSON file backend (local, edge)
Apia.LiteDb             Embedded document DB (desktop, offline)
Apia.Marten             PostgreSQL document-oriented (via Marten)
Apia.Mongo              MongoDB + Cosmos DB MongoDB-API
Apia.CosmosDb           Azure Cosmos DB native API
Apia.Remote             HTTP / OData backend

Apia.EFCore             EF Core base (abstract, not used directly)
Apia.EFCore.Sqlite      SQLite
Apia.EFCore.Postgres    PostgreSQL relational
Apia.EFCore.SqlServer   SQL Server
Apia.EFCore.MySql       MySQL / MariaDB
Apia.EFCore.Oracle      Oracle
```

---

## Backends

| Package | Technology | Use Case |
|---------|-----------|----------|
| `Apia.Ram` | In-process memory | Tests, prototypes, single-instance |
| `Apia.File` | JSON files | Local tools, edge, offline |
| `Apia.LiteDb` | LiteDB | Embedded apps, desktop, no server |
| `Apia.Marten` | PostgreSQL JSONB | Document-oriented, no schema migrations |
| `Apia.Mongo` | MongoDB Atlas | Document-oriented, cloud-agnostic |
| `Apia.CosmosDb` | Azure Cosmos DB | Azure, globally distributed |
| `Apia.Remote` | HTTP / OData | Microservices, remote memory |
| `Apia.EFCore.Sqlite` | SQLite | Local relational, testing with schema |
| `Apia.EFCore.Postgres` | PostgreSQL relational | Production relational |
| `Apia.EFCore.SqlServer` | SQL Server | Enterprise, Azure SQL |
| `Apia.EFCore.MySql` | MySQL / MariaDB | Web hosting, open source stacks |
| `Apia.EFCore.Oracle` | Oracle | Enterprise legacy |

> **Note:** `Apia.Mongo` is also compatible with the Cosmos DB MongoDB API,
> giving two integration paths for Azure depending on preference.

---

## Query Language

All queries are expressed via `Filter<T>` — a fluent, serializable, backend-agnostic
query model. Every function listed here is supported by all backends above.

### Entry Point

```csharp
Filter<PostRecord>
    .Where(p => p.AuthorId).Is(userId)
    .And(p => p.CreatedAt).IsAfter(since)
    .OrderByDescending(p => p.CreatedAt)
    .Take(20)
```

---

### Field Selection & Logical Operators

```csharp
.Where(p => p.Field)        // first condition
.And(p => p.Field)          // AND next condition
.Or(p => p.Field)           // OR next condition
.Not(p => p.Field)          // negate next condition
.AndGroup(q => q            // AND ( ... )
    .Where(...).Or(...))
.OrGroup(q => q             // OR ( ... )
    .Where(...).And(...))
```

---

### Comparison

```csharp
.Is(value)
.IsNot(value)
.IsGreaterThan(value)
.IsGreaterThanOrEqualTo(value)
.IsLessThan(value)
.IsLessThanOrEqualTo(value)
.IsBetween(min, max)
.IsIn(values)
.IsNotIn(values)
.IsNull()
.IsNotNull()
```

---

### Strings

```csharp
.Contains(substring)
.StartsWith(prefix)
.EndsWith(suffix)
.Matches(pattern)           // regex
.IsEmpty()
.IsNotEmpty()
.HasLengthOf(n)
.HasLengthBetween(min, max)
.IgnoringCase()             // modifier for the preceding operation
```

---

### Date & Time

```csharp
.IsBefore(date)
.IsAfter(date)
.IsBetween(from, to)
.IsOnDate(date)
.IsInYear(year)
.IsInMonth(year, month)
.IsToday()
.IsInThePast()
.IsInTheFuture()
.IsOlderThan(timeSpan)
.IsNewerThan(timeSpan)
```

---

### Collections / Array Fields

```csharp
.Contains(item)
.HasAny(p => p.SubField)
.HasAll(p => p.SubField)
.HasNone(p => p.SubField)
.HasCount(n)
.HasCountGreaterThan(n)
.IsEmpty()
.IsNotEmpty()
```

---

### Sorting

```csharp
.OrderBy(p => p.Field)
.OrderByDescending(p => p.Field)
.ThenBy(p => p.Field)
.ThenByDescending(p => p.Field)
.WithNullsFirst()
.WithNullsLast()
```

---

### Pagination

```csharp
.Take(n)
.Skip(n)
.Page(number, size)         // shorthand for Skip/Take
.First()
.Last()
```

---

### Aggregation

```csharp
.Count()
.Any()
.None()
.Sum(p => p.Field)
.Average(p => p.Field)
.Min(p => p.Field)
.Max(p => p.Field)
.Distinct()
.Distinct(p => p.Field)
```

---

### Result Control

```csharp
.FailIfEmpty()
.DefaultIfEmpty(value)
.Reverse()
```

---

### Combining Filters

```csharp
.Merge(otherFilter)         // AND-combination of two Filter objects
.Union(otherFilter)         // OR-combination
.Except(otherFilter)        // exclusion
```

---

## Projection Pattern

Simple projections are written once using `IMemory` and run against all backends:

```csharp
public sealed class UserFeedSynopsisStream()
    : RamSynopsisStream<UserPostSummaryProjection, UserFeedQuery>(
        async (memory, query) =>
        {
            // uses only IMemory — compatible with every backend
        })
```

When performance demands it, a backend-specific implementation can replace it
without changing the calling code:

```csharp
public sealed class PostgresUserFeedSynopsis
    : PostgresSynopsis<UserPostSummaryProjection, UserFeedQuery>
{
    // uses SQL joins, indexes, etc.
}
```

---

## Remote Memory

`Apia.Remote` allows `IMemory` to be served and consumed over HTTP via OData.

**Server:**
```csharp
// ASP.NET Core — exposes IMemory as OData endpoints
app.MapApiaOData(memory);
```

**Client:**
```csharp
var map = new RemoteMemoryMap("https://storage.example.com/api/");
map.Register<PostRecord>();
var memory = map.Build();

// Same use-case code as always:
await memory.Entities<PostRecord>().Find(
    Filter<PostRecord>.Where(p => p.AuthorId).Is(userId));
```

`Filter<T>` serializes cleanly to JSON and maps to OData query syntax —
no LINQ expression trees are transmitted over the wire.

---

## Optimistic Concurrency

All backends implement optimistic concurrency via versioning:

```csharp
var result = await memory.Entities<PostRecord>().Save(post);

result.Switch(
    saved    => ...,                          // success
    conflict => Handle(conflict.Current,      // version mismatch
                       conflict.Attempted));
```

---

## Transactions

```csharp
await using var tx = memory.Begin();
var txMemory = tx.Memory();

await new DeductCreditsUseCase(txMemory).Execute(userId, amount);
await new RecordPurchaseUseCase(txMemory).Execute(userId, itemId);

await tx.Commit();   // both persist — or rollback on dispose without commit
```