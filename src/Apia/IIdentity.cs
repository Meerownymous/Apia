namespace Apia;

public interface IIdentity<T>
{
    string Of(T entity);
}
