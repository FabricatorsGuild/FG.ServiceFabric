using System;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FluentAssertions;
using NUnit.Framework;

namespace FG.Common.Utils.Tests
{
	public class MockableServiceRegistrationTests
	{
		[Test]
		public void GetApplicaitonName_should_return_applicationinstance_name_from_uri()
		{
			var uri = new Uri("fabric:/my.sf.app/my.sf.service", UriKind.Absolute);
			var mockableServiceRegistration = new MockableServiceRegistration(new Type[0], typeof(StatefulService), null, null,
				null, null, false, uri, null);

			var applicationName = mockableServiceRegistration.GetApplicationName();

			applicationName.Should().Be("my.sf.app");
		}
	}
}