using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationActionNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterMoveFacingAngleInfoNode>(node => new SimulationNodeEmission(SimulationOperationCode.MoveFacingAngle)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ActionContextActiveInfoNode>(node => ActionContext(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ActionWindowActiveInfoNode>(node => ActionWindow(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CanActivateActionInfoNode>(node => CanActivate(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ActivateActionInstanceNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.ActivateActionInstance,
                text0: node.AdmissionProfile ? node.AdmissionProfile.ActionId : string.Empty,
                constants: SimulationNodeEmitterRegistry.Fields(
                    ("AdmissionProfile", SimulationNodeEmitterContext.AssetIdentity(node.AdmissionProfile)),
                    ("SourceInputRequest", node.SourceInputRequestId),
                    ("ConsumeSourceInputRequest", node.ConsumeSourceInputRequest),
                    ("TargetKey", node.TargetKey),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId),
                    ("ActionContext", SimulationNodeEmitterContext.AssetIdentity(
                        ((IActionContextAuthoring)node).ActionContext))))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<SubmitActionLifecycleTransitionNode>(node => Lifecycle(node)));
        }

        static SimulationNodeEmission ActionContext(IActionContextAuthoring node) =>
            new SimulationNodeEmission(
                SimulationOperationCode.ActionContextActive,
                text0: SimulationNodeEmitterContext.AssetIdentity(node.ActionContext));

        static SimulationNodeEmission ActionWindow(IActionWindowAuthoring node) =>
            new SimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive,
                text0: node.WindowType);

        static SimulationNodeEmission CanActivate(ICanActivateActionAuthoring node) =>
            new SimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: node.AdmissionProfile ? node.AdmissionProfile.ActionId : string.Empty,
                constants: SimulationNodeEmitterRegistry.Fields(
                    ("AdmissionProfile", SimulationNodeEmitterContext.AssetIdentity(node.AdmissionProfile)),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotDeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotOwnerId)));

        static SimulationNodeEmission Lifecycle(ISubmitActionLifecycleAuthoring node)
        {
            CharacterActionAuthoringRules.ValidateLifecycle(node.TransitionType);
            return new SimulationNodeEmission(
                SimulationOperationCode.SubmitActionLifecycle,
                integer0: (int)node.TransitionType,
                text0: node.Reason,
                constants: SimulationNodeEmitterRegistry.Fields(("ActionContext", SimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }
    }
}
