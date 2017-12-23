namespace FG.Common.Expressions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Provides a way to creates functions by using expressions
    /// </summary>
    public static partial class CreateInstanceFactory
    {
        //// TODO: Add methods to create types that take parameters

        public static Delegate CreateInstance(Type typeToInstantiate)
        {
            return CreateInstance(typeToInstantiate, typeToInstantiate);
        }

        public static Delegate CreateInstance(Type typeToInstantiate, Type typeToReturn)
        {
            var constructorInfo = typeToInstantiate.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);

            if (constructorInfo == null)
            {
                throw new Exception("Type " + typeToInstantiate.FullName + " does not contain a parameterless constructor.");
            }

            var currentExpression = Expression.Lambda(typeof(Func<>).MakeGenericType(typeToReturn), Expression.New(constructorInfo));
            return currentExpression.Compile();
        }

        public static Func<TTypeToReturn> CreateInstance<TTypeToInstantiate, TTypeToReturn>()
        {
            return (Func<TTypeToReturn>)CreateInstance(typeof(TTypeToInstantiate), typeof(TTypeToReturn));
        }

        public static Func<T> CreateInstance<T>()
        {
            return (Func<T>)CreateInstance(typeof(T));
        }
    }
}