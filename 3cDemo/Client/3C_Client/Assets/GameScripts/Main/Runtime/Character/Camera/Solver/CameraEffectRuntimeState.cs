namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeState
    {
        internal struct State
        {
            internal CameraEffectRequest Request;
            internal string Tag;
            internal float Elapsed;
            internal bool Retired;
            internal float RetireElapsed;
            internal float RetireStartElapsed;
            internal CameraPresentationStopReason RetireReason;
        }

        internal State CaptureState() => new State
        {
            Request = Request,
            Tag = Tag,
            Elapsed = Elapsed,
            Retired = Retired,
            RetireElapsed = RetireElapsed,
            RetireStartElapsed = RetireStartElapsed,
            RetireReason = RetireReason
        };

        internal void RestoreState(in State state)
        {
            Request = state.Request;
            Tag = state.Tag;
            Elapsed = state.Elapsed;
            Retired = state.Retired;
            RetireElapsed = state.RetireElapsed;
            RetireStartElapsed = state.RetireStartElapsed;
            RetireReason = state.RetireReason;
        }

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
