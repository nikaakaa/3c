namespace ThirdPersonCamera
{
    public interface ICameraInfluenceSource
    {
        CameraInfluenceRequest CurrentRequest { get; }
    }
}
