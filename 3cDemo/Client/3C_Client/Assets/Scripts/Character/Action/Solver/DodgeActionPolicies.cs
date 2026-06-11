namespace ThirdPersonAction
{
    public static class DodgeActionPolicies
    {
        public static ActionInterruptPolicy CreateDefaultFromNone(in DodgeActionConfig config)
        {
            return new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, config.Priority);
        }
    }
}
