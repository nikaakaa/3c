using System;

namespace ThirdPersonAction
{
    [Serializable]
    public struct ActionInterruptPolicyDefinition
    {
        public string fromStateId;
        public string targetStateId;
        public int minPriority;
        public ActionInterruptTimingRule timingRule;
        public float windowStart;
        public float windowEnd;
        public string windowId;
        public bool force;
        public string note;

        public ActionInterruptPolicyDefinition(
            string fromStateId,
            string targetStateId,
            int minPriority,
            ActionInterruptTimingRule timingRule = ActionInterruptTimingRule.Always,
            float windowStart = 0f,
            float windowEnd = 0f,
            bool force = false,
            string windowId = "",
            string note = "")
        {
            this.fromStateId = fromStateId ?? string.Empty;
            this.targetStateId = targetStateId ?? string.Empty;
            this.minPriority = minPriority;
            this.timingRule = timingRule;
            this.windowStart = windowStart;
            this.windowEnd = windowEnd;
            this.windowId = windowId ?? string.Empty;
            this.force = force;
            this.note = note ?? string.Empty;
        }

        public string FromStateId => fromStateId ?? string.Empty;
        public string TargetStateId => targetStateId ?? string.Empty;
        public int MinPriority => minPriority;
        public ActionInterruptTimingRule TimingRule => timingRule;
        public float WindowStart => windowStart;
        public float WindowEnd => windowEnd;
        public string WindowId => windowId ?? string.Empty;
        public bool Force => force;
        public string Note => note ?? string.Empty;
    }
}
