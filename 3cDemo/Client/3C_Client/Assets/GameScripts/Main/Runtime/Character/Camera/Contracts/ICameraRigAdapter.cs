namespace ThirdPersonCamera
{
    public interface ICameraRigAdapter
    {
        CameraBasisSnapshot BasisSnapshot { get; }
        CameraRigResult Result { get; }
        void Apply(in CameraFramePlan plan);
        void Reset();
    }
}
