namespace UnityHFSM
{
	public interface ITimer
	{
		long ElapsedTicks
		{
			get;
		}

		void Reset();
	}
}
