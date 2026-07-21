using System;
using BTSMTL.Timeline;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Input
{
    [Serializable]
    public abstract class CharacterInputValueInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Input Value Id"), ReadOnly]
        string m_InputValueId;

        [NonSerialized]
        bool m_ReportedSourceError;

        [NonSerialized]
        bool m_ReportedReadError;

        public string InputValueId => m_InputValueId;

        public void BindInputValue(string inputValueId)
        {
            m_InputValueId = inputValueId;
            ResetReports();
#if UNITY_EDITOR
            OnNodeChangedCallback();
#endif
        }

        protected bool TryGetInputValueId(out string inputValueId)
        {
            inputValueId = m_InputValueId;
            if (!string.IsNullOrEmpty(inputValueId))
                return true;

            ReportReadError("input value id is missing.");
            return false;
        }

        protected void ReportReadError(string message)
        {
            if (m_ReportedReadError)
                return;

            m_ReportedReadError = true;
            Debug.LogError($"{GetType().Name}: {message}");
        }

        void ReportSourceError(string message)
        {
            if (m_ReportedSourceError)
                return;

            m_ReportedSourceError = true;
            Debug.LogError($"{GetType().Name}: {message}");
        }

        void ResetReports()
        {
            m_ReportedSourceError = false;
            m_ReportedReadError = false;
        }
    }

    [Serializable]
    [NodeName("Character Input Value Info Bool")]
    [NodePath("Base/Value/Input/Input Value Info/Bool")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterInputBoolInfoNode : CharacterInputValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Character Input Value Info Float")]
    [NodePath("Base/Value/Input/Input Value Info/Float")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterInputFloatInfoNode : CharacterInputValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Character Input Value Info Vector2")]
    [NodePath("Base/Value/Input/Input Value Info/Vector2")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterInputVector2InfoNode : CharacterInputValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        Vector2PropertyPort m_Output = new Vector2PropertyPort();

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Character Input Value Info Vector2 Magnitude")]
    [NodePath("Base/Value/Input/Input Value Info/Vector2 Magnitude")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterInputVector2MagnitudeInfoNode : CharacterInputValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Magnitude"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeView("PipelineBlackboardValueNodeView")]
    public abstract class PipelineBlackboardValueInfoNode : ValueNode
    {
        [SerializeField]
        PipelineBlackboardVariableReference m_BlackboardVariable;

        [NonSerialized]
        bool m_ReportedSourceError;

        [NonSerialized]
        bool m_ReportedReadError;

        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;
        public abstract Type BlackboardValueType { get; }

#if UNITY_EDITOR
        public void ConfigureAuthoring(BaseExposedProperty declaration)
        {
            if (declaration == null || declaration.ValueType != BlackboardValueType)
                throw new InvalidOperationException($"{GetType().Name} requires {BlackboardValueType.Name} declaration.");

            m_BlackboardVariable = declaration.CreateBlackboardReference();
            m_ReportedSourceError = false;
            m_ReportedReadError = false;
            OnNodeChangedCallback();
        }
#endif

        protected bool TryReadBlackboardValue<T>(out T value)
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }

        void ReportReadError(string message)
        {
            if (m_ReportedReadError)
                return;

            m_ReportedReadError = true;
            Debug.LogError($"{GetType().Name}: {message}");
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard Bool")]
    [NodePath("Base/Value/Blackboard/Bool")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardBoolInfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        public override Type BlackboardValueType => typeof(bool);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out bool value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard Int")]
    [NodePath("Base/Value/Blackboard/Int")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardIntInfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        IntPropertyPort m_Output = new IntPropertyPort();

        public override Type BlackboardValueType => typeof(int);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out int value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard Float")]
    [NodePath("Base/Value/Blackboard/Float")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardFloatInfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        public override Type BlackboardValueType => typeof(float);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out float value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard String")]
    [NodePath("Base/Value/Blackboard/String")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardStringInfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        StringPropertyPort m_Output = new StringPropertyPort();

        public override Type BlackboardValueType => typeof(string);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out string value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard Vector2")]
    [NodePath("Base/Value/Blackboard/Vector2")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardVector2InfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        Vector2PropertyPort m_Output = new Vector2PropertyPort();

        public override Type BlackboardValueType => typeof(Vector2);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out Vector2 value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Pipeline Blackboard Vector3")]
    [NodePath("Base/Value/Blackboard/Vector3")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class PipelineBlackboardVector3InfoNode : PipelineBlackboardValueInfoNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        Vector3PropertyPort m_Output = new Vector3PropertyPort();

        public override Type BlackboardValueType => typeof(Vector3);

        protected override void OutputValue()
        {
            base.OutputValue();
            if (TryReadBlackboardValue(out Vector3 value))
                m_Output.Value = value;
        }
    }

    [Serializable]
    [NodeName("Character Action Request Info")]
    [NodePath("Base/Value/Input/Action Request Info/Has Request")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterActionRequestInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Request Id"), ReadOnly]
        string m_RequestId;

        [SerializeField, PropertyPort(PortDirection.Output, "Has Request"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        [NonSerialized]
        bool m_ReportedSourceError;

        [NonSerialized]
        bool m_ReportedReadError;

        public string RequestId => m_RequestId;

        public void BindActionRequest(string requestId)
        {
            m_RequestId = requestId;
            ResetReports();
#if UNITY_EDITOR
            OnNodeChangedCallback();
#endif
        }

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }

        void ReportSourceError(string message)
        {
            if (m_ReportedSourceError)
                return;

            m_ReportedSourceError = true;
            Debug.LogError($"{GetType().Name}: {message}");
        }

        void ReportReadError(string message)
        {
            if (m_ReportedReadError)
                return;

            m_ReportedReadError = true;
            Debug.LogError($"{GetType().Name}: {message}");
        }

        void ResetReports()
        {
            m_ReportedSourceError = false;
            m_ReportedReadError = false;
        }
    }

    [Serializable]
    [NodeName("State Exit Cause Info")]
    [NodePath("Base/Value/State/Exit Cause")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class StateExitCauseInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Cause")]
        StateExitCause m_Cause;

        [SerializeField, PropertyPort(PortDirection.Output, "Matches"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        public StateExitCause Cause => m_Cause;

#if UNITY_EDITOR
        public void ConfigureAuthoring(StateExitCause cause)
        {
            m_Cause = cause;
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Action Context Active Info")]
    [NodePath("Base/Value/Action/Context Active")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ActionContextActiveInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Active"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        public ActionContextSlot ActionContext => m_ActionContext;

#if UNITY_EDITOR
        public void ConfigureAuthoring(ActionContextSlot actionContext)
        {
            m_ActionContext = actionContext;
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

}
