using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "UnityCharacterControllerWorldSolver", menuName = "3C/Simulation/Unity CharacterController World Solver")]
    public sealed class UnityCharacterControllerWorldSolverDefinition : Float32WorldSolverDefinition
    {
        public const string ComponentId = "thirdperson.simulation.world-solver.unity-character-controller";
        public const string SemanticVersion = "2";
        static readonly SolverImplementationId s_ImplementationId =
            new SolverImplementationId("Unity.CharacterController.WorldSolver");

        public override SimulationWorldSolverDefinitionDescriptor BuildDescriptor(int tickRate)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            WorldCapability capabilities =
                WorldCapability.BodyMotion |
                WorldCapability.Grounding |
                WorldCapability.Collision |
                WorldCapability.Reconstructible |
                WorldCapability.AirborneVerticalMotion;
            WorldFeature features =
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.WallSlide;
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.WorldSolver,
                ComponentId,
                SemanticVersion,
                StableHash.Compute(
                    ComponentId,
                    SemanticVersion,
                    tickRate.ToString(),
                    s_ImplementationId.Value,
                    ((ulong)capabilities).ToString(),
                    ((ulong)features).ToString()));
            return new SimulationWorldSolverDefinitionDescriptor(
                identity,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion,
                s_ImplementationId,
                "2",
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
            SimulationWorldSolverDefinitionDescriptor solver = BuildDescriptor(tickRate);
            return new SimulationWorldIdentityDescriptor(
                solver,
                worldId,
                mapId,
                worldRevision,
                StableHash.Compute(
                    "unity-character-controller-world/2",
                    solver.Identity.ToString(),
                    worldId.Value,
                    mapId,
                    worldRevision.Value),
                StableHash.Compute("simulation-navigation-surface.none/1", solver.ImplementationId.Value),
                StableHash.Compute("simulation-query-profile.none/1", solver.ImplementationId.Value));
        }

        protected override ICharacterWorldSolver CreateSolverCore(
            int tickRate,
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Unity World Solver requires an Actor roster.", nameof(registrations));
            var bindings = new UnityCharacterControllerWorldBodyBinding[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
            {
                bindings[i] = registrations[i].WorldBodyBinding as UnityCharacterControllerWorldBodyBinding ??
                    throw new InvalidOperationException(
                        $"Unity CharacterController World Solver requires a CharacterController binding for Actor '{registrations[i].ActorId}'.");
            }
            return new UnityCharacterControllerWorldSolver(tickRate, bindings);
        }
    }
}
