namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeState
    {
        public CameraEffectRuntimeState(CameraEffectRequest request, string tag)
        {
            Reset(request, tag);
        }

        internal void Reset(CameraEffectRequest request, string tag)
        {
            Request = request;
            Tag = tag ?? string.Empty;
            Elapsed = 0f;
            Retired = false;
            RetireElapsed = 0f;
            RetireStartElapsed = 0f;
            RetireReason = default;
            ShakeSampleCount = 0;
            ShakeSourceForward = default;
            ShakeDistance = 0f;
            ShakeEnvelope = 1f;
            ZoomInitialized = false;
            ZoomStartOffset = 0f;
            ZoomTargetOffset = 0f;
            StretchInitialized = false;
            StretchStartRadiusOffset = 0f;
            StretchStartPositionOffset = default;
            StretchStartRollOffset = 0f;
        }

        public CameraEffectRequest Request { get; set; }
        public string Tag { get; set; }
        public float Elapsed;
        public bool Retired;
        public float RetireElapsed;
        public float RetireStartElapsed;
        public CameraPresentationStopReason RetireReason;
        public int ShakeSampleCount;
        public UnityEngine.Vector3 ShakeSourceForward;
        public float ShakeDistance;
        public float ShakeEnvelope;
        public bool ZoomInitialized;
        public float ZoomStartOffset;
        public float ZoomTargetOffset;
        public bool StretchInitialized;
        public float StretchStartRadiusOffset;
        public UnityEngine.Vector3 StretchStartPositionOffset;
        public float StretchStartRollOffset;
    }
}
