using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public interface IMotionMatchingPoseHistoryReadView
    {
        int Count { get; }
        bool HasGap { get; }
        ulong ResetSequence { get; }
        AnimationFootPlacementHistorySample LatestFootPlacement { get; }
        bool CoversSecondsBeforeLatest(float secondsBeforeLatest);
        bool TrySampleBone(
            float secondsBeforeLatest,
            int boneIndex,
            out Vector3 position,
            out Vector3 velocity);
    }
}
