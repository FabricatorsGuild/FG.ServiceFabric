using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Data
{
    public interface IEventStore : IActorStateProvider
    {
    }

    public interface IUnitOfWorkParticipant
    {

    }

    public interface IEvent
    {
        Guid EventId { get; }
    }
    
    public class EventStore<TEvent> : WrappedActorStateProvider, IActorStateProvider where TEvent : IEvent
    {
        public EventStore(ActorTypeInformation actorTypeInfor, IActorStateProvider innerStateProvider = null) 
            : base(actorTypeInfor, innerStateProvider)
        { }

        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //todo:transactional unit of work
            //Publish all changes within unit of work.
            PublishEvents(stateChanges);
            //Store unit of work
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);

        }

        private void PublishEvents(IReadOnlyCollection<ActorStateChange> stateChanges)
        {
            foreach (var actorStateChange in stateChanges)
            {
                if (actorStateChange.Value is TEvent)
                {
                    if (actorStateChange.ChangeKind != StateChangeKind.Add)
                    {
                        throw new Exception(
                            $"Unexpected {nameof(actorStateChange.ChangeKind)}: {actorStateChange.ChangeKind}");
                    }

                    //todo:communicate with a service bus
                    Task.Run(() => new SomeBus().Publish(
                        new {actorStateChange.Type.Name, Type = actorStateChange.Type.FullName, Data = actorStateChange.Value}));
                }
            }
        }
        

        public class SomeBus
        {
            private readonly JsonSerializerSettings _settings;
            private const string BaseFolderPath = @"C:\Temp\";

            public SomeBus()
            {
                _settings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                };
            }
            private static string GetFolderPath()
            {
                var folder = Path.Combine(BaseFolderPath, "Bus");
                Directory.CreateDirectory(folder);
                return folder;
            }

            public void Publish(object message)
            {
                try
                {
                    var addData = JsonConvert.SerializeObject(message, Formatting.Indented, _settings);
                    File.WriteAllText(Path.Combine(GetFolderPath(), Guid.NewGuid() + ".json"), addData);

                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
    }
}