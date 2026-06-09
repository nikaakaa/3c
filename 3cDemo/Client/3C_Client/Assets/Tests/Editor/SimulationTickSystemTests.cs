using System;
using System.Collections.Generic;
using NUnit.Framework;
using ThirdPersonInput;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Tests
{
    public sealed class SimulationTickSystemTests
    {
        [Test]
        public void TickEqualityUsesIntegerValue()
        {
            Assert.AreEqual(new SimulationTick(4), new SimulationTick(4));
            Assert.AreNotEqual(new SimulationTick(4), new SimulationTick(5));
        }

        [Test]
        public void TickComparisonUsesIntegerValue()
        {
            SimulationTick early = new SimulationTick(2);
            SimulationTick late = new SimulationTick(7);

            Assert.True(early < late);
            Assert.True(late > early);
            Assert.True(early <= new SimulationTick(2));
            Assert.True(late >= new SimulationTick(7));
        }

        [Test]
        public void TickOffsetProducesNewTick()
        {
            SimulationTick tick = new SimulationTick(10);

            Assert.AreEqual(new SimulationTick(13), tick.Add(3));
            Assert.AreEqual(new SimulationTick(8), tick.Subtract(2));
        }

        [Test]
        public void TickDifferenceIsSigned()
        {
            SimulationTick later = new SimulationTick(10);
            SimulationTick earlier = new SimulationTick(3);

            Assert.AreEqual(7, later.DifferenceFrom(earlier));
            Assert.AreEqual(-7, earlier.DifferenceFrom(later));
        }

        [Test]
        public void NegativeTickIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationTick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationTick.Zero.Subtract(1));
        }

        [Test]
        public void TickRateBuildsFixedDelta()
        {
            SimulationTickRate rate = new SimulationTickRate(60);

            Assert.AreEqual(60, rate.TicksPerSecond);
            Assert.AreEqual(1d / 60d, rate.FixedDeltaSeconds, 0.0000001d);
            Assert.AreEqual(SimulationTickRate.DefaultTicksPerSecond, SimulationTickRate.Default.TicksPerSecond);
        }

        [Test]
        public void InvalidTickRateIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationTickRate(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationTickRate(-30));
        }

        [Test]
        public void TickContextCarriesStableFacts()
        {
            SimulationTickContext context = new SimulationTickContext(new SimulationTick(12), new SimulationTickRate(30), SimulationTickRole.Server);

            Assert.AreEqual(12, context.TickValue);
            Assert.AreEqual(30, context.TickRate.TicksPerSecond);
            Assert.AreEqual(1d / 30d, context.FixedDeltaSeconds, 0.0000001d);
            Assert.AreEqual(SimulationTickRole.Server, context.Role);
        }

        [Test]
        public void AccumulatorEmitsNoTickWhenDeltaIsShort()
        {
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(new SimulationTickRate(60), 4);
            List<SimulationTickContext> ticks = new List<SimulationTickContext>();

            int emitted = accumulator.Accumulate(1d / 120d, ticks, SimulationTickRole.Client);

            Assert.AreEqual(0, emitted);
            Assert.AreEqual(0, ticks.Count);
            Assert.That(accumulator.AccumulatedSeconds, Is.GreaterThan(0d));
        }

        [Test]
        public void AccumulatorEmitsOneTickAtFixedDelta()
        {
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(new SimulationTickRate(60), 4);
            List<SimulationTickContext> ticks = new List<SimulationTickContext>();

            int emitted = accumulator.Accumulate(1d / 60d, ticks, SimulationTickRole.Client);

            Assert.AreEqual(1, emitted);
            Assert.AreEqual(new SimulationTick(0), ticks[0].Tick);
            Assert.AreEqual(new SimulationTick(1), accumulator.NextTick);
        }

        [Test]
        public void AccumulatorEmitsMultipleContinuousTicks()
        {
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(new SimulationTickRate(60), 8, new SimulationTick(10));
            List<SimulationTickContext> ticks = new List<SimulationTickContext>();

            int emitted = accumulator.Accumulate(3d / 60d, ticks, SimulationTickRole.Client);

            Assert.AreEqual(3, emitted);
            Assert.AreEqual(new SimulationTick(10), ticks[0].Tick);
            Assert.AreEqual(new SimulationTick(11), ticks[1].Tick);
            Assert.AreEqual(new SimulationTick(12), ticks[2].Tick);
            Assert.AreEqual(new SimulationTick(13), accumulator.NextTick);
        }

        [Test]
        public void AccumulatorCapsCatchUpTicksAndKeepsRemainder()
        {
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(new SimulationTickRate(60), 2);
            List<SimulationTickContext> ticks = new List<SimulationTickContext>();

            int first = accumulator.Accumulate(5d / 60d, ticks, SimulationTickRole.Client);

            Assert.AreEqual(2, first);
            Assert.AreEqual(2, ticks.Count);
            Assert.AreEqual(new SimulationTick(2), accumulator.NextTick);
            Assert.AreEqual(3d / 60d, accumulator.AccumulatedSeconds, 0.0000001d);

            int second = accumulator.Accumulate(0d, ticks, SimulationTickRole.Client);

            Assert.AreEqual(2, second);
            Assert.AreEqual(new SimulationTick(4), accumulator.NextTick);
            Assert.AreEqual(1d / 60d, accumulator.AccumulatedSeconds, 0.0000001d);
        }

        [Test]
        public void AccumulatorRejectsNegativeDelta()
        {
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(new SimulationTickRate(60), 4);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                accumulator.Accumulate(-0.01d, new List<SimulationTickContext>(), SimulationTickRole.Client));
        }

        [Test]
        public void RunnerUsesFixedPhaseOrder()
        {
            SimulationTickRunner runner = new SimulationTickRunner();
            RecordingHandler handler = new RecordingHandler();

            runner.Register(SimulationTickPhase.PresentationBridge, handler);
            runner.Register(SimulationTickPhase.ReadInput, handler);
            runner.Register(SimulationTickPhase.ExecuteMotion, handler);
            runner.Register(SimulationTickPhase.UpdateInputBuffer, handler);
            runner.Register(SimulationTickPhase.GameplayDecision, handler);
            runner.Register(SimulationTickPhase.WriteSnapshotAndEvents, handler);
            runner.Register(SimulationTickPhase.BuildMotion, handler);

            runner.Run(new SimulationTickContext(new SimulationTick(3), SimulationTickRate.Default, SimulationTickRole.Client));

            CollectionAssert.AreEqual(SimulationTickPhaseOrder.Phases, handler.Phases);
        }

        [Test]
        public void RunnerPassesSameContextToHandlers()
        {
            SimulationTickRunner runner = new SimulationTickRunner();
            RecordingHandler first = new RecordingHandler();
            RecordingHandler second = new RecordingHandler();
            SimulationTickContext context = new SimulationTickContext(new SimulationTick(9), new SimulationTickRate(20), SimulationTickRole.Server);

            runner.Register(SimulationTickPhase.ReadInput, first);
            runner.Register(SimulationTickPhase.ReadInput, second);

            int calls = runner.Run(in context);

            Assert.AreEqual(2, calls);
            Assert.AreEqual(9, first.Contexts[0].TickValue);
            Assert.AreEqual(9, second.Contexts[0].TickValue);
            Assert.AreEqual(SimulationTickRole.Server, first.Contexts[0].Role);
            Assert.AreEqual(20, second.Contexts[0].TickRate.TicksPerSecond);
        }

        [Test]
        public void RunnerSkipsEmptyPhases()
        {
            SimulationTickRunner runner = new SimulationTickRunner();
            RecordingHandler handler = new RecordingHandler();

            runner.Register(SimulationTickPhase.ExecuteMotion, handler);

            int calls = runner.Run(new SimulationTickContext(SimulationTick.Zero, SimulationTickRate.Default, SimulationTickRole.Client));

            Assert.AreEqual(1, calls);
            CollectionAssert.AreEqual(new[] { SimulationTickPhase.ExecuteMotion }, handler.Phases);
        }

        [Test]
        public void SameRunnerSetupBuildsSamePhaseRecord()
        {
            List<SimulationTickPhase> first = RunPhaseRecord();
            List<SimulationTickPhase> second = RunPhaseRecord();

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void ManualServerDriverProducesServerTickContexts()
        {
            ManualSimulationTickDriver driver = new ManualSimulationTickDriver(new SimulationTickRate(30), SimulationTickRole.Server, new SimulationTick(20));

            SimulationTickContext first = driver.Step();
            SimulationTickContext second = driver.Step();

            Assert.AreEqual(new SimulationTick(20), first.Tick);
            Assert.AreEqual(new SimulationTick(21), second.Tick);
            Assert.AreEqual(SimulationTickRole.Server, first.Role);
            Assert.AreEqual(1d / 30d, second.FixedDeltaSeconds, 0.0000001d);
        }

        [Test]
        public void InputRequestBufferCanUseSimulationTickAsStep()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            InputBufferSettings settings = new InputBufferSettings(3, 3, 3, 3);

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), new SimulationTick(5), settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, new SimulationTick(7), out BufferedInputRequest request));
            Assert.AreEqual(5, request.OriginStep);
            Assert.AreEqual(8, request.ExpireStep);
            Assert.True(buffer.TryConsume(InputRequestKind.Attack, new SimulationTick(8), out _));
            Assert.False(buffer.TryConsume(InputRequestKind.Attack, new SimulationTick(8), out _));
        }

        [Test]
        public void TickCoreDoesNotNeedUnitySceneObjects()
        {
            SimulationTickRate rate = SimulationTickRate.Default;
            SimulationTickAccumulator accumulator = new SimulationTickAccumulator(rate, 4);
            ManualSimulationTickDriver driver = new ManualSimulationTickDriver(rate, SimulationTickRole.Server);
            SimulationTickRunner runner = new SimulationTickRunner();
            RecordingHandler handler = new RecordingHandler();
            List<SimulationTickContext> ticks = new List<SimulationTickContext>();

            runner.Register(SimulationTickPhase.ReadInput, handler);
            accumulator.Accumulate(rate.FixedDeltaSeconds, ticks, SimulationTickRole.Client);
            driver.Step(runner);

            Assert.AreEqual(1, ticks.Count);
            Assert.AreEqual(1, handler.Phases.Count);
        }

        static List<SimulationTickPhase> RunPhaseRecord()
        {
            SimulationTickRunner runner = new SimulationTickRunner();
            RecordingHandler handler = new RecordingHandler();

            runner.Register(SimulationTickPhase.ReadInput, handler);
            runner.Register(SimulationTickPhase.UpdateInputBuffer, handler);
            runner.Register(SimulationTickPhase.GameplayDecision, handler);
            runner.Run(new SimulationTickContext(new SimulationTick(1), SimulationTickRate.Default, SimulationTickRole.Client));

            return handler.Phases;
        }

        sealed class RecordingHandler : ISimulationTickPhaseHandler
        {
            public List<SimulationTickPhase> Phases { get; } = new List<SimulationTickPhase>();
            public List<SimulationTickContext> Contexts { get; } = new List<SimulationTickContext>();

            public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
            {
                Phases.Add(phase);
                Contexts.Add(context);
            }
        }
    }
}
