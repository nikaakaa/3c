using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public static class CameraInfluenceResolver
    {
        public static CameraInfluenceRequest Resolve(CameraInfluenceRequest fallback, params CameraInfluenceRequest[] requests)
        {
            return ResolveRequests(fallback, requests);
        }

        public static CameraInfluenceRequest Resolve(CameraInfluenceRequest fallback, IReadOnlyList<CameraInfluenceRequest> requests)
        {
            return ResolveRequests(fallback, requests);
        }

        static CameraInfluenceRequest ResolveRequests(CameraInfluenceRequest fallback, IReadOnlyList<CameraInfluenceRequest> requests)
        {
            CameraInfluenceRequest selected = fallback;
            if (requests == null)
                return selected;

            for (int i = 0; i < requests.Count; i++)
            {
                CameraInfluenceRequest candidate = requests[i];
                if (!candidate.Active || candidate.Weight <= 0f)
                    continue;

                if (!selected.Active || candidate.Priority >= selected.Priority)
                    selected = candidate;
            }

            return selected;
        }
    }
}
