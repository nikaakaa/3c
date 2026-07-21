using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public static class CommittedActorObservationSchema
    {
        public const string Id = "float32-committed-actor-observation";
        public const int Version = 1;
        public static readonly StableHash CapabilityHash = StableHash.Compute(Id, Version.ToString());
    }

    public readonly struct CommittedActorObservation
    {
        public CommittedActorObservation(ActorId actorId, WorldBodyState body)
        {
            if (!actorId.IsValid || body.ActorId != actorId)
                throw new ArgumentException("Committed Actor observation identity is invalid.");
            ActorId = actorId;
            Body = body;
        }

        public ActorId ActorId { get; }
        public WorldBodyState Body { get; }

        public static implicit operator CommittedActorPose<Float32Vector3, Float32Yaw>(
            CommittedActorObservation observation) =>
            new CommittedActorPose<Float32Vector3, Float32Yaw>(
                observation.ActorId,
                observation.Body.Position,
                observation.Body.Yaw);
    }

    public sealed class CommittedActorObservationSnapshot :
        CommittedActorPoseSnapshot<Float32Vector3, Float32Yaw>
    {
        readonly ReadOnlyCollection<CommittedActorObservation> m_Actors;
        readonly Dictionary<ActorId, int> m_Indices;

        public CommittedActorObservationSnapshot(
            ulong observationTick,
            IEnumerable<CommittedActorObservation> actors)
            : this(observationTick, Materialize(actors))
        {
        }

        CommittedActorObservationSnapshot(ulong observationTick, SnapshotValues snapshot)
            : base(observationTick, snapshot.Poses)
        {
            List<CommittedActorObservation> values = snapshot.Observations;
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            m_Indices = new Dictionary<ActorId, int>(values.Count);
            var identity = new string[values.Count + 2];
            identity[0] = CommittedActorObservationSchema.Id;
            identity[1] = CommittedActorObservationSchema.Version.ToString();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].ActorId.IsValid || values[i].Body.ActorId != values[i].ActorId ||
                    i > 0 && values[i - 1].ActorId == values[i].ActorId)
                {
                    throw new ArgumentException("Committed observation roster contains an invalid or duplicate ActorId.", "actors");
                }
                m_Indices.Add(values[i].ActorId, i);
                identity[i + 2] = values[i].ActorId.Value;
            }
            ObservationTick = observationTick;
            m_Actors = values.AsReadOnly();
            RosterHash = StableHash.Compute(identity);
        }

        static SnapshotValues Materialize(IEnumerable<CommittedActorObservation> actors)
        {
            var observations = actors == null
                ? new List<CommittedActorObservation>()
                : new List<CommittedActorObservation>(actors);
            var poses = new CommittedActorPose<Float32Vector3, Float32Yaw>[observations.Count];
            for (int i = 0; i < observations.Count; i++)
                poses[i] = observations[i];
            return new SnapshotValues(observations, poses);
        }

        sealed class SnapshotValues
        {
            public SnapshotValues(
                List<CommittedActorObservation> observations,
                CommittedActorPose<Float32Vector3, Float32Yaw>[] poses)
            {
                Observations = observations;
                Poses = poses;
            }

            public List<CommittedActorObservation> Observations { get; }
            public CommittedActorPose<Float32Vector3, Float32Yaw>[] Poses { get; }
        }

        public new ulong ObservationTick { get; }
        public new StableHash RosterHash { get; }
        public new IReadOnlyList<CommittedActorObservation> Actors => m_Actors;

        public bool TryGetActor(ActorId actorId, out CommittedActorObservation observation)
        {
            if (m_Indices.TryGetValue(actorId, out int index))
            {
                observation = m_Actors[index];
                return true;
            }
            observation = default;
            return false;
        }

        public new CommittedActorObservation GetRequiredActor(ActorId actorId)
        {
            if (!TryGetActor(actorId, out CommittedActorObservation observation))
                throw new InvalidOperationException($"Committed observation Tick '{ObservationTick}' has no Actor '{actorId}'.");
            return observation;
        }

        public static CommittedActorObservationSnapshot FromState(SimulationWorldStateSet state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            var observations = new CommittedActorObservation[state.WorldState.Bodies.Count];
            for (int i = 0; i < observations.Length; i++)
            {
                WorldBodyState body = state.WorldState.Bodies[i];
                observations[i] = new CommittedActorObservation(body.ActorId, body);
            }
            return new CommittedActorObservationSnapshot(state.LastCompletedTick, observations);
        }
    }

    public sealed class AIPerceptionDescriptor
    {
        readonly ReadOnlyCollection<ActorId> m_CandidateActorIds;

        public AIPerceptionDescriptor(IEnumerable<ActorId> candidateActorIds, bool distanceThenActorId)
        {
            var values = candidateActorIds == null
                ? new List<ActorId>()
                : new List<ActorId>(candidateActorIds);
            values.Sort();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || i > 0 && values[i - 1] == values[i])
                    throw new ArgumentException("AI perception candidate roster contains an invalid or duplicate ActorId.", nameof(candidateActorIds));
            }
            m_CandidateActorIds = values.AsReadOnly();
            DistanceThenActorId = distanceThenActorId;
            var identity = new string[values.Count + 3];
            identity[0] = "ai-perception-descriptor/1";
            identity[1] = distanceThenActorId ? "distance" : "actor-id";
            identity[2] = CommittedActorObservationSchema.CapabilityHash.ToString();
            for (int i = 0; i < values.Count; i++)
                identity[i + 3] = values[i].Value;
            SchemaHash = StableHash.Compute(identity);
        }

        public IReadOnlyList<ActorId> CandidateActorIds => m_CandidateActorIds;
        public bool DistanceThenActorId { get; }
        public StableHash SchemaHash { get; }
    }

    public sealed class AIPerceptionFrame
    {
        readonly ReadOnlyCollection<CommittedActorObservation> m_Candidates;

        AIPerceptionFrame(
            CommittedActorObservationSnapshot snapshot,
            CommittedActorObservation self,
            IEnumerable<CommittedActorObservation> candidates)
        {
            Snapshot = snapshot;
            Self = self;
            m_Candidates = new List<CommittedActorObservation>(candidates).AsReadOnly();
        }

        public CommittedActorObservationSnapshot Snapshot { get; }
        public CommittedActorObservation Self { get; }
        public IReadOnlyList<CommittedActorObservation> Candidates => m_Candidates;

        public bool TrySelectNearest(out CommittedActorObservation selected)
        {
            selected = default;
            if (m_Candidates.Count == 0)
                return false;
            double closest = double.MaxValue;
            for (int i = 0; i < m_Candidates.Count; i++)
            {
                Float32Vector3 delta = m_Candidates[i].Body.Position - Self.Body.Position;
                double distanceSquared = delta.X.Value * delta.X.Value +
                                         delta.Y.Value * delta.Y.Value +
                                         delta.Z.Value * delta.Z.Value;
                if (distanceSquared < closest ||
                    distanceSquared.Equals(closest) && m_Candidates[i].ActorId.CompareTo(selected.ActorId) < 0)
                {
                    closest = distanceSquared;
                    selected = m_Candidates[i];
                }
            }
            return selected.ActorId.IsValid;
        }

        public static AIPerceptionFrame Create(
            ActorId selfActorId,
            AIPerceptionDescriptor descriptor,
            CommittedActorObservationSnapshot snapshot)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            CommittedActorObservation self = snapshot.GetRequiredActor(selfActorId);
            var candidates = new List<CommittedActorObservation>(descriptor.CandidateActorIds.Count);
            for (int i = 0; i < descriptor.CandidateActorIds.Count; i++)
            {
                ActorId actorId = descriptor.CandidateActorIds[i];
                if (actorId == selfActorId)
                    throw new InvalidOperationException($"AI perception candidate '{actorId}' is the controlled Actor.");
                candidates.Add(snapshot.GetRequiredActor(actorId));
            }
            if (descriptor.DistanceThenActorId)
            {
                candidates.Sort((left, right) =>
                {
                    double leftDistance = DistanceSquared(self.Body.Position, left.Body.Position);
                    double rightDistance = DistanceSquared(self.Body.Position, right.Body.Position);
                    int comparison = leftDistance.CompareTo(rightDistance);
                    return comparison != 0 ? comparison : left.ActorId.CompareTo(right.ActorId);
                });
            }
            return new AIPerceptionFrame(snapshot, self, candidates);
        }

        static double DistanceSquared(Float32Vector3 source, Float32Vector3 target)
        {
            double x = target.X.Value - source.X.Value;
            double y = target.Y.Value - source.Y.Value;
            double z = target.Z.Value - source.Z.Value;
            return x * x + y * y + z * z;
        }
    }

    public interface IFloat32CommittedActorObservationReadPort : ISimulationRuntimePort
    {
        CommittedActorObservationSnapshot Read();
    }

    public sealed class Float32CommittedActorObservationReadPort : IFloat32CommittedActorObservationReadPort
    {
        readonly SimulationWorldStateStore m_StateStore;

        public Float32CommittedActorObservationReadPort(
            SimulationComponentIdentity backend,
            SimulationWorldStateStore stateStore)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.CommittedObservation,
                Float32PipelineRuntimePortIds.CommittedObservationSchema,
                backend.ComponentId,
                CommittedActorObservationSchema.CapabilityHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public CommittedActorObservationSnapshot Read() =>
            CommittedActorObservationSnapshot.FromState(m_StateStore.Current);
    }
}
