namespace FG.Common.Tests.Expressions
{
    using FG.Common.Expressions;

    using NUnit.Framework;

    public class ExpressionFactoryTests
    {
        public interface ITest
        {
        }

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

        private class Test : ITest
        {
        }
    }
}
