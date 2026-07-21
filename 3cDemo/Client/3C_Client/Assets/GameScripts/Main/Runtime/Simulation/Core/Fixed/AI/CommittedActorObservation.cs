using System;
using ThirdPersonSimulation;
using FixedCommittedActorPose = ThirdPersonSimulation.CommittedActorPose<ThirdPersonSimulation.Fixed.FixedVector3, ThirdPersonSimulation.Fixed.FixedYaw>;
using FixedCommittedActorPoseSnapshot = ThirdPersonSimulation.CommittedActorPoseSnapshot<ThirdPersonSimulation.Fixed.FixedVector3, ThirdPersonSimulation.Fixed.FixedYaw>;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedCommittedActorObservationSnapshot : FixedCommittedActorPoseSnapshot
    {
        public FixedCommittedActorObservationSnapshot(
            ulong observationTick,
            System.Collections.Generic.IEnumerable<FixedCommittedActorPose> actors)
            : base(observationTick, actors)
        {
        }
    }

    public interface IFixedCommittedActorObservationReadPort : ISimulationRuntimePort
    {
        FixedCommittedActorObservationSnapshot Read();
    }

    public sealed class FixedCommittedActorObservationReadPort : IFixedCommittedActorObservationReadPort
    {
        readonly SimulationWorldStateStore m_StateStore;

        public FixedCommittedActorObservationReadPort(
            SimulationComponentIdentity backend,
            SimulationWorldStateStore stateStore)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.CommittedObservation,
                FixedPipelineRuntimePortIds.CommittedObservationSchema,
                backend.ComponentId,
                CommittedActorPoseSchema.CapabilityHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }

        public FixedCommittedActorObservationSnapshot Read()
        {
            SimulationWorldStateSet state = m_StateStore.Current;
            var observations = new FixedCommittedActorPose[state.WorldState.Bodies.Count];
            for (int i = 0; i < observations.Length; i++)
            {
                WorldBodyState body = state.WorldState.Bodies[i];
                observations[i] = new FixedCommittedActorPose(body.ActorId, body.Position, body.Yaw);
            }
            return new FixedCommittedActorObservationSnapshot(state.LastCompletedTick, observations);
        }
    }
}
