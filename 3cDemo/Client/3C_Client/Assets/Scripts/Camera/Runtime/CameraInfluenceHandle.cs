using System;

namespace ThirdPersonCamera
{
    public sealed class CameraInfluenceHandle : ICameraInfluenceSource, IDisposable
    {
        readonly CameraInfluenceStack owner;
        bool disposed;

        internal CameraInfluenceHandle(CameraInfluenceStack owner, CameraInfluenceRequest initialRequest)
        {
            this.owner = owner;
            CurrentRequest = initialRequest;
        }

        public CameraInfluenceRequest CurrentRequest { get; private set; }

        public void Set(CameraInfluenceRequest request)
        {
            if (!disposed)
                CurrentRequest = request;
        }

        public void Clear()
        {
            Set(default);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            owner.Unregister(this);
        }
    }
}
