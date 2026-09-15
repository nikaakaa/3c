using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public static class Float32CommittedActorPoseIngress
    {
        public static Float32LocalInputFrame Read(
            this IFloat32LocalInputSourcePort source,
            SimulationTickSourceIdentity tickSource,
            SimulationTick simulationTick,
            SimulationNumericProfile numericProfile,
            int tickRate,
            IReadOnlyList<Float32CharacterRuntimeActor> roster,
            CommittedActorPoseSnapshot<Float32Vector3, Float32Yaw> committedObservation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source.Read(
                tickSource,
                simulationTick,
                numericProfile,
                tickRate,
                roster,
                committedObservation);
        }
    }

    public sealed class Float32CommittedActorPoseReadPort : ICommittedActorPoseReadPort<Float32Vector3, Float32Yaw>
    {
        readonly SimulationWorldStateStore m_StateStore;

        public Float32CommittedActorPoseReadPort(
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
                CommittedActorPoseSchema.CapabilityHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }

        public CommittedActorPoseSnapshot<Float32Vector3, Float32Yaw> Read()
        {
            SimulationWorldStateSet state = m_StateStore.Current;
            var observations = new CommittedActorPose<Float32Vector3, Float32Yaw>[state.WorldState.Bodies.Count];
            for (int i = 0; i < observations.Length; i++)
            {
                WorldBodyState body = state.WorldState.Bodies[i];
                observations[i] = new CommittedActorPose<Float32Vector3, Float32Yaw>(body.ActorId, body.Position, body.Yaw);
            }
            return new CommittedActorPoseSnapshot<Float32Vector3, Float32Yaw>(state.LastCompletedTick, observations);
        }
    }
}
