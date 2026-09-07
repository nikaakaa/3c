using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterPoseSourceReadinessCategory : byte
    {
        Current = 1,
        DeferredTarget = 2
    }

    internal readonly struct CharacterPoseSourceReadinessView
    {
        internal CharacterPoseSourceReadinessView(
            ulong completionIdentity,
            PresentationPoseSourceAvailability availability,
            PresentationPoseSourceFailureReason failureReason,
            int resourceCatalogIndex,
            int groupClipIndex,
            ulong resourceGeneration,
            CharacterAclResourceFailureCode resourceFailureCode,
            string message)
        {
            CompletionIdentity = completionIdentity;
            Availability = availability;
            FailureReason = failureReason;
            ResourceCatalogIndex = resourceCatalogIndex;
            GroupClipIndex = groupClipIndex;
            ResourceGeneration = resourceGeneration;
            ResourceFailureCode = resourceFailureCode;
            Message = message ?? string.Empty;
            if (!IsValid)
                throw new ArgumentException("Character Pose source readiness is invalid.");
        }

        internal ulong CompletionIdentity { get; }
        internal PresentationPoseSourceAvailability Availability { get; }
        internal PresentationPoseSourceFailureReason FailureReason { get; }
        internal int ResourceCatalogIndex { get; }
        internal int GroupClipIndex { get; }
        internal ulong ResourceGeneration { get; }
        internal CharacterAclResourceFailureCode ResourceFailureCode { get; }
        internal string Message { get; }
        internal bool IsPending =>
            Availability == PresentationPoseSourceAvailability.Pending;
        internal bool IsReady =>
            Availability == PresentationPoseSourceAvailability.Ready;
        internal bool IsInvalid =>
            Availability == PresentationPoseSourceAvailability.Invalid;
        internal bool IsValid =>
            CompletionIdentity != 0 &&
            ((ResourceCatalogIndex == -1 &&
              GroupClipIndex == -1 &&
              ResourceGeneration == 0) ||
             (ResourceCatalogIndex >= 0 &&
              GroupClipIndex >= 0 &&
              ResourceGeneration != 0)) &&
            (Availability == PresentationPoseSourceAvailability.Pending &&
             FailureReason == PresentationPoseSourceFailureReason.None &&
             ResourceFailureCode == CharacterAclResourceFailureCode.None ||
             Availability == PresentationPoseSourceAvailability.Ready &&
             FailureReason == PresentationPoseSourceFailureReason.None &&
             ResourceFailureCode == CharacterAclResourceFailureCode.None ||
             Availability == PresentationPoseSourceAvailability.Invalid &&
             FailureReason != PresentationPoseSourceFailureReason.None &&
             ResourceFailureCode != CharacterAclResourceFailureCode.None);

        internal static CharacterPoseSourceReadinessView Ready(
            ulong completionIdentity) =>
            new CharacterPoseSourceReadinessView(
                completionIdentity,
                PresentationPoseSourceAvailability.Ready,
                PresentationPoseSourceFailureReason.None,
                -1,
                -1,
                0,
                CharacterAclResourceFailureCode.None,
                string.Empty);

        internal static CharacterPoseSourceReadinessView Ready(
            ulong completionIdentity,
            int resourceCatalogIndex,
            int groupClipIndex,
            ulong resourceGeneration) =>
            new CharacterPoseSourceReadinessView(
                completionIdentity,
                PresentationPoseSourceAvailability.Ready,
                PresentationPoseSourceFailureReason.None,
                resourceCatalogIndex,
                groupClipIndex,
                resourceGeneration,
                CharacterAclResourceFailureCode.None,
                string.Empty);

        internal static CharacterPoseSourceReadinessView Pending(
            ulong completionIdentity,
            int resourceCatalogIndex,
            int groupClipIndex,
            ulong resourceGeneration,
            string message) =>
            new CharacterPoseSourceReadinessView(
                completionIdentity,
                PresentationPoseSourceAvailability.Pending,
                PresentationPoseSourceFailureReason.None,
                resourceCatalogIndex,
                groupClipIndex,
                resourceGeneration,
                CharacterAclResourceFailureCode.None,
                message);

        internal static CharacterPoseSourceReadinessView Invalid(
            ulong completionIdentity,
            int resourceCatalogIndex,
            int groupClipIndex,
            ulong resourceGeneration,
            CharacterAclResourceFailureCode resourceFailureCode,
            string message) =>
            new CharacterPoseSourceReadinessView(
                completionIdentity,
                PresentationPoseSourceAvailability.Invalid,
                PresentationPoseSourceFailureReason.BackendFailure,
                resourceCatalogIndex,
                groupClipIndex,
                resourceGeneration,
                resourceFailureCode,
                message);
    }

    internal readonly struct CharacterPoseSourceReadinessKey : IEquatable<CharacterPoseSourceReadinessKey>
    {
        internal CharacterPoseSourceReadinessKey(
            CharacterPoseSourcePreparationKind kind,
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            int bindingIndex)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseSourcePreparationKind), kind) ||
                !poseNodeId.IsValid || bindingIndex < -1 ||
                (!sourceId.IsValid && bindingIndex < 0))
            {
                throw new ArgumentException("Character Pose source readiness key is invalid.");
            }
            Kind = kind;
            SourceId = sourceId;
            PoseNodeId = poseNodeId;
            BindingIndex = bindingIndex;
        }

        internal CharacterPoseSourcePreparationKind Kind { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PoseNodeId { get; }
        internal int BindingIndex { get; }
        internal bool IsValid =>
            Enum.IsDefined(typeof(CharacterPoseSourcePreparationKind), Kind) &&
            PoseNodeId.IsValid &&
            BindingIndex >= -1 &&
            (SourceId.IsValid || BindingIndex >= 0);

        public bool Equals(CharacterPoseSourceReadinessKey other) =>
            Kind == other.Kind &&
            SourceId.Equals(other.SourceId) &&
            PoseNodeId.Equals(other.PoseNodeId) &&
            BindingIndex == other.BindingIndex;

        public override bool Equals(object obj) =>
            obj is CharacterPoseSourceReadinessKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Kind, SourceId, PoseNodeId, BindingIndex);
    }

    internal readonly struct CharacterPoseSourceReadinessEntry
    {
        internal CharacterPoseSourceReadinessEntry(
            CharacterPoseSourceReadinessCategory category,
            in CharacterPoseSourceReadinessKey key,
            in CharacterPoseSourceReadinessView readiness)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseSourceReadinessCategory), category) ||
                !key.IsValid || !readiness.IsValid)
            {
                throw new ArgumentException("Character Pose source readiness entry is invalid.");
            }
            Category = category;
            Key = key;
            Readiness = readiness;
        }

        internal CharacterPoseSourceReadinessCategory Category { get; }
        internal CharacterPoseSourceReadinessKey Key { get; }
        internal CharacterPoseSourceReadinessView Readiness { get; }
    }

}
