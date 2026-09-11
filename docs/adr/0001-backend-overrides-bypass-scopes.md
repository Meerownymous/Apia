---
status: accepted
---

# Backend query overrides bypass scopes

A query carries a storage-agnostic default implementation that reads through `IVault<T>`, where
`ScopeAwareVault` enforces any registered `IScope`. The memory composition may optionally register a
backend-specific override (for example a hand-written SQL statement on Postgres) for the same query.
Such an override reads past `IVault<T>` and therefore past every scope. We accept this: scopes and
overrides are orthogonal, and the author of an override is responsible for applying the equivalent
filter themselves.

## Considered Options

- Pass the scope to the override as an `Expression` it must AND into its own query. Rejected: the
  signature cannot express which record types need a scope, so it is discipline wearing a type.
- Forbid overrides for scoped types. Rejected: the map does not know which types a query reads.
- Let a query declare the record types it touches, so the container can silently fall back to the
  default path when any of them is scoped. Rejected: it costs a member on every query object.

## Consequences

Registering an override for a query that reads scope-protected records is a security decision, not a
performance decision. `ScopeMemory` must not be documented as if it were a complete boundary.
