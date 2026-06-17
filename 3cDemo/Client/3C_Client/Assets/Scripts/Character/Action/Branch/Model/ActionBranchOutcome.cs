namespace ThirdPersonAction
{
    public readonly struct ActionBranchOutcome
    {
        public ActionBranchOutcome(
            ActionTimelineOutcome timelineOutcome,
            CharacterFrameCandidateOutput candidate,
            BodyOccupancyClaim bodyClaim,
            int sourceStep)
        {
            TimelineOutcome = timelineOutcome;
            Candidate = candidate;
            BodyClaim = bodyClaim;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public ActionTimelineOutcome TimelineOutcome { get; }
        public CharacterFrameCandidateOutput Candidate { get; }
        public BodyOccupancyClaim BodyClaim { get; }
        public int SourceStep { get; }
        public bool HasOutcome => TimelineOutcome.HasOutcome || Candidate.HasAnyCandidate || BodyClaim.HasClaim;

        public static ActionBranchOutcome None(int sourceStep = 0)
        {
            return new ActionBranchOutcome(
                ActionTimelineOutcome.None(0, sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.FullBodyAction, sourceStep),
                BodyOccupancyClaim.None(sourceStep),
                sourceStep);
        }
    }
}
