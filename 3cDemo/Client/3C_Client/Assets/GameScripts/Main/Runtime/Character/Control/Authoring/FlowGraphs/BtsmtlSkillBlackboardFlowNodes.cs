#if UNITY_EDITOR
using System;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Serializable]
    public struct BtsmtlSkillBlackboardReference
    {
        [SerializeField] string m_DeclarationId;
        [SerializeField] string m_OwnerId;

        public BtsmtlSkillBlackboardReference(string declarationId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(declarationId) || string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("黑板引用必须指定声明及其所属作用域的稳定身份。");
            m_DeclarationId = declarationId;
            m_OwnerId = ownerId;
        }

        public string DeclarationId => m_DeclarationId ?? string.Empty;
        public string OwnerId => m_OwnerId ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(m_DeclarationId) && !string.IsNullOrWhiteSpace(m_OwnerId);
    }

    public interface IBtsmtlSkillBlackboardAccessNode
    {
        BtsmtlSkillBlackboardReference Variable { get; }
        Type ValueType { get; }
        bool Writes { get; }
    }

    public interface IBtsmtlSkillBlackboardReadNode : IBtsmtlSkillBlackboardAccessNode { }

    public abstract class BtsmtlSkillBlackboardReadFlowNode<T> : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IBtsmtlSkillBlackboardReadNode
    {
        [SerializeField] BtsmtlSkillBlackboardReference m_Variable;
        public BtsmtlSkillBlackboardReference Variable => m_Variable;
        public Type ValueType => typeof(T);
        public bool Writes => false;

        public void SetVariable(BtsmtlSkillBlackboardReference variable)
        {
            if (!variable.IsValid)
                throw new ArgumentException("黑板读取必须引用有效声明。", nameof(variable));
            m_Variable = variable;
        }

        protected override void RegisterPorts() => AddValueOutput<T>("值", RejectAuthoringValue<T>, "m_Output");
    }

    [Name("读取布尔黑板值"), Category("BTSMTL/技能黑板")]
    public sealed class BtsmtlSkillBlackboardBooleanFlowNode : BtsmtlSkillBlackboardReadFlowNode<bool>
    {
        public override string CapabilityId => "pipeline-blackboard-bool";
    }

    [Name("读取数值黑板值"), Category("BTSMTL/技能黑板")]
    public sealed class BtsmtlSkillBlackboardScalarFlowNode : BtsmtlSkillBlackboardReadFlowNode<float>
    {
        public override string CapabilityId => "pipeline-blackboard-float";
    }

    public enum BtsmtlSkillBlackboardValueType
    {
        Boolean,
        Integer,
        Number,
        Identity,
        Vector2,
        Vector3
    }

    public abstract class BtsmtlSkillBlackboardAccessFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillBlackboardAccessNode
    {
        [SerializeField] BtsmtlSkillBlackboardReference m_Variable;
        [SerializeField] BtsmtlSkillBlackboardValueType m_ValueType = BtsmtlSkillBlackboardValueType.Number;
        [SerializeField] UnityEngine.Object m_FactContext;
        public override string CapabilityId => "exposed-property";
        public BtsmtlSkillBlackboardReference Variable => m_Variable;
        public BtsmtlSkillBlackboardValueType DeclaredType => m_ValueType;
        public UnityEngine.Object FactContext => m_FactContext;
        public abstract bool Writes { get; }
        public Type ValueType => ClrType(m_ValueType);

        public void Configure(BtsmtlSkillBlackboardReference variable, BtsmtlSkillBlackboardValueType type, UnityEngine.Object factContext)
        {
            if (!variable.IsValid || !Enum.IsDefined(typeof(BtsmtlSkillBlackboardValueType), type))
                throw new ArgumentException("黑板节点需要有效声明与明确值类型。");
            if (ValueType != ClrType(type) && (inConnections.Count != 0 || outConnections.Count != 0))
                throw new InvalidOperationException("修改黑板值类型前必须断开已有连接。");
            m_Variable = variable;
            m_ValueType = type;
            m_FactContext = factContext;
            GatherPorts();
        }

        static Type ClrType(BtsmtlSkillBlackboardValueType type) => type switch
        {
            BtsmtlSkillBlackboardValueType.Boolean => typeof(bool),
            BtsmtlSkillBlackboardValueType.Integer => typeof(int),
            BtsmtlSkillBlackboardValueType.Number => typeof(float),
            BtsmtlSkillBlackboardValueType.Identity => typeof(string),
            BtsmtlSkillBlackboardValueType.Vector2 => typeof(Vector2),
            BtsmtlSkillBlackboardValueType.Vector3 => typeof(Vector3),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    [Name("读取黑板声明"), Category("BTSMTL/技能黑板")]
    public sealed class BtsmtlSkillBlackboardGetFlowNode : BtsmtlSkillBlackboardAccessFlowNode, IBtsmtlSkillPureValueNode
    {
        public override bool Writes => false;
        protected override void RegisterPorts() => AddValueOutput("值", ValueType, RejectAuthoringValue<object>, "m_Output");
    }

    [Name("写入黑板声明"), Category("BTSMTL/技能黑板")]
    public sealed class BtsmtlSkillBlackboardSetFlowNode : BtsmtlSkillBlackboardAccessFlowNode
    {
        public override bool Writes => true;
        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueInput("值", ValueType, "m_Value");
        }
    }
}
#endif
