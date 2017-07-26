using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors
{
    public interface IReliableMessageEndpoint<in TMessageBase>
    {
        Task HandleMessageAsync<TMessage>(TMessage message) where TMessage : TMessageBase;
    }
}