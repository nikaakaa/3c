using System;
using System.Collections.Generic;
using System.Threading;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public static class RollbackPipelineRuntimePackageBuilder
    {
        public static SimulationPipelineDescriptor CreatePipeline(
            DeterministicRollbackModelPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            return new RollbackPipelinePassSet(policy).CreatePipeline();
        }

        public static SimulationPipelinePassFactoryCatalog CreatePortableFactoryCatalog(
            DeterministicRollbackModelPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            var passes = new RollbackPipelinePassSet(policy);
            return CreatePortableFactoryCatalog(passes);
        }

        public static FixedSimulationPipelineRuntimePackage Create(
            DeterministicRollbackModelPolicy policy,
            RollbackRuntimeState state)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            var passes = new RollbackPipelinePassSet(policy);
            SimulationPipelineDescriptor pipeline = passes.CreatePipeline();
            SimulationPipelinePassFactoryCatalog passFactoryCatalog = CreatePortableFactoryCatalog(passes);
            var runtimeFactories = new FixedPipelinePassRuntimeFactoryCatalog(new IFixedPipelinePassRuntimeFactory[]
            {
                new RollbackInputIngressPassRuntimeFactory(ResolveFactory(passFactoryCatalog, passes.Ingress), state),
                new RollbackSchedulePassRuntimeFactory(ResolveFactory(passFactoryCatalog, passes.Schedule), policy, state),
                new FixedProgramEvaluatePassRuntimeFactory(),
                new FixedWorldResolveBatchPassRuntimeFactory(),
                new FixedProgramFinalizePassRuntimeFactory(),
                new RollbackHistoryPassRuntimeFactory(ResolveFactory(passFactoryCatalog, passes.History), state),
                new RollbackHashEgressPassRuntimeFactory(ResolveFactory(passFactoryCatalog, passes.HashEgress), policy, state),
                new RollbackOutputDispositionPassRuntimeFactory(ResolveFactory(passFactoryCatalog, passes.OutputDisposition))
            });
            FixedPipelineProductRuntimeCatalog productFactories =
                FixedPassExecutionBackend.CreateProductRuntimeCatalog(new[]
                {
                    RollbackPipelineProducts.CreateRuntimeFactory()
                });
            return new FixedSimulationPipelineRuntimePackage(
                pipeline,
                passFactoryCatalog,
                runtimeFactories,
                productFactories);
        }

        static SimulationPipelinePassFactoryDescriptor ResolveFactory(
            SimulationPipelinePassFactoryCatalog catalog,
            SimulationPipelinePassDescriptor pass)
        {
            IReadOnlyList<SimulationPipelinePassFactoryDescriptor> candidates = catalog.FindFactories(pass.PassId);
            SimulationPipelinePassFactoryDescriptor match = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                SimulationPipelinePassFactoryDescriptor candidate = candidates[i];
                if (!candidate.Identity.ImplementationVersion.Equals(pass.ImplementationVersion))
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}@{pass.ImplementationVersion}' has multiple runtime factories.");
                match = candidate;
            }
            if (match == null)
                throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}@{pass.ImplementationVersion}' has no runtime factory.");
            if (match.Phase != pass.Phase || !match.SupportedConfigurationHash.Equals(pass.ConfigurationHash))
                throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}@{pass.ImplementationVersion}' runtime factory does not match its compiled descriptor.");
            return match;
        }

        static SimulationPipelinePassFactoryCatalog CreatePortableFactoryCatalog(
            RollbackPipelinePassSet passes)
        {
            var portableFactories = new List<SimulationPipelinePassFactoryDescriptor>
            {
                passes.CreateFactoryDescriptor(passes.Ingress),
                passes.CreateFactoryDescriptor(passes.Schedule),
                StandardFixedPipelinePassContracts.CreateFactoryDescriptor(StandardFixedPipelinePassContracts.ProgramEvaluate),
                StandardFixedPipelinePassContracts.CreateFactoryDescriptor(StandardFixedPipelinePassContracts.WorldResolveBatch),
                StandardFixedPipelinePassContracts.CreateFactoryDescriptor(StandardFixedPipelinePassContracts.ProgramFinalize),
                passes.CreateFactoryDescriptor(passes.History),
                passes.CreateFactoryDescriptor(passes.HashEgress),
                passes.CreateFactoryDescriptor(passes.OutputDisposition)
            };
            var products = new List<SimulationPipelineProductContract>(SimulationPipelineProducts.All)
            {
                RollbackPipelineProducts.Ingress
            };
            return new SimulationPipelinePassFactoryCatalog(
                FixedPassExecutionBackend.Descriptor.Identity,
                portableFactories,
                products);
        }
    }

    public sealed class DeterministicRollbackRuntimeLauncher : IFixedSimulationSessionRuntimeLauncher
    {
        readonly SimulationComponentIdentity m_SourceIdentity;
        int m_Launched;

        public DeterministicRollbackRuntimeLauncher(SimulationSessionSourceDescriptor source)
        {
            if (source == null || !source.Model.HasValue ||
                !string.Equals(source.Model.Value.ComponentId, DeterministicRollbackModelIdentity.ModelId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Rollback Runtime Launcher requires a DeterministicRollback Session Source.", nameof(source));
            }
            m_SourceIdentity = source.Identity;
            Descriptor = new FixedSimulationSessionRuntimeLauncherDescriptor(
                "thirdperson.simulation.runtime-launcher.deterministic-rollback",
                "1",
                source.NumericProfileId,
                source.TargetAbiVersion,
                StableHash.Compute(
                    "deterministic-rollback-runtime-launcher/1",
                    source.Identity.ToString(),
                    source.Model.Value.ToString(),
                    source.Protocol.Value.ToString()));
        }

        public FixedSimulationSessionRuntimeLauncherDescriptor Descriptor { get; }

        public FixedPassBackendCompositionResult Launch(FixedSimulationSessionCompositionRequest request)
        {
            if (Interlocked.Exchange(ref m_Launched, 1) != 0)
                throw new InvalidOperationException("Deterministic Rollback Runtime Launcher is single-use.");
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!request.Source.Identity.Equals(m_SourceIdentity) ||
                !request.PipelineRuntimePackage.Pipeline.PipelineId.Equals(
                    new SimulationPipelineId(DeterministicRollbackModelIdentity.PipelineId)) ||
                request.RestoreSource is not RollbackSnapshotRestoreSource ||
                request.Committer is not RollbackHistoryCommitter)
            {
                throw new InvalidOperationException("Deterministic Rollback composition is missing its formal Pipeline, restore Source, or Committer.");
            }
            if (!request.SolverDefinition.Deterministic ||
                (request.SolverDefinition.Capabilities & RequiredWorldCapabilities) != RequiredWorldCapabilities)
            {
                throw new InvalidOperationException("Deterministic Rollback composition requires a deterministic snapshotable World Solver.");
            }
            return FixedSimulationSessionComposer.Compose(request);
        }

        public static WorldCapability RequiredWorldCapabilities =>
            WorldCapability.BodyMotion |
            WorldCapability.Grounding |
            WorldCapability.Collision |
            WorldCapability.Reconstructible |
            WorldCapability.Snapshotable |
            WorldCapability.DeterministicReplay;
    }
}
