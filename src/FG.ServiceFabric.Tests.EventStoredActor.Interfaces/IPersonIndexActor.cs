using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.CQRS;
using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.EventStoredActor.Interfaces
{
	#region Contracts

	public interface IIndexActor : IReliableMessageReceiverActor, IActor
	{
		Task<IEnumerable<Guid>> ListCommandsAsync();
	}

	#endregion

	#region Commands

	[DataContract]
	public class IndexCommand : DomainCommandBase
	{
		[DataMember]
		public Guid PersonId { get; set; }
	}

	#endregion

	#region Models

	#endregion
}