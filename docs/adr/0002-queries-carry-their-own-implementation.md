# Queries carry their own implementation

A query object holds both what is asked and how to answer it, as a storage-agnostic implementation
reading through the memory it is handed: `IAggregateQuery<T>.Results(IMemory)` for many results,
`IProjectionQuery<T>.Result(IMemory)` for exactly one. The composition may additionally supply a
backend-specific override for a query, chosen at composition time and never visible to a use case.

We chose this over a registry that binds a query type to an implementation, because a registry makes
every forgotten registration a runtime failure, whereas a missing override is simply the default path.
It also removes the untyped `object query` parameter: a query states its own result type, so the
compiler rejects a mismatched pair that previously only failed in production.

## Consequences

The two-phase memory construction disappears with it (see ADR-0003), because a query now receives the
memory when it runs rather than when it is registered. A query therefore reads through whichever memory
it was asked through, which is what makes a branch's overlay and a scope reach inside it. Scopes stop at
overrides (see ADR-0001).

The plain defect reported alongside ADR-0001 in issue #12 goes with the registry rather than being
accepted: a registered source was handed the unscoped memory at the moment the memory was built, so a
scope could not reach inside such a source at all and a scope set to one author returned another
author's entities. There is no longer a registration at which a memory could be frozen. The contract
suite asserts the scope holding inside a query's own implementation on every backend, and the test
project asserts that nothing published takes an untyped value and that a query is named only by a
memory or by an override, so neither the registry nor the untyped path can return unnoticed.
