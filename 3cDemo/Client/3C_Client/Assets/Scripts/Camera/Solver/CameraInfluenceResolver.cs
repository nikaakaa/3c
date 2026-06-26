using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public static class CameraInfluenceResolver
    {
        public static CameraInfluenceRequest Resolve(CameraInfluenceRequest baseRequest, params CameraInfluenceRequest[] requests)
        {
            return ResolveRequests(baseRequest, requests);
        }

        public static CameraInfluenceRequest Resolve(CameraInfluenceRequest baseRequest, IReadOnlyList<CameraInfluenceRequest> requests)
        {
            return ResolveRequests(baseRequest, requests);
        }

        static CameraInfluenceRequest ResolveRequests(CameraInfluenceRequest baseRequest, IReadOnlyList<CameraInfluenceRequest> requests)
        {
            CameraInfluenceRequest selected = baseRequest;
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
