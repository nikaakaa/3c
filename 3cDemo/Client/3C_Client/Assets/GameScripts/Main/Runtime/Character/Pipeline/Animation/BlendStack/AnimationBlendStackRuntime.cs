using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendStackRuntime : IDisposable
    {
        readonly AnimationBlendSlotPayload m_Slot;
        readonly AnimationBlendCurveCatalogPayload m_CurveCatalog;
        readonly AnimationBlendProfileCatalogPayload m_ProfileCatalog;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly AnimationBlendEntryState[] m_Entries;
        readonly AnimationBlendEntryState[] m_CompactedEntries;
        readonly int[] m_EntrySourceCaptureIndices;
        readonly int[] m_CompactedSourceCaptureIndices;
        readonly float[] m_EntryScalarWeights;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_PlannedEntryMaximumWeights;
        readonly float[] m_StoredBoneOutputWeights;
        readonly float[] m_InertialBoneOutputWeights;
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
        int m_PendingInertialSourceCaptureIndex = -1;
        ulong m_LastRequestSequence;
        ulong m_LastCompletionIdentity;
        ulong m_LastContributionContinuityIdentity;
        ulong m_ContinuityIdentity = 1;
        ulong m_PendingPlanCompletionIdentity;
        ulong m_SourceFrameCompletionIdentity;
        float m_LastOutputWeight;
        float m_StoredOutputWeight;
        float m_InertialOutputWeight;
        float m_PendingCaptureOutputWeight;
        float m_PlannedStoredMaximumWeight;
        bool m_HasCompletedFrame;
        bool m_HasStoredPose;
        bool m_HasInertialBlend;
        bool m_HasPendingStoredCapture;
        bool m_HasPendingInertialCapture;
        bool m_Disposed;
        PoseSlotFrameAvailability m_LastAvailability;
        AnimationPoseNativeInvalidReason m_LastInvalidReason;
        AnimationSlotBlendFramePlanKind m_PendingPlanKind;
        AnimationBlendEntryState m_PendingInertialEntry;
        ulong m_PendingStoredContributionIdentity;
        ulong m_PendingInertialContributionIdentity;

        internal AnimationBlendStackRuntime(
            AnimationBlendSlotPayload slot,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            CharacterAnimationRigPayload rig,
            in AnimationPoseSlotNativeWriteBinding initialFinalWriteBinding)
        {
            m_Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            m_CurveCatalog = curveCatalog ?? throw new ArgumentNullException(nameof(curveCatalog));
            m_ProfileCatalog = profileCatalog ?? throw new ArgumentNullException(nameof(profileCatalog));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            if (!slot.PoseSlotId.IsValid || !slot.AnimationChannelId.IsValid ||
                !Enum.IsDefined(typeof(PoseSlotOutputPolicy), slot.OutputPolicy) ||
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
            m_EntryScalarWeights = new float[capacity];
            m_EntryBoneWeights = new float[checked(capacity * boneCount)];
            m_PlannedEntryMaximumWeights = new float[capacity];
            m_StoredBoneOutputWeights = new float[boneCount];
            m_InertialBoneOutputWeights = new float[boneCount];
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

        internal PoseSlotId PoseSlotId => m_Slot.PoseSlotId;
        internal AnimationChannelId AnimationChannelId => m_Slot.AnimationChannelId;
        internal PoseSlotOutputPolicy OutputPolicy => m_Slot.OutputPolicy;
        internal int EntryCount => m_EntryCount;
        internal bool HasStoredPose => m_HasStoredPose || m_HasPendingStoredCapture;
        internal bool HasInertialBlend => m_HasInertialBlend;
        internal bool HasPendingInertialCapture => m_HasPendingInertialCapture;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal PoseSlotFrameAvailability LastAvailability => m_LastAvailability;
        internal float LastOutputWeight => m_LastOutputWeight;
        internal AnimationPoseNativeInvalidReason LastInvalidReason => m_LastInvalidReason;
        internal ulong ContinuityIdentity => m_ContinuityIdentity;

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
                targetEmpty).GetIdentity(m_Slot.PoseSlotId);
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
            in ResolvedAnimationPoseRequest request,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!request.IsValid || request.AnimationChannelId != m_Slot.AnimationChannelId ||
                request.PoseSlotId != m_Slot.PoseSlotId ||
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
            if (m_HasPendingInertialCapture && m_PendingInertialEntry.SourceId.Equals(request.SourceId))
            {
                if (m_PendingInertialEntry.ProgramProducerIndex != request.ProgramProducerIndex)
                    throw new InvalidOperationException("Pending Inertial target producer differs from its source capture.");
                referenced = true;
            }
            if (!referenced)
                throw new InvalidOperationException("Animation source capture is not referenced by this Blend Stack.");

            AnimationPoseSourceCaptureBinding binding = m_Sources.PrepareCapture(in request, presentationDeltaSeconds);
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!m_Entries[i].IsEmpty && m_Entries[i].SourceId.Equals(request.SourceId))
                    m_EntrySourceCaptureIndices[i] = binding.SourceIndex;
            }
            if (m_HasPendingInertialCapture && m_PendingInertialEntry.SourceId.Equals(request.SourceId))
                m_PendingInertialSourceCaptureIndex = binding.SourceIndex;
            return binding;
        }

        internal AnimationBlendPushResult PushPoseRequest(in ResolvedAnimationPoseRequest request)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            if (!request.IsValid || request.AnimationChannelId != m_Slot.AnimationChannelId ||
                request.PoseSlotId != m_Slot.PoseSlotId || !request.SourceId.IsValid ||
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
            if (request.ExactTransitionIdentity != transition.GetIdentity(m_Slot.PoseSlotId))
                throw new InvalidOperationException("Resolved animation pose request does not carry the exact compiled transition.");
            return Push(new AnimationBlendPushRequest(
                m_Slot.AnimationChannelId,
                m_Slot.PoseSlotId,
                request.SourceId,
                false,
                request.ProgramProducerIndex,
                request.PresentationRequestSequence,
                transition));
        }

        internal AnimationBlendPushResult PushEmpty(ulong presentationRequestSequence)
        {
            RequireAlive();
            RequireNoPreparedPlan();
            RequireTarget(default, true, -1, presentationRequestSequence);
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload transition = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                -1,
                true);
            return Push(new AnimationBlendPushRequest(
                m_Slot.AnimationChannelId,
                m_Slot.PoseSlotId,
                default,
                true,
                -1,
                presentationRequestSequence,
                transition));
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
            AnimationBlendEntryState pending = m_PendingInertialEntry;
            if (m_HasPendingInertialCapture)
                pending.Advance(deltaSeconds);
            Array.Copy(m_CompactedEntries, 0, m_Entries, 0, m_EntryCount);
            Array.Clear(m_CompactedEntries, 0, m_EntryCount);
            if (m_HasPendingInertialCapture)
                m_PendingInertialEntry = pending;
        }

        internal AnimationSlotBlendJob PrepareSlotJob(
            ulong completionIdentity,
            in AnimationPoseSlotNativeWriteBinding finalWriteBinding,
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

            AnimationBlendSourcePoseNativeReadBinding sourceBinding =
                m_Sources.RequireNativeReadBinding(completionIdentity);
            AnimationSlotBlendFramePlanKind kind = ResolvePlanKind();
            ClearPlannedWeights();
            if (IsInertial(kind))
                PrepareInertialPlan(in finalWriteBinding, physicalSources, kind);
            else
                PrepareCrossFadePlan(in finalWriteBinding, physicalSources, kind);

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
            AnimationPoseSlotNativeWriteBinding output = binding.FinalWriteBinding;
            if (output.CompletedAt[0] != completionIdentity)
                throw new InvalidOperationException("Animation Blend Stack job has not completed the requested frame.");

            PoseSlotFrameAvailability availability = output.Availability[0];
            AnimationPoseNativeInvalidReason invalidReason = output.InvalidReason[0];
            if (availability == PoseSlotFrameAvailability.Invalid || invalidReason != AnimationPoseNativeInvalidReason.None)
            {
                m_LastCompletionIdentity = completionIdentity;
                m_LastAvailability = PoseSlotFrameAvailability.Invalid;
                m_LastInvalidReason = invalidReason == AnimationPoseNativeInvalidReason.None
                    ? AnimationPoseNativeInvalidReason.SlotPoseInvalid
                    : invalidReason;
                m_HasCompletedFrame = true;
                m_PendingPlanCompletionIdentity = 0;
                m_SourceFrameCompletionIdentity = 0;
                return;
            }
            if (availability != PoseSlotFrameAvailability.Pose && availability != PoseSlotFrameAvailability.NoPose)
                throw new InvalidOperationException("Animation Blend Stack job published an unknown availability.");

            CacheCompletedOutput(in output);
            if (m_HasPendingInertialCapture)
                CommitPendingInertialCapture();
            if (m_HasPendingStoredCapture)
                CommitPendingStoredCapture();
            if (IsInertial(m_PendingPlanKind) && m_EntryCount == 1 &&
                m_Entries[0].IsComplete(m_Rig.Bones.Count, m_ProfileCatalog.Require(m_Entries[0].BlendProfileIndex)))
            {
                m_HasInertialBlend = false;
            }

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
            m_HasInertialBlend = false;
            m_HasCompletedFrame = false;
            m_LastAvailability = default;
            m_LastInvalidReason = AnimationPoseNativeInvalidReason.None;
            m_LastOutputWeight = 0f;
            m_StoredOutputWeight = 0f;
            m_InertialOutputWeight = 0f;
            Array.Clear(m_LastBoneOutputWeights, 0, m_LastBoneOutputWeights.Length);
            Array.Clear(m_StoredBoneOutputWeights, 0, m_StoredBoneOutputWeights.Length);
            Array.Clear(m_InertialBoneOutputWeights, 0, m_InertialBoneOutputWeights.Length);
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
            if (m_HasPendingStoredCapture || m_HasPendingInertialCapture)
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
            if (m_Slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput && request.TargetEmpty)
                throw new InvalidOperationException($"Required Pose Slot '{m_Slot.PoseSlotId}' cannot target Empty.");

            AnimationBlendPushResult result = request.Transition.Technique == AnimationBlendTechnique.Inertial
                ? PushInertial(request)
                : PushCrossFade(request);
            m_LastRequestSequence = request.PresentationRequestSequence;
            return result;
        }

        AnimationBlendPushResult PushCrossFade(AnimationBlendPushRequest request)
        {
            bool startsNewContinuity = !request.TargetEmpty &&
                                       (!m_HasCompletedFrame || m_LastAvailability != PoseSlotFrameAvailability.Pose);
            bool replaceHistory = m_HasInertialBlend || m_EntryCount == m_Entries.Length ||
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

        AnimationBlendPushResult PushInertial(AnimationBlendPushRequest request)
        {
            if (request.TargetEmpty || request.Transition.SourceEmpty)
                throw new InvalidOperationException("Inertial Blend requires live source and target endpoints.");
            RequireCapturableFrame();
            RequireContributionIdentityCapacity(2);
            int removedCount = CopyEntrySourceIds(m_RemovedSourceIds);
            RequireReleaseCapacity(m_RemovedSourceIds, removedCount, request.SourceId);

            int captureIndex = FindSourceCaptureIndex(request.SourceId);
            AnimationBlendEntryState pendingEntry = CreateEntry(
                request,
                AllocateContributionContinuityIdentity());
            ulong inertialIdentity = AllocateContributionContinuityIdentity();

            CancelStackRelease(request.SourceId);
            for (int i = 0; i < removedCount; i++)
            {
                if (!m_RemovedSourceIds[i].Equals(request.SourceId))
                    StageStackRelease(m_RemovedSourceIds[i]);
            }
            m_PendingInertialEntry = pendingEntry;
            m_PendingInertialSourceCaptureIndex = captureIndex;
            m_PendingInertialContributionIdentity = inertialIdentity;
            m_HasPendingInertialCapture = true;
            m_PendingCaptureOutputWeight = m_LastOutputWeight;
            Array.Copy(m_LastBoneOutputWeights, m_PendingCaptureBoneOutputWeights, m_LastBoneOutputWeights.Length);
            return AnimationBlendPushResult.RebasedInertial;
        }

        void PrepareCrossFadePlan(
            in AnimationPoseSlotNativeWriteBinding finalWriteBinding,
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
                float alpha = entry.EvaluateOutputAlpha(
                    m_CurveCatalog.Require(entry.CanonicalCurveIndex),
                    m_ProfileCatalog.Require(entry.BlendProfileIndex));
                RequireNormalized(alpha);
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

            PoseSlotFrameAvailability availability = outputWeight > 0f || hasDenseOutput
                ? PoseSlotFrameAvailability.Pose
                : PoseSlotFrameAvailability.NoPose;
            if (availability == PoseSlotFrameAvailability.NoPose && m_Slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput)
                throw new InvalidOperationException("Required Pose Slot has no CrossFade output.");

            int contributionCount = availability == PoseSlotFrameAvailability.Pose
                ? CountCrossFadeContributions(usesStored)
                : 0;
            ulong historyCompletion = m_HasCompletedFrame && m_LastAvailability == PoseSlotFrameAvailability.Pose
                ? m_LastCompletionIdentity
                : 0;
            if (capturesStored && historyCompletion == 0)
                throw new InvalidOperationException("Stored Pose capture requires a completed Pose history frame.");

            AnimationSlotBlendFramePlanPreparation preparation = m_SlotWorkspace.PrepareInactivePage(
                in finalWriteBinding,
                availability == PoseSlotFrameAvailability.NoPose
                    ? AnimationSlotBlendFramePlanKind.CrossFade
                    : kind,
                m_Slot.OutputPolicy,
                m_Rig.ScalePolicy,
                availability,
                outputWeight,
                contributionCount,
                m_ContinuityIdentity,
                historyCompletion);
            try
            {
                if (availability == PoseSlotFrameAvailability.Pose)
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

        void PrepareInertialPlan(
            in AnimationPoseSlotNativeWriteBinding finalWriteBinding,
            AnimationPoseSourcePhysicalRegistry physicalSources,
            AnimationSlotBlendFramePlanKind kind)
        {
            AnimationBlendEntryState entry = m_HasPendingInertialCapture
                ? m_PendingInertialEntry
                : RequireSingleInertialEntry();
            int captureIndex = m_HasPendingInertialCapture
                ? m_PendingInertialSourceCaptureIndex
                : RequireSourceCaptureIndex(0);
            if (captureIndex < 0)
                throw new InvalidOperationException("Inertial target source has not been prepared in the source workspace.");
            AnimationPhysicalSourceIdentity physical = RequirePhysicalSource(physicalSources, entry);
            AnimationBlendCurvePayload curve = m_CurveCatalog.Require(entry.CanonicalCurveIndex);
            AnimationBlendProfilePayload profile = m_ProfileCatalog.Require(entry.BlendProfileIndex);
            float baseOutputWeight = m_HasPendingInertialCapture
                ? m_PendingCaptureOutputWeight
                : m_InertialOutputWeight;
            float[] baseBoneWeights = m_HasPendingInertialCapture
                ? m_PendingCaptureBoneOutputWeights
                : m_InertialBoneOutputWeights;

            EvaluateInertialEnvelope(entry.GetOutputNormalizedTime(profile), entry.GetOutputDuration(profile), curve,
                out float outputEnvelope, out float outputResidualWeight, out _);
            float diagnosticEnvelope = Mathf.Clamp01(outputEnvelope);
            float inertialScalarWeight = (1f - diagnosticEnvelope) * baseOutputWeight;
            float liveScalarWeight = diagnosticEnvelope;
            float outputWeight = inertialScalarWeight + liveScalarWeight;
            RequireNormalized(outputWeight);
            m_PlannedEntryMaximumWeights[0] = liveScalarWeight;
            m_PlannedStoredMaximumWeight = 0f;

            ulong historyCompletion = kind == AnimationSlotBlendFramePlanKind.InertialContinue
                ? 0
                : m_LastCompletionIdentity;
            if (kind != AnimationSlotBlendFramePlanKind.InertialContinue &&
                (!m_HasCompletedFrame || m_LastAvailability != PoseSlotFrameAvailability.Pose || historyCompletion == 0))
            {
                throw new InvalidOperationException("Inertial capture requires a completed Pose history frame.");
            }

            AnimationSlotBlendFramePlanPreparation preparation = m_SlotWorkspace.PrepareInactivePage(
                in finalWriteBinding,
                kind,
                m_Slot.OutputPolicy,
                m_Rig.ScalePolicy,
                PoseSlotFrameAvailability.Pose,
                outputWeight,
                2,
                m_ContinuityIdentity,
                historyCompletion);
            try
            {
                float leftEnvelope = WriteInertialBonePlans(preparation, entry, curve, profile, baseBoneWeights);
                float rightEnvelope = GetDiagnosticInertialEnvelope(entry, curve, profile, m_Rig.RightFootBoneIndex);
                float inertialLeftWeight = (1f - leftEnvelope) * baseBoneWeights[m_Rig.LeftFootBoneIndex];
                float liveLeftWeight = leftEnvelope;
                float inertialRightWeight = (1f - rightEnvelope) * baseBoneWeights[m_Rig.RightFootBoneIndex];
                float liveRightWeight = rightEnvelope;

                m_SlotWorkspace.SetPreparedEntry(
                    preparation,
                    0,
                    new AnimationSlotBlendFramePlanEntry(
                        -1,
                        -1,
                        0,
                        AnimationPoseContributionKind.Inertial,
                        -1,
                        m_HasPendingInertialCapture
                            ? m_PendingInertialContributionIdentity
                            : RequireInertialContributionIdentity(),
                        inertialScalarWeight,
                        inertialLeftWeight,
                        inertialRightWeight));
                m_SlotWorkspace.SetPreparedEntry(
                    preparation,
                    1,
                    new AnimationSlotBlendFramePlanEntry(
                        captureIndex,
                        physical.Index.Value,
                        physical.Generation,
                        AnimationPoseContributionKind.Live,
                        entry.ProgramProducerIndex,
                        entry.ContributionContinuityIdentity,
                        liveScalarWeight,
                        liveLeftWeight,
                        liveRightWeight));
                for (int boneIndex = 0; boneIndex < m_Rig.Bones.Count; boneIndex++)
                {
                    float envelope = GetDiagnosticInertialEnvelope(entry, curve, profile, boneIndex);
                    m_SlotWorkspace.SetPreparedDenseBoneWeight(
                        preparation,
                        0,
                        boneIndex,
                        (1f - envelope) * baseBoneWeights[boneIndex]);
                    m_SlotWorkspace.SetPreparedDenseBoneWeight(preparation, 1, boneIndex, envelope);
                    m_PlannedEntryMaximumWeights[0] = Mathf.Max(m_PlannedEntryMaximumWeights[0], envelope);
                }
                for (int parameterIndex = 0; parameterIndex < m_SlotWorkspace.ParameterCount; parameterIndex++)
                    m_SlotWorkspace.SetPreparedInertialParameterResidualWeight(preparation, parameterIndex, outputResidualWeight);
                m_SlotWorkspace.ValidateInactivePage(preparation);
                m_SlotWorkspace.CommitInactivePage(preparation);
            }
            catch
            {
                m_SlotWorkspace.AbortInactivePage(preparation);
                throw;
            }
        }

        float WriteInertialBonePlans(
            AnimationSlotBlendFramePlanPreparation preparation,
            AnimationBlendEntryState entry,
            AnimationBlendCurvePayload curve,
            AnimationBlendProfilePayload profile,
            float[] baseBoneWeights)
        {
            float leftEnvelope = 0f;
            for (int boneIndex = 0; boneIndex < m_Rig.Bones.Count; boneIndex++)
            {
                float duration = entry.GetBoneDuration(boneIndex, profile);
                EvaluateInertialEnvelope(entry.GetBoneNormalizedTime(boneIndex, profile), duration, curve,
                    out float envelope, out float residualWeight, out float residualDerivative);
                float residualTime = duration <= 0f
                    ? 0f
                    : entry.GetBoneNormalizedTime(boneIndex, profile) * duration;
                m_SlotWorkspace.SetPreparedInertialBone(
                    preparation,
                    boneIndex,
                    new AnimationSlotBlendInertialBonePlan(residualWeight, residualTime, residualDerivative));
                if (boneIndex == m_Rig.LeftFootBoneIndex)
                    leftEnvelope = Mathf.Clamp01(envelope);
                if (!float.IsFinite(baseBoneWeights[boneIndex]) || baseBoneWeights[boneIndex] < 0f || baseBoneWeights[boneIndex] > 1f)
                    throw new InvalidOperationException("Inertial captured Bone output weight is invalid.");
            }
            return leftEnvelope;
        }

        void CacheCompletedOutput(in AnimationPoseSlotNativeWriteBinding output)
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
            m_HasInertialBlend = false;
            m_HasPendingStoredCapture = false;
            m_PendingStoredContributionIdentity = 0;
            ClearPendingCaptureOutput();
        }

        void CommitPendingInertialCapture()
        {
            if (m_PendingPlanKind != AnimationSlotBlendFramePlanKind.InertialCapture &&
                m_PendingPlanKind != AnimationSlotBlendFramePlanKind.InertialRebase)
            {
                throw new InvalidOperationException("Inertial capture completed with the wrong frame plan kind.");
            }
            AnimationBlendEntryState entry = m_PendingInertialEntry;
            int captureIndex = m_PendingInertialSourceCaptureIndex;
            m_InertialOutputWeight = m_PendingCaptureOutputWeight;
            Array.Copy(m_PendingCaptureBoneOutputWeights, m_InertialBoneOutputWeights, m_InertialBoneOutputWeights.Length);
            ClearEntries();
            AddEntry(entry, captureIndex);
            m_HasInertialBlend = true;
            m_HasStoredPose = false;
            m_HasPendingInertialCapture = false;
            m_PendingInertialEntry = default;
            m_PendingInertialSourceCaptureIndex = -1;
            m_PendingInertialContributionIdentity = 0;
            ClearPendingCaptureOutput();
        }

        void RetireCompletedHistory()
        {
            if (IsInertial(m_PendingPlanKind))
                return;
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
                    m_Slot.PoseSlotId,
                    request.SourceId,
                    request.TargetEmpty,
                    request.PresentationRequestSequence),
                request.ProgramProducerIndex,
                request.Transition.Technique,
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
            if (request.AnimationChannelId != m_Slot.AnimationChannelId || request.PoseSlotId != m_Slot.PoseSlotId)
                throw new InvalidOperationException("Animation Blend push was routed to the wrong slot.");
            RequireTarget(request.SourceId, request.TargetEmpty, request.ProgramProducerIndex, request.PresentationRequestSequence);
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload exact = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                request.ProgramProducerIndex,
                request.TargetEmpty);
            if (!ReferenceEquals(exact, request.Transition) ||
                exact.GetIdentity(m_Slot.PoseSlotId) != request.Transition.GetIdentity(m_Slot.PoseSlotId))
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
            if (m_HasPendingInertialCapture)
            {
                return m_PendingInertialEntry.IsEmpty == targetEmpty &&
                       m_PendingInertialEntry.ProgramProducerIndex == programProducerIndex &&
                       (targetEmpty || m_PendingInertialEntry.SourceId.Equals(sourceId));
            }
            if (m_EntryCount == 0)
                return targetEmpty;
            AnimationBlendEntryState current = m_Entries[m_EntryCount - 1];
            return current.IsEmpty == targetEmpty &&
                   current.ProgramProducerIndex == programProducerIndex &&
                   (targetEmpty || current.SourceId.Equals(sourceId));
        }

        void GetCurrentEndpoint(out int producerIndex, out bool empty)
        {
            if (m_HasPendingInertialCapture)
            {
                producerIndex = m_PendingInertialEntry.ProgramProducerIndex;
                empty = false;
                return;
            }
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

        AnimationSlotBlendFramePlanKind ResolvePlanKind()
        {
            if (m_HasPendingInertialCapture)
                return m_HasInertialBlend
                    ? AnimationSlotBlendFramePlanKind.InertialRebase
                    : AnimationSlotBlendFramePlanKind.InertialCapture;
            if (m_HasPendingStoredCapture)
                return AnimationSlotBlendFramePlanKind.StoredCapture;
            return m_HasInertialBlend
                ? AnimationSlotBlendFramePlanKind.InertialContinue
                : AnimationSlotBlendFramePlanKind.CrossFade;
        }

        void CapturePendingOutput(ulong storedContributionIdentity)
        {
            m_PendingCaptureOutputWeight = m_LastOutputWeight;
            Array.Copy(m_LastBoneOutputWeights, m_PendingCaptureBoneOutputWeights, m_LastBoneOutputWeights.Length);
            m_PendingStoredContributionIdentity = storedContributionIdentity;
            m_HasPendingStoredCapture = true;
        }

        void RequireCapturableFrame()
        {
            if (!m_HasCompletedFrame || m_LastAvailability != PoseSlotFrameAvailability.Pose ||
                m_LastCompletionIdentity == 0)
            {
                throw new InvalidOperationException("Animation Blend Stack has no completed Pose frame to capture.");
            }
        }

        AnimationPhysicalSourceIdentity RequirePhysicalSource(
            AnimationPoseSourcePhysicalRegistry physicalSources,
            AnimationBlendEntryState entry)
        {
            AnimationPhysicalSourceIdentity identity = physicalSources.RequireIdentity(entry.SourceId);
            if (physicalSources.RequirePoseSlotId(identity) != m_Slot.PoseSlotId ||
                physicalSources.RequireProgramProducerIndex(identity) != entry.ProgramProducerIndex)
            {
                throw new InvalidOperationException("Animation physical source is routed to the wrong Blend Stack entry.");
            }
            return identity;
        }

        AnimationBlendEntryState RequireSingleInertialEntry()
        {
            if (!m_HasInertialBlend || m_EntryCount != 1 || m_Entries[0].IsEmpty)
                throw new InvalidOperationException("Animation Blend Stack Inertial state has no single live target.");
            return m_Entries[0];
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
            if (m_HasPendingInertialCapture && m_PendingInertialEntry.SourceId.Equals(sourceId))
                return m_PendingInertialSourceCaptureIndex;
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

        float GetDiagnosticInertialEnvelope(
            AnimationBlendEntryState entry,
            AnimationBlendCurvePayload curve,
            AnimationBlendProfilePayload profile,
            int boneIndex)
        {
            EvaluateInertialEnvelope(
                entry.GetBoneNormalizedTime(boneIndex, profile),
                entry.GetBoneDuration(boneIndex, profile),
                curve,
                out float envelope,
                out _,
                out _);
            return Mathf.Clamp01(envelope);
        }

        static void EvaluateInertialEnvelope(
            float normalizedTime,
            float durationSeconds,
            AnimationBlendCurvePayload curve,
            out float envelope,
            out float residualWeight,
            out float residualDerivativePerSecond)
        {
            float s = Mathf.Clamp01(normalizedTime);
            if (durationSeconds <= 0f)
            {
                envelope = 1f;
                residualWeight = 0f;
                residualDerivativePerSecond = 0f;
                return;
            }
            float c = AnimationBlendCurveEvaluator.Evaluate(curve, s);
            float derivative = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, s);
            float c0 = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, 0f);
            float c1 = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, 1f);
            float s2 = s * s;
            float s3 = s2 * s;
            float h10 = s3 - 2f * s2 + s;
            float h11 = s3 - s2;
            float h10Derivative = 3f * s2 - 4f * s + 1f;
            float h11Derivative = 3f * s2 - 2f * s;
            envelope = c - c0 * h10 - c1 * h11;
            float envelopeDerivative = derivative - c0 * h10Derivative - c1 * h11Derivative;
            residualWeight = 1f - envelope;
            residualDerivativePerSecond = -envelopeDerivative / durationSeconds;
            if (!float.IsFinite(envelope) || !float.IsFinite(residualWeight) ||
                !float.IsFinite(residualDerivativePerSecond))
            {
                throw new InvalidOperationException("Animation Inertial envelope is non-finite.");
            }
        }

        ulong RequireStoredContributionIdentity()
        {
            AnimationSlotBlendPoseWorkspaceBinding binding = m_SlotWorkspace.RequireActiveBinding();
            AnimationSlotBlendStoredPoseNativeState state = binding.StoredPose.State[0];
            if (state.Active != 1 || state.ContributionContinuityIdentity == 0)
                throw new InvalidOperationException("Animation Stored Pose Native state is unavailable.");
            return state.ContributionContinuityIdentity;
        }

        ulong RequireInertialContributionIdentity()
        {
            AnimationSlotBlendPoseWorkspaceBinding binding = m_SlotWorkspace.RequireActiveBinding();
            AnimationSlotBlendInertialNativeState state = binding.Inertial.State[0];
            if (state.Active != 1 || state.ContributionContinuityIdentity == 0)
                throw new InvalidOperationException("Animation Inertial Native state is unavailable.");
            return state.ContributionContinuityIdentity;
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

        int CopyReferencedSourceIds(AnimationPoseSourceId[] destination)
        {
            int count = CopyEntrySourceIds(destination);
            if (m_HasPendingInertialCapture)
                count = AppendUniqueSourceId(destination, count, m_PendingInertialEntry.SourceId);
            return count;
        }

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
            return m_HasPendingInertialCapture && m_PendingInertialEntry.SourceId.Equals(sourceId);
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
                    m_Slot.PoseSlotId,
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
            m_HasPendingInertialCapture = false;
            m_PendingInertialEntry = default;
            m_PendingInertialSourceCaptureIndex = -1;
            m_PendingStoredContributionIdentity = 0;
            m_PendingInertialContributionIdentity = 0;
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
            Array.Clear(m_EntryBoneWeights, 0, m_EntryBoneWeights.Length);
            Array.Clear(m_PlannedEntryMaximumWeights, 0, m_PlannedEntryMaximumWeights.Length);
            m_PlannedStoredMaximumWeight = 0f;
        }

        void RequireNoPreparedPlan()
        {
            if (m_PendingPlanCompletionIdentity != 0)
                throw new InvalidOperationException("Animation Blend Stack frame plan must complete before state changes.");
            if (m_HasCompletedFrame && m_LastAvailability == PoseSlotFrameAvailability.Invalid)
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

        static bool IsInertial(AnimationSlotBlendFramePlanKind kind) =>
            kind == AnimationSlotBlendFramePlanKind.InertialContinue ||
            kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
            kind == AnimationSlotBlendFramePlanKind.InertialRebase;

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
