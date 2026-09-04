namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeState
    {
        public CameraEffectRuntimeState(CameraEffectRequest request)
        {
            Request = request;
        }

        public CameraEffectRequest Request { get; set; }
        public float Elapsed;
        public bool Retired;
        public float RetireElapsed;
    }
}
