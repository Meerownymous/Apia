using System.Collections;
using System.Reflection;

namespace Apia.Ram.Tests.Assert;

public static class AssertRecord
{
    public static void Equal(object a, object b)
    {
        if (a is Array arrA && b is Array arrB)
        {
            CompareArrays(arrA, arrB, skipDefaults: false);
            return;
        }

        EnsureRecord(a);
        EnsureRecord(b);

        if (a.GetType() != b.GetType())
            throw new RecordEqualityException(
                $"Type mismatch: {a.GetType().Name} != {b.GetType().Name}"
            );

        var differences = GetDifferences(a, b, skipDefaults: false).ToList();

        if (differences.Any())
            throw new RecordEqualityException(
                $"Records differ:\n{string.Join("\n", differences)}"
            );
    }

    public static void NotEqual(object a, object b)
    {
        if (a is Array arrA && b is Array arrB)
        {
            var hasDifferences = ArrayDifferences(arrA, arrB, skipDefaults: false).Any();
            if (!hasDifferences)
                throw new RecordEqualityException("Arrays are equal but expected to differ.");
            return;
        }

        EnsureRecord(a);
        EnsureRecord(b);

        var differences = a.GetType() != b.GetType()
            ? (IEnumerable<string>)["Type mismatch"]
            : GetDifferences(a, b, skipDefaults: false).ToList();

        if (!differences.Any())
            throw new RecordEqualityException("Records are equal but expected to differ.");
    }

    public static void Satisfies(object[] expected, object[] actual)
    {
        CompareArrays(expected, actual, skipDefaults: true);
    }

    public static void Satisfies(object expected, object actual)
    {
        if (expected is Array arrExp && actual is Array arrAct)
        {
            CompareArrays(arrExp, arrAct, skipDefaults: true);
            return;
        }

        EnsureRecord(expected);
        EnsureRecord(actual);

        var differences = GetDifferences(expected, actual, skipDefaults: true).ToList();

        if (differences.Any())
            throw new RecordEqualityException(
                $"Records differ:\n{string.Join("\n", differences)}"
            );
    }

    public static void NotSatisfies(object[] expected, object[] actual)
    {
        var differences = ArrayDifferences(expected, actual, skipDefaults: true).ToList();

        if (!differences.Any())
            throw new RecordEqualityException("Arrays satisfy expectations but were expected not to.");
    }

    public static void NotSatisfies(object expected, object actual)
    {
        if (expected is Array arrExp && actual is Array arrAct)
        {
            var differences = ArrayDifferences(arrExp, arrAct, skipDefaults: true).ToList();
            if (!differences.Any())
                throw new RecordEqualityException("Arrays satisfy expectations but were expected not to.");
            return;
        }

        EnsureRecord(expected);
        EnsureRecord(actual);

        var diffs = GetDifferences(expected, actual, skipDefaults: true).ToList();

        if (!diffs.Any())
            throw new RecordEqualityException("Records satisfy expectations but were expected not to.");
    }

    private static void CompareArrays(Array expected, Array actual, bool skipDefaults)
    {
        var differences = ArrayDifferences(expected, actual, skipDefaults).ToList();

        if (differences.Any())
            throw new RecordEqualityException(
                $"Arrays differ:\n{string.Join("\n", differences)}"
            );
    }

    private static IEnumerable<string> ArrayDifferences(Array expected, Array actual, bool skipDefaults)
    {
        if (expected.Length != actual.Length)
        {
            yield return $"Array length mismatch: {expected.Length} != {actual.Length}";
            yield break;
        }

        for (int i = 0; i < expected.Length; i++)
        {
            var elemExp = expected.GetValue(i);
            var elemAct = actual.GetValue(i);

            if (elemExp is null && elemAct is null) continue;
            if (elemExp is null || elemAct is null)
            {
                yield return $"  [{i}]: \"{elemExp}\" != \"{elemAct}\"";
                continue;
            }

            foreach (var diff in GetDifferences(elemExp, elemAct, skipDefaults, $"[{i}]"))
                yield return diff;
        }
    }

    private static void EnsureRecord(object obj)
    {
        var type = obj.GetType();
        if (type.GetMethod("<Clone>$") is null)
            throw new ArgumentException($"{type.Name} is not a record type.");
    }

    private static IEnumerable<string> GetDifferences(object expected, object actual, bool skipDefaults, string path = "")
    {
        var expectedProps = expected.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in expectedProps)
        {
            var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            var valExpected = prop.GetValue(expected);

            // Skip BEFORE touching actual — only compare props that are set in expected
            if (skipDefaults && IsDefault(valExpected, prop.PropertyType))
                continue;

            var actualProp = actual.GetType().GetProperty(prop.Name,
                BindingFlags.Public | BindingFlags.Instance);

            if (actualProp is null)
            {
                yield return $"  {propPath}: property not found on actual";
                continue;
            }

            var valActual = actualProp.GetValue(actual);

            if (valExpected is null && valActual is null) continue;
            if (valExpected is null || valActual is null)
            {
                yield return $"  {propPath}: \"{valExpected}\" != \"{valActual}\"";
                continue;
            }

            var propType = prop.PropertyType;

            if (propType.GetMethod("<Clone>$") != null)
            {
                foreach (var diff in GetDifferences(valExpected, valActual, skipDefaults, propPath))
                    yield return diff;
            }
            else if (valExpected is IEnumerable enumExp && valActual is IEnumerable enumAct
                                                        && propType != typeof(string))
            {
                var listExp = enumExp.Cast<object>().ToList();
                var listAct = enumAct.Cast<object>().ToList();

                if (listExp.Count != listAct.Count)
                {
                    yield return $"  {propPath}: count {listExp.Count} != {listAct.Count}";
                    continue;
                }

                for (int i = 0; i < listExp.Count; i++)
                {
                    var elemPath = $"{propPath}[{i}]";
                    if (listExp[i].GetType().GetMethod("<Clone>$") != null)
                        foreach (var diff in GetDifferences(listExp[i], listAct[i], skipDefaults, elemPath))
                            yield return diff;
                    else if (!listExp[i].Equals(listAct[i]))
                        yield return $"  {elemPath}: \"{listExp[i]}\" != \"{listAct[i]}\"";
                }
            }
            else if (!valExpected.Equals(valActual))
            {
                yield return $"  {propPath}: \"{valExpected}\" != \"{valActual}\"";
            }
        }
    }

    private static bool IsDefault(object? value, Type type)
    {
        return value is null
               || (type == typeof(string) && (string)value == "")
               || (type.IsValueType && value.Equals(Activator.CreateInstance(type)));
    }
}

public sealed class RecordEqualityException(string message) : Exception(message);