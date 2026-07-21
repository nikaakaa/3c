using System;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    [PropertyColor(148, 129, 230)]
    public sealed class EquipmentUInt64PropertyPort : PropertyPort<ulong>
    {
    }

    [Serializable]
    [PropertyColor(230, 138, 106)]
    public sealed class EquipmentChangeFailurePropertyPort : PropertyPort<EquipmentChangeFailure>
    {
    }

    [Serializable]
    [NodeName("Read Equipment Identity")]
    [NodePath("Base/Value/Equipment/Read Identity")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ReadEquipmentIdentityNode : CharacterSimulationValueNode
    {
        [SerializeField, ShowInPanel("Slot Id")]
        string m_SlotId;

        [SerializeField, PropertyPort(PortDirection.Output, "Equipment"), ReadOnly]
        StringPropertyPort m_Equipment = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Feature"), ReadOnly]
        StringPropertyPort m_Feature = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Revision"), ReadOnly]
        EquipmentUInt64PropertyPort m_Revision = new EquipmentUInt64PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Equipped"), ReadOnly]
        BoolPropertyPort m_Equipped = new BoolPropertyPort();

        public string SlotId => Normalize(m_SlotId);

#if UNITY_EDITOR
        public void ConfigureAuthoring(string slotId)
        {
            m_SlotId = Normalize(slotId);
            OnNodeChangedCallback();
        }
#endif

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    [Serializable]
    [NodeName("Read Equipment Parameter")]
    [NodePath("Base/Value/Equipment/Read Parameter")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ReadEquipmentParameterNode : CharacterSimulationValueNode
    {
        [SerializeField, ShowInPanel("Slot Id")]
        string m_SlotId;

        [SerializeField, ShowInPanel("Feature Id")]
        string m_FeatureId;

        [SerializeField, ShowInPanel("Parameter Id")]
        string m_ParameterId;

        [SerializeField, ShowInPanel("Value Kind")]
        EquipmentParameterValueKind m_ValueKind = EquipmentParameterValueKind.Scalar;

        [SerializeField, PropertyPort(PortDirection.Input, "Expected Revision")]
        EquipmentUInt64PropertyPort m_ExpectedRevision = new EquipmentUInt64PropertyPort();

        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Value", "GetAcceptableValueTypes"), ReadOnly]
        PropertyPort m_Output = new FloatPropertyPort();

        public string SlotId => ReadEquipmentIdentityNode.Normalize(m_SlotId);
        public string FeatureId => ReadEquipmentIdentityNode.Normalize(m_FeatureId);
        public string ParameterId => ReadEquipmentIdentityNode.Normalize(m_ParameterId);
        public EquipmentParameterValueKind ValueKind => m_ValueKind;
        public PropertyPort OutputPort => m_Output;

        Type[] GetAcceptableValueTypes() => EquipmentNodeValueTypes.For(m_ValueKind);

#if UNITY_EDITOR
        public void ConfigureAuthoring(string slotId, string featureId, string parameterId, EquipmentParameterValueKind valueKind)
        {
            m_SlotId = ReadEquipmentIdentityNode.Normalize(slotId);
            m_FeatureId = ReadEquipmentIdentityNode.Normalize(featureId);
            m_ParameterId = ReadEquipmentIdentityNode.Normalize(parameterId);
            m_ValueKind = valueKind;
            SetPropertyPort("m_Output", EquipmentNodeValueTypes.PortType(valueKind), PortDirection.Output);
            OnNodeChangedCallback();
        }
#endif
    }

    [Serializable]
    public abstract class EquipmentChangeOperationNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Slot Id")]
        string m_SlotId;

        [SerializeField, ShowInPanel("Equipment Id")]
        string m_EquipmentId;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Input, "Expected Revision")]
        EquipmentUInt64PropertyPort m_ExpectedRevision = new EquipmentUInt64PropertyPort();

        public string SlotId => ReadEquipmentIdentityNode.Normalize(m_SlotId);
        public string EquipmentId => ReadEquipmentIdentityNode.Normalize(m_EquipmentId);
        public ActionContextSlot ActionContext => m_ActionContext;

#if UNITY_EDITOR
        public void ConfigureAuthoring(string slotId, string equipmentId, ActionContextSlot actionContext)
        {
            m_SlotId = ReadEquipmentIdentityNode.Normalize(slotId);
            m_EquipmentId = ReadEquipmentIdentityNode.Normalize(equipmentId);
            m_ActionContext = actionContext;
            OnNodeChangedCallback();
        }
#endif
    }

    [Serializable]
    [NodeName("Request Equipment Change")]
    [NodePath("Base/Action/Equipment/Request Change")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class RequestEquipmentChangeNode : EquipmentChangeOperationNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Accepted"), ReadOnly]
        BoolPropertyPort m_Accepted = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Failure"), ReadOnly]
        EquipmentChangeFailurePropertyPort m_Failure = new EquipmentChangeFailurePropertyPort();
    }

    [Serializable]
    [NodeName("Begin Equipment Change")]
    [NodePath("Base/Action/Equipment/Begin Change")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class BeginEquipmentChangeNode : EquipmentChangeOperationNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Begun"), ReadOnly]
        BoolPropertyPort m_Begun = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Change Id"), ReadOnly]
        EquipmentUInt64PropertyPort m_ChangeId = new EquipmentUInt64PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Failure"), ReadOnly]
        EquipmentChangeFailurePropertyPort m_Failure = new EquipmentChangeFailurePropertyPort();
    }

    [Serializable]
    public abstract class EquipmentPendingChangeOperationNode : CharacterSimulationOperationNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Change Id")]
        EquipmentUInt64PropertyPort m_ChangeId = new EquipmentUInt64PropertyPort();
    }

    [Serializable]
    [NodeName("Commit Equipment Change")]
    [NodePath("Base/Action/Equipment/Commit Change")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CommitEquipmentChangeNode : EquipmentPendingChangeOperationNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Committed"), ReadOnly]
        BoolPropertyPort m_Committed = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Failure"), ReadOnly]
        EquipmentChangeFailurePropertyPort m_Failure = new EquipmentChangeFailurePropertyPort();
    }

    [Serializable]
    [NodeName("Cancel Equipment Change")]
    [NodePath("Base/Action/Equipment/Cancel Change")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CancelEquipmentChangeNode : EquipmentPendingChangeOperationNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Cancelled"), ReadOnly]
        BoolPropertyPort m_Cancelled = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Failure"), ReadOnly]
        EquipmentChangeFailurePropertyPort m_Failure = new EquipmentChangeFailurePropertyPort();
    }

    [Serializable]
    public abstract class EquipmentSlotHostNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Slot Id")]
        string m_SlotId;

        public string SlotId => ReadEquipmentIdentityNode.Normalize(m_SlotId);

