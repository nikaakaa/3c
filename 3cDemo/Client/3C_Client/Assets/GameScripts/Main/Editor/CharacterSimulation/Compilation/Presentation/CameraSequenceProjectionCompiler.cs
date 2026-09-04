using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraSequenceProjectionCompiler
    {
        public static CameraSequencePayload Compile(CameraSequenceAsset asset)
        {
            asset.RequireValid();
            if (asset.TimeDomain == CameraTimeDomain.OwnerScaled ||
                asset.TimeDomain == CameraTimeDomain.LocalAvatarScaled)
                throw new InvalidOperationException(
                    $"Camera Sequence '{asset.SequenceId}' uses '{asset.TimeDomain}', but the current Presentation Tick has no formal source for that time domain.");
            var stages = new CameraSequenceStagePayload[asset.Stages.Count];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = CompileStage(asset.Stages[i]);
            return new CameraSequencePayload(asset.SequenceId, asset.TimeDomain, stages);
        }

        static CameraSequenceStagePayload CompileStage(CameraSequenceStage stage)
        {
            stage.RequireValid(stage.GetType().Name);
            switch (stage)
            {
                case CameraFrameOnePointByHeightStage byHeight:
                    return new CameraFrameOnePointByHeightPayload(
                        byHeight.StageId,
                        byHeight.MakeContextDependent,
                        byHeight.PlayLength,
                        byHeight.EntityHeight,
                        byHeight.HeightRatio,
                        byHeight.FieldOfView,
                        byHeight.ScreenOffset);
                case CameraFrameOnePointByScreenOffsetStage byScreen:
                    return new CameraFrameOnePointByScreenOffsetPayload(
                        byScreen.StageId,
                        byScreen.MakeContextDependent,
                        byScreen.PlayLength,
                        byScreen.AspectRatio,
                        byScreen.FieldOfView,
                        byScreen.ScreenOffset,
                        byScreen.Radius);
                case CameraFrameOnePointByTrackStage byTrack:
                    throw new InvalidOperationException(
                        $"Camera Sequence stage '{byTrack.StageId}' uses ZZZ ByTrack data, but its WorldBasicCameraData consumer is not closed.");
                case CameraRotationEulerOffsetStage euler:
                    return new CameraRotationEulerOffsetPayload(
                        euler.StageId,
                        euler.MakeContextDependent,
                        euler.PlayLength,
                        euler.Offset,
                        euler.FlipForward);
                default:
                    throw new InvalidOperationException(
                        $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed source evaluator.");
            }
        }
    }
}
