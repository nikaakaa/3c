using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootLifecycleInputPage
    {
        CharacterFootLifecycleContext m_Value;

        internal ref readonly CharacterFootLifecycleContext Value => ref m_Value;
        internal void Capture(in CharacterFootLifecycleContext value) => m_Value = value;
        internal void Clear() => m_Value = default;
    }

    public readonly struct CharacterFootLifecycleInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootLifecycleContext Value => ref m_Page.Value;

        internal CharacterFootLifecycleInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float PreviousOutputWeight => Value.PreviousOutputWeight;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousAnimatedSole => Value.PreviousAnimatedSole;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float ContactMotionSpeed => Value.Interpolation.ContactMotionSpeed;

        [DiagnosticTable("pre-state-sole-samples", 1, CharacterFootPlacementRigCalibration.MaximumSoleSamples)]
        public CharacterFootPreviousSoleInputPage PreviousOutputSoleSamples => new(m_Page);

        public CharacterFootLandingInputDiagnostics Landing => new(m_Page);

        public CharacterFootDiscreteInputDiagnostics Discrete => new(m_Page);

        public CharacterFootContactInputDiagnostics Contact => new(m_Page);

        public CharacterFootContactTransitionInputDiagnostics ContactTransition => new(m_Page);

        public CharacterFootInterpolationInputDiagnostics Interpolation => new(m_Page);

    }

    public readonly struct CharacterFootPreviousSoleInputPage
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly FixedList512Bytes<Vector3> Samples => ref m_Page.Value.PreviousOutputSoleSamples;

        internal CharacterFootPreviousSoleInputPage(CharacterFootLifecycleInputPage page) => m_Page = page;

        public int Count => Samples.Length;
        public CharacterFootPreviousSoleInputRow this[int index] => new(index, Samples[index]);
    }

    public readonly struct CharacterFootPreviousSoleInputRow
    {
        internal CharacterFootPreviousSoleInputRow(int sampleIndex, Vector3 position)
        {
            SampleIndex = sampleIndex;
            Position = position;
        }

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SampleIndex { get; }

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 Position { get; }
    }

    public readonly struct CharacterFootLandingInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootLandingContext m_Value => ref m_Page.Value.Landing;

        internal CharacterFootLandingInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        public CharacterFootLandingFactInputDiagnostics LastLanding => new(in m_Value.LastLanding);

        public CharacterFootLandingFactInputDiagnostics NextSwingLanding => new(in m_Value.NextSwingLanding);

        public CharacterFootLandingFactInputDiagnostics PromotedLanding => new(in m_Value.PromotedLanding);

        public CharacterFootLandingFactInputDiagnostics PlantTarget => new(in m_Value.PlantTarget);

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 NextSwingReferencePoint => m_Value.NextSwingReferencePoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float NextSwingPredictionError => m_Value.NextSwingPredictionError;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong TrackedEventIdentity => m_Value.TrackedEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootNextLandingTrackingState NextTrackingState => m_Value.NextTrackingState;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootPlantTargetState PlantTargetState => m_Value.PlantTargetState;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantTargetUpdated => m_Value.PlantTargetUpdated;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantVerificationAttempted => m_Value.PlantVerificationAttempted;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantVerificationUnavailable => m_Value.PlantVerificationUnavailable;

    }

    public readonly struct CharacterFootLandingFactInputDiagnostics
    {
        readonly CharacterFootLandingFact m_Value;

        internal CharacterFootLandingFactInputDiagnostics(in CharacterFootLandingFact value) => m_Value = value;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasValue => m_Value.HasValue;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong LandingEventIdentity => m_Value.LandingEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong TrajectoryGeneration => m_Value.TrajectoryGeneration;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public string FutureBodyTranslationSourceIdentity => m_Value.FutureBodyTranslationSourceIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SurfaceIdentity => m_Value.SurfaceIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 WorldPoint => m_Value.WorldPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 WorldNormal => m_Value.WorldNormal;

    }

    public readonly struct CharacterFootDiscreteInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootDiscreteStateContext m_Value => ref m_Page.Value.Discrete;

        internal CharacterFootDiscreteInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootConstraintState State => m_Value.State;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootLockResponse LockResponse => m_Value.LockResponse;

    }

    public readonly struct CharacterFootContactInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootContactContext m_Value => ref m_Page.Value.Contact;

        internal CharacterFootContactInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasContact => m_Value.HasContact;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong EventIdentity => m_Value.EventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong AcquiredFrameSequence => m_Value.AcquiredFrameSequence;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong AcquiredCompletionIdentity => m_Value.AcquiredCompletionIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong WorldRevision => m_Value.WorldRevision;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SurfaceIdentity => m_Value.SurfaceIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 Anchor => m_Value.Anchor;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 Normal => m_Value.Normal;

    }

    public readonly struct CharacterFootContactTransitionInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootContactTransitionContext m_Value => ref m_Page.Value.ContactTransition;

        internal CharacterFootContactTransitionInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasPreviousRequest => m_Value.HasPreviousRequest;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PreviousRequestedLock => m_Value.PreviousRequestedLock;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PreviousIsInZone => m_Value.PreviousIsInZone;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PreviousIsSliding => m_Value.PreviousIsSliding;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong PreviousEventIdentity => m_Value.PreviousEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public AnimationFootStepObservationLockMode PreviousMode => m_Value.PreviousMode;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float PreviousWeight => m_Value.PreviousWeight;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float SecondsSinceEdge => m_Value.SecondsSinceEdge;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool IsMoving => m_Value.IsMoving;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool IsLocking => m_Value.IsLocking;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool EnterGroundedZone => m_Value.EnterGroundedZone;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool LeaveGroundedZone => m_Value.LeaveGroundedZone;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool IsBreakToGround => m_Value.IsBreakToGround;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool EnterLockZone => m_Value.EnterLockZone;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool LeaveLockZone => m_Value.LeaveLockZone;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootContactEdge CurrentContactEdge => m_Value.CurrentContactEdge;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong CurrentEventIdentity => m_Value.CurrentEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float Time => m_Value.Time;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float RemainTime => m_Value.RemainTime;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong LatestContactEventIdentity => m_Value.LatestContactEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong LatestReleasedContactEventIdentity => m_Value.LatestReleasedContactEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong CompletedLockWeightEventIdentity => m_Value.CompletedLockWeightEventIdentity;

    }

    public readonly struct CharacterFootInterpolationInputDiagnostics
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly CharacterFootInterpolationState m_Value => ref m_Page.Value.Interpolation;

        internal CharacterFootInterpolationInputDiagnostics(CharacterFootLifecycleInputPage page) => m_Page = page;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool OutputWeightRebased => m_Value.OutputWeightRebased;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasOutput => m_Value.HasOutput;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasSwingPath => m_Value.HasSwingPath;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong SwingLandingEventIdentity => m_Value.SwingLandingEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong SwingGroundPathInputIdentity => m_Value.SwingGroundPathInputIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 SwingLandingPoint => m_Value.SwingLandingPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousTargetCorrection => m_Value.PreviousTargetCorrection;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousSwingTargetCorrection => m_Value.PreviousSwingTargetCorrection;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 EffectiveCorrection => m_Value.EffectiveCorrection;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 SwingResidual => m_Value.SwingResidual;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasTargetHeight => m_Value.HasTargetHeight;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong TargetHeightEventIdentity => m_Value.TargetHeightEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float FilteredTargetHeightAlongUp => m_Value.FilteredTargetHeightAlongUp;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool TargetHeightRetargetActive => m_Value.TargetHeightRetargetActive;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 Residual => m_Value.Residual;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float Progress => m_Value.Progress;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float StartResidual => m_Value.StartResidual;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool Completed => m_Value.Completed;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootInterpolationPolicy Policy => m_Value.Policy;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasPlantTarget => m_Value.HasPlantTarget;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong PlantTargetEventIdentity => m_Value.PlantTargetEventIdentity;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootPlantTargetKind PlantTargetKind => m_Value.PlantTargetKind;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootLockResponse PlantLockResponse => m_Value.PlantLockResponse;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantTargetVerified => m_Value.PlantTargetVerified;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantDirectFollow => m_Value.PlantDirectFollow;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PlantDesiredPoint => m_Value.PlantDesiredPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PlantFilteredPoint => m_Value.PlantFilteredPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousPlantSelectedWorldTarget => m_Value.PreviousPlantSelectedWorldTarget;

        public CharacterFootSupportTargetDiagnostics SelectedSupportTarget => new(in m_Value.SelectedSupportTarget);

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasPreviousResponseOutputPoint => m_Value.HasPreviousResponseOutputPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousResponseOutputPoint => m_Value.PreviousResponseOutputPoint;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PendingReleaseResponseRebase => m_Value.PendingReleaseResponseRebase;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PlantWorldResidual => m_Value.PlantWorldResidual;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool PlantWorldResidualTransitionActive => m_Value.PlantWorldResidualTransitionActive;

        public CharacterFootResponseHistoryInputDiagnostics ResponseHistory => new(in m_Value.ResponseHistory);

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasCorrectionResponseLineage => m_Value.HasCorrectionResponseLineage;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong CorrectionResponseWorldRevision => m_Value.CorrectionResponseWorldRevision;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootCorrectionResponseInitializationReason PendingCorrectionResponseInitializationReason => m_Value.PendingCorrectionResponseInitializationReason;

        [DiagnosticTable("pre-state-response-lineage", 1, 128)]
        public CharacterFootResponseLineageInputPage ResponseLineage => new(m_Page);

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SourceLineageByteCount => m_Value.CorrectionResponseSourceLineage.Length;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int ProfileRevisionByteCount => m_Value.CorrectionResponseProfileRevision.Length;

    }

    public readonly struct CharacterFootResponseHistoryInputDiagnostics
    {
        readonly CharacterFootCorrectionResponseHistory m_Value;

        internal CharacterFootResponseHistoryInputDiagnostics(in CharacterFootCorrectionResponseHistory value) => m_Value = value;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasValue => m_Value.HasValue;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float Scalar => m_Value.Scalar;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootCorrectionResponseDomain Domain => m_Value.Domain;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 AppliedDirection => m_Value.AppliedDirection;

    }

    public readonly struct CharacterFootResponseLineageInputPage
    {
        readonly CharacterFootLifecycleInputPage m_Page;
        ref readonly FixedString128Bytes Source => ref m_Page.Value.Interpolation.CorrectionResponseSourceLineage;
        ref readonly FixedString128Bytes Profile => ref m_Page.Value.Interpolation.CorrectionResponseProfileRevision;

        internal CharacterFootResponseLineageInputPage(CharacterFootLifecycleInputPage page) => m_Page = page;

        public int Count => Math.Max(Source.Length, Profile.Length);
        public CharacterFootResponseLineageInputRow this[int index] => new(
            index,
            index < Source.Length ? Source[index] : (byte)0,
            index < Profile.Length ? Profile[index] : (byte)0);
    }

    public readonly struct CharacterFootResponseLineageInputRow
    {
        internal CharacterFootResponseLineageInputRow(int byteIndex, byte source, byte profile)
        {
            ByteIndex = byteIndex;
            SourceUtf8Byte = source;
            ProfileUtf8Byte = profile;
        }

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int ByteIndex { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public byte SourceUtf8Byte { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public byte ProfileUtf8Byte { get; }
    }

    public readonly struct CharacterFootBodyTrajectoryInputDiagnostics
    {
        internal CharacterFootBodyTrajectoryInputDiagnostics(
            CharacterFootPlacementBank bank, CharacterFutureBodyTranslation consumed)
        {
            Available = consumed != null;
            SourceIdentity = Available ? consumed.SourceIdentity : string.Empty;
            Samples = new CharacterFootBodyTrajectoryInputPage(consumed);
            DurationSeconds = Available ? consumed.DurationSeconds : 0f;
            BodyTick = bank.BodyTrajectoryTick;
            ResetSequence = bank.BodyTrajectoryResetSequence;
            TimelineGeneration = bank.BodyTrajectoryGeneration;
            AuthorityTick = bank.BodyTrajectoryAuthorityTick;
            PredictionMotionRevision = bank.BodyTrajectoryPredictionMotionRevision;
            RequestedDuration = bank.BodyTrajectoryRequestedDuration;
            HasAttempt = bank.HasBodyTrajectoryAttempt;
        }

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool Available { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public string SourceIdentity { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SampleCount => Samples.Count;
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float DurationSeconds { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong BodyTick { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong ResetSequence { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong TimelineGeneration { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong AuthorityTick { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public ulong PredictionMotionRevision { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float RequestedDuration { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public bool HasAttempt { get; }
        [DiagnosticTable("future-body-trajectory", 1, CharacterFutureBodyTranslation.MaximumSampleCount)]
        public CharacterFootBodyTrajectoryInputPage Samples { get; }
    }

    public readonly struct CharacterFootBodyTrajectoryInputPage
    {
        readonly CharacterFutureBodyTranslation m_Source;

        internal CharacterFootBodyTrajectoryInputPage(CharacterFutureBodyTranslation source) => m_Source = source;

        public int Count => m_Source != null ? m_Source.SampleCount : 0;
        public CharacterFootBodyTrajectoryInputRow this[int index] => new(index, m_Source.SampleAt(index));
    }

    public readonly struct CharacterFootBodyTrajectoryInputRow
    {
        internal CharacterFootBodyTrajectoryInputRow(
            int sampleIndex, in CharacterFutureBodyTranslationSample sample)
        {
            SampleIndex = sampleIndex;
            ElapsedSeconds = sample.ElapsedSeconds;
            RelativePosition = new Vector3(
                sample.RelativePositionX, sample.RelativePositionY, sample.RelativePositionZ);
            Velocity = new Vector3(sample.VelocityX, sample.VelocityY, sample.VelocityZ);
        }

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public int SampleIndex { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float ElapsedSeconds { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 RelativePosition { get; }
        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 Velocity { get; }
    }
}
