using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterPoseFrameLineage :
        IEquatable<CharacterPoseFrameLineage>
    {
        public CharacterPoseFrameLineage(
            ActorId actorId,
            ulong frameIdentity,
            ulong completionIdentity,
            ulong presentationFrame,
            ulong bodyTick,
            string programId,
            string poseProgramIdentity,
            string projectionRevision,
            string rigId,
            string rigRevision,
            ulong tuningGeneration)
        {
            ActorId = actorId;
            FrameIdentity = frameIdentity;
            CompletionIdentity = completionIdentity;
            PresentationFrame = presentationFrame;
            BodyTick = bodyTick;
            ProgramId = programId ?? string.Empty;
            PoseProgramIdentity = poseProgramIdentity ?? string.Empty;
            ProjectionRevision = projectionRevision ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            TuningGeneration = tuningGeneration;
        }

        public ActorId ActorId { get; }
        public ulong FrameIdentity { get; }
        public ulong CompletionIdentity { get; }
        public ulong PresentationFrame { get; }
        public ulong BodyTick { get; }
        public string ProgramId { get; }
        public string PoseProgramIdentity { get; }
        public string ProjectionRevision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public ulong TuningGeneration { get; }
        internal bool IsOpenValid =>
            ActorId.IsValid &&
            FrameIdentity != 0 &&
            PresentationFrame != 0 &&
            BodyTick != 0 &&
            !string.IsNullOrEmpty(ProgramId) &&
            !string.IsNullOrEmpty(PoseProgramIdentity) &&
            !string.IsNullOrEmpty(ProjectionRevision) &&
            !string.IsNullOrEmpty(RigId) &&
            !string.IsNullOrEmpty(RigRevision) &&
            TuningGeneration != 0;
        public bool IsValid => IsOpenValid && CompletionIdentity != 0;

        internal CharacterPoseFrameLineage WithCompletion(
            ulong completionIdentity) =>
            new CharacterPoseFrameLineage(
                ActorId,
                FrameIdentity,
                completionIdentity,
                PresentationFrame,
                BodyTick,
                ProgramId,
                PoseProgramIdentity,
                ProjectionRevision,
                RigId,
                RigRevision,
                TuningGeneration);

        public bool Equals(CharacterPoseFrameLineage other) =>
            ActorId == other.ActorId &&
            FrameIdentity == other.FrameIdentity &&
            CompletionIdentity == other.CompletionIdentity &&
            PresentationFrame == other.PresentationFrame &&
            BodyTick == other.BodyTick &&
            string.Equals(ProgramId, other.ProgramId, StringComparison.Ordinal) &&
            string.Equals(PoseProgramIdentity, other.PoseProgramIdentity, StringComparison.Ordinal) &&
            string.Equals(ProjectionRevision, other.ProjectionRevision, StringComparison.Ordinal) &&
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            TuningGeneration == other.TuningGeneration;

        public override bool Equals(object obj) =>
            obj is CharacterPoseFrameLineage other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                ActorId,
                FrameIdentity,
                CompletionIdentity,
                PresentationFrame,
                HashCode.Combine(
                    BodyTick,
                    ProgramId,
                    PoseProgramIdentity,
                    ProjectionRevision,
                    RigId,
                    RigRevision,
                    TuningGeneration));

        public static bool operator ==(
            CharacterPoseFrameLineage left,
            CharacterPoseFrameLineage right) => left.Equals(right);

        public static bool operator !=(
            CharacterPoseFrameLineage left,
            CharacterPoseFrameLineage right) => !left.Equals(right);
    }

    internal readonly struct CharacterPoseProgramResult
    {
        internal CharacterPoseProgramResult(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationPoseAvailability outputAvailability,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex)
        {
            Lineage = lineage;
            Outcome = outcome;
            OutputAvailability = outputAvailability;
            OutputInvalidReason = outputInvalidReason;
            GraphInvalidReason = graphInvalidReason;
            InvalidOperationIndex = invalidOperationIndex;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationPoseAvailability OutputAvailability { get; }
        internal AnimationPoseNativeInvalidReason OutputInvalidReason { get; }
        internal AnimationPoseNativeInvalidReason GraphInvalidReason { get; }
        internal int InvalidOperationIndex { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            (Outcome == AnimationPresentationFrameOutcome.Committed
                ? OutputAvailability == AnimationPoseAvailability.Pose &&
                  OutputInvalidReason == AnimationPoseNativeInvalidReason.None &&
                  GraphInvalidReason == AnimationPoseNativeInvalidReason.None &&
                  InvalidOperationIndex == -1
                : Outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                  (OutputAvailability != AnimationPoseAvailability.Pose ||
                   OutputInvalidReason != AnimationPoseNativeInvalidReason.None ||
                   GraphInvalidReason != AnimationPoseNativeInvalidReason.None ||
                   InvalidOperationIndex >= 0));
        internal bool IsCompleted =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterPoseConstraintResult
    {
        internal CharacterPoseConstraintResult(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            int goalCount,
            bool solverProduced,
            in CharacterFullBodyIkResult fullBodyIk)
        {
            Lineage = lineage;
            Outcome = outcome;
            Availability = availability;
            InvalidReason = invalidReason;
            GoalCount = goalCount;
            SolverProduced = solverProduced;
            FullBodyIk = fullBodyIk;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason InvalidReason { get; }
        internal int GoalCount { get; }
        internal bool SolverProduced { get; }
        internal CharacterFullBodyIkResult FullBodyIk { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            (Outcome == AnimationPresentationFrameOutcome.Committed
                ? Availability == AnimationPoseAvailability.Pose &&
                  InvalidReason == AnimationPoseNativeInvalidReason.None &&
                  GoalCount >= 0 &&
                  SolverProduced &&
                  FullBodyIk.Succeeded
                : Outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                  Availability != AnimationPoseAvailability.Pose &&
                  InvalidReason != AnimationPoseNativeInvalidReason.None &&
                  GoalCount >= -1 &&
                  (!SolverProduced || !FullBodyIk.Succeeded));
        internal bool IsCompleted =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterFinalPosePublicationResult
    {
        internal CharacterFinalPosePublicationResult(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationFrameOutcome outcome,
            AnimationFinalPoseWriteOutcome writeOutcome,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex,
            ulong appliedCompletionIdentity)
        {
            Lineage = lineage;
            Outcome = outcome;
            WriteOutcome = writeOutcome;
            Availability = availability;
            OutputInvalidReason = outputInvalidReason;
            GraphInvalidReason = graphInvalidReason;
            InvalidOperationIndex = invalidOperationIndex;
            AppliedCompletionIdentity = appliedCompletionIdentity;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal AnimationPresentationFrameOutcome Outcome { get; }
        internal AnimationFinalPoseWriteOutcome WriteOutcome { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason OutputInvalidReason { get; }
        internal AnimationPoseNativeInvalidReason GraphInvalidReason { get; }
        internal int InvalidOperationIndex { get; }
        internal ulong AppliedCompletionIdentity { get; }
        internal bool IsValid =>
            Lineage.IsValid &&
            (Outcome == AnimationPresentationFrameOutcome.Committed
                ? WriteOutcome == AnimationFinalPoseWriteOutcome.Committed &&
                  Availability == AnimationPoseAvailability.Pose &&
                  OutputInvalidReason == AnimationPoseNativeInvalidReason.None &&
                  GraphInvalidReason == AnimationPoseNativeInvalidReason.None &&
                  InvalidOperationIndex == -1 &&
                  AppliedCompletionIdentity == Lineage.CompletionIdentity
                : Outcome == AnimationPresentationFrameOutcome.TypedInvalid &&
                  WriteOutcome == AnimationFinalPoseWriteOutcome.TypedInvalid &&
                  AppliedCompletionIdentity == 0 &&
                  (Availability != AnimationPoseAvailability.Pose ||
                   OutputInvalidReason != AnimationPoseNativeInvalidReason.None ||
                   GraphInvalidReason != AnimationPoseNativeInvalidReason.None ||
                   InvalidOperationIndex >= 0));
        internal bool IsPublished =>
            IsValid &&
            Outcome == AnimationPresentationFrameOutcome.Committed;
    }

    internal readonly struct CharacterPoseFrameExecutionResult
    {
        internal CharacterPoseFrameExecutionResult(
            in CharacterPoseProgramResult program,
            in CharacterPoseConstraintResult constraint,
            in CharacterFinalPosePublicationResult publication)
        {
            Program = program;
            Constraint = constraint;
            Publication = publication;
        }

        internal CharacterPoseProgramResult Program { get; }
        internal CharacterPoseConstraintResult Constraint { get; }
        internal CharacterFinalPosePublicationResult Publication { get; }
        internal CharacterPoseFrameLineage Lineage => Program.Lineage;
        internal bool IsValid =>
            Program.IsValid &&
            Constraint.IsValid &&
            Publication.IsValid &&
            Program.Lineage == Constraint.Lineage &&
            Program.Lineage == Publication.Lineage;
        internal bool IsPublished =>
            IsValid &&
            Program.IsCompleted &&
            Constraint.IsCompleted &&
            Publication.IsPublished;
    }
}
