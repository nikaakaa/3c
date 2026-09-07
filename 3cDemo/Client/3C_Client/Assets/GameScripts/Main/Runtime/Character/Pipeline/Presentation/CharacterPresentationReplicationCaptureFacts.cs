using System;
using System.Collections.Generic;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCamera;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterAnimationPresentationCaptureFrame
    {
        public CharacterAnimationPresentationCaptureFrame(
            ulong presentationFrame,
            ulong localLogicTick,
            ulong resetSequence,
            float presentationDeltaSeconds,
            in AnimationPresentationRuntimeSnapshot snapshot)
        {
            PresentationFrame = presentationFrame;
            LocalLogicTick = localLogicTick;
            ResetSequence = resetSequence;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            SnapshotAvailable = snapshot.CompletionIdentity != 0;
            ProjectionRevision = snapshot.ProjectionRevision;
            PoseGraphId = snapshot.PoseGraphId;
            PoseGraphRevision = snapshot.PoseGraphRevision;
            PosePlanHash = snapshot.PosePlanHash;
            CompletionIdentity = snapshot.CompletionIdentity;
            FinalAvailability = snapshot.FinalAvailability;
            FinalInvalidReason = snapshot.FinalInvalidReason;
            InvalidOperationIndex = snapshot.InvalidOperationIndex;
            PoseGraphCompletedAt = snapshot.PoseGraphCompletedAt;
            FinalAppliedAt = snapshot.FinalAppliedAt;
            ContinuityIdentity = snapshot.ContinuityIdentity;
            Parameters = new CharacterAnimationParameterCapturePage(
                SnapshotAvailable ? snapshot.Parameters : default,
                SnapshotAvailable);
            StateMachines = new CharacterAnimationStateMachineCapturePage(
                SnapshotAvailable ? snapshot.PoseStateMachines : default,
                SnapshotAvailable);
            Inertializations = new CharacterAnimationInertializationCapturePage(
                SnapshotAvailable ? snapshot.Inertializations : default,
                SnapshotAvailable);
            RuleEvaluations = new CharacterAnimationRuleEvaluationCapturePage(
                SnapshotAvailable
                    ? snapshot.StateMachineRuleEvaluations
                    : default);
        }

        [DiagnosticField]
        [DiagnosticKey("presentation-frame")]
        [DiagnosticGroup("animation-frame")]
        public ulong PresentationFrame { get; }

        [DiagnosticField]
        [DiagnosticKey("local-logic-tick")]
        [DiagnosticGroup("animation-frame")]
        public ulong LocalLogicTick { get; }

        [DiagnosticField]
        [DiagnosticKey("reset-sequence")]
        [DiagnosticGroup("animation-frame")]
        public ulong ResetSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("presentation-delta-seconds")]
        [DiagnosticGroup("animation-frame")]
        public float PresentationDeltaSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("has-snapshot")]
        [DiagnosticGroup("animation-frame")]
        public bool SnapshotAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("projection-revision")]
        [DiagnosticGroup("animation-identity")]
        public string ProjectionRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("pose-graph-id")]
        [DiagnosticGroup("animation-identity")]
        public string PoseGraphId { get; }

        [DiagnosticField]
        [DiagnosticKey("pose-graph-revision")]
        [DiagnosticGroup("animation-identity")]
        public string PoseGraphRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("pose-plan-hash")]
        [DiagnosticGroup("animation-identity")]
        public string PosePlanHash { get; }

        [DiagnosticField]
        [DiagnosticKey("completion-identity")]
        [DiagnosticGroup("animation-frame")]
        public ulong CompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("final-availability")]
        [DiagnosticGroup("animation-output")]
        public AnimationPoseAvailability FinalAvailability { get; }

        [DiagnosticField]
        [DiagnosticKey("final-invalid-reason")]
        [DiagnosticGroup("animation-output")]
        public AnimationPoseNativeInvalidReason FinalInvalidReason { get; }

        [DiagnosticField]
        [DiagnosticKey("invalid-operation-index")]
        [DiagnosticGroup("animation-output")]
        public int InvalidOperationIndex { get; }

        [DiagnosticField]
        [DiagnosticKey("pose-graph-completed-at")]
        [DiagnosticGroup("animation-output")]
        public ulong PoseGraphCompletedAt { get; }

        [DiagnosticField]
        [DiagnosticKey("final-applied-at")]
        [DiagnosticGroup("animation-output")]
        public ulong FinalAppliedAt { get; }

        [DiagnosticField]
        [DiagnosticKey("continuity-identity")]
        [DiagnosticGroup("animation-output")]
        public ulong ContinuityIdentity { get; }

        [DiagnosticTable("parameters", 1, 128)]
        [DiagnosticGroup("animation-parameters")]
        public CharacterAnimationParameterCapturePage Parameters { get; }

        [DiagnosticTable("state-machines", 1, 32)]
        [DiagnosticGroup("animation-state")]
        public CharacterAnimationStateMachineCapturePage StateMachines { get; }

        [DiagnosticTable("inertializations", 1, 64)]
        [DiagnosticGroup("animation-inertialization")]
        public CharacterAnimationInertializationCapturePage Inertializations { get; }

        [DiagnosticTable("transition-rule-evaluations", 1, 256)]
        [DiagnosticGroup("animation-transition-rules")]
        public CharacterAnimationRuleEvaluationCapturePage RuleEvaluations { get; }
    }

    public readonly struct CharacterAnimationRuleEvaluationCapturePage
    {
        readonly AnimationReadOnlyBuffer<PoseTransitionRuleEvaluationSnapshot>
            m_Source;

        public CharacterAnimationRuleEvaluationCapturePage(
            AnimationReadOnlyBuffer<PoseTransitionRuleEvaluationSnapshot>
                source)
        {
            m_Source = source;
        }

        public int Count => m_Source.Count;

        public CharacterAnimationRuleEvaluationCaptureRow this[int index] =>
            new CharacterAnimationRuleEvaluationCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterAnimationRuleEvaluationCaptureRow
    {
        internal CharacterAnimationRuleEvaluationCaptureRow(
            PoseTransitionRuleEvaluationSnapshot source)
        {
            StateMachineId = source.StateMachineId.Value;
            TransitionId = source.TransitionId.Value;
            Prospective = source.Prospective;
            RuleResult = source.RuleResult;
            OperationIndex = source.OperationIndex;
            OperationCode = source.OperationCode;
            ValueKind = source.ValueKind;
            BoolValue = source.BoolValue;
            FloatValue = source.FloatValue;
            EnumValue = source.EnumValue;
            IdentityValue = source.IdentityValue;
        }

        [DiagnosticField]
        [DiagnosticKey("state-machine-id")]
        public string StateMachineId { get; }

        [DiagnosticField]
        [DiagnosticKey("rule-transition-id")]
        public string TransitionId { get; }

        [DiagnosticField]
        [DiagnosticKey("prospective")]
        public bool Prospective { get; }

        [DiagnosticField]
        [DiagnosticKey("rule-result")]
        public bool RuleResult { get; }

        [DiagnosticField]
        [DiagnosticKey("operation-index")]
        public int OperationIndex { get; }

        [DiagnosticField]
        [DiagnosticKey("operation-code")]
        public PoseTransitionRuleOperationCode OperationCode { get; }

        [DiagnosticField]
        [DiagnosticKey("value-kind")]
        public PoseTransitionRuleValueKind ValueKind { get; }

        [DiagnosticField]
        [DiagnosticKey("bool-value")]
        public bool BoolValue { get; }

        [DiagnosticField]
        [DiagnosticKey("float-value")]
        public float FloatValue { get; }

        [DiagnosticField]
        [DiagnosticKey("enum-value")]
        public int EnumValue { get; }

        [DiagnosticField]
        [DiagnosticKey("identity-value")]
        public string IdentityValue { get; }
    }

    public readonly struct CharacterAnimationParameterCapturePage
    {
        readonly AnimationReadOnlyBuffer<AnimationPoseParameterSnapshot> m_Source;
        readonly bool m_Available;

        public CharacterAnimationParameterCapturePage(
            AnimationReadOnlyBuffer<AnimationPoseParameterSnapshot> source,
            bool available)
        {
            m_Source = source;
            m_Available = available;
        }

        public int Count => m_Available ? m_Source.Count : 0;

        public CharacterAnimationParameterCaptureRow this[int index] =>
            new CharacterAnimationParameterCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterAnimationParameterCaptureRow
    {
        internal CharacterAnimationParameterCaptureRow(
            AnimationPoseParameterSnapshot source)
        {
            ParameterId = source.ParameterId.Value;
            Value = source.Value;
            Available = source.Available;
        }

        [DiagnosticField]
        [DiagnosticKey("parameter-id")]
        public string ParameterId { get; }

        [DiagnosticField]
        [DiagnosticKey("parameter-value")]
        public float Value { get; }

        [DiagnosticField]
        [DiagnosticKey("parameter-available")]
        public bool Available { get; }
    }

    public readonly struct CharacterAnimationStateMachineCapturePage
    {
        readonly AnimationReadOnlyBuffer<PoseStateMachineRuntimeSnapshot> m_Source;
        readonly bool m_Available;

        public CharacterAnimationStateMachineCapturePage(
            AnimationReadOnlyBuffer<PoseStateMachineRuntimeSnapshot> source,
            bool available)
        {
            m_Source = source;
            m_Available = available;
        }

        public int Count => m_Available ? m_Source.Count : 0;

        public CharacterAnimationStateMachineCaptureRow this[int index] =>
            new CharacterAnimationStateMachineCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterAnimationStateMachineCaptureRow
    {
        internal CharacterAnimationStateMachineCaptureRow(
            PoseStateMachineRuntimeSnapshot source)
        {
            StateMachineId = source.StateMachineId.Value;
            NodeId = source.NodeId.Value;
            ActiveStateId = source.ActiveStateId.Value;
            TargetStateId = source.TargetStateId.Value;
            ActiveTransitionId = source.ActiveTransitionId.Value;
            EvaluatedTransitionId = source.EvaluatedTransitionId.Value;
            HasTransitionRuleResult = source.HasTransitionRuleResult;
            TransitionRuleResult = source.TransitionRuleResult;
            TimeInState = source.TimeInState;
            TransitionProgress = source.TransitionProgress;
            BlendLogic = source.BlendLogic;
            BlendMode = source.BlendMode;
            BlendDurationSeconds = source.BlendDurationSeconds;
            BlendElapsedSeconds = source.BlendElapsedSeconds;
            CurveIndex = source.CurveIndex;
            BlendProfileIndex = source.BlendProfileIndex;
        }

        [DiagnosticField]
        [DiagnosticKey("state-machine-id")]
        public string StateMachineId { get; }

        [DiagnosticField]
        [DiagnosticKey("node-id")]
        public string NodeId { get; }

        [DiagnosticField]
        [DiagnosticKey("active-state-id")]
        public string ActiveStateId { get; }

        [DiagnosticField]
        [DiagnosticKey("target-state-id")]
        public string TargetStateId { get; }

        [DiagnosticField]
        [DiagnosticKey("active-transition-id")]
        public string ActiveTransitionId { get; }

        [DiagnosticField]
        [DiagnosticKey("evaluated-transition-id")]
        public string EvaluatedTransitionId { get; }

        [DiagnosticField]
        [DiagnosticKey("has-transition-rule-result")]
        public bool HasTransitionRuleResult { get; }

        [DiagnosticField]
        [DiagnosticKey("transition-rule-result")]
        public bool TransitionRuleResult { get; }

        [DiagnosticField]
        [DiagnosticKey("time-in-state")]
        public float TimeInState { get; }

        [DiagnosticField]
        [DiagnosticKey("transition-progress")]
        public float TransitionProgress { get; }

        [DiagnosticField]
        [DiagnosticKey("blend-logic")]
        public AnimationTransitionBlendLogic BlendLogic { get; }

        [DiagnosticField]
        [DiagnosticKey("blend-mode")]
        public CharacterAnimationBlendMode BlendMode { get; }

        [DiagnosticField]
        [DiagnosticKey("blend-duration-seconds")]
        public float BlendDurationSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("blend-elapsed-seconds")]
        public float BlendElapsedSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("curve-index")]
        public int CurveIndex { get; }

        [DiagnosticField]
        [DiagnosticKey("blend-profile-index")]
        public int BlendProfileIndex { get; }
    }

    public readonly struct CharacterAnimationInertializationCapturePage
    {
        readonly AnimationReadOnlyBuffer<PoseInertializationSnapshot> m_Source;
        readonly bool m_Available;

        public CharacterAnimationInertializationCapturePage(
            AnimationReadOnlyBuffer<PoseInertializationSnapshot> source,
            bool available)
        {
            m_Source = source;
            m_Available = available;
        }

        public int Count => m_Available ? m_Source.Count : 0;

        public CharacterAnimationInertializationCaptureRow this[int index] =>
            new CharacterAnimationInertializationCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterAnimationInertializationCaptureRow
    {
        internal CharacterAnimationInertializationCaptureRow(
            PoseInertializationSnapshot source)
        {
            NodeId = source.NodeId.Value;
            State = source.State;
            EventIdentity = source.EventIdentity;
            Reason = source.Reason;
            ResetReason = source.ResetReason;
            ResetSequence = source.ResetSequence;
            PolicyId = source.PolicyId;
            PolicyRevision = source.PolicyRevision;
            RuleMode = source.RuleMode;
            ElapsedSeconds = source.ElapsedSeconds;
            DurationSeconds = source.DurationSeconds;
            AccumulatorGeneration = source.AccumulatorGeneration;
            HistoryCompletionIdentity = source.HistoryCompletionIdentity;
            OutputCompletionIdentity = source.OutputCompletionIdentity;
        }

        [DiagnosticField]
        [DiagnosticKey("node-id")]
        public string NodeId { get; }

        [DiagnosticField]
        [DiagnosticKey("state")]
        public PoseInertializationRuntimeState State { get; }

        [DiagnosticField]
        [DiagnosticKey("event-identity")]
        public ulong EventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("reason")]
        public PoseDiscontinuityReason Reason { get; }

        [DiagnosticField]
        [DiagnosticKey("reset-reason")]
        public PoseDiscontinuityResetReason ResetReason { get; }

        [DiagnosticField]
        [DiagnosticKey("reset-sequence")]
        public ulong ResetSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("policy-id")]
        public string PolicyId { get; }

        [DiagnosticField]
        [DiagnosticKey("policy-revision")]
        public string PolicyRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("rule-mode")]
        public PoseInertializationMode RuleMode { get; }

        [DiagnosticField]
        [DiagnosticKey("elapsed-seconds")]
        public float ElapsedSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("duration-seconds")]
        public float DurationSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("accumulator-generation")]
        public ulong AccumulatorGeneration { get; }

        [DiagnosticField]
        [DiagnosticKey("history-completion-identity")]
        public ulong HistoryCompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("output-completion-identity")]
        public ulong OutputCompletionIdentity { get; }
    }

    public readonly struct CharacterCameraPresentationCaptureFrame
    {
        readonly CameraStateRequest m_State;
        readonly CameraResponsePolicy m_Response;
        readonly CameraResolvedTargetPlan m_Target;
        readonly CameraPosePlan m_Plan;
        readonly CameraBasisSnapshot m_Basis;
        readonly IReadOnlyList<CameraCue> m_Cues;

        public CharacterCameraPresentationCaptureFrame(
            bool hasCamera,
            ulong presentationFrame,
            ulong localLogicTick,
            ulong resetSequence,
            float deltaSeconds,
            bool resetTracking,
            in CameraStateRequest state,
            in CameraResponsePolicy response,
            in CameraResolvedTargetPlan target,
            in CameraPosePlan plan,
            in CameraBasisSnapshot basis,
            bool finalOutputAvailable,
            Vector3 finalPosition,
            Quaternion finalRotation,
            float finalFieldOfView,
            IReadOnlyList<CameraCue> cues)
        {
            m_State = state;
            m_Response = response;
            m_Target = target;
            m_Plan = plan;
            m_Basis = basis;
            m_Cues = cues;
            HasCamera = hasCamera;
            PresentationFrame = presentationFrame;
            LocalLogicTick = localLogicTick;
            ResetSequence = resetSequence;
            DeltaSeconds = deltaSeconds;
            ResetTracking = resetTracking;
            FinalOutputAvailable = finalOutputAvailable;
            FinalPosition = finalPosition;
            FinalRotation = finalRotation;
            FinalFieldOfView = finalFieldOfView;
            Cues = new CharacterCameraCueCapturePage(cues);
        }

        public static CharacterCameraPresentationCaptureFrame Empty =>
            new CharacterCameraPresentationCaptureFrame(
                false,
                0,
                0,
                0,
                0f,
                false,
                default,
                default,
                default,
                default,
                default,
                false,
                default,
                default,
                0f,
                null);

        public CharacterCameraPresentationCaptureFrame WithPresentationContext(
            ulong presentationFrame,
            ulong localLogicTick,
            ulong resetSequence,
            float deltaSeconds) =>
            new CharacterCameraPresentationCaptureFrame(
                HasCamera,
                presentationFrame,
                localLogicTick,
                resetSequence,
                deltaSeconds,
                ResetTracking,
                in m_State,
                in m_Response,
                in m_Target,
                in m_Plan,
                in m_Basis,
                FinalOutputAvailable,
                FinalPosition,
                FinalRotation,
                FinalFieldOfView,
                m_Cues);

        [DiagnosticField]
        [DiagnosticKey("has-camera")]
        [DiagnosticGroup("camera-frame")]
        public bool HasCamera { get; }

        [DiagnosticField]
        [DiagnosticKey("presentation-frame")]
        [DiagnosticGroup("camera-frame")]
        public ulong PresentationFrame { get; }

        [DiagnosticField]
        [DiagnosticKey("local-logic-tick")]
        [DiagnosticGroup("camera-frame")]
        public ulong LocalLogicTick { get; }

        [DiagnosticField]
        [DiagnosticKey("reset-sequence")]
        [DiagnosticGroup("camera-frame")]
        public ulong ResetSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("delta-seconds")]
        [DiagnosticGroup("camera-frame")]
        public float DeltaSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("reset-tracking")]
        [DiagnosticGroup("camera-frame")]
        public bool ResetTracking { get; }

        [DiagnosticField]
        [DiagnosticKey("plan-valid")]
        [DiagnosticGroup("camera-plan")]
        public bool PlanValid => m_Plan.Valid;

        [DiagnosticField]
        [DiagnosticKey("mode")]
        [DiagnosticGroup("camera-plan")]
        public CameraMode Mode => m_Plan.Mode;

        [DiagnosticField]
        [DiagnosticKey("source-id")]
        [DiagnosticGroup("camera-plan")]
        public string SourceId => m_Plan.SourceId;

        [DiagnosticField]
        [DiagnosticKey("source-action-instance-id")]
        [DiagnosticGroup("camera-plan")]
        public ulong SourceActionInstanceId => m_Plan.SourceActionInstanceId;

        [DiagnosticField]
        [DiagnosticKey("blend-progress")]
        [DiagnosticGroup("camera-plan")]
        public float BlendProgress => m_Plan.BlendProgress;

        [DiagnosticField]
        [DiagnosticKey("follow-point")]
        [DiagnosticGroup("camera-plan")]
        public Vector3 FollowPoint => m_Plan.FollowPoint;

        [DiagnosticField]
        [DiagnosticKey("aim-point")]
        [DiagnosticGroup("camera-plan")]
        public Vector3 AimPoint => m_Plan.AimPoint;

        [DiagnosticField]
        [DiagnosticKey("field-of-view")]
        [DiagnosticGroup("camera-plan")]
        public float FieldOfView => m_Plan.FieldOfView;

        [DiagnosticField]
        [DiagnosticKey("look-delta")]
        [DiagnosticGroup("camera-response")]
        public Vector2 LookDelta => m_Plan.LookDelta;

        [DiagnosticField]
        [DiagnosticKey("response-mode")]
        [DiagnosticGroup("camera-response")]
        public CameraLookResponseMode ResponseMode => m_Response.LookResponse;

        [DiagnosticField]
        [DiagnosticKey("manual-orbit-weight")]
        [DiagnosticGroup("camera-response")]
        public float ManualOrbitWeight => m_Response.ManualOrbitWeight;

        [DiagnosticField]
        [DiagnosticKey("pitch-response-weight")]
        [DiagnosticGroup("camera-response")]
        public float PitchResponseWeight => m_Response.PitchResponseWeight;

        [DiagnosticField]
        [DiagnosticKey("yaw-response-weight")]
        [DiagnosticGroup("camera-response")]
        public float YawResponseWeight => m_Response.YawResponseWeight;

        [DiagnosticField]
        [DiagnosticKey("response-priority")]
        [DiagnosticGroup("camera-response")]
        public int ResponsePriority => m_Response.Priority;

        [DiagnosticField]
        [DiagnosticKey("response-weight")]
        [DiagnosticGroup("camera-response")]
        public float ResponseWeight => m_Response.Weight;

        [DiagnosticField]
        [DiagnosticKey("state-priority")]
        [DiagnosticGroup("camera-state")]
        public int StatePriority => m_State.Priority;

        [DiagnosticField]
        [DiagnosticKey("state-weight")]
        [DiagnosticGroup("camera-state")]
        public float StateWeight => m_State.Weight;

        [DiagnosticField]
        [DiagnosticKey("state-blend-in-seconds")]
        [DiagnosticGroup("camera-state")]
        public float StateBlendInSeconds => m_State.BlendInSeconds;

        [DiagnosticField]
        [DiagnosticKey("state-blend-out-seconds")]
        [DiagnosticGroup("camera-state")]
        public float StateBlendOutSeconds => m_State.BlendOutSeconds;

        [DiagnosticField]
        [DiagnosticKey("state-target-key")]
        [DiagnosticGroup("camera-state")]
        public string StateTargetKey => m_State.TargetKey;

        [DiagnosticField]
        [DiagnosticKey("state-source-id")]
        [DiagnosticGroup("camera-state")]
        public string StateSourceId => m_State.SourceId;

        [DiagnosticField]
        [DiagnosticKey("target-valid")]
        [DiagnosticGroup("camera-target")]
        public bool TargetValid => m_Target.Valid;

        [DiagnosticField]
        [DiagnosticKey("target-source-key")]
        [DiagnosticGroup("camera-target")]
        public string TargetSourceKey => m_Target.SourceKey;

        [DiagnosticField]
        [DiagnosticKey("target-follow-point")]
        [DiagnosticGroup("camera-target")]
        public Vector3 TargetFollowPoint => m_Target.FollowPoint;

        [DiagnosticField]
        [DiagnosticKey("target-aim-point")]
        [DiagnosticGroup("camera-target")]
        public Vector3 TargetAimPoint => m_Target.AimPoint;

        [DiagnosticField]
        [DiagnosticKey("final-output-available")]
        [DiagnosticGroup("camera-output")]
        public bool FinalOutputAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("final-position")]
        [DiagnosticGroup("camera-output")]
        public Vector3 FinalPosition { get; }

        [DiagnosticField]
        [DiagnosticKey("final-rotation")]
        [DiagnosticGroup("camera-output")]
        public Quaternion FinalRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("final-field-of-view")]
        [DiagnosticGroup("camera-output")]
        public float FinalFieldOfView { get; }

        [DiagnosticField]
        [DiagnosticKey("basis-valid")]
        [DiagnosticGroup("camera-output")]
        public bool BasisValid => m_Basis.Valid;

        [DiagnosticField]
        [DiagnosticKey("basis-yaw")]
        [DiagnosticGroup("camera-output")]
        public float BasisYaw => m_Basis.Yaw;

        [DiagnosticField]
        [DiagnosticKey("basis-pitch")]
        [DiagnosticGroup("camera-output")]
        public float BasisPitch => m_Basis.Pitch;

        [DiagnosticField]
        [DiagnosticKey("look-direction")]
        [DiagnosticGroup("camera-output")]
        public Vector3 LookDirection => m_Basis.LookDirection;

        [DiagnosticTable("cues", 1, 64)]
        [DiagnosticGroup("camera-cues")]
        public CharacterCameraCueCapturePage Cues { get; }
    }

    public readonly struct CharacterCameraCueCapturePage
    {
        readonly IReadOnlyList<CameraCue> m_Source;

        public CharacterCameraCueCapturePage(IReadOnlyList<CameraCue> source)
        {
            m_Source = source;
        }

        public int Count => m_Source?.Count ?? 0;

        public CharacterCameraCueCaptureRow this[int index] =>
            new CharacterCameraCueCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterCameraCueCaptureRow
    {
        internal CharacterCameraCueCaptureRow(CameraCue source)
        {
            CueId = source.CueId;
            CueKind = source.CueKind;
            CueType = source.CueType;
            Intensity = source.Intensity;
            DurationSeconds = source.DurationSeconds;
            Priority = source.Priority;
            SourceId = source.SourceId;
            SourceActionInstanceId = source.SourceActionInstanceId;
            Active = source.Active;
        }

        [DiagnosticField]
        [DiagnosticKey("cue-id")]
        public string CueId { get; }

        [DiagnosticField]
        [DiagnosticKey("cue-kind")]
        public CameraCueKind CueKind { get; }

        [DiagnosticField]
        [DiagnosticKey("cue-type")]
        public string CueType { get; }

        [DiagnosticField]
        [DiagnosticKey("intensity")]
        public float Intensity { get; }

        [DiagnosticField]
        [DiagnosticKey("duration-seconds")]
        public float DurationSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("priority")]
        public int Priority { get; }

        [DiagnosticField]
        [DiagnosticKey("source-id")]
        public string SourceId { get; }

        [DiagnosticField]
        [DiagnosticKey("source-action-instance-id")]
        public ulong SourceActionInstanceId { get; }

        [DiagnosticField]
        [DiagnosticKey("active")]
        public bool Active { get; }
    }

    public readonly struct CharacterPresentationCommandCaptureFacts
    {
        public CharacterPresentationCommandCaptureFacts(
            IReadOnlyList<CharacterPresentationCommand> commands)
        {
            CommandCount = commands?.Count ?? 0;
            Commands = new CharacterPresentationCommandCapturePage(commands);
        }

        [DiagnosticField]
        [DiagnosticKey("command-count")]
        [DiagnosticGroup("presentation-commands")]
        public int CommandCount { get; }

        [DiagnosticTable("signals", 1, 128)]
        [DiagnosticGroup("presentation-commands")]
        public CharacterPresentationCommandCapturePage Commands { get; }
    }

    public readonly struct CharacterPresentationCommandCapturePage
    {
        readonly IReadOnlyList<CharacterPresentationCommand> m_Source;

        public CharacterPresentationCommandCapturePage(
            IReadOnlyList<CharacterPresentationCommand> source)
        {
            m_Source = source;
        }

        public int Count => m_Source?.Count ?? 0;

        public CharacterPresentationCommandCaptureRow this[int index] =>
            new CharacterPresentationCommandCaptureRow(m_Source[index]);
    }

    public readonly struct CharacterPresentationCommandCaptureRow
    {
        internal CharacterPresentationCommandCaptureRow(
            CharacterPresentationCommand source)
        {
            EventIdentity = source.Header.EventId.ToString();
            Channel = source.Header.Channel;
            Kind = source.Kind;
            ProducerId = source.ProducerId;
            SampleTime = source.SampleTime;
            Weight = source.Weight;
            ProducerGeneration = source.ProducerGeneration;
            Cycle = source.Cycle;
            SourceActionInstanceId = source.SourceActionInstanceId;
            VisualTimeScale = source.VisualTimeScale;
        }

        [DiagnosticField]
        [DiagnosticKey("event-identity")]
        public string EventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("channel")]
        public string Channel { get; }

        [DiagnosticField]
        [DiagnosticKey("kind")]
        public CharacterPresentationCommandKind Kind { get; }

        [DiagnosticField]
        [DiagnosticKey("producer-id")]
        public string ProducerId { get; }

        [DiagnosticField]
        [DiagnosticKey("sample-time")]
        public float SampleTime { get; }

        [DiagnosticField]
        [DiagnosticKey("weight")]
        public float Weight { get; }

        [DiagnosticField]
        [DiagnosticKey("producer-generation")]
        public ulong ProducerGeneration { get; }

        [DiagnosticField]
        [DiagnosticKey("cycle")]
        public int Cycle { get; }

        [DiagnosticField]
        [DiagnosticKey("source-action-instance-id")]
        public ulong SourceActionInstanceId { get; }

        [DiagnosticField]
        [DiagnosticKey("visual-time-scale")]
        public float VisualTimeScale { get; }
    }

    public readonly struct CharacterPresentationFactCaptureFrame
    {
        internal CharacterPresentationFactCaptureFrame(
            in CharacterPresentationFactFrame frame)
        {
            HasFactFrame = frame.IsValid;
            SimulationTick = frame.SimulationTick.Value;
            Grounded = frame.Grounded;
            HorizontalSpeed = frame.HorizontalSpeed;
            HorizontalAcceleration = frame.HorizontalAcceleration;
            VerticalSpeed = frame.VerticalSpeed;
            MovementDirection = frame.MovementDirection;
            LocomotionPlanarBasis = frame.LocomotionPlanarBasis;
            DesiredPlanarVelocity = frame.DesiredPlanarVelocity;
            DesiredDirection = frame.DesiredDirection;
            FacingError = frame.FacingError;
            MotionPhase = frame.MotionPhase;
            MovementMode = frame.MovementModeId ?? string.Empty;
            CommittedMovementPlaybackClock clock = frame.MovementPlaybackClock;
            MovementPlaybackOwner = clock.IsValid ? clock.OwnerIdentity : string.Empty;
            MovementPlaybackGeneration = clock.IsValid ? clock.Generation : 0;
            MovementPlaybackContinuousTicks = clock.IsValid ? clock.ContinuousTicks : 0;
            CommittedLocomotionPlanarMotionTimeline timeline =
                frame.LocomotionMotionTimeline;
            LocomotionTimelineOwner =
                timeline.IsValid ? timeline.OwnerIdentity : string.Empty;
            LocomotionTimelineContinuationOwner =
                timeline.IsValid ? timeline.ContinuationOwnerIdentity : string.Empty;
        }

        public static CharacterPresentationFactCaptureFrame Empty => default;

        [DiagnosticField]
        [DiagnosticKey("has-fact-frame")]
        [DiagnosticGroup("presentation-facts")]
        public bool HasFactFrame { get; }

        [DiagnosticField]
        [DiagnosticKey("simulation-tick")]
        [DiagnosticGroup("presentation-facts")]
        public ulong SimulationTick { get; }

        [DiagnosticField]
        [DiagnosticKey("grounded")]
        [DiagnosticGroup("presentation-facts")]
        public bool Grounded { get; }

        [DiagnosticField]
        [DiagnosticKey("horizontal-speed")]
        [DiagnosticGroup("presentation-facts")]
        public float HorizontalSpeed { get; }

        [DiagnosticField]
        [DiagnosticKey("horizontal-acceleration")]
        [DiagnosticGroup("presentation-facts")]
        public float HorizontalAcceleration { get; }

        [DiagnosticField]
        [DiagnosticKey("vertical-speed")]
        [DiagnosticGroup("presentation-facts")]
        public float VerticalSpeed { get; }

        [DiagnosticField]
        [DiagnosticKey("movement-direction")]
        [DiagnosticGroup("presentation-facts")]
        public Vector2 MovementDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("locomotion-planar-basis")]
        [DiagnosticGroup("presentation-facts")]
        public Vector2 LocomotionPlanarBasis { get; }

        [DiagnosticField]
        [DiagnosticKey("desired-planar-velocity")]
        [DiagnosticGroup("presentation-facts")]
        public Vector2 DesiredPlanarVelocity { get; }

        [DiagnosticField]
        [DiagnosticKey("desired-direction")]
        [DiagnosticGroup("presentation-facts")]
        public Vector2 DesiredDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("facing-error")]
        [DiagnosticGroup("presentation-facts")]
        public float FacingError { get; }

        [DiagnosticField]
        [DiagnosticKey("motion-phase")]
        [DiagnosticGroup("presentation-facts")]
        public CharacterPresentationMotionPhase MotionPhase { get; }

        [DiagnosticField]
        [DiagnosticKey("movement-mode")]
        [DiagnosticGroup("presentation-facts")]
        public string MovementMode { get; }

        [DiagnosticField]
        [DiagnosticKey("movement-playback-owner")]
        [DiagnosticGroup("presentation-facts")]
        public string MovementPlaybackOwner { get; }

        [DiagnosticField]
        [DiagnosticKey("movement-playback-generation")]
        [DiagnosticGroup("presentation-facts")]
        public ulong MovementPlaybackGeneration { get; }

        [DiagnosticField]
        [DiagnosticKey("movement-playback-continuous-ticks")]
        [DiagnosticGroup("presentation-facts")]
        public int MovementPlaybackContinuousTicks { get; }

        [DiagnosticField]
        [DiagnosticKey("locomotion-timeline-owner")]
        [DiagnosticGroup("presentation-facts")]
        public string LocomotionTimelineOwner { get; }

        [DiagnosticField]
        [DiagnosticKey("locomotion-timeline-continuation-owner")]
        [DiagnosticGroup("presentation-facts")]
        public string LocomotionTimelineContinuationOwner { get; }
    }
}
