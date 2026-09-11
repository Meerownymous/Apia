# Memory is constructed, not built

A memory is created by its constructor from two immutable collections, the identities of the stored
types and the optional backend overrides. There is no memory map and no `Build()` step.

The two-phase construction existed only to break a circular dependency: a registered query source
needed the memory, and the memory needed its sources. ADR-0002 removed that circularity, leaving a
half-built object with no reason to exist. The collections stay fluent through extension methods over
decorators, which is what keeps the registration of overrides compile-time checked: the constraint
tying a query to its result type lives on a generic method, and a constructor cannot carry one.

## Consequences

The identities are backend-neutral and are reused across Ram, File and Postgres, which is what makes a
single contract test suite over all three backends cheap to write.
