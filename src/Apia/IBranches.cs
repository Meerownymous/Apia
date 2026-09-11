namespace Apia;

/// <summary>The units of work a memory hands out.</summary>
public interface IBranches
{
    /// <summary>A new unit of work.</summary>
    IBranch Branch();
}
