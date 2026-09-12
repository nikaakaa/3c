#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowCanvas;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillSystemNode { }

    public abstract class BtsmtlSkillStateLifecycleFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillSystemNode
    {
        protected override void RegisterPorts() => AddFlowOutput("执行", "Output");
    }

    [Name("进入状态"), Category("BTSMTL/技能状态"), DoNotList]
    [BtsmtlSkillNodeKind("@onEnter")]
    public sealed class BtsmtlSkillStateOnEnterFlowNode : BtsmtlSkillStateLifecycleFlowNode
    {
    }

    [Name("退出状态"), Category("BTSMTL/技能状态"), DoNotList]
    [BtsmtlSkillNodeKind("@onExit")]
    public sealed class BtsmtlSkillStateOnExitFlowNode : BtsmtlSkillStateLifecycleFlowNode
    {
    }

    [Name("状态主体已完成"), Category("BTSMTL/技能条件")]
    [BtsmtlSkillNodeKind("state-root-completed")]
    public sealed class BtsmtlSkillStateRootCompletedFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        protected override void RegisterPorts() => AddValueOutput<bool>("已完成", RejectAuthoringValue<bool>, "m_Output");
    }

    public enum BtsmtlSkillStateExitCause
    {
        StateTransition = 0,
        TreeSelfAbort = 1,
        TreeLowerPriorityAbort = 2,
        TreeParentStop = 3
    }

    [Name("状态退出原因"), Category("BTSMTL/技能条件")]
    [BtsmtlSkillNodeKind("state-exit-cause")]
    [BtsmtlSkillAuthoringField("cause", typeof(BtsmtlSkillStateExitCause), Optional = true)]
    public sealed class BtsmtlSkillStateExitCauseFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] BtsmtlSkillStateExitCause m_Cause;
        public BtsmtlSkillStateExitCause Cause => m_Cause;

        public void SetCause(BtsmtlSkillStateExitCause cause)
        {
            if (!Enum.IsDefined(typeof(BtsmtlSkillStateExitCause), cause))
                throw new ArgumentOutOfRangeException(nameof(cause));
            m_Cause = cause;
        }

        protected override void RegisterPorts() => AddValueOutput<bool>("符合原因", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("技能入口"), Category("BTSMTL/技能流程"), DoNotList]
    [BtsmtlSkillNodeKind("@root")]
    public sealed class BtsmtlSkillRootFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillSystemNode
    {
        protected override void RegisterPorts() => AddFlowOutput("执行", "Output");
    }

    [Name("成功完成"), Category("BTSMTL/技能流程")]
    [BtsmtlSkillNodeKind("succeed")]
    public sealed class BtsmtlSkillSucceedFlowNode : BtsmtlSkillFlowNode
    {
        protected override void RegisterPorts() => AddFlowInput("执行", RejectAuthoringExecution, "Input");
    }

    [Name("条件结果"), Category("BTSMTL/技能条件"), DoNotList]
    [BtsmtlSkillNodeKind("@result")]
    public sealed class BtsmtlSkillConditionResultFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IBtsmtlSkillSystemNode
    {
        protected override void RegisterPorts() => AddValueInput<bool>("结果", "m_Result");
    }

    [Name("状态机入口"), Category("BTSMTL/技能状态"), DoNotList]
    [BtsmtlSkillNodeKind("@enter")]
    public sealed class BtsmtlSkillStateEnterFlowNode : BtsmtlSkillCompositeFlowNode, IBtsmtlSkillStateStructureNode, IBtsmtlSkillSystemNode
    {
        protected override bool HasExecutionInput => false;

        protected override void RegisterPorts()
        {
            AddFlowOutput("转移", "Transfer");
            base.RegisterPorts();
        }
    }

    [Name("任意状态"), Category("BTSMTL/技能状态"), DoNotList]
    [BtsmtlSkillNodeKind("@any")]
    public sealed class BtsmtlSkillStateAnyFlowNode : BtsmtlSkillCompositeFlowNode, IBtsmtlSkillStateStructureNode, IBtsmtlSkillSystemNode
    {
        protected override bool HasExecutionInput => false;

        protected override void RegisterPorts()
        {
            AddFlowOutput("转移", "Transfer");
            base.RegisterPorts();
        }
    }

    [Name("状态机出口"), Category("BTSMTL/技能状态"), DoNotList]
    [BtsmtlSkillNodeKind("@exit")]
    public sealed class BtsmtlSkillStateExitFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillStateStructureNode, IBtsmtlSkillSystemNode
    {
        protected override void RegisterPorts() => AddFlowInput("退出", RejectAuthoringExecution, "StateIn");
    }

    [Name("技能状态机"), Category("BTSMTL/技能流程")]
    [BtsmtlSkillNodeKind("state-machine")]
    [BtsmtlSkillGraphReference("graphId", BtsmtlSkillFlowGraphRole.StateMachine)]
    [BtsmtlSkillAuthoringField(
        "graphId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference)]
    public sealed class BtsmtlSkillStateMachineFlowNode : BtsmtlSkillFlowNode, IGraphAssignable
    {
        [SerializeField] BtsmtlSkillFlowGraph m_StateMachine;
        public BtsmtlSkillFlowGraph StateMachine => m_StateMachine;

        public void SetStateMachine(BtsmtlSkillFlowGraph graph)
        {
            if (graph != null && graph.Role != BtsmtlSkillFlowGraphRole.StateMachine)
                throw new ArgumentException("A state machine node requires a skill state machine graph.", nameof(graph));
            m_StateMachine = graph;
        }

        protected override void RegisterPorts() => AddFlowInput("执行", RejectAuthoringExecution, "Input");
        Graph IGraphAssignable.subGraph { get => m_StateMachine; set => SetStateMachine((BtsmtlSkillFlowGraph)value); }
        Graph IGraphAssignable.currentInstance { get => null; set => throw new InvalidOperationException("Skill state machines execute as compiled data."); }
        BBParameter IGraphAssignable.subGraphParameter => null;
        List<BBMappingParameter> IGraphAssignable.variablesMap
        {
            get => null;
            set
            {
                if (value != null)
                    throw new InvalidOperationException("Skill parameters use their declared compilation contract.");
            }
        }
        Dictionary<Graph, Graph> IGraphAssignable.instances { get => new(); set => throw new InvalidOperationException("Skill state machines do not create authoring graph runtime instances."); }
    }

    [Name("技能状态"), Category("BTSMTL/技能状态")]
    [BtsmtlSkillNodeKind("state")]
    [BtsmtlSkillGraphReference("bodyGraphId", BtsmtlSkillFlowGraphRole.StateBody)]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.CompositeSteps, "steps")]
    [BtsmtlSkillAuthoringField(
        "bodyGraphId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference)]
    [BtsmtlSkillAuthoringField(
        "steps",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Object)]
    public sealed class BtsmtlSkillStateFlowNode : BtsmtlSkillCompositeFlowNode, IBtsmtlSkillStateStructureNode, IGraphAssignable
    {
        [SerializeField] BtsmtlSkillFlowGraph m_Body;
        public BtsmtlSkillFlowGraph Body => m_Body;
        protected override string ExecutionInputId => "StateIn";

        protected override void RegisterPorts()
        {
            base.RegisterPorts();
            AddFlowOutput("转移", "Transfer");
        }

        public void SetBody(BtsmtlSkillFlowGraph graph)
        {
            if (graph != null && graph.Role != BtsmtlSkillFlowGraphRole.StateBody)
                throw new ArgumentException("A state requires a skill state body graph.", nameof(graph));
            m_Body = graph;
        }

        Graph IGraphAssignable.subGraph { get => m_Body; set => SetBody((BtsmtlSkillFlowGraph)value); }
        Graph IGraphAssignable.currentInstance { get => null; set => throw new InvalidOperationException("Skill states execute as compiled data."); }
        BBParameter IGraphAssignable.subGraphParameter => null;
        List<BBMappingParameter> IGraphAssignable.variablesMap
        {
            get => null;
            set
            {
                if (value != null)
                    throw new InvalidOperationException("Skill parameters use their declared compilation contract.");
            }
        }
        Dictionary<Graph, Graph> IGraphAssignable.instances { get => new(); set => throw new InvalidOperationException("Skill states do not create authoring graph runtime instances."); }
    }
}
#endif
