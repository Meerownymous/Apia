namespace Apia.Scope;

/// <summary>A registry that maps entity types to their scope rules for a given filter.</summary>
public interface IScopeRegistry<TFilter>
{
    /// <summary>Registers a scope rule for entities of type TRecord.</summary>
    void Register<TRecord>(IScope<TRecord, TFilter> scope);

    /// <summary>Whether a scope rule exists for entities of type TRecord.</summary>
    bool HasScope<TRecord>();

    /// <summary>The scope rule for entities of type TRecord.</summary>
    IScope<TRecord, TFilter> ScopeFor<TRecord>();
}
