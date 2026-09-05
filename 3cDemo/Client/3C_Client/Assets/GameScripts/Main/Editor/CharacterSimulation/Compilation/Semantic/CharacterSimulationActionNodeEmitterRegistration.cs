using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationActionNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterMoveFacingAngleInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.MoveFacingAngle)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActionContextActiveInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionContextActive,
                text0: CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActionWindowActiveInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive,
                text0: node.WindowType)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CanActivateActionInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: node.ActionProfile ? node.ActionProfile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("ActionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionProfile)),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId)))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActivateActionInstanceNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActivateActionInstance,
                text0: node.ActionProfile ? node.ActionProfile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("ActionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionProfile)),
                    ("SourceInputRequest", node.SourceInputRequestId),
                    ("ConsumeSourceInputRequest", node.ConsumeSourceInputRequest),
                    ("TargetKey", node.TargetKey),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SubmitActionLifecycleTransitionNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.SubmitActionLifecycle,
                integer0: (int)node.TransitionType,
                text0: node.Reason,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))))));
        }
    }
}
