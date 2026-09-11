namespace Apia.Tests.Backend;

/// <summary>One storage technique the contract suite runs against.</summary>
public interface IBackend
{
    /// <summary>A memory over this backend, composed from the identities every backend shares.</summary>
    IMemory Memory();
}
