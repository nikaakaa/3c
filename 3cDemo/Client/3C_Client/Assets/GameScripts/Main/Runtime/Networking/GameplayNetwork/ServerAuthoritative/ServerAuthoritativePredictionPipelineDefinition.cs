using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class ServerAuthoritativePipelinePassDefinition :
        SimulationPipelinePassDefinition,
        IFloat32SimulationPipelinePassRuntimeProvider
    {
        [SerializeField] ServerAuthoritativeHybridModelDefinition m_Model;

        protected ServerAuthoritativeHybridModelDefinition Model => m_Model
            ? m_Model
            : throw new InvalidOperationException($"ServerAuthoritative Pass '{name}' requires its Model Definition.");
        protected ServerAuthoritativeModelPolicy Policy => Model.Policy;

        protected sealed override SimulationPipelinePassDescriptor BuildPortableDescriptor(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion)
        {
            SimulationPipelinePassDescriptor descriptor = BuildCanonicalDescriptor(Policy);
            if (!descriptor.PassId.Equals(passId) || !descriptor.ImplementationVersion.Equals(implementationVersion))
                throw new InvalidOperationException($"ServerAuthoritative Pass '{name}' must use its canonical identity.");
            return descriptor;
        }

        protected abstract SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy);
        protected abstract IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy);
        public IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => BuildRuntimeFactory(Policy);
        public IReadOnlyList<IFloat32PipelineProductSlotFactory> CreateAdditionalProductSlotFactories() =>
            ServerAuthoritativePipelineProductSlots.All;

#if UNITY_EDITOR
        public void SetModel(ServerAuthoritativeHybridModelDefinition model)
        {
            m_Model = model ? model : throw new ArgumentNullException(nameof(model));
        }
#endif
    }

    static class ServerAuthoritativePipelineProductSlots
    {
        static readonly IReadOnlyList<IFloat32PipelineProductSlotFactory> s_All =
            new IFloat32PipelineProductSlotFactory[]
            {
                new Float32ExclusiveProductSlotFactory<OwnerCanonicalInputBatch>(ServerAuthoritativeProducts.OwnerCanonicalInputBatch, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<AuthoritativeObservationBatch>(ServerAuthoritativeProducts.AuthoritativeObservationBatch, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<AuthoritativeActorBaseline>(ServerAuthoritativeProducts.AuthoritativeActorBaseline, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<PredictionCorrectionDecision>(ServerAuthoritativeProducts.PredictionCorrectionDecision, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<AcceptedAuthorityInputBatch>(ServerAuthoritativeProducts.AcceptedAuthorityInputBatch, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<AuthorityReplicationBatch>(ServerAuthoritativeProducts.AuthorityReplicationBatch, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<RemotePresentationBatch>(ServerAuthoritativeProducts.RemotePresentationBatch, Float32PipelineProductLifetime.OuterTransaction),
                new Float32ExclusiveProductSlotFactory<SelectedRemoteBodyBatch>(ServerAuthoritativeProducts.SelectedRemoteBodyBatch, Float32PipelineProductLifetime.OuterTransaction)
            };

        public static IReadOnlyList<IFloat32PipelineProductSlotFactory> All => s_All;
    }

    [CreateAssetMenu(fileName = "ServerAuthoritativePredictionPipeline", menuName = "3C/Simulation/Server Authoritative Prediction Pipeline")]
    public sealed class ServerAuthoritativePredictionPipelineDefinition :
        SimulationPipelineDefinition,
        IFloat32SimulationPipelineRuntimePackageProvider
    {
        public override SimulationPipelineDescriptor BuildPortableDescriptor()
        {
            SimulationPipelineDescriptor descriptor = base.BuildPortableDescriptor();
            RequireIdentity(descriptor, ServerAuthoritativePipelineIdentity.PredictionPipelineId);
            RequirePasses(descriptor, SimulationPipelinePhase.Ingress,
                ServerAuthoritativePredictionPassIds.OwnerInputIngress,
                ServerAuthoritativePredictionPassIds.ObservationIngress);
            RequirePasses(descriptor, SimulationPipelinePhase.Schedule,
                ServerAuthoritativePredictionPassIds.CorrectionSchedule);
            RequirePasses(descriptor, SimulationPipelinePhase.Step,
                StandardFloat32PipelinePassContracts.ProgramEvaluate.PassId,
                StandardFloat32PipelinePassContracts.WorldResolveBatch.PassId,
                StandardFloat32PipelinePassContracts.ProgramFinalize.PassId);
            RequirePasses(descriptor, SimulationPipelinePhase.Egress,
                ServerAuthoritativePredictionPassIds.HistoryEgress,
                ServerAuthoritativePredictionPassIds.OutputDisposition,
                ServerAuthoritativePredictionPassIds.InputCommandEgress,
                ServerAuthoritativePredictionPassIds.RemotePresentationEgress);
            return descriptor;
        }

        public Float32SimulationPipelineRuntimePackage BuildRuntimePackage() =>
            Float32SimulationPipelineRuntimePackageBuilder.BuildPassAuthored(this);

        static void RequireIdentity(SimulationPipelineDescriptor descriptor, string pipelineId)
        {
            if (!string.Equals(descriptor.PipelineId.Value, pipelineId, StringComparison.Ordinal) ||
                !string.Equals(descriptor.Revision.Value, ServerAuthoritativePipelineIdentity.Revision, StringComparison.Ordinal) ||
                descriptor.SchemaVersion.Value != ServerAuthoritativePipelineIdentity.SchemaVersion)
            {
                throw new InvalidOperationException("ServerAuthoritative Prediction Pipeline identity is not canonical.");
            }
        }

        internal static void RequirePasses(
            SimulationPipelineDescriptor descriptor,
            SimulationPipelinePhase phase,
            params SimulationPipelinePassId[] expected)
        {
            IReadOnlyList<SimulationPipelinePassDescriptor> actual = descriptor.GetPhase(phase);
            if (actual.Count != expected.Length)
                throw new InvalidOperationException($"ServerAuthoritative Pipeline phase '{phase}' has another Pass count.");
            for (int i = 0; i < actual.Count; i++)
            {
                if (!actual[i].PassId.Equals(expected[i]))
                    throw new InvalidOperationException($"ServerAuthoritative Pipeline phase '{phase}' Pass {i} is not canonical.");
            }
        }
    }

}
