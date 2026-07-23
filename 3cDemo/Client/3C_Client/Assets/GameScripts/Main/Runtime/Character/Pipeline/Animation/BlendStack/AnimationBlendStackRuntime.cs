using System;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendStackRuntime : IDisposable
    {
        readonly AnimationBlendNodePayload m_Slot;
        readonly AnimationChannelId m_AnimationChannelId;
        readonly AnimationSelectionAvailabilityPolicy m_AvailabilityPolicy;
        readonly AnimationBlendCurveCatalogPayload m_CurveCatalog;
        readonly AnimationBlendProfileCatalogPayload m_ProfileCatalog;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly AnimationBlendEntryState[] m_Entries;
        readonly AnimationBlendEntryState[] m_CompactedEntries;
        readonly int[] m_EntrySourceCaptureIndices;
        readonly int[] m_CompactedSourceCaptureIndices;
        readonly float[] m_EntryRawAlphas;
        readonly float[] m_EntryEasedAlphas;
        readonly float[] m_EntryScalarWeights;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_PlannedEntryMaximumWeights;
        readonly float[] m_StoredBoneOutputWeights;
        readonly float[] m_PendingCaptureBoneOutputWeights;
        readonly float[] m_LastBoneOutputWeights;
        readonly AnimationPoseSourceId[] m_RemovedSourceIds;
        readonly AnimationPoseSourceId[] m_PendingStackReleaseSourceIds;
        readonly AnimationBlendStackRelease[] m_StackReleases;
        readonly AnimationBlendSourcePoseWorkspace m_Sources;
        readonly AnimationSlotBlendPoseWorkspace m_SlotWorkspace;

        int m_EntryCount;
        int m_PendingStackReleaseCount;
        int m_StackReleaseHead;
        int m_StackReleaseCount;
        ulong m_LastRequestSequence;
        ulong m_LastCompletionIdentity;
        ulong m_LastContributionContinuityIdentity;
        ulong m_ContinuityIdentity = 1;
        ulong m_PendingPlanCompletionIdentity;
        ulong m_SourceFrameCompletionIdentity;
        float m_LastOutputWeight;
        float m_StoredOutputWeight;
        float m_PendingCaptureOutputWeight;
        float m_PlannedStoredMaximumWeight;
        bool m_HasCompletedFrame;
        bool m_HasStoredPose;
        bool m_HasPendingStoredCapture;
        bool m_SelectionUnavailable;
        bool m_Disposed;
        AnimationPoseAvailability m_LastAvailability;
        AnimationPoseNativeInvalidReason m_LastInvalidReason;
        AnimationSlotBlendFramePlanKind m_PendingPlanKind;
        ulong m_PendingStoredContributionIdentity;

        internal AnimationBlendStackRuntime(
            AnimationBlendNodePayload slot,
            AnimationChannelId animationChannelId,
            AnimationSelectionAvailabilityPolicy availabilityPolicy,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            CharacterAnimationRigPayload rig,
            in AnimationPlayerPoseNativeWriteBinding initialFinalWriteBinding)
        {
            m_Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            m_AnimationChannelId = animationChannelId;
            m_AvailabilityPolicy = availabilityPolicy;
            m_CurveCatalog = curveCatalog ?? throw new ArgumentNullException(nameof(curveCatalog));
            m_ProfileCatalog = profileCatalog ?? throw new ArgumentNullException(nameof(profileCatalog));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            if (!slot.NodeId.IsValid || !animationChannelId.IsValid ||
                !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), availabilityPolicy) ||
                slot.StackPolicy == null || curveCatalog.Entries.Count == 0 ||
                profileCatalog.Entries.Count == 0)
            {
                throw new ArgumentException("Animation Blend Stack assembly is invalid.");
            }

            rig.RequireValid();
            slot.StackPolicy.RequireValid();
            for (int i = 0; i < curveCatalog.Entries.Count; i++)
                curveCatalog.Require(i).RequireValid();
            for (int i = 0; i < profileCatalog.Entries.Count; i++)
                profileCatalog.Require(i).RequireValid(rig.Bones.Count, rig.RigId, rig.RigRevision);
            for (int i = 0; i < slot.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = slot.Transitions[i] ??
                    throw new InvalidOperationException($"Animation Blend transition #{i} is missing.");
                transition.RequireValid(curveCatalog.Entries.Count, profileCatalog.Entries.Count);
                curveCatalog.Require(transition.CurveIndex);
                profileCatalog.Require(transition.BlendProfileIndex);
            }

            int capacity = slot.StackPolicy.MaxActiveSourceEntries;
            int boneCount = rig.Bones.Count;
            int parameterCount = initialFinalWriteBinding.PoseParameters.Length;
            if (initialFinalWriteBinding.DenseLocalPoses.Length != boneCount || parameterCount <= 0)
                throw new ArgumentException("Animation Blend Stack final Slot layout is invalid.", nameof(initialFinalWriteBinding));

            m_Entries = new AnimationBlendEntryState[capacity];
            m_CompactedEntries = new AnimationBlendEntryState[capacity];
            m_EntrySourceCaptureIndices = new int[capacity];
            m_CompactedSourceCaptureIndices = new int[capacity];
            m_EntryRawAlphas = new float[capacity];
            m_EntryEasedAlphas = new float[capacity];
            m_EntryScalarWeights = new float[capacity];
            m_EntryBoneWeights = new float[checked(capacity * boneCount)];
            m_PlannedEntryMaximumWeights = new float[capacity];
            m_StoredBoneOutputWeights = new float[boneCount];
            m_PendingCaptureBoneOutputWeights = new float[boneCount];
            m_LastBoneOutputWeights = new float[boneCount];
            m_RemovedSourceIds = new AnimationPoseSourceId[capacity + 1];
            m_PendingStackReleaseSourceIds = new AnimationPoseSourceId[capacity + 1];
            m_StackReleases = new AnimationBlendStackRelease[capacity + 1];
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
        internal AnimationSelectionAvailabilityPolicy OutputPolicy => m_AvailabilityPolicy;
        internal int EntryCount => m_EntryCount;
        internal bool HasStoredPose => m_HasStoredPose || m_HasPendingStoredCapture;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal bool HasCurrentSelectionSample => !m_SelectionUnavailable;
        internal AnimationPoseAvailability LastAvailability => m_LastAvailability;
        internal float LastOutputWeight => m_LastOutputWeight;
        internal AnimationPoseNativeInvalidReason LastInvalidReason => m_LastInvalidReason;
        internal ulong ContinuityIdentity => m_ContinuityIdentity;

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
                entryBoneWeights.Length < checked((entryOffset + m_EntryCount) * m_Rig.Bones.Count) ||
                storedBoneWeights.Length < checked((stackIndex + 1) * m_Rig.Bones.Count))
            {
                throw new ArgumentException("Animation Blend Stack diagnostics capacity is invalid.");
            }

            for (int entryIndex = 0; entryIndex < m_EntryCount; entryIndex++)
            {
                AnimationBlendEntryState entry = m_Entries[entryIndex];
                AnimationBlendProfilePayload profile = m_ProfileCatalog.Require(entry.BlendProfileIndex);
                int diagnosticIndex = entryOffset + entryIndex;
                entryDestination[diagnosticIndex] = new AnimationBlendStackEntrySnapshot(
                    AnimationChannelId,
                    PoseNodeId,
                    entry.EntryId,
                    entryIndex,
                    entry.ProgramProducerIndex,
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
                    entryIndex * m_Rig.Bones.Count,
                    entryBoneWeights,
                    diagnosticIndex * m_Rig.Bones.Count,
                    m_Rig.Bones.Count);
            }

            Array.Copy(
                m_HasPendingStoredCapture ? m_PendingCaptureBoneOutputWeights : m_StoredBoneOutputWeights,
                0,
                storedBoneWeights,
                stackIndex * m_Rig.Bones.Count,
                m_Rig.Bones.Count);
            bool hasStored = m_HasStoredPose || m_HasPendingStoredCapture;
            AnimationSlotBlendStoredPoseNativeState storedState = m_HasStoredPose
                ? RequireStoredState()
                : default;
            ulong storedIdentity = m_HasStoredPose
                ? storedState.ContributionContinuityIdentity
                : m_HasPendingStoredCapture ? m_PendingStoredContributionIdentity : 0;
            stackDestination[stackIndex] = new AnimationBlendStackSnapshot(
                AnimationChannelId,
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
            return m_Entries[index].EntryId;
        }

        internal AnimationBlendTransitionIdentity ResolveExpectedTransitionIdentity(
            int targetProducerIndex,
            bool targetEmpty)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (targetEmpty ? targetProducerIndex != -1 : targetProducerIndex < 0)
                throw new ArgumentException("Animation Blend target endpoint is invalid.");
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            return m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                targetProducerIndex,
                targetEmpty).GetIdentity(m_Slot.NodeId);
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
            in AnimationSourcePoseSample sourceSample,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            AnimationSelectionFrame request = sourceSample.Selection;
            if (!request.IsValid || request.AnimationChannelId != m_AnimationChannelId ||
                m_SourceFrameCompletionIdentity == 0 ||
                m_SourceFrameCompletionIdentity != m_Sources.CompletionIdentity)
            {
                throw new ArgumentException("Animation source capture request is routed to the wrong Blend Stack.");
            }

            bool referenced = false;
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (m_Entries[i].IsEmpty || !m_Entries[i].SourceId.Equals(request.SourceId))
                    continue;
                if (m_Entries[i].ProgramProducerIndex != request.ProgramProducerIndex)
                    throw new InvalidOperationException("Animation source capture producer differs from its Blend entry.");
                referenced = true;
            }
            if (!referenced)
                throw new InvalidOperationException("Animation source capture is not referenced by this Blend Stack.");

            AnimationPoseSourceCaptureBinding binding = m_Sources.PrepareCapture(in sourceSample, presentationDeltaSeconds);
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!m_Entries[i].IsEmpty && m_Entries[i].SourceId.Equals(request.SourceId))
                    m_EntrySourceCaptureIndices[i] = binding.SourceIndex;
            }
            return binding;
        }

        internal AnimationBlendPushResult PushPoseRequest(in AnimationSelectionFrame request)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!request.IsValid || request.AnimationChannelId != m_AnimationChannelId || !request.SourceId.IsValid ||
                request.ProgramProducerIndex < 0)
            {
                throw new ArgumentException("Resolved animation pose request is routed to the wrong Blend Stack.");
            }

            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload transition = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                request.ProgramProducerIndex,
                false);
            AnimationBlendPushResult result = Push(new AnimationBlendPushRequest(
                m_AnimationChannelId,
                m_Slot.NodeId,
                request.SourceId,
                false,
                request.ProgramProducerIndex,
                request.PresentationRequestSequence,
                transition), false);
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

        internal AnimationBlendPushResult PushEmpty(ulong presentationRequestSequence)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            RequireTarget(default, true, -1, presentationRequestSequence);
            if (IsCurrentTarget(default, true, -1))
            {
                ContinueCurrentTarget(presentationRequestSequence);
                m_SelectionUnavailable = false;
                return AnimationBlendPushResult.ContinuedSource;
            }
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload transition = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                -1,
                true);
            AnimationBlendPushResult result = Push(new AnimationBlendPushRequest(
                m_AnimationChannelId,
                m_Slot.NodeId,
                default,
                true,
                -1,
                presentationRequestSequence,
                transition), true);
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
                m_CompactedEntries[i] = m_Entries[i];
                m_CompactedEntries[i].Advance(deltaSeconds);
            }
            Array.Copy(m_CompactedEntries, 0, m_Entries, 0, m_EntryCount);
            Array.Clear(m_CompactedEntries, 0, m_EntryCount);
        }

        internal AnimationSlotBlendJob PrepareSlotJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (physicalSources == null)
                throw new ArgumentNullException(nameof(physicalSources));
            if (completionIdentity == 0 || completionIdentity != m_SourceFrameCompletionIdentity ||
                finalWriteBinding.CompletionIdentity != completionIdentity ||
                finalWriteBinding.DenseLocalPoses.Length != m_Rig.Bones.Count)
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

        internal void CompleteFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity != m_PendingPlanCompletionIdentity)
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
                return;
            }
            if (availability != AnimationPoseAvailability.Pose && availability != AnimationPoseAvailability.NoPose)
                throw new InvalidOperationException("Animation Blend Stack job published an unknown availability.");

            CacheCompletedOutput(in output);
            if (m_HasPendingStoredCapture)
                CommitPendingStoredCapture();

            RetireCompletedHistory();
            PublishPendingStackReleases(completionIdentity);
            m_LastCompletionIdentity = completionIdentity;
            m_LastAvailability = availability;
            m_LastInvalidReason = AnimationPoseNativeInvalidReason.None;
            m_HasCompletedFrame = true;
            m_PendingPlanCompletionIdentity = 0;
            m_SourceFrameCompletionIdentity = 0;
        }

        internal bool TryDequeueStackRelease(out AnimationBlendStackRelease release)
        {
            RequireAlive();
            if (m_StackReleaseCount == 0)
            {
                release = default;
                return false;
            }
            release = m_StackReleases[m_StackReleaseHead];
            m_StackReleases[m_StackReleaseHead] = default;
            m_StackReleaseHead = (m_StackReleaseHead + 1) % m_StackReleases.Length;
            m_StackReleaseCount--;
            return true;
        }

        internal void ReleaseSource(AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation source identity is invalid.", nameof(sourceId));
            if (IsSourceReferenced(sourceId) || ContainsPendingRelease(sourceId) || ContainsQueuedRelease(sourceId))
                throw new InvalidOperationException("Animation source is still retained by its Blend Stack.");
            m_Sources.ReleaseSource(sourceId);
        }

        internal void Reset(ulong completionIdentity)
        {
            RequireAlive();
            if (m_PendingPlanCompletionIdentity != 0)
                throw new InvalidOperationException("Animation Blend Stack frame plan must complete before reset.");
            if (completionIdentity == 0 || completionIdentity < m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            int removedCount = CopyReferencedSourceIds(m_RemovedSourceIds);
            RequireReleaseCapacity(m_RemovedSourceIds, removedCount, default);
            RequireCanAdvanceContinuityIdentity();

            for (int i = 0; i < removedCount; i++)
                StageStackRelease(m_RemovedSourceIds[i]);
            Array.Clear(m_Entries, 0, m_Entries.Length);
            Fill(m_EntrySourceCaptureIndices, -1);
            m_EntryCount = 0;
            ClearPendingCaptures();
            m_HasStoredPose = false;
            m_SelectionUnavailable = false;
            m_HasCompletedFrame = false;
            m_LastAvailability = default;
            m_LastInvalidReason = AnimationPoseNativeInvalidReason.None;
            m_LastOutputWeight = 0f;
            m_StoredOutputWeight = 0f;
            Array.Clear(m_LastBoneOutputWeights, 0, m_LastBoneOutputWeights.Length);
            Array.Clear(m_StoredBoneOutputWeights, 0, m_StoredBoneOutputWeights.Length);
            m_Sources.ResetContinuity();
            m_SlotWorkspace.Reset();
            m_SourceFrameCompletionIdentity = 0;
            m_LastRequestSequence = 0;
            m_LastCompletionIdentity = completionIdentity;
            AdvanceContinuityIdentity();
            PublishPendingStackReleases(completionIdentity);
        }

        AnimationBlendPushResult Push(AnimationBlendPushRequest request, bool forceStoredCapture)
        {
            RequireRequest(request);
            if (forceStoredCapture && !request.TargetEmpty)
                throw new InvalidOperationException("Forced Stored Pose capture requires an Empty target.");
            if (m_HasPendingStoredCapture)
            {
                if (IsCurrentTarget(request.SourceId, request.TargetEmpty, request.ProgramProducerIndex))
                {
                    ContinueCurrentTarget(request.PresentationRequestSequence);
                    return AnimationBlendPushResult.ContinuedSource;
                }
                throw new InvalidOperationException("Animation Blend capture must complete before another target push.");
            }
            if (IsCurrentTarget(request.SourceId, request.TargetEmpty, request.ProgramProducerIndex))
            {
                ContinueCurrentTarget(request.PresentationRequestSequence);
                return AnimationBlendPushResult.ContinuedSource;
            }
            if (request.PresentationRequestSequence <= m_LastRequestSequence)
                throw new InvalidOperationException("Animation Blend push request order is not strictly increasing.");
            if (m_AvailabilityPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection && request.TargetEmpty)
                throw new InvalidOperationException($"Required Blend Stack '{m_Slot.NodeId}' cannot target Empty.");

            AnimationBlendPushResult result = PushCrossFade(request, forceStoredCapture);
            m_LastRequestSequence = request.PresentationRequestSequence;
            Debug.Log(
                $"Animation Blend target changed Channel={m_AnimationChannelId}, Node={m_Slot.NodeId}, " +
                $"Source={request.SourceId}, Producer={request.ProgramProducerIndex}, Empty={request.TargetEmpty}, " +
                $"Sequence={request.PresentationRequestSequence}, Duration={request.Transition.DurationSeconds:R}, " +
                $"Curve={request.Transition.CurveIndex}, Profile={request.Transition.BlendProfileIndex}, " +
                $"Result={result}, Entries={m_EntryCount}.");
            return result;
        }

        string BuildInvalidFrameDiagnostic(
            ulong completionIdentity,
            AnimationPoseNativeInvalidReason invalidReason)
        {
            var builder = new StringBuilder(512);
            builder.Append("Animation Blend Stack invalid")
                .Append(" Channel=").Append(m_AnimationChannelId)
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
                AnimationBlendEntryState entry = m_Entries[i];
                builder.Append(" | #").Append(i)
                    .Append(" Id=").Append(entry.EntryId)
                    .Append(", Source=").Append(entry.SourceId)
                    .Append(", Producer=").Append(entry.ProgramProducerIndex)
                    .Append(", Empty=").Append(entry.IsEmpty)
                    .Append(", Elapsed=").Append(entry.ElapsedSeconds)
                    .Append(", RawAlpha=").Append(m_EntryRawAlphas[i])
                    .Append(", EasedAlpha=").Append(m_EntryEasedAlphas[i])
                    .Append(", Weight=").Append(m_EntryScalarWeights[i])
                    .Append(", Capture=").Append(m_EntrySourceCaptureIndices[i]);
            }
            return builder.Append('.').ToString();
        }

        AnimationBlendPushResult PushCrossFade(AnimationBlendPushRequest request, bool forceStoredCapture)
        {
            bool startsNewContinuity = !request.TargetEmpty &&
                                       (!m_HasCompletedFrame || m_LastAvailability != AnimationPoseAvailability.Pose);
            bool replaceHistory = forceStoredCapture || m_EntryCount == m_Entries.Length ||
                                  m_EntryCount > 0 && m_Entries[m_EntryCount - 1].ElapsedSeconds <=
                                  m_Slot.StackPolicy.MaxBlendInTimeToReplaceNewest;
            int identityCount = replaceHistory ? 2 : 1;
            RequireContributionIdentityCapacity(identityCount);
            if (startsNewContinuity)
                RequireCanAdvanceContinuityIdentity();

            int captureIndex = FindSourceCaptureIndex(request.SourceId);
            AnimationBlendEntryState newEntry = CreateEntry(request, AllocateContributionContinuityIdentity());
            if (replaceHistory)
            {
                RequireCapturableFrame();
                int removedCount = CopyEntrySourceIds(m_RemovedSourceIds);
                RequireReleaseCapacity(m_RemovedSourceIds, removedCount, request.SourceId);
                ulong storedIdentity = AllocateContributionContinuityIdentity();

                CancelStackRelease(request.SourceId);
                CapturePendingOutput(storedIdentity);
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
                if (m_EntryCount == m_Entries.Length)
                    throw new InvalidOperationException("Animation Blend Stack capacity was exceeded.");
                for (int i = 0; i < m_EntryCount; i++)
                {
                    m_CompactedEntries[i] = m_Entries[i];
                    m_CompactedEntries[i].IncreasePushDepth(m_Slot.StackPolicy.DepthBlendTimeMultiplier);
                }
                Array.Copy(m_CompactedEntries, 0, m_Entries, 0, m_EntryCount);
                Array.Clear(m_CompactedEntries, 0, m_EntryCount);
                CancelStackRelease(request.SourceId);
                AddEntry(newEntry, captureIndex);
            }
            if (startsNewContinuity)
                AdvanceContinuityIdentity();
            return replaceHistory
                ? AnimationBlendPushResult.CapturedStoredPose
                : AnimationBlendPushResult.Pushed;
        }

        void PrepareCrossFadePlan(
            in AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            AnimationPoseSourcePhysicalRegistry physicalSources,
            AnimationSlotBlendFramePlanKind kind)
        {
            bool capturesStored = kind == AnimationSlotBlendFramePlanKind.StoredCapture;
            bool usesStored = capturesStored || m_HasStoredPose;
            float storedOutputWeight = capturesStored ? m_PendingCaptureOutputWeight : m_StoredOutputWeight;
            float[] storedBoneWeights = capturesStored ? m_PendingCaptureBoneOutputWeights : m_StoredBoneOutputWeights;

            float scalarResidual = 1f;
            for (int i = m_EntryCount - 1; i >= 0; i--)
            {
                AnimationBlendEntryState entry = m_Entries[i];
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
                if (!m_Entries[i].IsEmpty)
                    outputWeight += m_EntryScalarWeights[i];
            }
            RequireNormalized(outputWeight);

            bool hasDenseOutput = false;
            for (int boneIndex = 0; boneIndex < m_Rig.Bones.Count; boneIndex++)
            {
                float residual = 1f;
                float boneOutputWeight = 0f;
                for (int i = m_EntryCount - 1; i >= 0; i--)
                {
                    AnimationBlendEntryState entry = m_Entries[i];
                    float alpha = entry.EvaluateBoneAlpha(
                        boneIndex,
                        m_CurveCatalog.Require(entry.CanonicalCurveIndex),
                        m_ProfileCatalog.Require(entry.BlendProfileIndex));
                    RequireNormalized(alpha);
                    float weight = residual * alpha;
                    m_EntryBoneWeights[i * m_Rig.Bones.Count + boneIndex] = weight;
                    m_PlannedEntryMaximumWeights[i] = Mathf.Max(m_PlannedEntryMaximumWeights[i], weight);
                    if (!entry.IsEmpty)
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
            AnimationPoseSourcePhysicalRegistry physicalSources,
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
                        GetStoredResidualForBone(m_Rig.LeftFootBoneIndex) * storedBoneWeights[m_Rig.LeftFootBoneIndex],
                        GetStoredResidualForBone(m_Rig.RightFootBoneIndex) * storedBoneWeights[m_Rig.RightFootBoneIndex]));
                for (int boneIndex = 0; boneIndex < m_Rig.Bones.Count; boneIndex++)
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
                AnimationBlendEntryState entry = m_Entries[i];
                if (entry.IsEmpty)
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
                        entry.ProgramProducerIndex,
                        entry.ContributionContinuityIdentity,
                        m_EntryScalarWeights[i],
                        m_EntryBoneWeights[i * m_Rig.Bones.Count + m_Rig.LeftFootBoneIndex],
                        m_EntryBoneWeights[i * m_Rig.Bones.Count + m_Rig.RightFootBoneIndex]));
                for (int boneIndex = 0; boneIndex < m_Rig.Bones.Count; boneIndex++)
                {
                    m_SlotWorkspace.SetPreparedDenseBoneWeight(
                        preparation,
                        contributionIndex,
                        boneIndex,
                        m_EntryBoneWeights[i * m_Rig.Bones.Count + boneIndex]);
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
            for (int boneIndex = 0; boneIndex < m_LastBoneOutputWeights.Length; boneIndex++)
            {
                float weight = 0f;
                for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                    weight += output.DenseContributionWeights[contributionIndex * m_LastBoneOutputWeights.Length + boneIndex];
                RequireNormalized(weight);
                m_LastBoneOutputWeights[boneIndex] = weight;
            }
            m_LastOutputWeight = outputWeight;
        }

        void CommitPendingStoredCapture()
        {
            if (m_PendingPlanKind != AnimationSlotBlendFramePlanKind.StoredCapture)
                throw new InvalidOperationException("Stored Pose capture completed with the wrong frame plan kind.");
            m_StoredOutputWeight = m_PendingCaptureOutputWeight;
            Array.Copy(m_PendingCaptureBoneOutputWeights, m_StoredBoneOutputWeights, m_StoredBoneOutputWeights.Length);
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
                    m_CompactedEntries[keptCount] = m_Entries[i];
                    m_CompactedSourceCaptureIndices[keptCount] = m_EntrySourceCaptureIndices[i];
                    keptCount++;
                }
                else if (!m_Entries[i].IsEmpty)
                {
                    removedCount = AppendUniqueSourceId(m_RemovedSourceIds, removedCount, m_Entries[i].SourceId);
                }
            }
            RequireReleaseCapacity(m_RemovedSourceIds, removedCount, default);
            ClearEntries();
            Array.Copy(m_CompactedEntries, 0, m_Entries, 0, keptCount);
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
                    request.TargetEmpty,
                    request.PresentationRequestSequence),
                request.ProgramProducerIndex,
                request.Transition.DurationSeconds,
                request.Transition.CurveIndex,
                request.Transition.BlendProfileIndex,
                contributionContinuityIdentity);
        }

        void AddEntry(AnimationBlendEntryState entry, int sourceCaptureIndex)
        {
            if (m_EntryCount == m_Entries.Length)
                throw new InvalidOperationException("Animation Blend Stack capacity was exceeded without Stored Pose capture.");
            m_Entries[m_EntryCount] = entry;
            m_EntrySourceCaptureIndices[m_EntryCount] = entry.IsEmpty ? -1 : sourceCaptureIndex;
            m_EntryCount++;
        }

        void RequireRequest(AnimationBlendPushRequest request)
        {
            if (request.AnimationChannelId != m_AnimationChannelId || request.PoseNodeId != m_Slot.NodeId)
                throw new InvalidOperationException("Animation Blend push was routed to the wrong node.");
            RequireTarget(request.SourceId, request.TargetEmpty, request.ProgramProducerIndex, request.PresentationRequestSequence);
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload exact = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                request.ProgramProducerIndex,
                request.TargetEmpty);
            if (!ReferenceEquals(exact, request.Transition) ||
                exact.GetIdentity(m_Slot.NodeId) != request.Transition.GetIdentity(m_Slot.NodeId))
            {
                throw new InvalidOperationException("Animation Blend push did not use the compiled exact transition.");
            }
        }

        static void RequireTarget(
            AnimationPoseSourceId sourceId,
            bool targetEmpty,
            int programProducerIndex,
            ulong presentationRequestSequence)
        {
            if (presentationRequestSequence == 0 || targetEmpty == sourceId.IsValid ||
                targetEmpty == (programProducerIndex >= 0))
            {
                throw new ArgumentException("Animation Blend target identity is invalid.");
            }
        }

        bool IsCurrentTarget(
            AnimationPoseSourceId sourceId,
            bool targetEmpty,
            int programProducerIndex)
        {
            if (m_EntryCount == 0)
                return targetEmpty;
            AnimationBlendEntryState current = m_Entries[m_EntryCount - 1];
            return current.IsEmpty == targetEmpty &&
                   current.ProgramProducerIndex == programProducerIndex &&
                   (targetEmpty || current.SourceId.Equals(sourceId));
        }

        void GetCurrentEndpoint(out int producerIndex, out bool empty)
        {
            if (m_EntryCount == 0)
            {
                producerIndex = -1;
                empty = true;
                return;
            }
            AnimationBlendEntryState current = m_Entries[m_EntryCount - 1];
            producerIndex = current.ProgramProducerIndex;
            empty = current.IsEmpty;
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
            if (!m_HasCompletedFrame || m_LastAvailability != AnimationPoseAvailability.Pose ||
                m_LastCompletionIdentity == 0)
            {
                throw new InvalidOperationException("Animation Blend Stack has no completed Pose frame to capture.");
            }
        }

        AnimationPhysicalSourceIdentity RequirePhysicalSource(
            AnimationPoseSourcePhysicalRegistry physicalSources,
            AnimationBlendEntryState entry)
        {
            AnimationPhysicalSourceIdentity identity = physicalSources.RequireIdentity(entry.SourceId, m_Slot.NodeId);
            if (physicalSources.RequirePoseNodeId(identity) != m_Slot.NodeId ||
                physicalSources.RequireProgramProducerIndex(identity) != entry.ProgramProducerIndex)
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
                if (!m_Entries[i].IsEmpty && m_Entries[i].SourceId.Equals(sourceId))
                    return m_EntrySourceCaptureIndices[i];
            }
            return -1;
        }

        int CountCrossFadeContributions(bool usesStored)
        {
            int count = usesStored ? 1 : 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!m_Entries[i].IsEmpty)
                    count++;
            }
            return count;
        }

        float GetStoredResidualForBone(int boneIndex)
        {
            float residual = 1f;
            for (int i = m_EntryCount - 1; i >= 0; i--)
                residual -= m_EntryBoneWeights[i * m_Rig.Bones.Count + boneIndex];
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
                if (!m_Entries[i].IsEmpty)
                    count = AppendUniqueSourceId(destination, count, m_Entries[i].SourceId);
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
                if (!m_Entries[i].IsEmpty && m_Entries[i].SourceId.Equals(sourceId))
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
                if (m_StackReleaseCount == m_StackReleases.Length)
                    throw new InvalidOperationException("Animation Blend source retirement queue was not drained.");
                int tail = (m_StackReleaseHead + m_StackReleaseCount) % m_StackReleases.Length;
                m_StackReleases[tail] = new AnimationBlendStackRelease(
                    m_Slot.NodeId,
                    sourceId,
                    completionIdentity);
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
                int index = (m_StackReleaseHead + i) % m_StackReleases.Length;
                if (!m_StackReleases[index].SourceId.Equals(sourceId))
                    continue;
                for (int shift = i; shift + 1 < m_StackReleaseCount; shift++)
                {
                    int destination = (m_StackReleaseHead + shift) % m_StackReleases.Length;
                    int source = (m_StackReleaseHead + shift + 1) % m_StackReleases.Length;
                    m_StackReleases[destination] = m_StackReleases[source];
                }
                int tail = (m_StackReleaseHead + m_StackReleaseCount - 1) % m_StackReleases.Length;
                m_StackReleases[tail] = default;
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
                int index = (m_StackReleaseHead + i) % m_StackReleases.Length;
                if (!m_StackReleases[index].SourceId.Equals(retainedSourceId))
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
            if (pending > m_PendingStackReleaseSourceIds.Length || pending + queued > m_StackReleases.Length)
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
                AnimationPoseSourceId candidate = m_StackReleases[(m_StackReleaseHead + i) % m_StackReleases.Length].SourceId;
                if (!candidate.Equals(ignored) && candidate.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void ClearEntries()
        {
            Array.Clear(m_Entries, 0, m_Entries.Length);
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
            m_ContinuityIdentity++;
        }

        void RequireCanAdvanceContinuityIdentity()
        {
            if (m_ContinuityIdentity == ulong.MaxValue)
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
            Array.Clear(m_Entries, 0, m_Entries.Length);
            Array.Clear(m_PendingStackReleaseSourceIds, 0, m_PendingStackReleaseSourceIds.Length);
            Array.Clear(m_StackReleases, 0, m_StackReleases.Length);
            m_EntryCount = 0;
            m_PendingStackReleaseCount = 0;
            m_StackReleaseHead = 0;
            m_StackReleaseCount = 0;
            m_Disposed = true;
        }
    }
}
