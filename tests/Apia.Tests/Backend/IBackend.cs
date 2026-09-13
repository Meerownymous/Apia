namespace Apia.Tests.Backend;

/// <summary>One storage technique the contract suite runs against.</summary>
public interface IBackend
{
    /// <summary>A memory over this backend, composed from the identities every backend shares.</summary>
    IMemory Memory();

    /// <summary>
    /// A memory over this backend, composed from the same identities and with the given backend
    /// overrides, so that one query can be asked with and without an override of its own.
    /// </summary>
    IMemory Memory(IOverrides overrides);
}
