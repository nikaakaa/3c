#if UNITY_EDITOR
using System;
using FlowCanvas;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillFlowConnection : BinderConnection
    {
        public const int MinPriority = 0;

        [SerializeField] BtsmtlSkillFlowGraph m_Condition;
        [SerializeField] int m_Priority;
        [SerializeField] ProgramAbortPolicy m_AbortPolicy;

        public BtsmtlSkillFlowGraph Condition => m_Condition;
        public int Priority => m_Priority;
        public ProgramAbortPolicy AbortPolicy => m_AbortPolicy;

        public void Configure(BtsmtlSkillFlowGraph condition, int priority, ProgramAbortPolicy abortPolicy)
        {
            if (condition != null && condition.Role != BtsmtlSkillFlowGraphRole.ConditionRule)
                throw new ArgumentException("技能转移条件必须是条件规则图。", nameof(condition));
            if (priority < MinPriority)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (!Enum.IsDefined(typeof(ProgramAbortPolicy), abortPolicy))
                throw new ArgumentOutOfRangeException(nameof(abortPolicy));
            m_Condition = condition;
            m_Priority = priority;
            m_AbortPolicy = abortPolicy;
        }

        public static BtsmtlSkillFlowConnection Create(Port source, Port target)
        {
            if (source == null || target == null)
                throw new InvalidOperationException("技能转移连线两端端口缺失。");
            if (!(source is FlowOutput) || !(target is FlowInput))
                throw new InvalidOperationException("技能转移连线只能连接执行端口。");
            return BinderConnection.CreateValidatedForDomain(source, target, () => new BtsmtlSkillFlowConnection())
                as BtsmtlSkillFlowConnection
                ?? throw new InvalidOperationException("技能转移连线创建失败。");
        }

        public override TipConnectionStyle tipConnectionStyle => TipConnectionStyle.Arrow;

        protected override string GetDomainConnectionInfo()
        {
            string condition = m_Condition != null ? m_Condition.name : "无条件";
            return m_Priority != 0 ? $"{condition} · 优先级 {m_Priority}" : condition;
        }

        protected override void OnConnectionInspectorGUI() =>
            BtsmtlSkillTransferConnectionEditor.Draw(this);
    }
}
#endif
