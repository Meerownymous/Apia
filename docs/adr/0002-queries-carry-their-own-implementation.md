# Queries carry their own implementation

A query object holds both what is asked and how to answer it, as a storage-agnostic implementation
reading through the memory it is handed. The memory composition may additionally register a
backend-specific override for a query, chosen at composition time and never visible to a use case.

We chose this over a registry that binds a query type to an implementation, because a registry makes
every forgotten registration a runtime failure, whereas a missing override is simply the default path.
It also removes the untyped `object query` parameter: a query states its own result type, so the
compiler rejects a mismatched pair that previously only failed in production.

## Consequences

The two-phase memory construction disappears with it (see ADR-0003), because a query now receives the
memory when it runs rather than when it is registered. Scopes stop at overrides (see ADR-0001).
