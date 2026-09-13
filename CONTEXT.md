# Apia

A storage abstraction for application code. Use cases are written against one container so that the
storage technique can be swapped, and faked wholesale in tests, without the use case changing.

## Language

**Entity**:
The thing that is stored, identified by a Guid.
_Avoid_: Record, document, item, content

**Memory**:
The storage abstraction an application is written against. One container per application.
_Avoid_: Repository, context, store (store is narrower, see below)

**Vault**:
Read access to the entities of a single type within a memory.

**Store**:
The persistence of a single entity type inside one backend. Below the vault, never seen by a use case.

**Branch**:
A unit of work. Changes staged on it are layered over the committed state for anyone reading through
it, and reach the stores only on commit.

**Commit**:
The flush of a branch. Either every staged change takes effect or none does.

**Stale**:
The outcome of a commit whose branch read an entity that changed underneath it. Names every read that
went stale.

**Changed**:
An entity a branch read that another branch has written since, named by entity type and id. What a
stale outcome carries.

**Aggregate**:
A query returning many results. Carries its own storage-agnostic implementation.

**Projection**:
A query returning exactly one computed result. Has no identity and is never written.
_Avoid_: View

**Override**:
An optional backend-specific implementation of an aggregate or projection, chosen when the memory is
composed. Never visible to a use case.

**Scope**:
The rule deciding which entities are visible, writable and deletable for a given filter value.

**Filter**:
The value a scope is evaluated against, such as the id of the signed-in user.
