using System.Reflection;
using Xunit;

namespace Apia.Tests.Query;

/// <summary>
/// What a query is asked through, and what is no longer there to ask through. Nothing the library
/// publishes takes an untyped value, the memory and the branch included, so the untyped query path has
/// no way back. A query is named — in a parameter, a return type or a generic constraint, and by a
/// constructor as much as by a method — only by a memory or by an override, so a source registered
/// under a query type, and the registry that held it, cannot return unnoticed.
/// </summary>
public sealed class QuerySurfaceTests
{
    [Theory]
    [ClassData(typeof(ApiaAssemblies))]
    public void Member_TakesNoUntypedParameter(Assembly assembly)
        => Assert.Empty(
            assembly.GetTypes()
                .SelectMany(type => type
                    .GetConstructors()
                    .Concat<MethodBase>(
                        type.GetMethods(
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                            // An override of a member System.Object declares takes an object because
                            // System.Object says so, which is not this library asking for one.
                            .Where(member => member.GetBaseDefinition() == member))
                    .Where(member => member.GetParameters().Any(parameter => parameter.ParameterType == typeof(object)))
                    .Select(member => $"{type.Name}.{member.Name}")));

    [Theory]
    [ClassData(typeof(ApiaAssemblies))]
    public void Query_IsNamedOnlyByAMemoryOrByAnOverride(Assembly assembly)
        => Assert.Empty(
            assembly.GetTypes()
                .Where(type => !typeof(IMemory).IsAssignableFrom(type) && !typeof(IOverrides).IsAssignableFrom(type))
                .Where(type => type != typeof(IAggregateOverride<,>) && type != typeof(IProjectionOverride<,>))
                .Where(type => type != typeof(OverridesExtensions))
                .Where(type => type
                    .GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .SelectMany(member => member
                        .GetParameters()
                        .Select(parameter => parameter.ParameterType)
                        .Append(member.ReturnType)
                        .Concat(member.GetGenericArguments().SelectMany(asked => asked.GetGenericParameterConstraints())))
                    .Concat(type.GetConstructors()
                        .SelectMany(made => made.GetParameters().Select(parameter => parameter.ParameterType)))
                    .Concat(type.GetGenericArguments().SelectMany(asked => asked.GetGenericParameterConstraints()))
                    .Any(signature => signature.IsGenericType &&
                                      (signature.GetGenericTypeDefinition() == typeof(IAggregateQuery<>) ||
                                       signature.GetGenericTypeDefinition() == typeof(IProjectionQuery<>))))
                .Select(type => type.Name));
}
