namespace Apia;

public interface IIdentity<T>
{
    Guid Of(T entity);
}
