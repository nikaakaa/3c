namespace ThirdPersonGameplay.ScenePlay
{
    public enum BtsmtlScenePlayRuntimeOwnerState : byte
    {
        Unavailable = 1,
        Preparing = 2,
        Ready = 3,
        Failed = 4,
        Stopped = 5
    }

    public readonly struct BtsmtlScenePlayRuntimeOwnerStatus
    {
        public BtsmtlScenePlayRuntimeOwnerStatus(
            string ownerIdentity,
            BtsmtlScenePlayRuntimeOwnerState state,
            string failureCode = "",
            string failureMessage = "")
        {
            OwnerIdentity = ownerIdentity ?? string.Empty;
            State = state;
            FailureCode = failureCode ?? string.Empty;
            FailureMessage = failureMessage ?? string.Empty;
        }

        public string OwnerIdentity { get; }
        public BtsmtlScenePlayRuntimeOwnerState State { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
        public bool IsReady => State == BtsmtlScenePlayRuntimeOwnerState.Ready;
        public bool HasFailure => State == BtsmtlScenePlayRuntimeOwnerState.Failed;
    }

    public readonly struct BtsmtlScenePlayRuntimeOwnerReleaseResult
    {
        public BtsmtlScenePlayRuntimeOwnerReleaseResult(
            bool succeeded,
            string failureCode = "",
            string failureMessage = "")
        {
            Succeeded = succeeded;
            FailureCode = failureCode ?? string.Empty;
            FailureMessage = failureMessage ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
    }

    public interface IBtsmtlScenePlayRuntimeOwner
    {
        BtsmtlScenePlayRuntimeOwnerStatus Status { get; }
        BtsmtlScenePlayRuntimeOwnerReleaseResult Release();
    }
}
