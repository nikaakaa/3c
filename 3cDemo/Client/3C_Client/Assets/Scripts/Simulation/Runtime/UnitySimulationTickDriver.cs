using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class UnitySimulationTickDriver : MonoBehaviour
    {
        [SerializeField, Min(1)] int ticksPerSecond = SimulationTickRate.DefaultTicksPerSecond;
        [SerializeField, Min(1)] int maxTicksPerFrame = 4;
        [SerializeField] bool runAutomatically;

        readonly List<SimulationTickContext> ticks = new List<SimulationTickContext>();
        SimulationTickAccumulator accumulator;

        public event Action<SimulationTickContext> TickProduced;
        public SimulationTickRunner Runner { get; } = new SimulationTickRunner();
        public bool RunAutomatically { get => runAutomatically; set => runAutomatically = value; }
        public SimulationTick NextTick => EnsureAccumulator().NextTick;

        void Awake()
        {
            ResetDriver(SimulationTick.Zero);
        }

        void Update()
        {
            if (runAutomatically)
                Advance(Time.deltaTime);
        }

        public int Advance(float deltaSeconds)
        {
            SimulationTickAccumulator activeAccumulator = EnsureAccumulator();
            ticks.Clear();
            int count = activeAccumulator.Accumulate(deltaSeconds, ticks, SimulationTickRole.Client);

            for (int i = 0; i < count; i++)
            {
                SimulationTickContext context = ticks[i];
                Runner.Run(in context);
                TickProduced?.Invoke(context);
            }

            return count;
        }

        public void ResetDriver(SimulationTick startTick)
        {
            accumulator = new SimulationTickAccumulator(BuildRate(), SafeMaxTicksPerFrame(), startTick);
        }

        SimulationTickAccumulator EnsureAccumulator()
        {
            if (accumulator == null)
                ResetDriver(SimulationTick.Zero);

            return accumulator;
        }

        SimulationTickRate BuildRate()
        {
            return new SimulationTickRate(ticksPerSecond < 1 ? 1 : ticksPerSecond);
        }

        int SafeMaxTicksPerFrame()
        {
            return maxTicksPerFrame < 1 ? 1 : maxTicksPerFrame;
        }
    }
}
