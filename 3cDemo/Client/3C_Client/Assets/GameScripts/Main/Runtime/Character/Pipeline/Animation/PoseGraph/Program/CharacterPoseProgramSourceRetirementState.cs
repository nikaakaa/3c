using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseProgramSourceRetirementState
    {
        enum StandaloneOwner : byte
        {
            Direct = 1,
            Clip = 2,
            BlendSpace = 3
        }

        struct StandaloneRetirement
        {
            internal StandaloneOwner Owner;
            internal int PlayerIndex;
            internal AnimationPoseSourceId SourceId;
            internal PoseNodeId NodeId;
            internal CharacterPoseSourceRetirementHandle Source;
            internal AnimationPlayerReleaseToken Player;
        }

        sealed class PendingPoseRetirement
        {
            internal bool InUse;
            internal AnimationBlendStackRuntime Stack;
            internal CharacterAnimationTransitionRouteRuntime Route;
            internal AnimationBlendStackSourceReleaseToken StackRelease;
            internal AnimationPhysicalSourceIdentity PhysicalSource;
            internal CharacterPoseSourceRetirementHandle SourceRetirement;
            internal bool NotifyRouteAfterApply;

            internal AnimationBlendStackRelease Release =>
                StackRelease.Release;

            internal void Clear()
            {
                InUse = false;
                Stack = null;
                Route = null;
                StackRelease = default;
                PhysicalSource = default;
                SourceRetirement = default;
                NotifyRouteAfterApply = false;
            }
        }

        sealed class PendingActionRetirement
        {
            internal bool InUse;
            internal AnimationSlotId SlotId;
            internal AnimationBlendStackRuntime Stack;
            internal CharacterAnimationTransitionRouteRuntime Route;
            internal AnimationBlendStackSourceReleaseToken StackRelease;
            internal AnimationPhysicalSourceIdentity PhysicalSource;
            internal CharacterPoseSourceRetirementHandle SourceRetirement;
            internal ActionBackendSourceIdentity PlayableSource;
            internal ActionBackendSourceIdentity StoredPoseSource;
            internal ulong RequestIdentity;
            internal ulong PlayableCompletionIdentity;
            internal ulong StoredPoseCompletionIdentity;
            internal bool NotifyRouteAfterApply;

            internal AnimationBlendStackRelease Release =>
                StackRelease.Release;

            internal void Clear()
            {
                InUse = false;
                SlotId = default;
                Stack = null;
                Route = null;
                StackRelease = default;
                PhysicalSource = default;
                SourceRetirement = default;
                PlayableSource = default;
                StoredPoseSource = default;
                RequestIdentity = 0;
                PlayableCompletionIdentity = 0;
                StoredPoseCompletionIdentity = 0;
                NotifyRouteAfterApply = false;
            }
        }

        sealed class PreparedActionRetirement
        {
            internal PreparedActionRetirement(
                int retirementCapacity,
                int backendSourceCapacity)
            {
                Request = new ActionBackendReleaseRequest(
                    backendSourceCapacity);
                Sources = new List<PendingActionRetirement>(
                    retirementCapacity);
            }

            internal bool InUse;
            internal readonly ActionBackendReleaseRequest Request;
            internal readonly List<PendingActionRetirement> Sources;

            internal void Clear()
            {
                InUse = false;
                Request.Clear();
                Sources.Clear();
            }
        }

        static readonly Comparison<PendingActionRetirement>
            s_PendingActionComparison =
                (left, right) => left.PlayableSource.CompareTo(
                    right.PlayableSource);
        readonly StandaloneRetirement[] m_Standalone;
        readonly List<PendingPoseRetirement> m_PendingPose;
        readonly List<PendingActionRetirement> m_PendingAction;
        readonly List<PreparedActionRetirement> m_PreparedAction;
        readonly PendingPoseRetirement[] m_PendingPosePool;
        readonly PendingActionRetirement[] m_PendingActionPool;
        readonly PreparedActionRetirement[] m_PreparedActionPool;
        readonly List<PendingActionRetirement> m_ActionPendingScratch;
        readonly List<ActionBackendSourceIdentity> m_ActionSourceScratch;
        readonly HashSet<ActionBackendSourceIdentity> m_ExpectedActionSources;
        readonly string[] m_PlayableBackendResourceIds;
        readonly string[] m_StoredPoseBackendResourceIds;
        int m_StandaloneCount;
        int m_PendingActionFrameStartCount;
        ulong m_ActionRequestIdentity;
        ulong m_ActionCompletionIdentity;

        internal CharacterPoseProgramSourceRetirementState(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Standalone = new StandaloneRetirement[capacity];
            int backendSourceCapacity = checked(capacity * 2);
            m_PendingPose =
                new List<PendingPoseRetirement>(capacity);
            m_PendingAction =
                new List<PendingActionRetirement>(capacity);
            m_PreparedAction =
                new List<PreparedActionRetirement>(capacity);
            m_PendingPosePool = new PendingPoseRetirement[capacity];
            m_PendingActionPool = new PendingActionRetirement[capacity];
            m_PreparedActionPool = new PreparedActionRetirement[capacity];
            m_ActionPendingScratch =
                new List<PendingActionRetirement>(capacity);
            m_ActionSourceScratch =
                new List<ActionBackendSourceIdentity>(
                    backendSourceCapacity);
            m_ExpectedActionSources =
                new HashSet<ActionBackendSourceIdentity>(
                    backendSourceCapacity);
            m_PlayableBackendResourceIds = new string[capacity];
            m_StoredPoseBackendResourceIds = new string[capacity];
            for (int i = 0; i < capacity; i++)
            {
                m_PendingPosePool[i] = new PendingPoseRetirement();
                m_PendingActionPool[i] = new PendingActionRetirement();
                m_PreparedActionPool[i] =
                    new PreparedActionRetirement(
                        capacity,
                        backendSourceCapacity);
                m_PlayableBackendResourceIds[i] =
                    $"animation-source-slot/{i}/playable";
                m_StoredPoseBackendResourceIds[i] =
                    $"animation-source-slot/{i}/stored-pose";
            }
        }

        internal int StandaloneCapacity => m_Standalone.Length;
        internal bool HasPreparedStandalone => m_StandaloneCount != 0;
        internal int PendingPoseCount => m_PendingPose.Count;

        internal void BeginFrame()
        {
            m_PendingActionFrameStartCount = m_PendingAction.Count;
        }

        internal void CompleteFrame()
        {
            m_PendingActionFrameStartCount = 0;
        }

        internal void PrepareDirect(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.Direct,
                playerIndex,
                in source,
                in player);

        internal void PrepareClip(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.Clip,
                playerIndex,
                in source,
                in player);

        internal void PrepareBlendSpace(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.BlendSpace,
                playerIndex,
                in source,
                in player);

        internal void ApplyStandalone(
            CharacterPoseSourceModule sourceModule,
            AnimationSelectedPosePlayerRuntime[] directPlayers,
            AnimationClipPlayerRuntime[] clipPlayers,
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers,
            ulong completionIdentity)
        {
            if (sourceModule == null ||
                directPlayers == null ||
                clipPlayers == null ||
                blendSpacePlayers == null ||
                completionIdentity == 0)
            {
                throw new ArgumentException(
                    "Standalone Pose source retirement input is invalid.");
            }
            for (int i = 0; i < m_StandaloneCount; i++)
            {
                StandaloneRetirement retirement = m_Standalone[i];
                sourceModule.ApplyRetirement(in retirement.Source);
                switch (retirement.Owner)
                {
                    case StandaloneOwner.Direct:
                        directPlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    case StandaloneOwner.Clip:
                        clipPlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    case StandaloneOwner.BlendSpace:
                        blendSpacePlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Standalone pose source release owner is invalid.");
                }
                sourceModule.RecordRelease(
                    retirement.NodeId,
                    retirement.SourceId,
                    completionIdentity);
                m_Standalone[i] = default;
            }
            m_StandaloneCount = 0;
        }

        internal bool HasPendingAction(
            AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
            {
                throw new ArgumentException(
                    "Action playback identity is invalid.",
                    nameof(playbackId));
            }
            for (int i = 0; i < m_PendingAction.Count; i++)
            {
                if (m_PendingAction[i].Release.SourceId.PlaybackId
                    .Equals(playbackId))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool TryPrepareActionRequest(
            AnimationPlaybackId playbackId,
            out ActionBackendReleaseRequest request)
        {
            if (!playbackId.IsValid)
            {
                throw new ArgumentException(
                    "Action playback identity is invalid.",
                    nameof(playbackId));
            }
            m_ActionSourceScratch.Clear();
            m_ActionPendingScratch.Clear();
            for (int i = 0; i < m_PendingAction.Count; i++)
            {
                PendingActionRetirement candidate = m_PendingAction[i];
                if (!candidate.Release.SourceId.PlaybackId.Equals(
                        playbackId))
                {
                    continue;
                }
                if (candidate.RequestIdentity != 0)
                {
                    throw new InvalidOperationException(
                        $"Action playback '{playbackId}' already has a prepared backend release request.");
                }
                AddFixed(
                    m_ActionPendingScratch,
                    candidate,
                    "Action backend pending release scratch");
                AddFixed(
                    m_ActionSourceScratch,
                    candidate.PlayableSource,
                    "Action backend source scratch");
                AddFixed(
                    m_ActionSourceScratch,
                    candidate.StoredPoseSource,
                    "Action backend source scratch");
            }
            if (m_ActionPendingScratch.Count == 0)
            {
                request = null;
                return false;
            }
            m_ActionPendingScratch.Sort(s_PendingActionComparison);
            ulong requestIdentity = NextActionRequestIdentity();
            PreparedActionRetirement prepared = RentPreparedAction();
            try
            {
                prepared.Request.Prepare(
                    requestIdentity,
                    playbackId,
                    m_ActionSourceScratch);
                for (int i = 0;
                     i < m_ActionPendingScratch.Count;
                     i++)
                {
                    PendingActionRetirement pending =
                        m_ActionPendingScratch[i];
                    pending.RequestIdentity = requestIdentity;
                    AddFixed(
                        prepared.Sources,
                        pending,
                        "Prepared Action backend release");
                }
                AddFixed(
                    m_PreparedAction,
                    prepared,
                    "Prepared Action backend release journal");
                request = prepared.Request;
                return true;
            }
            catch
            {
                for (int i = 0;
                     i < m_ActionPendingScratch.Count;
                     i++)
                {
                    if (m_ActionPendingScratch[i].RequestIdentity ==
                        requestIdentity)
                    {
                        m_ActionPendingScratch[i].RequestIdentity = 0;
                    }
                }
                ReturnPreparedAction(prepared);
                throw;
            }
        }

        internal void StagePose(
            CharacterPoseSourceModule sourceModule,
            AnimationBlendStackRuntime stack,
            CharacterAnimationTransitionRouteRuntime route,
            in AnimationBlendStackSourceReleaseToken stackRelease,
            AnimationPhysicalSourceIdentity physical)
        {
            if (sourceModule == null ||
                stack == null ||
                route == null ||
                !stackRelease.IsValid ||
                !physical.IsValid)
            {
                throw new ArgumentException(
                    "Pending Pose source release is invalid.");
            }
            AnimationBlendStackRelease release = stackRelease.Release;
            sourceModule.RecordRelease(
                release.PoseNodeId,
                release.SourceId,
                release.CompletionIdentity);
            PendingPoseRetirement pending = RentPendingPose();
            pending.Stack = stack;
            pending.Route = route;
            pending.StackRelease = stackRelease;
            pending.PhysicalSource = physical;
            AddFixed(
                m_PendingPose,
                pending,
                "Pending Pose source release journal");
        }

        internal void StageAction(
            CharacterPoseSourceModule sourceModule,
            AnimationSlotId slotId,
            AnimationBlendStackRuntime stack,
            CharacterAnimationTransitionRouteRuntime route,
            in AnimationBlendStackSourceReleaseToken stackRelease,
            AnimationPhysicalSourceIdentity physical)
        {
            AnimationBlendStackRelease release = stackRelease.Release;
            if (sourceModule == null ||
                !slotId.IsValid ||
                stack == null ||
                route == null ||
                !stackRelease.IsValid ||
                !physical.IsValid ||
                !IsFiniteActionSource(release.SourceId))
            {
                throw new ArgumentException(
                    "Pending Action backend release is invalid.");
            }
            for (int i = 0; i < m_PendingAction.Count; i++)
            {
                PendingActionRetirement existing = m_PendingAction[i];
                if (existing.Release.SourceId.Equals(release.SourceId) &&
                    existing.Release.PoseNodeId == release.PoseNodeId)
                {
                    throw new InvalidOperationException(
                        $"Action source '{release.SourceId}' already waits for backend release.");
                }
            }
            int resourceIndex = physical.Index.Value;
            if ((uint)resourceIndex >=
                (uint)m_PlayableBackendResourceIds.Length)
            {
                throw new InvalidOperationException(
                    "Action backend physical source index exceeds the release journal capacity.");
            }
            PendingActionRetirement pending = RentPendingAction();
            pending.SlotId = slotId;
            pending.Stack = stack;
            pending.Route = route;
            pending.StackRelease = stackRelease;
            pending.PhysicalSource = physical;
            pending.PlayableSource = new ActionBackendSourceIdentity(
                ActionBackendSourceKind.Playable,
                m_PlayableBackendResourceIds[resourceIndex],
                physical.Generation);
            pending.StoredPoseSource = new ActionBackendSourceIdentity(
                ActionBackendSourceKind.StoredPoseCapture,
                m_StoredPoseBackendResourceIds[resourceIndex],
                physical.Generation);
            AddFixed(
                m_PendingAction,
                pending,
                "Pending Action backend release journal");
            var completion =
                new AnimationSlotSourceReleaseCompletion(
                    slotId,
                    release.SourceId.PlaybackId,
                    release.SourceId,
                    release.CompletionIdentity);
            sourceModule.RecordReleaseCompletion(in completion);
        }

        internal int PreparePendingRetirements(
            CharacterPoseSourceModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            for (int i = 0; i < m_PendingPose.Count; i++)
            {
                PendingPoseRetirement pending = m_PendingPose[i];
                pending.SourceRetirement = PrepareSourceRetirement(
                    sourceModule,
                    pending.Release.SourceId,
                    pending.Release.PoseNodeId,
                    pending.PhysicalSource);
            }
            int actionCount = ValidatePreparedActions(sourceModule);
            PrepareRouteNotifications();
            return actionCount;
        }

        internal void ApplyPendingPose(
            CharacterPoseSourceModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            for (int i = 0; i < m_PendingPose.Count; i++)
            {
                PendingPoseRetirement pending = m_PendingPose[i];
                CharacterAnimationTransitionRouteRuntime route =
                    pending.Route;
                sourceModule.ApplyRetirement(
                    in pending.SourceRetirement);
                pending.Stack.ApplyPreparedRelease(
                    in pending.StackRelease);
                bool notifyRoute = pending.NotifyRouteAfterApply;
                pending.Clear();
                if (notifyRoute)
                    route.NotifySourcesReleased();
            }
            m_PendingPose.Clear();
        }

        internal void ExecutePreparedActions(
            CharacterPoseSourceModule sourceModule)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            for (int requestIndex = 0;
                 requestIndex < m_PreparedAction.Count;
                 requestIndex++)
            {
                PreparedActionRetirement prepared =
                    m_PreparedAction[requestIndex];
                ExecuteAction(sourceModule, prepared);
                ReturnPreparedAction(prepared);
            }
            m_PreparedAction.Clear();
            m_PendingAction.Clear();
            sourceModule.CompleteDeferredReleases();
        }

        internal void Clear()
        {
            for (int i = 0; i < m_PreparedAction.Count; i++)
                m_PreparedAction[i].Clear();
            m_PreparedAction.Clear();
            for (int i = 0; i < m_PendingAction.Count; i++)
                m_PendingAction[i].Clear();
            m_PendingAction.Clear();
            for (int i = 0; i < m_PendingPose.Count; i++)
                m_PendingPose[i].Clear();
            m_PendingPose.Clear();
            m_ActionPendingScratch.Clear();
            m_ActionSourceScratch.Clear();
            m_ExpectedActionSources.Clear();
            m_PendingActionFrameStartCount = 0;
            ClearStandalone();
        }

        internal void DiscardFrame()
        {
            for (int i = 0; i < m_PreparedAction.Count; i++)
            {
                PreparedActionRetirement prepared = m_PreparedAction[i];
                for (int sourceIndex = 0;
                     sourceIndex < prepared.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionRetirement source =
                        prepared.Sources[sourceIndex];
                    source.RequestIdentity = 0;
                    source.PlayableCompletionIdentity = 0;
                    source.StoredPoseCompletionIdentity = 0;
                    source.SourceRetirement = default;
                    source.NotifyRouteAfterApply = false;
                }
                prepared.Clear();
            }
            m_PreparedAction.Clear();
            int committedCount = m_PendingActionFrameStartCount;
            if (committedCount < 0 ||
                committedCount > m_PendingAction.Count)
            {
                throw new InvalidOperationException(
                    "Pending Action backend release frame boundary is invalid.");
            }
            for (int i = committedCount;
                 i < m_PendingAction.Count;
                 i++)
            {
                m_PendingAction[i].Clear();
            }
            if (committedCount < m_PendingAction.Count)
            {
                m_PendingAction.RemoveRange(
                    committedCount,
                    m_PendingAction.Count - committedCount);
            }
            for (int i = 0; i < m_PendingPose.Count; i++)
                m_PendingPose[i].Clear();
            m_PendingPose.Clear();
            m_ActionPendingScratch.Clear();
            m_ActionSourceScratch.Clear();
            m_ExpectedActionSources.Clear();
            m_PendingActionFrameStartCount = 0;
        }

        void ExecuteAction(
            CharacterPoseSourceModule sourceModule,
            PreparedActionRetirement prepared)
        {
            ActionBackendReleaseRequest request =
                prepared?.Request ??
                throw new ArgumentNullException(nameof(prepared));
            for (int sourceIndex = 0;
                 sourceIndex < prepared.Sources.Count;
                 sourceIndex++)
            {
                PendingActionRetirement retirement =
                    prepared.Sources[sourceIndex];
                sourceModule.ApplyRetirement(
                    in retirement.SourceRetirement);
                retirement.Stack.ApplyPreparedRelease(
                    in retirement.StackRelease);
                if (retirement.NotifyRouteAfterApply)
                    retirement.Route.NotifySourcesReleased();
                sourceModule.RecordRelease(
                    retirement.Release.PoseNodeId,
                    retirement.Release.SourceId,
                    retirement.Release.CompletionIdentity);
                var playableCompletion =
                    new ActionBackendReleaseCompletion(
                        request.RequestIdentity,
                        request.PlaybackId,
                        retirement.PlayableSource,
                        retirement.PlayableCompletionIdentity);
                sourceModule.RecordReleaseCompletion(
                    in playableCompletion);
                var storedPoseCompletion =
                    new ActionBackendReleaseCompletion(
                        request.RequestIdentity,
                        request.PlaybackId,
                        retirement.StoredPoseSource,
                        retirement.StoredPoseCompletionIdentity);
                sourceModule.RecordReleaseCompletion(
                    in storedPoseCompletion);
                retirement.Clear();
            }
        }

        int ValidatePreparedActions(
            CharacterPoseSourceModule sourceModule)
        {
            int retirementCount = 0;
            for (int requestIndex = 0;
                 requestIndex < m_PreparedAction.Count;
                 requestIndex++)
            {
                PreparedActionRetirement prepared =
                    m_PreparedAction[requestIndex];
                ActionBackendReleaseRequest request =
                    prepared?.Request ??
                    throw new InvalidOperationException(
                        "Prepared Action backend release has no request.");
                prepared.Sources.Sort(s_PendingActionComparison);
                m_ExpectedActionSources.Clear();
                for (int i = 0; i < prepared.Sources.Count; i++)
                {
                    PendingActionRetirement candidate =
                        prepared.Sources[i];
                    if (candidate == null ||
                        !m_PendingAction.Contains(candidate) ||
                        candidate.RequestIdentity !=
                            request.RequestIdentity ||
                        !candidate.Release.SourceId.PlaybackId.Equals(
                            request.PlaybackId) ||
                        !m_ExpectedActionSources.Add(
                            candidate.PlayableSource) ||
                        !m_ExpectedActionSources.Add(
                            candidate.StoredPoseSource))
                    {
                        throw new InvalidOperationException(
                            "Action backend release request contains a detached or duplicate source.");
                    }
                    candidate.SourceRetirement =
                        PrepareSourceRetirement(
                            sourceModule,
                            candidate.Release.SourceId,
                            candidate.Release.PoseNodeId,
                            candidate.PhysicalSource);
                    candidate.PlayableCompletionIdentity =
                        NextActionCompletionIdentity();
                    candidate.StoredPoseCompletionIdentity =
                        NextActionCompletionIdentity();
                    retirementCount = checked(retirementCount + 1);
                }
                if (prepared.Sources.Count == 0 ||
                    m_ExpectedActionSources.Count !=
                        request.Sources.Count)
                {
                    throw new InvalidOperationException(
                        "Action backend release request source set is incomplete.");
                }
                for (int i = 0; i < request.Sources.Count; i++)
                {
                    if (!m_ExpectedActionSources.Contains(
                            request.Sources[i]))
                    {
                        throw new InvalidOperationException(
                            "Action backend release request source set is not exact.");
                    }
                }
            }
            if (retirementCount != m_PendingAction.Count)
            {
                throw new InvalidOperationException(
                    "Action backend release journal contains sources without a prepared request.");
            }
            return retirementCount;
        }

        void PrepareRouteNotifications()
        {
            for (int i = 0; i < m_PendingPose.Count; i++)
                m_PendingPose[i].NotifyRouteAfterApply = false;
            for (int i = 0; i < m_PendingAction.Count; i++)
                m_PendingAction[i].NotifyRouteAfterApply = false;
            for (int poseIndex = 0;
                 poseIndex < m_PendingPose.Count;
                 poseIndex++)
            {
                PendingPoseRetirement candidate =
                    m_PendingPose[poseIndex];
                if (!HasActionRouteRelease(
                        candidate.Route,
                        candidate.Release.CompletionIdentity) &&
                    !HasLaterPoseRouteRelease(
                        poseIndex,
                        candidate.Route,
                        candidate.Release.CompletionIdentity))
                {
                    candidate.NotifyRouteAfterApply = true;
                }
            }
            for (int requestIndex = 0;
                 requestIndex < m_PreparedAction.Count;
                 requestIndex++)
            {
                PreparedActionRetirement request =
                    m_PreparedAction[requestIndex];
                for (int sourceIndex = 0;
                     sourceIndex < request.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionRetirement candidate =
                        request.Sources[sourceIndex];
                    if (!HasLaterPreparedActionRouteRelease(
                            requestIndex,
                            sourceIndex,
                            candidate.Route,
                            candidate.Release.CompletionIdentity))
                    {
                        candidate.NotifyRouteAfterApply = true;
                    }
                }
            }
        }

        bool HasActionRouteRelease(
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int i = 0; i < m_PendingAction.Count; i++)
            {
                PendingActionRetirement pending = m_PendingAction[i];
                if (ReferenceEquals(pending.Route, route) &&
                    pending.Release.CompletionIdentity ==
                        completionIdentity)
                {
                    return true;
                }
            }
            return false;
        }

        bool HasLaterPoseRouteRelease(
            int currentIndex,
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int i = currentIndex + 1;
                 i < m_PendingPose.Count;
                 i++)
            {
                PendingPoseRetirement pending = m_PendingPose[i];
                if (ReferenceEquals(pending.Route, route) &&
                    pending.Release.CompletionIdentity ==
                        completionIdentity)
                {
                    return true;
                }
            }
            return false;
        }

        bool HasLaterPreparedActionRouteRelease(
            int currentRequestIndex,
            int currentSourceIndex,
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int requestIndex = currentRequestIndex;
                 requestIndex < m_PreparedAction.Count;
                 requestIndex++)
            {
                PreparedActionRetirement request =
                    m_PreparedAction[requestIndex];
                int sourceStart = requestIndex == currentRequestIndex
                    ? currentSourceIndex + 1
                    : 0;
                for (int sourceIndex = sourceStart;
                     sourceIndex < request.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionRetirement pending =
                        request.Sources[sourceIndex];
                    if (ReferenceEquals(pending.Route, route) &&
                        pending.Release.CompletionIdentity ==
                            completionIdentity)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        PendingPoseRetirement RentPendingPose()
        {
            for (int i = 0; i < m_PendingPosePool.Length; i++)
            {
                PendingPoseRetirement candidate = m_PendingPosePool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Pending Pose source release journal capacity was exceeded.");
        }

        PendingActionRetirement RentPendingAction()
        {
            for (int i = 0; i < m_PendingActionPool.Length; i++)
            {
                PendingActionRetirement candidate = m_PendingActionPool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Pending Action backend release journal capacity was exceeded.");
        }

        PreparedActionRetirement RentPreparedAction()
        {
            for (int i = 0; i < m_PreparedActionPool.Length; i++)
            {
                PreparedActionRetirement candidate =
                    m_PreparedActionPool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Prepared Action backend release journal capacity was exceeded.");
        }

        static void ReturnPreparedAction(
            PreparedActionRetirement prepared)
        {
            if (prepared == null || !prepared.InUse)
            {
                throw new InvalidOperationException(
                    "Prepared Action backend release journal entry is not active.");
            }
            prepared.Clear();
        }

        ulong NextActionRequestIdentity()
        {
            m_ActionRequestIdentity++;
            if (m_ActionRequestIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Action backend release request identity was exhausted.");
            }
            return m_ActionRequestIdentity;
        }

        ulong NextActionCompletionIdentity()
        {
            m_ActionCompletionIdentity++;
            if (m_ActionCompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Action backend release completion identity was exhausted.");
            }
            return m_ActionCompletionIdentity;
        }

        static CharacterPoseSourceRetirementHandle
            PrepareSourceRetirement(
                CharacterPoseSourceModule sourceModule,
                AnimationPoseSourceId sourceId,
                PoseNodeId poseNodeId,
                AnimationPhysicalSourceIdentity expectedPhysicalIdentity)
        {
            var permission =
                new CharacterPoseSourceRetirementPermission(
                    sourceId,
                    poseNodeId,
                    expectedPhysicalIdentity);
            return sourceModule.PrepareRetirement(in permission);
        }

        static void AddFixed<T>(
            List<T> destination,
            T value,
            string journalName)
        {
            if (destination.Count >= destination.Capacity)
            {
                throw new InvalidOperationException(
                    $"{journalName} capacity was exceeded.");
            }
            destination.Add(value);
        }

        internal static bool IsFiniteActionSource(
            AnimationPoseSourceId sourceId) =>
            sourceId.IsValid &&
            sourceId.SourceKind == AnimationPoseSourceKind.Timeline &&
            sourceId.SourceActionInstanceId != 0;

        internal void ClearStandalone()
        {
            Array.Clear(
                m_Standalone,
                0,
                m_StandaloneCount);
            m_StandaloneCount = 0;
        }

        void Prepare(
            StandaloneOwner owner,
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player)
        {
            if (owner == 0 ||
                playerIndex < 0 ||
                !source.IsValid ||
                !player.IsValid ||
                m_StandaloneCount >= m_Standalone.Length)
            {
                throw new InvalidOperationException(
                    "Standalone pose source release exceeds its compiled journal.");
            }
            CharacterPoseSourceRetirementPermission permission =
                source.Permission;
            m_Standalone[m_StandaloneCount++] =
                new StandaloneRetirement
                {
                    Owner = owner,
                    PlayerIndex = playerIndex,
                    SourceId = permission.SourceId,
                    NodeId = permission.PoseNodeId,
                    Source = source,
                    Player = player
                };
        }
    }
}
