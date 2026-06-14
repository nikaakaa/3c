using System;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct LocalRollbackSoakInputConfig
    {
        public LocalRollbackSoakInputConfig(int seed, int tickCount)
        {
            Seed = seed;
            TickCount = tickCount < 0 ? 0 : tickCount;
        }

        public int Seed { get; }
        public int TickCount { get; }
    }

    public static class LocalRollbackSoakInputGenerator
    {
        public static PredictionInputFrame GenerateFrame(int seed, SimulationTick tick)
        {
            uint state = Mix((uint)seed, (uint)tick.Value);
            Vector2 move = ResolveMove(Next(ref state) % 9u);
            Vector2 look = ResolveLook(ref state);
            bool runHeld = (Next(ref state) % 5u) == 0u || (move.sqrMagnitude > 0f && (Next(ref state) % 7u) == 0u);
            PredictionButtonFrame dodge = Button(ref state, 17u);
            PredictionButtonFrame attack = Button(ref state, 23u);
            PredictionButtonFrame jump = Button(ref state, 37u);
            PredictionButtonFrame interact = Button(ref state, 53u);

            return new PredictionInputFrame(tick, move, look, runHeld, dodge, attack, jump, interact);
        }

        public static void Populate(
            in LocalRollbackSoakInputConfig config,
            PredictionInputHistory inputHistory,
            PredictionSnapshotHistory snapshotHistory,
            ILocalRollbackSynctestSimulation simulation)
        {
            if (inputHistory == null)
                throw new ArgumentNullException(nameof(inputHistory));
            if (snapshotHistory == null)
                throw new ArgumentNullException(nameof(snapshotHistory));
            if (simulation == null)
                throw new ArgumentNullException(nameof(simulation));

            SimulationTick zero = SimulationTick.Zero;
            CharacterSimulationSnapshot zeroSnapshot = simulation.CaptureSnapshot(zero);
            snapshotHistory.Write(zeroSnapshot);
            RollbackCameraBasisState cameraBasisState = zeroSnapshot.CameraBasisState;
            for (int tickValue = 1; tickValue <= config.TickCount; tickValue++)
            {
                SimulationTick tick = new SimulationTick(tickValue);
                PredictionInputFrame input = GenerateFrame(config.Seed, tick).WithCameraBasis(in cameraBasisState);
                inputHistory.Write(in input);
                simulation.Advance(in input);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(tick);
                snapshotHistory.Write(in snapshot);
                cameraBasisState = snapshot.CameraBasisState;
            }
        }

        static PredictionButtonFrame Button(ref uint state, uint cadence)
        {
            uint value = Next(ref state) % cadence;
            if (value == 0u)
                return new PredictionButtonFrame(true, true, false);
            if (value == 1u)
                return new PredictionButtonFrame(false, true, false);
            if (value == 2u)
                return new PredictionButtonFrame(false, false, true);

            return PredictionButtonFrame.None;
        }

        static Vector2 ResolveMove(uint value)
        {
            switch (value)
            {
                case 0u: return Vector2.zero;
                case 1u: return Vector2.up;
                case 2u: return Vector2.down;
                case 3u: return Vector2.left;
                case 4u: return Vector2.right;
                case 5u: return new Vector2(1f, 1f).normalized;
                case 6u: return new Vector2(-1f, 1f).normalized;
                case 7u: return new Vector2(1f, -1f).normalized;
                default: return new Vector2(-1f, -1f).normalized;
            }
        }

        static Vector2 ResolveLook(ref uint state)
        {
            float x = SignedUnit(Next(ref state));
            float y = SignedUnit(Next(ref state)) * 0.35f;
            if ((Next(ref state) % 4u) == 0u)
                return Vector2.zero;

            return new Vector2(x, y);
        }

        static float SignedUnit(uint value)
        {
            return ((value & 1023u) / 511.5f) - 1f;
        }

        static uint Mix(uint seed, uint tick)
        {
            uint value = seed ^ 0x9E3779B9u;
            value ^= tick + 0x85EBCA6Bu + (value << 6) + (value >> 2);
            return value == 0u ? 0xA341316Cu : value;
        }

        static uint Next(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }
    }
}
