namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeState
    {
        public CameraEffectRuntimeState(CameraEffectRequest request, string tag)
        {
            Request = request;
            Tag = tag ?? string.Empty;
        }

        public CameraEffectRequest Request { get; set; }
        public string Tag { get; set; }
        public float Elapsed;
        public bool Retired;
        public float RetireElapsed;
        public float RetireStartElapsed;
        public CameraPresentationStopReason RetireReason;
    }
}
