using System;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    public enum AIMemoryValueKind : byte
    {
        Boolean = 1,
        Integer = 2,
        Scalar = 3,
        Vector2 = 4,
        Vector3 = 5,
        ActorId = 6,
        ActionTargetSnapshot = 7
    }

    public enum AIRequestRepeatPolicy : byte
    {
        OncePerActivation = 1,
        EveryEvaluation = 2
    }

    [Serializable]
    [NodeName("Read Self Observation")]
    [NodePath("AI/Observation/Read Self")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class ReadSelfObservationNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "ActorId"), ReadOnly]
        StringPropertyPort m_ActorId = new StringPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Position"), ReadOnly]
        Vector3PropertyPort m_ObservedPosition = new Vector3PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Yaw"), ReadOnly]
        FloatPropertyPort m_Yaw = new FloatPropertyPort();
    }

    [Serializable]
    [NodeName("Configured Candidates")]
    [NodePath("AI/Observation/Enumerate Configured Candidates")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class EnumerateConfiguredCandidatesNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Count"), ReadOnly]
        IntPropertyPort m_Count = new IntPropertyPort();
    }

    [Serializable]
    [NodeName("Select Nearest Candidate")]
    [NodePath("AI/Observation/Select Nearest Candidate")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class SelectNearestCandidateNode : ActionNode
    {
        protected override void DoAction()
        {
        }
    }

    [Serializable]
    [NodeName("Read Target Distance")]
    [NodePath("AI/Observation/Read Target Distance")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class ReadTargetDistanceNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Distance"), ReadOnly]
        FloatPropertyPort m_Distance = new FloatPropertyPort();
    }

    [Serializable]
    [NodeName("Read Target Direction")]
    [NodePath("AI/Observation/Read Target Direction")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class ReadTargetDirectionNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Direction"), ReadOnly]
        Vector2PropertyPort m_Direction = new Vector2PropertyPort();
    }

    [Serializable]
    [NodeName("Read Selected Target")]
    [NodePath("AI/Observation/Read Selected Target")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIObservation)]
    public sealed class ReadSelectedTargetSnapshotNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Target"), ReadOnly]
        AIActionTargetSnapshotPropertyPort m_Target = new AIActionTargetSnapshotPropertyPort();
    }

    [Serializable]
    [NodeName("Read AI Memory")]
    [NodePath("AI/Memory/Read")]
    [NodeView("AIMemoryNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIMemory)]
    public sealed class ReadAIMemoryNode : ValueNode
    {
        [SerializeField] PipelineBlackboardVariableReference m_BlackboardVariable;
        [SerializeField] AIMemoryValueKind m_ValueKind = AIMemoryValueKind.Scalar;
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Value", "GetAcceptableValueTypes")]
        PropertyPort m_Value = new PropertyPort();

        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;
        public AIMemoryValueKind ValueKind => m_ValueKind;
        public PropertyPort ValuePort => m_Value;
        Type[] GetAcceptableValueTypes() => AIControllerNodeValueTypes.For(m_ValueKind);

#if UNITY_EDITOR
        public void ConfigureAuthoring(BaseExposedProperty declaration, AIMemoryValueKind valueKind)
        {
            m_BlackboardVariable = new PipelineBlackboardVariableReference(declaration);
            m_ValueKind = valueKind;
            SetPropertyPort("m_Value", AIControllerNodeValueTypes.PortType(valueKind), PortDirection.Output);
        }
#endif
    }

    [Serializable]
    [NodeName("Write AI Memory")]
    [NodePath("AI/Memory/Write")]
    [NodeView("AIMemoryNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIMemory)]
    public sealed class WriteAIMemoryNode : ActionNode
    {
        [SerializeField] PipelineBlackboardVariableReference m_BlackboardVariable;
        [SerializeField] AIMemoryValueKind m_ValueKind = AIMemoryValueKind.Scalar;
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value", "GetAcceptableValueTypes")]
        PropertyPort m_Value = new PropertyPort();

        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;
        public AIMemoryValueKind ValueKind => m_ValueKind;
        public PropertyPort ValuePort => m_Value;
        Type[] GetAcceptableValueTypes() => AIControllerNodeValueTypes.For(m_ValueKind);

        protected override void DoAction()
        {
        }

#if UNITY_EDITOR
        public void ConfigureAuthoring(BaseExposedProperty declaration, AIMemoryValueKind valueKind)
        {
            m_BlackboardVariable = new PipelineBlackboardVariableReference(declaration);
            m_ValueKind = valueKind;
            SetPropertyPort("m_Value", AIControllerNodeValueTypes.PortType(valueKind), PortDirection.Input);
        }
#endif
    }

    [Serializable]
    [NodeName("Write Continuous Input")]
    [NodePath("AI/Intent/Write Continuous Input")]
    [NodeView("AIIntentBindingNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIIntent)]
    public sealed class WriteContinuousInputNode : ActionNode
    {
        [SerializeField] string m_InputId = string.Empty;
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value", typeof(bool), typeof(float), typeof(Vector2), typeof(Vector3))]
        PropertyPort m_Value = new PropertyPort();

        public string InputId => m_InputId ?? string.Empty;
        public PropertyPort ValuePort => m_Value;

        protected override void DoAction()
        {
        }

#if UNITY_EDITOR
        public void ConfigureInput(string inputId, Type propertyPortType)
        {
            m_InputId = inputId ?? string.Empty;
            SetPropertyPort("m_Value", propertyPortType, PortDirection.Input);
        }
#endif
    }

    [Serializable]
    [NodeName("Write Action Target")]
    [NodePath("AI/Intent/Write Action Target Snapshot")]
    [NodeView("AIIntentBindingNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIIntent)]
    public sealed class WriteActionTargetSnapshotNode : ActionNode
    {
        [SerializeField] string m_InputId = string.Empty;
        public string InputId => m_InputId ?? string.Empty;

        protected override void DoAction()
        {
        }

#if UNITY_EDITOR
        public void ConfigureInput(string inputId)
        {
            m_InputId = inputId ?? string.Empty;
        }
#endif
    }

    [Serializable]
    [NodeName("Submit Action Request")]
    [NodePath("AI/Intent/Submit Action Request")]
    [NodeView("AIIntentBindingNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIIntent)]
    public sealed class SubmitActionRequestNode : ActionNode
    {
        [SerializeField] string m_RequestId = string.Empty;
        [SerializeField, Min(0f)] float m_BufferSeconds;
        [SerializeField] int m_Priority;
        [SerializeField] AIRequestRepeatPolicy m_RepeatPolicy = AIRequestRepeatPolicy.OncePerActivation;

        public string RequestId => m_RequestId ?? string.Empty;
        public float BufferSeconds => m_BufferSeconds;
        public int Priority => m_Priority;
        public AIRequestRepeatPolicy RepeatPolicy => m_RepeatPolicy;

        protected override void DoAction()
        {
        }

#if UNITY_EDITOR
        public void ConfigureRequest(
            string requestId,
            float bufferSeconds,
            int priority,
            AIRequestRepeatPolicy repeatPolicy)
        {
            m_RequestId = requestId ?? string.Empty;
            m_BufferSeconds = Math.Max(0f, bufferSeconds);
            m_Priority = priority;
            m_RepeatPolicy = repeatPolicy;
        }
#endif
    }

    [Serializable]
    [NodeName("Wait Ticks")]
    [NodePath("AI/Flow/Wait Ticks")]
    [NodeAuthoringCapability(NodeAuthoringCapability.AIIntent)]
    public sealed class AIWaitTicksNode : ActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Ticks")]
        IntPropertyPort m_Ticks = new IntPropertyPort();

        protected override void DoAction()
        {
        }
    }

    static class AIControllerNodeValueTypes
    {
        static readonly Type[] s_Boolean = { typeof(bool) };
        static readonly Type[] s_Integer = { typeof(int) };
        static readonly Type[] s_Scalar = { typeof(float) };
        static readonly Type[] s_Vector2 = { typeof(Vector2) };
        static readonly Type[] s_Vector3 = { typeof(Vector3) };
        static readonly Type[] s_ActorIdentity = { typeof(AIActorIdValue) };
        static readonly Type[] s_ActionTarget = { typeof(AIActionTargetSnapshotValue) };

        public static Type[] For(AIMemoryValueKind kind)
        {
            return kind switch
            {
                AIMemoryValueKind.Boolean => s_Boolean,
                AIMemoryValueKind.Integer => s_Integer,
                AIMemoryValueKind.Scalar => s_Scalar,
                AIMemoryValueKind.Vector2 => s_Vector2,
                AIMemoryValueKind.Vector3 => s_Vector3,
                AIMemoryValueKind.ActorId => s_ActorIdentity,
                AIMemoryValueKind.ActionTargetSnapshot => s_ActionTarget,
                _ => Array.Empty<Type>()
            };
        }

        public static Type PortType(AIMemoryValueKind kind)
        {
            return kind switch
            {
                AIMemoryValueKind.Boolean => typeof(BoolPropertyPort),
                AIMemoryValueKind.Integer => typeof(IntPropertyPort),
                AIMemoryValueKind.Scalar => typeof(FloatPropertyPort),
                AIMemoryValueKind.Vector2 => typeof(Vector2PropertyPort),
                AIMemoryValueKind.Vector3 => typeof(Vector3PropertyPort),
                AIMemoryValueKind.ActorId => typeof(AIActorIdPropertyPort),
                AIMemoryValueKind.ActionTargetSnapshot => typeof(AIActionTargetSnapshotPropertyPort),
                _ => typeof(PropertyPort)
            };
        }
    }
}
