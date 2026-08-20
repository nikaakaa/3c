using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct PoseStateSourceSyncSnapshot
    {
        internal PoseStateSourceSyncSnapshot(
            string relationId,
            ulong generation,
            double followerEffectiveTime)
        {
            RelationId = relationId?.Trim() ??
                string.Empty;
            Generation = generation;
            FollowerEffectiveTime = followerEffectiveTime;
            if (string.IsNullOrWhiteSpace(RelationId) ||
                Generation == 0 ||
                !double.IsFinite(FollowerEffectiveTime) ||
                FollowerEffectiveTime < 0d)
            {
                throw new ArgumentException(
                    "Pose State Source Sync snapshot is invalid.");
            }
        }

        public string RelationId { get; }
        public ulong Generation { get; }
        public double FollowerEffectiveTime { get; }
    }

    public sealed class AnimationPresentationDebugView
    {
        readonly ActionAnimationPlaybackLifecycleSnapshot[]
            m_ActionPlaybacks;
        readonly ActionPresentationTimeSnapshot[]
            m_ActionTimes;
        readonly PoseStateSourceSyncSnapshot[]
            m_PoseStateSourceSyncRelations;

        internal AnimationPresentationDebugView(
            in AnimationPresentationRuntimeSnapshot posePlan,
            IReadOnlyList<
                ActionAnimationPlaybackLifecycleSnapshot>
                actionPlaybacks,
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
