using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendStackRuntime
    {
        readonly AnimationBlendSlotPayload m_Slot;
        readonly AnimationBlendCurveCatalogPayload m_CurveCatalog;
        readonly AnimationBlendProfileCatalogPayload m_ProfileCatalog;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly AnimationBlendEntryState[] m_Entries;
        readonly AnimationBlendEntryState[] m_CompactedEntries;
        readonly AnimationPlaybackId[] m_RemovedPlaybackIds;
        readonly AnimationPlaybackId[] m_StackReleasedPlaybackIds;
        readonly AnimationBlendSourcePoseWorkspace m_Sources;
        readonly AnimationSlotBlendPoseEvaluator m_Evaluator;

        int m_EntryCount;
        int m_StackReleasedHead;
        int m_StackReleasedCount;
        ulong m_LastRequestSequence;
        ulong m_LastCompletionIdentity;
        ulong m_LastContributionContinuityIdentity;
        ulong m_ContinuityIdentity = 1;
        float m_ElapsedSinceEvaluation;
        bool m_HasPendingInertialCapture;
        AnimationBlendEntryState m_PendingInertialEntry;
        ulong m_PendingInertialContributionIdentity;

        public AnimationBlendStackRuntime(
            AnimationBlendSlotPayload slot,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            CharacterAnimationRigPayload rig,
            int parameterCount)
        {
            m_Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            m_CurveCatalog = curveCatalog ?? throw new ArgumentNullException(nameof(curveCatalog));
            m_ProfileCatalog = profileCatalog ?? throw new ArgumentNullException(nameof(profileCatalog));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            if (!slot.PoseSlotId.IsValid || !slot.AnimationChannelId.IsValid ||
                !Enum.IsDefined(typeof(PoseSlotOutputPolicy), slot.OutputPolicy) ||
                slot.StackPolicy == null || curveCatalog.Entries.Count == 0 ||
                profileCatalog.Entries.Count == 0 || parameterCount < 0)
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
            m_Entries = new AnimationBlendEntryState[capacity];
            m_CompactedEntries = new AnimationBlendEntryState[capacity];
            m_RemovedPlaybackIds = new AnimationPlaybackId[capacity + 1];
            m_StackReleasedPlaybackIds = new AnimationPlaybackId[capacity + 1];
            m_Sources = new AnimationBlendSourcePoseWorkspace(rig, parameterCount, capacity + 1);
            m_Evaluator = new AnimationSlotBlendPoseEvaluator(rig, parameterCount, capacity);
        }

        public PoseSlotId PoseSlotId => m_Slot.PoseSlotId;
        public AnimationChannelId AnimationChannelId => m_Slot.AnimationChannelId;
        public PoseSlotOutputPolicy OutputPolicy => m_Slot.OutputPolicy;
        public int EntryCount => m_EntryCount;
        public bool HasStoredPose => m_Evaluator.StoredPose.Active;
        public bool HasInertialBlend => m_Evaluator.Inertial.Active;
        public bool HasPendingInertialCapture => m_HasPendingInertialCapture;
        public bool HasFrame => m_Evaluator.HasFrame;
        public PoseSlotFrame CurrentFrame => m_Evaluator.CurrentFrame;
        public ulong ContinuityIdentity => m_ContinuityIdentity;

        public AnimationBlendEntryId GetEntryId(int index)
        {
            if ((uint)index >= (uint)m_EntryCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Entries[index].EntryId;
        }

        public void BeginSourceFrame(ulong completionIdentity) => m_Sources.BeginFrame(completionIdentity);

        public void WriteSource(
            AnimationPlaybackId playbackId,
            int programProducerIndex,
            IReadOnlyList<AnimationLocalBonePose> denseLocalPose,
            IReadOnlyList<AnimationBlendBoneVelocity> denseVelocity,
            IReadOnlyList<float> poseParameters,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            float visualTimeScale)
        {
            m_Sources.WriteSource(
                playbackId,
                programProducerIndex,
                denseLocalPose,
                denseVelocity,
                poseParameters,
                leftFootFeatures,
                rightFootFeatures,
                hasFootFeatures,
                visualTimeScale);
        }

        public AnimationBlendPushResult PushTarget(
            AnimationPlaybackId playbackId,
            bool targetEmpty,
            int programProducerIndex,
            ulong presentationRequestSequence)
        {
            RequireTarget(playbackId, targetEmpty, programProducerIndex, presentationRequestSequence);
            if (IsCurrentTarget(playbackId, targetEmpty, programProducerIndex))
            {
                ContinueCurrentTarget(presentationRequestSequence);
                return AnimationBlendPushResult.ContinuedSource;
            }
            if (m_HasPendingInertialCapture)
                throw new InvalidOperationException("Animation Blend pending Inertial capture must complete before another push.");
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload transition = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                programProducerIndex,
                targetEmpty);
            return Push(new AnimationBlendPushRequest(
                m_Slot.AnimationChannelId,
                m_Slot.PoseSlotId,
                playbackId,
                targetEmpty,
                programProducerIndex,
                presentationRequestSequence,
                transition));
        }

        public AnimationBlendPushResult Push(AnimationBlendPushRequest request)
        {
            RequireRequestRouteAndTarget(request);
            if (m_HasPendingInertialCapture)
            {
                if (IsCurrentTarget(request.PlaybackId, request.TargetEmpty, request.ProgramProducerIndex))
                {
                    ContinueCurrentTarget(request.PresentationRequestSequence);
                    return AnimationBlendPushResult.ContinuedSource;
                }
                throw new InvalidOperationException("Animation Blend pending Inertial capture must complete before another push.");
            }
            RequireRequest(request);
            if (IsCurrentTarget(request.PlaybackId, request.TargetEmpty, request.ProgramProducerIndex))
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

        public void Advance(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            float elapsedSinceEvaluation = m_ElapsedSinceEvaluation + deltaSeconds;
            if (!float.IsFinite(elapsedSinceEvaluation))
                throw new InvalidOperationException("Animation Blend evaluation clock overflowed.");
            for (int i = 0; i < m_EntryCount; i++)
            {
                m_CompactedEntries[i] = m_Entries[i];
                m_CompactedEntries[i].Advance(deltaSeconds);
            }
            AnimationBlendEntryState pendingInertialEntry = m_PendingInertialEntry;
            if (m_HasPendingInertialCapture)
                pendingInertialEntry.Advance(deltaSeconds);
            Array.Copy(m_CompactedEntries, 0, m_Entries, 0, m_EntryCount);
            Array.Clear(m_CompactedEntries, 0, m_EntryCount);
            if (m_HasPendingInertialCapture)
                m_PendingInertialEntry = pendingInertialEntry;
            m_ElapsedSinceEvaluation = elapsedSinceEvaluation;
        }

        public PoseSlotFrame Evaluate(
            ulong completionIdentity,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (completionIdentity == m_LastCompletionIdentity)
            {
                invalidReason = AnimationBlendStackInvalidReason.DuplicateCompletion;
                return m_Evaluator.CurrentFrame;
            }
            if (m_HasPendingInertialCapture)
            {
                if (m_Sources.CompletionIdentity != completionIdentity)
                {
                    return FailPendingInertialCapture(
                        completionIdentity,
                        AnimationBlendStackInvalidReason.SourceFrameNotPrepared,
                        out invalidReason);
                }
                if (!m_Sources.TryGet(m_PendingInertialEntry.PlaybackId, out AnimationBlendSourcePoseFrame target) ||
                    target.ProgramProducerIndex != m_PendingInertialEntry.ProgramProducerIndex)
                {
                    return FailPendingInertialCapture(
                        completionIdentity,
                        AnimationBlendStackInvalidReason.MissingLiveSource,
                        out invalidReason);
                }
                CommitPendingInertialCapture(target);
            }
            PoseSlotFrame frame = m_Evaluator.Evaluate(
                m_Slot.PoseSlotId,
                m_Slot.OutputPolicy,
                m_Entries,
                m_EntryCount,
                m_Sources,
                m_CurveCatalog,
                m_ProfileCatalog,
                completionIdentity,
                m_ContinuityIdentity,
                m_ElapsedSinceEvaluation,
                out invalidReason);
            m_LastCompletionIdentity = completionIdentity;
            m_ElapsedSinceEvaluation = 0f;
            if (invalidReason == AnimationBlendStackInvalidReason.None)
                RetireCompletedHistory();
            return frame;
        }

        public bool TryDequeueStackReleasedPlayback(out AnimationPlaybackId playbackId)
        {
            if (m_StackReleasedCount == 0)
            {
                playbackId = default;
                return false;
            }
            playbackId = m_StackReleasedPlaybackIds[m_StackReleasedHead];
            m_StackReleasedPlaybackIds[m_StackReleasedHead] = default;
            m_StackReleasedHead = (m_StackReleasedHead + 1) % m_StackReleasedPlaybackIds.Length;
            m_StackReleasedCount--;
            return true;
        }

        public void Reset()
        {
            int removedCount = CopyEntryPlaybackIds(m_RemovedPlaybackIds);
            if (m_HasPendingInertialCapture)
                removedCount = AppendUniquePlaybackId(
                    m_RemovedPlaybackIds,
                    removedCount,
                    m_PendingInertialEntry.PlaybackId);
            RequireStackReleaseQueueCapacity(m_RemovedPlaybackIds, removedCount, default);
            RequireCanAdvanceContinuityIdentity();
            Array.Clear(m_Entries, 0, m_Entries.Length);
            m_EntryCount = 0;
            m_Evaluator.Reset();
            ClearPendingInertialCapture();
            for (int i = 0; i < removedCount; i++)
                EnqueueStackReleased(m_RemovedPlaybackIds[i]);
            m_LastRequestSequence = 0;
            m_LastCompletionIdentity = 0;
            m_ElapsedSinceEvaluation = 0f;
            AdvanceContinuityIdentity();
        }

        AnimationBlendPushResult PushCrossFade(AnimationBlendPushRequest request)
        {
            bool startsNewContinuity = !request.TargetEmpty &&
                                       (!m_Evaluator.HasFrame ||
                                        m_Evaluator.CurrentFrame.Availability != PoseSlotFrameAvailability.Pose);
            if (startsNewContinuity)
                RequireCanAdvanceContinuityIdentity();
            bool replaceHistory = m_EntryCount == m_Entries.Length ||
                                  m_EntryCount > 0 && m_Entries[m_EntryCount - 1].ElapsedSeconds <=
                                  m_Slot.StackPolicy.MaxBlendInTimeToReplaceNewest;
            bool capturedStoredPose = false;
            AnimationBlendEntryState newEntry;
            if (replaceHistory)
            {
                RequireCapturableFrame();
                int removedCount = CopyEntryPlaybackIds(m_RemovedPlaybackIds);
                RequireStackReleaseQueueCapacity(m_RemovedPlaybackIds, removedCount, request.PlaybackId);
                ulong storedContributionIdentity = m_Evaluator.CurrentFrame.Availability == PoseSlotFrameAvailability.Pose
                    ? AllocateContributionContinuityIdentity()
                    : 0;
                newEntry = CreateEntry(request, AllocateContributionContinuityIdentity());
                if (m_Evaluator.CurrentFrame.Availability == PoseSlotFrameAvailability.Pose)
                {
                    m_Evaluator.CaptureStoredPose(storedContributionIdentity);
                    capturedStoredPose = true;
                }
                else
                {
                    m_Evaluator.StoredPose.Clear();
                }
                Array.Clear(m_Entries, 0, m_Entries.Length);
                m_EntryCount = 0;
                for (int i = 0; i < removedCount; i++)
                {
                    if (!m_RemovedPlaybackIds[i].Equals(request.PlaybackId))
                        EnqueueStackReleased(m_RemovedPlaybackIds[i]);
                }
            }
            else
            {
                newEntry = CreateEntry(request, AllocateContributionContinuityIdentity());
                for (int i = 0; i < m_EntryCount; i++)
                {
                    m_CompactedEntries[i] = m_Entries[i];
                    m_CompactedEntries[i].IncreasePushDepth(m_Slot.StackPolicy.DepthBlendTimeMultiplier);
                }
                Array.Copy(m_CompactedEntries, 0, m_Entries, 0, m_EntryCount);
                Array.Clear(m_CompactedEntries, 0, m_EntryCount);
            }
            AddEntry(newEntry);
            if (startsNewContinuity)
                AdvanceContinuityIdentity();
            return capturedStoredPose
                ? AnimationBlendPushResult.CapturedStoredPose
                : AnimationBlendPushResult.Pushed;
        }

        AnimationBlendPushResult PushInertial(AnimationBlendPushRequest request)
        {
            if (request.TargetEmpty || request.Transition.SourceEmpty)
                throw new InvalidOperationException("Inertial Blend requires live source and target endpoints.");
            RequireCapturableFrame();
            if (m_Evaluator.CurrentFrame.Availability != PoseSlotFrameAvailability.Pose)
                throw new InvalidOperationException("Inertial Blend requires a completed Pose frame.");
            int removedCount = CopyEntryPlaybackIds(m_RemovedPlaybackIds);
            removedCount = AppendUniquePlaybackId(m_RemovedPlaybackIds, removedCount, request.PlaybackId);
            RequireStackReleaseQueueCapacity(m_RemovedPlaybackIds, removedCount, default);
            ulong inertialContributionIdentity = AllocateContributionContinuityIdentity();
            ulong entryContributionIdentity = AllocateContributionContinuityIdentity();
            m_PendingInertialEntry = CreateEntry(request, entryContributionIdentity);
            m_PendingInertialContributionIdentity = inertialContributionIdentity;
            m_HasPendingInertialCapture = true;
            CancelStackReleased(request.PlaybackId);
            return AnimationBlendPushResult.RebasedInertial;
        }

        void CommitPendingInertialCapture(AnimationBlendSourcePoseFrame target)
        {
            int removedCount = CopyEntryPlaybackIds(m_RemovedPlaybackIds);
            m_Evaluator.BeginInertial(target, m_PendingInertialContributionIdentity);
            Array.Clear(m_Entries, 0, m_Entries.Length);
            m_EntryCount = 0;
            AddEntry(m_PendingInertialEntry);
            for (int i = 0; i < removedCount; i++)
            {
                if (!m_RemovedPlaybackIds[i].Equals(target.PlaybackId))
                    EnqueueStackReleased(m_RemovedPlaybackIds[i]);
            }
            ClearPendingInertialCapture();
        }

        PoseSlotFrame FailPendingInertialCapture(
            ulong completionIdentity,
            AnimationBlendStackInvalidReason reason,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            AnimationPlaybackId targetPlaybackId = m_PendingInertialEntry.PlaybackId;
            if (!IsEntryPlaybackReferenced(targetPlaybackId))
                EnqueueStackReleased(targetPlaybackId);
            ClearPendingInertialCapture();
            PoseSlotFrame frame = m_Evaluator.PublishInvalid(
                m_Slot.PoseSlotId,
                completionIdentity,
                m_ContinuityIdentity,
                reason,
                out invalidReason);
            m_LastCompletionIdentity = completionIdentity;
            m_ElapsedSinceEvaluation = 0f;
            return frame;
        }

        void ClearPendingInertialCapture()
        {
            m_HasPendingInertialCapture = false;
            m_PendingInertialEntry = default;
            m_PendingInertialContributionIdentity = 0;
        }

        void RetireCompletedHistory()
        {
            if (m_Evaluator.Inertial.Active)
            {
                if (m_EntryCount == 1 && m_Entries[0].IsComplete(
                        m_Rig.Bones.Count,
                        m_ProfileCatalog.Require(m_Entries[0].BlendProfileIndex)))
                    m_Evaluator.Inertial.Clear();
                return;
            }
            if (m_Evaluator.StoredPose.Active && m_Evaluator.StoredMaximumWeight <= 0f)
                m_Evaluator.StoredPose.Clear();
            if (m_EntryCount <= 1)
                return;

            int keptCount = 0;
            int removedCount = 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                bool keep = i == m_EntryCount - 1 || m_Evaluator.GetEntryMaximumWeight(i) > 0f;
                if (keep)
                {
                    m_CompactedEntries[keptCount++] = m_Entries[i];
                }
                else if (!m_Entries[i].IsEmpty)
                {
                    m_RemovedPlaybackIds[removedCount++] = m_Entries[i].PlaybackId;
                }
            }
            int unreferencedCount = 0;
            for (int i = 0; i < removedCount; i++)
            {
                AnimationPlaybackId playbackId = m_RemovedPlaybackIds[i];
                bool retained = false;
                for (int keptIndex = 0; keptIndex < keptCount; keptIndex++)
                {
                    retained |= !m_CompactedEntries[keptIndex].IsEmpty &&
                                m_CompactedEntries[keptIndex].PlaybackId.Equals(playbackId);
                }
                bool duplicate = false;
                for (int previous = 0; previous < unreferencedCount; previous++)
                    duplicate |= m_RemovedPlaybackIds[previous].Equals(playbackId);
                if (!retained && !duplicate)
                    m_RemovedPlaybackIds[unreferencedCount++] = playbackId;
            }
            removedCount = unreferencedCount;
            RequireStackReleaseQueueCapacity(m_RemovedPlaybackIds, removedCount, default);
            Array.Clear(m_Entries, 0, m_Entries.Length);
            Array.Copy(m_CompactedEntries, 0, m_Entries, 0, keptCount);
            Array.Clear(m_CompactedEntries, 0, m_CompactedEntries.Length);
            m_EntryCount = keptCount;
            for (int i = 0; i < removedCount; i++)
                EnqueueStackReleased(m_RemovedPlaybackIds[i]);
        }

        AnimationBlendEntryState CreateEntry(
            AnimationBlendPushRequest request,
            ulong contributionContinuityIdentity)
        {
            var entryId = new AnimationBlendEntryId(
                m_Slot.PoseSlotId,
                request.PlaybackId,
                request.TargetEmpty,
                request.PresentationRequestSequence);
            return new AnimationBlendEntryState(
                entryId,
                request.ProgramProducerIndex,
                request.Transition.Technique,
                request.Transition.DurationSeconds,
                request.Transition.CurveIndex,
                request.Transition.BlendProfileIndex,
                contributionContinuityIdentity);
        }

        void AddEntry(AnimationBlendEntryState entry)
        {
            if (m_EntryCount == m_Entries.Length)
                throw new InvalidOperationException("Animation Blend Stack capacity was exceeded without Stored Pose capture.");
            m_Entries[m_EntryCount++] = entry;
            if (!entry.IsEmpty)
                CancelStackReleased(entry.PlaybackId);
        }

        void RequireRequest(AnimationBlendPushRequest request)
        {
            RequireRequestRouteAndTarget(request);
            GetCurrentEndpoint(out int sourceProducerIndex, out bool sourceEmpty);
            AnimationBlendTransitionPayload exact = m_Slot.RequireTransition(
                sourceProducerIndex,
                sourceEmpty,
                request.ProgramProducerIndex,
                request.TargetEmpty);
            if (!ReferenceEquals(exact, request.Transition))
                throw new InvalidOperationException("Animation Blend push did not use the compiled exact transition.");
        }

        void RequireRequestRouteAndTarget(AnimationBlendPushRequest request)
        {
            if (request.AnimationChannelId != m_Slot.AnimationChannelId || request.PoseSlotId != m_Slot.PoseSlotId)
                throw new InvalidOperationException("Animation Blend push was routed to the wrong slot.");
            RequireTarget(
                request.PlaybackId,
                request.TargetEmpty,
                request.ProgramProducerIndex,
                request.PresentationRequestSequence);
        }

        static void RequireTarget(
            AnimationPlaybackId playbackId,
            bool targetEmpty,
            int programProducerIndex,
            ulong presentationRequestSequence)
        {
            if (presentationRequestSequence == 0 || targetEmpty == playbackId.IsValid ||
                targetEmpty == (programProducerIndex >= 0))
            {
                throw new ArgumentException("Animation Blend target identity is invalid.");
            }
        }

        bool IsCurrentTarget(
            AnimationPlaybackId playbackId,
            bool targetEmpty,
            int programProducerIndex)
        {
            if (m_HasPendingInertialCapture)
            {
                return m_PendingInertialEntry.IsEmpty == targetEmpty &&
                       m_PendingInertialEntry.ProgramProducerIndex == programProducerIndex &&
                       (targetEmpty || m_PendingInertialEntry.PlaybackId.Equals(playbackId));
            }
            if (m_EntryCount == 0)
                return targetEmpty;
            AnimationBlendEntryState current = m_Entries[m_EntryCount - 1];
            return current.IsEmpty == targetEmpty &&
                   current.ProgramProducerIndex == programProducerIndex &&
                   (targetEmpty || current.PlaybackId.Equals(playbackId));
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

        void RequireCapturableFrame()
        {
            if (!m_Evaluator.HasFrame || m_Evaluator.CurrentFrame.Availability == PoseSlotFrameAvailability.Invalid)
                throw new InvalidOperationException("Animation Blend Stack has no valid completed frame to capture.");
        }

        int CopyEntryPlaybackIds(AnimationPlaybackId[] destination)
        {
            int count = 0;
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (m_Entries[i].IsEmpty)
                    continue;
                AnimationPlaybackId playbackId = m_Entries[i].PlaybackId;
                bool duplicate = false;
                for (int j = 0; j < count; j++)
                    duplicate |= destination[j].Equals(playbackId);
                if (!duplicate)
                    destination[count++] = playbackId;
            }
            return count;
        }

        static int AppendUniquePlaybackId(
            AnimationPlaybackId[] destination,
            int count,
            AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
                return count;
            for (int i = 0; i < count; i++)
            {
                if (destination[i].Equals(playbackId))
                    return count;
            }
            if (count == destination.Length)
                throw new InvalidOperationException("Animation Blend playback reference workspace was exceeded.");
            destination[count] = playbackId;
            return count + 1;
        }

        bool IsEntryPlaybackReferenced(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_EntryCount; i++)
            {
                if (!m_Entries[i].IsEmpty && m_Entries[i].PlaybackId.Equals(playbackId))
                    return true;
            }
            return false;
        }

        void EnqueueStackReleased(AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
                return;
            for (int i = 0; i < m_StackReleasedCount; i++)
            {
                int index = (m_StackReleasedHead + i) % m_StackReleasedPlaybackIds.Length;
                if (m_StackReleasedPlaybackIds[index].Equals(playbackId))
                    return;
            }
            if (m_StackReleasedCount == m_StackReleasedPlaybackIds.Length)
                throw new InvalidOperationException("Animation Blend source retirement queue was not drained.");
            int tail = (m_StackReleasedHead + m_StackReleasedCount) % m_StackReleasedPlaybackIds.Length;
            m_StackReleasedPlaybackIds[tail] = playbackId;
            m_StackReleasedCount++;
        }

        void CancelStackReleased(AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
                return;
            for (int i = 0; i < m_StackReleasedCount; i++)
            {
                int index = (m_StackReleasedHead + i) % m_StackReleasedPlaybackIds.Length;
                if (!m_StackReleasedPlaybackIds[index].Equals(playbackId))
                    continue;
                for (int shift = i; shift + 1 < m_StackReleasedCount; shift++)
                {
                    int destination = (m_StackReleasedHead + shift) % m_StackReleasedPlaybackIds.Length;
                    int source = (m_StackReleasedHead + shift + 1) % m_StackReleasedPlaybackIds.Length;
                    m_StackReleasedPlaybackIds[destination] = m_StackReleasedPlaybackIds[source];
                }
                int tail = (m_StackReleasedHead + m_StackReleasedCount - 1) % m_StackReleasedPlaybackIds.Length;
                m_StackReleasedPlaybackIds[tail] = default;
                m_StackReleasedCount--;
                return;
            }
        }

        void RequireStackReleaseQueueCapacity(
            AnimationPlaybackId[] playbackIds,
            int count,
            AnimationPlaybackId retainedPlaybackId)
        {
            int required = 0;
            for (int i = 0; i < count; i++)
            {
                AnimationPlaybackId playbackId = playbackIds[i];
                if (!playbackId.IsValid || playbackId.Equals(retainedPlaybackId))
                    continue;
                bool duplicate = false;
                for (int previous = 0; previous < i; previous++)
                    duplicate |= playbackIds[previous].Equals(playbackId);
                if (duplicate)
                    continue;
                bool queued = false;
                for (int j = 0; j < m_StackReleasedCount; j++)
                {
                    int queueIndex = (m_StackReleasedHead + j) % m_StackReleasedPlaybackIds.Length;
                    queued |= m_StackReleasedPlaybackIds[queueIndex].Equals(playbackId);
                }
                if (!queued)
                    required++;
            }
            if (required > m_StackReleasedPlaybackIds.Length - m_StackReleasedCount)
                throw new InvalidOperationException("Animation Blend source retirement queue was not drained.");
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

        void ContinueCurrentTarget(ulong presentationRequestSequence)
        {
            if (presentationRequestSequence < m_LastRequestSequence)
                throw new InvalidOperationException("Animation Blend continuation request is stale.");
            m_LastRequestSequence = presentationRequestSequence;
        }

        ulong AllocateContributionContinuityIdentity()
        {
            if (m_LastContributionContinuityIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation contribution continuity identity overflowed.");
            return ++m_LastContributionContinuityIdentity;
        }
    }
}
