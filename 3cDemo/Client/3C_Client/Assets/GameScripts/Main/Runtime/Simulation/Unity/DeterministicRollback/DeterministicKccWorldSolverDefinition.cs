using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicKcc;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [Serializable]
    public struct FixedRatioAuthoring
    {
        [SerializeField] long m_Numerator;
        [SerializeField] long m_Denominator;

        public FixedRatioAuthoring(long numerator, long denominator)
        {
            if (denominator == 0)
                throw new DivideByZeroException();
            m_Numerator = numerator;
            m_Denominator = denominator;
        }

        public FixedScalar Build(string field)
        {
            if (m_Denominator == 0)
                throw new InvalidOperationException($"Deterministic KCC field '{field}' has a zero denominator.");
            return FixedScalar.FromRatio(m_Numerator, m_Denominator);
        }
    }

    [CreateAssetMenu(fileName = "DeterministicKccWorldSolver", menuName = "3C/Simulation/Deterministic Rollback/KCC World Solver")]
    public sealed class DeterministicKccWorldSolverDefinition : SimulationWorldSolverDefinition
    {
        [SerializeField] DeterministicCollisionWorldAsset m_CollisionWorld;
        [SerializeField] FixedRatioAuthoring m_Radius = new FixedRatioAuthoring(35, 100);
        [SerializeField] FixedRatioAuthoring m_Height = new FixedRatioAuthoring(18, 10);
        [SerializeField] FixedRatioAuthoring m_SkinWidth = new FixedRatioAuthoring(5, 1000);
        [SerializeField] FixedRatioAuthoring m_MinimumGroundNormalY = new FixedRatioAuthoring(707106, 1000000);
        [SerializeField] FixedRatioAuthoring m_MaximumStepHeight = new FixedRatioAuthoring(3, 10);
        [SerializeField] FixedRatioAuthoring m_GroundSnapDistance = new FixedRatioAuthoring(12, 100);
        [SerializeField] FixedRatioAuthoring m_MaximumMovementDistance = new FixedRatioAuthoring(3, 1);
        [SerializeField] FixedRatioAuthoring m_QueryTolerance = new FixedRatioAuthoring(1, 100000);
        [SerializeField] FixedRatioAuthoring m_MinimumMovementDistance = new FixedRatioAuthoring(1, 100000);
        [SerializeField] FixedRatioAuthoring m_MinimumStepForwardDistance = new FixedRatioAuthoring(1, 100);
        [SerializeField] FixedRatioAuthoring m_NormalMergeDot = new FixedRatioAuthoring(9999, 10000);
        [SerializeField, Min(1)] int m_MaximumSweepIterations = 16;
        [SerializeField, Min(1)] int m_MaximumContactIterations = 8;
        [SerializeField, Min(1)] int m_MaximumCandidates = 256;
        [SerializeField, Min(1)] int m_MaximumContacts = 32;
        [SerializeField, Min(1)] int m_MaximumActorPairs = 64;
        [SerializeField, Range(1, 32)] int m_MaximumActorContactIterations = 8;

        public DeterministicCollisionWorldArtifact LoadCollisionWorld()
        {
            if (!m_CollisionWorld)
                throw new InvalidOperationException($"Deterministic KCC Definition '{name}' requires a Collision World Asset.");
            return m_CollisionWorld.Load();
        }

        public DeterministicKccConfiguration BuildConfiguration()
        {
            return new DeterministicKccConfiguration(
                m_Radius.Build(nameof(m_Radius)),
                m_Height.Build(nameof(m_Height)),
                m_SkinWidth.Build(nameof(m_SkinWidth)),
                m_MinimumGroundNormalY.Build(nameof(m_MinimumGroundNormalY)),
                m_MaximumStepHeight.Build(nameof(m_MaximumStepHeight)),
                m_GroundSnapDistance.Build(nameof(m_GroundSnapDistance)),
                m_MaximumMovementDistance.Build(nameof(m_MaximumMovementDistance)),
                m_QueryTolerance.Build(nameof(m_QueryTolerance)),
                m_MinimumMovementDistance.Build(nameof(m_MinimumMovementDistance)),
                m_MinimumStepForwardDistance.Build(nameof(m_MinimumStepForwardDistance)),
                m_NormalMergeDot.Build(nameof(m_NormalMergeDot)),
                m_MaximumSweepIterations,
                m_MaximumContactIterations,
                m_MaximumCandidates,
                m_MaximumContacts,
                m_MaximumActorPairs,
                m_MaximumActorContactIterations);
        }

        public override SimulationWorldSolverDefinitionDescriptor BuildDescriptor(int tickRate)
        {
            DeterministicCollisionWorldArtifact world = LoadCollisionWorld();
            DeterministicKccConfiguration configuration = BuildConfiguration();
            ThirdPersonSimulation.Fixed.CharacterWorldSolverDescriptor runtime = BuildRuntimeDescriptor();
            StableHash kccIdentity = BuildKccIdentity(tickRate, world, configuration);
            return new SimulationWorldSolverDefinitionDescriptor(
                new SimulationComponentIdentity(
                    SimulationComponentRole.WorldSolver,
                    DeterministicKccWorldSolver.SolverId,
                    DeterministicKccWorldSolver.SolverVersion,
                    kccIdentity),
                runtime.NumericProfile.Id,
                runtime.NumericProfile.AbiVersion,
                runtime.ImplementationId,
                runtime.Version,
                runtime.Capabilities,
                runtime.Features,
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore,
                true);
        }

        public override SimulationWorldIdentityDescriptor BuildWorldIdentity(
            int tickRate,
            SimulationWorldId worldId,
            string mapId,
            WorldRevision worldRevision)
        {
            DeterministicCollisionWorldArtifact world = LoadCollisionWorld();
            if (!string.Equals(world.MapId, mapId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Collision World MapId '{world.MapId}' does not match Session MapId '{mapId}'.");
            SimulationWorldSolverDefinitionDescriptor solver = BuildDescriptor(tickRate);
            return new SimulationWorldIdentityDescriptor(
                solver,
                worldId,
                mapId,
                worldRevision,
                world.ContentHash,
                StableHash.Compute("deterministic-kcc-navigation.none/1", world.ContentHash.Value),
                BuildConfiguration().ConfigurationHash);
        }

        public DeterministicKccWorldSolver CreateSolver(
            int tickRate,
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Deterministic KCC requires an Actor roster.", nameof(registrations));
            var bindings = new DeterministicKccWorldSolver.ActorBinding[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
                bindings[i] = new DeterministicKccWorldSolver.ActorBinding(registrations[i].ActorId, registrations[i].WorldBodyBindingId);
            return new DeterministicKccWorldSolver(
                tickRate,
                LoadCollisionWorld(),
                BuildConfiguration(),
                bindings);
        }

        public StableHash BuildKccIdentityHash(int tickRate) =>
            BuildKccIdentity(tickRate, LoadCollisionWorld(), BuildConfiguration());

        static ThirdPersonSimulation.Fixed.CharacterWorldSolverDescriptor BuildRuntimeDescriptor()
        {
            return new ThirdPersonSimulation.Fixed.CharacterWorldSolverDescriptor(
                FixedSimulationNumericProfile.Value,
                new SolverImplementationId(DeterministicKccWorldSolver.SolverId),
                DeterministicKccWorldSolver.SolverVersion,
                WorldCapability.BodyMotion |
                WorldCapability.Grounding |
                WorldCapability.Collision |
                WorldCapability.Reconstructible |
                WorldCapability.Snapshotable |
                WorldCapability.DeterministicReplay |
                WorldCapability.AirborneVerticalMotion,
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.WallSlide |
                WorldFeature.ActorCollision);
        }

        static StableHash BuildKccIdentity(
            int tickRate,
            DeterministicCollisionWorldArtifact world,
            DeterministicKccConfiguration configuration)
        {
            return DeterministicKccWorldSolver.ComputeIdentity(tickRate, world, configuration);
        }
    }
}
