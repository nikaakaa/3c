using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFootLifecycleInputDiagnostics
    {
        readonly CharacterFootLifecycleContext m_Value;

        internal CharacterFootLifecycleInputDiagnostics(in CharacterFootLifecycleContext value) => m_Value = value;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public float PreviousOutputWeight => m_Value.PreviousOutputWeight;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public Vector3 PreviousAnimatedSole => m_Value.PreviousAnimatedSole;

        public CharacterFootLandingInputDiagnostics Landing => new(in m_Value.Landing);

        public CharacterFootDiscreteInputDiagnostics Discrete => new(in m_Value.Discrete);

        public CharacterFootContactInputDiagnostics Contact => new(in m_Value.Contact);

        public CharacterFootContactTransitionInputDiagnostics ContactTransition => new(in m_Value.ContactTransition);

        public CharacterFootInterpolationInputDiagnostics Interpolation => new(in m_Value.Interpolation);

    }

    public readonly struct CharacterFootLandingInputDiagnostics
    {
        readonly CharacterFootLandingContext m_Value;

        internal CharacterFootLandingInputDiagnostics(in CharacterFootLandingContext value) => m_Value = value;

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
        readonly CharacterFootDiscreteStateContext m_Value;

        internal CharacterFootDiscreteInputDiagnostics(in CharacterFootDiscreteStateContext value) => m_Value = value;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootConstraintState State => m_Value.State;

        [DiagnosticField, DiagnosticGroup("replay-input")]
        public CharacterFootLockResponse LockResponse => m_Value.LockResponse;

    }

    public readonly struct CharacterFootContactInputDiagnostics
    {
        readonly CharacterFootContactContext m_Value;

        internal CharacterFootContactInputDiagnostics(in CharacterFootContactContext value) => m_Value = value;

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
        readonly CharacterFootContactTransitionContext m_Value;

        internal CharacterFootContactTransitionInputDiagnostics(in CharacterFootContactTransitionContext value) => m_Value = value;

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
        readonly CharacterFootInterpolationState m_Value;

        internal CharacterFootInterpolationInputDiagnostics(in CharacterFootInterpolationState value) => m_Value = value;

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
        public CharacterFootResponseLineageInputPage ResponseLineage => new(
            in m_Value.CorrectionResponseSourceLineage,
            in m_Value.CorrectionResponseProfileRevision);

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
        readonly FixedString128Bytes m_Source;
        readonly FixedString128Bytes m_Profile;

        internal CharacterFootResponseLineageInputPage(
            in FixedString128Bytes source, in FixedString128Bytes profile)
        {
            m_Source = source;
            m_Profile = profile;
        }

        public int Count => Math.Max(m_Source.Length, m_Profile.Length);
        public CharacterFootResponseLineageInputRow this[int index] => new(
            index,
            index < m_Source.Length ? m_Source[index] : (byte)0,
            index < m_Profile.Length ? m_Profile[index] : (byte)0);
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
        readonly CharacterFutureBodyTranslationSample m_Sample0;
        readonly CharacterFutureBodyTranslationSample m_Sample1;
        readonly CharacterFutureBodyTranslationSample m_Sample2;
        readonly CharacterFutureBodyTranslationSample m_Sample3;
        readonly CharacterFutureBodyTranslationSample m_Sample4;

        internal CharacterFootBodyTrajectoryInputPage(CharacterFutureBodyTranslation source)
        {
            Count = source != null ? source.SampleCount : 0;
            m_Sample0 = Count > 0 ? source.SampleAt(0) : default;
            m_Sample1 = Count > 1 ? source.SampleAt(1) : default;
            m_Sample2 = Count > 2 ? source.SampleAt(2) : default;
            m_Sample3 = Count > 3 ? source.SampleAt(3) : default;
            m_Sample4 = Count > 4 ? source.SampleAt(4) : default;
        }

        public int Count { get; }
        public CharacterFootBodyTrajectoryInputRow this[int index] => new(index, index switch
        {
            0 => m_Sample0,
            1 => m_Sample1,
            2 => m_Sample2,
            3 => m_Sample3,
            4 => m_Sample4,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        });
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
