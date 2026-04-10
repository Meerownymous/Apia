namespace Apia.Scope;

/// <summary>
/// A typed collection of per-record access policies for a given context type.
/// Built via fluent <c>With</c> calls and consumed by <see cref="PolicyMemory{TContext}"/>.
/// </summary>
public interface IPolicies<TContext>
{
    /// <summary>Whether an access policy is registered for <typeparamref name="TRecord"/>.</summary>
    bool Has<TRecord>();

    /// <summary>Returns the registered access policy for <typeparamref name="TRecord"/>.</summary>
    IAccessPolicy<TRecord, TContext> Of<TRecord>();

    /// <summary>
    /// Registers explicit read, write, and delete predicates for <typeparamref name="TRecord"/>.
    /// </summary>
    IPolicies<TContext> With<TRecord>(
        Func<TRecord, TContext, bool> read,
        Func<TRecord, TContext, bool> write,
        Func<TRecord, TContext, bool> delete);

    /// <summary>
    /// Registers a single predicate that applies to read, write, and delete for
    /// <typeparamref name="TRecord"/>.
    /// </summary>
    IPolicies<TContext> With<TRecord>(Func<TRecord, TContext, bool> access);
}
