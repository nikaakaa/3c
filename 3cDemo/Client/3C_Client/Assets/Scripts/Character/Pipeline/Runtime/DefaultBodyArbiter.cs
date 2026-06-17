using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAction
{
    public sealed class DefaultBodyArbiter : IBodyArbiter
    {
        public static DefaultBodyArbiter Instance { get; } = new DefaultBodyArbiter();

        public BodyOccupancyDecision Decide(in CharacterFrameArbitrationInput input)
        {
            if (input.OccupancyClaim.ClaimsFullBody)
            {
                return new BodyOccupancyDecision(
                    CharacterBodyDomain.FullBodyAction,
                    CharacterBodyDomain.None,
                    true,
                    input.LocomotionCandidate.HasMotionCandidate,
                    input.LocomotionCandidate.HasAnimationCandidate,
                    false,
                    input.SourceStep);
            }

            CharacterBodyDomain baseOwner = input.LocomotionCandidate.HasAnyCandidate
                ? CharacterBodyDomain.Locomotion
                : input.FullBodyActionCandidate.HasAnyCandidate
                    ? CharacterBodyDomain.FullBodyAction
                    : CharacterBodyDomain.None;
            CharacterBodyDomain upperBodyOwner = input.UpperBodyCandidate.HasAnyCandidate ||
                                                input.OccupancyClaim.ClaimsUpperBody
                ? CharacterBodyDomain.UpperBody
                : CharacterBodyDomain.None;

            return new BodyOccupancyDecision(
                baseOwner,
                upperBodyOwner,
                false,
                false,
                false,
                upperBodyOwner == CharacterBodyDomain.UpperBody,
                input.SourceStep);
        }

        public CharacterFramePlan CreatePlan(in CharacterFrameSubmission submission)
        {
            CharacterFrameArbitrationInput input = CharacterFrameArbitrationInput.FromSubmission(in submission);
            BodyOccupancyDecision decision = Decide(in input);
            LocomotionPreemptionFact locomotionPreemption = decision.FullBodyClaimAccepted
                ? submission.LocomotionPreemption
                : LocomotionPreemptionFact.None;
            return new CharacterFramePlan(decision, locomotionPreemption);
        }
    }
}
