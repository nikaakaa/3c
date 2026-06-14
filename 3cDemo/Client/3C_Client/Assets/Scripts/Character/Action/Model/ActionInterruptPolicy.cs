using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionInterruptPolicy
    {
        public ActionInterruptPolicy(
            ActionStateId fromState,
            ActionStateId targetState,
            int minPriority,
            ActionInterruptTimingRule timingRule = ActionInterruptTimingRule.Always,
            float windowStart = 0f,
            float windowEnd = 0f,
            bool force = false,
            string windowId = "")
        {
            FromState = fromState;
            TargetState = targetState;
            MinPriority = minPriority;
            TimingRule = timingRule;
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            WindowId = windowId ?? string.Empty;
            Force = force;
        }

        public ActionStateId FromState { get; }
        public ActionStateId TargetState { get; }
        public int MinPriority { get; }
        public ActionInterruptTimingRule TimingRule { get; }
        public float WindowStart { get; }
        public float WindowEnd { get; }
        public string WindowId { get; }
        public bool Force { get; }
        public bool RequiresTimelineWindow => !string.IsNullOrWhiteSpace(WindowId);

        public bool Matches(ActionInterruptContext context, ActionInterruptRequest request)
        {
            return FromState.Matches(context.CurrentState) && TargetState.Matches(request.TargetState);
        }
    }
}
