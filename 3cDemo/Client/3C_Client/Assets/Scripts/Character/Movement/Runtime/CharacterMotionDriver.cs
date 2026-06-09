using UnityEngine;

namespace ThirdPersonMovement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotionDriver : MonoBehaviour, IBasicLocomotionMotionExecutor
    {
        [SerializeField] Transform rotationRoot;
        [SerializeField] bool applyGravity = true;
        [SerializeField] float gravity = -20f;
        [SerializeField] float groundedVerticalVelocity = -2f;

        CharacterController characterController;
        CharacterControllerBasicMotionExecutor executor;

        public Vector3 LastWorldDirection => executor != null ? executor.LastWorldDirection : Vector3.zero;
        public float CurrentSpeed => executor != null ? executor.CurrentSpeed : 0f;

        void Awake()
        {
            EnsureReady();
        }

        public void ExecuteBasicMovement(in MovementCommand command)
        {
            EnsureReady();
            executor.ExecuteBasicMovement(in command);
        }

        void OnValidate()
        {
            executor = null;
        }

        void EnsureReady()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (rotationRoot == null)
                rotationRoot = transform;
            if (executor == null)
                executor = new CharacterControllerBasicMotionExecutor(characterController, rotationRoot, applyGravity, gravity, groundedVerticalVelocity);
        }
    }
}
