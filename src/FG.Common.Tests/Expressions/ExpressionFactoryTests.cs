using FG.Common.Expressions;
using NUnit.Framework;

namespace FG.Common.Tests.Expressions
{
    public class ExpressionFactoryTests
    {
        [Test]
        public void TestCreateInstance()
        {
            var testFactory = CreateInstanceFactory.CreateInstance<Test>();
            Assert.NotNull(testFactory);

            var instance = testFactory();
            Assert.NotNull(instance);
            Assert.IsAssignableFrom<Test>(instance);
        }

        [Test]
        public void TestCreateInstance_Interface_Implementation()
        {
            var testFactory = CreateInstanceFactory.CreateInstance<Test, ITest>();
            Assert.NotNull(testFactory);
        }

        public interface ITest
        {
        }

        private class Test : ITest
        {
        }
    }
}