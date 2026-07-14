using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Network;
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
            m_Result = ActionActivationResult.InvalidRequest;
            m_Activated.Value = false;

            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            if (!m_ActionProfile || string.IsNullOrEmpty(m_ActionProfile.ActionId))
                return;

            if (!TryResolveSourceRequest(context, out string sourceInputRequestId, out ulong inputSequence))
                return;

            ActionTargetSnapshot targetSnapshot = ResolveTargetSnapshot(context);
            ActionActivationRequest request = new ActionActivationRequest(
                m_ActionProfile.ActionId,
                sourceInputRequestId,
                inputSequence,
                context.LocalLogicTick,
                m_TargetKey,
                targetSnapshot,
                Owner != null ? Owner.GraphAuthoringId : string.Empty,
                GUID,
                GetType().Name);

            m_Result = context.SubmitActionActivation(request, out ActionInstanceHandle handle);
            m_Activated.Value = m_Result == ActionActivationResult.Activated;
            if (m_Activated.Value)
                context.SetActionContext(m_ActionContext, handle);
        }

        bool TryResolveSourceRequest(CharacterGraphContext context, out string sourceInputRequestId, out ulong inputSequence)
        {
            sourceInputRequestId = string.Empty;
            inputSequence = context.TickContext.InputSequence;
            if (string.IsNullOrEmpty(m_SourceInputRequestId))
                return true;

            CharacterInputRequest request;
            bool found = m_ConsumeSourceInputRequest
                ? context.TryConsumeInputRequest(m_SourceInputRequestId, out request)
                : context.TryGetInputRequest(m_SourceInputRequestId, out request);
            if (!found)
                return false;

            sourceInputRequestId = request.RequestId;
            inputSequence = request.InputSequence;
            return true;
        }

        ActionTargetSnapshot ResolveTargetSnapshot(CharacterGraphContext context)
        {
            if (m_TargetSnapshotVariable.IsValid &&
                context.TryGetBlackboardValue(Owner, m_TargetSnapshotVariable, out ActionTargetSnapshot snapshot))
                return snapshot;

            return ActionTargetSnapshot.None;
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
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
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            if (!context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle))
                return;

            var transition = new ActionLifecycleTransition(
                handle.ActionInstanceId,
                m_TransitionType,
                context.LocalLogicTick,
                handle.InputSequence,
                m_Reason,
                Owner != null ? Owner.GraphAuthoringId : string.Empty,
                GUID,
                GetType().Name);
            m_Submitted.Value = context.SubmitActionLifecycleTransition(transition);
            if (m_Submitted.Value)
                m_LastSubmissionDebug = $"{handle.ActionInstanceId} {m_TransitionType} {m_Reason} tick:{context.LocalLogicTick}";
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }

    [Serializable]
    [NodeName("Submit Gameplay Cue")]
    [NodePath("Base/Action/Output/Submit Gameplay Cue")]
    public sealed class SubmitGameplayCueNode : ActionNode
    {
        [SerializeField]
        PipelineBlackboardVariableReference m_BlackboardVariable;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Cue Id")]
        string m_CueId;

        [SerializeField, ShowInPanel("Cue Type")]
        string m_CueType;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;
        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            if (!context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle))
                return;

            var cue = new GameplayCueFact(
                handle.ActionId,
                m_CueId,
                m_CueType,
                handle.ActionInstanceId,
                default,
                default,
                default,
                context.LocalLogicTick);
            m_Submitted.Value = context.SubmitGameplayCue(Owner, m_BlackboardVariable, cue);
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }

    [Serializable]
    [NodeName("Submit Gameplay Result Event")]
    [NodePath("Base/Action/Output/Submit Gameplay Result Event")]
    public sealed class SubmitGameplayResultEventNode : ActionNode
    {
        [SerializeField]
        PipelineBlackboardVariableReference m_BlackboardVariable;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Require Action Context")]
        bool m_RequireActionContext = true;

        [SerializeField, ShowInPanel("Behavior Profile")]
        GameplayBehaviorProfile m_BehaviorProfile;

        [SerializeField, ShowInPanel("Result Type")]
        string m_ResultType = "ActionWindowResult";

        [SerializeField, ShowInPanel("Window Id")]
        string m_WindowId;

        [SerializeField, ShowInPanel("Target Id")]
        string m_TargetId;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;
        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            ulong actionInstanceId = 0;
            if (context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle))
                actionInstanceId = handle.ActionInstanceId;
            else if (m_RequireActionContext)
                return;

            string behaviorId = actionInstanceId == 0 && m_BehaviorProfile ? m_BehaviorProfile.BehaviorId : string.Empty;
            GameplayResultEvent resultEvent = new GameplayResultEvent(
                behaviorId,
                BuildResultId(actionInstanceId, context.LocalLogicTick, GUID),
                actionInstanceId,
                m_WindowId,
                m_TargetId,
                m_ResultType,
                context.LocalLogicTick);
            m_Submitted.Value = context.SubmitGameplayResultEvent(Owner, m_BlackboardVariable, resultEvent);
        }

        static ulong BuildResultId(ulong actionInstanceId, ulong localLogicTick, string sourceId)
        {
            ulong sourceHash = StableHash(sourceId);
            return (actionInstanceId != 0 ? actionInstanceId : sourceHash) * 1000003UL + localLogicTick;
        }

        static ulong StableHash(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }
            }

            return hash == 0 ? 1UL : hash;
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }
}
