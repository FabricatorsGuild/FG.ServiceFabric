namespace FG.ServiceFabric.Services.Runtime.State
{
	public enum ReliableStateChangeKind
	{
		None = 0,
		Add = 1,
		Update = 2,
		AddOrUpdate = 3,
		Remove = 4,
		Enqueue = 5,
		Dequeue = 6,
	}
}