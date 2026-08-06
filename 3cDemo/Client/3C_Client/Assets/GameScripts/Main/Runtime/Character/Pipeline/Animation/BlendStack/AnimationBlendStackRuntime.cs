using System;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal readonly struct AnimationBlendStackSourceReleaseToken
    {
        internal AnimationBlendStackSourceReleaseToken(
            int releaseOrdinal,
            in AnimationBlendStackRelease release,
            in AnimationBlendSourcePoseReleaseToken sourcePoseRelease)
        {
            if (releaseOrdinal < 0 ||
                !release.PoseNodeId.IsValid ||
                !release.SourceId.IsValid ||
                release.CompletionIdentity == 0 ||
                !sourcePoseRelease.IsValid ||
                !sourcePoseRelease.SourceId.Equals(release.SourceId))
            {
                throw new ArgumentException(
                    "Animation Blend Stack source release token is invalid.");
            }
            ReleaseOrdinal = releaseOrdinal;
            Release = release;
            SourcePoseRelease = sourcePoseRelease;
        }

        internal int ReleaseOrdinal { get; }
        internal AnimationBlendStackRelease Release { get; }
        internal AnimationBlendSourcePoseReleaseToken SourcePoseRelease { get; }
        internal bool IsValid =>
            ReleaseOrdinal >= 0 &&
            Release.PoseNodeId.IsValid &&
            Release.SourceId.IsValid &&
            Release.CompletionIdentity != 0 &&
            SourcePoseRelease.IsValid;
    }

    internal sealed class AnimationBlendStackRuntime : IDisposable
    {
        struct State
        {
            internal int EntryCount;
            internal int StackReleaseHead;
            internal int StackReleaseCount;
            internal ulong LastRequestSequence;
            internal ulong LastCompletionIdentity;
            internal ulong LastContributionContinuityIdentity;
            internal ulong ContinuityIdentity;
            internal ulong NextContinuityIdentity;
            internal ulong PendingPlanCompletionIdentity;
            internal ulong SourceFrameCompletionIdentity;
            internal float LastOutputWeight;
            internal float StoredOutputWeight;
            internal float PendingCaptureOutputWeight;
            internal float PlannedStoredMaximumWeight;
            internal bool HasCompletedFrame;
            internal bool HasStoredPose;
            internal bool HasPendingStoredCapture;
            internal bool SelectionUnavailable;
            internal AnimationPoseAvailability
                LastAvailability;
            internal AnimationPoseNativeInvalidReason
                LastInvalidReason;
            internal AnimationSlotBlendFramePlanKind
                PendingPlanKind;
            internal ulong PendingStoredContributionIdentity;
        }

        readonly AnimationBlendNodePayload m_Slot;
        readonly AnimationChannelId m_AnimationChannelId;
        readonly PresentationPoseSourceProviderId m_PresentationPoseSourceProviderId;
        readonly PresentationPoseSourceIndex m_PresentationPoseSourceIndex;
        readonly AnimationSelectionAvailabilityPolicy m_AvailabilityPolicy;
        readonly AnimationBlendCurveCatalogPayload m_CurveCatalog;
        readonly AnimationBlendProfileCatalogPayload m_ProfileCatalog;
        readonly CharacterAnimationRigPayload m_Rig;
        AnimationBlendEntryState[] m_CommittedEntries;
        AnimationBlendEntryState[] m_PendingEntries;
        readonly uint[] m_PendingEntryVersions;
        readonly int[] m_PendingEntryDirtyIndices;
        readonly AnimationBlendEntryState[] m_CompactedEntries;
        readonly int[] m_EntrySourceCaptureIndices;
        readonly int[] m_CompactedSourceCaptureIndices;
        readonly float[] m_EntryRawAlphas;
        readonly float[] m_EntryEasedAlphas;
        readonly float[] m_EntryScalarWeights;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_PlannedEntryMaximumWeights;
        float[] m_CommittedStoredBoneOutputWeights;
        float[] m_PendingStoredBoneOutputWeights;
        readonly float[] m_PendingCaptureBoneOutputWeights;
        float[] m_CommittedLastBoneOutputWeights;
        float[] m_PendingLastBoneOutputWeights;
        readonly AnimationPoseSourceId[] m_RemovedSourceIds;
        readonly AnimationPoseSourceId[] m_PendingStackReleaseSourceIds;
        readonly AnimationBlendStackRelease[] m_CommittedStackReleases;
        readonly AnimationBlendStackRelease[] m_PendingStackReleases;
        readonly uint[] m_PendingStackReleaseVersions;
        readonly int[] m_PendingStackReleaseDirtyIndices;
        readonly AnimationBlendSourcePoseWorkspace m_Sources;
        readonly AnimationSlotBlendPoseWorkspace m_SlotWorkspace;
        State m_CommittedState;
        State m_PendingState;

        int m_PendingStackReleaseCount;
        int m_PreparedSourceReleaseCount;
        int m_AppliedPreparedSourceReleaseCount;
        bool m_Disposed;
        bool m_FrameOpen;
        bool m_PendingStoredBoneOutputWritten;
        bool m_PendingLastBoneOutputWritten;
        uint m_PendingEntryVersion;
        int m_PendingEntryDirtyCount;
        uint m_PendingStackReleaseVersion;
        int m_PendingStackReleaseDirtyCount;
        ulong m_PreparedCompletionIdentity;
        float m_MaxBlendInTimeToReplaceNewest;
        float m_DepthBlendTimeMultiplier;

        int m_EntryCount
        {
            get => m_FrameOpen ? m_PendingState.EntryCount : m_CommittedState.EntryCount;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.EntryCount = value;
                else
                    m_CommittedState.EntryCount = value;
            }
        }

        int m_StackReleaseHead
        {
            get => m_FrameOpen ? m_PendingState.StackReleaseHead : m_CommittedState.StackReleaseHead;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.StackReleaseHead = value;
                else
                    m_CommittedState.StackReleaseHead = value;
            }
        }

        int m_StackReleaseCount
        {
            get => m_FrameOpen ? m_PendingState.StackReleaseCount : m_CommittedState.StackReleaseCount;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.StackReleaseCount = value;
                else
                    m_CommittedState.StackReleaseCount = value;
            }
        }

        ulong m_LastRequestSequence
        {
            get => m_FrameOpen ? m_PendingState.LastRequestSequence : m_CommittedState.LastRequestSequence;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastRequestSequence = value;
                else
                    m_CommittedState.LastRequestSequence = value;
            }
        }

        ulong m_LastCompletionIdentity
        {
            get => m_FrameOpen ? m_PendingState.LastCompletionIdentity : m_CommittedState.LastCompletionIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastCompletionIdentity = value;
                else
                    m_CommittedState.LastCompletionIdentity = value;
            }
        }

        ulong m_LastContributionContinuityIdentity
        {
            get => m_FrameOpen ? m_PendingState.LastContributionContinuityIdentity : m_CommittedState.LastContributionContinuityIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastContributionContinuityIdentity = value;
                else
                    m_CommittedState.LastContributionContinuityIdentity = value;
            }
        }

        ulong m_ContinuityIdentity
        {
            get => m_FrameOpen ? m_PendingState.ContinuityIdentity : m_CommittedState.ContinuityIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.ContinuityIdentity = value;
                else
                    m_CommittedState.ContinuityIdentity = value;
            }
        }

        ulong m_NextContinuityIdentity
        {
            get => m_FrameOpen ? m_PendingState.NextContinuityIdentity : m_CommittedState.NextContinuityIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.NextContinuityIdentity = value;
                else
                    m_CommittedState.NextContinuityIdentity = value;
            }
        }

        ulong m_PendingPlanCompletionIdentity
        {
            get => m_FrameOpen ? m_PendingState.PendingPlanCompletionIdentity : m_CommittedState.PendingPlanCompletionIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.PendingPlanCompletionIdentity = value;
                else
                    m_CommittedState.PendingPlanCompletionIdentity = value;
            }
        }

        ulong m_SourceFrameCompletionIdentity
        {
            get => m_FrameOpen ? m_PendingState.SourceFrameCompletionIdentity : m_CommittedState.SourceFrameCompletionIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.SourceFrameCompletionIdentity = value;
                else
                    m_CommittedState.SourceFrameCompletionIdentity = value;
            }
        }

        float m_LastOutputWeight
        {
            get => m_FrameOpen ? m_PendingState.LastOutputWeight : m_CommittedState.LastOutputWeight;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastOutputWeight = value;
                else
                    m_CommittedState.LastOutputWeight = value;
            }
        }

        float m_StoredOutputWeight
        {
            get => m_FrameOpen ? m_PendingState.StoredOutputWeight : m_CommittedState.StoredOutputWeight;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.StoredOutputWeight = value;
                else
                    m_CommittedState.StoredOutputWeight = value;
            }
        }

        float m_PendingCaptureOutputWeight
        {
            get => m_FrameOpen ? m_PendingState.PendingCaptureOutputWeight : m_CommittedState.PendingCaptureOutputWeight;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.PendingCaptureOutputWeight = value;
                else
                    m_CommittedState.PendingCaptureOutputWeight = value;
            }
        }

        float m_PlannedStoredMaximumWeight
        {
            get => m_FrameOpen ? m_PendingState.PlannedStoredMaximumWeight : m_CommittedState.PlannedStoredMaximumWeight;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.PlannedStoredMaximumWeight = value;
                else
                    m_CommittedState.PlannedStoredMaximumWeight = value;
            }
        }

        bool m_HasCompletedFrame
        {
            get => m_FrameOpen ? m_PendingState.HasCompletedFrame : m_CommittedState.HasCompletedFrame;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.HasCompletedFrame = value;
                else
                    m_CommittedState.HasCompletedFrame = value;
            }
        }

        bool m_HasStoredPose
        {
            get => m_FrameOpen ? m_PendingState.HasStoredPose : m_CommittedState.HasStoredPose;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.HasStoredPose = value;
                else
                    m_CommittedState.HasStoredPose = value;
            }
        }

        bool m_HasPendingStoredCapture
        {
            get => m_FrameOpen ? m_PendingState.HasPendingStoredCapture : m_CommittedState.HasPendingStoredCapture;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.HasPendingStoredCapture = value;
                else
                    m_CommittedState.HasPendingStoredCapture = value;
            }
        }

        bool m_SelectionUnavailable
        {
            get => m_FrameOpen ? m_PendingState.SelectionUnavailable : m_CommittedState.SelectionUnavailable;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.SelectionUnavailable = value;
                else
                    m_CommittedState.SelectionUnavailable = value;
            }
        }

        AnimationPoseAvailability m_LastAvailability
        {
            get => m_FrameOpen ? m_PendingState.LastAvailability : m_CommittedState.LastAvailability;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastAvailability = value;
                else
                    m_CommittedState.LastAvailability = value;
            }
        }

        AnimationPoseNativeInvalidReason m_LastInvalidReason
        {
            get => m_FrameOpen ? m_PendingState.LastInvalidReason : m_CommittedState.LastInvalidReason;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.LastInvalidReason = value;
                else
                    m_CommittedState.LastInvalidReason = value;
            }
        }

        AnimationSlotBlendFramePlanKind m_PendingPlanKind
        {
            get => m_FrameOpen ? m_PendingState.PendingPlanKind : m_CommittedState.PendingPlanKind;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.PendingPlanKind = value;
                else
                    m_CommittedState.PendingPlanKind = value;
            }
        }

        ulong m_PendingStoredContributionIdentity
        {
            get => m_FrameOpen ? m_PendingState.PendingStoredContributionIdentity : m_CommittedState.PendingStoredContributionIdentity;
            set
            {
                if (m_FrameOpen)
                    m_PendingState.PendingStoredContributionIdentity = value;
                else
                    m_CommittedState.PendingStoredContributionIdentity = value;
            }
        }

        float[] m_StoredBoneOutputWeights =>
            m_FrameOpen && m_PendingStoredBoneOutputWritten
                ? m_PendingStoredBoneOutputWeights
                : m_CommittedStoredBoneOutputWeights;

        float[] m_LastBoneOutputWeights =>
            m_FrameOpen && m_PendingLastBoneOutputWritten
                ? m_PendingLastBoneOutputWeights
                : m_CommittedLastBoneOutputWeights;

        int EntryCapacity => m_CommittedEntries.Length;
        int StackReleaseCapacity => m_CommittedStackReleases.Length;

        internal AnimationBlendStackRuntime(
            AnimationBlendNodePayload slot,
            AnimationChannelId animationChannelId,
            PresentationPoseSourceProviderId presentationPoseSourceProviderId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            AnimationSelectionAvailabilityPolicy availabilityPolicy,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            CharacterAnimationRigPayload rig,
            in AnimationPlayerPoseNativeWriteBinding initialFinalWriteBinding)
        {
            m_Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            m_AnimationChannelId = animationChannelId;
            m_PresentationPoseSourceProviderId =
                presentationPoseSourceProviderId;
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex;
            m_AvailabilityPolicy = availabilityPolicy;
            m_CurveCatalog = curveCatalog ?? throw new ArgumentNullException(nameof(curveCatalog));
            m_ProfileCatalog = profileCatalog ?? throw new ArgumentNullException(nameof(profileCatalog));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            bool actionOwned = animationChannelId.IsValid;
            bool providerOwned = presentationPoseSourceProviderId.IsValid &&
                                 presentationPoseSourceIndex.IsValid;
            if (!slot.NodeId.IsValid || actionOwned == providerOwned ||
                !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), availabilityPolicy) ||
                slot.StackPolicy == null || curveCatalog.Entries.Count == 0 ||
                profileCatalog.Entries.Count == 0)
            {
                throw new ArgumentException("Animation Blend Stack assembly is invalid.");
            }

            rig.RequireValid();
            slot.StackPolicy.RequireValid();
            m_MaxBlendInTimeToReplaceNewest =
                slot.StackPolicy.MaxBlendInTimeToReplaceNewest;
            m_DepthBlendTimeMultiplier =
                slot.StackPolicy.DepthBlendTimeMultiplier;
            for (int i = 0; i < curveCatalog.Entries.Count; i++)
                curveCatalog.Require(i).RequireValid();
            for (int i = 0; i < profileCatalog.Entries.Count; i++)
                profileCatalog.Require(i).RequireValid(rig.PoseBoneCount, rig.RigId, rig.RigRevision);
            for (int i = 0; i < slot.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = slot.Transitions[i] ??
                    throw new InvalidOperationException($"Animation Blend transition #{i} is missing.");
                transition.RequireValid(curveCatalog.Entries.Count, profileCatalog.Entries.Count);
                curveCatalog.Require(transition.CurveIndex);
                profileCatalog.Require(transition.BlendProfileIndex);
            }

            int capacity = slot.StackPolicy.MaxActiveSourceEntries;
            int boneCount = rig.PoseBoneCount;
            int parameterCount = initialFinalWriteBinding.PoseParameters.Length;
            if (initialFinalWriteBinding.DenseLocalPoses.Length != boneCount || parameterCount <= 0)
                throw new ArgumentException("Animation Blend Stack final Slot layout is invalid.", nameof(initialFinalWriteBinding));

            m_CommittedEntries = new AnimationBlendEntryState[capacity];
            m_PendingEntries = new AnimationBlendEntryState[capacity];
            m_PendingEntryVersions = new uint[capacity];
            m_PendingEntryDirtyIndices = new int[capacity];
            m_CompactedEntries = new AnimationBlendEntryState[capacity];
            m_EntrySourceCaptureIndices = new int[capacity];
            m_CompactedSourceCaptureIndices = new int[capacity];
            m_EntryRawAlphas = new float[capacity];
            m_EntryEasedAlphas = new float[capacity];
            m_EntryScalarWeights = new float[capacity];
            m_EntryBoneWeights = new float[checked(capacity * boneCount)];
            m_PlannedEntryMaximumWeights = new float[capacity];
            m_CommittedStoredBoneOutputWeights = new float[boneCount];
            m_PendingStoredBoneOutputWeights = new float[boneCount];
            m_PendingCaptureBoneOutputWeights = new float[boneCount];
            m_CommittedLastBoneOutputWeights = new float[boneCount];
            m_PendingLastBoneOutputWeights = new float[boneCount];
            m_RemovedSourceIds = new AnimationPoseSourceId[capacity + 1];
            m_PendingStackReleaseSourceIds = new AnimationPoseSourceId[capacity + 1];
            m_CommittedStackReleases = new AnimationBlendStackRelease[capacity + 1];
            m_PendingStackReleases = new AnimationBlendStackRelease[capacity + 1];
            m_PendingStackReleaseVersions = new uint[capacity + 1];
            m_PendingStackReleaseDirtyIndices = new int[capacity + 1];
            m_CommittedState.ContinuityIdentity = 1;
            m_CommittedState.NextContinuityIdentity = 2;
            Fill(m_EntrySourceCaptureIndices, -1);
            Fill(m_CompactedSourceCaptureIndices, -1);

            try
            {
                m_Sources = new AnimationBlendSourcePoseWorkspace(rig, parameterCount, capacity + 1);
                m_SlotWorkspace = new AnimationSlotBlendPoseWorkspace(capacity, in initialFinalWriteBinding);
            }
            catch
            {
                m_Sources?.Dispose();
                m_SlotWorkspace?.Dispose();
                throw;
            }
        }

        internal PoseNodeId PoseNodeId => m_Slot.NodeId;
        internal int PlayerIndex => m_SlotWorkspace.PhysicalPlayerIndex;
        internal AnimationChannelId AnimationChannelId => m_AnimationChannelId;
        internal PresentationPoseSourceProviderId
            PresentationPoseSourceProviderId =>
                m_PresentationPoseSourceProviderId;
        internal PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex;
        internal AnimationSelectionAvailabilityPolicy OutputPolicy => m_AvailabilityPolicy;
        internal int EntryCount => m_EntryCount;
        internal bool HasStoredPose => m_HasStoredPose || m_HasPendingStoredCapture;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal bool HasCurrentSelectionSample => !m_SelectionUnavailable;
        internal AnimationPoseAvailability LastAvailability => m_LastAvailability;
        internal float LastOutputWeight => m_LastOutputWeight;
        internal AnimationPoseNativeInvalidReason LastInvalidReason => m_LastInvalidReason;
        internal ulong ContinuityIdentity => m_ContinuityIdentity;

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block)
        {
            if (layout == null || block == null)
                return "Animation Blend Stack tuning payload is missing.";
            if (m_FrameOpen)
                return "Animation Blend Stack tuning cannot change during an open frame.";
            string ownerId = $"animation-blend-policy:{m_Slot.PolicyId}";
            float replaceNewest = m_MaxBlendInTimeToReplaceNewest;
            float depthMultiplier = m_DepthBlendTimeMultiplier;
            for (int i = 0; i < layout.Entries.Count; i++)
            {
                CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                if (entry.Interaction !=
                        CharacterPoseTuningInteractionPolicy.TunableDefault ||
                    !string.Equals(
                        entry.OwnerId,
                        ownerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                CharacterPoseTuningValue value = block.GetValue(entry);
                if (value.Kind != CharacterPoseTuningValueKind.Float ||
                    !float.IsFinite(value.FloatValue))
                {
                    return $"Animation Blend Stack tuning field '{entry.FieldId}' is invalid.";
                }
                if (entry.FieldId.EndsWith(
                        "/max-blend-in-time-to-replace-newest",
                        StringComparison.Ordinal))
                {
                    if (value.FloatValue < 0f)
                        return "Animation Blend Stack Replace Newest Window cannot be negative.";
                    replaceNewest = value.FloatValue;
                }
                else if (entry.FieldId.EndsWith(
                             "/depth-blend-time-multiplier",
                             StringComparison.Ordinal))
                {
                    if (value.FloatValue <= 0f)
                        return "Animation Blend Stack Depth Blend Time Multiplier must be greater than zero.";
                    depthMultiplier = value.FloatValue;
                }
            }
            m_MaxBlendInTimeToReplaceNewest = replaceNewest;
            m_DepthBlendTimeMultiplier = depthMultiplier;
            return string.Empty;
        }

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen ||
                m_PendingPlanCompletionIdentity != 0 ||
                m_SourceFrameCompletionIdentity != 0 ||
                m_PreparedCompletionIdentity != 0 ||
                m_Sources.HasOpenFrame ||
                m_PendingStackReleaseCount != 0 ||
                m_PreparedSourceReleaseCount != 0 ||
                m_AppliedPreparedSourceReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack frame is already open.");
            }
            m_PendingState = m_CommittedState;
            AdvanceVersion(
                ref m_PendingEntryVersion,
                m_PendingEntryVersions);
            AdvanceVersion(
                ref m_PendingStackReleaseVersion,
                m_PendingStackReleaseVersions);
            m_PendingEntryDirtyCount = 0;
            m_PendingStoredBoneOutputWritten = false;
            m_PendingLastBoneOutputWritten = false;
            m_PendingStackReleaseDirtyCount = 0;
            m_SlotWorkspace.BeginFrame();
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            if (m_Sources.HasOpenFrame)
            {
                m_Sources.DiscardFrame(
                    m_Sources.CompletionIdentity);
            }
            m_SlotWorkspace.DiscardFrame();
            m_Sources.DiscardPreparedReleases();
            ClearPendingStackReleaseSources();
            m_PendingEntryDirtyCount = 0;
            m_PendingStoredBoneOutputWritten = false;
            m_PendingLastBoneOutputWritten = false;
            m_PendingStackReleaseDirtyCount = 0;
            m_PreparedCompletionIdentity = 0;
            m_PreparedSourceReleaseCount = 0;
            m_AppliedPreparedSourceReleaseCount = 0;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen ||
                m_PendingPlanCompletionIdentity != 0 ||
                m_SourceFrameCompletionIdentity != 0 ||
                m_PreparedCompletionIdentity != 0 ||
                m_PendingStackReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack frame is not sealed.");
            }
            if (m_Sources.HasOpenFrame)
            {
                m_Sources.CommitFrame(
                    m_Sources.CompletionIdentity);
            }
            m_SlotWorkspace.CommitFrame();
            CommitPendingEntries();
            CommitPendingStackReleases();
            if (m_PendingStoredBoneOutputWritten)
                Swap(ref m_CommittedStoredBoneOutputWeights, ref m_PendingStoredBoneOutputWeights);
            if (m_PendingLastBoneOutputWritten)
                Swap(ref m_CommittedLastBoneOutputWeights, ref m_PendingLastBoneOutputWeights);
            m_CommittedState = m_PendingState;
            m_PendingStoredBoneOutputWritten = false;
            m_PendingLastBoneOutputWritten = false;
            m_FrameOpen = false;
        }

        internal void CopyDiagnostics(
            int stackIndex,
            AnimationBlendStackSnapshot[] stackDestination,
            AnimationBlendStackEntrySnapshot[] entryDestination,
            int entryOffset,
            float[] entryBoneWeights,
            float[] storedBoneWeights)
        {
            RequireAlive();
            if ((uint)stackIndex >= (uint)stackDestination.Length || entryOffset < 0 ||
                entryOffset > entryDestination.Length - m_EntryCount ||
                entryBoneWeights.Length < checked((entryOffset + m_EntryCount) * m_Rig.PoseBoneCount) ||
                storedBoneWeights.Length < checked((stackIndex + 1) * m_Rig.PoseBoneCount))
            {
                throw new ArgumentException("Animation Blend Stack diagnostics capacity is invalid.");
            }

            for (int entryIndex = 0; entryIndex < m_EntryCount; entryIndex++)
            {
                AnimationBlendEntryState entry = ReadEntry(entryIndex);
                AnimationBlendProfilePayload profile = m_ProfileCatalog.Require(entry.BlendProfileIndex);
                int diagnosticIndex = entryOffset + entryIndex;
                entryDestination[diagnosticIndex] = new AnimationBlendStackEntrySnapshot(
                    AnimationChannelId,
                    PresentationPoseSourceProviderId,
                    PresentationPoseSourceIndex,
                    PoseNodeId,
                    entry.EntryId,
                    entryIndex,
                    entry.SourceOwnerIndex,
                    entry.CanonicalCurveIndex,
                    m_CurveCatalog.Entries[entry.CanonicalCurveIndex].CanonicalHash,
                    entry.BlendProfileIndex,
                    profile.ProfileId,
                    entry.PushDepth,
                    entry.GetOutputDuration(profile),
                    entry.ElapsedSeconds,
                    m_EntryRawAlphas[entryIndex],
                    m_EntryEasedAlphas[entryIndex],
                    m_EntryScalarWeights[entryIndex],
                    entry.ContributionContinuityIdentity);
                Array.Copy(
                    m_EntryBoneWeights,
                    entryIndex * m_Rig.PoseBoneCount,
                    entryBoneWeights,
                    diagnosticIndex * m_Rig.PoseBoneCount,
                    m_Rig.PoseBoneCount);
            }

            Array.Copy(
                m_HasPendingStoredCapture ? m_PendingCaptureBoneOutputWeights : m_StoredBoneOutputWeights,
                0,
                storedBoneWeights,
                stackIndex * m_Rig.PoseBoneCount,
                m_Rig.PoseBoneCount);
            bool hasStored = m_HasStoredPose || m_HasPendingStoredCapture;
            AnimationSlotBlendStoredPoseNativeState storedState = m_HasStoredPose
                ? RequireStoredState()
                : default;
            ulong storedIdentity = m_HasStoredPose
                ? storedState.ContributionContinuityIdentity
                : m_HasPendingStoredCapture ? m_PendingStoredContributionIdentity : 0;
            stackDestination[stackIndex] = new AnimationBlendStackSnapshot(
                AnimationChannelId,
                PresentationPoseSourceProviderId,
                PresentationPoseSourceIndex,
                PoseNodeId,
                OutputPolicy,
                entryOffset,
                m_EntryCount,
                m_HasCompletedFrame ? m_LastAvailability : AnimationPoseAvailability.Invalid,
                m_HasCompletedFrame ? m_LastInvalidReason : AnimationPoseNativeInvalidReason.None,
                m_HasCompletedFrame ? m_LastOutputWeight : 0f,
                m_ContinuityIdentity,
                m_LastCompletionIdentity,
                hasStored,
                m_HasPendingStoredCapture,
                m_HasPendingStoredCapture ? m_PendingCaptureOutputWeight : m_StoredOutputWeight,
                storedIdentity,
                storedState.CapturedAtCompletionIdentity,
                storedState.SourceHistoryCompletionIdentity,
                storedState.HasFootFeatures == 1,
                storedState.LeftFootFeatures,
                storedState.RightFootFeatures);
        }

        internal AnimationBlendEntryId GetEntryId(int index)
        {
            RequireAlive();
            if ((uint)index >= (uint)m_EntryCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ReadEntry(index).EntryId;
        }

        internal void GetCurrentRoutingEndpoint(
            out int sourceOwnerIndex,
            out AnimationBlendTransitionEndpointKind endpointKind)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            GetCurrentEndpoint(out sourceOwnerIndex, out endpointKind);
        }

        internal void BeginSourceFrame(ulong completionIdentity)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            m_Sources.BeginFrame(completionIdentity);
            m_SourceFrameCompletionIdentity = completionIdentity;
        }

        internal AnimationPoseSourceCaptureBinding PrepareCapture(
            in AnimationResolvedPoseSourceSample sourceSample,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            AnimationPoseSampleRequest request = sourceSample.Request;
            if (!request.IsValid ||
                m_SourceFrameCompletionIdentity == 0 ||
                m_SourceFrameCompletionIdentity != m_Sources.CompletionIdentity)
            {
                throw new ArgumentException("Animation source capture request is routed to the wrong Blend Stack.");
            }

            bool referenced = false;
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (entry.IsSourcePose || !entry.SourceId.Equals(request.SourceId))
                    continue;
                if (entry.SourceOwnerIndex != request.SourceOwnerIndex)
                    throw new InvalidOperationException("Animation source capture producer differs from its Blend entry.");
                referenced = true;
            }
            if (!referenced)
                throw new InvalidOperationException("Animation source capture is not referenced by this Blend Stack.");

            AnimationPoseSourceCaptureBinding binding = m_Sources.PrepareCapture(in sourceSample, presentationDeltaSeconds);
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (!entry.IsSourcePose && entry.SourceId.Equals(request.SourceId))
                    m_EntrySourceCaptureIndices[i] = binding.SourceIndex;
            }
            return binding;
        }

        internal AnimationBlendPushResult PushPoseRequest(
            in AnimationPoseSampleRequest request,
            AnimationBlendTransitionPayload transition,
            bool executeAsHardCut)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!request.IsValid || !request.SourceId.IsValid ||
                request.SourceOwnerIndex < 0)
            {
                throw new ArgumentException("Resolved animation pose request is routed to the wrong Blend Stack.");
            }

            AnimationBlendPushResult result = Push(new AnimationBlendPushRequest(
                m_Slot.NodeId,
                request.SourceId,
                AnimationBlendTransitionEndpointKind.SourceOwner,
                request.SourceOwnerIndex,
                request.PresentationRequestSequence,
                transition,
                executeAsHardCut));
            m_SelectionUnavailable = false;
            return result;
        }

        internal void PushUnavailable(AnimationPlaybackId playbackId)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!playbackId.IsValid)
                throw new ArgumentException("Unavailable Blend Stack playback is invalid.", nameof(playbackId));
            m_SelectionUnavailable = true;
        }

        internal AnimationBlendPushResult PushSourcePose(
            ulong presentationRequestSequence,
            AnimationBlendTransitionPayload transition,
            bool executeAsHardCut)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            RequireTarget(
                default,
                AnimationBlendTransitionEndpointKind.SourcePose,
                -1,
                presentationRequestSequence);
            if (IsCurrentTarget(
                    default,
                    AnimationBlendTransitionEndpointKind.SourcePose,
                    -1))
            {
                ContinueCurrentTarget(presentationRequestSequence);
                m_SelectionUnavailable = false;
                return AnimationBlendPushResult.ContinuedSource;
            }
            AnimationBlendPushResult result = Push(new AnimationBlendPushRequest(
                m_Slot.NodeId,
                default,
                AnimationBlendTransitionEndpointKind.SourcePose,
                -1,
                presentationRequestSequence,
                transition,
                executeAsHardCut));
            m_SelectionUnavailable = false;
            return result;
        }

        internal void Advance(float deltaSeconds)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                entry.Advance(deltaSeconds);
                WriteEntry(i, entry);
            }
        }

        internal AnimationSlotBlendJob PrepareSlotJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            PhysicalPoseSourceRegistry physicalSources)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (physicalSources == null)
                throw new ArgumentNullException(nameof(physicalSources));
            if (completionIdentity == 0 || completionIdentity != m_SourceFrameCompletionIdentity ||
                finalWriteBinding.CompletionIdentity != completionIdentity ||
                finalWriteBinding.DenseLocalPoses.Length != m_Rig.PoseBoneCount)
            {
                throw new InvalidOperationException("Animation Blend Stack completion or final Slot layout is not current.");
            }

            AnimationBlendSourcePoseNativeReadBinding sourceBinding = m_Sources.RequireNativeReadBinding(completionIdentity);
            AnimationSlotBlendFramePlanKind kind;
            if (m_SelectionUnavailable)
            {
                kind = AnimationSlotBlendFramePlanKind.Unavailable;
                PrepareUnavailablePlan(in finalWriteBinding);
            }
            else
            {
                kind = ResolvePlanKind();
                ClearPlannedWeights();
                PrepareCrossFadePlan(in finalWriteBinding, physicalSources, kind);
            }

            AnimationSlotBlendPoseWorkspaceBinding workspaceBinding = m_SlotWorkspace.RequireActiveBinding();
            var job = new AnimationSlotBlendJob(workspaceBinding, sourceBinding);
            m_PendingPlanCompletionIdentity = completionIdentity;
            m_PendingPlanKind = kind;
            return job;
        }

        internal void PrepareCompletion(ulong completionIdentity)
        {
            RequireAlive();
            if (!m_FrameOpen || completionIdentity == 0 ||
                completionIdentity != m_PendingPlanCompletionIdentity ||
                completionIdentity != m_SourceFrameCompletionIdentity ||
                m_PreparedCompletionIdentity != 0)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack completion preparation is not current.");
            }
            RetireCompletedHistory();
            PublishPendingStackReleases(completionIdentity);
            m_PreparedCompletionIdentity = completionIdentity;
        }

        internal void CompleteFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 ||
                completionIdentity != m_PendingPlanCompletionIdentity ||
                completionIdentity != m_PreparedCompletionIdentity)
                throw new InvalidOperationException("Animation Blend Stack completion does not match its committed frame plan.");
            AnimationSlotBlendPoseWorkspaceBinding binding = m_SlotWorkspace.RequireActiveBinding();
            AnimationPlayerPoseNativeWriteBinding output = binding.FinalWriteBinding;
            if (output.CompletedAt[0] != completionIdentity)
                throw new InvalidOperationException("Animation Blend Stack job has not completed the requested frame.");

            AnimationPoseAvailability availability = output.Availability[0];
            AnimationPoseNativeInvalidReason invalidReason = output.InvalidReason[0];
            if (availability == AnimationPoseAvailability.Invalid || invalidReason != AnimationPoseNativeInvalidReason.None)
            {
                Debug.LogError(BuildInvalidFrameDiagnostic(completionIdentity, invalidReason));
                m_LastCompletionIdentity = completionIdentity;
                m_LastAvailability = AnimationPoseAvailability.Invalid;
                m_LastInvalidReason = invalidReason == AnimationPoseNativeInvalidReason.None
                    ? AnimationPoseNativeInvalidReason.SlotPoseInvalid
                    : invalidReason;
                m_HasCompletedFrame = true;
                m_PendingPlanCompletionIdentity = 0;
                m_SourceFrameCompletionIdentity = 0;
                m_PreparedCompletionIdentity = 0;
                return;
            }
            if (availability != AnimationPoseAvailability.Pose && availability != AnimationPoseAvailability.NoPose)
                throw new InvalidOperationException("Animation Blend Stack job published an unknown availability.");

            CacheCompletedOutput(in output);
            if (m_HasPendingStoredCapture)
                CommitPendingStoredCapture();

            m_LastCompletionIdentity = completionIdentity;
            m_LastAvailability = availability;
            m_LastInvalidReason = AnimationPoseNativeInvalidReason.None;
            m_HasCompletedFrame = true;
            m_PendingPlanCompletionIdentity = 0;
            m_SourceFrameCompletionIdentity = 0;
            m_PreparedCompletionIdentity = 0;
        }

        internal int PendingReleaseCount
        {
            get
            {
                RequireAlive();
                return m_StackReleaseCount;
            }
        }

        internal int PendingPriorFrameReleaseCount(
            ulong stagingCompletionIdentity)
        {
            RequireAlive();
            if (!m_FrameOpen ||
                stagingCompletionIdentity == 0 ||
                stagingCompletionIdentity != m_PreparedCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack release staging frame is not current.");
            }

            int count = 0;
            for (int i = 0; i < m_StackReleaseCount; i++)
            {
                AnimationBlendStackRelease release =
                    ReadStackRelease(
                        (m_StackReleaseHead + i) %
                        StackReleaseCapacity);
                if (release.CompletionIdentity >
                    stagingCompletionIdentity)
                {
                    throw new InvalidOperationException(
                        "Animation Blend Stack release was published by a future frame.");
                }
                if (release.CompletionIdentity ==
                    stagingCompletionIdentity)
                {
                    break;
                }
                count++;
            }
            return count;
        }

        internal AnimationBlendStackSourceReleaseToken PrepareRelease(
            int releaseOrdinal,
            ulong stagingCompletionIdentity)
        {
            RequireAlive();
            if (releaseOrdinal < 0 ||
                releaseOrdinal != m_PreparedSourceReleaseCount ||
                m_AppliedPreparedSourceReleaseCount != 0 ||
                m_StackReleaseCount == 0)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack release ordinal is not current.");
            }
            int queueIndex = m_StackReleaseHead;
            AnimationBlendStackRelease release =
                ReadStackRelease(queueIndex);
            bool preparedFrame = m_FrameOpen &&
                                 stagingCompletionIdentity == m_PreparedCompletionIdentity;
            bool resetFrame = !m_FrameOpen &&
                              stagingCompletionIdentity == m_LastCompletionIdentity;
            if (stagingCompletionIdentity == 0 ||
                !preparedFrame && !resetFrame ||
                release.PoseNodeId != PoseNodeId ||
                !release.SourceId.IsValid ||
                release.CompletionIdentity == 0 ||
                release.CompletionIdentity > stagingCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack source release is not valid for the completed staging frame.");
            }
            if (IsSourceReferenced(release.SourceId) ||
                ContainsPendingRelease(release.SourceId) ||
                HasDuplicateQueuedRelease(
                    release.SourceId,
                    queueIndex))
            {
                throw new InvalidOperationException(
                    "Animation source is not releasable by its Blend Stack.");
            }
            AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                m_Sources.PrepareRelease(release.SourceId);
            WriteStackRelease(queueIndex, default);
            m_StackReleaseHead =
                (m_StackReleaseHead + 1) % StackReleaseCapacity;
            m_StackReleaseCount--;
            m_PreparedSourceReleaseCount++;
            return new AnimationBlendStackSourceReleaseToken(
                releaseOrdinal,
                in release,
                in sourcePoseRelease);
        }

        internal void ApplyPreparedRelease(
            in AnimationBlendStackSourceReleaseToken token)
        {
            AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                token.SourcePoseRelease;
            m_Sources.ApplyPreparedRelease(in sourcePoseRelease);
            m_AppliedPreparedSourceReleaseCount++;
            if (m_AppliedPreparedSourceReleaseCount ==
                m_PreparedSourceReleaseCount)
            {
                m_PreparedSourceReleaseCount = 0;
                m_AppliedPreparedSourceReleaseCount = 0;
            }
        }

        internal void Reset(ulong completionIdentity)
        {
            RequireAlive();
            if (m_PreparedSourceReleaseCount != 0 ||
                m_AppliedPreparedSourceReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack prepared releases were not applied.");
            }
            if (m_PendingPlanCompletionIdentity != 0)
                throw new InvalidOperationException("Animation Blend Stack frame plan must complete before reset.");
            if (completionIdentity == 0 || completionIdentity < m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            int removedCount = CopyReferencedSourceIds(m_RemovedSourceIds);
            RequireReleaseCapacity(m_RemovedSourceIds, removedCount, default);
            RequireCanAdvanceContinuityIdentity();

            for (int i = 0; i < removedCount; i++)
                StageStackRelease(m_RemovedSourceIds[i]);
            ClearEntries();
            Fill(m_EntrySourceCaptureIndices, -1);
            ClearPendingCaptures();
            m_HasStoredPose = false;
            m_SelectionUnavailable = false;
            m_HasCompletedFrame = false;
            m_LastAvailability = default;
            m_LastInvalidReason = AnimationPoseNativeInvalidReason.None;
            m_LastOutputWeight = 0f;
            m_StoredOutputWeight = 0f;
            ClearLastBoneOutputWeights();
            ClearStoredBoneOutputWeights();
            m_Sources.ResetContinuity();
            m_SlotWorkspace.Reset();
            m_SourceFrameCompletionIdentity = 0;
            m_LastRequestSequence = 0;
            m_LastCompletionIdentity = completionIdentity;
            AdvanceContinuityIdentity();
            PublishPendingStackReleases(completionIdentity);
        }

        AnimationBlendPushResult Push(AnimationBlendPushRequest request)
        {
            RequireRequest(request);
            if (m_HasPendingStoredCapture)
            {
                if (IsCurrentTarget(
                        request.SourceId,
                        request.TargetEndpointKind,
                        request.SourceOwnerIndex))
                {
                    ContinueCurrentTarget(request.PresentationRequestSequence);
                    return AnimationBlendPushResult.ContinuedSource;
                }
                throw new InvalidOperationException("Animation Blend capture must complete before another target push.");
            }
            if (IsCurrentTarget(
                    request.SourceId,
                    request.TargetEndpointKind,
                    request.SourceOwnerIndex))
            {
                ContinueCurrentTarget(request.PresentationRequestSequence);
                return AnimationBlendPushResult.ContinuedSource;
            }
            if (request.PresentationRequestSequence <= m_LastRequestSequence)
                throw new InvalidOperationException("Animation Blend push request order is not strictly increasing.");
            if (m_AvailabilityPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection &&
                request.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
            {
                throw new InvalidOperationException(
                    $"Required Blend Stack '{m_Slot.NodeId}' cannot target Source Pose.");
            }

            AnimationBlendPushResult result = PushCrossFade(request);
            m_LastRequestSequence = request.PresentationRequestSequence;
            return result;
        }

        string BuildInvalidFrameDiagnostic(
            ulong completionIdentity,
            AnimationPoseNativeInvalidReason invalidReason)
        {
            var builder = new StringBuilder(512);
            builder.Append("Animation Blend Stack invalid")
                .Append(" Channel=").Append(m_AnimationChannelId)
                .Append(", Provider=").Append(m_PresentationPoseSourceProviderId)
                .Append(", Node=").Append(m_Slot.NodeId)
                .Append(", Completion=").Append(completionIdentity)
                .Append(", Reason=").Append(invalidReason)
                .Append(", PlanKind=").Append(m_PendingPlanKind)
                .Append(", SelectionUnavailable=").Append(m_SelectionUnavailable)
                .Append(", HasStored=").Append(m_HasStoredPose)
                .Append(", HasPendingStored=").Append(m_HasPendingStoredCapture)
                .Append(", StoredWeight=").Append(m_StoredOutputWeight)
                .Append(", PendingStoredWeight=").Append(m_PendingCaptureOutputWeight)
                .Append(", Entries=").Append(m_EntryCount);
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                builder.Append(" | #").Append(i)
                    .Append(" Id=").Append(entry.EntryId)
                    .Append(", Source=").Append(entry.SourceId)
                    .Append(", Owner=").Append(entry.SourceOwnerIndex)
                    .Append(", SourcePose=").Append(entry.IsSourcePose)
                    .Append(", Elapsed=").Append(entry.ElapsedSeconds)
                    .Append(", RawAlpha=").Append(m_EntryRawAlphas[i])
                    .Append(", EasedAlpha=").Append(m_EntryEasedAlphas[i])
                    .Append(", Weight=").Append(m_EntryScalarWeights[i])
                    .Append(", Capture=").Append(m_EntrySourceCaptureIndices[i]);
            }
            return builder.Append('.').ToString();
        }

        AnimationBlendPushResult PushCrossFade(AnimationBlendPushRequest request)
        {
            bool startsNewContinuity =
                                       request.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                                       (!m_HasCompletedFrame || m_LastAvailability != AnimationPoseAvailability.Pose);
            bool replaceHistory = m_EntryCount == EntryCapacity ||
                                  m_EntryCount > 0 && ReadEntry(m_EntryCount - 1).ElapsedSeconds <=
                                  m_MaxBlendInTimeToReplaceNewest;
            bool captureHistory =
                replaceHistory &&
                HasCapturableFrame();
            bool replaceSourcePoseHistory =
                replaceHistory &&
                !captureHistory &&
                CanReplaceSourcePoseHistoryWithoutCapture();
            int identityCount = captureHistory ? 2 : 1;
            RequireContributionIdentityCapacity(identityCount);
            if (startsNewContinuity)
                RequireCanAdvanceContinuityIdentity();

            int captureIndex = FindSourceCaptureIndex(request.SourceId);
            AnimationBlendEntryState newEntry = CreateEntry(request, AllocateContributionContinuityIdentity());
            if (replaceHistory)
            {
                if (!captureHistory &&
                    !replaceSourcePoseHistory)
                {
                    RequireCapturableFrame();
                }
                int removedCount = CopyEntrySourceIds(m_RemovedSourceIds);
                RequireReleaseCapacity(m_RemovedSourceIds, removedCount, request.SourceId);

                CancelStackRelease(request.SourceId);
                if (captureHistory)
                {
                    ulong storedIdentity =
                        AllocateContributionContinuityIdentity();
                    CapturePendingOutput(storedIdentity);
                }
                for (int i = 0; i < removedCount; i++)
                {
                    if (!m_RemovedSourceIds[i].Equals(request.SourceId))
                        StageStackRelease(m_RemovedSourceIds[i]);
                }
                ClearEntries();
                AddEntry(newEntry, captureIndex);
            }
            else
            {
                if (m_EntryCount == EntryCapacity)
                    throw new InvalidOperationException("Animation Blend Stack capacity was exceeded.");
                for (int i = 0; i < m_EntryCount; i++)
                {
                    m_CompactedEntries[i] = ReadEntry(i);
                    m_CompactedEntries[i].IncreasePushDepth(
                        m_DepthBlendTimeMultiplier);
                }
                for (int i = 0; i < m_EntryCount; i++)
                    WriteEntry(i, m_CompactedEntries[i]);
                Array.Clear(m_CompactedEntries, 0, m_EntryCount);
                CancelStackRelease(request.SourceId);
                AddEntry(newEntry, captureIndex);
            }
            if (startsNewContinuity)
                AdvanceContinuityIdentity();
            return captureHistory
                ? AnimationBlendPushResult.CapturedStoredPose
                : AnimationBlendPushResult.Pushed;
        }

        void PrepareCrossFadePlan(
            in AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            PhysicalPoseSourceRegistry physicalSources,
            AnimationSlotBlendFramePlanKind kind)
        {
            bool capturesStored = kind == AnimationSlotBlendFramePlanKind.StoredCapture;
            bool usesStored = capturesStored || m_HasStoredPose;
            float storedOutputWeight = capturesStored ? m_PendingCaptureOutputWeight : m_StoredOutputWeight;
            float[] storedBoneWeights = capturesStored ? m_PendingCaptureBoneOutputWeights : m_StoredBoneOutputWeights;

            float scalarResidual = 1f;
            for (int i = m_EntryCount - 1; i >= 0; i--)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                AnimationBlendProfilePayload profile = m_ProfileCatalog.Require(entry.BlendProfileIndex);
                float rawAlpha = entry.GetOutputNormalizedTime(profile);
                float alpha = AnimationBlendCurveEvaluator.Evaluate(
                    m_CurveCatalog.Require(entry.CanonicalCurveIndex),
                    rawAlpha);
                RequireNormalized(alpha);
                m_EntryRawAlphas[i] = rawAlpha;
                m_EntryEasedAlphas[i] = alpha;
                m_EntryScalarWeights[i] = scalarResidual * alpha;
                m_PlannedEntryMaximumWeights[i] = m_EntryScalarWeights[i];
                scalarResidual *= 1f - alpha;
            }

            float storedScalarWeight = usesStored ? scalarResidual * storedOutputWeight : 0f;
            float outputWeight = storedScalarWeight;
            m_PlannedStoredMaximumWeight = storedScalarWeight;
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!ReadEntry(i).IsSourcePose)
                    outputWeight += m_EntryScalarWeights[i];
            }
            RequireNormalized(outputWeight);

            bool hasDenseOutput = false;
            for (int boneIndex = 0; boneIndex < m_Rig.PoseBoneCount; boneIndex++)
            {
                float residual = 1f;
                float boneOutputWeight = 0f;
                for (int i = m_EntryCount - 1; i >= 0; i--)
                {
                    AnimationBlendEntryState entry = ReadEntry(i);
                    float alpha = entry.EvaluateBoneAlpha(
                        boneIndex,
                        m_CurveCatalog.Require(entry.CanonicalCurveIndex),
                        m_ProfileCatalog.Require(entry.BlendProfileIndex));
                    RequireNormalized(alpha);
                    float weight = residual * alpha;
                    m_EntryBoneWeights[i * m_Rig.PoseBoneCount + boneIndex] = weight;
                    m_PlannedEntryMaximumWeights[i] = Mathf.Max(m_PlannedEntryMaximumWeights[i], weight);
                    if (!entry.IsSourcePose)
                        boneOutputWeight += weight;
                    residual *= 1f - alpha;
                }
                float storedWeight = usesStored ? residual * storedBoneWeights[boneIndex] : 0f;
                boneOutputWeight += storedWeight;
                RequireNormalized(boneOutputWeight);
                hasDenseOutput |= boneOutputWeight > 0f;
                m_PlannedStoredMaximumWeight = Mathf.Max(m_PlannedStoredMaximumWeight, storedWeight);
            }

            AnimationPoseAvailability availability = outputWeight > 0f || hasDenseOutput
                ? AnimationPoseAvailability.Pose
                : AnimationPoseAvailability.NoPose;
            if (availability == AnimationPoseAvailability.NoPose && m_AvailabilityPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection)
                throw new InvalidOperationException("Required Blend Stack has no CrossFade output.");

            int contributionCount = availability == AnimationPoseAvailability.Pose
                ? CountCrossFadeContributions(usesStored)
                : 0;
            ulong historyCompletion = m_HasCompletedFrame && m_LastAvailability == AnimationPoseAvailability.Pose
                ? m_LastCompletionIdentity
                : 0;
            if (capturesStored && historyCompletion == 0)
                throw new InvalidOperationException("Stored Pose capture requires a completed Pose history frame.");

            AnimationSlotBlendFramePlanPreparation preparation = m_SlotWorkspace.PrepareInactivePage(
                in finalWriteBinding,
                availability == AnimationPoseAvailability.NoPose
                    ? AnimationSlotBlendFramePlanKind.CrossFade
                    : kind,
                m_AvailabilityPolicy,
                m_Rig.ScalePolicy,
                availability,
                AnimationPoseNativeInvalidReason.None,
                outputWeight,
                contributionCount,
                m_ContinuityIdentity,
                historyCompletion);
            try
            {
                if (availability == AnimationPoseAvailability.Pose)
                    WriteCrossFadePlan(preparation, physicalSources, usesStored, capturesStored, storedScalarWeight, storedBoneWeights);
                m_SlotWorkspace.ValidateInactivePage(preparation);
                m_SlotWorkspace.CommitInactivePage(preparation);
            }
            catch
            {
                m_SlotWorkspace.AbortInactivePage(preparation);
                throw;
            }
        }

        void PrepareUnavailablePlan(in AnimationPlayerPoseNativeWriteBinding finalWriteBinding)
        {
            AnimationSlotBlendFramePlanPreparation preparation = m_SlotWorkspace.PrepareInactivePage(
                in finalWriteBinding,
                AnimationSlotBlendFramePlanKind.Unavailable,
                m_AvailabilityPolicy,
                m_Rig.ScalePolicy,
                AnimationPoseAvailability.Invalid,
                AnimationPoseNativeInvalidReason.RequiredPoseMissing,
                0f,
                0,
                m_ContinuityIdentity,
                0);
            try
            {
                m_SlotWorkspace.ValidateInactivePage(preparation);
                m_SlotWorkspace.CommitInactivePage(preparation);
            }
            catch
            {
                m_SlotWorkspace.AbortInactivePage(preparation);
                throw;
            }
        }

        void WriteCrossFadePlan(
            AnimationSlotBlendFramePlanPreparation preparation,
            PhysicalPoseSourceRegistry physicalSources,
            bool usesStored,
            bool capturesStored,
            float storedScalarWeight,
            float[] storedBoneWeights)
        {
            int contributionIndex = 0;
            if (usesStored)
            {
                ulong storedIdentity = capturesStored
                    ? m_PendingStoredContributionIdentity
                    : RequireStoredContributionIdentity();
                m_SlotWorkspace.SetPreparedEntry(
                    preparation,
                    contributionIndex,
                    new AnimationSlotBlendFramePlanEntry(
                        -1,
                        -1,
                        0,
                        AnimationPoseContributionKind.Stored,
                        -1,
                        storedIdentity,
                        storedScalarWeight,
                        GetStoredResidualForBone(m_Rig.LeftLeg.AnklePhysicalBoneIndex) * storedBoneWeights[m_Rig.LeftLeg.AnklePhysicalBoneIndex],
                        GetStoredResidualForBone(m_Rig.RightLeg.AnklePhysicalBoneIndex) * storedBoneWeights[m_Rig.RightLeg.AnklePhysicalBoneIndex]));
                for (int boneIndex = 0; boneIndex < m_Rig.PoseBoneCount; boneIndex++)
                {
                    float residual = GetStoredResidualForBone(boneIndex);
                    m_SlotWorkspace.SetPreparedDenseBoneWeight(
                        preparation,
                        contributionIndex,
                        boneIndex,
                        residual * storedBoneWeights[boneIndex]);
                }
                contributionIndex++;
            }

            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (entry.IsSourcePose)
                    continue;
                int captureIndex = RequireSourceCaptureIndex(i);
                AnimationPhysicalSourceIdentity physical = RequirePhysicalSource(physicalSources, entry);
                m_SlotWorkspace.SetPreparedEntry(
                    preparation,
                    contributionIndex,
                    new AnimationSlotBlendFramePlanEntry(
                        captureIndex,
                        physical.Index.Value,
                        physical.Generation,
                        AnimationPoseContributionKind.Live,
                        entry.SourceOwnerIndex,
                        entry.ContributionContinuityIdentity,
                        m_EntryScalarWeights[i],
                        m_EntryBoneWeights[i * m_Rig.PoseBoneCount + m_Rig.LeftLeg.AnklePhysicalBoneIndex],
                        m_EntryBoneWeights[i * m_Rig.PoseBoneCount + m_Rig.RightLeg.AnklePhysicalBoneIndex]));
                for (int boneIndex = 0; boneIndex < m_Rig.PoseBoneCount; boneIndex++)
                {
                    m_SlotWorkspace.SetPreparedDenseBoneWeight(
                        preparation,
                        contributionIndex,
                        boneIndex,
                        m_EntryBoneWeights[i * m_Rig.PoseBoneCount + boneIndex]);
                }
                contributionIndex++;
            }
        }

        void CacheCompletedOutput(in AnimationPlayerPoseNativeWriteBinding output)
        {
            float outputWeight = output.OutputWeight[0];
            if (!float.IsFinite(outputWeight) || outputWeight < 0f || outputWeight > 1f)
                throw new InvalidOperationException("Animation Blend Stack output weight is invalid.");
            int contributionCount = output.ContributionCount[0];
            if (contributionCount < 0 || contributionCount > output.Range.ContributionCapacity)
                throw new InvalidOperationException("Animation Blend Stack contribution count is invalid.");
            m_PendingLastBoneOutputWritten = true;
            for (int boneIndex = 0; boneIndex < m_PendingLastBoneOutputWeights.Length; boneIndex++)
            {
                float weight = 0f;
                for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                    weight += output.DenseContributionWeights[contributionIndex * m_PendingLastBoneOutputWeights.Length + boneIndex];
                RequireNormalized(weight);
                m_PendingLastBoneOutputWeights[boneIndex] = weight;
            }
            m_LastOutputWeight = outputWeight;
        }

        void CommitPendingStoredCapture()
        {
            if (m_PendingPlanKind != AnimationSlotBlendFramePlanKind.StoredCapture)
                throw new InvalidOperationException("Stored Pose capture completed with the wrong frame plan kind.");
            m_StoredOutputWeight = m_PendingCaptureOutputWeight;
            Array.Copy(m_PendingCaptureBoneOutputWeights, m_PendingStoredBoneOutputWeights, m_PendingStoredBoneOutputWeights.Length);
            m_PendingStoredBoneOutputWritten = true;
            m_HasStoredPose = m_PlannedStoredMaximumWeight > 0f;
            m_HasPendingStoredCapture = false;
            m_PendingStoredContributionIdentity = 0;
            ClearPendingCaptureOutput();
        }

        void RetireCompletedHistory()
        {
            if (m_HasStoredPose && m_PlannedStoredMaximumWeight <= 0f)
                m_HasStoredPose = false;
            if (m_EntryCount <= 1)
                return;

            int keptCount = 0;
            int removedCount = 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                bool keep = i == m_EntryCount - 1 || m_PlannedEntryMaximumWeights[i] > 0f;
                if (keep)
                {
                    m_CompactedEntries[keptCount] = ReadEntry(i);
                    m_CompactedSourceCaptureIndices[keptCount] = m_EntrySourceCaptureIndices[i];
                    keptCount++;
                }
                else if (!ReadEntry(i).IsSourcePose)
                {
                    removedCount = AppendUniqueSourceId(m_RemovedSourceIds, removedCount, ReadEntry(i).SourceId);
                }
            }
            RequireReleaseCapacity(m_RemovedSourceIds, removedCount, default);
            ClearEntries();
            for (int i = 0; i < keptCount; i++)
                WriteEntry(i, m_CompactedEntries[i]);
            Array.Copy(m_CompactedSourceCaptureIndices, 0, m_EntrySourceCaptureIndices, 0, keptCount);
            Array.Clear(m_CompactedEntries, 0, keptCount);
            Fill(m_CompactedSourceCaptureIndices, -1);
            m_EntryCount = keptCount;
            for (int i = 0; i < removedCount; i++)
            {
                if (!IsSourceReferenced(m_RemovedSourceIds[i]))
                    StageStackRelease(m_RemovedSourceIds[i]);
            }
        }

        AnimationBlendEntryState CreateEntry(
            AnimationBlendPushRequest request,
            ulong contributionContinuityIdentity)
        {
            return new AnimationBlendEntryState(
                new AnimationBlendEntryId(
                    m_Slot.NodeId,
                    request.SourceId,
                    request.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose,
                    request.PresentationRequestSequence),
                request.SourceOwnerIndex,
                request.ExecuteAsHardCut ? 0f : request.Transition.DurationSeconds,
                request.Transition.CurveIndex,
                request.Transition.BlendProfileIndex,
                contributionContinuityIdentity);
        }

        void AddEntry(AnimationBlendEntryState entry, int sourceCaptureIndex)
        {
            if (m_EntryCount == EntryCapacity)
                throw new InvalidOperationException("Animation Blend Stack capacity was exceeded without Stored Pose capture.");
            WriteEntry(m_EntryCount, entry);
            m_EntrySourceCaptureIndices[m_EntryCount] = entry.IsSourcePose ? -1 : sourceCaptureIndex;
            m_EntryCount++;
        }

        void RequireRequest(AnimationBlendPushRequest request)
        {
            if (request.PoseNodeId != m_Slot.NodeId)
                throw new InvalidOperationException("Animation Blend push was routed to the wrong node.");
            RequireTarget(
                request.SourceId,
                request.TargetEndpointKind,
                request.SourceOwnerIndex,
                request.PresentationRequestSequence);
            GetCurrentEndpoint(
                out int sourceOwnerIndex,
                out AnimationBlendTransitionEndpointKind sourceEndpointKind);
            AnimationBlendTransitionPayload exact = m_Slot.RequireTransition(
                sourceOwnerIndex,
                sourceEndpointKind,
                request.SourceOwnerIndex,
                request.TargetEndpointKind);
            if (!ReferenceEquals(exact, request.Transition) ||
                exact.GetIdentity(m_Slot.NodeId) != request.Transition.GetIdentity(m_Slot.NodeId))
            {
                throw new InvalidOperationException("Animation Blend push did not use the compiled exact transition.");
            }
        }

        void RequireTarget(
            AnimationPoseSourceId sourceId,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            int sourceOwnerIndex,
            ulong presentationRequestSequence)
        {
            bool sourceOwner =
                targetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner;
            if (presentationRequestSequence == 0 ||
                !Enum.IsDefined(
                    typeof(AnimationBlendTransitionEndpointKind),
                    targetEndpointKind) ||
                targetEndpointKind == AnimationBlendTransitionEndpointKind.NoPose ||
                sourceOwner != sourceId.IsValid ||
                sourceOwner != (sourceOwnerIndex >= 0) ||
                targetEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose &&
                !m_AnimationChannelId.IsValid)
            {
                throw new ArgumentException("Animation Blend target identity is invalid.");
            }
        }

        bool IsCurrentTarget(
            AnimationPoseSourceId sourceId,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            int sourceOwnerIndex)
        {
            if (m_EntryCount == 0)
            {
                return targetEndpointKind ==
                       (m_AnimationChannelId.IsValid
                           ? AnimationBlendTransitionEndpointKind.SourcePose
                           : AnimationBlendTransitionEndpointKind.NoPose);
            }
            AnimationBlendEntryState current = ReadEntry(m_EntryCount - 1);
            bool sourcePose =
                targetEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose;
            return current.IsSourcePose == sourcePose &&
                   current.SourceOwnerIndex == sourceOwnerIndex &&
                   (sourcePose || current.SourceId.Equals(sourceId));
        }

        void GetCurrentEndpoint(
            out int sourceOwnerIndex,
            out AnimationBlendTransitionEndpointKind endpointKind)
        {
            if (m_EntryCount == 0)
            {
                sourceOwnerIndex = -1;
                endpointKind = m_AnimationChannelId.IsValid
                    ? AnimationBlendTransitionEndpointKind.SourcePose
                    : AnimationBlendTransitionEndpointKind.NoPose;
                return;
            }
            AnimationBlendEntryState current = ReadEntry(m_EntryCount - 1);
            sourceOwnerIndex = current.SourceOwnerIndex;
            endpointKind = current.IsSourcePose
                ? AnimationBlendTransitionEndpointKind.SourcePose
                : AnimationBlendTransitionEndpointKind.SourceOwner;
        }

        AnimationSlotBlendFramePlanKind ResolvePlanKind() =>
            m_HasPendingStoredCapture
                ? AnimationSlotBlendFramePlanKind.StoredCapture
                : AnimationSlotBlendFramePlanKind.CrossFade;

        void CapturePendingOutput(ulong storedContributionIdentity)
        {
            m_PendingCaptureOutputWeight = m_LastOutputWeight;
            Array.Copy(m_LastBoneOutputWeights, m_PendingCaptureBoneOutputWeights, m_LastBoneOutputWeights.Length);
            m_PendingStoredContributionIdentity = storedContributionIdentity;
            m_HasPendingStoredCapture = true;
        }

        void RequireCapturableFrame()
        {
            if (!HasCapturableFrame())
            {
                throw new InvalidOperationException("Animation Blend Stack has no completed Pose frame to capture.");
            }
        }

        bool HasCapturableFrame() =>
            m_HasCompletedFrame &&
            m_LastAvailability ==
            AnimationPoseAvailability.Pose &&
            m_LastCompletionIdentity != 0;

        bool CanReplaceSourcePoseHistoryWithoutCapture() =>
            m_EntryCount > 0 &&
            ReadEntry(m_EntryCount - 1).IsSourcePose &&
            !m_HasStoredPose &&
            !m_HasPendingStoredCapture &&
            (!m_HasCompletedFrame ||
             m_LastAvailability ==
             AnimationPoseAvailability.NoPose);

        AnimationPhysicalSourceIdentity RequirePhysicalSource(
            PhysicalPoseSourceRegistry physicalSources,
            AnimationBlendEntryState entry)
        {
            AnimationPhysicalSourceIdentity identity = physicalSources.RequireIdentity(entry.SourceId, m_Slot.NodeId);
            if (physicalSources.RequirePoseNodeId(identity) != m_Slot.NodeId ||
                physicalSources.RequireSourceOwnerIndex(identity) != entry.SourceOwnerIndex)
            {
                throw new InvalidOperationException("Animation physical source is routed to the wrong Blend Stack entry.");
            }
            return identity;
        }

        int RequireSourceCaptureIndex(int entryIndex)
        {
            int index = m_EntrySourceCaptureIndices[entryIndex];
            if (index < 0)
                throw new InvalidOperationException($"Animation Blend source entry #{entryIndex} has no prepared capture index.");
            return index;
        }

        int FindSourceCaptureIndex(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                return -1;
            for (int i = m_EntryCount - 1; i >= 0; i--)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (!entry.IsSourcePose && entry.SourceId.Equals(sourceId))
                    return m_EntrySourceCaptureIndices[i];
            }
            return -1;
        }

        int CountCrossFadeContributions(bool usesStored)
        {
            int count = usesStored ? 1 : 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!ReadEntry(i).IsSourcePose)
                    count++;
            }
            return count;
        }

        float GetStoredResidualForBone(int boneIndex)
        {
            float residual = 1f;
            for (int i = m_EntryCount - 1; i >= 0; i--)
                residual -= m_EntryBoneWeights[i * m_Rig.PoseBoneCount + boneIndex];
            if (residual < 0f && residual > -0.0001f)
                residual = 0f;
            RequireNormalized(residual);
            return residual;
        }

        ulong RequireStoredContributionIdentity()
        {
            return RequireStoredState().ContributionContinuityIdentity;
        }

        AnimationSlotBlendStoredPoseNativeState RequireStoredState()
        {
            AnimationSlotBlendPoseWorkspaceBinding binding = m_SlotWorkspace.RequireActiveBinding();
            AnimationSlotBlendStoredPoseNativeState state = binding.StoredPose.State[0];
            if (state.Active != 1 || state.ContributionContinuityIdentity == 0)
                throw new InvalidOperationException("Animation Stored Pose Native state is unavailable.");
            return state;
        }

        int CopyEntrySourceIds(AnimationPoseSourceId[] destination)
        {
            int count = 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (!entry.IsSourcePose)
                    count = AppendUniqueSourceId(destination, count, entry.SourceId);
            }
            return count;
        }

        int CopyReferencedSourceIds(AnimationPoseSourceId[] destination) =>
            CopyEntrySourceIds(destination);

        static int AppendUniqueSourceId(
            AnimationPoseSourceId[] destination,
            int count,
            AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                return count;
            for (int i = 0; i < count; i++)
            {
                if (destination[i].Equals(sourceId))
                    return count;
            }
            if (count == destination.Length)
                throw new InvalidOperationException("Animation Blend source reference workspace was exceeded.");
            destination[count] = sourceId;
            return count + 1;
        }

        bool IsSourceReferenced(AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_EntryCount; i++)
            {
                AnimationBlendEntryState entry = ReadEntry(i);
                if (!entry.IsSourcePose && entry.SourceId.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void StageStackRelease(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid || ContainsPendingRelease(sourceId) || ContainsQueuedRelease(sourceId))
                return;
            if (m_PendingStackReleaseCount == m_PendingStackReleaseSourceIds.Length)
                throw new InvalidOperationException("Animation Blend pending source release capacity was exceeded.");
            m_PendingStackReleaseSourceIds[m_PendingStackReleaseCount++] = sourceId;
        }

        void PublishPendingStackReleases(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            for (int i = 0; i < m_PendingStackReleaseCount; i++)
            {
                AnimationPoseSourceId sourceId = m_PendingStackReleaseSourceIds[i];
                if (!sourceId.IsValid || IsSourceReferenced(sourceId) || ContainsQueuedRelease(sourceId))
                    continue;
                if (m_StackReleaseCount == StackReleaseCapacity)
                    throw new InvalidOperationException("Animation Blend source retirement queue was not drained.");
                int tail = (m_StackReleaseHead + m_StackReleaseCount) % StackReleaseCapacity;
                WriteStackRelease(tail, new AnimationBlendStackRelease(
                    m_Slot.NodeId,
                    sourceId,
                    completionIdentity));
                m_StackReleaseCount++;
            }
            Array.Clear(m_PendingStackReleaseSourceIds, 0, m_PendingStackReleaseCount);
            m_PendingStackReleaseCount = 0;
        }

        void CancelStackRelease(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                return;
            for (int i = 0; i < m_PendingStackReleaseCount; i++)
            {
                if (!m_PendingStackReleaseSourceIds[i].Equals(sourceId))
                    continue;
                for (int shift = i; shift + 1 < m_PendingStackReleaseCount; shift++)
                    m_PendingStackReleaseSourceIds[shift] = m_PendingStackReleaseSourceIds[shift + 1];
                m_PendingStackReleaseSourceIds[--m_PendingStackReleaseCount] = default;
                break;
            }
            for (int i = 0; i < m_StackReleaseCount; i++)
            {
                int index = (m_StackReleaseHead + i) % StackReleaseCapacity;
                if (!ReadStackRelease(index).SourceId.Equals(sourceId))
                    continue;
                for (int shift = i; shift + 1 < m_StackReleaseCount; shift++)
                {
                    int destination = (m_StackReleaseHead + shift) % StackReleaseCapacity;
                    int source = (m_StackReleaseHead + shift + 1) % StackReleaseCapacity;
                    WriteStackRelease(destination, ReadStackRelease(source));
                }
                int tail = (m_StackReleaseHead + m_StackReleaseCount - 1) % StackReleaseCapacity;
                WriteStackRelease(tail, default);
                m_StackReleaseCount--;
                break;
            }
        }

        void RequireReleaseCapacity(
            AnimationPoseSourceId[] sourceIds,
            int count,
            AnimationPoseSourceId retainedSourceId)
        {
            int pending = 0;
            int queued = 0;
            for (int i = 0; i < m_PendingStackReleaseCount; i++)
            {
                if (!m_PendingStackReleaseSourceIds[i].Equals(retainedSourceId))
                    pending++;
            }
            for (int i = 0; i < m_StackReleaseCount; i++)
            {
                int index = (m_StackReleaseHead + i) % StackReleaseCapacity;
                if (!ReadStackRelease(index).SourceId.Equals(retainedSourceId))
                    queued++;
            }
            for (int i = 0; i < count; i++)
            {
                AnimationPoseSourceId sourceId = sourceIds[i];
                if (!sourceId.IsValid || sourceId.Equals(retainedSourceId) ||
                    ContainsPendingReleaseExcept(sourceId, retainedSourceId) ||
                    ContainsQueuedReleaseExcept(sourceId, retainedSourceId))
                {
                    continue;
                }
                pending++;
            }
            if (pending > m_PendingStackReleaseSourceIds.Length || pending + queued > StackReleaseCapacity)
                throw new InvalidOperationException("Animation Blend source retirement queue was not drained.");
        }

        bool ContainsPendingRelease(AnimationPoseSourceId sourceId) =>
            ContainsPendingReleaseExcept(sourceId, default);

        bool ContainsPendingReleaseExcept(AnimationPoseSourceId sourceId, AnimationPoseSourceId ignored)
        {
            for (int i = 0; i < m_PendingStackReleaseCount; i++)
            {
                AnimationPoseSourceId candidate = m_PendingStackReleaseSourceIds[i];
                if (!candidate.Equals(ignored) && candidate.Equals(sourceId))
                    return true;
            }
            return false;
        }

        bool ContainsQueuedRelease(AnimationPoseSourceId sourceId) =>
            ContainsQueuedReleaseExcept(sourceId, default);

        bool ContainsQueuedReleaseExcept(AnimationPoseSourceId sourceId, AnimationPoseSourceId ignored)
        {
            for (int i = 0; i < m_StackReleaseCount; i++)
            {
                AnimationPoseSourceId candidate = ReadStackRelease((m_StackReleaseHead + i) % StackReleaseCapacity).SourceId;
                if (!candidate.Equals(ignored) && candidate.Equals(sourceId))
                    return true;
            }
            return false;
        }

        bool HasDuplicateQueuedRelease(
            AnimationPoseSourceId sourceId,
            int currentQueueIndex)
        {
            for (int i = 0; i < m_StackReleaseCount; i++)
            {
                int index =
                    (m_StackReleaseHead + i) % StackReleaseCapacity;
                if (index != currentQueueIndex &&
                    ReadStackRelease(index).SourceId.Equals(sourceId))
                {
                    return true;
                }
            }
            return false;
        }

        AnimationBlendEntryState ReadEntry(int index)
        {
            if ((uint)index >= (uint)EntryCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_FrameOpen && m_PendingEntryVersions[index] == m_PendingEntryVersion
                ? m_PendingEntries[index]
                : m_CommittedEntries[index];
        }

        void WriteEntry(int index, AnimationBlendEntryState entry)
        {
            if ((uint)index >= (uint)EntryCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!m_FrameOpen)
            {
                m_CommittedEntries[index] = entry;
                return;
            }
            if (m_PendingEntryVersions[index] != m_PendingEntryVersion)
                m_PendingEntryDirtyIndices[m_PendingEntryDirtyCount++] = index;
            m_PendingEntries[index] = entry;
            m_PendingEntryVersions[index] = m_PendingEntryVersion;
        }

        void CommitPendingEntries()
        {
            for (int i = 0; i < m_PendingEntryDirtyCount; i++)
            {
                int index = m_PendingEntryDirtyIndices[i];
                m_CommittedEntries[index] = m_PendingEntries[index];
            }
            m_PendingEntryDirtyCount = 0;
        }

        AnimationBlendStackRelease ReadStackRelease(int index)
        {
            if ((uint)index >= (uint)StackReleaseCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_FrameOpen &&
                   m_PendingStackReleaseVersions[index] == m_PendingStackReleaseVersion
                ? m_PendingStackReleases[index]
                : m_CommittedStackReleases[index];
        }

        void WriteStackRelease(int index, AnimationBlendStackRelease release)
        {
            if ((uint)index >= (uint)StackReleaseCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!m_FrameOpen)
            {
                m_CommittedStackReleases[index] = release;
                return;
            }
            if (m_PendingStackReleaseVersions[index] != m_PendingStackReleaseVersion)
                m_PendingStackReleaseDirtyIndices[m_PendingStackReleaseDirtyCount++] = index;
            m_PendingStackReleases[index] = release;
            m_PendingStackReleaseVersions[index] = m_PendingStackReleaseVersion;
        }

        void CommitPendingStackReleases()
        {
            for (int i = 0; i < m_PendingStackReleaseDirtyCount; i++)
            {
                int index = m_PendingStackReleaseDirtyIndices[i];
                m_CommittedStackReleases[index] = m_PendingStackReleases[index];
            }
            m_PendingStackReleaseDirtyCount = 0;
        }

        void ClearPendingStackReleaseSources()
        {
            Array.Clear(
                m_PendingStackReleaseSourceIds,
                0,
                m_PendingStackReleaseCount);
            m_PendingStackReleaseCount = 0;
        }

        void ClearLastBoneOutputWeights()
        {
            float[] destination = m_FrameOpen
                ? m_PendingLastBoneOutputWeights
                : m_CommittedLastBoneOutputWeights;
            Array.Clear(destination, 0, destination.Length);
            if (m_FrameOpen)
                m_PendingLastBoneOutputWritten = true;
        }

        void ClearStoredBoneOutputWeights()
        {
            float[] destination = m_FrameOpen
                ? m_PendingStoredBoneOutputWeights
                : m_CommittedStoredBoneOutputWeights;
            Array.Clear(destination, 0, destination.Length);
            if (m_FrameOpen)
                m_PendingStoredBoneOutputWritten = true;
        }

        void ClearEntries()
        {
            Fill(m_EntrySourceCaptureIndices, -1);
            m_EntryCount = 0;
        }

        void ClearPendingCaptures()
        {
            m_HasPendingStoredCapture = false;
            m_PendingStoredContributionIdentity = 0;
            ClearPendingCaptureOutput();
        }

        void ClearPendingCaptureOutput()
        {
            m_PendingCaptureOutputWeight = 0f;
            Array.Clear(m_PendingCaptureBoneOutputWeights, 0, m_PendingCaptureBoneOutputWeights.Length);
        }

        void ClearPlannedWeights()
        {
            Array.Clear(m_EntryScalarWeights, 0, m_EntryScalarWeights.Length);
            Array.Clear(m_EntryRawAlphas, 0, m_EntryRawAlphas.Length);
            Array.Clear(m_EntryEasedAlphas, 0, m_EntryEasedAlphas.Length);
            Array.Clear(m_EntryBoneWeights, 0, m_EntryBoneWeights.Length);
            Array.Clear(m_PlannedEntryMaximumWeights, 0, m_PlannedEntryMaximumWeights.Length);
            m_PlannedStoredMaximumWeight = 0f;
        }

        void RequireNoPreparedPlan()
        {
            if (m_PendingPlanCompletionIdentity != 0)
                throw new InvalidOperationException("Animation Blend Stack frame plan must complete before state changes.");
            if (m_HasCompletedFrame && m_LastAvailability == AnimationPoseAvailability.Invalid &&
                m_LastInvalidReason != AnimationPoseNativeInvalidReason.RequiredPoseMissing &&
                m_LastInvalidReason != AnimationPoseNativeInvalidReason.SourceIncomplete)
                throw new InvalidOperationException("Animation Blend Stack is Invalid and must be reset.");
        }

        void ContinueCurrentTarget(ulong presentationRequestSequence)
        {
            if (presentationRequestSequence < m_LastRequestSequence)
                throw new InvalidOperationException("Animation Blend continuation request is stale.");
            m_LastRequestSequence = presentationRequestSequence;
        }

        void AdvanceContinuityIdentity()
        {
            RequireCanAdvanceContinuityIdentity();
            m_ContinuityIdentity =
                m_NextContinuityIdentity++;
        }

        void RequireCanAdvanceContinuityIdentity()
        {
            if (m_NextContinuityIdentity ==
                ulong.MaxValue)
                throw new InvalidOperationException("Animation Blend slot continuity identity overflowed.");
        }

        void RequireContributionIdentityCapacity(int count)
        {
            if (count <= 0 || m_LastContributionContinuityIdentity > ulong.MaxValue - (ulong)count)
                throw new InvalidOperationException("Animation contribution continuity identity overflowed.");
        }

        ulong AllocateContributionContinuityIdentity()
        {
            if (m_LastContributionContinuityIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation contribution continuity identity overflowed.");
            return ++m_LastContributionContinuityIdentity;
        }

        static void RequireNormalized(float value)
        {
            if (!float.IsFinite(value) || value < 0f || value > 1f)
                throw new InvalidOperationException("Animation Blend weight is outside [0, 1].");
        }

        static void Fill(int[] values, int value)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = value;
        }

        static void AdvanceVersion(ref uint version, uint[] versions)
        {
            version++;
            if (version != 0)
                return;
            Array.Clear(versions, 0, versions.Length);
            version = 1;
        }

        static void Swap<T>(ref T[] left, ref T[] right)
        {
            T[] temporary = left;
            left = right;
            right = temporary;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationBlendStackRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_SlotWorkspace.Dispose();
            m_Sources.Dispose();
            Array.Clear(m_CommittedEntries, 0, m_CommittedEntries.Length);
            Array.Clear(m_PendingEntries, 0, m_PendingEntries.Length);
            Array.Clear(m_PendingStackReleaseSourceIds, 0, m_PendingStackReleaseSourceIds.Length);
            Array.Clear(m_CommittedStackReleases, 0, m_CommittedStackReleases.Length);
            Array.Clear(m_PendingStackReleases, 0, m_PendingStackReleases.Length);
            m_EntryCount = 0;
            m_PendingStackReleaseCount = 0;
            m_PreparedSourceReleaseCount = 0;
            m_AppliedPreparedSourceReleaseCount = 0;
            m_StackReleaseHead = 0;
            m_StackReleaseCount = 0;
            m_Disposed = true;
        }
    }
}
