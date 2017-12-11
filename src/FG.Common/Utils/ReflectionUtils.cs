using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;

namespace FG.Common.Utils
{
	public static class ReflectionUtils
	{
		public static T ActivateInternalCtor<T>(params object[] args)
		{
			var type = typeof(T);
			var instance = type.ActivateInternalCtor(args);
			return (T) instance;
		}

		public static object ActivateCtor(this Type type, params object[] args)
		{
			var constructorInfos = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			var matchingCtor = constructorInfos.FirstOrDefault(c =>
				c.GetParameters().Length == args.Length &&
				c.GetParameters().Select((a, i) => new {Type = a?.ParameterType ?? typeof(object), Index = i})
					.All(a => a.Type.IsAssignableFrom(args[a.Index]?.GetType() ?? typeof(object))));

			if (matchingCtor == null) return null;

			var instance = matchingCtor.Invoke(BindingFlags.CreateInstance, null, args, CultureInfo.CurrentCulture);
			return instance;
		}

		public static object ActivateInternalCtor(this Type type, params object[] args)
		{
			var constructorInfos = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

			var matchingCtor = constructorInfos.FirstOrDefault(c =>
				c.GetParameters().Length == args.Length &&
				c.GetParameters().Select((a, i) => new {Type = a?.ParameterType ?? typeof(object), Index = i})
					.All(a => a.Type.IsAssignableFrom(args[a.Index]?.GetType() ?? typeof(object))));

			if (matchingCtor == null) return null;

			var instance = matchingCtor.Invoke(BindingFlags.CreateInstance, null, args, CultureInfo.CurrentCulture);
			return instance;
		}

		public static void SetPrivateProperty<TImplementingType, TPropertyValue>(this TImplementingType that,
			Expression<Func<TPropertyValue>> propertyExpression, TPropertyValue value)
		{
			var memberExpr = propertyExpression.Body as MemberExpression;
			if (memberExpr == null)
				throw new ArgumentException("propertyExpression should represent access to a member");
			string memberName = memberExpr.Member.Name;

			var propertyInfo = typeof(TImplementingType)
				.GetProperty(memberName,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty);

			if (propertyInfo == null)
				throw new ArgumentException($"Cannot set value on property {memberName} on {that.GetType().Name}");

			propertyInfo.SetValue(that, value);
		}

		private static FieldInfo GetPrivateField(Type type, string fieldName)
		{
			var fieldInfo = type
				.GetField(fieldName,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);

			if (fieldInfo != null) return fieldInfo;

			if (type.BaseType != null)
			{
				return GetPrivateField(type.BaseType, fieldName);
			}

			return null;
		}

		private static PropertyInfo GetPrivateProperty(Type type, string propertyName)
		{
			var propertyInfo = type
				.GetProperty(propertyName,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);

			if (propertyInfo != null) return propertyInfo;

			if (type.BaseType != null)
			{
				return GetPrivateProperty(type.BaseType, propertyName);
			}

			return null;
		}

		private static FieldInfo GetPrivateStaticField(Type type, string fieldName)
		{
			var fieldInfo = type
				.GetField(fieldName,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);

			if (fieldInfo != null) return fieldInfo;

			if (type.BaseType != null)
			{
				return GetPrivateField(type.BaseType, fieldName);
			}

			return null;
		}

		private static MethodInfo GetPrivateOrPublicMethod(Type type, string methodName, Type[] argTypes)
		{
			var methodInfos = type
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			var methodInfo = methodInfos.SingleOrDefault(method =>
				method.Name == methodName && AreParameterTypesValid(method.GetParameters(), argTypes));

			if (methodInfo != null) return methodInfo;

			if (type.BaseType != null)
			{
				return GetPrivateOrPublicMethod(type.BaseType, methodName, argTypes);
			}

			return null;
		}

		private static bool AreParameterTypesValid(ParameterInfo[] parameterInfos, Type[] argumentTypes)
		{
			if (argumentTypes.Length > parameterInfos.Length) return false;

			for (var i = 0; i < parameterInfos.Length; i++)
			{
				var parameterInfo = parameterInfos[i];

				if (argumentTypes.Length >= i)
				{
					var argumentType = argumentTypes[i];

					if (parameterInfo.ParameterType.IsGenericParameter)
					{
						foreach (var genericParameterConstraint in parameterInfo.ParameterType.GetGenericParameterConstraints())
						{
							if (!genericParameterConstraint.IsAssignableFrom(argumentType)) return false;
						}
					}
					else if (!parameterInfo.ParameterType.IsAssignableFrom(argumentType)) return false;
				}
				else
				{
					if (!parameterInfo.IsOptional) return false;
				}
			}

			return true;
		}

