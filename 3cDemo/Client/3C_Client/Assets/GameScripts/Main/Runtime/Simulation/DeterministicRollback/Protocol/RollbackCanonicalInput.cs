using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            CharacterSimulationInput input,
            RollbackInputProvenance provenance)
        {
            if (!actorId.IsValid || !tick.IsValid || inputSequence == 0 || input == null ||
                !Enum.IsDefined(typeof(RollbackInputProvenance), provenance) ||
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
        public CharacterSimulationInput Input { get; }
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
        readonly ReadOnlyCollection<RollbackActorInputFrame> m_Frames;

        public RollbackActorInputBatch(IEnumerable<RollbackActorInputFrame> frames)
        {
            var values = new List<RollbackActorInputFrame>(frames ?? throw new ArgumentNullException(nameof(frames)));
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            if (values.Count == 0 || values[0] == null)
                throw new ArgumentException("Rollback input batch requires at least one frame.", nameof(frames));
            ActorId = values[0].ActorId;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || values[i].ActorId != ActorId ||
                    values[i].Provenance != RollbackInputProvenance.LocalExplicit ||
                    i > 0 && values[i - 1].Tick.Value + 1 != values[i].Tick.Value)
                {
                    throw new ArgumentException(
                        "Rollback input batch must contain one Actor's contiguous local explicit Tick range.",
                        nameof(frames));
                }
            }
            m_Frames = values.AsReadOnly();
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.ActorInputBatch;
        public ActorId ActorId { get; }
        public IReadOnlyList<RollbackActorInputFrame> Frames => m_Frames;
    }

    public sealed class RollbackRelayedExplicitInputBatch : IRollbackProtocolPayload
    {
        readonly ReadOnlyCollection<RollbackActorInputFrame> m_Frames;

        public RollbackRelayedExplicitInputBatch(IEnumerable<RollbackActorInputFrame> frames)
        {
            var values = new List<RollbackActorInputFrame>(frames ?? throw new ArgumentNullException(nameof(frames)));
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            if (values.Count == 0 || values[0] == null)
                throw new ArgumentException("Rollback relayed input batch requires at least one frame.", nameof(frames));
            ActorId = values[0].ActorId;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || values[i].ActorId != ActorId ||
                    values[i].Provenance != RollbackInputProvenance.RelayedExplicit ||
                    i > 0 && values[i - 1].Tick.Value + 1 != values[i].Tick.Value)
                {
                    throw new ArgumentException(
                        "Rollback relayed input batch must contain one Actor's contiguous explicit Tick range.",
                        nameof(frames));
                }
            }
            m_Frames = values.AsReadOnly();
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.RelayedExplicitInputBatch;
        public ActorId ActorId { get; }
        public IReadOnlyList<RollbackActorInputFrame> Frames => m_Frames;
    }

    public sealed class RollbackCanonicalInputBundle : IRollbackProtocolPayload
    {
        readonly ReadOnlyCollection<RollbackActorInputFrame> m_Actors;

        public RollbackCanonicalInputBundle(
            SimulationTick tick,
            ulong bundleSequence,
            IEnumerable<RollbackActorInputFrame> actors)
        {
            if (!tick.IsValid || bundleSequence == 0)
                throw new ArgumentException("Rollback canonical bundle identity is incomplete.");
            var values = new List<RollbackActorInputFrame>(actors ?? throw new ArgumentNullException(nameof(actors)));
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Rollback canonical bundle requires an Actor roster.", nameof(actors));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || values[i].Tick != tick ||
                    i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId))
                {
                    throw new ArgumentException("Rollback canonical bundle Actor order or Tick is invalid.", nameof(actors));
                }
            }
            Tick = tick;
            BundleSequence = bundleSequence;
            m_Actors = values.AsReadOnly();
            BundleHash = RollbackInputCodec.ComputeBundleHash(this);
            GameplayHash = RollbackInputCodec.ComputeGameplayBundleHash(this);
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
        readonly ReadOnlyCollection<RollbackCanonicalInputBundle> m_FinalBundles;

        public RollbackCanonicalConfirmation(
            ulong previousConfirmedTick,
            SimulationTick confirmedTick,
            IEnumerable<RollbackCanonicalInputBundle> finalBundles)
        {
            if (!confirmedTick.IsValid || confirmedTick.Value <= previousConfirmedTick)
                throw new ArgumentException("Rollback canonical confirmation range is invalid.");
            var values = new List<RollbackCanonicalInputBundle>(
                finalBundles ?? throw new ArgumentNullException(nameof(finalBundles)));
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            ulong expectedCount = confirmedTick.Value - previousConfirmedTick;
            if ((ulong)values.Count != expectedCount)
                throw new ArgumentException("Rollback canonical confirmation does not cover its complete Tick range.");
            for (int i = 0; i < values.Count; i++)
            {
                ulong expectedTick = checked(previousConfirmedTick + (ulong)i + 1);
                if (values[i] == null || values[i].Tick.Value != expectedTick)
                    throw new ArgumentException("Rollback canonical confirmation bundle order is invalid.");
                for (int actorIndex = 0; actorIndex < values[i].Actors.Count; actorIndex++)
                {
                    if (values[i].Actors[actorIndex].Provenance != RollbackInputProvenance.CanonicalExplicit &&
                        values[i].Actors[actorIndex].Provenance != RollbackInputProvenance.ConfirmedExplicit)
                        throw new ArgumentException("Rollback canonical confirmation contains predicted input.");
                }
            }
            PreviousConfirmedTick = previousConfirmedTick;
            ConfirmedTick = confirmedTick;
            m_FinalBundles = values.AsReadOnly();
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.CanonicalConfirmation;
        public ulong PreviousConfirmedTick { get; }
        public SimulationTick ConfirmedTick { get; }
        public IReadOnlyList<RollbackCanonicalInputBundle> FinalBundles => m_FinalBundles;
    }
}
