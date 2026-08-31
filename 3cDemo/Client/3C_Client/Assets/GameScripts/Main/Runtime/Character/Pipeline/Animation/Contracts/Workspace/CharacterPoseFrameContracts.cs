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
}
