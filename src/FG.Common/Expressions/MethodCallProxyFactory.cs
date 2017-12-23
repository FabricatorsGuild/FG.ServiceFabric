namespace FG.Common.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public class MethodCallProxyFactory
    {
        public class CreateMethodProxyFactory
        {
            public static Delegate CreateMethodProxy(MethodInfo methodInfo, Type returnValueType, Type parameterType, string parameterName = null)
            {
                var typeParameter = Expression.Parameter(parameterType, parameterName);
                return Expression.Lambda(typeof(Func<,>).MakeGenericType(parameterType, returnValueType), Expression.Call(methodInfo, typeParameter), typeParameter).Compile();
            }

            public static Func<TParameterType, TReturnValue> CreateMethodProxy<TParameterType, TReturnValue>(MethodInfo methodInfo, string parameterName = null)
            {
                return (Func<TParameterType, TReturnValue>)CreateMethodProxy(methodInfo, typeof(TReturnValue), typeof(TParameterType), parameterName);
            }
        }
    }
}