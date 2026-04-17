using OneOf;

namespace Apia;

public interface IVault<T>
{
    Task<OneOf<T, NotFound>> Load(Guid id);
}
