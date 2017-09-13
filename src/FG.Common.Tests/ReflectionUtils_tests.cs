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
	public class Data
	{
		
	}

	public class Data2 : Data
	{
		
	}

	public class TestClass
	{
		public string MethodWithOneGenericArg<T>(string value)
		{
			return $"{typeof(T).Name} - {value}";
		}

		public string MethodWithOneConstrainedGenericArg<T>(string value)
			where T : Data
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

			var callGenericMethodResult = new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(Data2) }, "hello");

			callGenericMethodResult.Should().BeOfType<string>();
			callGenericMethodResult.Should().Be("Data2 - hello");
		}

		[Test]
		public void CallGenericMethod_for_non_generic_class_should_match_generic_method_arguments_with_constraints_exactly()
		{
			var methodWithOneGenericArgName = nameof(TestClass.MethodWithOneConstrainedGenericArg);

			var callGenericMethodResult = new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] { typeof(Data2) }, "hello");

			callGenericMethodResult.Should().BeOfType<string>();
			callGenericMethodResult.Should().Be("Data2 - hello");
		}

		[Test]
		public void CallGenericMethod_for_non_generic_class_should_fail_generic_method_arguments_with_constraints_exactly_when_constraints_are_not_met()
		{
			var methodWithOneGenericArgName = nameof(TestClass.MethodWithOneConstrainedGenericArg);

			var callGenericMethodResult = (Action)(() => { new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(string)}, "hello"); });

			callGenericMethodResult.ShouldThrow<ArgumentException>();
		}
	}

}
