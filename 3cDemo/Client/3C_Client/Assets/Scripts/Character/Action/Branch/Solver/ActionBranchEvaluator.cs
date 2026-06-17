namespace ThirdPersonAction
{
    public readonly struct ActionBranchEvaluationInput
    {
        public ActionBranchEvaluationInput(
            ActionBranchDefinition branch,
            int currentFrame,
            int sourceStep)
        {
            Branch = branch;
            CurrentFrame = currentFrame < 0 ? 0 : currentFrame;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public ActionBranchDefinition Branch { get; }
        public int CurrentFrame { get; }
        public int SourceStep { get; }
    }

    public static class ActionBranchEvaluator
    {
        public static ActionBranchOutcome Evaluate(in ActionBranchEvaluationInput input)
        {
            ActionBranchDefinition branch = input.Branch;
            if (!branch.CanEvaluate || branch.RootNode.Kind != ActionNodeKind.Timeline)
                return ActionBranchOutcome.None(input.SourceStep);

            ActionTimelineOutcome timelineOutcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(
                    branch.RootNode.TimelineNode.Timeline,
                    input.CurrentFrame,
                    input.SourceStep));
            CharacterFrameCandidateOutput candidate = CharacterFrameCandidateOutput.FullBodyAction(
                timelineOutcome.HasMotion,
                timelineOutcome.HasAnimation,
                input.SourceStep);
            BodyOccupancyClaim claim = timelineOutcome.HasOutcome
                ? branch.DefaultBodyClaim
                : BodyOccupancyClaim.None(input.SourceStep);

            return new ActionBranchOutcome(
                timelineOutcome,
                candidate,
                claim,
                input.SourceStep);
        }
    }
}
