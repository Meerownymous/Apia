using Apia.Scope;
using Xunit;

namespace Apia.Tests.Scope;

/// <summary>
/// The scope interface itself: four members and an answer to none of them, so that a rule a scope
/// never stated cannot be supplied on its behalf.
/// </summary>
public sealed class ScopeInterfaceTests
{
    [Fact]
    public void Scope_CarriesNoImplementation()
        => Assert.Empty(
            typeof(IScope<,>).GetMethods().Where(rule => !rule.IsAbstract).Select(rule => rule.Name));
}
