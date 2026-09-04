using System.Collections.Generic;

namespace ThirdPersonCamera
{
    internal interface ICameraEffectOwner
    {
        CameraEffectKind Kind { get; }
        CameraEffectStage Stage { get; }
        bool UpdatesBySource { get; }
        bool HasResource(string resourceId);
        CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input);
        float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input);
        bool IsExpired(CameraEffectRuntimeState active);
        float RetireDuration(CameraEffectRuntimeState active);
    }
}
