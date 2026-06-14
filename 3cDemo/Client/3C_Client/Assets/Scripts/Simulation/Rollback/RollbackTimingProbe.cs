using System.Text;
using ThirdPersonCamera;
using ThirdPersonPresentation;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct RollbackTimingProbePose
    {
        public RollbackTimingProbePose(
            Vector3 sourcePosition,
            float sourceYaw,
            Vector3 visualPosition,
            float visualYaw,
            bool hasVisual,
            bool correctionActive,
            bool hasCamera,
            float cameraYaw,
            float cameraPitch,
            Vector3 cameraAnchorPosition,
            Vector3 cameraFollowPosition,
            Vector3 cameraAimPosition,
            Vector3 freeLookPosition,
            float freeLookYaw,
            Vector3 mainCameraPosition,
            float mainCameraYaw,
            bool hasCameraFollow,
            bool hasCameraAim,
            bool hasFreeLook,
            bool hasMainCamera)
        {
            SourcePosition = sourcePosition;
            SourceYaw = sourceYaw;
            VisualPosition = visualPosition;
            VisualYaw = visualYaw;
            HasVisual = hasVisual;
            CorrectionActive = correctionActive;
            HasCamera = hasCamera;
            CameraYaw = cameraYaw;
            CameraPitch = cameraPitch;
            CameraAnchorPosition = cameraAnchorPosition;
            CameraFollowPosition = cameraFollowPosition;
            CameraAimPosition = cameraAimPosition;
            FreeLookPosition = freeLookPosition;
            FreeLookYaw = freeLookYaw;
            MainCameraPosition = mainCameraPosition;
            MainCameraYaw = mainCameraYaw;
            HasCameraFollow = hasCameraFollow;
            HasCameraAim = hasCameraAim;
            HasFreeLook = hasFreeLook;
            HasMainCamera = hasMainCamera;
        }

        public Vector3 SourcePosition { get; }
        public float SourceYaw { get; }
        public Vector3 VisualPosition { get; }
        public float VisualYaw { get; }
        public bool HasVisual { get; }
        public bool CorrectionActive { get; }
        public bool HasCamera { get; }
        public float CameraYaw { get; }
        public float CameraPitch { get; }
        public Vector3 CameraAnchorPosition { get; }
        public Vector3 CameraFollowPosition { get; }
        public Vector3 CameraAimPosition { get; }
        public Vector3 FreeLookPosition { get; }
        public float FreeLookYaw { get; }
        public Vector3 MainCameraPosition { get; }
        public float MainCameraYaw { get; }
        public bool HasCameraFollow { get; }
        public bool HasCameraAim { get; }
        public bool HasFreeLook { get; }
        public bool HasMainCamera { get; }
    }

    public static class RollbackTimingProbe
    {
        public static bool TryCapture(
            PresentationTransformInterpolator presentationInterpolator,
            ThirdPersonCameraController camera,
            Transform fallbackSource,
            out RollbackTimingProbePose pose)
        {
            Transform source = presentationInterpolator != null ? presentationInterpolator.Source : fallbackSource;
            Transform visual = presentationInterpolator != null ? presentationInterpolator.VisualTarget : null;
            Transform cameraAnchor = camera != null ? camera.FollowAnchorSource : null;
            Transform cameraFollow = camera != null ? camera.CameraFollowTarget : null;
            Transform cameraAim = camera != null ? camera.CameraAimTarget : null;
            Transform freeLook = camera != null && camera.FreeLook != null ? camera.FreeLook.transform : null;
            Transform mainCamera = Camera.main != null ? Camera.main.transform : null;
            Transform safeFallback = fallbackSource != null ? fallbackSource : source;
            pose = new RollbackTimingProbePose(
                source != null ? source.position : safeFallback != null ? safeFallback.position : Vector3.zero,
                source != null ? source.eulerAngles.y : safeFallback != null ? safeFallback.eulerAngles.y : 0f,
                visual != null ? visual.position : Vector3.zero,
                visual != null ? visual.eulerAngles.y : 0f,
                visual != null,
                presentationInterpolator != null && presentationInterpolator.IsCorrectionActive,
                camera != null,
                camera != null ? camera.Yaw : 0f,
                camera != null ? camera.Pitch : 0f,
                cameraAnchor != null ? cameraAnchor.position : Vector3.zero,
                cameraFollow != null ? cameraFollow.position : Vector3.zero,
                cameraAim != null ? cameraAim.position : Vector3.zero,
                freeLook != null ? freeLook.position : Vector3.zero,
                freeLook != null ? freeLook.eulerAngles.y : 0f,
                mainCamera != null ? mainCamera.position : Vector3.zero,
                mainCamera != null ? mainCamera.eulerAngles.y : 0f,
                cameraFollow != null,
                cameraAim != null,
                freeLook != null,
                mainCamera != null);
            return source != null;
        }

        public static string Format(
            in LocalRollbackSynctestResult result,
            bool applyReplayResultToScene,
            bool hasVisualStartPose,
            bool hasPresentationState,
            bool hasTimingProbeStart,
            in RollbackTimingProbePose startPose,
            in RollbackTimingProbePose replayPose,
            in RollbackTimingProbePose finalPose)
        {
            StringBuilder builder = new StringBuilder(384);
            builder.Append("ROLLBACK_TIMING_PROBE");
            builder.Append(" result=").Append(result.Success ? "PASS" : "FAIL");
            builder.Append(" restore=").Append(result.RestoreTick.Value);
            builder.Append(" end=").Append(result.EndTick.Value);
            builder.Append(" applyReplay=").Append(applyReplayResultToScene);
            builder.Append(" visualPose=").Append(hasVisualStartPose);
            builder.Append(" presentationState=").Append(hasPresentationState);
            builder.Append(" cameraState=local-only");
            builder.Append(" startCaptured=").Append(hasTimingProbeStart);
            builder.Append(" sourceStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.SourcePosition)).Append('/').Append(startPose.SourceYaw.ToString("F3"));
            builder.Append(" sourceReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.SourcePosition)).Append('/').Append(replayPose.SourceYaw.ToString("F3"));
            builder.Append(" sourceFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.SourcePosition)).Append('/').Append(finalPose.SourceYaw.ToString("F3"));
            builder.Append(" visualStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.VisualPosition)).Append('/').Append(startPose.VisualYaw.ToString("F3"));
            builder.Append(" visualReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.VisualPosition)).Append('/').Append(replayPose.VisualYaw.ToString("F3"));
            builder.Append(" visualFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.VisualPosition)).Append('/').Append(finalPose.VisualYaw.ToString("F3"));
            builder.Append(" hasVisualFinal=").Append(finalPose.HasVisual);
            builder.Append(" correctionFinal=").Append(finalPose.CorrectionActive);
            builder.Append(" hasCameraFinal=").Append(finalPose.HasCamera);
            builder.Append(" camYawStart=").Append(startPose.CameraYaw.ToString("F3")).Append('/').Append(startPose.CameraPitch.ToString("F3"));
            builder.Append(" camYawReplay=").Append(replayPose.CameraYaw.ToString("F3")).Append('/').Append(replayPose.CameraPitch.ToString("F3"));
            builder.Append(" camYawFinal=").Append(finalPose.CameraYaw.ToString("F3")).Append('/').Append(finalPose.CameraPitch.ToString("F3"));
            builder.Append(" camAnchorStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.CameraAnchorPosition));
            builder.Append(" camAnchorReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.CameraAnchorPosition));
            builder.Append(" camAnchorFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.CameraAnchorPosition));
            builder.Append(" camFollowStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.CameraFollowPosition));
            builder.Append(" camFollowReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.CameraFollowPosition));
            builder.Append(" camFollowFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.CameraFollowPosition));
            builder.Append(" camAimStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.CameraAimPosition));
            builder.Append(" camAimReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.CameraAimPosition));
            builder.Append(" camAimFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.CameraAimPosition));
            builder.Append(" freeLookStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.FreeLookPosition)).Append('/').Append(startPose.FreeLookYaw.ToString("F3"));
            builder.Append(" freeLookReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.FreeLookPosition)).Append('/').Append(replayPose.FreeLookYaw.ToString("F3"));
            builder.Append(" freeLookFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.FreeLookPosition)).Append('/').Append(finalPose.FreeLookYaw.ToString("F3"));
            builder.Append(" mainCameraStart=").Append(LocalRollbackSynctestLogFormatter.Format(startPose.MainCameraPosition)).Append('/').Append(startPose.MainCameraYaw.ToString("F3"));
            builder.Append(" mainCameraReplay=").Append(LocalRollbackSynctestLogFormatter.Format(replayPose.MainCameraPosition)).Append('/').Append(replayPose.MainCameraYaw.ToString("F3"));
            builder.Append(" mainCameraFinal=").Append(LocalRollbackSynctestLogFormatter.Format(finalPose.MainCameraPosition)).Append('/').Append(finalPose.MainCameraYaw.ToString("F3"));
            return builder.ToString();
        }
    }
}
