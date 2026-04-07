namespace Apia;

/// <summary>An ordering over a sequence of items.</summary>
public interface ISort<T>
{
    /// <summary>The items from source ordered by this sort's criteria.</summary>
    IEnumerable<T> Sorted(IEnumerable<T> source);
}
