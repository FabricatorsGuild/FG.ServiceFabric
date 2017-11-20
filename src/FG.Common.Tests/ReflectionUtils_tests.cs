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

			var callGenericMethodResult =
				new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(Data2)}, "hello");

			callGenericMethodResult.Should().BeOfType<string>();
			callGenericMethodResult.Should().Be("Data2 - hello");
		}

		[Test]
		public void CallGenericMethod_for_non_generic_class_should_match_generic_method_arguments_with_constraints_exactly()
		{
			var methodWithOneGenericArgName = nameof(TestClass.MethodWithOneConstrainedGenericArg);

			var callGenericMethodResult =
				new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(Data2)}, "hello");

			callGenericMethodResult.Should().BeOfType<string>();
			callGenericMethodResult.Should().Be("Data2 - hello");
		}

		[Test]
		public void
			CallGenericMethod_for_non_generic_class_should_fail_generic_method_arguments_with_constraints_exactly_when_constraints_are_not_met()
		{
			var methodWithOneGenericArgName = nameof(TestClass.MethodWithOneConstrainedGenericArg);

			var callGenericMethodResult = (Action) (() =>
			{
				new TestClass().CallGenericMethod(methodWithOneGenericArgName, new Type[] {typeof(string)}, "hello");
			});

			callGenericMethodResult.ShouldThrow<ArgumentException>();
		}
	}

	public class TextUtils_test
	{
		[Test]
		public void Concat_should_not_throw_exception_for_empty_enumerable()
		{
			var collection = new string[0];

			var result = collection.Concat(" - ");

			result.Should().Be("");
		}

		[Test]
		public void Concat_for_single_item_should_return_item_without_glue()
		{
			var collection = new string[] {"first"};

			var result = collection.Concat(" - ");

			result.Should().Be("first");
		}

		[Test]
		public void Concat_for_two_items_should_return_item_with_glue()
		{
			var collection = new string[] {"first", "second"};

			var result = collection.Concat(" - ");

			result.Should().Be("first - second");
		}

		[Test]
		public void Concat_for_multiple_items_should_return_item_with_glue()
		{
			var collection = new string[] {"first", "second", "third", "fourth", "fifth"};

			var result = collection.Concat(" - ");

			result.Should().Be("first - second - third - fourth - fifth");
		}

		[Test]
		public void Concat_with_lamba_glue_should_not_throw_exception_for_empty_enumerable()
		{
			var collection = new string[0];

			var result = collection.Concat((a, b, i) => $"[{i}:{a?.Substring(0, 1)}|{b?.Substring(0, 1)}]");

			result.Should().Be("");
		}

		[Test]
		public void Concat_with_lamba_glue_for_single_item_should_return_item_without_glue()
		{
			var collection = new string[] {"first"};

			var result = collection.Concat((a, b, i) => $"[{i}:{a?.Substring(0, 1)}|{b?.Substring(0, 1)}]");

			result.Should().Be("first");
		}

		[Test]
		public void Concat_with_lamba_glue_for_two_items_should_return_item_with_glue()
		{
			var collection = new string[] {"first", "second"};

			var result = collection.Concat((a, b, i) => $"[{i}:{a?.Substring(0, 1)}|{b?.Substring(0, 1)}]");

			result.Should().Be("first[0:f|s]second");
		}

		[Test]
		public void Concat_with_lamba_glue_for_multiple_items_should_return_item_with_glue()
		{
			var collection = new string[] {"first", "second", "third", "fourth", "fifth"};

			var result = collection.Concat((a, b, i) => $"[{i}:{a?.Substring(0, 1)}|{b?.Substring(0, 1)}]");

			result.Should().Be("first[0:f|s]second[1:s|t]third[2:t|f]fourth[3:f|f]fifth");
		}
	}
}