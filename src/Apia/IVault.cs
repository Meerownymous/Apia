using OneOf;

namespace Apia;

public interface IVault<T> where T : notnull
{
    Task<OneOf<T, NotFound>> Load(Guid id);
}
