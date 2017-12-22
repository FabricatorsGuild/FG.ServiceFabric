namespace FG.ServiceFabric.Services.Remoting.Tests
{
    using System.Collections.Immutable;
    using System.Linq;

    using FG.ServiceFabric.Services.Remoting.FabricTransport;

    using NUnit.Framework;

    public class ServiceRequestContextWrapperTests
    {
        [Test]
        public void TestConstructorKeysAndDispose()
        {
            using (var wrapper = new ServiceRequestContextWrapper(new CustomServiceRequestHeader(ImmutableDictionary<string, string>.Empty.Add("a", "a1").Add("b", "b1"))))
            {
                Assert.IsTrue(wrapper.Keys.Contains("a") && wrapper.Keys.Contains("b"));
                Assert.IsTrue(ServiceRequestContext.Current.Keys.Contains("a") && ServiceRequestContext.Current.Keys.Contains("b"));
            }

            Assert.IsFalse(ServiceRequestContext.Current.Keys.Contains("a") || ServiceRequestContext.Current.Keys.Contains("b"));
        }

        [Test]
        public void TestCorrelationId()
        {
            using (var wrapper = new ServiceRequestContextWrapper(new CustomServiceRequestHeader(ImmutableDictionary<string, string>.Empty.Add("a", "a1").Add("b", "b1"))))
            {
                wrapper.CorrelationId = "abc123";
                Assert.AreEqual("abc123", wrapper.CorrelationId);
            }
        }
    }
}