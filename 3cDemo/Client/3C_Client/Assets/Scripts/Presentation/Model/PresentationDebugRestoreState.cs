namespace ThirdPersonPresentation
{
    public readonly struct PresentationDebugRestoreState
    {
        public PresentationDebugRestoreState(
            PresentationPose previousTickPose,
            PresentationPose currentTickPose,
            PresentationPose correctionStartPose,
            float correctionDurationSeconds,
            float correctionElapsedSeconds,
            bool hasPreviousTickPose,
            bool hasCurrentTickPose,
            bool correctionActive)
        {
            PreviousTickPose = previousTickPose;
            CurrentTickPose = currentTickPose;
            CorrectionStartPose = correctionStartPose;
            CorrectionDurationSeconds = correctionDurationSeconds > 0f ? correctionDurationSeconds : 0f;
            CorrectionElapsedSeconds = correctionElapsedSeconds > 0f ? correctionElapsedSeconds : 0f;
            HasPreviousTickPose = hasPreviousTickPose;
            HasCurrentTickPose = hasCurrentTickPose;
            CorrectionActive = correctionActive;
        }

        public PresentationPose PreviousTickPose { get; }
        public PresentationPose CurrentTickPose { get; }
        public PresentationPose CorrectionStartPose { get; }
        public float CorrectionDurationSeconds { get; }
        public float CorrectionElapsedSeconds { get; }
        public bool HasPreviousTickPose { get; }
        public bool HasCurrentTickPose { get; }
        public bool CorrectionActive { get; }
    }
}
