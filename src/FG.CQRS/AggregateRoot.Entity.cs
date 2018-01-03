namespace FG.CQRS
{
    public abstract partial class AggregateRoot<TAggregateRootEventInterface>
    {
        public abstract class
            Entity<TAggregateRoot, TEntityEventInterface> : Component<TAggregateRoot, TEntityEventInterface>
            where TEntityEventInterface : class, TAggregateRootEventInterface
            where TAggregateRoot : AggregateRoot<TAggregateRootEventInterface>
        {
            protected Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot)
            {
            }
        }
    }
}