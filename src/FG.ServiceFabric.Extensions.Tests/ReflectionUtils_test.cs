using FluentAssertions;
using NUnit.Framework;

namespace FG.Common.Utils.Tests
{ 
	public class ReflectionUtils_test
	{
		
		[Test]
		public void ActivateInternal_should_create_instance_from_default_ctor()
		{
			var type = typeof(ReflectionUtils.InternallyActivatedClass);

			var instance = type.ActivateInternalCtor();

			instance.Should().NotBeNull();
			instance.Should().BeOfType<ReflectionUtils.InternallyActivatedClass>();
		}

		[Test]
		public void ActivateInternal_should_create_instance_from_default_ctor_with_args()
		{
			var type = typeof(ReflectionUtils.InternallyActivatedClass);

			var instance = type.ActivateInternalCtor("hello", 5);

			instance.Should().NotBeNull();
			instance.Should().BeOfType<ReflectionUtils.InternallyActivatedClass>();
		}
	}

	
}