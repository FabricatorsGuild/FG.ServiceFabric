using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FG.Common.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace FG.Common.Tests
{

	public class TestClass
	{
		public string MethodWithOneGenericArg<T>(string value)
		{
			return $"{typeof(T).Name} - {value}";
		}
	}

    public class ReflectionUtils_tests
    {
		[Test]
	    public void CallGenericMethod_for_non_generic_class_should_match_generic_method_arguments_exactly()
		{
			var methodWithOneGenericArgName = nameof(TestClass.MethodWithOneGenericArg);

			var callGenericMethodResult = typeof(TestClass).CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(int)}, "hello");

			callGenericMethodResult.Should().BeOfType<string>();
			callGenericMethodResult.Should().Be("Int32 - hello");
		}

    }
}
