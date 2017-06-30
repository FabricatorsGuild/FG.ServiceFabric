using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IHandleDomainRequest<TRequestValue, in TDomainRequest>
        where TDomainRequest : IDomainRequest<TRequestValue>
    {
        Task<TRequestValue> Handle(TDomainRequest domainRequest);
    }
}