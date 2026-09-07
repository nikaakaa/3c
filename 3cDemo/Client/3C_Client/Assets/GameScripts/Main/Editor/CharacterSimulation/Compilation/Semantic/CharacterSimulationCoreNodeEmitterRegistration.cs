using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationCoreNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<RootNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Root)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<LoopNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)node.LoopStopType)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ParallelNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)node.Mode)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SequenceNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Sequence)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SelectorNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Selector)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SucceedNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Succeed)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<SubTreeNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.SubGraph)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateMachineNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateMachine, text0: node.Graph?.GraphAuthoringId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.State, text0: node.SubTree?.GraphAuthoringId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateMachineEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateEnter)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateMachineAnyStateNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateAny)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateMachineExitNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExit)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateOnEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnEnter)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateOnExitNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnExit)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateRootCompletedNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateRootCompleted)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<StateExitCauseInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)node.Cause)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<TimelineEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)node.EnterType)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<TimelineNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.Timeline,
                integer0: (int)node.PlaybackMode,
                text0: node.Timeline?.AuthoringId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ExposedPropertyNode>(node => new CharacterSimulationNodeEmission(
                node.NodeType == ExposedPropertyNodeType.Get ? SimulationOperationCode.BlackboardGet : SimulationOperationCode.BlackboardSet,
                integer0: (int)node.NodeType,
                text0: node.BlackboardVariable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("DeclarationOwner", node.BlackboardVariable.DeclarationOwnerId),
                    ("FactContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.FactContext))))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ConditionRuleResultNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.ConditionResult)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CompareNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Compare, integer0: (int)node.Comparison)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<AndNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.And)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<OrNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Or)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<NotNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Not)));
        }
    }
}
