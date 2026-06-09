using Animancer;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [CreateAssetMenu(fileName = "LocomotionAnimationSet", menuName = "3C/Animation/LocomotionAnimationSet")]
    public sealed class LocomotionAnimationSetSO : ScriptableObject
    {
        [SerializeField] string idleKey = "Idle";
        [SerializeField] string walkStartKey = "WalkStart";
        [SerializeField] string runStartKey = "RunStart";
        [SerializeField] string walkLoopKey = "WalkLoop";
        [SerializeField] string runLoopKey = "RunLoop";
        [SerializeField] string walkEndKey = "WalkEnd";
        [SerializeField] string runEndKey = "RunEnd";
        [SerializeField] LocomotionAnimationEntry idle;
        [SerializeField] LocomotionAnimationEntry walkStart;
        [SerializeField] LocomotionAnimationEntry runStart;
        [SerializeField] LocomotionAnimationEntry walkLoop;
        [SerializeField] LocomotionAnimationEntry runLoop;
        [SerializeField] LocomotionAnimationEntry walkEnd;
        [SerializeField] LocomotionAnimationEntry runEnd;

        public string IdleKey => idleKey;
        public string WalkStartKey => walkStartKey;
        public string RunStartKey => runStartKey;
        public string WalkLoopKey => walkLoopKey;
        public string RunLoopKey => runLoopKey;
        public string WalkEndKey => walkEndKey;
        public string RunEndKey => runEndKey;

        public StringReference Resolve(BasicMovementPhase phase, LocomotionAnimationGait gait, LocomotionAnimationGait lastMovingGait)
        {
            return ResolveEntry(phase, gait, lastMovingGait).KeyReference;
        }

        public LocomotionAnimationEntry ResolveEntry(BasicMovementPhase phase, LocomotionAnimationGait gait, LocomotionAnimationGait lastMovingGait)
        {
            LocomotionAnimationEntry entry = phase switch
            {
                BasicMovementPhase.MoveStart => gait == LocomotionAnimationGait.Run ? EntryOrFallback(runStart, runStartKey) : EntryOrFallback(walkStart, walkStartKey),
                BasicMovementPhase.MoveLoop => gait == LocomotionAnimationGait.Run ? EntryOrFallback(runLoop, runLoopKey) : EntryOrFallback(walkLoop, walkLoopKey),
                BasicMovementPhase.MoveStop => lastMovingGait == LocomotionAnimationGait.Run ? EntryOrFallback(runEnd, runEndKey) : EntryOrFallback(walkEnd, walkEndKey),
                _ => EntryOrFallback(idle, idleKey)
            };

            return entry;
        }

        public LocomotionAnimationTiming ResolveTiming(
            BasicMovementPhase phase,
            LocomotionAnimationGait gait,
            LocomotionAnimationGait lastMovingGait,
            float fallbackExitDuration)
        {
            return ResolveEntry(phase, gait, lastMovingGait).ResolveTiming(fallbackExitDuration);
        }

        public LocomotionAnimationSetValidationResult Validate()
        {
            LocomotionAnimationSetValidationResult result = new LocomotionAnimationSetValidationResult();
            ValidateEntry(EntryOrFallback(idle, idleKey), "Idle", false, result);
            ValidateEntry(EntryOrFallback(walkStart, walkStartKey), "MoveStart + Walk", false, result);
            ValidateEntry(EntryOrFallback(runStart, runStartKey), "MoveStart + Run", false, result);
            ValidateEntry(EntryOrFallback(walkLoop, walkLoopKey), "MoveLoop + Walk", false, result);
            ValidateEntry(EntryOrFallback(runLoop, runLoopKey), "MoveLoop + Run", false, result);
            ValidateEntry(EntryOrFallback(walkEnd, walkEndKey), "MoveStop + Walk", true, result);
            ValidateEntry(EntryOrFallback(runEnd, runEndKey), "MoveStop + Run", true, result);
            return result;
        }

        public void ResetToDefaultSet()
        {
            idleKey = "Idle";
            walkStartKey = "WalkStart";
            runStartKey = "RunStart";
            walkLoopKey = "WalkLoop";
            runLoopKey = "RunLoop";
            walkEndKey = "WalkEnd";
            runEndKey = "RunEnd";
            idle = default;
            walkStart = default;
            runStart = default;
            walkLoop = default;
            runLoop = default;
            walkEnd = default;
            runEnd = default;
        }

        static LocomotionAnimationEntry EntryOrFallback(LocomotionAnimationEntry entry, string fallbackKey)
        {
            return string.IsNullOrWhiteSpace(entry.Key)
                ? new LocomotionAnimationEntry(fallbackKey)
                : entry;
        }

        static void ValidateEntry(LocomotionAnimationEntry entry, string mapping, bool isStop, LocomotionAnimationSetValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
                result.AddError($"Animation mapping '{mapping}' is missing.");

            if (!entry.HasValidSpeed)
                result.AddError($"Animation mapping '{mapping}' has invalid speed.");

            if (isStop && entry.ExitDurationMode == LocomotionAnimationExitDurationMode.Manual && entry.ExitDurationOverride < 0f)
                result.AddError($"Animation mapping '{mapping}' is missing MoveStop exit duration.");
        }
    }
}
