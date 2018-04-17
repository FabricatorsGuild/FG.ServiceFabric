namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System;
    using System.Reflection;

    using Microsoft.ServiceFabric.Services.Runtime;

    internal static class MockServiceReflectionHelper
    {
        public static MethodInfo StatefulRunAsyncMethodInfo { get; } = GetRunAsync(typeof(StatefulServiceBase));

        public static MethodInfo StatelessRunAsync { get; } = GetRunAsync(typeof(StatelessService));

        public static MethodInfo GetRunAsync(Type type)
        {
            return type.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static MethodInfo GetRunAsync(bool isStateful)
        {
            return isStateful ? StatefulRunAsyncMethodInfo : StatelessRunAsync;
        }
    }
}