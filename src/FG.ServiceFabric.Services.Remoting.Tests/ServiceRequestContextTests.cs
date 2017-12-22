namespace FG.ServiceFabric.Services.Remoting.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.Common.Extensions;
    using FG.ServiceFabric.Services.Remoting.FabricTransport;

    using NUnit.Framework;

    [TestFixture]
    public class ServiceRequestContextTests
    {
        [TearDown]
        public void ResetContext()
        {
            ServiceRequestContext.Current.Clear();
        }

        [Test]
        public void TestClear()
        {
            var context = ServiceRequestContext.Current;
            Assert.IsTrue(context.Properties.Any() == false);

            var propertyName = "propertyName";
            var propertyValue = "propertyValue";

            context[propertyName] = propertyValue;

            Assert.IsTrue(context.Properties.Count == 1);

            context.Clear();

            Assert.IsTrue(context.Properties.Count == 0);
        }

        [Test]
        public async Task TestCopyOnWriteAndBranchImmutabilityAndIsolation()
        {
            ServiceRequestContext.Current.Update(d => d.Add("a", "a1").Add("b", "b1"));
            Assert.AreEqual(2, ServiceRequestContext.Current.Properties.Count);

            var semaphore = new SemaphoreSlim(0);

            var t1 = Task.Run(
                async () =>
                    {
                        // task part 1
                        await Task.Delay(30);
                        Assert.AreEqual(2, ServiceRequestContext.Current.Properties.Count);
                        ServiceRequestContext.Current.Update(d => d.Add("c", "c1").Add("d", "d1"));
                        Assert.AreEqual(4, ServiceRequestContext.Current.Properties.Count);

                        semaphore.Release();

                        // after this wait, the test task will clear it's properties
                        await semaphore.WaitAsync();

                        Assert.AreEqual(4, ServiceRequestContext.Current.Properties.Count);
                        semaphore.Release();
                    });

            // Wait for part 1 of the task to complete
            await semaphore.WaitAsync();
            Assert.AreEqual(2, ServiceRequestContext.Current.Properties.Count);
            ServiceRequestContext.Current.Update(d => d.Clear());
            semaphore.Release();

            // Wait for part 2 of the task to complete
            await semaphore.WaitAsync();
        }

        [Test]
        public void TestCurrent()
        {
            var context = ServiceRequestContext.Current;
            Assert.IsNotNull(context);
        }

        [Test]
        public void TestIndex()
        {
            var context = ServiceRequestContext.Current;
            Assert.IsTrue(context.Properties.Any() == false);

            var propertyName = "propertyName";
            var propertyValue = "propertyValue";

            context[propertyName] = propertyValue;

            Assert.IsTrue(context.Properties.Count == 1);

            Assert.AreEqual(propertyValue, context[propertyName]);
        }

        [Test]
        public void TestKeys()
        {
            var context = ServiceRequestContext.Current;

            var keys = context.Keys;
            Assert.IsTrue(keys.Any() == false);

            var propertyName = "propertyName";
            var propertyValue = "propertyValue";

            context[propertyName] = propertyValue;

            // Ensure the key is not added to the keys collection we fetched earlier
            Assert.IsTrue(keys.Any() == false);

            keys = context.Keys;
            Assert.IsTrue(keys.Any(key => string.Compare(key, propertyName, StringComparison.OrdinalIgnoreCase) == 0));
        }

        [Test]
        public void TestProperties()
        {
            var context = ServiceRequestContext.Current;

            var properties = context.Properties;
            Assert.IsTrue(properties.Any() == false);

            var propertyName = "propertyName";
            var propertyValue = "propertyValue";

            context[propertyName] = propertyValue;

            // Ensure the property is not added to the keys collection we fetched earlier
            Assert.IsTrue(properties.Any() == false);

            properties = context.Properties;
            Assert.IsTrue(properties.Count == 1);

            Assert.IsTrue(
                properties.Any(
                    property => string.Compare(property.Key, propertyName, StringComparison.OrdinalIgnoreCase) == 0
                                && string.Compare(property.Value, propertyValue, StringComparison.OrdinalIgnoreCase) == 0));
        }

        [Test]
        public void TestUpdate()
        {
            var context = ServiceRequestContext.Current;
            Assert.IsTrue(context.Properties.Any() == false);

            context.Update(d => d.Add("a", "a1").Add("b", "b1"));

            Assert.IsTrue(context.Properties.Count == 2);

            context.Update(d => d.RemoveRange("a", "b"));

            Assert.IsTrue(context.Properties.Count == 0);
        }
    }
}