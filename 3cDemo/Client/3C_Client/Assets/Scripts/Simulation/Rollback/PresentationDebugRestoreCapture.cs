using ThirdPersonPresentation;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct PresentationDebugRestoreCapture
    {
        public PresentationDebugRestoreCapture(
            PresentationPose visualStartPose,
            bool hasVisualStartPose,
            PresentationDebugRestoreState restoreState,
            bool hasRestoreState)
        {
            VisualStartPose = visualStartPose;
            HasVisualStartPose = hasVisualStartPose;
            RestoreState = restoreState;
            HasRestoreState = hasRestoreState;
        }

        public PresentationPose VisualStartPose { get; }
        public bool HasVisualStartPose { get; }
        public PresentationDebugRestoreState RestoreState { get; }
        public bool HasRestoreState { get; }
    }

    public static class PresentationDebugRestoreGuard
    {
        public static PresentationDebugRestoreCapture Capture(PresentationTransformInterpolator interpolator)
        {
            PresentationPose visualPose = default;
            bool hasVisualPose = false;
            if (interpolator != null && interpolator.VisualTarget != null)
            {
                visualPose = PresentationPose.FromTransform(interpolator.VisualTarget);
                hasVisualPose = true;
            }

            PresentationDebugRestoreState state = default;
            bool hasState = false;
            if (interpolator != null)
            {
                state = interpolator.CaptureDebugRestoreState();
                hasState = true;
            }

            return new PresentationDebugRestoreCapture(visualPose, hasVisualPose, state, hasState);
        }

        public static void Restore(
            PresentationTransformInterpolator interpolator,
            bool applyReplayResultToScene,
            float visualCorrectionSeconds,
            in PresentationDebugRestoreCapture capture)
        {
            if (interpolator == null)
                return;

            if (applyReplayResultToScene)
            {
                if (capture.HasVisualStartPose)
                {
                    interpolator.BeginCorrection(capture.VisualStartPose, Mathf.Max(0f, visualCorrectionSeconds));
                    interpolator.UpdateVisualTarget();
                }

                return;
            }

            if (capture.HasRestoreState)
                interpolator.RestoreDebugRestoreState(capture.RestoreState);
            else
                interpolator.ResetSamples();

            interpolator.UpdateVisualTarget();
        }
    }
}
