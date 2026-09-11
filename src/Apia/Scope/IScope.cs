using System.Linq.Expressions;
using OneOf;

namespace Apia.Scope;

/// <summary>
/// The rule deciding which entities of type <typeparamref name="T"/> are visible, writable and
/// deletable for a given filter value of type <typeparamref name="TFilter"/>. Every rule is stated:
/// "anything you can see, you can change" is a decision, not a default.
/// </summary>
public interface IScope<T, TFilter> where T : notnull
{
    /// <summary>Whether the given entity is visible for the given filter value.</summary>
    bool Includes(T entity, TFilter filter);

    /// <summary>Whether the given entity may be saved for the given filter value.</summary>
    bool CanWrite(T entity, TFilter filter);

    /// <summary>Whether the given entity may be removed for the given filter value.</summary>
    bool CanDelete(T entity, TFilter filter);

    /// <summary>
    /// The condition equivalent of <see cref="Includes"/>, or <see cref="None"/> when the rule cannot be
    /// expressed as one. A backend that understands conditions pushes it into its own query language
    /// instead of fetching everything and discarding most of it.
    /// </summary>
    OneOf<Expression<Func<T, bool>>, None> Condition(TFilter filter);
}
