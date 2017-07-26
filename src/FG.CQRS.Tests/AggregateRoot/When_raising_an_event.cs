using System;
using System.Threading.Tasks;
using FG.CQRS.Exceptions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace FG.CQRS.Tests.AggregateRoot
{
    [TestFixture]
    public class When_raising_an_event
    {
        private TestAggregateRoot _aggregateRoot;
        [SetUp]
        public void SetupDomain()
        {
            var eventStream = new TestEventStream();

            var domainEventControllerMock = new Mock<IDomainEventController>();
            domainEventControllerMock.Setup(_ => _.RaiseDomainEventAsync(It.IsAny<IDomainEvent>()))
                .Callback<IDomainEvent>(_ => eventStream.Append(_))
                .Returns(Task.CompletedTask);

            _aggregateRoot = new TestAggregateRoot();
            _aggregateRoot.Initialize(domainEventControllerMock.Object);
        }

        [Test]
        public void Then_exception_is_thrown_if_first_event_and_not_inheriting_from_IAggregateRootCreatedEvent()
        {
            _aggregateRoot.Invoking(d => d.AddEntity("foobar")).ShouldThrow<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_if_IAggregateRootCreatedEvent_is_missing_an_AggregateRootId()
        {
            _aggregateRoot.Invoking(d => d.ForTestForceRaiseAnyEvent(new TestCreatedEvent())).ShouldThrow<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_when_event_belongs_to_another_aggregate_root()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);
            _aggregateRoot.Invoking(d => d.ForTestForceRaiseAnyEvent(new MaliciousEvent { AggregateRootId = Guid.NewGuid() }))
                .ShouldThrow<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_when_no_applier_exists_for_that_event()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);
            _aggregateRoot.Invoking(d => d.ForTestForceRaiseAnyEvent(new UnhandledEvent()))
                .ShouldThrow<HandlerNotFoundException>();
        }
    }
}