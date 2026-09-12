#if UNITY_EDITOR
using System;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Serializable]
    public sealed class BtsmtlSkillTransferPayload
    {
        [SerializeField] BtsmtlSkillFlowGraph m_Condition;
        [SerializeField] int m_Priority;
        [SerializeField] ProgramAbortPolicy m_AbortPolicy;
        [SerializeField] int m_Order;

        public BtsmtlSkillFlowGraph Condition => m_Condition;
        public int Priority => m_Priority;
        public ProgramAbortPolicy AbortPolicy => m_AbortPolicy;
        public int Order => m_Order;

        public void Configure(
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order)
        {
            if (condition != null && condition.Role != BtsmtlSkillFlowGraphRole.ConditionRule)
                throw new ArgumentException("技能转移条件必须是条件规则图。", nameof(condition));
            if (priority < BtsmtlSkillFlowConnection.MinPriority)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (!Enum.IsDefined(typeof(ProgramAbortPolicy), abortPolicy))
                throw new ArgumentOutOfRangeException(nameof(abortPolicy));
            if (order < BtsmtlSkillFlowConnection.MinOrder)
                throw new ArgumentOutOfRangeException(nameof(order));
            m_Condition = condition;
            m_Priority = priority;
            m_AbortPolicy = abortPolicy;
            m_Order = order;
        }
    }

    public sealed class BtsmtlSkillFlowConnection : BinderConnection
    {
        public const int MinPriority = 0;
        public const int MinOrder = 0;

        [SerializeField] BtsmtlSkillTransferPayload m_Payload;
        [SerializeField, HideInInspector] BtsmtlSkillFlowGraph m_Condition;
        [SerializeField, HideInInspector] int m_Priority;
        [SerializeField, HideInInspector] ProgramAbortPolicy m_AbortPolicy;
        [SerializeField, HideInInspector] int m_Order;

        public BtsmtlSkillTransferPayload Payload
        {
            get
            {
                if (m_Payload == null)
                {
                    m_Payload = new BtsmtlSkillTransferPayload();
                    m_Payload.Configure(m_Condition, m_Priority, m_AbortPolicy, m_Order);
                }
                return m_Payload;
            }
        }

        public BtsmtlSkillFlowGraph Condition => Payload.Condition;
        public int Priority => Payload.Priority;
        public ProgramAbortPolicy AbortPolicy => Payload.AbortPolicy;
        public int Order => Payload.Order;

        public void Configure(
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order)
        {
            Payload.Configure(condition, priority, abortPolicy, order);
        }

        public static BtsmtlSkillFlowConnection Create(Port source, Port target)
        {
            if (source == null || target == null)
                throw new InvalidOperationException("技能转移连线两端端口缺失。");
            if (!(source is FlowOutput) || !(target is FlowInput))
                throw new InvalidOperationException("技能转移连线只能连接执行端口。");
            int order = source.parent.outConnections.OfType<BtsmtlSkillFlowConnection>()
                .Select(value => value.Order)
                .DefaultIfEmpty(MinOrder - 1)
                .Max() + 1;
            BtsmtlSkillFlowConnection connection = BinderConnection.CreateValidatedForDomain(
                    source,
                    target,
                    () => new BtsmtlSkillFlowConnection())
                as BtsmtlSkillFlowConnection;
            if (connection == null)
                throw new InvalidOperationException("技能转移连线创建失败。");
            connection.Configure(null, 0, ProgramAbortPolicy.None, order);
            return connection;
        }

        public override TipConnectionStyle tipConnectionStyle => TipConnectionStyle.Arrow;

        protected override string GetDomainConnectionInfo()
        {
            string condition = Condition != null ? Condition.name : "无条件";
            string priority = m_Priority != 0 ? $" · 优先级 {m_Priority}" : string.Empty;
            return $"{condition} · 顺序 {Order}{priority}";
        }

        protected override void OnConnectionInspectorGUI() =>
            BtsmtlSkillTransferConnectionEditor.Draw(this);
    }
}
#endif
