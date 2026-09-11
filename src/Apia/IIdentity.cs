namespace Apia;

/// <summary>The id an entity of type T is stored under.</summary>
public interface IIdentity<T> where T : notnull
{
    /// <summary>The id of the given entity.</summary>
    Guid Of(T entity);
}
