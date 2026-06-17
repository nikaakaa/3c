namespace ThirdPersonAction
{
    public interface IBodyArbiter
    {
        BodyOccupancyDecision Decide(in CharacterFrameArbitrationInput input);
        CharacterFramePlan CreatePlan(in CharacterFrameSubmission submission);
    }
}
