using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    internal static class FullBodyReferenceResolver
    {
        public static InputRequestBufferComponent ResolveInputBuffer(Component owner, InputRequestBufferComponent current)
        {
            if (current != null)
                return current;

            current = owner.GetComponent<InputRequestBufferComponent>();
            return current != null ? current : owner.GetComponentInParent<InputRequestBufferComponent>();
        }

        public static PlayerLocomotionController ResolveLocomotionController(Component owner, PlayerLocomotionController current)
        {
            if (current != null)
                return current;

            current = owner.GetComponent<PlayerLocomotionController>();
            return current != null ? current : owner.GetComponentInParent<PlayerLocomotionController>();
        }

        public static MonoBehaviour ResolveFacingProviderBehaviour(Component owner, MonoBehaviour current)
        {
            return current != null ? current : owner.GetComponent<TransformFacingDirectionProvider>();
        }

        public static bool TryResolveLocomotionActionExecutor(
            PlayerLocomotionController locomotionController,
            out IActionMovementExecutor executor,
            out MonoBehaviour executorBehaviour)
        {
            executor = null;
            executorBehaviour = null;

            if (locomotionController == null || locomotionController.MotionExecutorBehaviour == null)
                return false;

            executor = locomotionController.MotionExecutorBehaviour as IActionMovementExecutor;
            if (executor == null)
                return false;

            executorBehaviour = locomotionController.MotionExecutorBehaviour;
            return true;
        }

        public static bool TryResolveComponentInterface<T>(
            Component owner,
            out T service,
            out MonoBehaviour serviceBehaviour)
            where T : class
        {
            MonoBehaviour[] behaviours = owner.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T candidate)
                {
                    service = candidate;
                    serviceBehaviour = behaviours[i];
                    return true;
                }
            }

            service = null;
            serviceBehaviour = null;
            return false;
        }
    }
}
