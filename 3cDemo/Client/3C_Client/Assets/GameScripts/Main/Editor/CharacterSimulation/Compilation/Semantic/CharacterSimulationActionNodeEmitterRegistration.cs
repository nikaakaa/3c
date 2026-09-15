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
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActionContextActiveInfoNode>(node => ActionContext(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActionWindowActiveInfoNode>(node => ActionWindow(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CanActivateActionInfoNode>(node => CanActivate(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ActivateActionInstanceNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActivateActionInstance,
                text0: node.AdmissionProfile ? node.AdmissionProfile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("AdmissionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(node.AdmissionProfile)),
                    ("SourceInputRequest", node.SourceInputRequestId),
                    ("ConsumeSourceInputRequest", node.ConsumeSourceInputRequest),
                    ("TargetKey", node.TargetKey),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(
                        ((IActionContextAuthoring)node).ActionContext))))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SubmitActionLifecycleTransitionNode>(node => Lifecycle(node)));
        }

        static CharacterSimulationNodeEmission ActionContext(IActionContextAuthoring node) =>
            new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionContextActive,
                text0: CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext));

        static CharacterSimulationNodeEmission ActionWindow(IActionWindowAuthoring node) =>
            new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive,
                text0: node.WindowType);

        static CharacterSimulationNodeEmission CanActivate(ICanActivateActionAuthoring node) =>
            new CharacterSimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: node.AdmissionProfile ? node.AdmissionProfile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("AdmissionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(node.AdmissionProfile)),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotDeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotOwnerId)));

        static CharacterSimulationNodeEmission Lifecycle(ISubmitActionLifecycleAuthoring node)
        {
            CharacterActionAuthoringRules.ValidateLifecycle(node.TransitionType);
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.SubmitActionLifecycle,
                integer0: (int)node.TransitionType,
                text0: node.Reason,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }
    }
}
