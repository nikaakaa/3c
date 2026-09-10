using System;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedCommittedActorPoseReadPort : ICommittedActorPoseReadPort<FixedVector3, FixedYaw>
    {
        readonly SimulationWorldStateStore m_StateStore;

        public FixedCommittedActorPoseReadPort(
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

        public CommittedActorPoseSnapshot<FixedVector3, FixedYaw> Read()
        {
            SimulationWorldStateSet state = m_StateStore.Current;
            var observations = new CommittedActorPose<FixedVector3, FixedYaw>[state.WorldState.Bodies.Count];
            for (int i = 0; i < observations.Length; i++)
            {
                WorldBodyState body = state.WorldState.Bodies[i];
                observations[i] = new CommittedActorPose<FixedVector3, FixedYaw>(body.ActorId, body.Position, body.Yaw);
            }
            return new CommittedActorPoseSnapshot<FixedVector3, FixedYaw>(state.LastCompletedTick, observations);
        }
    }
}
