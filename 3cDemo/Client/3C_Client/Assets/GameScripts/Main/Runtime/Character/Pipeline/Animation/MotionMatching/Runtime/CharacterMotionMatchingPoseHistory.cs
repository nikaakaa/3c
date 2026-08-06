using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingBasePoseContinuityIdentity : IEquatable<MotionMatchingBasePoseContinuityIdentity>
    {
        public MotionMatchingBasePoseContinuityIdentity(
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceIndex sourceIndex,
            MotionMatchingSelectionGeneration selectionGeneration,
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity)
        {
            if (!providerId.IsValid || !sourceIndex.IsValid ||
                !selectionGeneration.IsValid || databaseIdentity == null)
                throw new ArgumentException("Motion Matching Base Pose continuity identity is invalid.");
            ProviderId = providerId;
            SourceIndex = sourceIndex;
            SelectionGeneration = selectionGeneration;
            DatabaseIdentity = databaseIdentity;
        }

        public PresentationPoseSourceProviderId ProviderId { get; }
        public PresentationPoseSourceIndex SourceIndex { get; }
        public MotionMatchingSelectionGeneration SelectionGeneration { get; }
        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public bool IsValid => ProviderId.IsValid && SourceIndex.IsValid &&
            SelectionGeneration.IsValid && DatabaseIdentity != null;
        public bool Equals(MotionMatchingBasePoseContinuityIdentity other) =>
            ProviderId == other.ProviderId &&
            SourceIndex == other.SourceIndex &&
            SelectionGeneration.Equals(other.SelectionGeneration) && DatabaseIdentity != null &&
            other.DatabaseIdentity != null && DatabaseIdentity.EqualsExact(other.DatabaseIdentity);
        public override bool Equals(object obj) => obj is MotionMatchingBasePoseContinuityIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            ProviderId,
            SourceIndex,
            SelectionGeneration,
            DatabaseIdentity?.ContentHash);
    }

    public readonly struct MotionMatchingBasePoseFrameInput
    {
        public MotionMatchingBasePoseFrameInput(
            float presentationTime,
            MotionMatchingBasePoseContinuityIdentity continuityIdentity,
            IReadOnlyList<Vector3> boneLocalPositions,
            AnimationFootPlacementSample footPlacement)
        {
            if (!float.IsFinite(presentationTime) || presentationTime < 0f || !continuityIdentity.IsValid ||
                boneLocalPositions == null || boneLocalPositions.Count == 0 || !footPlacement.IsValid)
                throw new ArgumentException("Motion Matching Base Pose frame input is incomplete.");
            PresentationTime = presentationTime;
            ContinuityIdentity = continuityIdentity;
            BoneLocalPositions = boneLocalPositions;
            FootPlacement = footPlacement;
        }

        public float PresentationTime { get; }
        public MotionMatchingBasePoseContinuityIdentity ContinuityIdentity { get; }
        public IReadOnlyList<Vector3> BoneLocalPositions { get; }
        public AnimationFootPlacementSample FootPlacement { get; }
    }

    public sealed class CharacterMotionMatchingPoseHistory :
        IMotionMatchingPoseHistoryReadView
    {
        enum PendingMutation : byte
        {
            None = 0,
            Reserved = 1,
            Append = 2,
            Gap = 3,
            Skip = 4
        }

        readonly int m_BoneCount;
        readonly int m_Capacity;
        readonly float[] m_PresentationTimes;
        readonly MotionMatchingBasePoseContinuityIdentity[] m_Continuities;
        readonly Vector3[] m_Positions;
        readonly Vector3[] m_Velocities;
        readonly AnimationFootPlacementSample[] m_FootPlacement;
        readonly Vector3[] m_PendingPositions;
        readonly Vector3[] m_PendingVelocities;
        int m_Start;
        int m_Count;
        ulong m_ResetSequence;
        bool m_HasGap;
        PendingMutation m_PendingMutation;
        float m_PendingPresentationTime;
        MotionMatchingBasePoseContinuityIdentity m_PendingContinuity;
        AnimationFootPlacementSample m_PendingFootPlacement;
        ulong m_PendingResetSequence;
        int m_PendingPreviousPhysical;
        float m_PendingDeltaTime;
        bool m_FrameOpen;

        public CharacterMotionMatchingPoseHistory(int boneCount, int capacity)
        {
            if (boneCount <= 0 || capacity <= 0)
                throw new ArgumentOutOfRangeException();
            m_BoneCount = boneCount;
            m_Capacity = capacity;
            m_PresentationTimes = new float[capacity];
            m_Continuities = new MotionMatchingBasePoseContinuityIdentity[capacity];
            m_Positions = new Vector3[capacity * boneCount];
            m_Velocities = new Vector3[capacity * boneCount];
            m_FootPlacement = new AnimationFootPlacementSample[capacity];
            m_PendingPositions = new Vector3[boneCount];
            m_PendingVelocities = new Vector3[boneCount];
        }

        public int BoneCount => m_BoneCount;
        public int Capacity => m_Capacity;
        public int Count => m_Count;
        public bool HasGap => m_HasGap;
        public ulong ResetSequence => m_ResetSequence;
        public float LatestPresentationTime => m_Count == 0 ? 0f : m_PresentationTimes[PhysicalIndex(m_Count - 1)];
        public AnimationFootPlacementSample LatestFootPlacement => m_Count == 0 ? default : m_FootPlacement[PhysicalIndex(m_Count - 1)];
        public MotionMatchingBasePoseContinuityIdentity LatestContinuity => m_Count == 0 ? default : m_Continuities[PhysicalIndex(m_Count - 1)];

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Pose History frame is already open.");
            m_PendingMutation = PendingMutation.None;
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            if (m_PendingMutation == PendingMutation.Append)
                CommitPendingAppend();
            else if (m_PendingMutation == PendingMutation.Gap)
                ApplyGap(m_PendingResetSequence);
            ClearPending();
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            ClearPending();
        }

        internal bool PrepareCompletion(
            float presentationTime,
            in MotionMatchingBasePoseContinuityIdentity continuity,
            ulong resetSequence)
        {
            RequireOpenFrame();
            if (m_PendingMutation != PendingMutation.None ||
                !float.IsFinite(presentationTime) ||
                presentationTime < 0f ||
                !continuity.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose History completion cannot be prepared.");
            }
            bool preservesHistory = resetSequence == m_ResetSequence;
            if (preservesHistory &&
                m_Count > 0 &&
                presentationTime <= LatestPresentationTime)
            {
                m_PendingMutation = PendingMutation.Skip;
                return false;
            }
            m_PendingPreviousPhysical =
                !preservesHistory || m_Count == 0
                    ? -1
                    : PhysicalIndex(m_Count - 1);
            m_PendingDeltaTime =
                m_PendingPreviousPhysical < 0
                    ? 0f
                    : presentationTime -
                      m_PresentationTimes[m_PendingPreviousPhysical];
            m_PendingPresentationTime = presentationTime;
            m_PendingContinuity = continuity;
            m_PendingResetSequence = resetSequence;
            m_PendingMutation = PendingMutation.Reserved;
            return true;
        }

        internal void CompletePreparedAppend(
            Vector3[] boneLocalPositions,
            in AnimationFootPlacementSample footPlacement)
        {
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                Vector3 position = boneLocalPositions[boneIndex];
                m_PendingPositions[boneIndex] = position;
                m_PendingVelocities[boneIndex] =
                    m_PendingPreviousPhysical < 0
                        ? Vector3.zero
                        : (position -
                           m_Positions[
                               m_PendingPreviousPhysical *
                               m_BoneCount +
                               boneIndex]) /
                          m_PendingDeltaTime;
            }
            m_PendingFootPlacement = footPlacement;
            m_PendingMutation = PendingMutation.Append;
        }

        internal void CompletePreparedGap()
        {
            m_PendingMutation = PendingMutation.Gap;
        }

        void CommitPendingAppend()
        {
            if (m_PendingResetSequence != m_ResetSequence)
                ApplyReset(m_PendingResetSequence);
            int physical = m_Count < m_Capacity ? PhysicalIndex(m_Count) : m_Start;
            int destination = physical * m_BoneCount;
            Array.Copy(m_PendingPositions, 0, m_Positions, destination, m_BoneCount);
            Array.Copy(m_PendingVelocities, 0, m_Velocities, destination, m_BoneCount);
            m_PresentationTimes[physical] = m_PendingPresentationTime;
            m_Continuities[physical] = m_PendingContinuity;
            m_FootPlacement[physical] = m_PendingFootPlacement;
            if (m_Count < m_Capacity)
                m_Count++;
            else
                m_Start = (m_Start + 1) % m_Capacity;
            m_HasGap = false;
        }

        public void MarkGap(ulong resetSequence)
        {
            if (m_FrameOpen)
            {
                if (m_PendingMutation != PendingMutation.None)
                    throw new InvalidOperationException("Motion Matching Pose History already has a pending mutation.");
                m_PendingResetSequence = resetSequence;
                m_PendingMutation = PendingMutation.Gap;
                return;
            }
            ApplyGap(resetSequence);
        }

        public bool TrySampleBone(float secondsBeforeLatest, int boneIndex, out Vector3 position, out Vector3 velocity)
        {
            if (!float.IsFinite(secondsBeforeLatest) || secondsBeforeLatest < 0f || (uint)boneIndex >= (uint)m_BoneCount || m_Count == 0 || m_HasGap)
            {
                position = default;
                velocity = default;
                return false;
            }
            float targetTime = LatestPresentationTime - secondsBeforeLatest;
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
                if (logical == m_Count - 1 || m_PresentationTimes[current] == targetTime)
                {
                    position = m_Positions[current * m_BoneCount + boneIndex];
                    velocity = m_Velocities[current * m_BoneCount + boneIndex];
                    return true;
                }
                int next = PhysicalIndex(logical + 1);
                float alpha = Mathf.InverseLerp(m_PresentationTimes[current], m_PresentationTimes[next], targetTime);
                position = Vector3.LerpUnclamped(
                    m_Positions[current * m_BoneCount + boneIndex],
                    m_Positions[next * m_BoneCount + boneIndex],
                    alpha);
                velocity = Vector3.LerpUnclamped(
                    m_Velocities[current * m_BoneCount + boneIndex],
                    m_Velocities[next * m_BoneCount + boneIndex],
                    alpha);
                return true;
            }
            position = default;
            velocity = default;
            return false;
        }

        public bool CoversSecondsBeforeLatest(float secondsBeforeLatest)
        {
            if (!float.IsFinite(secondsBeforeLatest) || secondsBeforeLatest < 0f || m_Count == 0 || m_HasGap)
                return false;
            return LatestPresentationTime - secondsBeforeLatest >= m_PresentationTimes[PhysicalIndex(0)];
        }

        public void Reset(ulong resetSequence)
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Pose History cannot reset while a frame is open.");
            ApplyReset(resetSequence);
        }

        void ApplyReset(ulong resetSequence)
        {
            Array.Clear(m_PresentationTimes, 0, m_PresentationTimes.Length);
            Array.Clear(m_Continuities, 0, m_Continuities.Length);
            Array.Clear(m_Positions, 0, m_Positions.Length);
            Array.Clear(m_Velocities, 0, m_Velocities.Length);
            Array.Clear(m_FootPlacement, 0, m_FootPlacement.Length);
            m_Start = 0;
            m_Count = 0;
            m_ResetSequence = resetSequence;
            m_HasGap = false;
        }

        void ApplyGap(ulong resetSequence)
        {
            ApplyReset(resetSequence);
            m_HasGap = true;
        }

        void ClearPending()
        {
            m_PendingMutation = PendingMutation.None;
            m_PendingPresentationTime = 0f;
            m_PendingContinuity = default;
            m_PendingFootPlacement = default;
            m_PendingResetSequence = 0;
            m_PendingPreviousPhysical = -1;
            m_PendingDeltaTime = 0f;
            m_FrameOpen = false;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Pose History has no open frame.");
        }

        int PhysicalIndex(int logicalIndex) => (m_Start + logicalIndex) % m_Capacity;
        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
