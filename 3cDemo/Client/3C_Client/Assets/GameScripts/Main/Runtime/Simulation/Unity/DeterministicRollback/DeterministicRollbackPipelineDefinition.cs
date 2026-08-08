using System;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [CreateAssetMenu(fileName = "DeterministicRollbackPipeline", menuName = "3C/Simulation/Deterministic Rollback/Pipeline")]
    public sealed class DeterministicRollbackPipelineDefinition :
        SimulationPipelineDefinition,
        IFixedSimulationPipelineDefinition
    {
        [SerializeField, Min(0)] int m_OffensiveRequestDelayTicks = 2;
        [SerializeField, Min(2)] int m_HistoryLengthTicks = 180;
        [SerializeField, Min(1)] int m_HashCadenceTicks = 10;
        [SerializeField, Min(1)] int m_MaximumRollbackDepthTicks = 90;
        [SerializeField, Min(1)] int m_MaximumPredictionLeadTicks = 1;
        [SerializeField, Min(0)] int m_ConfirmationDelayTicks = 4;
        [SerializeField, Min(1)] int m_MaximumQueuedBundles = 256;
        [SerializeField, Min(2)] int m_MaximumQueuedSnapshots = 192;
        [SerializeField, Min(1)] int m_MaximumOutputRecords = 8192;
        [SerializeField] RollbackMissingInputPolicy m_MissingInputPolicy =
            RollbackMissingInputPolicy.ContinuousValuesWithEmptyRequests;
        [SerializeField] RollbackSnapshotAuthority m_SnapshotAuthority =
            RollbackSnapshotAuthority.LowestPeerId;

        public override SimulationPipelineDescriptor BuildPortableDescriptor() =>
            RollbackPipelineRuntimePackageBuilder.CreatePipeline(BuildPolicy());

        public DeterministicRollbackModelPolicy BuildPolicy()
        {
            return new DeterministicRollbackModelPolicy(
                m_OffensiveRequestDelayTicks,
                m_HistoryLengthTicks,
                m_HashCadenceTicks,
                m_MaximumRollbackDepthTicks,
                m_MaximumPredictionLeadTicks,
                m_ConfirmationDelayTicks,
                m_MaximumQueuedBundles,
                m_MaximumQueuedSnapshots,
                m_MaximumOutputRecords,
                m_MissingInputPolicy,
                m_SnapshotAuthority);
        }

        public FixedSimulationPipelineRuntimePackage BuildRuntimePackage(RollbackRuntimeState state)
        {
            return RollbackPipelineRuntimePackageBuilder.Create(
                BuildPolicy(),
                state ?? throw new ArgumentNullException(nameof(state)));
        }

        public SimulationPipelinePassFactoryCatalog BuildFixedPortableFactoryCatalog() =>
            RollbackPipelineRuntimePackageBuilder.CreatePortableFactoryCatalog(BuildPolicy());
    }
}
