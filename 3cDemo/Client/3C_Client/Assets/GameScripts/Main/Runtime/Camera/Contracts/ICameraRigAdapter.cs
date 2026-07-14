namespace ThirdPersonCamera
{
    public interface ICameraRigAdapter
    {
        CameraBasisSnapshot BasisSnapshot { get; }
        void Apply(CameraPosePlan plan);
    }
}
