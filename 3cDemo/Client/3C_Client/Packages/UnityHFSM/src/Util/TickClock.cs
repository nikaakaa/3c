using System.Collections.Generic;

namespace UnityHFSM
{
	/// <summary>
	/// Simulation tick clock. The single owner advances it once per logic tick;
	/// all timers read the shared integer tick count, so timing is deterministic
	/// across replays and numeric targets.
	/// </summary>
	public sealed class TickClock
	{
		public long Ticks { get; private set; }

		public void Advance(long ticks = 1)
		{
			Ticks += ticks;
		}
	}

	/// <summary> A timer that can be bound to a <see cref="TickClock"/>. </summary>
	public interface ITickBound
	{
		void Bind(TickClock clock);
	}

	/// <summary> Exposes the timing object owned by a state or transition. </summary>
	public interface ITimerHolder
	{
		ITimer Timer { get; }
	}

	/// <summary>
	/// Implemented by state machines so a nested machine can share its parent's clock.
	/// </summary>
	public interface ITickClockHost
	{
		TickClock Clock { get; }
		List<ITickBound> BoundTimers { get; }
		void AdoptClock(TickClock shared);
	}
}
