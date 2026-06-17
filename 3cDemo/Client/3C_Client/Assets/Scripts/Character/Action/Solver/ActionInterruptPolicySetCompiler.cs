using System;
using System.Collections.Generic;

namespace ThirdPersonAction
{
    public static class ActionInterruptPolicySetCompiler
    {
        public static IReadOnlyList<ActionInterruptPolicy> Compile(ActionInterruptPolicySet policySet)
        {
            if (policySet == null || policySet.Count == 0)
                return Array.Empty<ActionInterruptPolicy>();

            ActionInterruptPolicy[] result = new ActionInterruptPolicy[policySet.Count];
            for (int i = 0; i < policySet.Count; i++)
                result[i] = Compile(policySet[i]);

            return result;
        }

        public static ActionInterruptPolicy Compile(ActionInterruptPolicyDefinition definition)
        {
            return new ActionInterruptPolicy(
                ToStateId(definition.FromStateId),
                ToStateId(definition.TargetStateId),
                definition.MinPriority,
                definition.TimingRule,
                definition.WindowStart,
                definition.WindowEnd,
                definition.Force,
                definition.WindowId,
                definition.RequiredFactId,
                definition.RequestType);
        }

        static ActionStateId ToStateId(string value)
        {
            return new ActionStateId((value ?? string.Empty).Trim());
        }
    }
}
