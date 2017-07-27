using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class ActorStateProviderExtensions
    {
        public static Task<object> LoadStateAsync(this IActorStateProvider actorStateProvider, Type stateType, ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
	        var methodInfo = typeof(IActorStateProvider).GetMethod("LoadStateAsync");
	        var genericMethodInfo = methodInfo.MakeGenericMethod(stateType);

	        var result = genericMethodInfo.Invoke(actorStateProvider, new object[] { actorId, stateName, cancellationToken });
	        return Task.FromResult(result);
        }

	    public static object GetTaskResult(this Task that, Type resultType)
	    {
		    var taskType = typeof(Task<>).MakeGenericType(resultType);

		    if (taskType.IsAssignableFrom(that.GetType()))
		    {
			    var resultProperty = taskType.GetProperty("Result");
			    var result = resultProperty.GetValue(that);

			    return result;
		    }
		    return null;
	    }
    }
}