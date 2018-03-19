using System;
using System.Linq;
using System.Threading.Tasks;
using FG.CQRS.Exceptions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.CQRS.Tests.AggregateRoot
{
    [TestFixture]
    public class When_raising_an_event
    {
        [SetUp]
        public void SetupDomain()
        {
            _eventStream = new TestEventStream();

            var domainEventControllerMock = new Mock<IDomainEventController>();
            domainEventControllerMock.Setup(_ => _.RaiseDomainEventAsync(It.IsAny<IDomainEvent>()))
                .Callback<IDomainEvent>(_ => _eventStream.Append(_))
                .Returns(Task.CompletedTask);

            _aggregateRoot = new TestAggregateRoot();
            _aggregateRoot.Initialize(domainEventControllerMock.Object);
        }

        private TestAggregateRoot _aggregateRoot;
        private TestEventStream _eventStream;

        [Test]
        public void Then_create_event_has_version_1()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);

            _eventStream.DomainEvents.OfType<IAggregateRootCreatedEvent>().Single().Version.Should().Be(1);
        }

        [Test]
        public void Then_events_are_versioned_in_sequential_order()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);
            _aggregateRoot.ForTestForceRaiseAnyEvent(new TestEntityL1AddedEvent(1));
            _aggregateRoot.ForTestForceRaiseAnyEvent(new TestEntityL1AddedEvent(2));
            _aggregateRoot.ForTestForceRaiseAnyEvent(new TestEntityL1AddedEvent(3));

            _eventStream.DomainEvents.OfType<IAggregateRootEvent>().Select(e => e.Version)
                .Should().BeEquivalentTo(new[] {1, 2, 3, 4});
        }

        [Test]
        public void Then_exception_is_thrown_if_first_event_and_not_inheriting_from_IAggregateRootCreatedEvent()
        {
            _aggregateRoot.Invoking(d => d.AddEntity("foobar")).Should().Throw<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_if_IAggregateRootCreatedEvent_is_missing_an_AggregateRootId()
        {
            _aggregateRoot.Invoking(d => d.ForTestForceRaiseAnyEvent(new TestCreatedEvent()))
                .Should().Throw<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_when_event_belongs_to_another_aggregate_root()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);
            _aggregateRoot.Invoking(d =>
                    d.ForTestForceRaiseAnyEvent(new MaliciousEvent {AggregateRootId = Guid.NewGuid()}))
                .Should().Throw<AggregateRootException>();
        }

        [Test]
        public void Then_exception_is_thrown_when_no_applier_exists_for_that_event()
        {
            var aggregateRootId = Guid.NewGuid();

            _aggregateRoot.Create(aggregateRootId);
            _aggregateRoot.Invoking(d => d.ForTestForceRaiseAnyEvent(new UnhandledEvent()))
                .Should().Throw<HandlerNotFoundException>();
        }
    }
}