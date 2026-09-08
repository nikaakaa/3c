using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ParadoxNotion.Design;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillPureValueNode { }
    public interface IBtsmtlSkillStateStructureNode { }

    public abstract class BtsmtlSkillFlowNode : FlowScriptNode
    {
        public abstract string CapabilityId { get; }
        public override bool ignoreSelfInstancePortAssignment => true;

        protected static void RejectAuthoringExecution(Flow flow) =>
            throw new InvalidOperationException("Skill authoring nodes must execute through the compiled Skill Program.");

        protected static T RejectAuthoringValue<T>() =>
            throw new InvalidOperationException("Skill values must be evaluated by the compiled Skill Program.");
    }

    [Serializable]
    public sealed class BtsmtlSkillStepPort
    {
        [SerializeField, HideInInspector] string m_Id;
        [SerializeField] string m_Name;
        [SerializeField] BtsmtlSkillFlowGraph m_Condition;
        [SerializeField] int m_Priority;
        [SerializeField] ProgramAbortPolicy m_AbortPolicy;

        public BtsmtlSkillStepPort() : this(Guid.NewGuid().ToString("N"), string.Empty) { }

        public BtsmtlSkillStepPort(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A skill step requires a stable port identity.", nameof(id));
            m_Id = id;
            m_Name = name ?? string.Empty;
        }

        public string Id => m_Id;
        public string Name => m_Name;
        public BtsmtlSkillFlowGraph Condition => m_Condition;
        public int Priority => m_Priority;
        public ProgramAbortPolicy AbortPolicy => m_AbortPolicy;

        public void Configure(string name, BtsmtlSkillFlowGraph condition, int priority, ProgramAbortPolicy abortPolicy)
        {
            if (condition != null && condition.Role != BtsmtlSkillFlowGraphRole.ConditionRule)
                throw new ArgumentException("A skill step condition requires a condition rule graph.", nameof(condition));
            if (!Enum.IsDefined(typeof(ProgramAbortPolicy), abortPolicy))
                throw new ArgumentOutOfRangeException(nameof(abortPolicy));
            m_Name = name ?? string.Empty;
            m_Condition = condition;
            m_Priority = priority;
            m_AbortPolicy = abortPolicy;
        }
    }

    public abstract class BtsmtlSkillCompositeFlowNode : BtsmtlSkillFlowNode
    {
        [SerializeField, GatherPortsCallback] List<BtsmtlSkillStepPort> m_Steps = new()
        {
            new BtsmtlSkillStepPort(Guid.NewGuid().ToString("N"), "步骤 1"),
            new BtsmtlSkillStepPort(Guid.NewGuid().ToString("N"), "步骤 2")
        };

        public IReadOnlyList<BtsmtlSkillStepPort> Steps => m_Steps;
        protected virtual bool HasExecutionInput => true;
        protected virtual string ExecutionInputId => "Input";

        public void SetSteps(IEnumerable<BtsmtlSkillStepPort> steps)
        {
            List<BtsmtlSkillStepPort> next = (steps ?? throw new ArgumentNullException(nameof(steps))).ToList();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillStepPort step in next)
                if (step == null || string.IsNullOrWhiteSpace(step.Id) || !identities.Add(step.Id))
                    throw new ArgumentException("Skill step identities must be present and unique.", nameof(steps));
            foreach (var connection in outConnections.OfType<BinderConnection>())
                if (!identities.Contains(connection.sourcePortID))
                    throw new InvalidOperationException("Disconnect a skill step before removing its port.");
            m_Steps = next;
            GatherPorts();
        }

        protected override void RegisterPorts()
        {
            if (HasExecutionInput)
                AddFlowInput("执行", RejectAuthoringExecution, ExecutionInputId);
            for (int i = 0; i < m_Steps.Count; i++)
                AddFlowOutput($"{i + 1}. {m_Steps[i].Name}", m_Steps[i].Id);
        }

#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI() => BtsmtlSkillStepInspector.Draw(this);
#endif
    }

    [Name("顺序执行"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillSequenceFlowNode : BtsmtlSkillCompositeFlowNode
    {
        public override string CapabilityId => "sequence";
    }

    [Name("选择执行"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillSelectorFlowNode : BtsmtlSkillCompositeFlowNode
    {
        public override string CapabilityId => "selector";
    }

    public enum BtsmtlSkillParallelMode
    {
        JumpComplete,
        UpdateAll
    }

    public enum BtsmtlSkillLoopStopType
    {
        None,
        Success,
        Failure
    }

    [Name("循环执行"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillLoopFlowNode : BtsmtlSkillFlowNode
    {
        [SerializeField] BtsmtlSkillLoopStopType m_StopType;
        public override string CapabilityId => "loop";
        public BtsmtlSkillLoopStopType StopType => m_StopType;

        public void SetStopType(BtsmtlSkillLoopStopType stopType)
        {
            if (!Enum.IsDefined(typeof(BtsmtlSkillLoopStopType), stopType))
                throw new ArgumentOutOfRangeException(nameof(stopType));
            m_StopType = stopType;
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddFlowOutput("循环内容", "Output");
        }
    }

    [Name("并行执行"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillParallelFlowNode : BtsmtlSkillCompositeFlowNode
    {
        [SerializeField] BtsmtlSkillParallelMode m_Mode;
        public override string CapabilityId => "parallel";
        public BtsmtlSkillParallelMode Mode => m_Mode;

        public void SetMode(BtsmtlSkillParallelMode mode)
        {
            if (!Enum.IsDefined(typeof(BtsmtlSkillParallelMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            m_Mode = mode;
        }
    }
}
