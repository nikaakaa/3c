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

    public interface IBtsmtlSkillBlackboardReadNode
    {
        BtsmtlSkillBlackboardReference Variable { get; }
        Type ValueType { get; }
    }

    public abstract class BtsmtlSkillBlackboardReadFlowNode<T> : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IBtsmtlSkillBlackboardReadNode
    {
        [SerializeField] BtsmtlSkillBlackboardReference m_Variable;
        public BtsmtlSkillBlackboardReference Variable => m_Variable;
        public Type ValueType => typeof(T);

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
}
#endif
