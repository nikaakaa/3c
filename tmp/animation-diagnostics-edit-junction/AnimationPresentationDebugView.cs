using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct PoseStateSourceSyncSnapshot
    {
        internal PoseStateSourceSyncSnapshot(
            string relationId,
            bool initialized,
            long leaderOrdinal,
            long followerOrdinal,
            AnimationSyncTimeMapping timeMapping,
            string planIdentity,
            float leaderFraction,
            float followerFraction,
            int leaderOccurrenceIndex,
            int followerOccurrenceIndex,
            double followerEffectiveTime)
        {
            RelationId = relationId?.Trim() ??
                string.Empty;
            Initialized = initialized;
            LeaderOrdinal = leaderOrdinal;
            FollowerOrdinal = followerOrdinal;
            TimeMapping = timeMapping;
            PlanIdentity = planIdentity?.Trim() ?? string.Empty;
            LeaderFraction = leaderFraction;
            FollowerFraction = followerFraction;
            LeaderOccurrenceIndex = leaderOccurrenceIndex;
            FollowerOccurrenceIndex = followerOccurrenceIndex;
            FollowerEffectiveTime = followerEffectiveTime;
            if (string.IsNullOrWhiteSpace(RelationId) ||
                Initialized &&
                (LeaderOrdinal < 0 ||
                 FollowerOrdinal < 0 ||
                 TimeMapping is not (
                     AnimationSyncTimeMapping.MarkerSegmentFraction or
                     AnimationSyncTimeMapping.GeneratedFootPhase) ||
                 (TimeMapping == AnimationSyncTimeMapping.GeneratedFootPhase) !=
                     !string.IsNullOrWhiteSpace(PlanIdentity) ||
                 !float.IsFinite(LeaderFraction) ||
                 LeaderFraction < 0f ||
                 LeaderFraction > 1f ||
                 !float.IsFinite(FollowerFraction) ||
                 FollowerFraction < 0f ||
                 FollowerFraction > 1f ||
                 LeaderOccurrenceIndex < 0 ||
                 FollowerOccurrenceIndex < 0 ||
                 !double.IsFinite(FollowerEffectiveTime) ||
                 FollowerEffectiveTime < 0d))
            {
                throw new ArgumentException(
                    "Pose State Source Sync snapshot is invalid.");
            }
        }

        public string RelationId { get; }
        public bool Initialized { get; }
        public long LeaderOrdinal { get; }
        public long FollowerOrdinal { get; }
        public AnimationSyncTimeMapping TimeMapping { get; }
        public string PlanIdentity { get; }
        public float LeaderFraction { get; }
        public float FollowerFraction { get; }
        public int LeaderOccurrenceIndex { get; }
        public int FollowerOccurrenceIndex { get; }
        public double FollowerEffectiveTime { get; }
    }

    public sealed class AnimationPresentationDebugView
    {
        readonly ActionAnimationPlaybackLifecycleSnapshot[]
            m_ActionPlaybacks;
        readonly ActionMarkerPlaybackSnapshot[]
            m_ActionMarkerPlaybacks;
        readonly ActionMarkerRelationSnapshot[]
            m_ActionMarkerRelations;
        readonly ActionPresentationTimeSnapshot[]
            m_ActionTimes;
        readonly PoseStateSourceSyncSnapshot[]
            m_PoseStateSourceSyncRelations;

        internal AnimationPresentationDebugView(
            in AnimationPresentationRuntimeSnapshot posePlan,
            IReadOnlyList<
                ActionAnimationPlaybackLifecycleSnapshot>
                actionPlaybacks,
            IReadOnlyList<ActionMarkerPlaybackSnapshot>
                actionMarkerPlaybacks,
            IReadOnlyList<ActionMarkerRelationSnapshot>
                actionMarkerRelations,
            IReadOnlyList<ActionPresentationTimeSnapshot>
                actionTimes,
            IReadOnlyList<PoseStateSourceSyncSnapshot>
                poseStateSourceSyncRelations)
        {
            if (posePlan.CompletionIdentity == 0)
            {
                throw new ArgumentException(
                    "Animation Presentation Debug View has no committed Pose Plan.",
                    nameof(posePlan));
            }
            PosePlan = posePlan;
            m_ActionPlaybacks = Copy(actionPlaybacks);
            m_ActionMarkerPlaybacks =
                Copy(actionMarkerPlaybacks);
            m_ActionMarkerRelations =
                Copy(actionMarkerRelations);
            m_ActionTimes =
                Copy(actionTimes);
            m_PoseStateSourceSyncRelations =
                Copy(poseStateSourceSyncRelations);
        }

        public ulong CompletionIdentity =>
            PosePlan.CompletionIdentity;
        public AnimationPresentationRuntimeSnapshot
            PosePlan { get; }
        public IReadOnlyList<
            ActionAnimationPlaybackLifecycleSnapshot>
            ActionPlaybacks => m_ActionPlaybacks;
        public IReadOnlyList<ActionMarkerPlaybackSnapshot>
            ActionMarkerPlaybacks =>
                m_ActionMarkerPlaybacks;
        public IReadOnlyList<ActionMarkerRelationSnapshot>
            ActionMarkerRelations =>
                m_ActionMarkerRelations;
        public IReadOnlyList<ActionPresentationTimeSnapshot>
            ActionTimes =>
                m_ActionTimes;
        public IReadOnlyList<PoseStateSourceSyncSnapshot>
            PoseStateSourceSyncRelations =>
                m_PoseStateSourceSyncRelations;

        static T[] Copy<T>(
            IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<T>();
            var result = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }
    }
}
