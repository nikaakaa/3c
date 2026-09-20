using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public enum RollbackInputProvenance : byte
    {
        LocalExplicit = 1,
        RelayedExplicit = 2,
        PredictedContinuous = 3,
        PredictedNeutral = 4,
        CanonicalExplicit = 5,
        ConfirmedExplicit = 6
    }

    public enum RollbackInputStage : byte
    {
        Captured = 1,
        RelayedExplicit = 2,
        Predicted = 3,
        Canonical = 4,
        Confirmed = 5
    }

    public readonly struct RollbackInputIdentity : IEquatable<RollbackInputIdentity>
    {
        public RollbackInputIdentity(
            ActorId actorId,
            SimulationTick tick,
            ulong inputSequence,
            StableHash gameplayHash)
        {
            if (!actorId.IsValid || !tick.IsValid || inputSequence == 0 || !gameplayHash.IsValid)
                throw new ArgumentException("Rollback input identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            InputSequence = inputSequence;
            GameplayHash = gameplayHash;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ulong InputSequence { get; }
        public StableHash GameplayHash { get; }
        public bool Equals(RollbackInputIdentity other) =>
            ActorId.Equals(other.ActorId) && Tick == other.Tick && InputSequence == other.InputSequence &&
            GameplayHash.Equals(other.GameplayHash);
        public override bool Equals(object obj) => obj is RollbackInputIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ActorId, Tick, InputSequence, GameplayHash);
    }

    public sealed class RollbackActorInputFrame
    {
        public RollbackActorInputFrame(
            ActorId actorId,
            SimulationTick tick,
            ulong inputSequence,
            SimulationInput input,
            RollbackInputProvenance provenance)
        {
            if (!actorId.IsValid || !tick.IsValid || inputSequence == 0 || input == null ||
                !IsValidProvenance(provenance) ||
                input.NumericProfile != FixedSimulationNumericProfile.Value || input.Sequence != inputSequence ||
                input.TickSource.SourceTick != tick.Value)
            {
                throw new ArgumentException("Rollback Actor input frame is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            InputSequence = inputSequence;
            Input = input;
            Provenance = provenance;
            InputHash = RollbackInputCodec.ComputeInputHash(actorId, tick, input);
            GameplayHash = RollbackInputCodec.ComputeGameplayInputHash(actorId, tick, input);
            Identity = new RollbackInputIdentity(actorId, tick, inputSequence, GameplayHash);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ulong InputSequence { get; }
        public SimulationInput Input { get; }
        internal static bool IsValidProvenance(RollbackInputProvenance provenance) =>
            provenance == RollbackInputProvenance.LocalExplicit ||
            provenance == RollbackInputProvenance.RelayedExplicit ||
            provenance == RollbackInputProvenance.PredictedContinuous ||
            provenance == RollbackInputProvenance.PredictedNeutral ||
            provenance == RollbackInputProvenance.CanonicalExplicit ||
            provenance == RollbackInputProvenance.ConfirmedExplicit;

        public RollbackInputProvenance Provenance { get; }
        public StableHash InputHash { get; }
        public StableHash GameplayHash { get; }
        public RollbackInputIdentity Identity { get; }
        public bool IsExplicit =>
            Provenance == RollbackInputProvenance.LocalExplicit ||
            Provenance == RollbackInputProvenance.RelayedExplicit ||
            Provenance == RollbackInputProvenance.CanonicalExplicit ||
            Provenance == RollbackInputProvenance.ConfirmedExplicit;
    }

    public sealed class RollbackActorInputBatch : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackActorInputFrame> m_Frames;

        public RollbackActorInputBatch(IReadOnlyList<RollbackActorInputFrame> frames)
            : this(RollbackProtocolArray.Copy(frames), true)
        {
        }

        RollbackActorInputBatch(RollbackActorInputFrame[] frames, bool _)
        {
            Array.Sort(frames, (left, right) => left.Tick.CompareTo(right.Tick));
            if (frames.Length == 0 || frames[0] == null)
                throw new ArgumentException("Rollback input batch requires at least one frame.", nameof(frames));
            ActorId = frames[0].ActorId;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null || frames[i].ActorId != ActorId ||
                    frames[i].Provenance != RollbackInputProvenance.LocalExplicit ||
                    i > 0 && frames[i - 1].Tick.Value + 1 != frames[i].Tick.Value)
                {
                    throw new ArgumentException(
                        "Rollback input batch must contain one Actor's contiguous local explicit Tick range.",
                        nameof(frames));
                }
            }
            m_Frames = frames;
        }

        public static RollbackActorInputBatch FromOwnedFrames(RollbackActorInputFrame[] frames) =>
            new RollbackActorInputBatch(frames ?? throw new ArgumentNullException(nameof(frames)), true);

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.ActorInputBatch;
        public ActorId ActorId { get; }
        public IReadOnlyList<RollbackActorInputFrame> Frames => m_Frames;
    }

    public sealed class RollbackRelayedExplicitInputBatch : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackActorInputFrame> m_Frames;

        public RollbackRelayedExplicitInputBatch(IReadOnlyList<RollbackActorInputFrame> frames)
            : this(RollbackProtocolArray.Copy(frames), true)
        {
        }

        RollbackRelayedExplicitInputBatch(RollbackActorInputFrame[] frames, bool _)
        {
            Array.Sort(frames, (left, right) => left.Tick.CompareTo(right.Tick));
            if (frames.Length == 0 || frames[0] == null)
                throw new ArgumentException("Rollback relayed input batch requires at least one frame.", nameof(frames));
            ActorId = frames[0].ActorId;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null || frames[i].ActorId != ActorId ||
                    frames[i].Provenance != RollbackInputProvenance.RelayedExplicit ||
                    i > 0 && frames[i - 1].Tick.Value + 1 != frames[i].Tick.Value)
                {
                    throw new ArgumentException(
                        "Rollback relayed input batch must contain one Actor's contiguous explicit Tick range.",
                        nameof(frames));
                }
            }
            m_Frames = frames;
        }

        public static RollbackRelayedExplicitInputBatch FromOwnedFrames(RollbackActorInputFrame[] frames) =>
            new RollbackRelayedExplicitInputBatch(frames ?? throw new ArgumentNullException(nameof(frames)), true);

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.RelayedExplicitInputBatch;
        public ActorId ActorId { get; }
        public IReadOnlyList<RollbackActorInputFrame> Frames => m_Frames;
    }

    public sealed class RollbackCanonicalInputBundle : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackActorInputFrame> m_Actors;

        public RollbackCanonicalInputBundle(
            SimulationTick tick,
            ulong bundleSequence,
            IReadOnlyList<RollbackActorInputFrame> actors)
            : this(tick, bundleSequence, CopyActors(tick, bundleSequence, actors), true)
        {
        }

        RollbackCanonicalInputBundle(
            SimulationTick tick,
            ulong bundleSequence,
            RollbackActorInputFrame[] actors,
            bool _)
        {
            Array.Sort(actors, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (actors.Length == 0)
                throw new ArgumentException("Rollback canonical bundle requires an Actor roster.", nameof(actors));
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] == null || actors[i].Tick != tick ||
                    i > 0 && actors[i - 1].ActorId.Equals(actors[i].ActorId))
                {
                    throw new ArgumentException("Rollback canonical bundle Actor order or Tick is invalid.", nameof(actors));
                }
            }
            Tick = tick;
            BundleSequence = bundleSequence;
            m_Actors = actors;
            BundleHash = RollbackInputCodec.ComputeBundleHash(this);
            GameplayHash = RollbackInputCodec.ComputeGameplayBundleHash(this);
        }

        public static RollbackCanonicalInputBundle FromOwnedActors(
            SimulationTick tick,
            ulong bundleSequence,
            RollbackActorInputFrame[] actors)
        {
            RequireIdentity(tick, bundleSequence);
            return new RollbackCanonicalInputBundle(
                tick,
                bundleSequence,
                actors ?? throw new ArgumentNullException(nameof(actors)),
                true);
        }

        static RollbackActorInputFrame[] CopyActors(
            SimulationTick tick,
            ulong bundleSequence,
            IReadOnlyList<RollbackActorInputFrame> actors)
        {
            RequireIdentity(tick, bundleSequence);
            return RollbackProtocolArray.Copy(actors);
        }

        static void RequireIdentity(SimulationTick tick, ulong bundleSequence)
        {
            if (!tick.IsValid || bundleSequence == 0)
                throw new ArgumentException("Rollback canonical bundle identity is incomplete.");
        }

        public SimulationTick Tick { get; }
        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.CanonicalBundle;
        public ulong BundleSequence { get; }
        public IReadOnlyList<RollbackActorInputFrame> Actors => m_Actors;
        public StableHash BundleHash { get; }
        public StableHash GameplayHash { get; }

        public RollbackActorInputFrame GetRequired(ActorId actorId)
        {
            int low = 0;
            int high = m_Actors.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int comparison = m_Actors[middle].ActorId.CompareTo(actorId);
                if (comparison == 0)
                    return m_Actors[middle];
                if (comparison < 0)
                    low = middle + 1;
                else
                    high = middle - 1;
            }
            throw new KeyNotFoundException($"Canonical bundle '{Tick}' has no Actor '{actorId}'.");
        }
    }

    public sealed class RollbackCanonicalConfirmation : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackCanonicalInputBundle> m_FinalBundles;

        public RollbackCanonicalConfirmation(
            ulong previousConfirmedTick,
            SimulationTick confirmedTick,
            IReadOnlyList<RollbackCanonicalInputBundle> finalBundles)
            : this(
                previousConfirmedTick,
                confirmedTick,
                CopyFinalBundles(previousConfirmedTick, confirmedTick, finalBundles),
                true)
        {
        }

        RollbackCanonicalConfirmation(
            ulong previousConfirmedTick,
            SimulationTick confirmedTick,
            RollbackCanonicalInputBundle[] finalBundles,
            bool _)
        {
            Array.Sort(finalBundles, (left, right) => left.Tick.CompareTo(right.Tick));
            ulong expectedCount = confirmedTick.Value - previousConfirmedTick;
            if ((ulong)finalBundles.Length != expectedCount)
                throw new ArgumentException("Rollback canonical confirmation does not cover its complete Tick range.");
            for (int i = 0; i < finalBundles.Length; i++)
            {
                ulong expectedTick = checked(previousConfirmedTick + (ulong)i + 1);
                if (finalBundles[i] == null || finalBundles[i].Tick.Value != expectedTick)
                    throw new ArgumentException("Rollback canonical confirmation bundle order is invalid.");
                for (int actorIndex = 0; actorIndex < finalBundles[i].Actors.Count; actorIndex++)
                {
                    if (finalBundles[i].Actors[actorIndex].Provenance != RollbackInputProvenance.CanonicalExplicit &&
                        finalBundles[i].Actors[actorIndex].Provenance != RollbackInputProvenance.ConfirmedExplicit)
                        throw new ArgumentException("Rollback canonical confirmation contains predicted input.");
                }
            }
            PreviousConfirmedTick = previousConfirmedTick;
            ConfirmedTick = confirmedTick;
            m_FinalBundles = finalBundles;
        }

        public static RollbackCanonicalConfirmation FromOwnedBundles(
            ulong previousConfirmedTick,
            SimulationTick confirmedTick,
            RollbackCanonicalInputBundle[] finalBundles)
        {
            RequireRange(previousConfirmedTick, confirmedTick);
            return new RollbackCanonicalConfirmation(
                previousConfirmedTick,
                confirmedTick,
                finalBundles ?? throw new ArgumentNullException(nameof(finalBundles)),
                true);
        }

        static RollbackCanonicalInputBundle[] CopyFinalBundles(
            ulong previousConfirmedTick,
            SimulationTick confirmedTick,
            IReadOnlyList<RollbackCanonicalInputBundle> finalBundles)
        {
            RequireRange(previousConfirmedTick, confirmedTick);
            return RollbackProtocolArray.Copy(finalBundles);
        }

        static void RequireRange(ulong previousConfirmedTick, SimulationTick confirmedTick)
        {
            if (!confirmedTick.IsValid || confirmedTick.Value <= previousConfirmedTick)
                throw new ArgumentException("Rollback canonical confirmation range is invalid.");
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.CanonicalConfirmation;
        public ulong PreviousConfirmedTick { get; }
        public SimulationTick ConfirmedTick { get; }
        public IReadOnlyList<RollbackCanonicalInputBundle> FinalBundles => m_FinalBundles;
    }

    static class RollbackProtocolArray
    {
        public static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.Count == 0)
                return Array.Empty<T>();
            var values = new T[source.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = source[i];
            return values;
        }
    }
}
