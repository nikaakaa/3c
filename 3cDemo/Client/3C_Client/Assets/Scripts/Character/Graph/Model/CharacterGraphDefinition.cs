using ThirdPersonAction;

namespace ThirdPersonCharacterGraph
{
    public sealed class CharacterGraphDefinition
    {
        public CharacterGraphDefinition(
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

        public static CharacterGraphDefinition Empty =>
            new CharacterGraphDefinition(
                LocomotionBranchDefinition.Empty,
                ActionBranchDefinition.Empty,
                UpperBodyBranchDefinition.Empty,
                CueBranchDefinition.Empty);
    }
}
