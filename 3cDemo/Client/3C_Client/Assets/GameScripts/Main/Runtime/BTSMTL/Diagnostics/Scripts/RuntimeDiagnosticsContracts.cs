using System;
using UnityEngine;

namespace BTSMTL.Diagnostics
{
    public readonly struct RuntimeProgramRevision : IEquatable<RuntimeProgramRevision>
    {
        public RuntimeProgramRevision(string programId, string sourceRevision, string programHash)
        {
            ProgramId = programId ?? string.Empty;
            SourceRevision = sourceRevision ?? string.Empty;
            ProgramHash = programHash ?? string.Empty;
        }

        public string ProgramId { get; }
        public string SourceRevision { get; }
        public string ProgramHash { get; }
        public bool IsValid => !string.IsNullOrEmpty(ProgramId) && !string.IsNullOrEmpty(SourceRevision) && !string.IsNullOrEmpty(ProgramHash);

        public bool Equals(RuntimeProgramRevision other)
        {
            return string.Equals(ProgramId, other.ProgramId, StringComparison.Ordinal) &&
                   string.Equals(SourceRevision, other.SourceRevision, StringComparison.Ordinal) &&
                   string.Equals(ProgramHash, other.ProgramHash, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is RuntimeProgramRevision other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ProgramId?.GetHashCode() ?? 0;
                hash = hash * 31 + (SourceRevision?.GetHashCode() ?? 0);
                hash = hash * 31 + (ProgramHash?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString() => $"{ProgramId}@{SourceRevision}:{ProgramHash}";
    }

    public enum RuntimeSourceTargetKind
    {
        Source,
        Operation,
        Constant,
        StateSlot,
        Reference,
        Producer,
        CatalogEntry,
        BodyMotion
    }

    public readonly struct RuntimeSourceTarget : IEquatable<RuntimeSourceTarget>
    {
        public RuntimeSourceTarget(RuntimeSourceTargetKind kind, int index)
        {
            if (kind == RuntimeSourceTargetKind.Source && index != -1)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (kind != RuntimeSourceTargetKind.Source && index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Kind = kind;
            Index = index;
        }

        public RuntimeSourceTargetKind Kind { get; }
        public int Index { get; }
        public bool IsProgramTarget => Kind != RuntimeSourceTargetKind.Source;
        public static RuntimeSourceTarget Source => new RuntimeSourceTarget(RuntimeSourceTargetKind.Source, -1);
        public bool Equals(RuntimeSourceTarget other) => Kind == other.Kind && Index == other.Index;
        public override bool Equals(object obj) => obj is RuntimeSourceTarget other && Equals(other);
        public override int GetHashCode() => (int)Kind * 397 ^ Index;
        public override string ToString() => IsProgramTarget ? $"{Kind}:{Index}" : "Source";
    }

    public enum RuntimeSourceElementKind
    {
        None,
        Graph,
        Node,
        Edge,
        BlackboardDeclaration,
        Timeline,
        Track,
        Clip,
        TreeClip,
        BodyMotionProfile
    }

    public readonly struct RuntimeSourceElementKey : IEquatable<RuntimeSourceElementKey>
    {
        public RuntimeSourceElementKey(
            RuntimeSourceElementKind kind,
            string graphAuthoringId = "",
            string elementAuthoringId = "",
            string timelineAuthoringId = "",
            string trackAuthoringId = "",
            string clipAuthoringId = "")
        {
            Kind = kind;
            GraphAuthoringId = graphAuthoringId ?? string.Empty;
            ElementAuthoringId = elementAuthoringId ?? string.Empty;
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
        }

        public RuntimeSourceElementKind Kind { get; }
        public string GraphAuthoringId { get; }
        public string ElementAuthoringId { get; }
        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public bool IsValid => Kind != RuntimeSourceElementKind.None &&
                               (!string.IsNullOrEmpty(GraphAuthoringId) ||
                                !string.IsNullOrEmpty(TimelineAuthoringId) ||
                                Kind == RuntimeSourceElementKind.BodyMotionProfile && !string.IsNullOrEmpty(ElementAuthoringId));

        public bool Equals(RuntimeSourceElementKey other)
        {
            return Kind == other.Kind &&
                   string.Equals(GraphAuthoringId, other.GraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(ElementAuthoringId, other.ElementAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(TimelineAuthoringId, other.TimelineAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(TrackAuthoringId, other.TrackAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(ClipAuthoringId, other.ClipAuthoringId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is RuntimeSourceElementKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 31 + (GraphAuthoringId?.GetHashCode() ?? 0);
                hash = hash * 31 + (ElementAuthoringId?.GetHashCode() ?? 0);
                hash = hash * 31 + (TimelineAuthoringId?.GetHashCode() ?? 0);
                hash = hash * 31 + (TrackAuthoringId?.GetHashCode() ?? 0);
                hash = hash * 31 + (ClipAuthoringId?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static RuntimeSourceElementKey Graph(string graphId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.Graph, graphId);
        public static RuntimeSourceElementKey Node(string graphId, string nodeId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.Node, graphId, nodeId);
        public static RuntimeSourceElementKey Edge(string graphId, string edgeId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.Edge, graphId, edgeId);
        public static RuntimeSourceElementKey Declaration(string graphId, string declarationId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.BlackboardDeclaration, graphId, declarationId);
        public static RuntimeSourceElementKey Timeline(string timelineId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.Timeline, timelineAuthoringId: timelineId);
        public static RuntimeSourceElementKey Track(string timelineId, string trackId) => new RuntimeSourceElementKey(RuntimeSourceElementKind.Track, timelineAuthoringId: timelineId, trackAuthoringId: trackId);
        public static RuntimeSourceElementKey Clip(string timelineId, string trackId, string clipId, bool treeClip = false) =>
            new RuntimeSourceElementKey(treeClip ? RuntimeSourceElementKind.TreeClip : RuntimeSourceElementKind.Clip, timelineAuthoringId: timelineId, trackAuthoringId: trackId, clipAuthoringId: clipId);
        public static RuntimeSourceElementKey BodyMotionProfile(string assetPath) =>
            new RuntimeSourceElementKey(RuntimeSourceElementKind.BodyMotionProfile, elementAuthoringId: assetPath);
    }

    public readonly struct RuntimeSourceElementHandle : IEquatable<RuntimeSourceElementHandle>
    {
        public RuntimeSourceElementHandle(int value, RuntimeSourceElementKind kind)
        {
            Value = value;
            Kind = kind;
        }

        public int Value { get; }
        public RuntimeSourceElementKind Kind { get; }
        public bool IsValid => Value > 0 && Kind != RuntimeSourceElementKind.None;
        public static RuntimeSourceElementHandle Invalid => default;
        public bool Equals(RuntimeSourceElementHandle other) => Value == other.Value && Kind == other.Kind;
        public override bool Equals(object obj) => obj is RuntimeSourceElementHandle other && Equals(other);
        public override int GetHashCode() => Value * 397 ^ (int)Kind;
        public override string ToString() => IsValid ? $"{Kind}:{Value}" : "Invalid";
    }

    public enum RuntimeInstanceKind
    {
        None,
        Character,
        Graph,
        RunnableActivation,
        StateActivation,
        TimelinePlayback,
        TreeClip
    }

    public readonly struct RuntimeInstanceKey : IEquatable<RuntimeInstanceKey>
    {
        public RuntimeInstanceKey(
            RuntimeInstanceKind kind,
            Guid characterRuntimeId,
            Guid graphRuntimeId,
            string stateId,
            ulong activationGeneration,
            ulong timelinePlaybackId,
            int treeClipCycle)
        {
            Kind = kind;
            CharacterRuntimeId = characterRuntimeId;
            GraphRuntimeId = graphRuntimeId;
            StateId = stateId ?? string.Empty;
            ActivationGeneration = activationGeneration;
            TimelinePlaybackId = timelinePlaybackId;
            TreeClipCycle = treeClipCycle;
        }

        public RuntimeInstanceKind Kind { get; }
        public Guid CharacterRuntimeId { get; }
        public Guid GraphRuntimeId { get; }
        public string StateId { get; }
        public ulong ActivationGeneration { get; }
        public ulong TimelinePlaybackId { get; }
        public int TreeClipCycle { get; }
        public bool IsValid => Kind != RuntimeInstanceKind.None && CharacterRuntimeId != Guid.Empty;

        public static RuntimeInstanceKey Character(Guid characterId) => new RuntimeInstanceKey(RuntimeInstanceKind.Character, characterId, Guid.Empty, string.Empty, 0, 0, -1);
        public static RuntimeInstanceKey Graph(Guid characterId, Guid graphId) => new RuntimeInstanceKey(RuntimeInstanceKind.Graph, characterId, graphId, string.Empty, 0, 0, -1);
        public static RuntimeInstanceKey Runnable(Guid characterId, Guid graphId, string nodeId, ulong generation) => new RuntimeInstanceKey(RuntimeInstanceKind.RunnableActivation, characterId, graphId, nodeId, generation, 0, -1);
        public static RuntimeInstanceKey State(Guid characterId, Guid graphId, string stateId, ulong generation) => new RuntimeInstanceKey(RuntimeInstanceKind.StateActivation, characterId, graphId, stateId, generation, 0, -1);
        public static RuntimeInstanceKey Timeline(Guid characterId, ulong playbackId) => new RuntimeInstanceKey(RuntimeInstanceKind.TimelinePlayback, characterId, Guid.Empty, string.Empty, 0, playbackId, -1);
        public static RuntimeInstanceKey TreeClip(Guid characterId, Guid graphId, ulong playbackId, int cycle) => new RuntimeInstanceKey(RuntimeInstanceKind.TreeClip, characterId, graphId, string.Empty, 0, playbackId, cycle);

        public bool Equals(RuntimeInstanceKey other)
        {
            return Kind == other.Kind && CharacterRuntimeId.Equals(other.CharacterRuntimeId) && GraphRuntimeId.Equals(other.GraphRuntimeId) &&
                   ActivationGeneration == other.ActivationGeneration && TimelinePlaybackId == other.TimelinePlaybackId && TreeClipCycle == other.TreeClipCycle &&
                   string.Equals(StateId, other.StateId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is RuntimeInstanceKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 31 + CharacterRuntimeId.GetHashCode();
                hash = hash * 31 + GraphRuntimeId.GetHashCode();
                hash = hash * 31 + (StateId?.GetHashCode() ?? 0);
                hash = hash * 31 + ActivationGeneration.GetHashCode();
                hash = hash * 31 + TimelinePlaybackId.GetHashCode();
                hash = hash * 31 + TreeClipCycle;
                return hash;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case RuntimeInstanceKind.Character:
                    return $"Character:{CharacterRuntimeId:N}";
                case RuntimeInstanceKind.Graph:
                    return $"Graph:{GraphRuntimeId:N}";
                case RuntimeInstanceKind.RunnableActivation:
                    return $"Runnable:{GraphRuntimeId:N}/{StateId}/{ActivationGeneration}";
                case RuntimeInstanceKind.StateActivation:
                    return $"State:{GraphRuntimeId:N}/{StateId}/{ActivationGeneration}";
                case RuntimeInstanceKind.TimelinePlayback:
                    return $"Timeline:{TimelinePlaybackId}";
                case RuntimeInstanceKind.TreeClip:
                    return $"TreeClip:{TimelinePlaybackId}/{TreeClipCycle}/{GraphRuntimeId:N}";
                default:
                    return "None";
            }
        }
    }

    public enum RuntimeTraceDomain
    {
        Logic,
        Presentation,
        Lifecycle
    }

    [Flags]
    public enum RuntimeTraceChannel
    {
        None = 0,
        Graph = 1 << 0,
        StateMachine = 1 << 1,
        Timeline = 1 << 2,
        Blackboard = 1 << 3,
        Animation = 1 << 4,
        Motion = 1 << 5,
        GameplayEffect = 1 << 6,
        Network = 1 << 7,
        FootPlacement = 1 << 8,
        Equipment = 1 << 9,
        All = Graph | StateMachine | Timeline | Blackboard | Animation | Motion | GameplayEffect | Network | FootPlacement | Equipment
    }

    public enum RuntimeTraceEventKind
    {
        None,
        TargetAttached,
        TargetDetached,
        GraphCreated,
        GraphDestroyed,
        NodeEntered,
        NodeStatus,
        NodeCompleted,
        NodeStopRequested,
        NodeStopping,
        NodeStopped,
        NodeForceStopped,
        EdgeEvaluated,
        EdgeSelected,
        ConditionGraphEvaluated,
        StateTransitionEvaluated,
        StateTransitionSelected,
        StateScopeEntered,
        StateScopeExited,
        StateExitStarted,
        StateExitWaiting,
        TimelineRequested,
        TimelineStarted,
        TimelineLogicTime,
        TimelineVisualTime,
        TimelineCompleted,
        TimelineCancelled,
        TimelineStopped,
        TrackActive,
        ClipActive,
        TreeClipEntered,
        TreeClipUpdated,
        TreeClipExited,
        TreeClipDestroyed,
        BlackboardWritten,
        BlackboardCleared,
        BlackboardProjected,
        MotionContribution,
        MotionResolved,
        ActionSnapshot,
        ActionActivationRequested,
        ActionLifecycleTransitioned,
        ActionWindowSampled,
        ActionCueSubmitted,
        ActionResultSubmitted,
        GameplayEffectLifecycle,
        GameplayAttributeChanged,
        GameplayTagChanged,
        GameplayCueSubmitted,
        AnimationPlaybackCompleted,
        AnimationPlaybackReleased,
        AnimationSelectionSubmitted,
        AnimationProducerSampled,
        AnimationPlaybackPending,
        AnimationPlaybackSelected,
        AnimationPlaybackRetained,
        AnimationPlaybackRetired,
        AnimationMarkerSync,
        MotionMatchingQuery,
        MotionMatchingTrajectory,
        MotionMatchingPoseHistory,
        MotionMatchingAdmission,
        MotionMatchingCandidateRejected,
        MotionMatchingSearchTraversal,
        MotionMatchingTopK,
        MotionMatchingPlan,
        MotionMatchingSelection,
        MotionMatchingPoseSource,
        MotionMatchingReset,
        PresentationInterpolated,
        CameraSnapshot,
        CameraRequest,
        CameraCue,
        SimulationTick,
        SimulationRestore,
        SimulationEvaluate,
        SimulationWorldBatch,
        SimulationFinalize,
        SimulationStatePublished,
        SimulationCommit,
        SimulationFailure,
        SimulationNetworkModel,
        FootPlacementSnapshot,
        EquipmentSnapshot,
        EquipmentChange,
        EquipmentHost,
        EquipmentVisual,
        MotionMatchingFrame
    }

    public enum DebugValueKind
    {
        None,
        Boolean,
        Int64,
        UInt64,
        Double,
        String,
        Guid,
        Vector2,
        Vector3,
        Quaternion,
        TypeOnly
    }

    public readonly struct DebugValueSnapshot
    {
        DebugValueSnapshot(DebugValueKind kind, bool boolean, long signed, ulong unsigned, double number, string text, Vector4 vector)
        {
            Kind = kind;
            Boolean = boolean;
            Signed = signed;
            Unsigned = unsigned;
            Number = number;
            Text = text ?? string.Empty;
            Vector = vector;
        }

        public DebugValueKind Kind { get; }
        public bool Boolean { get; }
        public long Signed { get; }
        public ulong Unsigned { get; }
        public double Number { get; }
        public string Text { get; }
        public Vector4 Vector { get; }

        public static DebugValueSnapshot Capture(object value)
        {
            if (value == null)
                return default;
            if (value is bool boolean)
                return new DebugValueSnapshot(DebugValueKind.Boolean, boolean, 0, 0, 0, string.Empty, default);
            if (value is sbyte || value is short || value is int || value is long)
                return new DebugValueSnapshot(DebugValueKind.Int64, false, Convert.ToInt64(value), 0, 0, string.Empty, default);
            if (value is byte || value is ushort || value is uint || value is ulong)
                return new DebugValueSnapshot(DebugValueKind.UInt64, false, 0, Convert.ToUInt64(value), 0, string.Empty, default);
            if (value is float || value is double || value is decimal)
                return new DebugValueSnapshot(DebugValueKind.Double, false, 0, 0, Convert.ToDouble(value), string.Empty, default);
            if (value is string text)
                return new DebugValueSnapshot(DebugValueKind.String, false, 0, 0, 0, text, default);
            if (value is Guid guid)
                return new DebugValueSnapshot(DebugValueKind.Guid, false, 0, 0, 0, guid.ToString("D"), default);
            if (value is Vector2 vector2)
                return new DebugValueSnapshot(DebugValueKind.Vector2, false, 0, 0, 0, string.Empty, new Vector4(vector2.x, vector2.y, 0, 0));
            if (value is Vector3 vector3)
                return new DebugValueSnapshot(DebugValueKind.Vector3, false, 0, 0, 0, string.Empty, new Vector4(vector3.x, vector3.y, vector3.z, 0));
            if (value is Quaternion quaternion)
                return new DebugValueSnapshot(DebugValueKind.Quaternion, false, 0, 0, 0, string.Empty, new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
            Type type = value.GetType();
            return new DebugValueSnapshot(DebugValueKind.TypeOnly, false, 0, 0, 0, type.FullName ?? type.Name, default);
        }

        public string DisplayValue()
        {
            switch (Kind)
            {
                case DebugValueKind.Boolean: return Boolean ? "true" : "false";
                case DebugValueKind.Int64: return Signed.ToString();
                case DebugValueKind.UInt64: return Unsigned.ToString();
                case DebugValueKind.Double: return Number.ToString("0.###");
                case DebugValueKind.String:
                case DebugValueKind.Guid:
                case DebugValueKind.TypeOnly: return Text;
                case DebugValueKind.Vector2: return $"({Vector.x:0.###}, {Vector.y:0.###})";
                case DebugValueKind.Vector3: return $"({Vector.x:0.###}, {Vector.y:0.###}, {Vector.z:0.###})";
                case DebugValueKind.Quaternion: return $"({Vector.x:0.###}, {Vector.y:0.###}, {Vector.z:0.###}, {Vector.w:0.###})";
                default: return string.Empty;
            }
        }
    }

    public readonly struct RuntimeTimelinePlaybackProvenance
    {
        public RuntimeTimelinePlaybackProvenance(
            string sourceGraphAuthoringId,
            string sourceNodeAuthoringId,
            Guid sourceGraphRuntimeId,
            ulong sourceActivationGeneration,
            string stateMachineGraphAuthoringId,
            string stateId,
            Guid stateMachineGraphRuntimeId,
            ulong stateActivationGeneration,
            string sourceName)
        {
            SourceGraphAuthoringId = sourceGraphAuthoringId ?? string.Empty;
            SourceNodeAuthoringId = sourceNodeAuthoringId ?? string.Empty;
            SourceGraphRuntimeId = sourceGraphRuntimeId;
            SourceActivationGeneration = sourceActivationGeneration;
            StateMachineGraphAuthoringId = stateMachineGraphAuthoringId ?? string.Empty;
            StateId = stateId ?? string.Empty;
            StateMachineGraphRuntimeId = stateMachineGraphRuntimeId;
            StateActivationGeneration = stateActivationGeneration;
            SourceName = sourceName ?? string.Empty;
        }

        public string SourceGraphAuthoringId { get; }
        public string SourceNodeAuthoringId { get; }
        public Guid SourceGraphRuntimeId { get; }
        public ulong SourceActivationGeneration { get; }
        public string StateMachineGraphAuthoringId { get; }
        public string StateId { get; }
        public Guid StateMachineGraphRuntimeId { get; }
        public ulong StateActivationGeneration { get; }
        public string SourceName { get; }
        public bool IsValid => !string.IsNullOrEmpty(SourceGraphAuthoringId) &&
                               !string.IsNullOrEmpty(SourceNodeAuthoringId) &&
                               SourceGraphRuntimeId != Guid.Empty &&
                               SourceActivationGeneration != 0;
        public bool HasStateActivation => !string.IsNullOrEmpty(StateMachineGraphAuthoringId) &&
                                          !string.IsNullOrEmpty(StateId) &&
                                          StateMachineGraphRuntimeId != Guid.Empty &&
                                          StateActivationGeneration != 0;
    }

    public struct RuntimeFootIkLegTraceSnapshot
    {
        public bool IsAvailable;
        public bool DidCurrentTraceHit;
        public int CurrentSurfaceIdentity;
        public string CurrentQueryShape;
        public string CurrentQueryPurpose;
        public int CurrentQueryFootIndex;
        public Vector3 CurrentQueryOrigin;
        public Vector3 CurrentQueryCapsuleEnd;
        public Vector3 CurrentQueryDirection;
        public float CurrentQueryRadius;
        public float CurrentQueryMaximumDistance;
        public int CurrentQueryLayerMask;
        public float CurrentQueryMinimumGroundNormalDot;
        public Vector3 CurrentHitLocation;
        public Vector3 CurrentImpactPoint;
        public Vector3 CurrentHitNormal;
        public float CurrentHitDistance;
        public string ContactState;
        public string TransitionReason;
        public string ContactDecision;
        public bool ContactSurfaceValid;
        public bool ContactSurfaceDistanceAccepted;
        public bool ContactCaptureSpeedAccepted;
        public bool ContactRetentionSpeedAccepted;
        public bool ContactConfidenceAccepted;
        public float MaximumContactSurfaceDistance;
        public float PlantSpeedThreshold;
        public float UnalignmentSpeedThreshold;
        public float PlantConfidenceEnter;
        public float PlantConfidenceExit;
        public float AnchorDistance;
        public bool AnchorDistanceAccepted;
        public float MaximumAnchorDistance;
        public float AnchorBlendSpeed;
        public bool HasSurfaceAnchor;
        public Vector3 SurfaceLocalAnchor;
        public Quaternion SurfaceLocalRotation;
        public Vector3 AnchorWorldPosition;
        public Quaternion AnchorWorldRotation;
        public bool PredictiveRewritten;
        public string PredictionRejectReason;
        public int FutureSurfaceIdentity;
        public Vector3 FutureSupportPoint;
        public Vector3 FutureSupportNormal;
        public int GroundEnvelopeSegmentCount;
        public string GroundEnvelopeRejectReason;
        public int PredictiveQueryCount;
        public int PredictiveRejectedQueryCount;
        public int PredictiveRawHitCount;
        public int PredictiveRejectNoCandidateCount;
        public int PredictiveRejectHeightDiscontinuityCount;
        public int PredictiveRejectEdgeGapCount;
        public int PredictiveRejectSurfaceDiscontinuityCount;
        public int PredictiveRejectReachExceededCount;
        public int PredictiveRejectSlopeExceededCount;
        public int PredictiveRejectStepExceededCount;
        public int PredictiveRejectInvalidCandidateCount;
        public int PredictiveRejectUnsupportedCenterCount;
        public bool FutureLandingQueryAvailable;
        public string FutureLandingQueryShape;
        public string FutureLandingQueryPurpose;
        public Vector3 FutureLandingQueryOrigin;
        public Vector3 FutureLandingQueryDirection;
        public float FutureLandingQueryRadius;
        public float FutureLandingQueryMaximumDistance;
        public float FutureLandingQueryMinimumGroundNormalDot;
        public bool FootFeatureValid;
        public bool PredictedStepValid;
        public bool PredictedStepHasLandingEvent;
        public bool PredictedStepSourceBound;
        public bool HasAuthoritativeLandingEvent;
        public ulong ExpectedLandingEventIdentity;
        public bool LandingEventIdentityValid;
        public bool CurrentEventIsPreSwing;
        public bool CurrentEventIsSwing;
        public ulong LandingEventIdentity;
        public ulong SourceSampleIdentity;
        public int SourceSampleCycle;
        public int EventOrdinal;
        public ulong ContributionContinuityIdentity;
        public float CurrentEventFootPoseWeight;
        public float PlanPredictionBlend;
        public float PoseSynchronizedPredictionBlend;
        public float LandingConfidence;
        public float AuthoredLandingDelaySeconds;
        public float LandingEventPhase;
        public float LandingLiftOffPhase;
        public Vector3 RootLocalLanding;
        public Vector3 RootLocalRouteSample0;
        public Vector3 RootLocalRouteSample1;
        public Vector3 RootLocalRouteSample2;
        public Vector3 RootLocalRouteSample3;
        public Vector3 RootLocalRouteSample4;
        public Vector3 RootLocalRouteSample5;
        public Vector3 RootLocalRouteSample6;
        public Vector3 RootLocalRouteSample7;
        public Vector3 RootLocalRouteSample8;
        public Vector3 RootLocalRouteSample9;
        public Vector3 RootLocalRouteSample10;
        public Vector3 RootLocalRouteSample11;
        public Vector3 RootLocalRouteSample12;
        public Vector3 RootLocalRouteSample13;
        public Vector3 RootLocalRouteSample14;
        public Vector3 RootLocalRouteSample15;
        public Vector3 RootLocalRouteSample16;
        public Vector3 RootLocalRouteSample17;
        public Vector3 RootLocalRouteSample18;
        public Vector3 RootLocalRouteSample19;
        public Vector3 RootLocalRouteSample20;
        public Vector3 RootLocalRouteSample21;
        public Vector3 RootLocalRouteSample22;
        public Vector3 RootLocalRouteSample23;
        public Vector3 RootLocalRouteSample24;
        public Vector3 AuthoredFootRouteStart;
        public Vector3 AuthoredFootRouteLanding;
        public float PredictionDistance;
        public ulong PredictivePlanSequence;
        public ulong PredictivePlanGeneratedFrame;
        public float PredictivePlanGenerationPhase;
        public bool IncomingPredictedStepValid;
        public bool IncomingLandingEventIdentityValid;
        public ulong IncomingLandingEventIdentity;
        public float IncomingEventPhase;
        public float IncomingLiftOffPhase;
        public string PredictivePlanState;
        public string PredictivePlanTransitionReason;
        public string PredictivePlanEndReason;
        public float PredictiveExecutionProgress;
        public ulong PlanLandingEventIdentity;
        public ulong PlanSourceSampleIdentity;
        public int PlanSourceSampleCycle;
        public int PlanEventOrdinal;
        public ulong PlanContributionContinuityIdentity;
        public float PlanElapsedSeconds;
        public float PlanSecondsToLiftOff;
        public float PlanSwingDuration;
        public bool PlanHasPathGeometry;
        public bool PlanHasExecutablePath;
        public Vector3 FrozenPlanarVelocity;
        public float MotionLinearLandingError;
        public float MotionAngularLandingError;
        public float MotionLandingError;
        public float MotionLandingTolerance;
        public Vector3 CurrentSoleWorldPosition;
        public Vector3 FixedPathStartWorldPosition;
        public Vector3 FixedLandingWorldPosition;
        public Vector3 CurrentPathWorldPosition;
        public Vector3 CurrentPathRootWorldPosition;
        public Vector3 CurrentPathHipWorldPosition;
        public Vector3 PredictedHipWorldPosition;
        public Vector3 FrozenRootStartWorldPosition;
        public Quaternion FrozenRootStartWorldRotation;
        public Vector3 FrozenRootLandingWorldPosition;
        public Quaternion FrozenRootLandingWorldRotation;
        public Vector3 PredictionUp;
        public float MinimumLandingConfidence;
        public float MaximumPredictionReachRatio;
        public float PredictionReachRatio;
        public float CastAbove;
        public float CastBelow;
        public int PredictiveRouteSampleCount;
        public int PredictiveAcceptedHitCount;
        public int PredictiveEdgePlaneCandidateCount;
        public int PredictiveAcceptedEdgePlaneCount;
        public float PathSphereRadius;
        public float SwingCapsuleRadius;
        public float SoleSupportRadius;
        public int CurrentPathSurfaceIdentity;
        public Vector3 CurrentPathSupportPoint;
        public Vector3 CurrentPathSupportNormal;
        public float PreClearanceHeelPathDistance;
        public float PreClearanceToePathDistance;
        public float PostClearanceHeelPathDistance;
        public float PostClearanceToePathDistance;
        public bool PredictiveClearanceEvaluated;
        public float PredictiveResidualPenetration;
        public float AuthoredAnimationClearance;
        public float AnimationClearanceContinuityOffset;
        public float AnimationClearanceContinuityContribution;
        public float ReachClearance;
        public float CompositeAnimationClearance;
        public int PlannedFootRouteWorldSampleCount;
        public Vector3 PlannedFootRouteWorldSample0;
        public Vector3 PlannedFootRouteWorldSample1;
        public Vector3 PlannedFootRouteWorldSample2;
        public Vector3 PlannedFootRouteWorldSample3;
        public Vector3 PlannedFootRouteWorldSample4;
        public Vector3 PlannedFootRouteWorldSample5;
        public Vector3 PlannedFootRouteWorldSample6;
        public int PredictivePathDiagnosticSampleCount;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample0;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample1;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample2;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample3;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample4;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample5;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample6;
        public RuntimeFootIkPathSampleSnapshot PredictivePathSample7;
        public float RequiredLift;
        public float AppliedLift;
        public Vector3 BaselineGoalWorldPosition;
        public Vector3 FinalGoalWorldPosition;
        public string BaselineGoalApplication;
        public string FinalGoalSourceKind;
        public bool SolverResultAvailable;
        public float PlantConfidence;
        public bool PlantContact;
        public float SoleHeight;
        public float PlacementWeight;
        public float AnimationFootSpeed;
        public float SurfaceDistance;
        public int SoleSupportSurfaceIdentity;
        public Vector3 SoleSupportPoint;
        public Vector3 SoleSupportNormal;
        public float SoleClearanceTarget;
        public Vector3 SoleClearanceTargetTranslation;
        public Vector3 SoleAnklePosition;
        public Vector3 SoleHeelPosition;
        public Vector3 SoleToePosition;
        public float SoleHeelPlaneDistance;
        public float SoleToePlaneDistance;
        public float ResidualSolePenetration;
        public Vector3 FinalGoalSoleHeelPosition;
        public Vector3 FinalGoalSoleToePosition;
        public Vector3 SolvedSoleAnklePosition;
        public Vector3 SolvedSoleHeelPosition;
        public Vector3 SolvedSoleToePosition;
        public bool FinalPhysicalEvaluationAvailable;
        public string FinalPhysicalSupportKind;
        public int FinalPhysicalSupportSurfaceIdentity;
        public Vector3 FinalPhysicalSupportPoint;
        public Vector3 FinalPhysicalSupportNormal;
        public float FinalPhysicalHeelPlaneDistance;
        public float FinalPhysicalToePlaneDistance;
        public float FinalPhysicalResidualPenetration;
        public float AnimatedAnkleComponentY;
        public float AnchorBlendWeight;
        public float BaselineGoalPositionWeight;
        public float BaselineGoalRotationWeight;
        public float FinalGoalPositionWeight;
        public float FinalGoalRotationWeight;
        public float TargetOffset;
        public float OffsetTarget;
        public float UnconstrainedOffset;
        public float SoleConstraintOffset;
        public float CurrentOffset;
        public float OffsetSpringVelocity;
        public float PreviousOffsetTarget;
        public bool OffsetSpringInitialized;
        public Vector3 TargetNormal;
        public Vector3 CurrentNormal;
        public Vector3 NormalSpringVelocity;
        public Vector3 PreviousNormalTarget;
        public bool NormalSpringInitialized;
        public float PredictionHorizon;
        public Vector3 CurrentGroundingComponentPosition;
        public Vector3 BaselineGoalComponentPosition;
        public Vector3 FinalGoalComponentPosition;
        public Vector3 SolvedComponentPosition;
        public float PositionResidual;
        public float RotationResidualDegrees;
    }

    public struct RuntimeFootIkPathSampleSnapshot
    {
        public float Fraction;
        public Vector3 Position;
        public Vector3 Normal;
        public int SurfaceIdentity;
        public Vector3 AnimationRootPosition;
        public Vector3 HipPosition;
    }

    public struct RuntimeFootIkTraceSnapshot
    {
        public bool IsAvailable;
        public ulong FrameSequence;
        public ulong ResetSequence;
        public ulong GroundingCompletionIdentity;
        public ulong ModifierCompletionIdentity;
        public ulong SolverCompletionIdentity;
        public bool HasPredictiveModifier;
        public string SolverBackendIdentity;
        public string SolverFailure;
        public bool NodeExecuted;
        public bool BodyGrounded;
        public float PlacementAlpha;
        public float PresentationDeltaSeconds;
        public float PoseRootVerticalDelta;
        public Vector3 PoseRootWorldPosition;
        public Quaternion PoseRootWorldRotation;
        public float PelvisLyraTargetOffset;
        public float PelvisResolvedTargetOffset;
        public float CurrentPelvisOffset;
        public float PelvisSpringVelocity;
        public float PreviousPelvisTarget;
        public bool PelvisSpringInitialized;
        public Vector3 PelvisPreSolveTranslation;
        public float PelvisGoalPositionWeight;
        public string PelvisGoalApplication;
        public string PelvisGoalSourceKind;
        public bool PelvisSupportAvailable;
        public string PelvisSupportSide;
        public bool PelvisSupportSwitched;
        public ulong PelvisSupportPlanSequence;
        public float PelvisCurrentSupportTarget;
        public float PelvisSelectedSupportTarget;
        public bool LeftPelvisHasActionConstraint;
        public string LeftPelvisConstraintMode;
        public string LeftPelvisSupportPhase;
        public string LeftPelvisBodyPivotMode;
        public bool LeftPelvisCandidate;
        public ulong LeftPelvisPlanSequence;
        public float LeftPelvisDisplacement;
        public bool RightPelvisHasActionConstraint;
        public string RightPelvisConstraintMode;
        public string RightPelvisSupportPhase;
        public string RightPelvisBodyPivotMode;
        public bool RightPelvisCandidate;
        public ulong RightPelvisPlanSequence;
        public float RightPelvisDisplacement;
        public string LyraSourceIdentity;
        public string SpringIdentity;
        public string RigId;
        public string RigRevision;
        public string ProfileId;
        public string ProfileRevision;
        public string PosePlanHash;
        public string CalibrationId;
        public string CalibrationRevision;
        public int PhysicsSceneIdentity;
        public int SelfFilterIdentity;
        public int BaselineProducerOperationIndex;
        public int BaselineProducerCallSiteIndex;
        public int BaselineGoalOffset;
        public int BaselineGoalCount;
        public string BaselineRigId;
        public string BaselineRigRevision;
        public RuntimeFootIkLegTraceSnapshot Left;
        public RuntimeFootIkLegTraceSnapshot Right;
    }

    public struct RuntimeTracePayload
    {
        public string Status;
        public string Name;
        public string Detail;
        public string Cause;
        public string AnimationChannelId;
        public string OwnerId;
        public string RelatedElementId;
        public float Time;
        public float SecondaryTime;
        public float NormalizedTime;
        public float Weight;
        public float FinalWeight;
        public int Priority;
        public int Cycle;
        public int TrackIndex;
        public int ClipIndex;
        public bool Flag;
        public DebugValueSnapshot Value;
        public RuntimeTimelinePlaybackProvenance TimelinePlayback;
        public RuntimeFootIkTraceSnapshot FootIk;
    }

    public readonly struct RuntimeTraceEvent
    {
        public RuntimeTraceEvent(
            Guid sessionId,
            RuntimeProgramRevision programRevision,
            RuntimeTraceDomain domain,
            RuntimeTraceChannel channel,
            ulong position,
            ulong sequence,
            RuntimeInstanceKey runtimeInstance,
            RuntimeSourceElementHandle source,
            RuntimeTraceEventKind kind,
            RuntimeTracePayload payload)
        {
            SessionId = sessionId;
            ProgramRevision = programRevision;
            Domain = domain;
            Channel = channel;
            Position = position;
            Sequence = sequence;
            RuntimeInstance = runtimeInstance;
            Source = source;
            Kind = kind;
            Payload = payload;
        }

        public Guid SessionId { get; }
        public RuntimeProgramRevision ProgramRevision { get; }
        public RuntimeTraceDomain Domain { get; }
        public RuntimeTraceChannel Channel { get; }
        public ulong Position { get; }
        public ulong Sequence { get; }
        public RuntimeInstanceKey RuntimeInstance { get; }
        public RuntimeSourceElementHandle Source { get; }
        public RuntimeTraceEventKind Kind { get; }
        public RuntimeTracePayload Payload { get; }
    }

    public interface IRuntimeDiagnosticsContextSource
    {
        RuntimeDiagnosticsContext RuntimeDiagnostics { get; }
    }

    public interface IRuntimeDebugProgram
    {
        RuntimeProgramRevision Revision { get; }
        IDebugSourceMap SourceMap { get; }
    }
}
