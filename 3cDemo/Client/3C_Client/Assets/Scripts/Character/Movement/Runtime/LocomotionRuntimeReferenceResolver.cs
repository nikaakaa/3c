using ThirdPersonAnimation;
using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal static class LocomotionRuntimeReferenceResolver
    {
        public static bool TryResolveComponentInterface<T>(
            Component owner,
            out T service,
            out MonoBehaviour serviceBehaviour)
            where T : class
        {
            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
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

        public static IFacingDirectionProvider ResolveFacingProvider(
            Component owner,
            MonoBehaviour serializedBehaviour,
            out MonoBehaviour resolvedBehaviour)
        {
            resolvedBehaviour = serializedBehaviour;
            if (resolvedBehaviour == null)
                resolvedBehaviour = owner.GetComponent<TransformFacingDirectionProvider>();
            if (resolvedBehaviour == null && TryResolveComponentInterface(owner, out IFacingDirectionProvider _, out MonoBehaviour providerBehaviour))
                resolvedBehaviour = providerBehaviour;
            return resolvedBehaviour as IFacingDirectionProvider;
        }

        public static ILocomotionAnimationPresenter ResolveLocomotionPresenter(
            Component owner,
            out MonoBehaviour presenterBehaviour,
            out ILocomotionAnimationPlaybackProgressController playbackProgressController)
        {
            if (TryResolveComponentInterface(owner, out ILocomotionAnimationPresenter directPresenter, out presenterBehaviour))
            {
                playbackProgressController = directPresenter;
                return directPresenter;
            }

            MonoBehaviour[] behaviours = owner.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILocomotionAnimationPresenter presenter)
                {
                    presenterBehaviour = behaviours[i];
                    playbackProgressController = presenter;
                    return presenter;
                }
            }

            presenterBehaviour = null;
            playbackProgressController = null;
            return null;
        }

        public static ThirdPersonCameraController ResolveCameraController(Component owner)
        {
            ThirdPersonCameraController controller = owner.GetComponent<ThirdPersonCameraController>();
            if (controller != null)
                return controller;

            controller = owner.GetComponentInParent<ThirdPersonCameraController>(true);
            if (controller != null)
                return controller;

            return owner.GetComponentInChildren<ThirdPersonCameraController>(true);
        }
    }
}
