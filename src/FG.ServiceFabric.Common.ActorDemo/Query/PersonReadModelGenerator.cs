using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                .For<IFirstNameUpdated>(e => ReadModel.FirstName = e.FirstName)
                .For<ILastNameUpdated>(e => ReadModel.LastName = e.LastName)
                .For<IMaritalStatusUpdated>(e => ReadModel.MaritalStatus = e.MaritalStatus)
                ;
        }
    }
}
