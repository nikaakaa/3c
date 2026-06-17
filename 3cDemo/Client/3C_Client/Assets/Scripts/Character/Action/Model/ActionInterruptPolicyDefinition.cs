using System;

namespace ThirdPersonAction
{
    [Serializable]
    public struct ActionInterruptPolicyDefinition
    {
        public string fromStateId;
        public string targetStateId;
        public ActionRequestType requestType;
        public int minPriority;
        public ActionInterruptTimingRule timingRule;
        public float windowStart;
        public float windowEnd;
        public string windowId;
        public string requiredFactId;
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
            string requiredFactId = "",
            string note = "",
            ActionRequestType requestType = ActionRequestType.None)
        {
            this.fromStateId = fromStateId ?? string.Empty;
            this.targetStateId = targetStateId ?? string.Empty;
            this.requestType = requestType;
            this.minPriority = minPriority;
            this.timingRule = timingRule;
            this.windowStart = windowStart;
            this.windowEnd = windowEnd;
            this.windowId = windowId ?? string.Empty;
            this.requiredFactId = requiredFactId ?? string.Empty;
            this.force = force;
            this.note = note ?? string.Empty;
        }

        public string FromStateId => fromStateId ?? string.Empty;
        public string TargetStateId => targetStateId ?? string.Empty;
        public ActionRequestType RequestType => requestType;
        public int MinPriority => minPriority;
        public ActionInterruptTimingRule TimingRule => timingRule;
        public float WindowStart => windowStart;
        public float WindowEnd => windowEnd;
        public string WindowId => windowId ?? string.Empty;
        public string RequiredFactId => requiredFactId ?? string.Empty;
        public bool Force => force;
        public string Note => note ?? string.Empty;
    }
}
