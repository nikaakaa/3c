using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{
    public enum AnimationTransitionBlendLogic : byte
    {
        StandardBlend = 1,
        Inertialization = 2
    }

    public enum TransitionRoutingCoveragePolicy : byte
    {
        CompleteMatrix = 1,
        DeclaredRules = 2
    }

    public enum TransitionRoutingLifecycle : byte
    {
        Idle = 0,
        AwaitingTarget = 1,
        Prepared = 2,
        AwaitingCaptureCompletion = 3,
        Committed = 4,
        Invalid = 5
    }

    public enum TransitionRouteDecisionKind : byte
    {
        None = 0,
        StandardBlend = 1,
        AwaitingReadiness = 2,
        InertializationRequest = 3,
        Reset = 4,
        Invalid = 5
    }

    public enum TransitionRoutingCompletionOutcome : byte
    {
        None = 0,
        CaptureCommitted = 1,
        ReleaseCompleted = 2
    }

    public enum TransitionRoutingResetReason : byte
    {
        None = 0,
        Explicit = 1,
        Seek = 2,
        OwnerGenerationChanged = 3,
        PlanReplacement = 4
    }

    public enum TransitionRoutingReasonCode : ushort
    {
        None = 0,
        InvalidSchemaVersion = 1,
        MissingDefinitionRevision = 2,
        MissingEndpointCatalog = 3,
        InvalidEndpoint = 4,
        DuplicateEndpoint = 5,
        MissingSourcePoseEndpoint = 6,
        InvalidRule = 7,
        DuplicateRule = 8,
        DuplicatePair = 9,
        UnknownSourceEndpoint = 10,
        UnknownTargetEndpoint = 11,
        MissingPair = 12,
        InvalidBlendLogic = 13,
        InvalidStandardBlendDuration = 14,
        InvalidInertializationDuration = 15,
        InertializationTargetsSourcePose = 16,
        PlanIdentityMismatch = 17,
        InvalidFrameIdentity = 18,
        NonMonotonicFrame = 19,
        InvalidOwnerIdentity = 20,
        OwnerIdentityMismatch = 21,
        InvalidSelectionGeneration = 22,
        MissingCompiledRule = 23,
        TargetNotReady = 24,
        CapturePlanNotReady = 25,
        UnexpectedCaptureCompletion = 26,
        CaptureCompletionIdentityMismatch = 27,
        CaptureFailed = 28,
        UnexpectedReleaseCompletion = 29,
        ReleaseCompletionIdentityMismatch = 30,
        ReleaseFailed = 31,
        ResetApplied = 32,
        ConflictingCompletionFacts = 33,
        InvalidCoveragePolicy = 34
    }

    public enum TransitionRoutingEventKind : byte
    {
        CompiledRuleSelected = 1,
        StandardBlendIssued = 2,
        AwaitingTarget = 3,
        RequestPrepared = 4,
        AwaitingCapture = 5,
        CaptureCommitted = 6,
        ReleaseCompleted = 7,
        Rebased = 8,
        Reset = 9,
        Invalid = 10
    }

    public readonly struct AnimationTransitionRule
    {
        public AnimationTransitionRule(
            TransitionRuleId ruleId,
            TransitionEndpointId sourceEndpoint,
            TransitionEndpointId targetEndpoint,
            AnimationTransitionBlendLogic blendLogic,
            double durationSeconds,
            TransitionBlendCurveId blendCurveId,
            TransitionBlendProfileId blendProfileId)
        {
            RuleId = ruleId;
            SourceEndpoint = sourceEndpoint;
            TargetEndpoint = targetEndpoint;
            BlendLogic = blendLogic;
            DurationSeconds = durationSeconds;
            BlendCurveId = blendCurveId;
            BlendProfileId = blendProfileId;
        }

        public TransitionRuleId RuleId { get; }
        public TransitionEndpointId SourceEndpoint { get; }
        public TransitionEndpointId TargetEndpoint { get; }
        public AnimationTransitionBlendLogic BlendLogic { get; }
        public double DurationSeconds { get; }
        public TransitionBlendCurveId BlendCurveId { get; }
        public TransitionBlendProfileId BlendProfileId { get; }
        public bool IsHardCutOutcome => BlendLogic == AnimationTransitionBlendLogic.StandardBlend && DurationSeconds == 0d;
    }

    public sealed class TransitionRoutingDefinition
    {
        readonly TransitionEndpointId[] m_Endpoints;
        readonly AnimationTransitionRule[] m_Rules;

        public TransitionRoutingDefinition(
            int schemaVersion,
            TransitionDefinitionRevision definitionRevision,
            TransitionRoutingCoveragePolicy coveragePolicy,
            IReadOnlyList<TransitionEndpointId> endpoints,
            IReadOnlyList<AnimationTransitionRule> rules,
            bool supportsSourcePoseInertialization = false)
        {
            SchemaVersion = schemaVersion;
            DefinitionRevision = definitionRevision;
            CoveragePolicy = coveragePolicy;
            m_Endpoints = Copy(endpoints);
            m_Rules = Copy(rules);
            SupportsSourcePoseInertialization = supportsSourcePoseInertialization;
        }

        public int SchemaVersion { get; }
        public TransitionDefinitionRevision DefinitionRevision { get; }
        public TransitionRoutingCoveragePolicy CoveragePolicy { get; }
        public bool SupportsSourcePoseInertialization { get; }
        public IReadOnlyList<TransitionEndpointId> Endpoints => m_Endpoints;
        public IReadOnlyList<AnimationTransitionRule> Rules => m_Rules;

        static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                return Array.Empty<T>();
            var result = new T[source.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = source[i];
            return result;
        }
    }

    public readonly struct TransitionRuleKey : IEquatable<TransitionRuleKey>
    {
        public TransitionRuleKey(TransitionEndpointId source, TransitionEndpointId target)
        {
            Source = source;
            Target = target;
        }

        public TransitionEndpointId Source { get; }
        public TransitionEndpointId Target { get; }
        public bool Equals(TransitionRuleKey other) => Source == other.Source && Target == other.Target;
        public override bool Equals(object obj) => obj is TransitionRuleKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Source, Target);
        public override string ToString() => $"{Source} -> {Target}";
    }

    public sealed class CompiledTransitionRoutingPlan
    {
        readonly TransitionEndpointId[] m_Endpoints;
        readonly AnimationTransitionRule[] m_Rules;
        readonly Dictionary<TransitionRuleKey, AnimationTransitionRule> m_RuleByPair;

        public CompiledTransitionRoutingPlan(
            TransitionRoutingPlanId planId,
            int schemaVersion,
            TransitionDefinitionRevision definitionRevision,
            TransitionRoutingCoveragePolicy coveragePolicy,
            StableHash canonicalHash,
            TransitionEndpointId[] endpoints,
            AnimationTransitionRule[] rules)
        {
            PlanId = planId;
            SchemaVersion = schemaVersion;
            DefinitionRevision = definitionRevision;
            CoveragePolicy = coveragePolicy;
            CanonicalHash = canonicalHash;
            m_Endpoints = (TransitionEndpointId[])endpoints.Clone();
            m_Rules = (AnimationTransitionRule[])rules.Clone();
            m_RuleByPair = new Dictionary<TransitionRuleKey, AnimationTransitionRule>(m_Rules.Length);
            for (int i = 0; i < m_Rules.Length; i++)
            {
                AnimationTransitionRule rule = m_Rules[i];
                m_RuleByPair.Add(new TransitionRuleKey(rule.SourceEndpoint, rule.TargetEndpoint), rule);
            }
        }

        public TransitionRoutingPlanId PlanId { get; }
        public int SchemaVersion { get; }
        public TransitionDefinitionRevision DefinitionRevision { get; }
        public TransitionRoutingCoveragePolicy CoveragePolicy { get; }
        public StableHash CanonicalHash { get; }
        public IReadOnlyList<TransitionEndpointId> Endpoints => m_Endpoints;
        public IReadOnlyList<AnimationTransitionRule> Rules => m_Rules;

        public bool TryGetRule(
            TransitionEndpointId source,
            TransitionEndpointId target,
            out AnimationTransitionRule rule) =>
            m_RuleByPair.TryGetValue(new TransitionRuleKey(source, target), out rule);
    }

    public readonly struct TransitionRoutingDiagnostic
    {
        public TransitionRoutingDiagnostic(
            TransitionRoutingReasonCode code,
            string message,
            TransitionRuleId ruleId = default,
            TransitionEndpointId sourceEndpoint = default,
            TransitionEndpointId targetEndpoint = default)
        {
            Code = code;
            Message = message ?? string.Empty;
            RuleId = ruleId;
            SourceEndpoint = sourceEndpoint;
            TargetEndpoint = targetEndpoint;
        }

        public TransitionRoutingReasonCode Code { get; }
        public string Message { get; }
        public TransitionRuleId RuleId { get; }
        public TransitionEndpointId SourceEndpoint { get; }
        public TransitionEndpointId TargetEndpoint { get; }
        public override string ToString() => $"{Code}: {Message}";
    }

    public sealed class TransitionRoutingCompileResult
    {
        readonly TransitionRoutingDiagnostic[] m_Diagnostics;

        internal TransitionRoutingCompileResult(
            CompiledTransitionRoutingPlan plan,
            TransitionRoutingDiagnostic[] diagnostics)
        {
            Plan = plan;
            m_Diagnostics = diagnostics ?? Array.Empty<TransitionRoutingDiagnostic>();
        }

        public bool Succeeded => Plan != null && m_Diagnostics.Length == 0;
        public CompiledTransitionRoutingPlan Plan { get; }
        public IReadOnlyList<TransitionRoutingDiagnostic> Diagnostics => m_Diagnostics;
    }

    public readonly struct TransitionCompletionFact
    {
        public TransitionCompletionFact(
            bool isPresent,
            TransitionRequestEventId requestEventId,
            TransitionRequestGeneration requestGeneration,
            bool succeeded)
        {
            IsPresent = isPresent;
            RequestEventId = requestEventId;
            RequestGeneration = requestGeneration;
            Succeeded = succeeded;
        }

        public bool IsPresent { get; }
        public TransitionRequestEventId RequestEventId { get; }
        public TransitionRequestGeneration RequestGeneration { get; }
        public bool Succeeded { get; }
        public static TransitionCompletionFact None => default;
    }

    public readonly struct TransitionRoutingFrameInput
    {
        public TransitionRoutingFrameInput(
            TransitionRoutingPlanId planId,
            TransitionFrameId frameId,
            TransitionRouteOwnerId ownerNodeId,
            TransitionEndpointId currentEndpoint,
            TransitionEndpointId requestedEndpoint,
            TransitionSelectionGeneration selectionGeneration,
            bool targetReady,
            bool capturePlanReady,
            TransitionCompletionFact captureCompletion,
            TransitionCompletionFact releaseCompletion,
            TransitionRoutingResetReason resetReason)
        {
            PlanId = planId;
            FrameId = frameId;
            OwnerNodeId = ownerNodeId;
            CurrentEndpoint = currentEndpoint;
            RequestedEndpoint = requestedEndpoint;
            SelectionGeneration = selectionGeneration;
            TargetReady = targetReady;
            CapturePlanReady = capturePlanReady;
            CaptureCompletion = captureCompletion;
            ReleaseCompletion = releaseCompletion;
            ResetReason = resetReason;
        }

        public TransitionRoutingPlanId PlanId { get; }
        public TransitionFrameId FrameId { get; }
        public TransitionRouteOwnerId OwnerNodeId { get; }
        public TransitionEndpointId CurrentEndpoint { get; }
        public TransitionEndpointId RequestedEndpoint { get; }
        public TransitionSelectionGeneration SelectionGeneration { get; }
        public bool TargetReady { get; }
        public bool CapturePlanReady { get; }
        public TransitionCompletionFact CaptureCompletion { get; }
        public TransitionCompletionFact ReleaseCompletion { get; }
        public TransitionRoutingResetReason ResetReason { get; }
    }

    public readonly struct StandardBlendCommand
    {
        public StandardBlendCommand(AnimationTransitionRule rule)
        {
            RuleId = rule.RuleId;
            SourceEndpoint = rule.SourceEndpoint;
            TargetEndpoint = rule.TargetEndpoint;
            DurationSeconds = rule.DurationSeconds;
            BlendCurveId = rule.BlendCurveId;
            BlendProfileId = rule.BlendProfileId;
            IsHardCutOutcome = rule.IsHardCutOutcome;
        }

        public TransitionRuleId RuleId { get; }
        public TransitionEndpointId SourceEndpoint { get; }
        public TransitionEndpointId TargetEndpoint { get; }
        public double DurationSeconds { get; }
        public TransitionBlendCurveId BlendCurveId { get; }
        public TransitionBlendProfileId BlendProfileId { get; }
        public bool IsHardCutOutcome { get; }
    }

    public readonly struct PoseInertializationRequest
    {
        public PoseInertializationRequest(
            TransitionRequestEventId requestEventId,
            TransitionRouteOwnerId routeOwnerNodeId,
            TransitionRuleId ruleId,
            TransitionEndpointId sourceEndpoint,
            TransitionEndpointId targetEndpoint,
            TransitionSelectionGeneration selectionGeneration,
            TransitionRequestGeneration requestGeneration,
            double durationSeconds,
            TransitionBlendProfileId blendProfileId)
        {
            RequestEventId = requestEventId;
            RouteOwnerNodeId = routeOwnerNodeId;
            RuleId = ruleId;
            SourceEndpoint = sourceEndpoint;
            TargetEndpoint = targetEndpoint;
            SelectionGeneration = selectionGeneration;
            RequestGeneration = requestGeneration;
            DurationSeconds = durationSeconds;
            BlendProfileId = blendProfileId;
        }

        public TransitionRequestEventId RequestEventId { get; }
        public TransitionRouteOwnerId RouteOwnerNodeId { get; }
        public TransitionRuleId RuleId { get; }
        public TransitionEndpointId SourceEndpoint { get; }
        public TransitionEndpointId TargetEndpoint { get; }
        public TransitionSelectionGeneration SelectionGeneration { get; }
        public TransitionRequestGeneration RequestGeneration { get; }
        public double DurationSeconds { get; }
        public TransitionBlendProfileId BlendProfileId { get; }
        public bool IsValid =>
            RequestEventId.IsValid &&
            RouteOwnerNodeId.IsValid &&
            RuleId.IsValid &&
            SourceEndpoint.IsValid &&
            TargetEndpoint.IsValid &&
            SelectionGeneration.IsValid &&
            RequestGeneration.IsValid &&
            DurationSeconds > 0d;
    }

    public readonly struct TransitionRoutingFrameOutput
    {
        public TransitionRoutingFrameOutput(
            TransitionRouteDecisionKind routeDecision,
            TransitionRoutingCompletionOutcome completionOutcome,
            TransitionRoutingLifecycle lifecycle,
            TransitionRuleId activeRuleId,
            StandardBlendCommand standardBlendCommand,
            bool hasStandardBlendCommand,
            PoseInertializationRequest inertializationRequest,
            bool hasInertializationRequest,
            bool capturePermission,
            bool releasePermission,
            bool rebaseRequired,
            TransitionRoutingReasonCode reasonCode,
            string reason)
        {
            RouteDecision = routeDecision;
            CompletionOutcome = completionOutcome;
            Lifecycle = lifecycle;
            ActiveRuleId = activeRuleId;
            StandardBlendCommand = standardBlendCommand;
            HasStandardBlendCommand = hasStandardBlendCommand;
            InertializationRequest = inertializationRequest;
            HasInertializationRequest = hasInertializationRequest;
            CapturePermission = capturePermission;
            ReleasePermission = releasePermission;
            RebaseRequired = rebaseRequired;
            ReasonCode = reasonCode;
            Reason = reason ?? string.Empty;
        }

        public TransitionRouteDecisionKind RouteDecision { get; }
        public TransitionRoutingCompletionOutcome CompletionOutcome { get; }
        public TransitionRoutingLifecycle Lifecycle { get; }
        public TransitionRuleId ActiveRuleId { get; }
        public StandardBlendCommand StandardBlendCommand { get; }
        public bool HasStandardBlendCommand { get; }
        public PoseInertializationRequest InertializationRequest { get; }
        public bool HasInertializationRequest { get; }
        public bool CapturePermission { get; }
        public bool ReleasePermission { get; }
        public bool RebaseRequired { get; }
        public TransitionRoutingReasonCode ReasonCode { get; }
        public string Reason { get; }
        public bool IsInvalid => RouteDecision == TransitionRouteDecisionKind.Invalid;
    }

    public readonly struct TransitionRoutingEvent
    {
        public TransitionRoutingEvent(
            TransitionFrameId frameId,
            TransitionRoutingEventKind kind,
            TransitionRoutingLifecycle lifecycle,
            TransitionRuleId ruleId,
            TransitionRequestEventId requestEventId,
            TransitionRequestGeneration requestGeneration,
            TransitionRoutingReasonCode reasonCode,
            string message)
        {
            FrameId = frameId;
            Kind = kind;
            Lifecycle = lifecycle;
            RuleId = ruleId;
            RequestEventId = requestEventId;
            RequestGeneration = requestGeneration;
            ReasonCode = reasonCode;
            Message = message ?? string.Empty;
        }

        public TransitionFrameId FrameId { get; }
        public TransitionRoutingEventKind Kind { get; }
        public TransitionRoutingLifecycle Lifecycle { get; }
        public TransitionRuleId RuleId { get; }
        public TransitionRequestEventId RequestEventId { get; }
        public TransitionRequestGeneration RequestGeneration { get; }
        public TransitionRoutingReasonCode ReasonCode { get; }
        public string Message { get; }
    }

    public readonly struct TransitionRoutingRuntimeSnapshot
    {
        public TransitionRoutingRuntimeSnapshot(
            TransitionRoutingPlanId planId,
            TransitionDefinitionRevision definitionRevision,
            TransitionRouteOwnerId ownerNodeId,
            TransitionFrameId frameId,
            TransitionEndpointId currentEndpoint,
            TransitionEndpointId requestedEndpoint,
            TransitionSelectionGeneration selectionGeneration,
            ulong moduleGeneration,
            TransitionRoutingLifecycle lifecycle,
            TransitionRuleId activeRuleId,
            PoseInertializationRequest activeRequest,
            bool hasActiveRequest,
            bool captureCompleted,
            bool releaseCompleted,
            ulong rebaseCount,
            TransitionRoutingResetReason resetReason,
            TransitionRoutingReasonCode reasonCode,
            string reason)
        {
            PlanId = planId;
            DefinitionRevision = definitionRevision;
            OwnerNodeId = ownerNodeId;
            FrameId = frameId;
            CurrentEndpoint = currentEndpoint;
            RequestedEndpoint = requestedEndpoint;
            SelectionGeneration = selectionGeneration;
            ModuleGeneration = moduleGeneration;
            Lifecycle = lifecycle;
            ActiveRuleId = activeRuleId;
            ActiveRequest = activeRequest;
            HasActiveRequest = hasActiveRequest;
            CaptureCompleted = captureCompleted;
            ReleaseCompleted = releaseCompleted;
            RebaseCount = rebaseCount;
            ResetReason = resetReason;
            ReasonCode = reasonCode;
            Reason = reason ?? string.Empty;
        }

        public TransitionRoutingPlanId PlanId { get; }
        public TransitionDefinitionRevision DefinitionRevision { get; }
        public TransitionRouteOwnerId OwnerNodeId { get; }
        public TransitionFrameId FrameId { get; }
        public TransitionEndpointId CurrentEndpoint { get; }
        public TransitionEndpointId RequestedEndpoint { get; }
        public TransitionSelectionGeneration SelectionGeneration { get; }
        public ulong ModuleGeneration { get; }
        public TransitionRoutingLifecycle Lifecycle { get; }
        public TransitionRuleId ActiveRuleId { get; }
        public PoseInertializationRequest ActiveRequest { get; }
        public bool HasActiveRequest { get; }
        public bool CaptureCompleted { get; }
        public bool ReleaseCompleted { get; }
        public ulong RebaseCount { get; }
        public TransitionRoutingResetReason ResetReason { get; }
        public TransitionRoutingReasonCode ReasonCode { get; }
        public string Reason { get; }
    }
}
