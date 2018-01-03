using System;

namespace FG.CQRS
{
    public interface IHasIdentity
    {
        Guid Id { get; set; }
    }
}