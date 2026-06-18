using System;
using System.Collections.Generic;

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
        public ActionTransitionResistanceRule resistanceRule;
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
            ActionRequestType requestType = ActionRequestType.None,
            ActionTransitionResistanceRule resistanceRule = ActionTransitionResistanceRule.UseCurrentState)
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
            this.resistanceRule = resistanceRule;
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
        public ActionTransitionResistanceRule ResistanceRule => resistanceRule;
        public string Note => note ?? string.Empty;
    }

    [Serializable]
    public struct ActionTransitionPolicyRowDefinition
    {
        public string fromActionId;
        public string toActionId;
        public ActionRequestType requestType;
        public string requiredFactId;
        public int minPriority;
        public bool force;
        public ActionTransitionResistanceRule resistanceRule;
        public string diagnosticsLabel;

        public ActionTransitionPolicyRowDefinition(
            string fromActionId,
            string toActionId,
            ActionRequestType requestType,
            string requiredFactId,
            int minPriority,
            bool force = false,
            ActionTransitionResistanceRule resistanceRule = ActionTransitionResistanceRule.UseCurrentState,
            string diagnosticsLabel = "")
        {
            this.fromActionId = fromActionId ?? string.Empty;
            this.toActionId = toActionId ?? string.Empty;
            this.requestType = requestType;
            this.requiredFactId = requiredFactId ?? string.Empty;
            this.minPriority = minPriority;
            this.force = force;
            this.resistanceRule = resistanceRule;
            this.diagnosticsLabel = diagnosticsLabel ?? string.Empty;
        }

        public string FromActionId => fromActionId ?? string.Empty;
        public string ToActionId => toActionId ?? string.Empty;
        public ActionRequestType RequestType => requestType;
        public string RequiredFactId => requiredFactId ?? string.Empty;
        public int MinPriority => minPriority;
        public bool Force => force;
        public ActionTransitionResistanceRule ResistanceRule => resistanceRule;
        public string DiagnosticsLabel => diagnosticsLabel ?? string.Empty;
    }

    [Serializable]
    public struct ActionTransitionPolicyMatrixDefinition
    {
        public ActionTransitionPolicyRowDefinition[] rows;

        public ActionTransitionPolicyMatrixDefinition(ActionTransitionPolicyRowDefinition[] rows)
        {
            this.rows = rows ?? Array.Empty<ActionTransitionPolicyRowDefinition>();
        }

        public IReadOnlyList<ActionTransitionPolicyRowDefinition> Rows => rows ?? Array.Empty<ActionTransitionPolicyRowDefinition>();
        public int Count => Rows.Count;
    }
}
