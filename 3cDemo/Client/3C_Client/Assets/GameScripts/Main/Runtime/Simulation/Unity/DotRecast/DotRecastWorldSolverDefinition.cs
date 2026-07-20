using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecast;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "DotRecastWorldSolver", menuName = "3C/Simulation/DotRecast Navigation World Solver")]
    public sealed class DotRecastWorldSolverDefinition : Float32WorldSolverDefinition
    {
        public const string ComponentId = DotRecastWorldConfigurationIdentity.WorldSolverDefinitionComponentId;
        public const string SemanticVersion = DotRecastWorldConfigurationIdentity.WorldSolverDefinitionSemanticVersion;

        [SerializeField] NavigationSurfaceAsset m_NavigationSurface;
        [SerializeField] float m_ContactRadius;
        [SerializeField] float m_ContactHeight;
        [SerializeField] float m_ContactSkinWidth;
        [SerializeField] int m_ContactIterationCount;
        [SerializeField] float m_ContactTolerance;
        [SerializeField] float m_MaximumDepenetrationDistance;

        public NavigationSurfaceAsset NavigationSurface => m_NavigationSurface;
        public ActorContactShape ContactShape => BuildContactShape();
        public ActorContactSolverConfiguration ContactConfiguration => BuildContactConfiguration();

        public override SimulationWorldSolverDefinitionDescriptor BuildDescriptor(int tickRate)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (!m_NavigationSurface)
                throw new InvalidOperationException("DotRecast World Solver requires an explicit Navigation Surface asset.");
            NavigationSurfaceArtifact surface = m_NavigationSurface.Load();
            ActorContactShape contactShape = BuildContactShape();
            ActorContactSolverConfiguration contactConfiguration = BuildContactConfiguration();
            WorldCapability capabilities =
                WorldCapability.BodyMotion |
                WorldCapability.Grounding |
                WorldCapability.Collision |
                WorldCapability.Reconstructible;
            WorldFeature features =
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.ActorCollision |
                WorldFeature.NavigationSurface |
                WorldFeature.ObservedKinematicActorContact;
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.WorldSolver,
                ComponentId,
                SemanticVersion,
                DotRecastWorldConfigurationIdentity.ComputeSolverDefinition(
                    ComponentId,
                    SemanticVersion,
                    surface.WorldConfigurationHash,
                    contactShape,
                    contactConfiguration,
                    capabilities,
                    features));
            return new SimulationWorldSolverDefinitionDescriptor(
                identity,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion,
                new SolverImplementationId(DotRecastWorldSolver.ImplementationIdentity),
                DotRecastWorldSolver.SolverVersion,
                capabilities,
                features,
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore |
                SimulationPipelineExecutionSupport.Authoritative,
                false);
        }

        public override SimulationWorldIdentityDescriptor BuildWorldIdentity(
            int tickRate,
            SimulationWorldId worldId,
            string mapId,
            WorldRevision worldRevision)
        {
            NavigationSurfaceArtifact surface = m_NavigationSurface
                ? m_NavigationSurface.Load()
                : throw new InvalidOperationException("DotRecast World Solver requires an explicit Navigation Surface asset.");
            if (!string.Equals(surface.MapId, mapId, StringComparison.Ordinal) ||
                !string.Equals(surface.WorldRevision, worldRevision.Value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("DotRecast Navigation Surface does not match the Composition MapId or WorldRevision.");
            }
            return new SimulationWorldIdentityDescriptor(
                BuildDescriptor(tickRate),
                worldId,
                mapId,
                worldRevision,
                DotRecastWorldConfigurationIdentity.Compute(
                    surface.WorldConfigurationHash,
                    BuildContactShape(),
                    BuildContactConfiguration()),
                surface.ContentHash,
                surface.QueryProfile.ConfigurationHash);
        }

        protected override ICharacterWorldSolver CreateSolverCore(
            int tickRate,
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            BuildDescriptor(tickRate);
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("DotRecast World Solver requires an Actor roster.", nameof(registrations));
            var bindings = new DotRecastBodyBindingDescriptor[registrations.Count];
            ActorContactShape contactShape = BuildContactShape();
            for (int i = 0; i < registrations.Count; i++)
            {
                var binding = registrations[i].WorldBodyBinding as DotRecastStateWorldBodyBinding ??
                    throw new InvalidOperationException(
                        $"DotRecast World Solver requires a state-only binding for Actor '{registrations[i].ActorId}' and rejects CharacterController bindings.");
                binding.RequireValid();
                if (binding.ActorId != registrations[i].ActorId)
                    throw new InvalidOperationException($"DotRecast binding ActorId does not match registration '{registrations[i].ActorId}'.");
                if (binding.ContactShape != contactShape)
                    throw new InvalidOperationException($"DotRecast binding contact shape does not match the World Solver configuration for Actor '{registrations[i].ActorId}'.");
                bindings[i] = new DotRecastBodyBindingDescriptor(binding.BindingId, binding.InitialBody, binding.ContactShape);
            }
            return new DotRecastWorldSolver(
                tickRate,
                m_NavigationSurface.CopyCanonicalArtifact(),
                BuildContactConfiguration(),
                bindings);
        }

        ActorContactShape BuildContactShape()
        {
            return new ActorContactShape(
                Float32ScalarBoundary.ConvertExternal(m_ContactRadius, $"{name}/contact-radius"),
                Float32ScalarBoundary.ConvertExternal(m_ContactHeight, $"{name}/contact-height"),
                Float32ScalarBoundary.ConvertExternal(m_ContactSkinWidth, $"{name}/contact-skin-width"));
        }

        ActorContactSolverConfiguration BuildContactConfiguration()
        {
            return new ActorContactSolverConfiguration(
                m_ContactIterationCount,
                Float32ScalarBoundary.ConvertExternal(m_ContactTolerance, $"{name}/contact-tolerance"),
                Float32ScalarBoundary.ConvertExternal(
                    m_MaximumDepenetrationDistance,
                    $"{name}/maximum-depenetration-distance"));
        }
    }
}
