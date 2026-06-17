using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public readonly struct MovementAnimationContext
    {
        public MovementAnimationContext(
            BasicMovementPhase phase,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed)
            : this(phase, BasicMovementGait.Walk, hasMoveIntent, inputStrength, worldDirection, planarSpeed)
        {
        }

        public MovementAnimationContext(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed)
            : this(
                phase,
                gait,
                hasMoveIntent,
                inputStrength,
                worldDirection,
                planarSpeed,
                default,
                false)
        {
        }

        public MovementAnimationContext(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed,
            TurnBackMotionPolicy turnBackMotionPolicy,
            bool hasTurnBackMotionPolicy)
            : this(
                phase,
                gait,
                hasMoveIntent,
                inputStrength,
                worldDirection,
                planarSpeed,
                turnBackMotionPolicy,
                hasTurnBackMotionPolicy,
                LocomotionFootPhaseMatchResult.NotRequested,
                false)
        {
        }

        public MovementAnimationContext(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed,
            TurnBackMotionPolicy turnBackMotionPolicy,
            bool hasTurnBackMotionPolicy,
            LocomotionFootPhaseMatchResult entryFootPhaseMatchResult,
            bool hasEntryFootPhaseMatchRequest)
            : this(
                phase,
                gait,
                hasMoveIntent,
                inputStrength,
                worldDirection,
                planarSpeed,
                turnBackMotionPolicy,
                hasTurnBackMotionPolicy,
                entryFootPhaseMatchResult,
                hasEntryFootPhaseMatchRequest,
                null)
        {
        }

        public MovementAnimationContext(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed,
            TurnBackMotionPolicy turnBackMotionPolicy,
            bool hasTurnBackMotionPolicy,
            LocomotionFootPhaseMatchResult entryFootPhaseMatchResult,
            bool hasEntryFootPhaseMatchRequest,
            RunLocomotionAnimationConfigSO animationConfig)
        {
            Phase = phase;
            Gait = gait;
            HasMoveIntent = hasMoveIntent;
            InputStrength = Mathf.Clamp01(inputStrength);
            WorldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            PlanarSpeed = Mathf.Max(0f, planarSpeed);
            TurnBackMotionPolicy = turnBackMotionPolicy;
            HasTurnBackMotionPolicy = hasTurnBackMotionPolicy && turnBackMotionPolicy.IsEnabled;
            HasEntryFootPhaseMatchRequest = hasEntryFootPhaseMatchRequest;
            EntryFootPhaseMatchResult = hasEntryFootPhaseMatchRequest
                ? entryFootPhaseMatchResult
                : LocomotionFootPhaseMatchResult.NotRequested;
            AnimationConfig = animationConfig;
        }

        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public bool HasMoveIntent { get; }
        public float InputStrength { get; }
        public Vector3 WorldDirection { get; }
        public float PlanarSpeed { get; }
        public TurnBackMotionPolicy TurnBackMotionPolicy { get; }
        public bool HasTurnBackMotionPolicy { get; }
        public LocomotionFootPhaseMatchResult EntryFootPhaseMatchResult { get; }
        public RunLocomotionAnimationConfigSO AnimationConfig { get; }
        public bool HasEntryFootPhaseMatchRequest { get; }
        public bool HasEntryStartNormalizedTimeOverride => HasEntryFootPhaseMatchRequest && EntryFootPhaseMatchResult.IsValid;
    }
}
