using System.Text.Json;
using OneOf;

namespace Apia.File;

/// <summary>
/// File-backed single-record store. Persists as {TypeName}.json in the given directory.
/// Use for settings, config, or any singleton-style state.
/// </summary>
public sealed class FileVault<TResult> : IVault<TResult>
{
    private readonly string path;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private uint loadedVersion;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileVault(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, $"{typeof(TResult).Name}.json");
    }

    public async Task<OneOf<TResult, NotFound>> Load()
    {
        await writeLock.WaitAsync();
        try
        {
            OneOf<TResult, NotFound> result;
            if (!System.IO.File.Exists(path))
            {
                result = new NotFound();
            }
            else
            {
                await using var stream = System.IO.File.OpenRead(path);
                var versioned = await JsonSerializer.DeserializeAsync<Versioned<TResult>>(stream, JsonOptions);
                if (versioned is null)
                {
                    result = new NotFound();
                }
                else
                {
                    loadedVersion = versioned.Version;
                    result = versioned.Record;
                }
            }
            return result;
        }
        finally { writeLock.Release(); }
    }

    public async Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        await writeLock.WaitAsync();
        try
        {
            uint currentVersion = 0;
            if (System.IO.File.Exists(path))
            {
                await using var readStream = System.IO.File.OpenRead(path);
                var existing = await JsonSerializer.DeserializeAsync<Versioned<TResult>>(readStream, JsonOptions);
                if (existing is not null)
                {
                    if (existing.Version != loadedVersion)
                    {
                        var conflict = new Conflict<TResult>(existing.Record, record);
                        return OneOf<TResult, Conflict<TResult>>.FromT1(conflict);
                    }
                    currentVersion = existing.Version;
                }
            }

            var versioned = new Versioned<TResult>(record, currentVersion + 1);
            await using var writeStream = System.IO.File.Open(path, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(writeStream, versioned, JsonOptions);
            return OneOf<TResult, Conflict<TResult>>.FromT0(record);
        }
        finally { writeLock.Release(); }
    }
}
