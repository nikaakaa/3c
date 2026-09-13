using System;
using Cinemachine;
using UnityEditor;

namespace ThirdPersonCamera
{
    internal static class CameraShotProjectionCompiler
    {
        public static CameraShotPayload Compile(
            CameraShotAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset.CinePrefabPath);
            if (!prefab)
                throw new InvalidOperationException(
                    $"Camera Shot '{asset.ShotId}' references missing CinePrefab '{asset.CinePrefabPath}'.");
            if (!prefab.GetComponentInChildren<CinemachineVirtualCamera>(true))
                throw new InvalidOperationException(
                    $"Camera Shot '{asset.ShotId}' CinePrefab '{asset.CinePrefabPath}' has no CinemachineVirtualCamera.");
            if (asset.TimeDomain == CameraTimeDomain.OwnerScaled ||
                asset.TimeDomain == CameraTimeDomain.LocalAvatarScaled)
                throw new InvalidOperationException(
                    $"Camera Shot '{asset.ShotId}' uses '{asset.TimeDomain}', but Presentation has no formal owner time source.");
            var blendIn = new CameraShotBlendPayload(
                asset.BlendIn.Duration,
                context.CompileCurve(context.RequireCurve(asset.BlendIn.Curve)),
                asset.BlendIn.UseCoreSpace,
                asset.BlendIn.UseDelta);
            var blendOut = new CameraShotBlendPayload(
                asset.BlendOut.Duration,
                context.CompileCurve(context.RequireCurve(asset.BlendOut.Curve)),
                asset.BlendOut.UseCoreSpace,
                asset.BlendOut.UseDelta);
            var payload = new CameraShotPayload(
                asset.ShotId,
                asset.CinePrefabPath,
                asset.FollowTargetSlotId,
                asset.LookAtTargetSlotId,
                asset.NearClipPlane,
                asset.FarClipPlane,
                asset.Duration,
                asset.TimeDomain,
                asset.IgnoreCameraCollision,
                asset.ApplyEntityTimeScale,
                asset.FollowOffset,
                asset.LookAtOffset,
                asset.OffsetRotation,
                asset.FieldOfView,
                blendIn,
                blendOut,
                asset.BlendWithIgnoreLookAtTarget,
                asset.Priority,
                asset.Tag);
            payload.RequireValid($"Camera Shot '{asset.ShotId}'");
            return payload;
        }
    }
}
