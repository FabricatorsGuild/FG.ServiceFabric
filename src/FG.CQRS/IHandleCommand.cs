using System.Threading.Tasks;

namespace FG.CQRS
{
	public interface IHandleCommand<in TCommand>
		where TCommand : ICommand
	{
		Task Handle(TCommand command);
	}
}