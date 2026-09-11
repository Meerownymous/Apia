---
status: accepted
---

# Backend query overrides bypass scopes

A query carries a storage-agnostic default implementation that reads through `IVault<T>`, where
`ScopedVault` enforces any `IScope` the memory was composed with. The composition may optionally supply
a backend-specific override (for example a hand-written SQL statement on Postgres) for the same query.
Such an override reads past `IVault<T>` and therefore past every scope. We accept this: scopes and
overrides are orthogonal, and the author of an override is responsible for applying the equivalent
filter themselves.

`ScopeMemory` is given the same overrides the inner memory was composed with, so an override still
answers a query asked through a scoped memory. It is handed the scoped memory as the one to read
through; whether it does so is its own business, and a hand-written statement will not.

## Considered Options

- Pass the scope to the override as an `Expression` it must AND into its own query. Rejected: the
  signature cannot express which entity types need a scope, so it is discipline wearing a type.
- Forbid overrides for scoped types. Rejected: the composition does not know which types a query reads.
- Let a query declare the entity types it touches, so the memory can silently fall back to the default
  path when any of them is scoped. Rejected: it costs a member on every query object.

## Consequences

Supplying an override for a query that reads scope-protected entities is a security decision, not a
performance decision. `ScopeMemory` must not be documented as if it were a complete boundary, and its
own documentation says so. Revisiting this is tracked as issue #12.
