using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IHandleCommand<in TCommand>
        where TCommand : ICommand
    {
        Task Handle(TCommand command);
    }
}