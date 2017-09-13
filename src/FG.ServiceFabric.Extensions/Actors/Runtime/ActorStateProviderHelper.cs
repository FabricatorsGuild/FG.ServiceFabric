using System;
using System.Fabric;
using System.Globalization;
using System.Reflection;
using Microsoft.ServiceFabric.Actors.Generator;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class ActorStateProviderHelper
    {
		private const long DefaultMaxPrimaryReplicationQueueSize = 8192;
		private const long DefaultMaxSecondaryReplicationQueueSize = 16384;

		public static IActorStateProvider CreateDefaultStateProvider(ActorTypeInformation actorTypeInfo)
        {
            var assembly = typeof(Microsoft.ServiceFabric.Actors.Runtime.ActorService).Assembly;

            var internalActorStateProviderHelperType = assembly.GetType("Microsoft.ServiceFabric.Actors.Runtime.ActorStateProviderHelper");

            var createDefaultStateProviderMethod = internalActorStateProviderHelperType.GetMethod("CreateDefaultStateProvider", BindingFlags.NonPublic | BindingFlags.Static);
            var actorStateProvider = (IActorStateProvider)createDefaultStateProviderMethod.Invoke(null, new object[] {actorTypeInfo});

            return actorStateProvider;
        }

		/// <summary>
		/// This is used by Kvs and Volatile actor state provider.
		/// </summary>
		/// <param name="codePackage"></param>
		/// <param name="actorImplType"></param>
		/// <returns></returns>
		public static ReplicatorSettings GetActorReplicatorSettings(CodePackageActivationContext codePackage, Type actorImplType)
		{
			var settings = ReplicatorSettings.LoadFrom(
				codePackage,
				ActorNameFormat.GetConfigPackageName(actorImplType),
				ActorNameFormat.GetFabricServiceReplicatorConfigSectionName(actorImplType));

			settings.SecurityCredentials = SecurityCredentials.LoadFrom(
				codePackage,
				ActorNameFormat.GetConfigPackageName(actorImplType),
				ActorNameFormat.GetFabricServiceReplicatorSecurityConfigSectionName(actorImplType));

			var nodeContext = FabricRuntime.GetNodeContext();
			var endpoint = codePackage.GetEndpoint(ActorNameFormat.GetFabricServiceReplicatorEndpointName(actorImplType));

			settings.ReplicatorAddress = string.Format(
				CultureInfo.InvariantCulture,
				"{0}:{1}",
				nodeContext.IPAddressOrFQDN,
				endpoint.Port);

			if (!settings.MaxPrimaryReplicationQueueSize.HasValue)
			{
				settings.MaxPrimaryReplicationQueueSize = DefaultMaxPrimaryReplicationQueueSize;
			}

			if (!settings.MaxSecondaryReplicationQueueSize.HasValue)
			{
				settings.MaxSecondaryReplicationQueueSize = DefaultMaxSecondaryReplicationQueueSize;
			}

			return settings;
		}
	}
}