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

    internal sealed class CharacterPoseSourcePreparationPage
    {
        readonly CharacterPoseSourcePreparation[] m_Preparations;
        ulong m_CompletionIdentity;
        int m_Count;

        internal CharacterPoseSourcePreparationPage(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Preparations =
                new CharacterPoseSourcePreparation[capacity];
        }

        internal CharacterPoseSourcePreparationView Begin(
            ulong completionIdentity)
        {
            if (completionIdentity == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completionIdentity));
            }
            Clear();
            m_CompletionIdentity = completionIdentity;
            return new CharacterPoseSourcePreparationView(
                this,
                completionIdentity);
        }

        internal int Add(
            in CharacterPoseSourcePreparation preparation)
        {
            if (!preparation.IsValid ||
                preparation.Capture.CompletionIdentity !=
                    m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose source preparation does not match the open demand page.");
            }
            if (m_Count >= m_Preparations.Length)
            {
                throw new InvalidOperationException(
                    "Character Pose source preparation capacity was exceeded.");
            }
            int index = m_Count++;
            m_Preparations[index] = preparation;
            return index;
        }

        internal bool Matches(ulong completionIdentity) =>
            completionIdentity != 0 &&
            completionIdentity == m_CompletionIdentity;

        internal int RequireCount(ulong completionIdentity)
        {
            RequireOpen(completionIdentity);
            return m_Count;
        }

        internal CharacterPoseSourcePreparation Require(
            int index,
            ulong completionIdentity)
        {
            RequireOpen(completionIdentity);
            if ((uint)index >= (uint)m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Preparations[index];
        }

        internal void Clear()
        {
            Array.Clear(m_Preparations, 0, m_Count);
            m_CompletionIdentity = 0;
            m_Count = 0;
        }

        void RequireOpen(ulong completionIdentity)
        {
            if (!Matches(completionIdentity))
            {
                throw new InvalidOperationException(
                    "Character Pose source preparation page is stale.");
            }
        }
    }

    internal readonly struct CharacterPoseSourcePreparationView
    {
        internal CharacterPoseSourcePreparationView(
            CharacterPoseSourcePreparationPage page,
            ulong completionIdentity)
        {
            if (page == null || !page.Matches(completionIdentity))
            {
                throw new ArgumentException(
                    "Character Pose source preparation view is invalid.");
            }
            m_Page = page;
            CompletionIdentity = completionIdentity;
        }

        readonly CharacterPoseSourcePreparationPage m_Page;
        internal ulong CompletionIdentity { get; }
        internal int Count => m_Page.RequireCount(CompletionIdentity);
        internal bool IsValid =>
            m_Page != null &&
            m_Page.Matches(CompletionIdentity);

        internal CharacterPoseSourcePreparation Get(int index) =>
            m_Page.Require(index, CompletionIdentity);

        internal bool Matches(
            in CharacterPoseSourcePreparationView other) =>
            m_Page != null &&
            ReferenceEquals(m_Page, other.m_Page) &&
            CompletionIdentity == other.CompletionIdentity &&
            IsValid &&
            other.IsValid;
    }
}
