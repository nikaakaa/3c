using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingBasePoseContinuityIdentity : IEquatable<MotionMatchingBasePoseContinuityIdentity>
    {
        public MotionMatchingBasePoseContinuityIdentity(AnimationPlaybackId playbackId, MotionMatchingSelectionGeneration selectionGeneration)
        {
            if (!playbackId.IsValid || !selectionGeneration.IsValid)
                throw new ArgumentException("Motion Matching Base Pose continuity identity is invalid.");
            PlaybackId = playbackId;
            SelectionGeneration = selectionGeneration;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public MotionMatchingSelectionGeneration SelectionGeneration { get; }
        public bool IsValid => PlaybackId.IsValid && SelectionGeneration.IsValid;
        public bool Equals(MotionMatchingBasePoseContinuityIdentity other) => PlaybackId.Equals(other.PlaybackId) && SelectionGeneration.Equals(other.SelectionGeneration);
        public override bool Equals(object obj) => obj is MotionMatchingBasePoseContinuityIdentity other && Equals(other);
        public override int GetHashCode() => unchecked((PlaybackId.GetHashCode() * 397) ^ SelectionGeneration.GetHashCode());
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

    public sealed class CharacterMotionMatchingPoseHistory
    {
        readonly int m_BoneCount;
        readonly int m_Capacity;
        readonly float[] m_PresentationTimes;
        readonly MotionMatchingBasePoseContinuityIdentity[] m_Continuities;
        readonly Vector3[] m_Positions;
        readonly Vector3[] m_Velocities;
        readonly AnimationFootPlacementSample[] m_FootPlacement;
        int m_Start;
        int m_Count;
        ulong m_ResetSequence;
        bool m_HasGap;

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
        }

        public int BoneCount => m_BoneCount;
        public int Capacity => m_Capacity;
        public int Count => m_Count;
        public bool HasGap => m_HasGap;
        public ulong ResetSequence => m_ResetSequence;
        public float LatestPresentationTime => m_Count == 0 ? 0f : m_PresentationTimes[PhysicalIndex(m_Count - 1)];
        public AnimationFootPlacementSample LatestFootPlacement => m_Count == 0 ? default : m_FootPlacement[PhysicalIndex(m_Count - 1)];
        public MotionMatchingBasePoseContinuityIdentity LatestContinuity => m_Count == 0 ? default : m_Continuities[PhysicalIndex(m_Count - 1)];

        public void Append(MotionMatchingBasePoseFrameInput frame, ulong resetSequence)
        {
            if (resetSequence != m_ResetSequence)
                Reset(resetSequence);
            if (frame.BoneLocalPositions.Count != m_BoneCount)
                throw new ArgumentException("Motion Matching Base Pose Bone count does not match the compiled history.", nameof(frame));
            if (m_Count > 0 && frame.PresentationTime <= LatestPresentationTime)
                throw new InvalidOperationException("Motion Matching Base Pose history time must be strictly increasing.");
            int physical = m_Count < m_Capacity ? PhysicalIndex(m_Count) : m_Start;
            int previousPhysical = m_Count == 0 ? -1 : PhysicalIndex(m_Count - 1);
            float deltaTime = previousPhysical < 0 ? 0f : frame.PresentationTime - m_PresentationTimes[previousPhysical];
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                Vector3 position = frame.BoneLocalPositions[boneIndex];
                if (!IsFinite(position))
                    throw new ArgumentException($"Motion Matching Base Pose Bone #{boneIndex} is non-finite.", nameof(frame));
                int destination = physical * m_BoneCount + boneIndex;
                m_Positions[destination] = position;
                m_Velocities[destination] = previousPhysical < 0
                    ? Vector3.zero
                    : (position - m_Positions[previousPhysical * m_BoneCount + boneIndex]) / deltaTime;
            }
            m_PresentationTimes[physical] = frame.PresentationTime;
            m_Continuities[physical] = frame.ContinuityIdentity;
            m_FootPlacement[physical] = frame.FootPlacement;
            if (m_Count < m_Capacity)
                m_Count++;
            else
                m_Start = (m_Start + 1) % m_Capacity;
            m_HasGap = false;
        }

        public void MarkGap(ulong resetSequence)
        {
            if (resetSequence != m_ResetSequence)
                Reset(resetSequence);
            m_HasGap = true;
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

        int PhysicalIndex(int logicalIndex) => (m_Start + logicalIndex) % m_Capacity;
        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
