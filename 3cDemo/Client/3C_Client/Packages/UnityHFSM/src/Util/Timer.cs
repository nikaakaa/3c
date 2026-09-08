namespace UnityHFSM
{
	/// <summary>
	/// Default tick-driven timer. It measures elapsed ticks of the
	/// <see cref="TickClock"/> it is bound to; the owner advances the clock
	/// once per logic tick.
	/// </summary>
	public class Timer : ITimer, ITickBound
	{
		public long startTicks;
		private TickClock clock;

		public long ElapsedTicks => clock == null ? 0 : clock.Ticks - startTicks;

		public void Reset()
		{
			startTicks = clock == null ? 0 : clock.Ticks;
		}

		public void Bind(TickClock clock)
		{
			this.clock = clock;
		}

		public static bool operator >(Timer timer, long ticks) => timer.ElapsedTicks > ticks;
		public static bool operator <(Timer timer, long ticks) => timer.ElapsedTicks < ticks;
		public static bool operator >=(Timer timer, long ticks) => timer.ElapsedTicks >= ticks;
		public static bool operator <=(Timer timer, long ticks) => timer.ElapsedTicks <= ticks;
	}
}
