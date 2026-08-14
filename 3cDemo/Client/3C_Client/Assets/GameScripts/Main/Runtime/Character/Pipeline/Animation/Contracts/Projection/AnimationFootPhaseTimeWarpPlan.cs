using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public struct AnimationFootPhaseWarpKnot
    {
        [SerializeField] float m_LeaderFraction;
        [SerializeField] float m_FollowerFraction;

        public AnimationFootPhaseWarpKnot(
            float leaderFraction,
            float followerFraction)
        {
            m_LeaderFraction = leaderFraction;
            m_FollowerFraction = followerFraction;
            if (!IsValid)
                throw new ArgumentOutOfRangeException(nameof(leaderFraction));
        }

        public float LeaderFraction => m_LeaderFraction;
        public float FollowerFraction => m_FollowerFraction;
        public bool IsValid =>
            float.IsFinite(m_LeaderFraction) &&
            float.IsFinite(m_FollowerFraction) &&
            m_LeaderFraction >= 0f && m_LeaderFraction <= 1f &&
            m_FollowerFraction >= 0f && m_FollowerFraction <= 1f;
    }

    [Serializable]
    public sealed class AnimationFootPhaseWarpSegmentPlan
    {
        [SerializeField] int m_LeaderOccurrenceIndex = -1;
        [SerializeField] int m_FollowerOccurrenceIndex = -1;
        [SerializeField] string m_PreviousMarkerId = string.Empty;
        [SerializeField] string m_NextMarkerId = string.Empty;
        [SerializeField] AnimationFootPhaseWarpKnot[] m_Knots =
            Array.Empty<AnimationFootPhaseWarpKnot>();

        public AnimationFootPhaseWarpSegmentPlan(
            int leaderOccurrenceIndex,
            int followerOccurrenceIndex,
            string previousMarkerId,
            string nextMarkerId,
            AnimationFootPhaseWarpKnot[] knots)
        {
            m_LeaderOccurrenceIndex = leaderOccurrenceIndex;
            m_FollowerOccurrenceIndex = followerOccurrenceIndex;
            m_PreviousMarkerId = previousMarkerId?.Trim() ?? string.Empty;
            m_NextMarkerId = nextMarkerId?.Trim() ?? string.Empty;
            m_Knots = knots == null
                ? throw new ArgumentNullException(nameof(knots))
                : (AnimationFootPhaseWarpKnot[])knots.Clone();
            RequireValid();
        }

        public int LeaderOccurrenceIndex => m_LeaderOccurrenceIndex;
        public int FollowerOccurrenceIndex => m_FollowerOccurrenceIndex;
        public string PreviousMarkerId => m_PreviousMarkerId ?? string.Empty;
        public string NextMarkerId => m_NextMarkerId ?? string.Empty;
        public IReadOnlyList<AnimationFootPhaseWarpKnot> Knots =>
            m_Knots ?? Array.Empty<AnimationFootPhaseWarpKnot>();

        public float Evaluate(float leaderFraction)
        {
            RequireValid();
            if (!float.IsFinite(leaderFraction) || leaderFraction < 0f || leaderFraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(leaderFraction));
            for (int i = 1; i < m_Knots.Length; i++)
            {
                AnimationFootPhaseWarpKnot next = m_Knots[i];
                if (leaderFraction > next.LeaderFraction)
                    continue;
                AnimationFootPhaseWarpKnot previous = m_Knots[i - 1];
                float width = next.LeaderFraction - previous.LeaderFraction;
                float fraction = width <= 0f
                    ? 0f
                    : (leaderFraction - previous.LeaderFraction) / width;
                return Mathf.Lerp(
                    previous.FollowerFraction,
                    next.FollowerFraction,
                    fraction);
            }
            return 1f;
        }

        public void RequireValid()
        {
            if (m_LeaderOccurrenceIndex < 0 || m_FollowerOccurrenceIndex < 0 ||
                string.IsNullOrWhiteSpace(PreviousMarkerId) ||
                string.IsNullOrWhiteSpace(NextMarkerId) ||
                m_Knots == null || m_Knots.Length < 2 ||
                m_Knots[0].LeaderFraction != 0f ||
                m_Knots[0].FollowerFraction != 0f ||
                m_Knots[m_Knots.Length - 1].LeaderFraction != 1f ||
                m_Knots[m_Knots.Length - 1].FollowerFraction != 1f)
                throw new InvalidOperationException("Foot phase warp segment is invalid.");
            for (int i = 0; i < m_Knots.Length; i++)
            {
                if (!m_Knots[i].IsValid ||
                    i > 0 &&
                    (m_Knots[i].LeaderFraction <= m_Knots[i - 1].LeaderFraction ||
                     m_Knots[i].FollowerFraction <= m_Knots[i - 1].FollowerFraction))
                    throw new InvalidOperationException("Foot phase warp knots are not strictly monotonic.");
            }
        }
    }

    [Serializable]
    public sealed class AnimationFootPhaseTimeWarpPlan
    {
        public const string AlgorithmIdentity = "animation-foot-phase-time-warp/v1";

        [SerializeField] string m_PlanIdentity = string.Empty;
        [SerializeField] string m_AlgorithmIdentity = AlgorithmIdentity;
        [SerializeField] string m_LeaderArtifactHash = string.Empty;
        [SerializeField] string m_FollowerArtifactHash = string.Empty;
        [SerializeField] string m_LeaderSourceIdentity = string.Empty;
        [SerializeField] string m_FollowerSourceIdentity = string.Empty;
        [SerializeField] AnimationFootPhaseWarpSegmentPlan[] m_Segments =
            Array.Empty<AnimationFootPhaseWarpSegmentPlan>();

        public AnimationFootPhaseTimeWarpPlan(
            string planIdentity,
            string leaderArtifactHash,
            string followerArtifactHash,
            string leaderSourceIdentity,
            string followerSourceIdentity,
            AnimationFootPhaseWarpSegmentPlan[] segments)
        {
            m_PlanIdentity = planIdentity?.Trim() ?? string.Empty;
            m_LeaderArtifactHash = leaderArtifactHash?.Trim() ?? string.Empty;
            m_FollowerArtifactHash = followerArtifactHash?.Trim() ?? string.Empty;
            m_LeaderSourceIdentity = leaderSourceIdentity?.Trim() ?? string.Empty;
            m_FollowerSourceIdentity = followerSourceIdentity?.Trim() ?? string.Empty;
            m_Segments = segments == null
                ? throw new ArgumentNullException(nameof(segments))
                : (AnimationFootPhaseWarpSegmentPlan[])segments.Clone();
            RequireValid();
        }

        public string PlanIdentity => m_PlanIdentity ?? string.Empty;
        public string Algorithm => m_AlgorithmIdentity ?? string.Empty;
        public string LeaderArtifactHash => m_LeaderArtifactHash ?? string.Empty;
        public string FollowerArtifactHash => m_FollowerArtifactHash ?? string.Empty;
        public string LeaderSourceIdentity => m_LeaderSourceIdentity ?? string.Empty;
        public string FollowerSourceIdentity => m_FollowerSourceIdentity ?? string.Empty;
        public IReadOnlyList<AnimationFootPhaseWarpSegmentPlan> Segments =>
            m_Segments ?? Array.Empty<AnimationFootPhaseWarpSegmentPlan>();

        public AnimationFootPhaseWarpSegmentPlan RequireSegment(
            int leaderOccurrenceIndex,
            int followerOccurrenceIndex,
            string previousMarkerId,
            string nextMarkerId)
        {
            RequireValid();
            for (int i = 0; i < m_Segments.Length; i++)
            {
                AnimationFootPhaseWarpSegmentPlan segment = m_Segments[i];
                if (segment.LeaderOccurrenceIndex == leaderOccurrenceIndex &&
                    segment.FollowerOccurrenceIndex == followerOccurrenceIndex &&
                    string.Equals(segment.PreviousMarkerId, previousMarkerId, StringComparison.Ordinal) &&
                    string.Equals(segment.NextMarkerId, nextMarkerId, StringComparison.Ordinal))
                    return segment;
            }
            throw new InvalidOperationException(
                $"Foot phase warp plan '{PlanIdentity}' has no occurrence {leaderOccurrenceIndex}->{followerOccurrenceIndex} for '{previousMarkerId}->{nextMarkerId}'.");
        }

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(PlanIdentity) ||
                !string.Equals(Algorithm, AlgorithmIdentity, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(LeaderArtifactHash) ||
                string.IsNullOrWhiteSpace(FollowerArtifactHash) ||
                string.IsNullOrWhiteSpace(LeaderSourceIdentity) ||
                string.IsNullOrWhiteSpace(FollowerSourceIdentity) ||
                m_Segments == null || m_Segments.Length == 0)
                throw new InvalidOperationException("Foot phase time warp plan is invalid.");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Segments.Length; i++)
            {
                AnimationFootPhaseWarpSegmentPlan segment = m_Segments[i] ??
                    throw new InvalidOperationException("Foot phase warp segment is missing.");
                segment.RequireValid();
                string key = string.Concat(
                    segment.LeaderOccurrenceIndex,
                    ":",
                    segment.FollowerOccurrenceIndex,
                    ":",
                    segment.PreviousMarkerId,
                    ":",
                    segment.NextMarkerId);
                if (!identities.Add(key))
                    throw new InvalidOperationException("Foot phase warp segment identity is duplicated.");
            }
        }
    }

    [Serializable]
    public sealed class ActionAnimationFootPhaseTimeWarpPlan
    {
        [SerializeField] string m_LeaderProgramProducerId = string.Empty;
        [SerializeField] string m_FollowerProgramProducerId = string.Empty;
        [SerializeField] AnimationFootPhaseTimeWarpPlan m_TimeWarp;

        public ActionAnimationFootPhaseTimeWarpPlan(
            string leaderProgramProducerId,
            string followerProgramProducerId,
            AnimationFootPhaseTimeWarpPlan timeWarp)
        {
            m_LeaderProgramProducerId = leaderProgramProducerId?.Trim() ?? string.Empty;
            m_FollowerProgramProducerId = followerProgramProducerId?.Trim() ?? string.Empty;
            m_TimeWarp = timeWarp ?? throw new ArgumentNullException(nameof(timeWarp));
            RequireValid();
        }

        public string LeaderProgramProducerId => m_LeaderProgramProducerId ?? string.Empty;
        public string FollowerProgramProducerId => m_FollowerProgramProducerId ?? string.Empty;
        public AnimationFootPhaseTimeWarpPlan TimeWarp => m_TimeWarp;

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(LeaderProgramProducerId) ||
                string.IsNullOrWhiteSpace(FollowerProgramProducerId) ||
                string.Equals(LeaderProgramProducerId, FollowerProgramProducerId, StringComparison.Ordinal) ||
                TimeWarp == null)
                throw new InvalidOperationException("Action Foot Phase Time Warp plan is invalid.");
            TimeWarp.RequireValid();
        }
    }
}
