namespace ThirdPersonCharacterGraph
{
    public sealed class CharacterBehaviorGraphDefinition
    {
        public CharacterBehaviorGraphDefinition(
            LocomotionBranchDefinition locomotion,
            ActionBranchDefinition action,
            UpperBodyBranchDefinition upperBody,
            CueBranchDefinition cue)
        {
            Locomotion = locomotion;
            Action = action;
            UpperBody = upperBody;
            Cue = cue;
        }

        public LocomotionBranchDefinition Locomotion { get; }
        public ActionBranchDefinition Action { get; }
        public UpperBodyBranchDefinition UpperBody { get; }
        public CueBranchDefinition Cue { get; }
        public bool HasAnyBranch =>
            Locomotion.IsDefined ||
            Action.IsDefined ||
            UpperBody.IsDefined ||
            Cue.IsDefined;

        public static CharacterBehaviorGraphDefinition Empty =>
            new CharacterBehaviorGraphDefinition(
                LocomotionBranchDefinition.Empty,
                ActionBranchDefinition.Empty,
                UpperBodyBranchDefinition.Empty,
                CueBranchDefinition.Empty);
    }
}
