using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Domain;
using FG.ServiceFabric.Tests.Actor.Interfaces;

namespace FG.ServiceFabric.Tests.Actor.Query
{
    public class ReadModelGenerator : AggregateRootReadModelGenerator<PersonEventStream, IPersonEvent, PersonReadModel>
    {
        public ReadModelGenerator(IEventStreamReader<PersonEventStream> eventStreamReader) : base(eventStreamReader)
        {
            RegisterEventAppliers()
                .For<IPersonFirstNameUpdated>(e => ReadModel.FirstName = e.FirstName)
                .For<IMaritalStatusUpdated>(e => ReadModel.MaritalStatus = e.MaritalStatus)
                ;
        }
    }
}
