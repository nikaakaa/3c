using System.Collections.Generic;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct CharacterNativePoseCaptureFrame
    {
        readonly string m_PoseGraphId;
        readonly CharacterPoseDiagnosticFrame m_Frame;
        readonly AnimationPoseAvailability m_Availability;
        readonly ulong m_CompletionIdentity;
        readonly ulong m_ContinuityIdentity;
        readonly AnimationReadOnlyBuffer<float> m_PoseParameters;
        readonly AnimationReadOnlyBuffer<byte> m_PoseParameterAvailability;
        readonly AnimationReadOnlyBuffer<AnimationPoseSourceContribution> m_Contributions;
        readonly CharacterAnimationInputContract m_Contract;
        readonly CharacterPoseWorldContextAdapter m_World;
        readonly CharacterNativeStateCapturePage m_States;
        internal CharacterNativePoseCaptureFrame(in ComposedAnimationPoseFrame pose,
            in CharacterPoseDiagnosticFrame frame, CharacterAnimationInputContract contract,
            CharacterPoseWorldContextAdapter world, CharacterNativeStateCapturePage states)
        {
            m_PoseGraphId = pose.PoseGraphId;
            m_Frame = frame;
            m_Availability = pose.Availability;
            m_CompletionIdentity = pose.CompletionIdentity;
            m_ContinuityIdentity = pose.ContinuityIdentity;
            m_PoseParameters = pose.PoseParameters;
            m_PoseParameterAvailability = pose.PoseParameterAvailability;
            m_Contributions = pose.Contributions;
            m_Contract = contract;
            m_World = world;
            m_States = states;
        }
        [DiagnosticField, DiagnosticKey("presentation-frame"), DiagnosticGroup("animation-frame")]
        public ulong PresentationFrame => m_Frame.PresentationFrame;
        [DiagnosticField, DiagnosticKey("local-logic-tick"), DiagnosticGroup("animation-frame")]
        public ulong LocalLogicTick => m_Frame.BodyTick;
        [DiagnosticField, DiagnosticKey("reset-sequence"), DiagnosticGroup("animation-frame")]
        public ulong ResetSequence => m_Frame.ResetGeneration;
        [DiagnosticField, DiagnosticKey("completion-identity"), DiagnosticGroup("animation-frame")]
        public ulong CompletionIdentity => m_Frame.CompletionIdentity;
        [DiagnosticField, DiagnosticKey("has-snapshot"), DiagnosticGroup("animation-frame")]
        public bool HasSnapshot => m_Frame.CompletionIdentity != 0;
        [DiagnosticField, DiagnosticKey("pose-graph-id"), DiagnosticGroup("animation-identity")]
        public string PoseGraphId => m_PoseGraphId;
        [DiagnosticField, DiagnosticKey("pose-graph-revision"), DiagnosticGroup("animation-identity")]
        public string PoseGraphRevision => m_Frame.GraphRevision;
        [DiagnosticField, DiagnosticKey("input-contract-hash"), DiagnosticGroup("animation-identity")]
        public string InputContractHash => m_Frame.InputContractHash;
        [DiagnosticField, DiagnosticKey("instance-id"), DiagnosticGroup("animation-identity")]
        public ulong InstanceId => m_Frame.InstanceId;
        [DiagnosticField, DiagnosticKey("final-availability"), DiagnosticGroup("animation-output")]
        public AnimationPoseAvailability FinalAvailability => m_Availability;
        [DiagnosticField, DiagnosticKey("continuity-identity"), DiagnosticGroup("animation-output")]
        public ulong ContinuityIdentity => m_ContinuityIdentity;
        [DiagnosticTable("state-machines", 1, 64), DiagnosticGroup("animation-state")]
        public CharacterNativeStateCapturePage StateMachines => m_States;
        [DiagnosticTable("parameters", 1, 128), DiagnosticGroup("animation-parameters")]
        public CharacterNativeParameterCapturePage Parameters => new CharacterNativeParameterCapturePage(m_PoseParameters, m_PoseParameterAvailability, m_Contract);
        [DiagnosticTable("sources", 1, 128), DiagnosticGroup("animation-output")]
        public CharacterNativeSourceCapturePage Sources => new CharacterNativeSourceCapturePage(m_Contributions, m_CompletionIdentity, m_World);
    }

    public readonly struct CharacterNativeParameterCapturePage
    {
        readonly AnimationReadOnlyBuffer<float> m_Parameters;
        readonly AnimationReadOnlyBuffer<byte> m_Availability;
        readonly CharacterAnimationInputContract m_Contract;
        internal CharacterNativeParameterCapturePage(AnimationReadOnlyBuffer<float> parameters, AnimationReadOnlyBuffer<byte> availability, CharacterAnimationInputContract contract)
        { m_Parameters = parameters; m_Availability = availability; m_Contract = contract; }
        public int Count => m_Parameters.Count;
        public CharacterNativeParameterCaptureRow this[int index] => new CharacterNativeParameterCaptureRow(
            m_Contract.Parameters[index].ParameterId.Value, m_Parameters[index], m_Availability[index] != 0);
    }

    public readonly struct CharacterNativeParameterCaptureRow
    {
        internal CharacterNativeParameterCaptureRow(string id, float value, bool available)
        { ParameterId = id; Value = value; Available = available; }
        [DiagnosticField, DiagnosticKey("parameter-id")] public string ParameterId { get; }
        [DiagnosticField, DiagnosticKey("parameter-value")] public float Value { get; }
        [DiagnosticField, DiagnosticKey("parameter-available")] public bool Available { get; }
    }

    public readonly struct CharacterNativeSourceCapturePage
    {
        readonly AnimationReadOnlyBuffer<AnimationPoseSourceContribution> m_Contributions;
        readonly ulong m_CompletionIdentity;
        readonly CharacterPoseWorldContextAdapter m_World;
        internal CharacterNativeSourceCapturePage(AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions, ulong completionIdentity, CharacterPoseWorldContextAdapter world)
        { m_Contributions = contributions; m_CompletionIdentity = completionIdentity; m_World = world; }
        public int Count => m_Contributions.Count;
        public CharacterNativeSourceCaptureRow this[int index]
        {
            get
            {
                AnimationPoseSourceContribution source = m_Contributions[index];
                ClipSamplePlan clip;
                if (source.Kind != AnimationPoseContributionKind.Live ||
                    !m_World.TryReadClipSample(in source, out clip))
                    clip = default;
                return new CharacterNativeSourceCaptureRow(in source, in clip);
            }
        }
    }

    public readonly struct CharacterNativeSourceCaptureRow
    {
        readonly AnimationPoseSourceContribution m_Source;
        readonly ClipSamplePlan m_Clip;
        internal CharacterNativeSourceCaptureRow(in AnimationPoseSourceContribution source, in ClipSamplePlan clip)
        { m_Source = source; m_Clip = clip; }
        [DiagnosticField, DiagnosticKey("node-id"), DiagnosticGroup("animation-output")]
        public string NodeId => m_Source.NodeId.Value;
        [DiagnosticField, DiagnosticKey("selection-generation"), DiagnosticGroup("animation-output")]
        public ulong SelectionGeneration => m_Source.SourceId.SelectionGeneration.Value;
        [DiagnosticField, DiagnosticKey("source-kind"), DiagnosticGroup("animation-output")]
        public AnimationPoseSourceKind SourceKind => m_Source.SourceId.SourceKind;
        [DiagnosticField, DiagnosticKey("action-instance-id"), DiagnosticGroup("animation-output")]
        public ulong ActionInstanceId => m_Source.SourceId.SourceActionInstanceId;
        [DiagnosticField, DiagnosticKey("backend"), DiagnosticGroup("animation-output")]
        public CharacterAnimationSamplingBackendKind Backend => m_Clip.Backend;
        [DiagnosticField, DiagnosticKey("clip-instance-id"), DiagnosticGroup("animation-output")]
        public int ClipInstanceId => m_Clip.Clip ? m_Clip.Clip.GetInstanceID() : 0;
        [DiagnosticField, DiagnosticKey("kind"), DiagnosticGroup("animation-output")]
        public AnimationPoseContributionKind Kind => m_Source.Kind;
        [DiagnosticField, DiagnosticKey("continuity"), DiagnosticGroup("animation-output")]
        public ulong Continuity => m_Source.ContributionContinuityIdentity;
        [DiagnosticField, DiagnosticKey("weight"), DiagnosticGroup("animation-output")]
        public float Weight => m_Source.Weight;
        [DiagnosticField, DiagnosticKey("left-foot-weight"), DiagnosticGroup("animation-output")]
        public float LeftFootWeight => m_Source.LeftFootWeight;
        [DiagnosticField, DiagnosticKey("right-foot-weight"), DiagnosticGroup("animation-output")]
        public float RightFootWeight => m_Source.RightFootWeight;
        [DiagnosticField, DiagnosticKey("has-clip-sample"), DiagnosticGroup("animation-output")]
        public bool HasClipSample => m_Clip.IsValid;
        [DiagnosticField, DiagnosticKey("clip-binding-index"), DiagnosticGroup("animation-output")]
        public int ClipBindingIndex => m_Clip.ClipBindingIndex;
        [DiagnosticField, DiagnosticKey("resource-catalog-index"), DiagnosticGroup("animation-output")]
        public int ResourceCatalogIndex => m_Clip.ResourceCatalogIndex;
        [DiagnosticField, DiagnosticKey("group-clip-index"), DiagnosticGroup("animation-output")]
        public int GroupClipIndex => m_Clip.GroupClipIndex;
        [DiagnosticField, DiagnosticKey("clip-time"), DiagnosticGroup("animation-output")]
        public float ClipTime => m_Clip.ClipTime;
        [DiagnosticField, DiagnosticKey("continuous-clip-time"), DiagnosticGroup("animation-output")]
        public double ContinuousClipTime => m_Clip.ContinuousClipTime;
        [DiagnosticField, DiagnosticKey("normalized-time"), DiagnosticGroup("animation-output")]
        public float NormalizedTime => m_Clip.NormalizedTime;
        [DiagnosticField, DiagnosticKey("duration-seconds"), DiagnosticGroup("animation-output")]
        public float DurationSeconds => m_Clip.DurationSeconds;
        [DiagnosticField, DiagnosticKey("loop"), DiagnosticGroup("animation-output")]
        public bool Loop => m_Clip.IsLooping;
    }

    public readonly struct CharacterNativeBodyCaptureFrame
    {
        readonly CharacterBodyPresentationFrame m_Body;
        readonly CharacterPresentationFactFrame m_Facts;
        internal CharacterNativeBodyCaptureFrame(in CharacterBodyPresentationFrame body, in CharacterPresentationFactFrame facts)
        { m_Body = body; m_Facts = facts; }
        [DiagnosticField, DiagnosticKey("simulation-tick"), DiagnosticGroup("presentation-facts")]
        public ulong SimulationTick => m_Body.CurrentTick;
        [DiagnosticField, DiagnosticKey("body-reset"), DiagnosticGroup("presentation-facts")]
        public ulong BodyReset => m_Body.ResetSequence;
        [DiagnosticField, DiagnosticKey("visible-position"), DiagnosticGroup("presentation-facts")]
        public Vector3 VisiblePosition => m_Body.VisiblePosition;
        [DiagnosticField, DiagnosticKey("target-position"), DiagnosticGroup("presentation-facts")]
        public Vector3 TargetPosition => m_Body.TargetPosition;
        [DiagnosticField, DiagnosticKey("visible-velocity"), DiagnosticGroup("presentation-facts")]
        public Vector3 VisibleVelocity => m_Body.VisibleVelocity;
        [DiagnosticField, DiagnosticKey("visible-delta"), DiagnosticGroup("presentation-facts")]
        public Vector3 VisibleDelta => m_Body.VisibleTranslationDelta;
        [DiagnosticField, DiagnosticKey("source-delta"), DiagnosticGroup("presentation-facts")]
        public Vector3 SourceDelta => m_Body.SourceTranslationDelta;
        [DiagnosticField, DiagnosticKey("sample-alpha"), DiagnosticGroup("presentation-facts")]
        public float SampleAlpha => m_Body.SampleAlpha;
        [DiagnosticField, DiagnosticKey("grounded"), DiagnosticGroup("presentation-facts")]
        public bool Grounded => m_Facts.Grounded;
        [DiagnosticField, DiagnosticKey("movement-mode"), DiagnosticGroup("presentation-facts")]
        public string MovementMode => m_Facts.MovementModeId;
        [DiagnosticField, DiagnosticKey("movement-playback-generation"), DiagnosticGroup("presentation-facts")]
        public ulong MovementGeneration => m_Facts.MovementPlaybackClock.Generation;
        [DiagnosticField, DiagnosticKey("movement-playback-continuous-ticks"), DiagnosticGroup("presentation-facts")]
        public int MovementTicks => m_Facts.MovementPlaybackClock.ContinuousTicks;
    }

    public readonly struct CharacterNativeCommandCaptureFrame
    {
        readonly IReadOnlyList<ActionAnimationPlaybackCommand> m_Commands;
        internal CharacterNativeCommandCaptureFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands) { m_Commands = commands; }
        [DiagnosticField, DiagnosticKey("command-count"), DiagnosticGroup("presentation-commands")]
        public int CommandCount => m_Commands.Count;
        [DiagnosticTable("signals", 1, 128), DiagnosticGroup("presentation-commands")]
        public CharacterNativeCommandCapturePage Commands => new CharacterNativeCommandCapturePage(m_Commands);
    }

    public readonly struct CharacterNativeCommandCapturePage
    {
        readonly IReadOnlyList<ActionAnimationPlaybackCommand> m_Commands;
        internal CharacterNativeCommandCapturePage(IReadOnlyList<ActionAnimationPlaybackCommand> commands) { m_Commands = commands; }
        public int Count => m_Commands.Count;
        public CharacterNativeCommandCaptureRow this[int index] => new CharacterNativeCommandCaptureRow(m_Commands[index]);
    }

    public readonly struct CharacterNativeCommandCaptureRow
    {
        readonly ActionAnimationPlaybackCommand m_Command;
        internal CharacterNativeCommandCaptureRow(ActionAnimationPlaybackCommand command) { m_Command = command; }
        [DiagnosticField, DiagnosticKey("kind"), DiagnosticGroup("presentation-commands")]
        public ActionAnimationPlaybackCommandKind Kind => m_Command.Kind;
        [DiagnosticField, DiagnosticKey("tick"), DiagnosticGroup("presentation-commands")]
        public ulong Tick => m_Command.LocalLogicTick;
        [DiagnosticField, DiagnosticKey("action-instance-id"), DiagnosticGroup("presentation-commands")]
        public ulong ActionInstanceId => m_Command.ActionInstanceId;
        [DiagnosticField, DiagnosticKey("channel"), DiagnosticGroup("presentation-commands")]
        public string Channel => m_Command.AnimationChannelId.Value;
        [DiagnosticField, DiagnosticKey("producer-id"), DiagnosticGroup("presentation-commands")]
        public string ProducerId => m_Command.ProgramProducerId;
        [DiagnosticField, DiagnosticKey("generation"), DiagnosticGroup("presentation-commands")]
        public ulong Generation => m_Command.Generation;
        [DiagnosticField, DiagnosticKey("has-raw-sample"), DiagnosticGroup("presentation-commands")]
        public bool HasRawSample => m_Command.HasCommittedRawSample;
        [DiagnosticField, DiagnosticKey("raw-time"), DiagnosticGroup("presentation-commands")]
        public float RawTime => m_Command.CommittedRawSample.VisualTime;
        [DiagnosticField, DiagnosticKey("raw-weight"), DiagnosticGroup("presentation-commands")]
        public float RawWeight => m_Command.CommittedRawSample.ProducerWeight;
        [DiagnosticField, DiagnosticKey("has-projected-sample"), DiagnosticGroup("presentation-commands")]
        public bool HasProjectedSample => m_Command.ProjectedSample.IsValid;
        [DiagnosticField, DiagnosticKey("projected-weight"), DiagnosticGroup("presentation-commands")]
        public float ProjectedWeight => m_Command.ProjectedSample.ProducerWeight;
    }

    public readonly struct CharacterNativeCameraCaptureFrame
    {
        readonly CameraFramePlan m_Plan;
        readonly CameraBasisSnapshot m_Basis;
        internal CharacterNativeCameraCaptureFrame(ulong frame, ulong reset, float deltaSeconds,
            in CameraFramePlan plan, in CameraBasisSnapshot basis, bool targetValid, CameraResetReason resetReason)
        { PresentationFrame = frame; ResetSequence = reset; DeltaSeconds = deltaSeconds; m_Plan = plan; m_Basis = basis; TargetValid = targetValid; ResetReason = (int)resetReason; }
        [DiagnosticField, DiagnosticKey("presentation-frame"), DiagnosticGroup("camera-frame")] public ulong PresentationFrame { get; }
        [DiagnosticField, DiagnosticKey("reset-sequence"), DiagnosticGroup("camera-frame")] public ulong ResetSequence { get; }
        [DiagnosticField, DiagnosticKey("delta-seconds"), DiagnosticGroup("camera-frame")] public float DeltaSeconds { get; }
        [DiagnosticField, DiagnosticKey("target-valid"), DiagnosticGroup("camera-output")] public bool TargetValid { get; }
        [DiagnosticField, DiagnosticKey("reset-reason"), DiagnosticGroup("camera-frame")] public int ResetReason { get; }
        [DiagnosticField, DiagnosticKey("collision-status"), DiagnosticGroup("camera-output")] public int CollisionStatus => (int)m_Plan.Collision.Status;
        [DiagnosticField, DiagnosticKey("collision-correction-distance"), DiagnosticGroup("camera-output")] public float CollisionCorrection => m_Plan.Collision.CorrectionDistance;
        [DiagnosticField, DiagnosticKey("has-camera"), DiagnosticGroup("camera-output")]
        public bool HasCamera => PresentationFrame != 0;
        [DiagnosticField, DiagnosticKey("plan-valid"), DiagnosticGroup("camera-output")]
        public bool PlanValid => m_Plan.Valid;
        [DiagnosticField, DiagnosticKey("final-output-available"), DiagnosticGroup("camera-output")]
        public bool FinalOutputAvailable => m_Plan.Valid;
        [DiagnosticField, DiagnosticKey("basis-valid"), DiagnosticGroup("camera-output")]
        public bool BasisValid => m_Basis.Valid;
        [DiagnosticField, DiagnosticKey("final-position"), DiagnosticGroup("camera-output")]
        public Vector3 FinalPosition => m_Plan.Location;
        [DiagnosticField, DiagnosticKey("final-rotation"), DiagnosticGroup("camera-output")]
        public Quaternion FinalRotation => m_Plan.Rotation;
        [DiagnosticField, DiagnosticKey("field-of-view"), DiagnosticGroup("camera-output")]
        public float FieldOfView => m_Plan.FieldOfView;
        [DiagnosticField, DiagnosticKey("reset-tracking"), DiagnosticGroup("camera-output")]
        public bool ResetTracking => m_Plan.ResetHistory;
        [DiagnosticField, DiagnosticKey("sequence-id"), DiagnosticGroup("camera-output")]
        public string SequenceId => m_Plan.SequenceId;
        [DiagnosticField, DiagnosticKey("shot-id"), DiagnosticGroup("camera-output")]
        public string ShotId => m_Plan.ShotId;
        [DiagnosticField, DiagnosticKey("source-id"), DiagnosticGroup("camera-output")]
        public string SourceId => m_Plan.SourceId;
        [DiagnosticField, DiagnosticKey("action-instance-id"), DiagnosticGroup("camera-output")]
        public ulong ActionInstanceId => m_Plan.SourceActionInstanceId;
        [DiagnosticField, DiagnosticKey("blend-progress"), DiagnosticGroup("camera-output")]
        public float BlendProgress => m_Plan.BlendProgress;
    }
}
