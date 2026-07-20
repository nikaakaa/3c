using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    [NodeName("Activate Action Instance")]
    [NodePath("Base/Action/Low Level/Activate Action Instance")]
    public sealed class ActivateActionInstanceNode : ActionNode
    {
        [SerializeField, ShowInPanel("Action Profile")]
        ActionProfile m_ActionProfile;

        [SerializeField, ShowInPanel("Source Input Request Id")]
        string m_SourceInputRequestId;

        [SerializeField, ShowInPanel("Consume Source Input Request")]
        bool m_ConsumeSourceInputRequest = true;

        [SerializeField, ShowInPanel("Target Key")]
        string m_TargetKey;

        [SerializeField]
        PipelineBlackboardVariableReference m_TargetSnapshotVariable;

        [SerializeField, ShowInPanel("Output Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Activated"), ReadOnly]
        BoolPropertyPort m_Activated = new BoolPropertyPort();

        [NonSerialized]
        ActionActivationResult m_Result = ActionActivationResult.InvalidRequest;

        public override State ReturnState => m_Result == ActionActivationResult.Activated ? State.Success : State.Failure;
        public ActionProfile ActionProfile => m_ActionProfile;
        public string SourceInputRequestId => m_SourceInputRequestId;
        public bool ConsumeSourceInputRequest => m_ConsumeSourceInputRequest;
        public string TargetKey => m_TargetKey;
        public PipelineBlackboardVariableReference TargetSnapshotVariable => m_TargetSnapshotVariable;
        public ActionContextSlot ActionContext => m_ActionContext;

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            ActionProfile actionProfile,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            ActionContextSlot actionContext,
            string targetKey,
            PipelineBlackboardVariableReference targetSnapshotVariable)
        {
            m_ActionProfile = actionProfile;
            m_SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            m_ConsumeSourceInputRequest = consumeSourceInputRequest;
            m_ActionContext = actionContext;
            m_TargetKey = targetKey ?? string.Empty;
            m_TargetSnapshotVariable = targetSnapshotVariable;
            OnNodeChangedCallback();
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (var reference in base.GetAssetReferences())
                yield return reference;

            yield return new NodeAssetReference(this, "m_ActionProfile", "Action Profile", m_ActionProfile, true);
            yield return new NodeAssetReference(this, "m_ActionContext", "Action Context", m_ActionContext, false);
        }
#endif

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Submit Action Lifecycle Transition")]
    [NodePath("Base/Action/Low Level/Submit Action Lifecycle Transition")]
    public sealed class SubmitActionLifecycleTransitionNode : ActionNode
    {
        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Transition Type")]
        ActionLifecycleTransitionType m_TransitionType = ActionLifecycleTransitionType.Complete;

        [SerializeField, ShowInPanel("Reason")]
        string m_Reason;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        [NonSerialized]
        string m_LastSubmissionDebug = string.Empty;

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;
        public ActionContextSlot ActionContext => m_ActionContext;
        public ActionLifecycleTransitionType TransitionType => m_TransitionType;
        public string Reason => m_Reason;
        [TreeDesigner.ShowInInspector("Last Lifecycle Submission")]
        public string LastSubmissionDebug => m_LastSubmissionDebug;

#if UNITY_EDITOR
        public void ConfigureAuthoring(ActionContextSlot actionContext, ActionLifecycleTransitionType transitionType, string reason)
        {
            m_ActionContext = actionContext;
            m_TransitionType = transitionType;
            m_Reason = reason ?? string.Empty;
            OnNodeChangedCallback();
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (var reference in base.GetAssetReferences())
                yield return reference;

            yield return new NodeAssetReference(this, "m_ActionContext", "Action Context", m_ActionContext, false);
        }
#endif

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Action Window Active Info")]
    [NodePath("Base/Value/Action/Window Active")]
    public sealed class ActionWindowActiveInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Window Type")]
        string m_WindowType;

        [SerializeField, PropertyPort(PortDirection.Output, "Active"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        public string WindowType => m_WindowType ?? string.Empty;

#if UNITY_EDITOR
        public void ConfigureAuthoring(string windowType)
        {
            m_WindowType = string.IsNullOrWhiteSpace(windowType) ? string.Empty : windowType.Trim();
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Can Activate Action Info")]
    [NodePath("Base/Value/Action/Can Activate")]
    public sealed class CanActivateActionInfoNode : ValueNode
    {
        [SerializeField, ShowInPanel("Target Action Profile")]
        ActionProfile m_ActionProfile;

        [SerializeField]
        PipelineBlackboardVariableReference m_TargetSnapshotVariable;

        [SerializeField, PropertyPort(PortDirection.Output, "Allowed"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        public ActionProfile ActionProfile => m_ActionProfile;
        public PipelineBlackboardVariableReference TargetSnapshotVariable => m_TargetSnapshotVariable;

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            ActionProfile actionProfile,
            PipelineBlackboardVariableReference targetSnapshotVariable)
        {
            m_ActionProfile = actionProfile;
            m_TargetSnapshotVariable = targetSnapshotVariable;
            OnNodeChangedCallback();
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (NodeAssetReference reference in base.GetAssetReferences())
                yield return reference;

            yield return new NodeAssetReference(this, "m_ActionProfile", "Target Action Profile", m_ActionProfile, true);
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

}
