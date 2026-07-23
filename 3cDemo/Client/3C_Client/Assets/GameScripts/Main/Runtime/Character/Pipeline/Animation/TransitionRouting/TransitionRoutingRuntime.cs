using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{
    public sealed class TransitionRoutingWorkspace
    {
        readonly TransitionRoutingEvent[] m_Events;
        int m_EventStart;
        int m_EventCount;

        internal bool IsBound;
        internal TransitionRoutingPlanId PlanId;
        internal TransitionDefinitionRevision DefinitionRevision;
        internal TransitionRouteOwnerId OwnerNodeId;
        internal TransitionFrameId LastFrameId;
        internal TransitionEndpointId CurrentEndpoint;
        internal TransitionEndpointId RequestedEndpoint;
        internal TransitionSelectionGeneration SelectionGeneration;
        internal TransitionRoutingLifecycle Lifecycle;
        internal TransitionRuleId ActiveRuleId;
        internal PoseInertializationRequest ActiveRequest;
        internal bool HasActiveRequest;
        internal bool HasStandardCommand;
        internal TransitionEndpointId StandardTarget;
        internal TransitionRuleId StandardRuleId;
        internal TransitionSelectionGeneration StandardSelectionGeneration;
        internal bool HasInertialIntent;
        internal bool PendingRebaseRequired;
        internal bool CaptureCompleted;
        internal bool ReleaseCompleted;
        internal ulong RequestGenerationValue;
        internal ulong ModuleGenerationValue;
        internal ulong RebaseCount;
        internal TransitionRoutingResetReason LastResetReason;
        internal TransitionRoutingReasonCode LastReasonCode;
        internal string LastReason = string.Empty;

        public TransitionRoutingWorkspace(int eventCapacity = 128)
        {
            if (eventCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventCapacity));
            m_Events = new TransitionRoutingEvent[eventCapacity];
            Lifecycle = TransitionRoutingLifecycle.Idle;
        }

        public TransitionRoutingRuntimeSnapshot Snapshot { get; internal set; }
        public int EventCount => m_EventCount;

        public TransitionRoutingEvent[] CopyEvents()
        {
            var result = new TransitionRoutingEvent[m_EventCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = m_Events[(m_EventStart + i) % m_Events.Length];
            return result;
        }

        public void ClearEvents()
        {
            Array.Clear(m_Events, 0, m_Events.Length);
            m_EventStart = 0;
            m_EventCount = 0;
        }

        internal void Append(TransitionRoutingEvent item)
        {
            int writeIndex;
            if (m_EventCount < m_Events.Length)
            {
                writeIndex = (m_EventStart + m_EventCount) % m_Events.Length;
                m_EventCount++;
            }
            else
            {
                writeIndex = m_EventStart;
                m_EventStart = (m_EventStart + 1) % m_Events.Length;
            }

            m_Events[writeIndex] = item;
        }
    }

    public static class TransitionRoutingRuntime
    {
        public static TransitionRoutingFrameOutput Step(
            CompiledTransitionRoutingPlan plan,
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (plan == null)
                return Invalid(workspace, input, TransitionRoutingReasonCode.PlanIdentityMismatch, "Compiled plan is required.");
            if (!input.PlanId.IsValid || input.PlanId != plan.PlanId)
                return Invalid(workspace, input, TransitionRoutingReasonCode.PlanIdentityMismatch, "Frame PlanId does not match the compiled plan.");
            if (!input.FrameId.IsValid)
                return Invalid(workspace, input, TransitionRoutingReasonCode.InvalidFrameIdentity, "Frame identity must be positive.");
            if (!input.OwnerNodeId.IsValid)
                return Invalid(workspace, input, TransitionRoutingReasonCode.InvalidOwnerIdentity, "Owner node identity is required.");
            if (!input.CurrentEndpoint.IsValid || !input.RequestedEndpoint.IsValid)
                return Invalid(workspace, input, TransitionRoutingReasonCode.InvalidEndpoint, "Current and requested endpoints are required.");
            if (!input.SelectionGeneration.IsValid)
                return Invalid(workspace, input, TransitionRoutingReasonCode.InvalidSelectionGeneration, "Selection generation must be positive.");

            if (!workspace.IsBound)
                Bind(workspace, plan, input.OwnerNodeId);

            if (input.ResetReason != TransitionRoutingResetReason.None)
                return ApplyReset(plan, workspace, input);

            if (workspace.PlanId != plan.PlanId)
                return Invalid(workspace, input, TransitionRoutingReasonCode.PlanIdentityMismatch, "Plan replacement requires an explicit PlanReplacement reset.");
            if (workspace.OwnerNodeId != input.OwnerNodeId)
                return Invalid(workspace, input, TransitionRoutingReasonCode.OwnerIdentityMismatch, "Owner replacement requires an explicit OwnerGenerationChanged reset.");
            if (workspace.LastFrameId.IsValid && input.FrameId.Value <= workspace.LastFrameId.Value)
                return Invalid(workspace, input, TransitionRoutingReasonCode.NonMonotonicFrame, "Frame identity must increase monotonically.");

            workspace.LastFrameId = input.FrameId;
            workspace.CurrentEndpoint = input.CurrentEndpoint;
            workspace.RequestedEndpoint = input.RequestedEndpoint;
            workspace.SelectionGeneration = input.SelectionGeneration;
            workspace.LastResetReason = TransitionRoutingResetReason.None;
            workspace.LastReasonCode = TransitionRoutingReasonCode.None;
            workspace.LastReason = string.Empty;

            TransitionRoutingDecisionKind completionDecision = TransitionRoutingDecisionKind.None;
            bool releasePermission = false;
            if (input.CaptureCompletion.IsPresent)
            {
                TransitionRoutingFrameOutput captureResult = ApplyCaptureCompletion(workspace, input);
                if (captureResult.IsInvalid)
                    return captureResult;
                completionDecision = captureResult.DecisionKind;
                releasePermission = captureResult.ReleasePermission;
            }

            if (input.ReleaseCompletion.IsPresent)
            {
                TransitionRoutingFrameOutput releaseResult = ApplyReleaseCompletion(workspace, input);
                if (releaseResult.IsInvalid)
                    return releaseResult;
                completionDecision = releaseResult.DecisionKind;
            }

            if (input.CurrentEndpoint == input.RequestedEndpoint)
            {
                workspace.HasStandardCommand = false;
                if (!workspace.HasActiveRequest)
                {
                    workspace.HasInertialIntent = false;
                    workspace.PendingRebaseRequired = false;
                    workspace.Lifecycle = TransitionRoutingLifecycle.Idle;
                }

                return Complete(
                    workspace,
                    input,
                    completionDecision,
                    workspace.Lifecycle,
                    releasePermission: releasePermission);
            }

            if (!plan.TryGetRule(input.CurrentEndpoint, input.RequestedEndpoint, out AnimationTransitionRule rule))
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.MissingCompiledRule,
                    $"Compiled rule is missing for '{input.CurrentEndpoint}' -> '{input.RequestedEndpoint}'.");
            }

            workspace.ActiveRuleId = rule.RuleId;
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.CompiledRuleSelected,
                workspace.Lifecycle,
                rule.RuleId,
                workspace.HasActiveRequest ? workspace.ActiveRequest.RequestEventId : default,
                workspace.HasActiveRequest ? workspace.ActiveRequest.RequestGeneration : default,
                TransitionRoutingReasonCode.None,
                $"{rule.SourceEndpoint} -> {rule.TargetEndpoint}: {rule.BlendLogic}"));

            return rule.BlendLogic == AnimationTransitionBlendLogic.StandardBlend
                ? ApplyStandardBlend(workspace, input, rule, releasePermission)
                : ApplyInertialization(workspace, input, rule, releasePermission);
        }

        static TransitionRoutingFrameOutput ApplyStandardBlend(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            in AnimationTransitionRule rule,
            bool releasePermission)
        {
            if (workspace.HasActiveRequest || workspace.HasInertialIntent)
            {
                workspace.RequestGenerationValue++;
                workspace.HasActiveRequest = false;
                workspace.ActiveRequest = default;
                workspace.HasInertialIntent = false;
                workspace.PendingRebaseRequired = false;
                workspace.CaptureCompleted = false;
                workspace.ReleaseCompleted = false;
            }

            bool duplicate =
                workspace.HasStandardCommand &&
                workspace.StandardRuleId == rule.RuleId &&
                workspace.StandardTarget == input.RequestedEndpoint &&
                workspace.StandardSelectionGeneration == input.SelectionGeneration;

            workspace.Lifecycle = TransitionRoutingLifecycle.Idle;
            workspace.HasStandardCommand = true;
            workspace.StandardRuleId = rule.RuleId;
            workspace.StandardTarget = input.RequestedEndpoint;
            workspace.StandardSelectionGeneration = input.SelectionGeneration;

            if (duplicate)
                return Complete(workspace, input, TransitionRoutingDecisionKind.None, workspace.Lifecycle, releasePermission: releasePermission);

            var command = new StandardBlendCommand(rule);
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.StandardBlendIssued,
                workspace.Lifecycle,
                rule.RuleId,
                default,
                default,
                TransitionRoutingReasonCode.None,
                command.IsHardCutOutcome ? "Hard Cut outcome" : "Standard Blend command"));
            return Complete(
                workspace,
                input,
                TransitionRoutingDecisionKind.StandardBlend,
                workspace.Lifecycle,
                command,
                true,
                releasePermission: releasePermission);
        }

        static TransitionRoutingFrameOutput ApplyInertialization(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            in AnimationTransitionRule rule,
            bool releasePermission)
        {
            workspace.HasStandardCommand = false;
            bool matchingRequest =
                workspace.HasActiveRequest &&
                workspace.ActiveRequest.RuleId == rule.RuleId &&
                workspace.ActiveRequest.SourceEndpoint == input.CurrentEndpoint &&
                workspace.ActiveRequest.TargetEndpoint == input.RequestedEndpoint &&
                workspace.ActiveRequest.SelectionGeneration == input.SelectionGeneration;

            if (!input.TargetReady || !input.CapturePlanReady)
            {
                bool replacedRequest = workspace.HasActiveRequest && !matchingRequest;
                if (replacedRequest)
                {
                    workspace.RequestGenerationValue++;
                    workspace.HasActiveRequest = false;
                    workspace.ActiveRequest = default;
                    workspace.PendingRebaseRequired = true;
                }

                workspace.HasInertialIntent = true;
                workspace.ActiveRuleId = rule.RuleId;
                workspace.RequestedEndpoint = input.RequestedEndpoint;
                workspace.SelectionGeneration = input.SelectionGeneration;
                workspace.Lifecycle = TransitionRoutingLifecycle.AwaitingTarget;
                TransitionRoutingReasonCode code = input.TargetReady
                    ? TransitionRoutingReasonCode.CapturePlanNotReady
                    : TransitionRoutingReasonCode.TargetNotReady;
                string reason = input.TargetReady ? "Capture plan is not ready." : "Target is not ready.";
                workspace.LastReasonCode = code;
                workspace.LastReason = reason;
                workspace.Append(new TransitionRoutingEvent(
                    input.FrameId,
                    TransitionRoutingEventKind.AwaitingTarget,
                    workspace.Lifecycle,
                    rule.RuleId,
                    default,
                    default,
                    code,
                    reason));
                return Complete(
                    workspace,
                    input,
                    TransitionRoutingDecisionKind.AwaitingReadiness,
                    workspace.Lifecycle,
                    reasonCode: code,
                    reason: reason,
                    releasePermission: releasePermission);
            }

            if (matchingRequest)
            {
                if (workspace.Lifecycle == TransitionRoutingLifecycle.Prepared)
                {
                    workspace.Lifecycle = TransitionRoutingLifecycle.AwaitingCaptureCompletion;
                    workspace.Append(new TransitionRoutingEvent(
                        input.FrameId,
                        TransitionRoutingEventKind.AwaitingCapture,
                        workspace.Lifecycle,
                        rule.RuleId,
                        workspace.ActiveRequest.RequestEventId,
                        workspace.ActiveRequest.RequestGeneration,
                        TransitionRoutingReasonCode.None,
                        "Waiting for capture completion."));
                }

                return Complete(
                    workspace,
                    input,
                    TransitionRoutingDecisionKind.None,
                    workspace.Lifecycle,
                    releasePermission: releasePermission);
            }

            bool rebaseRequired =
                workspace.PendingRebaseRequired ||
                workspace.HasActiveRequest ||
                workspace.Lifecycle == TransitionRoutingLifecycle.Prepared ||
                workspace.Lifecycle == TransitionRoutingLifecycle.AwaitingCaptureCompletion ||
                workspace.Lifecycle == TransitionRoutingLifecycle.Committed;

            workspace.RequestGenerationValue++;
            var requestGeneration = new TransitionRequestGeneration(workspace.RequestGenerationValue);
            var requestEventId = new TransitionRequestEventId(StableHash.Compute(
                "transition-routing-request",
                workspace.PlanId.ToString(),
                workspace.OwnerNodeId.ToString(),
                rule.RuleId.ToString(),
                input.CurrentEndpoint.ToString(),
                input.RequestedEndpoint.ToString(),
                input.SelectionGeneration.ToString(),
                requestGeneration.ToString(),
                workspace.ModuleGenerationValue.ToString()));
            var request = new PoseInertializationRequest(
                requestEventId,
                workspace.OwnerNodeId,
                rule.RuleId,
                input.CurrentEndpoint,
                input.RequestedEndpoint,
                input.SelectionGeneration,
                requestGeneration,
                rule.DurationSeconds,
                rule.BlendProfileId);

            workspace.ActiveRequest = request;
            workspace.HasActiveRequest = true;
            workspace.HasInertialIntent = true;
            workspace.PendingRebaseRequired = false;
            workspace.CaptureCompleted = false;
            workspace.ReleaseCompleted = false;
            workspace.Lifecycle = TransitionRoutingLifecycle.Prepared;
            if (rebaseRequired)
                workspace.RebaseCount++;

            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                rebaseRequired ? TransitionRoutingEventKind.Rebased : TransitionRoutingEventKind.RequestPrepared,
                workspace.Lifecycle,
                rule.RuleId,
                requestEventId,
                requestGeneration,
                TransitionRoutingReasonCode.None,
                rebaseRequired ? "Request prepared with rebase." : "Request prepared."));
            return Complete(
                workspace,
                input,
                TransitionRoutingDecisionKind.InertializationRequest,
                workspace.Lifecycle,
                request: request,
                hasRequest: true,
                capturePermission: true,
                releasePermission: releasePermission,
                rebaseRequired: rebaseRequired);
        }

        static TransitionRoutingFrameOutput ApplyCaptureCompletion(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input)
        {
            if (!workspace.HasActiveRequest ||
                (workspace.Lifecycle != TransitionRoutingLifecycle.Prepared &&
                 workspace.Lifecycle != TransitionRoutingLifecycle.AwaitingCaptureCompletion))
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.UnexpectedCaptureCompletion,
                    "Capture completion has no matching prepared request.");
            }

            if (!Matches(workspace.ActiveRequest, input.CaptureCompletion))
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.CaptureCompletionIdentityMismatch,
                    "Capture completion identity or generation does not match the current request.");
            }

            if (!input.CaptureCompletion.Succeeded)
                return Invalid(workspace, input, TransitionRoutingReasonCode.CaptureFailed, "Capture completion reported failure.");

            workspace.CaptureCompleted = true;
            workspace.Lifecycle = TransitionRoutingLifecycle.Committed;
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.CaptureCommitted,
                workspace.Lifecycle,
                workspace.ActiveRequest.RuleId,
                workspace.ActiveRequest.RequestEventId,
                workspace.ActiveRequest.RequestGeneration,
                TransitionRoutingReasonCode.None,
                "Capture committed; old source release is permitted."));
            return Complete(
                workspace,
                input,
                TransitionRoutingDecisionKind.CaptureCommitted,
                workspace.Lifecycle,
                releasePermission: true);
        }

        static TransitionRoutingFrameOutput ApplyReleaseCompletion(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input)
        {
            if (!workspace.HasActiveRequest || workspace.Lifecycle != TransitionRoutingLifecycle.Committed)
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.UnexpectedReleaseCompletion,
                    "Release completion has no matching committed request.");
            }

            if (!Matches(workspace.ActiveRequest, input.ReleaseCompletion))
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.ReleaseCompletionIdentityMismatch,
                    "Release completion identity or generation does not match the current request.");
            }

            if (!input.ReleaseCompletion.Succeeded)
                return Invalid(workspace, input, TransitionRoutingReasonCode.ReleaseFailed, "Release completion reported failure.");

            PoseInertializationRequest completedRequest = workspace.ActiveRequest;
            workspace.ReleaseCompleted = true;
            workspace.HasActiveRequest = false;
            workspace.ActiveRequest = default;
            workspace.HasInertialIntent = false;
            workspace.PendingRebaseRequired = false;
            workspace.Lifecycle = TransitionRoutingLifecycle.Idle;
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.ReleaseCompleted,
                workspace.Lifecycle,
                completedRequest.RuleId,
                completedRequest.RequestEventId,
                completedRequest.RequestGeneration,
                TransitionRoutingReasonCode.None,
                "Release completed."));
            return Complete(workspace, input, TransitionRoutingDecisionKind.ReleaseCompleted, workspace.Lifecycle);
        }

        static TransitionRoutingFrameOutput ApplyReset(
            CompiledTransitionRoutingPlan plan,
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input)
        {
            workspace.IsBound = true;
            workspace.PlanId = plan.PlanId;
            workspace.DefinitionRevision = plan.DefinitionRevision;
            workspace.OwnerNodeId = input.OwnerNodeId;
            workspace.LastFrameId = input.FrameId;
            workspace.CurrentEndpoint = input.CurrentEndpoint;
            workspace.RequestedEndpoint = input.RequestedEndpoint;
            workspace.SelectionGeneration = input.SelectionGeneration;
            workspace.Lifecycle = TransitionRoutingLifecycle.Idle;
            workspace.ActiveRuleId = default;
            workspace.ActiveRequest = default;
            workspace.HasActiveRequest = false;
            workspace.HasStandardCommand = false;
            workspace.HasInertialIntent = false;
            workspace.PendingRebaseRequired = false;
            workspace.CaptureCompleted = false;
            workspace.ReleaseCompleted = false;
            workspace.RequestGenerationValue++;
            workspace.ModuleGenerationValue++;
            workspace.LastResetReason = input.ResetReason;
            workspace.LastReasonCode = TransitionRoutingReasonCode.ResetApplied;
            workspace.LastReason = $"Reset applied: {input.ResetReason}.";
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.Reset,
                workspace.Lifecycle,
                default,
                default,
                default,
                TransitionRoutingReasonCode.ResetApplied,
                workspace.LastReason));
            return Complete(
                workspace,
                input,
                TransitionRoutingDecisionKind.Reset,
                workspace.Lifecycle,
                reasonCode: TransitionRoutingReasonCode.ResetApplied,
                reason: workspace.LastReason);
        }

        static void Bind(
            TransitionRoutingWorkspace workspace,
            CompiledTransitionRoutingPlan plan,
            TransitionRouteOwnerId ownerNodeId)
        {
            workspace.IsBound = true;
            workspace.PlanId = plan.PlanId;
            workspace.DefinitionRevision = plan.DefinitionRevision;
            workspace.OwnerNodeId = ownerNodeId;
            workspace.ModuleGenerationValue = 1;
            workspace.Lifecycle = TransitionRoutingLifecycle.Idle;
        }

        static bool Matches(
            in PoseInertializationRequest request,
            in TransitionCompletionFact completion) =>
            completion.IsPresent &&
            completion.RequestEventId == request.RequestEventId &&
            completion.RequestGeneration == request.RequestGeneration;

        static TransitionRoutingFrameOutput Invalid(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            TransitionRoutingReasonCode code,
            string reason)
        {
            workspace.Lifecycle = TransitionRoutingLifecycle.Invalid;
            workspace.LastReasonCode = code;
            workspace.LastReason = reason ?? string.Empty;
            workspace.Append(new TransitionRoutingEvent(
                input.FrameId,
                TransitionRoutingEventKind.Invalid,
                workspace.Lifecycle,
                workspace.ActiveRuleId,
                workspace.HasActiveRequest ? workspace.ActiveRequest.RequestEventId : default,
                workspace.HasActiveRequest ? workspace.ActiveRequest.RequestGeneration : default,
                code,
                workspace.LastReason));
            return Complete(
                workspace,
                input,
                TransitionRoutingDecisionKind.Invalid,
                workspace.Lifecycle,
                reasonCode: code,
                reason: workspace.LastReason);
        }

        static TransitionRoutingFrameOutput Complete(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            TransitionRoutingDecisionKind decisionKind,
            TransitionRoutingLifecycle lifecycle,
            StandardBlendCommand command = default,
            bool hasCommand = false,
            PoseInertializationRequest request = default,
            bool hasRequest = false,
            bool capturePermission = false,
            bool releasePermission = false,
            bool rebaseRequired = false,
            TransitionRoutingReasonCode reasonCode = TransitionRoutingReasonCode.None,
            string reason = "")
        {
            if (reasonCode != TransitionRoutingReasonCode.None)
            {
                workspace.LastReasonCode = reasonCode;
                workspace.LastReason = reason ?? string.Empty;
            }

            var output = new TransitionRoutingFrameOutput(
                decisionKind,
                lifecycle,
                workspace.ActiveRuleId,
                command,
                hasCommand,
                request,
                hasRequest,
                capturePermission,
                releasePermission,
                rebaseRequired,
                reasonCode,
                reason);
            workspace.Snapshot = new TransitionRoutingRuntimeSnapshot(
                workspace.PlanId,
                workspace.DefinitionRevision,
                workspace.OwnerNodeId,
                input.FrameId,
                input.CurrentEndpoint,
                input.RequestedEndpoint,
                input.SelectionGeneration,
                workspace.ModuleGenerationValue,
                workspace.Lifecycle,
                workspace.ActiveRuleId,
                workspace.ActiveRequest,
                workspace.HasActiveRequest,
                workspace.CaptureCompleted,
                workspace.ReleaseCompleted,
                workspace.RebaseCount,
                workspace.LastResetReason,
                workspace.LastReasonCode,
                workspace.LastReason);
            return output;
        }
    }
}
