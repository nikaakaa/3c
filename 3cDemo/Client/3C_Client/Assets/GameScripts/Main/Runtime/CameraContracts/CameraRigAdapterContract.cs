namespace ThirdPersonCamera
{
    public interface ICameraBasisSnapshotProvider
    {
        CameraBasisSnapshot BasisSnapshot { get; }
    }

    public interface ICameraRigAdapter : ICameraBasisSnapshotProvider
    {
        CameraRigResult Result { get; }
        void ValidateBinding(string shotId);
        void Apply(in CameraFramePlan plan);
        void Reset();
    }
}
