using System;
using System.Collections.Generic;
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

    internal readonly struct CharacterPoseSourceDemand
    {
        internal CharacterPoseSourceDemand(
            in CharacterPoseFrameLineage lineage,
            IReadOnlyList<PoseSourceProviderDemand> providerDemands,
            int actionSourceCount,
            int providerSourceCount)
        {
            if (!lineage.IsValid ||
                providerDemands == null ||
                actionSourceCount < 0 ||
                providerSourceCount < 0)
            {
                throw new ArgumentException(
                    "Character Pose source demand is invalid.");
            }
            for (int i = 0; i < providerDemands.Count; i++)
            {
                PoseSourceProviderDemand demand = providerDemands[i];
                if (!demand.IsValid ||
                    demand.FrameSequence != lineage.PresentationFrame)
                {
                    throw new ArgumentException(
                        "Character Pose provider demand lineage is invalid.");
                }
            }
            Lineage = lineage;
            ProviderDemands = providerDemands;
            ActionSourceCount = actionSourceCount;
            ProviderSourceCount = providerSourceCount;
            IsValid = true;
        }

        internal CharacterPoseFrameLineage Lineage { get; }
        internal IReadOnlyList<PoseSourceProviderDemand> ProviderDemands
        {
            get;
        }
        internal int ActionSourceCount { get; }
        internal int ProviderSourceCount { get; }
        internal bool IsValid { get; }
    }

    internal enum CharacterPoseSourceFrameOutcome : byte
    {
        AwaitingSample = 1,
        Prepared = 2,
        Invalid = 3
    }

    internal readonly struct CharacterPoseSourceFrameResult
    {
        internal CharacterPoseSourceFrameResult(
            in CharacterPoseSourceDemand demand,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSources,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSources)
        {
            if (!demand.IsValid ||
                actionSources == null ||
                providerSources == null ||
                actionSources.Count != demand.ActionSourceCount ||
                providerSources.Count != demand.ProviderSourceCount)
            {
                throw new ArgumentException(
                    "Character Pose source frame does not match its demand.");
            }
            bool pending = false;
            bool invalid = false;
            foreach (AnimationResolvedPoseSourceSample sample in
                     actionSources.Values)
            {
                if (sample?.IsValid != true)
                    invalid = true;
            }
            foreach (PresentationPoseSourceSample sample in
                     providerSources.Values)
            {
                if (sample?.IsValid != true ||
                    sample.FrameSequence !=
                        demand.Lineage.PresentationFrame ||
                    sample.Availability ==
                        PresentationPoseSourceAvailability.Invalid)
                {
                    invalid = true;
                }
                else if (sample.Availability ==
                         PresentationPoseSourceAvailability.Pending)
                {
                    pending = true;
                }
            }
            Demand = demand;
            ActionSources = actionSources;
            ProviderSources = providerSources;
            Availability = invalid
                ? PresentationPoseSourceAvailability.Invalid
                : pending
                    ? PresentationPoseSourceAvailability.Pending
                    : PresentationPoseSourceAvailability.Ready;
            Outcome = invalid
                ? CharacterPoseSourceFrameOutcome.Invalid
                : pending
                    ? CharacterPoseSourceFrameOutcome.AwaitingSample
                    : CharacterPoseSourceFrameOutcome.Prepared;
            FailureReason = invalid
                ? PresentationPoseSourceFailureReason.SampleInvalid
                : PresentationPoseSourceFailureReason.None;
        }

        internal CharacterPoseSourceDemand Demand { get; }
        internal CharacterPoseFrameLineage Lineage => Demand.Lineage;
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample> ActionSources { get; }
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> ProviderSources { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal CharacterPoseSourceFrameOutcome Outcome { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal bool IsValid =>
            Demand.IsValid &&
            ActionSources != null &&
            ProviderSources != null &&
            ActionSources.Count == Demand.ActionSourceCount &&
            ProviderSources.Count == Demand.ProviderSourceCount &&
            (Availability == PresentationPoseSourceAvailability.Ready
                ? Outcome == CharacterPoseSourceFrameOutcome.Prepared &&
                  FailureReason == PresentationPoseSourceFailureReason.None
                : Availability == PresentationPoseSourceAvailability.Pending
                    ? Outcome ==
                      CharacterPoseSourceFrameOutcome.AwaitingSample &&
                      FailureReason == PresentationPoseSourceFailureReason.None
                    : Availability ==
                      PresentationPoseSourceAvailability.Invalid &&
                      Outcome == CharacterPoseSourceFrameOutcome.Invalid &&
                      FailureReason !=
                      PresentationPoseSourceFailureReason.None);
        internal bool IsReady =>
            IsValid &&
            Availability == PresentationPoseSourceAvailability.Ready;
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
