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
        public bool Grounded;
        public bool CurrentGroundingHit;
        public int SurfaceIdentity;
        public string ConstraintState;
        public string TransitionReason;
        public string LockType;
        public string PredictionRejectReason;
        public string GoalApplication;
        public string GoalSourceKind;
        public bool SolverResultAvailable;
        public float PlantConfidence;
        public float SoleHeight;
        public float PlacementWeight;
        public float PlantWeight;
        public float ContactWeight;
        public float GoalPositionWeight;
        public float GoalRotationWeight;
        public float LegExtensionRatio;
        public float AnkleTwistDegrees;
        public int QueryCount;
        public int RejectedQueryCount;
        public Vector3 GroundingComponentPosition;
        public Vector3 GoalComponentPosition;
        public Vector3 SolvedComponentPosition;
        public float PositionResidual;
        public float RotationResidualDegrees;
    }

    public struct RuntimeFootIkTraceSnapshot
    {
        public bool IsAvailable;
        public ulong FrameSequence;
        public ulong GoalCompletionIdentity;
        public ulong SolverCompletionIdentity;
        public string GroundingBackendIdentity;
        public string SolverBackendIdentity;
        public string SolverFailure;
        public bool BodyGrounded;
        public bool RootHit;
        public int RootSurfaceIdentity;
        public float PelvisTargetOffset;
        public float PelvisResolvedOffset;
        public bool RejectLeftGoal;
        public bool RejectRightGoal;
        public string PelvisHeightMode;
        public string MovementCompensationMode;
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
