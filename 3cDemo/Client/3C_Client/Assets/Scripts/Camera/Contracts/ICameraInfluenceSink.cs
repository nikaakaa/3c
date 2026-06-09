namespace ThirdPersonCamera
{
    public interface ICameraInfluenceSink
    {
        CameraInfluenceHandle CreateInfluenceHandle(CameraInfluenceRequest initialRequest);
        void RegisterInfluenceSource(ICameraInfluenceSource source);
        void UnregisterInfluenceSource(ICameraInfluenceSource source);
    }
}
