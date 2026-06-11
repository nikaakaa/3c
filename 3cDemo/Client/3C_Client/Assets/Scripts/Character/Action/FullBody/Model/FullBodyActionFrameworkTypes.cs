namespace ThirdPersonAction
{
    public enum FullBodyOwnerKind
    {
        None = 0,
        Locomotion = 1,
        Action = 2
    }

    public readonly struct FullBodyOwner
    {
        FullBodyOwner(FullBodyOwnerKind kind, ActionStateId actionState)
        {
            Kind = kind;
            ActionState = actionState;
        }

        public FullBodyOwnerKind Kind { get; }
        public ActionStateId ActionState { get; }
        public bool IsLocomotion => Kind == FullBodyOwnerKind.Locomotion;
        public bool IsAction => Kind == FullBodyOwnerKind.Action && ActionState.IsValid;

        public static FullBodyOwner None => new FullBodyOwner(FullBodyOwnerKind.None, ActionStateIds.None);
        public static FullBodyOwner Locomotion => new FullBodyOwner(FullBodyOwnerKind.Locomotion, ActionStateIds.None);

        public static FullBodyOwner Action(ActionStateId actionState)
        {
            return new FullBodyOwner(FullBodyOwnerKind.Action, actionState.IsValid ? actionState : ActionStateIds.None);
        }
    }
}
