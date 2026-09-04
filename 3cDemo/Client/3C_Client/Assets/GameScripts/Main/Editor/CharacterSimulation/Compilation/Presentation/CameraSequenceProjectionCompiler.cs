using System;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraSequenceProjectionCompiler
    {
        public static CameraSequencePayload Compile(CameraSequenceAsset asset)
        {
            asset.RequireValid();
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
                    return new CameraFrameOnePointByTrackPayload(
                        byTrack.StageId,
                        byTrack.MakeContextDependent,
                        byTrack.PlayLength,
                        byTrack.CameraOrbits
                            .Select(value => new CameraOrbitPayload(value.Height, value.Radius, value.ScreenY))
                            .ToArray(),
                        byTrack.AspectRatio,
                        byTrack.FieldOfView,
                        byTrack.ScreenOffset,
                        byTrack.ElevationRatio,
                        byTrack.PolarAngle);
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