		private static bool AreGenericTypesValid(Type[] genericTypes, Type[] argumentTypes)
		{
			if (argumentTypes.Length > genericTypes.Length) return false;

			for (var i = 0; i < genericTypes.Length; i++)
			{
				var genericType = genericTypes[i];

				if (argumentTypes.Length >= i)
				{
					var argumentType = argumentTypes[i];
					foreach (var genericParameterConstraint in genericType.GetGenericParameterConstraints())
					{
						if (!genericParameterConstraint.IsAssignableFrom(argumentType)) return false;
					}
				}
			}

			return true;
		}

		public static void SetPrivateField<TImplementingType, TPropertyValue>(this TImplementingType that,
			string fieldName, TPropertyValue value)
		{
			var fieldInfo = GetPrivateField(that.GetType(), fieldName);
			if (fieldInfo == null)
				throw new ArgumentException($"Cannot set value on field {fieldName} on {that.GetType().Name}");

			fieldInfo.SetValue(that, value);
		}

		public static TPropertyValue GetPrivateField<TImplementingType, TPropertyValue>(this TImplementingType that,
			string fieldName)
		{
			var fieldInfo = GetPrivateField(that.GetType(), fieldName);
			if (fieldInfo == null)
				throw new ArgumentException($"Cannot get value on field {fieldName} on {that.GetType().Name}");

			return (TPropertyValue) fieldInfo.GetValue(that);
		}

		public static TPropertyValue GetPrivateProperty<TImplementingType, TPropertyValue>(this TImplementingType that,
			string propertyName)
		{
			var property = GetPrivateProperty(that.GetType(), propertyName);
			if (property == null)
				throw new ArgumentException($"Cannot get value on property {propertyName} on {that.GetType().Name}");

			return (TPropertyValue) property.GetValue(that);
		}

		public static TResult GetPrivateStaticField<TResult>(this Type type, string fieldName)
		{
			var fieldInfo = GetPrivateStaticField(type, fieldName);
			if (fieldInfo == null)
				throw new ArgumentException($"Method {fieldName} does not exist on {type.Name}");

			return (TResult) fieldInfo.GetValue(null);
		}

		public static TResult CallPrivateStaticMethod<TResult>(this Type type, string methodName, params object[] args)
		{
			var methodInfo = type.GetMethod(
				name: methodName,
				bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
				types: args.Select(a => a.GetType()).ToArray(),
				binder: null,
				modifiers: null);

			if (methodInfo == null)
			{
				methodInfo = type.GetMethod(
					name: methodName,
					bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (methodInfo == null)
				{
					throw new ArgumentException($"Method {methodName} does not exist on {type.Name}");
				}
			}

			return (TResult) methodInfo.Invoke(null, args);
		}

		public static object CallPrivateMethod(this object that, string methodName, params object[] args)
		{
			var methodInfo = GetPrivateOrPublicMethod(that.GetType(), methodName, args.Select(a => a.GetType()).ToArray());
			if (methodInfo == null) throw new ArgumentException($"Method {methodName} does not exist on {that.GetType().Name}");

			return methodInfo.Invoke(that, args);
		}

		public static object CallGenericMethod(this object that, string methodName, Type[] genericTypes, params object[] args)
		{
			var type = that.GetType();
			return CallGenericMethod(that, type, methodName, genericTypes, args);
		}

		private static object CallGenericMethod(this object that, Type type, string methodName, Type[] genericTypes,
			params object[] args)
		{
			var argTypes = args.Select(a => a.GetType()).ToArray();
			var methodInfos = type
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			var methodInfo = methodInfos.SingleOrDefault(method =>
				method.Name == methodName &&
				AreParameterTypesValid(method.GetParameters(), argTypes) &&
				AreGenericTypesValid(method.GetGenericArguments(), genericTypes));

			if (methodInfo != null)
			{
				return methodInfo.MakeGenericMethod(genericTypes).Invoke(that, args);
			}

			if (type.BaseType != null)
			{
				return CallGenericMethod(that, type.BaseType, methodName, genericTypes, args);
			}

			var genericArgumentsList = genericTypes.Any()
				? $"<{genericTypes.Aggregate("", (a, b) => $"{a},{b.Name}").TrimStart(',')}>"
				: "";
			throw new ArgumentException($"Method {methodName}{genericArgumentsList} does not exist on {that.GetType().Name}");
		}

		public static bool ImplementsInterface(this Type type, Type interfaceType)
		{
			return interfaceType.IsAssignableFrom(type);
		}

		public static bool ImplementsInterface<TInterface>(this Type type)
		{
			return ImplementsInterface(type, typeof(TInterface));
		}

		public static object CreateInstanceOfInternal(Assembly assembly, string typeName)
		{
			var type = assembly.GetType(typeName);
			return CreateInstanceOfInternal(type);
		}

		public static object CreateInstanceOfInternal(Type type, params object[] parameters)
		{
			var instance = Activator.CreateInstance(type,
				BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters,
				CultureInfo.CurrentCulture);

			return instance;
		}

		public class InternallyActivatedClass
		{
			internal InternallyActivatedClass()
			{
			}

			internal InternallyActivatedClass(string a, int b)
			{
			}
		}
	}
}