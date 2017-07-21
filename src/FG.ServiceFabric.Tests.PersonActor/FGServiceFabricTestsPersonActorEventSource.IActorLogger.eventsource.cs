/*******************************************************************************************
*  This class is autogenerated from the class ActorLogger
*  Do not directly update this class as changes will be lost on rebuild.
*******************************************************************************************/
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Tests.PersonActor
{
	internal sealed partial class FGServiceFabricTestsPersonActorEventSource
	{

		private const int FailedToSendMessageEventId = 1001;

		[Event(FailedToSendMessageEventId, Level = EventLevel.LogAlways, Message = "{4}", Keywords = Keywords.Actor)]
		private void FailedToSendMessage(
			bool autogenerated, 
			string machineName, 
			string actorId, 
			string serviceUri, 
			string message, 
			string source, 
			string exceptionTypeName, 
			string exception)
		{
			WriteEvent(
				FailedToSendMessageEventId, 
				autogenerated, 
				machineName, 
				actorId, 
				serviceUri, 
				message, 
				source, 
				exceptionTypeName, 
				exception);
		}

		[NonEvent]
		public void FailedToSendMessage(
			bool autogenerated, 
			string machineName, 
			Microsoft.ServiceFabric.Actors.ActorId actorId, 
			System.Uri serviceUri, 
			System.Exception ex)
		{
			if (this.IsEnabled())
			{
				FailedToSendMessage(
					autogenerated, 
					Environment.MachineName, 
					actorId.ToString(), 
					serviceUri.ToString(), 
					ex.Message, 
					ex.Source, 
					ex.GetType().FullName, 
					ex.AsJson());
			}
		}


		private const int MovedToDeadLettersEventId = 2002;

		[Event(MovedToDeadLettersEventId, Level = EventLevel.LogAlways, Message = "Moved To Dead Letters {2}", Keywords = Keywords.Actor)]
		public void MovedToDeadLetters(
			bool autogenerated, 
			string machineName, 
			int depth)
		{
			WriteEvent(
				MovedToDeadLettersEventId, 
				autogenerated, 
				machineName, 
				depth);
		}


	}
}