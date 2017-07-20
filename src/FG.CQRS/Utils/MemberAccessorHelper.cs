using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FG.CQRS.Utils
{
    public static class MemberAccessorHelper
    {
        private static readonly IDictionary<Type, Func<object, object>[]> TypeFields = new ConcurrentDictionary<Type, Func<object, object>[]>();

        private static Func<object, object> BuildFieldGetter(FieldInfo field)
        {
            var obj = Expression.Parameter(typeof(object), "obj");

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Field(
                        Expression.Convert(obj, field.DeclaringType),
                        field),
                    typeof(object)),
                obj).Compile();
        }

        public static IEnumerable<object> GetFieldAndPropertyValues(object o)
        {
            return GetFieldsAndPropertyGetters(o.GetType()).Select(getter => getter(o));
        } 

        public static Func<object, object>[] GetFieldsAndPropertyGetters(Type type)
        {
            return InnerGetField(type);
        }

        private static Func<object, object>[] InnerGetField(Type type)
        {
            Func<object, object>[] fields;
            if (!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<object, object>>();
                if (!type.IsPrimitive)
                {
                    newFields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(BuildFieldGetter));

                    var baseType = type.BaseType;
                    if (baseType != null && baseType != typeof (object))
                    {
                        newFields.AddRange(GetFieldsAndPropertyGetters(baseType));
                    }
                }
                TypeFields[type] = fields = newFields.ToArray();
            }
            Contract.Assume(fields != null);
            return fields;
        }
    }

    public static class MemberAccessorHelper<T>
    {
        public static readonly Func<object, object>[] Fields;

        static MemberAccessorHelper()
        {
            Fields = MemberAccessorHelper.GetFieldsAndPropertyGetters(typeof(T));
        }

        public static Func<object, object>[] GetFieldsAndProperties(Type type)
        {
            return type == typeof(T) ? Fields : MemberAccessorHelper.GetFieldsAndPropertyGetters(type);
        }
    }
}