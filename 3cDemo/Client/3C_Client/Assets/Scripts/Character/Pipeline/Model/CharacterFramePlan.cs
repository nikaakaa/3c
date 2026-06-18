using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAction
{
    public readonly struct CharacterFramePlan
    {
        public CharacterFramePlan(BodyOccupancyDecision occupancyDecision)
            : this(occupancyDecision, LocomotionPreemptionFact.None)
        {
        }

        public CharacterFramePlan(
            BodyOccupancyDecision occupancyDecision,
            LocomotionPreemptionFact locomotionPreemption)
        {
            OccupancyDecision = occupancyDecision;
            LocomotionPreemption = locomotionPreemption;
        }

        public BodyOccupancyDecision OccupancyDecision { get; }
        public LocomotionPreemptionFact LocomotionPreemption { get; }
        public int SourceStep => OccupancyDecision.SourceStep;
        public bool HasPlan => OccupancyDecision.HasDecision;
        public bool SuppressesLocomotionMotion => OccupancyDecision.SuppressLocomotionMotion;
        public bool SuppressesLocomotionAnimation => OccupancyDecision.SuppressLocomotionAnimation;
        public CharacterBodyDomain BaseSlotOwner => OccupancyDecision.BaseSlotOwner;
        public CharacterBodyDomain UpperBodySlotOwner => OccupancyDecision.UpperBodySlotOwner;
        public bool UpperBodySlotSuppressed => OccupancyDecision.UpperBodySlotSuppressed;

        public static CharacterFramePlan None(int sourceStep = 0)
        {
            return new CharacterFramePlan(BodyOccupancyDecision.None(sourceStep));
        }

        public static CharacterFramePlan PassThrough(in CharacterFrameSubmission submission)
        {
            return DefaultBodyArbiter.Instance.CreatePlan(in submission);
        }
    }
}
