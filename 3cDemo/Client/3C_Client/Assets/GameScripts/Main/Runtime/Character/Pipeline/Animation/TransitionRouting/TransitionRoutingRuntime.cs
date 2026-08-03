using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{

    public sealed class TransitionRoutingWorkspace
    {
        readonly TransitionRoutingEventJournal m_Events;
        readonly TransitionRoutingWorkspaceState m_State;

        internal bool IsBound { get => m_State.IsBound; set => m_State.IsBound = value; }
        internal TransitionRoutingPlanId PlanId { get => m_State.PlanId; set => m_State.PlanId = value; }
        internal TransitionDefinitionRevision DefinitionRevision { get => m_State.DefinitionRevision; set => m_State.DefinitionRevision = value; }
        internal TransitionRouteOwnerId OwnerNodeId { get => m_State.OwnerNodeId; set => m_State.OwnerNodeId = value; }
        internal TransitionFrameId LastFrameId { get => m_State.LastFrameId; set => m_State.LastFrameId = value; }
        internal TransitionEndpointId CurrentEndpoint { get => m_State.CurrentEndpoint; set => m_State.CurrentEndpoint = value; }
        internal TransitionEndpointId RequestedEndpoint { get => m_State.RequestedEndpoint; set => m_State.RequestedEndpoint = value; }
        internal TransitionSelectionGeneration SelectionGeneration { get => m_State.SelectionGeneration; set => m_State.SelectionGeneration = value; }
        internal TransitionRoutingLifecycle Lifecycle { get => m_State.Lifecycle; set => m_State.Lifecycle = value; }
        internal TransitionRuleId ActiveRuleId { get => m_State.ActiveRuleId; set => m_State.ActiveRuleId = value; }
        internal PoseInertializationRequest ActiveRequest { get => m_State.ActiveRequest; set => m_State.ActiveRequest = value; }
        internal bool HasActiveRequest { get => m_State.HasActiveRequest; set => m_State.HasActiveRequest = value; }
        internal bool HasStandardCommand { get => m_State.HasStandardCommand; set => m_State.HasStandardCommand = value; }
        internal TransitionEndpointId StandardTarget { get => m_State.StandardTarget; set => m_State.StandardTarget = value; }
        internal TransitionRuleId StandardRuleId { get => m_State.StandardRuleId; set => m_State.StandardRuleId = value; }
        internal TransitionSelectionGeneration StandardSelectionGeneration { get => m_State.StandardSelectionGeneration; set => m_State.StandardSelectionGeneration = value; }
        internal bool HasInertialIntent { get => m_State.HasInertialIntent; set => m_State.HasInertialIntent = value; }
        internal bool PendingRebaseRequired { get => m_State.PendingRebaseRequired; set => m_State.PendingRebaseRequired = value; }
        internal bool CaptureCompleted { get => m_State.CaptureCompleted; set => m_State.CaptureCompleted = value; }
        internal bool ReleaseCompleted { get => m_State.ReleaseCompleted; set => m_State.ReleaseCompleted = value; }
        internal ulong RequestGenerationValue { get => m_State.RequestGenerationValue; set => m_State.RequestGenerationValue = value; }
        internal ulong ModuleGenerationValue { get => m_State.ModuleGenerationValue; set => m_State.ModuleGenerationValue = value; }
        internal ulong RebaseCount { get => m_State.RebaseCount; set => m_State.RebaseCount = value; }
        internal TransitionRoutingResetReason LastResetReason { get => m_State.LastResetReason; set => m_State.LastResetReason = value; }
        internal TransitionRoutingReasonCode LastReasonCode { get => m_State.LastReasonCode; set => m_State.LastReasonCode = value; }
        internal string LastReason { get => m_State.LastReason; set => m_State.LastReason = value; }

        public TransitionRoutingWorkspace(int eventCapacity = 128)
        {
            if (eventCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventCapacity));
            m_State = new TransitionRoutingWorkspaceState();
            m_Events = new TransitionRoutingEventJournal(eventCapacity);
        }

        public TransitionRoutingRuntimeSnapshot Snapshot { get => m_State.Snapshot; internal set => m_State.Snapshot = value; }
        public int EventCount => m_Events.Count;

        public TransitionRoutingEvent[] CopyEvents()
        {
            return m_Events.CopyEvents();
        }

        public void ClearEvents()
        {
            m_Events.Clear();
        }

        public void BeginFrame()
        {
            m_State.BeginFrame();
            m_Events.BeginFrame();
        }

        public void CommitFrame()
        {
            m_State.CommitFrame();
            m_Events.CommitFrame();
        }

        public void DiscardFrame()
        {
            m_State.DiscardFrame();
            m_Events.DiscardFrame();
        }

        internal void Append(TransitionRoutingEvent item)
        {
            m_Events.Append(item);
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

            bool selectionGenerationChanged =
                workspace.SelectionGeneration.IsValid &&
                workspace.SelectionGeneration != input.SelectionGeneration;
            workspace.LastFrameId = input.FrameId;
            workspace.CurrentEndpoint = input.CurrentEndpoint;
            workspace.RequestedEndpoint = input.RequestedEndpoint;
            workspace.SelectionGeneration = input.SelectionGeneration;
            workspace.LastResetReason = TransitionRoutingResetReason.None;
            workspace.LastReasonCode = TransitionRoutingReasonCode.None;
            workspace.LastReason = string.Empty;

            if (input.CaptureCompletion.IsPresent && input.ReleaseCompletion.IsPresent)
            {
                return Invalid(
                    workspace,
                    input,
                    TransitionRoutingReasonCode.ConflictingCompletionFacts,
                    "Capture and release completion cannot be reported in the same frame.");
            }

            TransitionRoutingCompletionOutcome completionOutcome = TransitionRoutingCompletionOutcome.None;
            bool releasePermission = false;
            if (input.CaptureCompletion.IsPresent)
            {
                TransitionRoutingFrameOutput captureResult = ApplyCaptureCompletion(workspace, input);
                if (captureResult.IsInvalid)
                    return captureResult;
                completionOutcome = captureResult.CompletionOutcome;
                releasePermission = captureResult.ReleasePermission;
            }

            if (input.ReleaseCompletion.IsPresent)
            {
                TransitionRoutingFrameOutput releaseResult = ApplyReleaseCompletion(workspace, input);
                if (releaseResult.IsInvalid)
                    return releaseResult;
                completionOutcome = releaseResult.CompletionOutcome;
            }

            if (input.CurrentEndpoint == input.RequestedEndpoint && !selectionGenerationChanged)
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
                    TransitionRouteDecisionKind.None,
                    workspace.Lifecycle,
                    releasePermission: releasePermission,
                    completionOutcome: completionOutcome);
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
                ? ApplyStandardBlend(workspace, input, rule, releasePermission, completionOutcome)
                : ApplyInertialization(workspace, input, rule, releasePermission, completionOutcome);
        }

        static TransitionRoutingFrameOutput ApplyStandardBlend(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            in AnimationTransitionRule rule,
            bool releasePermission,
            TransitionRoutingCompletionOutcome completionOutcome)
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
                return Complete(
                    workspace,
                    input,
                    TransitionRouteDecisionKind.None,
                    workspace.Lifecycle,
                    releasePermission: releasePermission,
                    completionOutcome: completionOutcome);

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
                TransitionRouteDecisionKind.StandardBlend,
                workspace.Lifecycle,
                command,
                true,
                releasePermission: releasePermission,
                completionOutcome: completionOutcome);
        }

        static TransitionRoutingFrameOutput ApplyInertialization(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            in AnimationTransitionRule rule,
            bool releasePermission,
            TransitionRoutingCompletionOutcome completionOutcome)
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
                    TransitionRouteDecisionKind.AwaitingReadiness,
                    workspace.Lifecycle,
                    reasonCode: code,
                    reason: reason,
                    releasePermission: releasePermission,
                    completionOutcome: completionOutcome);
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
                    TransitionRouteDecisionKind.None,
                    workspace.Lifecycle,
                    releasePermission: releasePermission,
                    completionOutcome: completionOutcome);
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
                TransitionRouteDecisionKind.InertializationRequest,
                workspace.Lifecycle,
                request: request,
                hasRequest: true,
                capturePermission: true,
                releasePermission: releasePermission,
                rebaseRequired: rebaseRequired,
                completionOutcome: completionOutcome);
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
                TransitionRouteDecisionKind.None,
                workspace.Lifecycle,
                releasePermission: true,
                completionOutcome: TransitionRoutingCompletionOutcome.CaptureCommitted);
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
            return Complete(
                workspace,
                input,
                TransitionRouteDecisionKind.None,
                workspace.Lifecycle,
                completionOutcome: TransitionRoutingCompletionOutcome.ReleaseCompleted);
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
                TransitionRouteDecisionKind.Reset,
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
                TransitionRouteDecisionKind.Invalid,
                workspace.Lifecycle,
                reasonCode: code,
                reason: workspace.LastReason);
        }

        static TransitionRoutingFrameOutput Complete(
            TransitionRoutingWorkspace workspace,
            in TransitionRoutingFrameInput input,
            TransitionRouteDecisionKind routeDecision,
            TransitionRoutingLifecycle lifecycle,
            StandardBlendCommand command = default,
            bool hasCommand = false,
            PoseInertializationRequest request = default,
            bool hasRequest = false,
            bool capturePermission = false,
            bool releasePermission = false,
            bool rebaseRequired = false,
            TransitionRoutingReasonCode reasonCode = TransitionRoutingReasonCode.None,
            string reason = "",
            TransitionRoutingCompletionOutcome completionOutcome = TransitionRoutingCompletionOutcome.None)
        {
            if (reasonCode != TransitionRoutingReasonCode.None)
            {
                workspace.LastReasonCode = reasonCode;
                workspace.LastReason = reason ?? string.Empty;
            }

            var output = new TransitionRoutingFrameOutput(
                routeDecision,
                completionOutcome,
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
