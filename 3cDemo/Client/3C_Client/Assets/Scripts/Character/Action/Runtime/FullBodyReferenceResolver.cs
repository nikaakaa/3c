using UnityEngine;

namespace ThirdPersonAction
{
    internal static class FullBodyReferenceResolver
    {
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
