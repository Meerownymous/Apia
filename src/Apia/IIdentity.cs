namespace Apia;

public interface IIdentity<T> where T : notnull
{
    Guid Of(T entity);
}
