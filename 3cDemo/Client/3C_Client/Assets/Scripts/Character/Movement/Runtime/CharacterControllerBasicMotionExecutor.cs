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
            LastWorldDirection = worldDirection;

            if (command.HasMovement && command.RotationSpeed > 0f)
                RotateTowards(command.DesiredFacing, command.RotationSpeed, deltaTime);

            Vector3 planarVelocity = command.HasMovement ? worldDirection * command.PlanarSpeed : Vector3.zero;
            Move(planarVelocity, deltaTime);
        }

        void RotateTowards(Vector3 worldDirection, float rotationSpeed, float deltaTime)
        {
            if (rotationRoot == null || worldDirection.sqrMagnitude <= 0.000001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            rotationRoot.rotation = Quaternion.RotateTowards(rotationRoot.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        void Move(Vector3 planarVelocity, float deltaTime)
        {
            if (applyGravity)
                UpdateVerticalVelocity(deltaTime);
            else
                verticalVelocity = 0f;

            Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * deltaTime);
            CurrentSpeed = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
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
