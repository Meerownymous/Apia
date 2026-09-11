namespace Apia;

/// <summary>The staged changes of a branch, one entry per entity type it has touched.</summary>
public interface IStagings
{
    /// <summary>The changes staged for entities of type T, empty until the branch touches the type.</summary>
    Staged<T> Entries<T>() where T : notnull;

    /// <summary>The staged changes of every entity type the branch has touched.</summary>
    IEnumerable<IStagedChanges> Changes();
}
