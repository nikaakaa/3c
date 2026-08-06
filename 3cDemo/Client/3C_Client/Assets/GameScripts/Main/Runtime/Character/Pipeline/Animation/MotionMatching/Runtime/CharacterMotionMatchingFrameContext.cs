using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal readonly struct CharacterMotionMatchingRigLineage : IEquatable<CharacterMotionMatchingRigLineage>
    {
        internal CharacterMotionMatchingRigLineage(string rigId, string rigRevision, int poseBoneCount)
        {
            if (string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision) || poseBoneCount <= 0)
                throw new ArgumentException("Motion Matching Rig lineage is incomplete.");
            RigId = rigId;
            RigRevision = rigRevision;
            PoseBoneCount = poseBoneCount;
        }

        internal string RigId { get; }
        internal string RigRevision { get; }
        internal int PoseBoneCount { get; }
        internal bool IsValid => !string.IsNullOrEmpty(RigId) && !string.IsNullOrEmpty(RigRevision) && PoseBoneCount > 0;

        public bool Equals(CharacterMotionMatchingRigLineage other) =>
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            PoseBoneCount == other.PoseBoneCount;

        public override bool Equals(object obj) => obj is CharacterMotionMatchingRigLineage other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RigId, RigRevision, PoseBoneCount);
    }

    internal readonly struct CharacterMotionMatchingTrajectoryReadView
    {
        readonly MotionMatchingTrajectoryEnvelope m_Envelope;

        internal CharacterMotionMatchingTrajectoryReadView(MotionMatchingTrajectoryEnvelope envelope)
        {
            m_Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            if (!envelope.SourceIdentity.IsValid || envelope.Count == 0)
                throw new ArgumentException("Motion Matching Trajectory page is incomplete.", nameof(envelope));
        }

        internal int Count => m_Envelope?.Count ?? 0;
        internal MotionMatchingTrajectorySourceIdentity SourceIdentity => m_Envelope?.SourceIdentity ?? default;
        internal ulong ResetSequence => m_Envelope?.ResetSequence ?? 0;
        internal MotionMatchingTrajectoryEnvelope Envelope => m_Envelope;
        internal MotionMatchingTrajectoryEnvelopePoint GetPoint(int index) => m_Envelope[index];
        internal bool IsValid => m_Envelope != null && Count > 0 && SourceIdentity.IsValid;
    }

    internal readonly struct CharacterMotionMatchingFrameContext
    {
        internal CharacterMotionMatchingFrameContext(
            ulong frameIdentity,
            float deltaTime,
            in CharacterPresentationFactFrame facts,
            CharacterMotionMatchingTrajectoryReadView trajectory,
            CharacterMotionMatchingRigLineage rigLineage)
        {
            if (frameIdentity == 0 || !float.IsFinite(deltaTime) || deltaTime < 0f ||
                !facts.IsValid || !trajectory.IsValid || !rigLineage.IsValid ||
                facts.Identity.RenderFrame != frameIdentity)
            {
                throw new ArgumentException("Motion Matching Frame Context is incomplete.");
            }
            FrameIdentity = frameIdentity;
            DeltaTime = deltaTime;
            Facts = facts;
            Trajectory = trajectory;
            RigLineage = rigLineage;
        }

        internal ulong FrameIdentity { get; }
        internal float DeltaTime { get; }
        internal CharacterPresentationFactFrame Facts { get; }
        internal CharacterMotionMatchingTrajectoryReadView Trajectory { get; }
        internal CharacterMotionMatchingRigLineage RigLineage { get; }
        internal ulong ResetSequence => Trajectory.ResetSequence;
        internal bool IsValid => FrameIdentity != 0 && Facts.IsValid && Trajectory.IsValid && RigLineage.IsValid;

        internal static CharacterMotionMatchingFrameContext Resolve(
            float deltaTime,
            in CharacterPresentationFactFrame facts,
            MotionMatchingTrajectoryEnvelope trajectory,
            CharacterAnimationRigPayload rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            return new CharacterMotionMatchingFrameContext(
                facts.Identity.RenderFrame,
                deltaTime,
                in facts,
                new CharacterMotionMatchingTrajectoryReadView(trajectory),
                new CharacterMotionMatchingRigLineage(rig.RigId, rig.RigRevision, rig.PoseBoneCount));
        }
    }
}
