using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal readonly struct CharacterMotionMatchingSourceLineage
    {
        internal CharacterMotionMatchingSourceLineage(
            PoseNodeId nodeId,
            CharacterMotionMatchingBindingId bindingId,
            int bindingRevision,
            CharacterMotionMatchingProfileId profileId,
            int profileRevision,
            CharacterMotionMatchingDatabaseChooserId chooserId,
            int chooserRevision,
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            CharacterMotionMatchingSourceSetId sourceSetId,
            int sourceSetRevision,
            CharacterMotionMatchingSourceClipId sourceClipId,
            CharacterMotionMatchingSegmentId segmentId,
            CharacterMotionMatchingSampleId sampleId,
            float sampleTime,
            MotionMatchingSelectionGeneration selectionGeneration)
        {
            if (!nodeId.IsValid || !bindingId.IsValid || bindingRevision <= 0 ||
                !profileId.IsValid || profileRevision <= 0 || !chooserId.IsValid || chooserRevision <= 0 ||
                databaseIdentity == null || !sourceSetId.IsValid || sourceSetRevision <= 0 ||
                !sourceClipId.IsValid || !segmentId.IsValid || !sampleId.IsValid ||
                !float.IsFinite(sampleTime) || sampleTime < 0f || !selectionGeneration.IsValid)
            {
                throw new ArgumentException("Motion Matching source lineage is incomplete.");
            }
            NodeId = nodeId;
            BindingId = bindingId;
            BindingRevision = bindingRevision;
            ProfileId = profileId;
            ProfileRevision = profileRevision;
            ChooserId = chooserId;
            ChooserRevision = chooserRevision;
            DatabaseIdentity = databaseIdentity;
            SourceSetId = sourceSetId;
            SourceSetRevision = sourceSetRevision;
            SourceClipId = sourceClipId;
            SegmentId = segmentId;
            SampleId = sampleId;
            SampleTime = sampleTime;
            SelectionGeneration = selectionGeneration;
        }

        internal PoseNodeId NodeId { get; }
        internal CharacterMotionMatchingBindingId BindingId { get; }
        internal int BindingRevision { get; }
        internal CharacterMotionMatchingProfileId ProfileId { get; }
        internal int ProfileRevision { get; }
        internal CharacterMotionMatchingDatabaseChooserId ChooserId { get; }
        internal int ChooserRevision { get; }
        internal CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        internal CharacterMotionMatchingSourceSetId SourceSetId { get; }
        internal int SourceSetRevision { get; }
        internal CharacterMotionMatchingSourceClipId SourceClipId { get; }
        internal CharacterMotionMatchingSegmentId SegmentId { get; }
        internal CharacterMotionMatchingSampleId SampleId { get; }
        internal float SampleTime { get; }
        internal MotionMatchingSelectionGeneration SelectionGeneration { get; }
        internal bool IsValid => NodeId.IsValid && BindingId.IsValid && BindingRevision > 0 &&
                                 ProfileId.IsValid && ProfileRevision > 0 && ChooserId.IsValid && ChooserRevision > 0 &&
                                 DatabaseIdentity != null && SourceSetId.IsValid && SourceSetRevision > 0 &&
                                 SourceClipId.IsValid && SegmentId.IsValid && SampleId.IsValid &&
                                 float.IsFinite(SampleTime) && SampleTime >= 0f && SelectionGeneration.IsValid;
    }

    internal readonly struct CharacterPoseHistoryRootKinematics
    {
        internal CharacterPoseHistoryRootKinematics(
            AnimationLocalBonePose rootPose,
            Vector3 linearVelocity,
            Vector3 angularVelocity)
        {
            if (!rootPose.IsValid || !Finite(linearVelocity) || !Finite(angularVelocity))
                throw new ArgumentException("Pose History root kinematics are invalid.");
            RootPose = rootPose;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }

        internal AnimationLocalBonePose RootPose { get; }
        internal Vector3 LinearVelocity { get; }
        internal Vector3 AngularVelocity { get; }
        internal bool IsValid => RootPose.IsValid && Finite(LinearVelocity) && Finite(AngularVelocity);
        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal readonly struct CharacterPoseHistoryReadView :
        IMotionMatchingPoseHistoryReadView
    {
        readonly CharacterPoseHistoryCollectorRuntime m_Owner;
        readonly ulong m_Generation;

        internal CharacterPoseHistoryReadView(
            CharacterPoseHistoryCollectorRuntime owner,
            ulong generation,
            ulong readFrameIdentity)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (generation == 0 || readFrameIdentity == 0)
                throw new ArgumentException("Pose History read view identity is incomplete.");
            m_Generation = generation;
            ReadFrameIdentity = readFrameIdentity;
        }

        internal ulong ReadFrameIdentity { get; }
        internal CharacterMotionMatchingRigLineage RigLineage => RequireOwner().RigLineage;
        internal CharacterMotionMatchingSourceLineage LatestSourceLineage => RequireOwner().LatestSourceLineage;
        internal CharacterPoseHistoryRootKinematics LatestRootKinematics => RequireOwner().LatestRootKinematics;
        internal bool IsValid => m_Owner != null && m_Owner.CommittedGeneration == m_Generation;
        public int Count => RequireOwner().Count;
        public bool HasGap => RequireOwner().HasGap;
        public ulong ResetSequence => RequireOwner().ResetSequence;
        public AnimationFootPlacementSample LatestFootPlacement => RequireOwner().LatestFootPlacement;

        public bool CoversSecondsBeforeLatest(float secondsBeforeLatest) =>
            RequireOwner().CoversSecondsBeforeLatest(secondsBeforeLatest);

        public bool TrySampleBone(
            float secondsBeforeLatest,
            int boneIndex,
            out Vector3 position,
            out Vector3 velocity) =>
            RequireOwner().TrySampleBone(
                secondsBeforeLatest,
                boneIndex,
                out position,
                out velocity);

        CharacterPoseHistoryCollectorRuntime RequireOwner()
        {
            if (m_Owner == null || m_Owner.CommittedGeneration != m_Generation)
                throw new InvalidOperationException("Pose History read view is stale.");
            return m_Owner;
        }
    }

    internal sealed class CharacterPoseHistoryCollectorRuntime
    {
        readonly CharacterPoseHistoryId m_HistoryId;
        readonly CharacterMotionMatchingRigLineage m_RigLineage;
        readonly int m_BoneCount;
        readonly int m_Capacity;
        readonly double[] m_PresentationTimes;
        readonly ulong[] m_FrameIdentities;
        readonly CharacterMotionMatchingSourceLineage[] m_SourceLineages;
        readonly CharacterPoseHistoryRootKinematics[] m_RootKinematics;
        readonly AnimationFootPlacementSample[] m_FootPlacement;
        readonly Vector3[] m_Positions;
        readonly Vector3[] m_Velocities;
        readonly Vector3[] m_PendingPositions;
        readonly Vector3[] m_PendingVelocities;

        int m_Start;
        int m_Count;
        ulong m_ResetSequence;
        ulong m_CommittedGeneration = 1;
        ulong m_OpenFrameIdentity;
        double m_PendingPresentationTime;
        ulong m_PendingFrameIdentity;
        CharacterMotionMatchingSourceLineage m_PendingSourceLineage;
        CharacterPoseHistoryRootKinematics m_PendingRootKinematics;
        AnimationFootPlacementSample m_PendingFootPlacement;
        bool m_HasGap;
        bool m_FrameOpen;
        bool m_CommitPrepared;

        internal CharacterPoseHistoryCollectorRuntime(
            CharacterPoseHistoryId historyId,
            CharacterMotionMatchingRigLineage rigLineage,
            int boneCount,
            int capacity)
        {
            if (!historyId.IsValid || !rigLineage.IsValid || boneCount <= 0 || capacity <= 0)
                throw new ArgumentException("Pose History Collector runtime layout is incomplete.");
            m_HistoryId = historyId;
            m_RigLineage = rigLineage;
            m_BoneCount = boneCount;
            m_Capacity = capacity;
            m_PresentationTimes = new double[capacity];
            m_FrameIdentities = new ulong[capacity];
            m_SourceLineages = new CharacterMotionMatchingSourceLineage[capacity];
            m_RootKinematics = new CharacterPoseHistoryRootKinematics[capacity];
            m_FootPlacement = new AnimationFootPlacementSample[capacity];
            m_Positions = new Vector3[checked(capacity * boneCount)];
            m_Velocities = new Vector3[checked(capacity * boneCount)];
            m_PendingPositions = new Vector3[boneCount];
            m_PendingVelocities = new Vector3[boneCount];
        }

        internal CharacterPoseHistoryId HistoryId => m_HistoryId;
        internal CharacterMotionMatchingRigLineage RigLineage => m_RigLineage;
        internal int BoneCount => m_BoneCount;
        internal int Capacity => m_Capacity;
        internal int Count => m_Count;
        internal bool HasGap => m_HasGap;
        internal ulong ResetSequence => m_ResetSequence;
        internal ulong CommittedGeneration => m_CommittedGeneration;
        internal CharacterMotionMatchingSourceLineage LatestSourceLineage =>
            m_Count == 0 ? default : m_SourceLineages[PhysicalIndex(m_Count - 1)];
        internal CharacterPoseHistoryRootKinematics LatestRootKinematics =>
            m_Count == 0 ? default : m_RootKinematics[PhysicalIndex(m_Count - 1)];
        internal AnimationFootPlacementSample LatestFootPlacement =>
            m_Count == 0 ? default : m_FootPlacement[PhysicalIndex(m_Count - 1)];
        double LatestPresentationTime =>
            m_Count == 0 ? 0d : m_PresentationTimes[PhysicalIndex(m_Count - 1)];

        internal CharacterPoseHistoryReadView BeginFrame(
            in CharacterMotionMatchingFrameContext frameContext)
        {
            if (m_FrameOpen || !frameContext.IsValid ||
                frameContext.FrameIdentity <= m_OpenFrameIdentity)
            {
                throw new InvalidOperationException("Pose History Collector frame cannot be opened.");
            }
            if (!m_RigLineage.Equals(frameContext.RigLineage))
                throw new InvalidOperationException("Pose History Collector Rig lineage does not match the Frame Context.");
            if (m_ResetSequence != frameContext.ResetSequence)
                ApplyReset(frameContext.ResetSequence, false);
            m_OpenFrameIdentity = frameContext.FrameIdentity;
            m_FrameOpen = true;
            m_CommitPrepared = false;
            return new CharacterPoseHistoryReadView(
                this,
                m_CommittedGeneration,
                frameContext.FrameIdentity);
        }

        internal void PrepareCommit(
            in CharacterMotionMatchingFrameContext frameContext,
            in CharacterMotionMatchingSourceLineage sourceLineage,
            AnimationLocalBonePose rootPose,
            Vector3[] featureBoneLocalPositions,
            in AnimationFootPlacementSample footPlacement)
        {
            RequireOpenFrame(frameContext.FrameIdentity);
            if (m_CommitPrepared || !sourceLineage.IsValid ||
                sourceLineage.NodeId == default || !rootPose.IsValid ||
                featureBoneLocalPositions == null || featureBoneLocalPositions.Length != m_BoneCount ||
                !footPlacement.IsValid)
            {
                throw new InvalidOperationException("Pose History Collector commit is incomplete or duplicated.");
            }
            double presentationTime = frameContext.Facts.PresentationTime;
            if (double.IsNaN(presentationTime) || double.IsInfinity(presentationTime) || presentationTime < 0d ||
                m_Count > 0 && presentationTime <= LatestPresentationTime)
            {
                throw new InvalidOperationException("Pose History Collector presentation time is not strictly increasing.");
            }
            int previousPhysical = m_Count == 0 ? -1 : PhysicalIndex(m_Count - 1);
            float deltaTime = previousPhysical < 0
                ? 0f
                : (float)(presentationTime - m_PresentationTimes[previousPhysical]);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                Vector3 position = featureBoneLocalPositions[boneIndex];
                if (!Finite(position))
                    throw new InvalidOperationException($"Pose History Collector feature Bone #{boneIndex} is invalid.");
                m_PendingPositions[boneIndex] = position;
                m_PendingVelocities[boneIndex] = previousPhysical < 0
                    ? Vector3.zero
                    : (position - m_Positions[previousPhysical * m_BoneCount + boneIndex]) / deltaTime;
            }
            Vector3 rootLinearVelocity = previousPhysical < 0
                ? Vector3.zero
                : (rootPose.Position - m_RootKinematics[previousPhysical].RootPose.Position) / deltaTime;
            Vector3 rootAngularVelocity = previousPhysical < 0
                ? Vector3.zero
                : AnimationPoseMath.QuaternionLog(
                    rootPose.Rotation *
                    Quaternion.Inverse(m_RootKinematics[previousPhysical].RootPose.Rotation)) / deltaTime;
            m_PendingPresentationTime = presentationTime;
            m_PendingFrameIdentity = frameContext.FrameIdentity;
            m_PendingSourceLineage = sourceLineage;
            m_PendingRootKinematics = new CharacterPoseHistoryRootKinematics(
                rootPose,
                rootLinearVelocity,
                rootAngularVelocity);
            m_PendingFootPlacement = footPlacement;
            m_CommitPrepared = true;
        }

        internal void CommitFrame(ulong frameIdentity)
        {
            RequireOpenFrame(frameIdentity);
            if (!m_CommitPrepared)
                throw new InvalidOperationException("Pose History Collector frame has no completed base Pose commit.");
            int physical = m_Count < m_Capacity ? PhysicalIndex(m_Count) : m_Start;
            Array.Copy(m_PendingPositions, 0, m_Positions, physical * m_BoneCount, m_BoneCount);
            Array.Copy(m_PendingVelocities, 0, m_Velocities, physical * m_BoneCount, m_BoneCount);
            m_PresentationTimes[physical] = m_PendingPresentationTime;
            m_FrameIdentities[physical] = m_PendingFrameIdentity;
            m_SourceLineages[physical] = m_PendingSourceLineage;
            m_RootKinematics[physical] = m_PendingRootKinematics;
            m_FootPlacement[physical] = m_PendingFootPlacement;
            if (m_Count < m_Capacity)
                m_Count++;
            else
                m_Start = (m_Start + 1) % m_Capacity;
            m_HasGap = false;
            SealFrame();
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireOpenFrame(frameIdentity);
            SealFrame();
        }

        internal void MarkGap(ulong resetSequence)
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Pose History Collector cannot mark a gap while a frame is open.");
            ApplyReset(resetSequence, true);
        }

        internal bool CoversSecondsBeforeLatest(float secondsBeforeLatest)
        {
            if (!float.IsFinite(secondsBeforeLatest) || secondsBeforeLatest < 0f || m_Count == 0 || m_HasGap)
                return false;
            return LatestPresentationTime - secondsBeforeLatest >= m_PresentationTimes[PhysicalIndex(0)];
        }

        internal bool TrySampleBone(
            float secondsBeforeLatest,
            int boneIndex,
            out Vector3 position,
            out Vector3 velocity)
        {
            if (!float.IsFinite(secondsBeforeLatest) || secondsBeforeLatest < 0f ||
                (uint)boneIndex >= (uint)m_BoneCount || m_Count == 0 || m_HasGap)
            {
                position = default;
                velocity = default;
                return false;
            }
            double targetTime = LatestPresentationTime - secondsBeforeLatest;
            int first = PhysicalIndex(0);
            if (targetTime < m_PresentationTimes[first])
            {
                position = default;
                velocity = default;
                return false;
            }
            for (int logical = m_Count - 1; logical >= 0; logical--)
            {
                int current = PhysicalIndex(logical);
                if (m_PresentationTimes[current] > targetTime)
                    continue;
                int currentOffset = current * m_BoneCount + boneIndex;
                if (logical == m_Count - 1 || m_PresentationTimes[current] == targetTime)
                {
                    position = m_Positions[currentOffset];
                    velocity = m_Velocities[currentOffset];
                    return true;
                }
                int next = PhysicalIndex(logical + 1);
                float alpha = (float)((targetTime - m_PresentationTimes[current]) /
                                      (m_PresentationTimes[next] - m_PresentationTimes[current]));
                int nextOffset = next * m_BoneCount + boneIndex;
                position = Vector3.LerpUnclamped(m_Positions[currentOffset], m_Positions[nextOffset], alpha);
                velocity = Vector3.LerpUnclamped(m_Velocities[currentOffset], m_Velocities[nextOffset], alpha);
                return true;
            }
            position = default;
            velocity = default;
            return false;
        }

        void ApplyReset(ulong resetSequence, bool gap)
        {
            Array.Clear(m_PresentationTimes, 0, m_PresentationTimes.Length);
            Array.Clear(m_FrameIdentities, 0, m_FrameIdentities.Length);
            Array.Clear(m_SourceLineages, 0, m_SourceLineages.Length);
            Array.Clear(m_RootKinematics, 0, m_RootKinematics.Length);
            Array.Clear(m_FootPlacement, 0, m_FootPlacement.Length);
            Array.Clear(m_Positions, 0, m_Positions.Length);
            Array.Clear(m_Velocities, 0, m_Velocities.Length);
            m_Start = 0;
            m_Count = 0;
            m_ResetSequence = resetSequence;
            m_HasGap = gap;
            AdvanceGeneration();
        }

        void SealFrame()
        {
            m_PendingPresentationTime = 0d;
            m_PendingFrameIdentity = 0;
            m_PendingSourceLineage = default;
            m_PendingRootKinematics = default;
            m_PendingFootPlacement = default;
            m_CommitPrepared = false;
            m_FrameOpen = false;
            AdvanceGeneration();
        }

        void AdvanceGeneration()
        {
            m_CommittedGeneration = m_CommittedGeneration == ulong.MaxValue
                ? 1
                : m_CommittedGeneration + 1;
        }

        void RequireOpenFrame(ulong frameIdentity)
        {
            if (!m_FrameOpen || frameIdentity == 0 || frameIdentity != m_OpenFrameIdentity)
                throw new InvalidOperationException("Pose History Collector frame identity is stale.");
        }

        int PhysicalIndex(int logicalIndex) => (m_Start + logicalIndex) % m_Capacity;
        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
