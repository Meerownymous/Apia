using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using OneOf;

namespace Apia;

public static class OneOfExtensions
{
    public static bool Is<T>(this IOneOf oneOf) => oneOf.Value is T;
}