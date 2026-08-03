using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PhysicalPoseSourceKind : byte
    {
        Action = 1,
        Presentation = 2
    }

    public readonly struct PhysicalPoseSourceKey :
        IEquatable<PhysicalPoseSourceKey>
    {
        PhysicalPoseSourceKey(
            PhysicalPoseSourceKind kind,
            AnimationPlaybackId actionPlaybackId,
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceIndex presentationSourceIndex,
            PresentationPoseSourceGeneration presentationGeneration)
        {
            Kind = kind;
            ActionPlaybackId = actionPlaybackId;
            ProviderId = providerId;
            PresentationSourceIndex = presentationSourceIndex;
            PresentationGeneration = presentationGeneration;
            if (!IsValid)
                throw new ArgumentException(
                    "Physical Pose source key is invalid.");
        }

        public PhysicalPoseSourceKind Kind { get; }
        public AnimationPlaybackId ActionPlaybackId { get; }
        public PresentationPoseSourceProviderId ProviderId { get; }
        public PresentationPoseSourceIndex PresentationSourceIndex { get; }
        public PresentationPoseSourceGeneration PresentationGeneration { get; }
        public bool IsValid =>
            Kind == PhysicalPoseSourceKind.Action
                ? ActionPlaybackId.IsValid &&
                  !ProviderId.IsValid &&
                  !PresentationSourceIndex.IsValid &&
                  !PresentationGeneration.IsValid
                : Kind == PhysicalPoseSourceKind.Presentation &&
                  !ActionPlaybackId.IsValid &&
                  ProviderId.IsValid &&
                  PresentationSourceIndex.IsValid &&
                  PresentationGeneration.IsValid;

        public static PhysicalPoseSourceKey Action(
            AnimationPlaybackId playbackId) =>
            new PhysicalPoseSourceKey(
                PhysicalPoseSourceKind.Action,
                playbackId,
                default,
                default,
                default);

        public static PhysicalPoseSourceKey Presentation(
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceIndex sourceIndex,
            PresentationPoseSourceGeneration generation) =>
            new PhysicalPoseSourceKey(
                PhysicalPoseSourceKind.Presentation,
                default,
                providerId,
                sourceIndex,
                generation);

        public bool Equals(PhysicalPoseSourceKey other) =>
            Kind == other.Kind &&
            ActionPlaybackId.Equals(other.ActionPlaybackId) &&
            ProviderId.Equals(other.ProviderId) &&
            PresentationSourceIndex.Equals(other.PresentationSourceIndex) &&
            PresentationGeneration.Equals(other.PresentationGeneration);
        public override bool Equals(object obj) =>
            obj is PhysicalPoseSourceKey other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(
                (int)Kind,
                ActionPlaybackId,
                ProviderId,
                PresentationSourceIndex,
                PresentationGeneration);
        public override string ToString() =>
            Kind == PhysicalPoseSourceKind.Action
                ? $"action/{ActionPlaybackId}"
                : $"presentation/{ProviderId}/{PresentationSourceIndex}@{PresentationGeneration}";
    }

    public readonly struct PhysicalPoseSourceIdentity :
        IEquatable<PhysicalPoseSourceIdentity>
    {
        public PhysicalPoseSourceIdentity(int index, ulong generation)
        {
            Index = index;
            Generation = generation;
            if (!IsValid)
                throw new ArgumentException(
                    "Physical Pose source identity is invalid.");
        }

        public int Index { get; }
        public ulong Generation { get; }
        public bool IsValid => Index >= 0 && Generation != 0;
        public bool Equals(PhysicalPoseSourceIdentity other) =>
            Index == other.Index && Generation == other.Generation;
        public override bool Equals(object obj) =>
            obj is PhysicalPoseSourceIdentity other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(Index, Generation);
    }

    public readonly struct PhysicalPoseSourceMutationLease
    {
        internal PhysicalPoseSourceMutationLease(ulong identity)
        {
            Identity = identity;
        }

        public ulong Identity { get; }
        public bool IsValid => Identity != 0;
    }
}
