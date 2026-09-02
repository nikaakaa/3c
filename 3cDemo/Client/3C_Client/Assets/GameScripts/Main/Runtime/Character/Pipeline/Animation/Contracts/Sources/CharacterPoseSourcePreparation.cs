using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterPoseSourcePreparationKind : byte
    {
        Action = 1,
        Provider = 2,
        DirectPlayer = 3,
        ClipPlayer = 4,
        BlendSpacePlayer = 5
    }

    internal readonly struct CharacterPoseSourcePreparation
    {
        CharacterPoseSourcePreparation(
            CharacterPoseSourcePreparationKind kind,
            in AnimationPoseSampleRequest request,
            PresentationPoseSourceSample providerSample,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            int bindingIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId)
        {
            Kind = kind;
            Request = request;
            ProviderSample = providerSample;
            SourceId = sourceId;
            SourceOwnerIndex = sourceOwnerIndex;
            m_EncodedBindingIndex = bindingIndex < 0
                ? 0
                : checked(bindingIndex + 1);
            Clips = clips;
            Capture = capture;
            PoseNodeId = poseNodeId;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source preparation is invalid.");
            }
        }

        readonly int m_EncodedBindingIndex;
        internal CharacterPoseSourcePreparationKind Kind { get; }
        internal AnimationPoseSampleRequest Request { get; }
        internal PresentationPoseSourceSample ProviderSample { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal int SourceOwnerIndex { get; }
        internal int BindingIndex => m_EncodedBindingIndex - 1;
        internal AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        internal AnimationPoseSourceCaptureBinding Capture { get; }
        internal PoseNodeId PoseNodeId { get; }

        internal bool IsValid
        {
            get
            {
                if (!PoseNodeId.IsValid ||
                    Capture.CompletionIdentity == 0)
                {
                    return false;
                }
                switch (Kind)
                {
                    case CharacterPoseSourcePreparationKind.Action:
                        return Request.IsValid &&
                               Request.SourceId.SourceKind ==
                               AnimationPoseSourceKind.Timeline &&
                               ProviderSample == null &&
                               !SourceId.IsValid &&
                               SourceOwnerIndex == -1 &&
                               m_EncodedBindingIndex == 0 &&
                               Clips.Count == 0 &&
                               Capture.SourceId.Equals(
                                   Request.SourceId);
                    case CharacterPoseSourcePreparationKind.Provider:
                        return Request.IsValid &&
                               Request.SourceId.SourceKind ==
                               AnimationPoseSourceKind.MotionMatching &&
                               ProviderSample?.IsValid == true &&
                               ProviderSample.Availability ==
                               PresentationPoseSourceAvailability.Ready &&
                               !SourceId.IsValid &&
                               SourceOwnerIndex == -1 &&
                               m_EncodedBindingIndex == 0 &&
                               Clips.Count == 0 &&
                               Capture.SourceId.Equals(
                                   Request.SourceId);
                    case CharacterPoseSourcePreparationKind.DirectPlayer:
                        return !Request.IsValid &&
                               ProviderSample?.IsValid == true &&
                               ProviderSample.Availability ==
                               PresentationPoseSourceAvailability.Ready &&
                               SourceId.IsValid &&
                               SourceId.SourceKind ==
                               AnimationPoseSourceKind.MotionMatching &&
                               SourceOwnerIndex >= 0 &&
                               m_EncodedBindingIndex > 0 &&
                               Clips.Count > 0 &&
                               Capture.SourceId.Equals(SourceId);
                    case CharacterPoseSourcePreparationKind.ClipPlayer:
                        return !Request.IsValid &&
                               ProviderSample == null &&
                               SourceId.IsValid &&
                               SourceId.SourceKind ==
                               AnimationPoseSourceKind.Clip &&
                               SourceOwnerIndex >= 0 &&
                               m_EncodedBindingIndex > 0 &&
                               Clips.Count > 0 &&
                               Capture.SourceId.Equals(SourceId);
                    case CharacterPoseSourcePreparationKind.BlendSpacePlayer:
                        return !Request.IsValid &&
                               ProviderSample == null &&
                               SourceId.IsValid &&
                               SourceId.SourceKind ==
                               AnimationPoseSourceKind.BlendSpace &&
                               SourceOwnerIndex >= 0 &&
                               m_EncodedBindingIndex > 0 &&
                               Clips.Count > 0 &&
                               Capture.SourceId.Equals(SourceId);
                    default:
                        return false;
                }
            }
        }

        internal static CharacterPoseSourcePreparation Action(
            in AnimationPoseSampleRequest request,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId) =>
            new CharacterPoseSourcePreparation(
                CharacterPoseSourcePreparationKind.Action,
                in request,
                null,
                default,
                -1,
                -1,
                default,
                in capture,
                poseNodeId);

        internal static CharacterPoseSourcePreparation Provider(
            in AnimationPoseSampleRequest request,
            in PresentationPoseSourceSample sample,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId) =>
            new CharacterPoseSourcePreparation(
                CharacterPoseSourcePreparationKind.Provider,
                in request,
                sample,
                default,
                -1,
                -1,
                default,
                in capture,
                poseNodeId);

        internal static CharacterPoseSourcePreparation DirectPlayer(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in PresentationPoseSourceSample sample,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId) =>
            new CharacterPoseSourcePreparation(
                CharacterPoseSourcePreparationKind.DirectPlayer,
                default,
                sample,
                sourceId,
                sourceOwnerIndex,
                bindingIndex,
                clips,
                in capture,
                poseNodeId);

        internal static CharacterPoseSourcePreparation ClipPlayer(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId) =>
            new CharacterPoseSourcePreparation(
                CharacterPoseSourcePreparationKind.ClipPlayer,
                default,
                null,
                sourceId,
                sourceOwnerIndex,
                bindingIndex,
                clips,
                in capture,
                poseNodeId);

        internal static CharacterPoseSourcePreparation BlendSpacePlayer(
            int bindingIndex,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId poseNodeId) =>
            new CharacterPoseSourcePreparation(
                CharacterPoseSourcePreparationKind.BlendSpacePlayer,
                default,
                null,
                sourceId,
                sourceOwnerIndex,
                bindingIndex,
                clips,
                in capture,
                poseNodeId);
    }
}
