using System;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FluentAssertions;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistence
{
    // ReSharper disable InconsistentNaming
    public class Service_fabric_ServiceName_tests
    {
        [Test]
        public void Parse_safe_serviceName_from_SF_serviceName()
        {
            var serviceName = new Uri("fabric:/sf.application.name/servicename.with.some.namespace");

            var name = StateSessionHelper.GetServiceName(serviceName);
            name.Should().Be("sf.application.name-servicename.with.some.namespace");
        }
    }

    // ReSharper restore InconsistentNaming
}