using UnityEngine;

namespace ThirdPersonMovement
{
    public sealed class CharacterControllerBasicMotionExecutor : IBasicLocomotionMotionExecutor
    {
        readonly CharacterController characterController;
        readonly Transform rotationRoot;
        readonly bool applyGravity;
        readonly float gravity;
        readonly float groundedVerticalVelocity;
        float verticalVelocity;

        public CharacterControllerBasicMotionExecutor(
            CharacterController characterController,
            Transform rotationRoot,
            bool applyGravity,
            float gravity,
            float groundedVerticalVelocity)
        {
            this.characterController = characterController;
            this.rotationRoot = rotationRoot;
            this.applyGravity = applyGravity;
            this.gravity = gravity;
            this.groundedVerticalVelocity = groundedVerticalVelocity;
        }

        public Vector3 LastWorldDirection { get; private set; }
        public float CurrentSpeed { get; private set; }

        public void ExecuteBasicMovement(in MovementCommand command)
        {
            if (characterController == null)
                return;

            float deltaTime = command.DeltaTime;
            if (deltaTime <= 0f)
                return;

            Vector3 worldDirection = command.WorldDirection;
            worldDirection.y = 0f;
            worldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            Vector3 animationWorldDelta = ResolveAnimationWorldDelta(in command);
            LastWorldDirection = ResolveLastWorldDirection(worldDirection, animationWorldDelta);

            if (command.HasMovement && command.RotationSpeed > 0f)
                RotateTowards(command.DesiredFacing, command.RotationSpeed, deltaTime);

            if (command.HasAnimationMotion && Mathf.Abs(command.AnimationYawDelta) > 0.0001f)
                RotateByAnimationYaw(command.AnimationYawDelta);

            Vector3 planarVelocity = command.HasMovement ? worldDirection * command.PlanarSpeed : Vector3.zero;
            Move(planarVelocity, animationWorldDelta, deltaTime);
        }

        public void ExecuteActionMovement(in ActionMovementCommand command)
        {
            if (characterController == null)
                return;

            float deltaTime = command.DeltaTime;
            if (deltaTime <= 0f || !command.HasMovement)
                return;

            Vector3 worldDirection = command.WorldDirection;
            worldDirection.y = 0f;
            worldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            LastWorldDirection = worldDirection;

            if (command.RotateToDirection)
                RotateImmediate(worldDirection);

            Vector3 planarVelocity = worldDirection * (command.PlanarDistance / deltaTime);
            Move(planarVelocity, Vector3.zero, deltaTime);
        }

        void RotateTowards(Vector3 worldDirection, float rotationSpeed, float deltaTime)
        {
            if (rotationRoot == null || worldDirection.sqrMagnitude <= 0.000001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            rotationRoot.rotation = Quaternion.RotateTowards(rotationRoot.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        void RotateByAnimationYaw(float yawDelta)
        {
            Transform root = ResolveMotionRoot();
            if (root == null)
                return;

            root.rotation = root.rotation * Quaternion.Euler(0f, yawDelta, 0f);
        }

        void RotateImmediate(Vector3 worldDirection)
        {
            if (rotationRoot == null || worldDirection.sqrMagnitude <= 0.000001f)
                return;

            rotationRoot.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        }

        Vector3 ResolveAnimationWorldDelta(in MovementCommand command)
        {
            if (!command.HasAnimationMotion)
                return Vector3.zero;

            Vector3 localDelta = command.AnimationLocalPlanarDelta;
            localDelta.y = 0f;
            if (localDelta.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            Transform root = ResolveMotionRoot();
            Vector3 worldDelta = root != null ? root.TransformDirection(localDelta) : localDelta;
            worldDelta.y = 0f;
            return worldDelta;
        }

        Vector3 ResolveLastWorldDirection(Vector3 inputWorldDirection, Vector3 animationWorldDelta)
        {
            if (inputWorldDirection.sqrMagnitude > 0.000001f)
                return inputWorldDirection;

            animationWorldDelta.y = 0f;
            return animationWorldDelta.sqrMagnitude > 0.000001f ? animationWorldDelta.normalized : Vector3.zero;
        }

        Transform ResolveMotionRoot()
        {
            if (rotationRoot != null)
                return rotationRoot;

            return characterController != null ? characterController.transform : null;
        }

        void Move(Vector3 planarVelocity, Vector3 animationWorldDelta, float deltaTime)
        {
            if (applyGravity)
                UpdateVerticalVelocity(deltaTime);
            else
                verticalVelocity = 0f;

            Vector3 planarDisplacement = planarVelocity * deltaTime + animationWorldDelta;
            Vector3 displacement = planarDisplacement + Vector3.up * verticalVelocity * deltaTime;
            characterController.Move(displacement);
            CurrentSpeed = deltaTime > 0f ? new Vector3(planarDisplacement.x, 0f, planarDisplacement.z).magnitude / deltaTime : 0f;
        }

        void UpdateVerticalVelocity(float deltaTime)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedVerticalVelocity;
            else
                verticalVelocity += gravity * deltaTime;
        }
    }
}
