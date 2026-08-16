using System;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal enum AnimationPoseSourcePrepareKind : byte
    {
        CommittedUpdate = 1,
        PreparedResource = 2
    }

    internal readonly struct AnimationPoseSourceClipBinding
    {
        internal AnimationPoseSourceClipBinding(
            int clipBindingIndex,
            AnimationClip clip)
        {
            if (clipBindingIndex < 0 || !clip ||
                !float.IsFinite(clip.length) || clip.length <= 0f)
                throw new ArgumentException("Animation pose source clip binding is invalid.");
            ClipBindingIndex = clipBindingIndex;
            Clip = clip;
        }

        internal int ClipBindingIndex { get; }
        internal AnimationClip Clip { get; }
        internal bool IsValid => ClipBindingIndex >= 0 && Clip &&
                                 float.IsFinite(Clip.length) && Clip.length > 0f;
    }

    internal readonly struct AnimationPoseSourceReleaseToken
    {
        internal AnimationPoseSourceReleaseToken(int permissionIndex, ulong generation)
        {
            if (permissionIndex < 0 || generation == 0)
                throw new ArgumentException("Animation pose source release token is invalid.");
            PermissionIndex = permissionIndex;
            Generation = generation;
        }

        internal int PermissionIndex { get; }
        internal ulong Generation { get; }
        internal bool IsValid => PermissionIndex >= 0 && Generation != 0;
    }

    internal readonly struct AnimationPoseSourcePrepareResult
    {
        internal AnimationPoseSourcePrepareResult(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            ulong frameIdentity,
            ulong completionIdentity,
            AnimationScriptPlayable output,
            AnimationPoseSourcePrepareKind kind)
        {
            SourceId = sourceId;
            PlayerNodeId = playerNodeId;
            FrameIdentity = frameIdentity;
            CompletionIdentity = completionIdentity;
            Output = output;
            Kind = kind;
            if (!IsValid)
                throw new ArgumentException("Animation pose source prepare result is invalid.");
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal ulong FrameIdentity { get; }
        internal ulong CompletionIdentity { get; }
        internal AnimationScriptPlayable Output { get; }
        internal AnimationPoseSourcePrepareKind Kind { get; }
        internal bool IsPreparedResource => Kind == AnimationPoseSourcePrepareKind.PreparedResource;
        internal bool IsValid => SourceId.IsValid && PlayerNodeId.IsValid &&
                                 FrameIdentity != 0 && CompletionIdentity != 0 &&
                                 Output.IsValid() && Output.GetInputCount() == 1 &&
                                 (Kind == AnimationPoseSourcePrepareKind.CommittedUpdate ||
                                  Kind == AnimationPoseSourcePrepareKind.PreparedResource);
    }

    internal sealed class AnimancerPoseSamplingBackend : IDisposable
    {
        enum SourceFramePhase : byte
        {
            Closed = 0,
            Preparing = 1,
            Validated = 2,
            EvaluateBarrier = 3
        }

        struct SourceFrameMutation
        {
            internal AnimationPlayerSourceKey Key;
            internal SourceVisual Visual;
            internal AnimationPoseSourceCaptureBinding Capture;
            internal AnimationPoseSourcePrepareKind Kind;
            internal int OwnerSlotIndex;
            internal int ClipCount;

            internal bool IsValid => Key.IsValid && Visual != null &&
                                     OwnerSlotIndex >= 0 && ClipCount > 0;

            internal void Clear()
            {
                Key = default;
                Visual = null;
                Capture = default;
                Kind = default;
                OwnerSlotIndex = -1;
                ClipCount = 0;
            }
        }

        struct SourceOwnerSlot
        {
            internal AnimationPlayerSourceKey Key;
            internal SourceVisual Visual;

            internal bool IsOccupied => Visual != null;

            internal void Clear()
            {
                Key = default;
                Visual = null;
            }
        }

        struct SourceReleasePermission
        {
            internal AnimationPlayerSourceKey Key;
            internal SourceVisual Visual;
            internal int OwnerSlotIndex;
            internal ulong Generation;

            internal bool IsValid => Key.IsValid && Visual != null &&
                                     OwnerSlotIndex >= 0 && Generation != 0;

            internal void Clear()
            {
                Key = default;
                Visual = null;
                OwnerSlotIndex = -1;
                Generation = 0;
            }
        }

        readonly AnimancerComponent m_Animancer;
        readonly AnimancerGraph m_Graph;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly Transform m_RootTransform;
        readonly NativeArray<TransformStreamHandle> m_Handles;
        readonly NativeArray<AnimationLocalBonePose> m_ReferencePose;
        readonly NativeArray<int> m_PhysicalParentIndices;
        readonly NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        readonly int m_SourceCapacity;
        readonly int m_ClipCapacity;
        readonly SourceOwnerSlot[] m_SourceOwnerSlots;
        readonly byte[] m_PendingOwnerSlotReservations;
        readonly SourceFrameMutation[] m_FrameMutations;
        readonly ClipSamplePlan[] m_PendingClipPlans;
        readonly ClipState[] m_PendingClipStates;
        readonly float[] m_PendingNormalizedClipWeights;
        readonly SourceReleasePermission[] m_ReleasePermissions;
        readonly SourceVisual[] m_DeferredReleases;

        int m_CommittedSourceCount;
        int m_FrameMutationCount;
        int m_ReleasePermissionEntryCount;
        int m_UnconsumedReleasePermissionCount;
        int m_DeferredReleaseCount;
        ulong m_LastReleasePermissionGeneration;
        ulong m_FrameIdentity;
        SourceFramePhase m_FramePhase;
        bool m_Disposed;

        internal AnimancerPoseSamplingBackend(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            int sourceCapacity,
            int clipCapacity)
        {
            if (!animancer)
                throw new ArgumentNullException(nameof(animancer));
            if (!rigBinding)
                throw new ArgumentNullException(nameof(rigBinding));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            if (clipCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(clipCapacity));
            rig.RequireValid();
            rigBinding.RequireValid(rig);
            if (!animancer.Animator || rigBinding.Animator != animancer.Animator)
                throw new ArgumentException("Animation Rig Binding does not belong to the Animancer Animator.", nameof(rigBinding));
            AnimancerGraph graph = animancer.Graph;
            if (!graph.PlayableGraph.IsValid())
                throw new InvalidOperationException("Animancer pose sampling graph is unavailable.");

            int clipPlanCapacity = checked(sourceCapacity * clipCapacity);
            var sourceOwnerSlots = new SourceOwnerSlot[sourceCapacity];
            var pendingOwnerSlotReservations = new byte[sourceCapacity];
            var frameMutations = new SourceFrameMutation[sourceCapacity];
            var pendingClipPlans = new ClipSamplePlan[clipPlanCapacity];
            var pendingClipStates = new ClipState[clipPlanCapacity];
            var pendingNormalizedClipWeights = new float[clipPlanCapacity];
            var releasePermissions = new SourceReleasePermission[sourceCapacity];
            var deferredReleases = new SourceVisual[sourceCapacity];
            var physicalBones = new Transform[rigBinding.PhysicalBones.Count];
            for (int i = 0; i < physicalBones.Length; i++)
                physicalBones[i] = rigBinding.PhysicalBones[i];

            NativeArray<TransformStreamHandle> handles = default;
            NativeArray<AnimationLocalBonePose> referencePose = default;
            NativeArray<int> physicalParentIndices = default;
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones = default;
            try
            {
                handles = new NativeArray<TransformStreamHandle>(
                    rig.PhysicalBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                referencePose = new NativeArray<AnimationLocalBonePose>(
                    rig.PoseBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                physicalParentIndices = new NativeArray<int>(
                    rig.PhysicalBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                virtualBones = new NativeArray<CharacterVirtualBoneDescriptor>(
                    rig.VirtualBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                for (int boneIndex = 0; boneIndex < rig.PhysicalBoneCount; boneIndex++)
                {
                    handles[boneIndex] = animancer.Animator.BindStreamTransform(rigBinding.PhysicalBones[boneIndex]);
                    CharacterAnimationPhysicalBonePayload bone = rig.PhysicalBones[boneIndex];
                    referencePose[boneIndex] = new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        bone.ReferenceLocalScale);
                    physicalParentIndices[boneIndex] = bone.ParentPhysicalIndex;
                }
                for (int virtualIndex = 0; virtualIndex < rig.VirtualBoneCount; virtualIndex++)
                {
                    CharacterAnimationVirtualBonePayload bone = rig.VirtualBones[virtualIndex];
                    referencePose[bone.PoseBoneIndex] = new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        Vector3.one);
                    virtualBones[virtualIndex] = new CharacterVirtualBoneDescriptor(
                        new CharacterPoseBoneRuntimeId(bone.VirtualBoneId),
                        bone.SourcePhysicalBoneIndex,
                        bone.TargetPhysicalBoneIndex,
                        bone.PoseBoneIndex);
                }
            }
            catch
            {
                if (virtualBones.IsCreated)
                    virtualBones.Dispose();
                if (physicalParentIndices.IsCreated)
                    physicalParentIndices.Dispose();
                if (referencePose.IsCreated)
                    referencePose.Dispose();
                if (handles.IsCreated)
                    handles.Dispose();
                throw;
            }

            m_Animancer = animancer;
            m_Graph = graph;
            m_Rig = rig;
            m_RootTransform =
                physicalBones[rig.RootPhysicalBoneIndex];
            m_Handles = handles;
            m_ReferencePose = referencePose;
            m_PhysicalParentIndices = physicalParentIndices;
            m_VirtualBones = virtualBones;
            m_SourceCapacity = sourceCapacity;
            m_ClipCapacity = clipCapacity;
            m_SourceOwnerSlots = sourceOwnerSlots;
            m_PendingOwnerSlotReservations = pendingOwnerSlotReservations;
            m_FrameMutations = frameMutations;
            m_PendingClipPlans = pendingClipPlans;
            m_PendingClipStates = pendingClipStates;
            m_PendingNormalizedClipWeights = pendingNormalizedClipWeights;
            m_ReleasePermissions = releasePermissions;
            m_DeferredReleases = deferredReleases;
        }

        internal Transform RootTransform
        {
            get
            {
                RequireAvailable();
                return m_RootTransform;
            }
        }

        internal NativeArray<TransformStreamHandle> Handles
        {
            get
            {
                RequireAvailable();
                return m_Handles;
            }
        }

        internal NativeArray<AnimationLocalBonePose> ReferencePose
        {
            get
            {
                RequireAvailable();
                return m_ReferencePose;
            }
        }

        internal int SourceCapacity => m_SourceCapacity;
        internal int ClipCapacity => m_ClipCapacity;
        internal bool HasOpenFrame => m_FramePhase != SourceFramePhase.Closed;

        internal void BeginFrame(ulong frameIdentity)
        {
            RequireAvailable();
            if (frameIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(frameIdentity));
            if (m_FramePhase != SourceFramePhase.Closed)
                throw new InvalidOperationException("Animancer pose source frame is already open.");
            if (m_ReleasePermissionEntryCount != 0 ||
                m_UnconsumedReleasePermissionCount != 0 ||
                m_DeferredReleaseCount != 0)
                throw new InvalidOperationException("Animancer pose source lifecycle from the committed frame is not finalized.");
            m_FrameIdentity = frameIdentity;
            m_FramePhase = SourceFramePhase.Preparing;
        }

        internal AnimationPoseSourcePrepareResult PrepareOrUpdate(
            in AnimationPoseSampleRequest request,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            if (!request.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("Resolved animation pose request is invalid.", nameof(request));
            return PrepareOrUpdate(
                request.SourceId,
                request.Clips,
                clipCatalog,
                in capture,
                playerNodeId);
        }

        internal AnimationPoseSourcePrepareResult PrepareOrUpdate(
            AnimationPoseSourceId sourceId,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            RequireFramePhase(SourceFramePhase.Preparing);
            if (!sourceId.IsValid || !playerNodeId.IsValid ||
                clips.Count <= 0 || clips.Count > m_ClipCapacity)
            {
                throw new ArgumentException("Resolved animation pose source request exceeds its compiled capacity.");
            }
            if (!capture.SourceId.Equals(sourceId) || capture.CompletionIdentity == 0)
                throw new ArgumentException("Animation pose capture identity does not match the request.", nameof(capture));

            var key = new AnimationPlayerSourceKey(sourceId, playerNodeId);
            RequireNoFrameMutation(key);
            if (m_FrameMutationCount >= m_FrameMutations.Length)
                throw new InvalidOperationException("Animancer pose source mutation capacity was exceeded.");

            int ownerSlotIndex = FindCommittedSourceSlot(key);
            bool preparedResource = ownerSlotIndex < 0;
            if (preparedResource)
            {
                if (clipCatalog.Count <= 0 || clipCatalog.Count > m_ClipCapacity)
                    throw new ArgumentException("New animation pose source has no compiled clip catalog.");
                ValidateClipCatalog(clipCatalog);
            }
            else if (clipCatalog.Count != 0)
            {
                throw new ArgumentException("Committed animation pose source must not resubmit its immutable clip catalog.");
            }
            if (preparedResource && checked(m_CommittedSourceCount + CountPreparedResources()) >= m_SourceCapacity)
                throw new InvalidOperationException("Animancer committed and prepared source capacity was exceeded.");
            SourceVisual visual = preparedResource
                ? null
                : m_SourceOwnerSlots[ownerSlotIndex].Visual;
            if (preparedResource)
            {
                ownerSlotIndex = FindFreeOwnerSlot();
                m_PendingOwnerSlotReservations[ownerSlotIndex] = 1;
            }
            int mutationIndex = m_FrameMutationCount;
            try
            {
                if (preparedResource)
                    visual = CreateSource(key, clipCatalog);
                float inverseTotalWeight = ValidateClipPlans(
                    clips,
                    visual);
                int clipOffset = checked(mutationIndex * m_ClipCapacity);
                for (int i = 0; i < clips.Count; i++)
                {
                    m_PendingClipPlans[clipOffset + i] = clips[i];
                    m_PendingClipStates[clipOffset + i] =
                        visual.RequireClip(
                            clips[i].ClipBindingIndex,
                            clips[i].Clip);
                    m_PendingNormalizedClipWeights[clipOffset + i] =
                        clips[i].Weight * inverseTotalWeight;
                }
                AnimationPoseSourcePrepareKind kind = preparedResource
                    ? AnimationPoseSourcePrepareKind.PreparedResource
                    : AnimationPoseSourcePrepareKind.CommittedUpdate;
                m_FrameMutations[mutationIndex] = new SourceFrameMutation
                {
                    Key = key,
                    Visual = visual,
                    Capture = capture,
                    Kind = kind,
                    OwnerSlotIndex = ownerSlotIndex,
                    ClipCount = clips.Count
                };
                m_FrameMutationCount++;
                return new AnimationPoseSourcePrepareResult(
                    sourceId,
                    playerNodeId,
                    m_FrameIdentity,
                    capture.CompletionIdentity,
                    visual.CapturePlayable,
                    kind);
            }
            catch
            {
                int clipOffset = checked(mutationIndex * m_ClipCapacity);
                Array.Clear(m_PendingClipPlans, clipOffset, clips.Count);
                Array.Clear(m_PendingClipStates, clipOffset, clips.Count);
                Array.Clear(
                    m_PendingNormalizedClipWeights,
                    clipOffset,
                    clips.Count);
                if (preparedResource && visual != null)
                    visual.Destroy(m_Graph.PlayableGraph);
                if (preparedResource)
                    m_PendingOwnerSlotReservations[ownerSlotIndex] = 0;
                throw;
            }
        }

        internal AnimationPoseSourceReleaseToken StageRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            RequireFramePhase(SourceFramePhase.Preparing);
            var key = RequireKey(sourceId, playerNodeId);
            int ownerSlotIndex = FindCommittedSourceSlot(key);
            if (ownerSlotIndex < 0)
                throw new InvalidOperationException($"Animation pose source '{key}' is not committed.");
            if (FindFrameMutation(key) >= 0)
                throw new InvalidOperationException($"Animation pose source '{key}' cannot be captured and released in the same frame.");
            if (FindReleasePermission(key) >= 0)
                throw new InvalidOperationException($"Animation pose source '{key}' has a duplicate release mutation.");
            if (m_ReleasePermissionEntryCount >= m_ReleasePermissions.Length)
                throw new InvalidOperationException("Animancer pose source release capacity was exceeded.");
            ulong generation = NextReleasePermissionGeneration();
            int permissionIndex = m_ReleasePermissionEntryCount++;
            m_ReleasePermissions[permissionIndex] = new SourceReleasePermission
            {
                Key = key,
                Visual = m_SourceOwnerSlots[ownerSlotIndex].Visual,
                OwnerSlotIndex = ownerSlotIndex,
                Generation = generation
            };
            m_UnconsumedReleasePermissionCount++;
            return new AnimationPoseSourceReleaseToken(
                permissionIndex,
                generation);
        }

        internal void ValidateFrame(ulong frameIdentity)
        {
            RequireAvailable();
            RequireFrame(frameIdentity, SourceFramePhase.Preparing);
            int preparedCount = CountPreparedResources();
            if (checked(m_CommittedSourceCount + preparedCount) > m_SourceCapacity ||
                checked(m_ReleasePermissionEntryCount + preparedCount) > m_SourceCapacity)
            {
                throw new InvalidOperationException("Animancer pose source frame exceeds its compiled lifecycle capacity.");
            }
            int occupiedSlotCount = 0;
            for (int i = 0; i < m_SourceOwnerSlots.Length; i++)
            {
                SourceOwnerSlot slot = m_SourceOwnerSlots[i];
                if (slot.IsOccupied != slot.Key.IsValid)
                    throw new InvalidOperationException("Animancer pose source owner slot is invalid.");
                if (slot.IsOccupied)
                    occupiedSlotCount++;
            }
            if (occupiedSlotCount != m_CommittedSourceCount)
                throw new InvalidOperationException("Animancer pose source owner slot count is invalid.");
            for (int i = 0; i < m_FrameMutationCount; i++)
            {
                SourceFrameMutation mutation = m_FrameMutations[i];
                if (!mutation.IsValid ||
                    (uint)mutation.OwnerSlotIndex >=
                    (uint)m_SourceOwnerSlots.Length)
                    throw new InvalidOperationException("Animancer pose source mutation journal is invalid.");
                SourceOwnerSlot slot =
                    m_SourceOwnerSlots[mutation.OwnerSlotIndex];
                if (mutation.Kind ==
                    AnimationPoseSourcePrepareKind.PreparedResource)
                {
                    if (slot.IsOccupied ||
                        m_PendingOwnerSlotReservations[
                            mutation.OwnerSlotIndex] == 0)
                    {
                        throw new InvalidOperationException("Animancer prepared pose source owner slot is invalid.");
                    }
                }
                else if (mutation.Kind ==
                         AnimationPoseSourcePrepareKind.CommittedUpdate)
                {
                    if (!slot.Key.Equals(mutation.Key) ||
                        !ReferenceEquals(slot.Visual, mutation.Visual))
                    {
                        throw new InvalidOperationException("Animancer committed pose source owner slot is invalid.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Animancer pose source mutation kind is invalid.");
                }
                for (int previous = 0; previous < i; previous++)
                {
                    if (m_FrameMutations[previous].OwnerSlotIndex ==
                        mutation.OwnerSlotIndex)
                    {
                        throw new InvalidOperationException("Animancer pose source mutations target the same owner slot.");
                    }
                }
            }
            for (int i = 0; i < m_ReleasePermissionEntryCount; i++)
            {
                SourceReleasePermission permission =
                    m_ReleasePermissions[i];
                if (!permission.IsValid ||
                    (uint)permission.OwnerSlotIndex >=
                    (uint)m_SourceOwnerSlots.Length)
                {
                    throw new InvalidOperationException("Animancer pose source release permission is invalid.");
                }
                SourceOwnerSlot slot =
                    m_SourceOwnerSlots[permission.OwnerSlotIndex];
                if (!slot.Key.Equals(permission.Key) ||
                    !ReferenceEquals(slot.Visual, permission.Visual))
                {
                    throw new InvalidOperationException("Animancer pose source release owner slot is invalid.");
                }
            }
            m_FramePhase = SourceFramePhase.Validated;
        }

        internal void EnterEvaluateBarrier(ulong frameIdentity)
        {
            RequireAvailable();
            RequireFrame(frameIdentity, SourceFramePhase.Validated);
            m_FramePhase = SourceFramePhase.EvaluateBarrier;
            for (int mutationIndex = 0; mutationIndex < m_FrameMutationCount; mutationIndex++)
            {
                SourceFrameMutation mutation = m_FrameMutations[mutationIndex];
                int clipOffset = checked(mutationIndex * m_ClipCapacity);
                PrepareClips(
                    mutation.Visual,
                    m_PendingClipPlans,
                    m_PendingClipStates,
                    m_PendingNormalizedClipWeights,
                    clipOffset,
                    mutation.ClipCount);
                var job = new AnimationSourcePoseCaptureJob(
                    mutation.Capture,
                    m_Rig.BoneCounts,
                    m_Handles,
                    m_ReferencePose,
                    m_PhysicalParentIndices,
                    m_VirtualBones,
                    mutation.Visual.ComponentScratch,
                    m_Rig.RootPhysicalBoneIndex,
                    m_Rig.RootBonePolicy,
                    m_Rig.ScalePolicy);
                mutation.Visual.CapturePlayable.SetJobData(job);
            }
        }

        internal void CommitFrame(ulong frameIdentity)
        {
            RequireAvailable();
            RequireFrame(frameIdentity, SourceFramePhase.EvaluateBarrier);
            for (int i = 0; i < m_FrameMutationCount; i++)
            {
                SourceFrameMutation mutation = m_FrameMutations[i];
                if (mutation.Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                {
                    m_SourceOwnerSlots[mutation.OwnerSlotIndex] =
                        new SourceOwnerSlot
                        {
                            Key = mutation.Key,
                            Visual = mutation.Visual
                        };
                    m_CommittedSourceCount++;
                }
            }
            ClearFrameMutations(false);
            m_FrameIdentity = 0;
            m_FramePhase = SourceFramePhase.Closed;
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireAvailable();
            if (m_FramePhase != SourceFramePhase.Preparing &&
                m_FramePhase != SourceFramePhase.Validated)
            {
                throw new InvalidOperationException("Animancer pose source frame cannot be discarded after the Evaluate Barrier.");
            }
            if (m_FrameIdentity != frameIdentity || frameIdentity == 0)
                throw new InvalidOperationException("Animancer pose source frame identity is stale.");
            ClearFrameMutations(true);
            ClearReleasePermissions();
            m_FrameIdentity = 0;
            m_FramePhase = SourceFramePhase.Closed;
        }

        internal void Release(in AnimationPoseSourceReleaseToken token)
        {
            RequireAvailable();
            RequireClosedFrame();
            if (!token.IsValid ||
                (uint)token.PermissionIndex >= (uint)m_ReleasePermissionEntryCount)
            {
                throw new InvalidOperationException("Animation pose source release token is outside the committed batch.");
            }
            SourceReleasePermission permission =
                m_ReleasePermissions[token.PermissionIndex];
            if (!permission.IsValid || permission.Generation != token.Generation)
                throw new InvalidOperationException("Animation pose source release token is stale or already consumed.");
            m_SourceOwnerSlots[permission.OwnerSlotIndex].Clear();
            m_CommittedSourceCount--;
            m_DeferredReleases[m_DeferredReleaseCount++] =
                permission.Visual;
            m_ReleasePermissions[token.PermissionIndex].Clear();
            m_UnconsumedReleasePermissionCount--;
        }

        internal bool ContainsCommitted(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            return FindCommittedSourceSlot(
                RequireKey(sourceId, playerNodeId)) >= 0;
        }

        internal void ExecuteDeferredReleases()
        {
            RequireAvailable();
            RequireClosedFrame();
            if (m_UnconsumedReleasePermissionCount != 0)
                throw new InvalidOperationException("Animancer pose source release permissions were not fully consumed.");
            DestroyDeferredReleases();
            ClearReleasePermissions();
        }

        internal void Clear()
        {
            RequireNotDisposed();
            RequireClosedFrame();
            ClearReleasePermissions();
            DestroyDeferredReleases();
            DestroyCommittedSources();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            try
            {
                ClearFrameMutations(true);
                ClearReleasePermissions();
                DestroyDeferredReleases();
                DestroyCommittedSources();
            }
            finally
            {
                m_VirtualBones.Dispose();
                m_PhysicalParentIndices.Dispose();
                m_ReferencePose.Dispose();
                m_Handles.Dispose();
                m_Disposed = true;
            }
        }

        void DestroyCommittedSources()
        {
            PlayableGraph playableGraph = m_Graph.PlayableGraph;
            for (int i = 0; i < m_SourceOwnerSlots.Length; i++)
            {
                SourceVisual visual = m_SourceOwnerSlots[i].Visual;
                if (visual != null)
                    visual.Destroy(playableGraph);
                m_SourceOwnerSlots[i].Clear();
            }
            m_CommittedSourceCount = 0;
        }

        void DestroyDeferredReleases()
        {
            PlayableGraph graph = m_Graph.PlayableGraph;
            for (int i = 0; i < m_DeferredReleaseCount; i++)
            {
                m_DeferredReleases[i].Destroy(graph);
                m_DeferredReleases[i] = null;
            }
            m_DeferredReleaseCount = 0;
        }

        SourceVisual CreateSource(
            AnimationPlayerSourceKey key,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog)
        {
            var mixer = new ManualMixerState
            {
                Key = new AnimationPoseSourceMixerKey(key),
                Speed = 0f,
                IsPlaying = true,
                Weight = 1f
            };
            AnimationScriptPlayable capturePlayable = default;
            NativeArray<CharacterComponentBonePose> componentScratch = default;
            SourceVisual visual = null;
            try
            {
                componentScratch = new NativeArray<CharacterComponentBonePose>(
                    m_Rig.PhysicalBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                var job = new AnimationSourcePoseCaptureJob(
                    default,
                    m_Rig.BoneCounts,
                    m_Handles,
                    m_ReferencePose,
                    m_PhysicalParentIndices,
                    m_VirtualBones,
                    componentScratch,
                    m_Rig.RootPhysicalBoneIndex,
                    m_Rig.RootBonePolicy,
                    m_Rig.ScalePolicy,
                    false);
                mixer.SetGraph(m_Graph);
                capturePlayable = AnimationScriptPlayable.Create(m_Graph.PlayableGraph, job, 1);
                capturePlayable.SetProcessInputs(true);
                m_Graph.PlayableGraph.Connect(mixer.Playable, 0, capturePlayable, 0);
                capturePlayable.SetInputWeight(0, 1f);
                visual = new SourceVisual(
                    key,
                    mixer,
                    capturePlayable,
                    componentScratch,
                    clipCatalog.Count);
                for (int i = 0; i < clipCatalog.Count; i++)
                    visual.AddClip(clipCatalog[i]);
                return visual;
            }
            catch
            {
                if (visual != null)
                {
                    visual.Destroy(m_Graph.PlayableGraph);
                }
                else
                {
                    if (componentScratch.IsCreated)
                        componentScratch.Dispose();
                    if (capturePlayable.IsValid() && m_Graph.PlayableGraph.IsValid())
                        m_Graph.PlayableGraph.DestroyPlayable(capturePlayable);
                    mixer.Destroy();
                }
                throw;
            }
        }

        static void ValidateClipCatalog(
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                AnimationPoseSourceClipBinding current = catalog[i];
                if (!current.IsValid)
                    throw new InvalidOperationException("Animation pose source clip catalog contains an invalid binding.");
                for (int j = 0; j < i; j++)
                {
                    if (catalog[j].ClipBindingIndex == current.ClipBindingIndex)
                        throw new InvalidOperationException($"Animation pose source clip catalog duplicates binding #{current.ClipBindingIndex}.");
                }
            }
        }

        static float ValidateClipPlans(
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            SourceVisual visual)
        {
            float totalWeight = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                ClipSamplePlan plan = clips[i];
                if (!plan.IsValid)
                    throw new InvalidOperationException("Animation pose source clip plan is invalid.");
                visual.RequireClip(plan.ClipBindingIndex, plan.Clip);
                for (int previous = 0; previous < i; previous++)
                {
                    if (clips[previous].ClipBindingIndex == plan.ClipBindingIndex)
                        throw new InvalidOperationException($"Animation pose source clip plan duplicates binding #{plan.ClipBindingIndex}.");
                }
                totalWeight += plan.Weight;
            }
            if (!float.IsFinite(totalWeight) || totalWeight <= 0f)
                throw new InvalidOperationException("Animation pose source clip plan has no positive total weight.");
            return 1f / totalWeight;
        }

        static void PrepareClips(
            SourceVisual visual,
            ClipSamplePlan[] clips,
            ClipState[] clipStates,
            float[] normalizedWeights,
            int clipOffset,
            int clipCount)
        {
            visual.ClearClipWeights();
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
            {
                int index = clipOffset + clipIndex;
                ClipSamplePlan plan = clips[index];
                ClipState child = clipStates[index];
                child.IsPlaying = true;
                child.Speed = 0f;
                child.Time = plan.IsLooping ? (float)plan.ContinuousClipTime : plan.ClipTime;
                child.Weight = normalizedWeights[index];
                visual.TrackActiveClip(child);
            }

            visual.Mixer.Speed = 0f;
            visual.Mixer.IsPlaying = true;
            visual.Mixer.Weight = 1f;
        }

        int CountPreparedResources()
        {
            int count = 0;
            for (int i = 0; i < m_FrameMutationCount; i++)
            {
                if (m_FrameMutations[i].Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                    count++;
            }
            return count;
        }

        int FindFrameMutation(AnimationPlayerSourceKey key)
        {
            for (int i = 0; i < m_FrameMutationCount; i++)
            {
                if (m_FrameMutations[i].Key.Equals(key))
                    return i;
            }
            return -1;
        }

        void RequireNoFrameMutation(AnimationPlayerSourceKey key)
        {
            if (FindFrameMutation(key) >= 0)
                throw new InvalidOperationException($"Animation pose source '{key}' has a duplicate frame mutation.");
            if (FindReleasePermission(key) >= 0)
                throw new InvalidOperationException($"Animation pose source '{key}' cannot be captured and released in the same frame.");
        }

        int FindReleasePermission(AnimationPlayerSourceKey key)
        {
            for (int i = 0; i < m_ReleasePermissionEntryCount; i++)
            {
                if (m_ReleasePermissions[i].IsValid &&
                    m_ReleasePermissions[i].Key.Equals(key))
                    return i;
            }
            return -1;
        }

        ulong NextReleasePermissionGeneration()
        {
            if (m_LastReleasePermissionGeneration == ulong.MaxValue)
                throw new InvalidOperationException("Animation pose source release permission generation was exhausted.");
            m_LastReleasePermissionGeneration++;
            return m_LastReleasePermissionGeneration;
        }

        void ClearReleasePermissions()
        {
            if (m_ReleasePermissionEntryCount > 0)
            {
                Array.Clear(
                    m_ReleasePermissions,
                    0,
                    m_ReleasePermissionEntryCount);
            }
            m_ReleasePermissionEntryCount = 0;
            m_UnconsumedReleasePermissionCount = 0;
        }

        int FindCommittedSourceSlot(AnimationPlayerSourceKey key)
        {
            for (int i = 0; i < m_SourceOwnerSlots.Length; i++)
            {
                if (m_SourceOwnerSlots[i].Key.Equals(key))
                    return i;
            }
            return -1;
        }

        int FindFreeOwnerSlot()
        {
            for (int i = 0; i < m_SourceOwnerSlots.Length; i++)
            {
                if (!m_SourceOwnerSlots[i].IsOccupied &&
                    m_PendingOwnerSlotReservations[i] == 0)
                {
                    return i;
                }
            }
            throw new InvalidOperationException("Animancer committed source slot capacity was exceeded.");
        }

        void ClearFrameMutations(bool destroyPreparedResources)
        {
            PlayableGraph graph = m_Graph.PlayableGraph;
            for (int i = 0; i < m_FrameMutationCount; i++)
            {
                SourceFrameMutation mutation = m_FrameMutations[i];
                if (destroyPreparedResources &&
                    mutation.Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                {
                    mutation.Visual.Destroy(graph);
                }
                if (mutation.Kind ==
                    AnimationPoseSourcePrepareKind.PreparedResource)
                {
                    m_PendingOwnerSlotReservations[
                        mutation.OwnerSlotIndex] = 0;
                }
                int clipOffset = checked(i * m_ClipCapacity);
                Array.Clear(m_PendingClipPlans, clipOffset, mutation.ClipCount);
                Array.Clear(m_PendingClipStates, clipOffset, mutation.ClipCount);
                Array.Clear(
                    m_PendingNormalizedClipWeights,
                    clipOffset,
                    mutation.ClipCount);
                m_FrameMutations[i].Clear();
            }
            m_FrameMutationCount = 0;
        }

        static AnimationPlayerSourceKey RequireKey(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId)
        {
            if (!sourceId.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.");
            return new AnimationPlayerSourceKey(sourceId, playerNodeId);
        }

        void RequireFrame(ulong frameIdentity, SourceFramePhase phase)
        {
            RequireFramePhase(phase);
            if (frameIdentity == 0 || m_FrameIdentity != frameIdentity)
                throw new InvalidOperationException("Animancer pose source frame identity is stale.");
        }

        void RequireFramePhase(SourceFramePhase phase)
        {
            if (m_FramePhase != phase)
                throw new InvalidOperationException($"Animancer pose source frame phase must be {phase}, actual {m_FramePhase}.");
        }

        void RequireClosedFrame()
        {
            RequireFramePhase(SourceFramePhase.Closed);
        }

        void RequireNotDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimancerPoseSamplingBackend));
        }

        void RequireAvailable()
        {
            RequireNotDisposed();
            if (!m_Animancer || !m_Graph.PlayableGraph.IsValid())
                throw new InvalidOperationException("Animancer pose sampling graph is unavailable.");
        }

        sealed class SourceVisual
        {
            readonly int[] m_ClipStateIndicesByBinding;
            readonly AnimationClip[] m_Clips;
            readonly ClipState[] m_ClipStates;
            readonly ClipState[] m_ActiveClipStates;
            int m_ClipCount;
            int m_ActiveClipCount;
            bool m_Destroyed;

            public SourceVisual(
                AnimationPlayerSourceKey key,
                ManualMixerState mixer,
                AnimationScriptPlayable capturePlayable,
                NativeArray<CharacterComponentBonePose> componentScratch,
                int clipCapacity)
            {
                Key = key;
                Mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
                CapturePlayable = capturePlayable;
                ComponentScratch = componentScratch;
                if (!key.IsValid || !capturePlayable.IsValid() ||
                    !componentScratch.IsCreated || clipCapacity <= 0)
                {
                    throw new ArgumentException("Animation pose source visual is invalid.");
                }
                m_ClipStateIndicesByBinding = new int[clipCapacity];
                m_Clips = new AnimationClip[clipCapacity];
                m_ClipStates = new ClipState[clipCapacity];
                m_ActiveClipStates = new ClipState[clipCapacity];
                for (int i = 0; i < m_ClipStateIndicesByBinding.Length; i++)
                    m_ClipStateIndicesByBinding[i] = -1;
            }

            public AnimationPlayerSourceKey Key { get; }
            public ManualMixerState Mixer { get; }
            public AnimationScriptPlayable CapturePlayable { get; }
            public NativeArray<CharacterComponentBonePose> ComponentScratch { get; }

            public void ClearClipWeights()
            {
                for (int i = 0; i < m_ActiveClipCount; i++)
                {
                    m_ActiveClipStates[i].Weight = 0f;
                    m_ActiveClipStates[i] = null;
                }
                m_ActiveClipCount = 0;
            }

            public void TrackActiveClip(ClipState clip)
            {
                if (m_Destroyed || clip == null ||
                    m_ActiveClipCount >= m_ActiveClipStates.Length)
                {
                    throw new InvalidOperationException("Animation pose source active clip capacity was exceeded.");
                }
                m_ActiveClipStates[m_ActiveClipCount++] = clip;
            }

            public void AddClip(in AnimationPoseSourceClipBinding binding)
            {
                if (m_Destroyed || !binding.IsValid ||
                    m_ClipCount >= m_ClipStates.Length ||
                    (uint)binding.ClipBindingIndex >=
                    (uint)m_ClipStateIndicesByBinding.Length ||
                    FindClip(binding.ClipBindingIndex) >= 0)
                {
                    throw new InvalidOperationException("Animation pose source clip catalog mutation is invalid.");
                }
                ClipState child = Mixer.Add(binding.Clip);
                child.Key = new AnimationPoseSourceClipKey(Key, binding.ClipBindingIndex);
                Mixer.DontSynchronize(child);
                child.IsPlaying = true;
                child.Speed = 0f;
                child.Weight = 0f;
                int index = m_ClipCount++;
                m_ClipStateIndicesByBinding[binding.ClipBindingIndex] = index;
                m_Clips[index] = binding.Clip;
                m_ClipStates[index] = child;
            }

            public ClipState RequireClip(int clipBindingIndex, AnimationClip clip)
            {
                int index = FindClip(clipBindingIndex);
                if (m_Destroyed || index < 0 || !clip ||
                    !ReferenceEquals(m_Clips[index], clip) ||
                    !ReferenceEquals(m_ClipStates[index].Clip, clip))
                {
                    throw new InvalidOperationException(
                        $"Animation source '{Key}' ClipBindingIndex #{clipBindingIndex} is not in its compiled catalog.");
                }
                return m_ClipStates[index];
            }

            int FindClip(int clipBindingIndex)
            {
                return (uint)clipBindingIndex <
                       (uint)m_ClipStateIndicesByBinding.Length
                    ? m_ClipStateIndicesByBinding[clipBindingIndex]
                    : -1;
            }

            public void Destroy(PlayableGraph graph)
            {
                if (m_Destroyed)
                    return;
                m_Destroyed = true;
                if (CapturePlayable.IsValid() && graph.IsValid())
                    graph.DestroyPlayable(CapturePlayable);
                Mixer.Destroy();
                ComponentScratch.Dispose();
                Array.Clear(m_Clips, 0, m_Clips.Length);
                Array.Clear(m_ClipStates, 0, m_ClipStates.Length);
                Array.Clear(m_ActiveClipStates, 0, m_ActiveClipStates.Length);
                for (int i = 0; i < m_ClipStateIndicesByBinding.Length; i++)
                    m_ClipStateIndicesByBinding[i] = -1;
                m_ClipCount = 0;
                m_ActiveClipCount = 0;
            }
        }

        readonly struct AnimationPoseSourceMixerKey : IEquatable<AnimationPoseSourceMixerKey>
        {
            public AnimationPoseSourceMixerKey(AnimationPlayerSourceKey source)
            {
                if (!source.IsValid)
                    throw new ArgumentException("Animation pose source mixer key is invalid.", nameof(source));
                Source = source;
            }

            public AnimationPlayerSourceKey Source { get; }
            public bool Equals(AnimationPoseSourceMixerKey other) => Source.Equals(other.Source);
            public override bool Equals(object obj) => obj is AnimationPoseSourceMixerKey other && Equals(other);
            public override int GetHashCode() => Source.GetHashCode();
            public override string ToString() => $"PoseSource/{Source}";
        }

        readonly struct AnimationPoseSourceClipKey : IEquatable<AnimationPoseSourceClipKey>
        {
            public AnimationPoseSourceClipKey(AnimationPlayerSourceKey source, int clipBindingIndex)
            {
                if (!source.IsValid || clipBindingIndex < 0)
                    throw new ArgumentException("Animation pose source clip key is invalid.");
                Source = source;
                ClipBindingIndex = clipBindingIndex;
            }

            public AnimationPlayerSourceKey Source { get; }
            public int ClipBindingIndex { get; }

            public bool Equals(AnimationPoseSourceClipKey other) =>
                Source.Equals(other.Source) && ClipBindingIndex == other.ClipBindingIndex;

            public override bool Equals(object obj) => obj is AnimationPoseSourceClipKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return Source.GetHashCode() * 397 ^ ClipBindingIndex;
                }
            }

            public override string ToString() => $"PoseSource/{Source}/Clip/{ClipBindingIndex}";
        }

        readonly struct AnimationPlayerSourceKey : IEquatable<AnimationPlayerSourceKey>
        {
            public AnimationPlayerSourceKey(AnimationPoseSourceId sourceId, PoseNodeId playerNodeId)
            {
                if (!sourceId.IsValid || !playerNodeId.IsValid)
                    throw new ArgumentException("Animation Player source key is invalid.");
                SourceId = sourceId;
                PlayerNodeId = playerNodeId;
            }

            public AnimationPoseSourceId SourceId { get; }
            public PoseNodeId PlayerNodeId { get; }
            public bool IsValid => SourceId.IsValid && PlayerNodeId.IsValid;

            public bool Equals(AnimationPlayerSourceKey other) =>
                SourceId.Equals(other.SourceId) && PlayerNodeId.Equals(other.PlayerNodeId);

            public override bool Equals(object obj) => obj is AnimationPlayerSourceKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return SourceId.GetHashCode() * 397 ^ PlayerNodeId.GetHashCode();
                }
            }

            public override string ToString() => $"{SourceId}/Player/{PlayerNodeId}";
        }
    }
}