#if UNITY_EDITOR
        public void ConfigureAuthoring(string slotId)
        {
            m_SlotId = ReadEquipmentIdentityNode.Normalize(slotId);
            OnNodeChangedCallback();
        }
#endif
    }

    [Serializable]
    [NodeName("Enter Equipment Feature Host")]
    [NodePath("Base/Action/Equipment/Enter Feature Host")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class EnterEquipmentFeatureHostNode : EquipmentSlotHostNode
    {
    }

    [Serializable]
    [NodeName("Exit Equipment Feature Host")]
    [NodePath("Base/Action/Equipment/Exit Feature Host")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ExitEquipmentFeatureHostNode : EquipmentSlotHostNode
    {
    }

    [Serializable]
    [NodeName("Resolve Equipment Action Route")]
    [NodePath("Base/Action/Equipment/Resolve Action Route")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ResolveEquipmentActionRouteNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Route Id")]
        string m_RouteId;

        public string RouteId => ReadEquipmentIdentityNode.Normalize(m_RouteId);

#if UNITY_EDITOR
        public void ConfigureAuthoring(string routeId)
        {
            m_RouteId = ReadEquipmentIdentityNode.Normalize(routeId);
            OnNodeChangedCallback();
        }
#endif
    }

    static class EquipmentNodeValueTypes
    {
        static readonly Type[] s_Boolean = { typeof(bool) };
        static readonly Type[] s_Int32 = { typeof(int) };
        static readonly Type[] s_Scalar = { typeof(float) };
        static readonly Type[] s_Vector2 = { typeof(Vector2) };
        static readonly Type[] s_Vector3 = { typeof(Vector3) };
        static readonly Type[] s_Identity = { typeof(string) };

        public static Type[] For(EquipmentParameterValueKind kind) => kind switch
        {
            EquipmentParameterValueKind.Boolean => s_Boolean,
            EquipmentParameterValueKind.Int32 => s_Int32,
            EquipmentParameterValueKind.Scalar => s_Scalar,
            EquipmentParameterValueKind.Vector2 => s_Vector2,
            EquipmentParameterValueKind.Vector3 => s_Vector3,
            EquipmentParameterValueKind.Yaw => s_Scalar,
            EquipmentParameterValueKind.GameplayTag => s_Identity,
            EquipmentParameterValueKind.GameplayEffect => s_Identity,
            EquipmentParameterValueKind.AnimationProducer => s_Identity,
            _ => Array.Empty<Type>()
        };

        public static Type PortType(EquipmentParameterValueKind kind) => kind switch
        {
            EquipmentParameterValueKind.Boolean => typeof(BoolPropertyPort),
            EquipmentParameterValueKind.Int32 => typeof(IntPropertyPort),
            EquipmentParameterValueKind.Scalar => typeof(FloatPropertyPort),
            EquipmentParameterValueKind.Vector2 => typeof(Vector2PropertyPort),
            EquipmentParameterValueKind.Vector3 => typeof(Vector3PropertyPort),
            EquipmentParameterValueKind.Yaw => typeof(FloatPropertyPort),
            EquipmentParameterValueKind.GameplayTag => typeof(StringPropertyPort),
            EquipmentParameterValueKind.GameplayEffect => typeof(StringPropertyPort),
            EquipmentParameterValueKind.AnimationProducer => typeof(StringPropertyPort),
            _ => typeof(PropertyPort)
        };
    }
}
