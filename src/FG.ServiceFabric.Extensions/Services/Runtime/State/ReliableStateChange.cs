using System;

namespace FG.ServiceFabric.Services.Runtime.State
{
	public class ReliableStateChange
	{
		//
		// Summary:
		//     Creates an instance of ReliableStateChange class.
		//
		// Parameters:
		//   schema:
		//     schema (collection/dictionary/queue)
		//
		//   stateName:
		//     Name of the actor state
		//
		//   type:
		//     Type of value associated with given actor state name.
		//
		//   value:
		//     Value associated with given actor state name.
		//
		//   changeKind:
		//     Kind of state change for given actor state name.
		public ReliableStateChange(string schema, Type type, object value, ReliableStateChangeKind changeKind)
		{
			Schema = schema;
			Type = type;
			Value = value;
			ChangeKind = changeKind;
		}

		//
		// Summary:
		//     Creates an instance of ReliableStateChange class.
		//
		// Parameters:
		//   schema:
		//     schema (collection/dictionary/queue)
		//
		//   stateName:
		//     Name of the actor state
		//
		//   type:
		//     Type of value associated with given actor state name.
		//
		//   value:
		//     Value associated with given actor state name.
		//
		//   changeKind:
		//     Kind of state change for given actor state name.
		public ReliableStateChange(string schema, string stateName, Type type, object value,
			ReliableStateChangeKind changeKind)
		{
			Schema = schema;
			StateName = stateName;
			Type = type;
			Value = value;
			ChangeKind = changeKind;
		}

		/// Summary:
		/// Gets the name of the schema (collection/dictionary/queue)
		public string Schema { get; }

		//
		// Summary:
		//     Gets name of the state.
		public string StateName { get; }

		//
		// Summary:
		//     Gets the type of value associated with given state name.
		public Type Type { get; }

		//
		// Summary:
		//     Gets the value associated with given state name.
		public object Value { get; }

		//
		// Summary:
		//     Gets the kind of change
		public ReliableStateChangeKind ChangeKind { get; }
	}
}