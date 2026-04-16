using OneOf;

namespace Apia.TestAssets.Assert;

public static class AssertOneOf
{
    public static void Is<TExpected>(IOneOf oneOf)
    {
        if (oneOf.Value is TExpected)
            return;

        throw new OneOfAssertionException(typeof(TExpected), oneOf.Value.GetType());
    }

    public static async Task Is<TExpected>(Task<IOneOf> task)
    {
        var result = await task;
        Is<TExpected>(result);
    }
}

public class OneOfAssertionException(Type expected, Type actual)
    : Exception($"Expected OneOf to be <{expected.Name}> but was <{actual.Name}>.");