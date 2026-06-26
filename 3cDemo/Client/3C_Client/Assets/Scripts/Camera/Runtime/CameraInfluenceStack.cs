using System.Collections.Generic;

namespace ThirdPersonCamera
{
    public sealed class CameraInfluenceStack
    {
        readonly List<ICameraInfluenceSource> sources = new List<ICameraInfluenceSource>();
        readonly List<CameraInfluenceRequest> resolveBuffer = new List<CameraInfluenceRequest>();

        public int Count => sources.Count;

        public CameraInfluenceHandle CreateHandle(CameraInfluenceRequest initialRequest)
        {
            CameraInfluenceHandle handle = new CameraInfluenceHandle(this, initialRequest);
            Register(handle);
            return handle;
        }

        public void Register(ICameraInfluenceSource source)
        {
            if (source != null && !sources.Contains(source))
                sources.Add(source);
        }

        public void Unregister(ICameraInfluenceSource source)
        {
            if (source != null)
                sources.Remove(source);
        }

        public CameraInfluenceRequest Resolve(CameraInfluenceRequest baseRequest)
        {
            resolveBuffer.Clear();
            for (int i = 0; i < sources.Count; i++)
                resolveBuffer.Add(sources[i].CurrentRequest);

            return CameraInfluenceResolver.Resolve(baseRequest, resolveBuffer);
        }
    }
}
