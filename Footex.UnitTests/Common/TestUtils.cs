using System.Reflection;

namespace Footex.UnitTests.Common;

public static class TestUtils
{
    public static TResult InvokeStaticMethod<T, TResult>(
        string methodName,
        params object[] parameters
    )
    {
        var type = typeof(T);
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            throw new ArgumentException(
                $"Static method '{methodName}' not found on type '{type.Name}'."
            );
        }
        return (TResult)method.Invoke(null, parameters)!;
    }

    public static TResult InvokeInstanceMethod<T, TResult>(
        T instance,
        string methodName,
        params object[] parameters
    )
    {
        var type = typeof(T);
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new ArgumentException(
                $"Instance method '{methodName}' not found on type '{type.Name}'."
            );
        }
        return (TResult)method.Invoke(instance, parameters)!;
    }

    public static TResult InvokePrivateMethod<T, TResult>(
        T instance,
        string methodName,
        params object[] parameters
    )
    {
        var type = typeof(T);
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new ArgumentException(
                $"Private method '{methodName}' not found on type '{type.Name}'."
            );
        }
        return (TResult)method.Invoke(instance, parameters)!;
    }

    public static async Task<TResult> InvokePrivateMethodAsync<T, TResult>(
        T instance,
        string methodName,
        params object[] parameters
    )
    {
        var type = typeof(T);
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new ArgumentException(
                $"Private method '{methodName}' not found on type '{type.Name}'."
            );
        }

        var result = method.Invoke(instance, parameters);
        if (result is Task<TResult> task)
        {
            return await task;
        }
        else if (result is Task)
        {
            await (Task)result;
            return default(TResult)!;
        }
        else
        {
            return (TResult)result!;
        }
    }
}
