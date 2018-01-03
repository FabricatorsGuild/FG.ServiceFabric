using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.CQRS.Tests.AggregateRoot
{
    [TestFixture]
    public class When_applying_event
    {
        private static void Check_that_event_is_Raised_and_Applied<TEventStream, TDomain>
            (Action<TDomain> method, Action<TDomain> asserts)
            where TDomain : class, IEventStored, new()
            where TEventStream : class, IDomainEventStream, new()
        {
            var eventStream = new TEventStream();

            var domainEventControllerMock = new Mock<IDomainEventController>();
            domainEventControllerMock.Setup(_ => _.RaiseDomainEventAsync(It.IsAny<IDomainEvent>()))
                .Callback<IDomainEvent>(_ => eventStream.Append(_))
                .Returns(Task.CompletedTask);

            var domain = new TDomain();
            domain.Initialize(domainEventControllerMock.Object);

            // Perform the method
            method(domain);

            // Assert the result
            asserts(domain);

            // Recreate the domain
            domain = new TDomain();
            domain.Initialize(domainEventControllerMock.Object, eventStream.DomainEvents);

            // Asser the same result
            asserts(domain);
        }

        [Test]
        public void Then_events_are_dispatched_to_children_in_multiple_levels()
        {
            var aggregateRootId = Guid.NewGuid();

            Check_that_event_is_Raised_and_Applied<TestEventStream, TestAggregateRoot>(
                testAggregateRoot =>
                {
                    testAggregateRoot.Create(aggregateRootId);
                    testAggregateRoot.AddEntity("First level");
                    testAggregateRoot.EntityL1S[0].AddEntity(2);
                },
                testAggregateRoot =>
                {
                    testAggregateRoot.AggregateRootId.Should().Be(aggregateRootId);
                    testAggregateRoot.EntityL1S[0].Name.Should().Be("First level");
                    testAggregateRoot.EntityL1S[0].EntityL2S[0].Value.Should().Be(2);
                });
        }

        [Test]
        public void Then_single_event_can_be_dispatched_and_applied_on_multiple_levels()
        {
            var aggregateRootId = Guid.NewGuid();

            Check_that_event_is_Raised_and_Applied<TestEventStream, TestAggregateRoot>(
                testAggregateRoot =>
                {
                    testAggregateRoot.ForTestForceRaiseAnyEvent(
                        new TestCreatedEventCreatingAllChilrenInOneGoEvent
                        {
                            AggregateRootId = aggregateRootId,
                            Name = "First level",
                            Value = 2,
                            Entityl1Id = 1,
                            Entityl2Id = 1
                        });
                },
                testAggregateRoot =>
                {
                    testAggregateRoot.AggregateRootId.Should().Be(aggregateRootId);
                    testAggregateRoot.EntityL1S[0].Name.Should().Be("First level");
                    testAggregateRoot.EntityL1S[0].EntityL2S[0].Value.Should().Be(2);
                });
        }
    }
}