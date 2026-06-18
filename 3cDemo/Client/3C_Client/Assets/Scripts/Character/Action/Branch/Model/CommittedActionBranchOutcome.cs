namespace ThirdPersonAction
{
    public readonly struct CommittedActionBranchOutcome
    {
        public CommittedActionBranchOutcome(
            ActionTimelineOutcome timelineOutcome,
            CharacterFrameCandidateOutput candidate,
            BodyOccupancyClaim bodyClaim,
            int sourceStep)
            : this(timelineOutcome, candidate, bodyClaim, sourceStep, default, string.Empty)
        {
        }

        public CommittedActionBranchOutcome(
            ActionTimelineOutcome timelineOutcome,
            CharacterFrameCandidateOutput candidate,
            BodyOccupancyClaim bodyClaim,
            int sourceStep,
            CommittedActionNodeId selectedNodeId,
            string diagnostic)
        {
            TimelineOutcome = timelineOutcome;
            Candidate = candidate;
            BodyClaim = bodyClaim;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            SelectedNodeId = selectedNodeId;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ActionTimelineOutcome TimelineOutcome { get; }
        public CharacterFrameCandidateOutput Candidate { get; }
        public BodyOccupancyClaim BodyClaim { get; }
        public int SourceStep { get; }
        public CommittedActionNodeId SelectedNodeId { get; }
        public string Diagnostic { get; }
        public bool HasOutcome => TimelineOutcome.HasOutcome || Candidate.HasAnyCandidate || BodyClaim.HasClaim;
        public bool HasDiagnostic => !string.IsNullOrWhiteSpace(Diagnostic);
        public bool HasEvaluation => HasOutcome || HasDiagnostic || SelectedNodeId.IsValid;

        public static CommittedActionBranchOutcome None(int sourceStep = 0)
        {
            return new CommittedActionBranchOutcome(
                ActionTimelineOutcome.None(0, sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.CommittedAction, sourceStep),
                BodyOccupancyClaim.None(sourceStep),
                sourceStep);
        }

        public static CommittedActionBranchOutcome DiagnosticOnly(int sourceStep, string diagnostic)
        {
            return new CommittedActionBranchOutcome(
                ActionTimelineOutcome.None(0, sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.CommittedAction, sourceStep),
                BodyOccupancyClaim.None(sourceStep),
                sourceStep,
                default,
                diagnostic);
        }
    }
}
