using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public static class StateMachinePorts
    {
        public const string StateIn = "StateIn";
        public const string StateOut = "StateOut";
    }

    [Serializable]
    public abstract class StateMachineControlNode : BaseNode
    {
#if UNITY_EDITOR
        public override NodeCapabilities Capabilities => base.Capabilities &
                                                         ~NodeCapabilities.Deletable &
                                                         ~NodeCapabilities.Copiable &
                                                         ~NodeCapabilities.Groupable &
                                                         ~NodeCapabilities.Stackable;
#endif
    }

    [Serializable]
    [NodeName("Enter")]
    [NodeColor(82, 214, 128)]
    [NodePath("Base/StateMachine/Enter")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    [Output(StateMachinePorts.StateOut, PortCapacity.Multi)]
    public sealed class StateMachineEnterNode : StateMachineControlNode
    {
    }

    [Serializable]
    [NodeName("Any State")]
    [NodeColor(80, 205, 210)]
    [NodePath("Base/StateMachine/AnyState")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    [Output(StateMachinePorts.StateOut, PortCapacity.Multi)]
    public sealed class StateMachineAnyStateNode : StateMachineControlNode
    {
    }

    [Serializable]
    [NodeName("Exit")]
    [NodeColor(240, 94, 94)]
    [NodePath("Base/StateMachine/Exit")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class StateMachineExitNode : StateMachineControlNode
    {
#if UNITY_EDITOR
        public override IEnumerable<FlowPortDeclaration> GetFlowPortDeclarations(BaseGraph owner)
        {
            yield return new FlowPortDeclaration(StateMachinePorts.StateIn, PortDirection.Input, PortCapacity.Multi);
        }
#endif
    }
}
