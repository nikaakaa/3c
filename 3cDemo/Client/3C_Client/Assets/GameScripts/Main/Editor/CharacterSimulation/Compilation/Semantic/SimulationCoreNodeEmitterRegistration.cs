using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationCoreNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<RootNode>(node => new SimulationNodeEmission(SimulationOperationCode.Root)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<LoopNode>(node => new SimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)node.LoopStopType)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ParallelNode>(node => new SimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)node.Mode)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<SequenceNode>(node => new SimulationNodeEmission(SimulationOperationCode.Sequence)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<SelectorNode>(node => new SimulationNodeEmission(SimulationOperationCode.Selector)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<SucceedNode>(node => new SimulationNodeEmission(SimulationOperationCode.Succeed)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<SubTreeNode>(node => new SimulationNodeEmission(SimulationOperationCode.SubGraph)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateMachineNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateMachine, text0: node.Graph?.GraphAuthoringId)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateNode>(node => new SimulationNodeEmission(SimulationOperationCode.State, text0: node.SubTree?.GraphAuthoringId)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateMachineEnterNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateEnter)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateMachineAnyStateNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateAny)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateMachineExitNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateExit)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateOnEnterNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateOnEnter)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateOnExitNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateOnExit)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateRootCompletedNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateRootCompleted)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<StateExitCauseInfoNode>(node => new SimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)node.Cause)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<TimelineEnterNode>(node => new SimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)node.EnterType)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<TimelineNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.Timeline,
                integer0: (int)node.PlaybackMode,
                text0: node.Timeline?.AuthoringId,
                constants: SimulationNodeEmitterRegistry.Fields(("ActionContext", SimulationNodeEmitterContext.AssetIdentity(node.ActionContext))))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ExposedPropertyNode>(node => new SimulationNodeEmission(
                node.NodeType == ExposedPropertyNodeType.Get ? SimulationOperationCode.BlackboardGet : SimulationOperationCode.BlackboardSet,
                integer0: (int)node.NodeType,
                text0: node.BlackboardVariable.DeclarationId,
                constants: SimulationNodeEmitterRegistry.Fields(
                    ("DeclarationOwner", node.BlackboardVariable.DeclarationOwnerId),
                    ("FactContext", SimulationNodeEmitterContext.AssetIdentity(node.FactContext))))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ConditionRuleResultNode>(node => new SimulationNodeEmission(SimulationOperationCode.ConditionResult)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CompareNode>(node => new SimulationNodeEmission(SimulationOperationCode.Compare, integer0: (int)node.Comparison)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<AndNode>(node => new SimulationNodeEmission(SimulationOperationCode.And)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<OrNode>(node => new SimulationNodeEmission(SimulationOperationCode.Or)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<NotNode>(node => new SimulationNodeEmission(SimulationOperationCode.Not)));
        }
    }
}
