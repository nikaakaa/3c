using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct MovementCommand
    {
        public MovementCommand(Vector3 worldDirection, float planarSpeed, float rotationSpeed, float deltaTime, BasicMovementPhase phase)
            : this(worldDirection, planarSpeed, rotationSpeed, deltaTime, phase, BasicMovementGait.Walk, BasicMovementMotionFacts.None(phase))
        {
        }

        public MovementCommand(
            Vector3 worldDirection,
            float planarSpeed,
            float rotationSpeed,
            float deltaTime,
            BasicMovementPhase phase,
            BasicMovementMotionFacts motionFacts)
            : this(worldDirection, planarSpeed, rotationSpeed, deltaTime, phase, BasicMovementGait.Walk, motionFacts)
        {
        }

        public MovementCommand(
            Vector3 worldDirection,
            float planarSpeed,
            float rotationSpeed,
            float deltaTime,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            BasicMovementMotionFacts motionFacts)
        {
            WorldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            DesiredFacing = WorldDirection;
            PlanarSpeed = Mathf.Max(0f, planarSpeed);
            RotationSpeed = Mathf.Max(0f, rotationSpeed);
            DeltaTime = Mathf.Max(0f, deltaTime);
            Phase = phase;
            Gait = gait;
            HasAnimationMotion = motionFacts.HasAnimationMotion;
            AnimationLocalPlanarDelta = motionFacts.HasAnimationMotion
                ? new Vector3(motionFacts.LocalPlanarDelta.x, 0f, motionFacts.LocalPlanarDelta.z)
                : Vector3.zero;
            AnimationYawDelta = motionFacts.HasAnimationMotion ? motionFacts.YawDelta : 0f;
            AnimationMotionSourcePhase = motionFacts.SourcePhase;
            AnimationMotionSourceAliasKey = motionFacts.SourceAliasKey;
        }

        public Vector3 WorldDirection { get; }
        public Vector3 DesiredFacing { get; }
        public float PlanarSpeed { get; }
        public float RotationSpeed { get; }
        public float DeltaTime { get; }
        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public bool HasAnimationMotion { get; }
        public Vector3 AnimationLocalPlanarDelta { get; }
        public float AnimationYawDelta { get; }
        public BasicMovementPhase AnimationMotionSourcePhase { get; }
        public string AnimationMotionSourceAliasKey { get; }
        public bool HasMovement => WorldDirection.sqrMagnitude > 0.000001f && PlanarSpeed > 0f;
    }
}
