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
            IReadOnlyList<SimulationActorBinding> roster,
            CommittedActorPoseSnapshot<Float32Vector3, Float32Yaw> committedObservation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (committedObservation is not CommittedActorObservationSnapshot float32Observation)
                throw new InvalidOperationException("Float32 Local Input requires the committed Float32 World Body observation projection.");
            return source.Read(
                tickSource,
                simulationTick,
                numericProfile,
                tickRate,
                roster,
                float32Observation);
        }
    }
}
