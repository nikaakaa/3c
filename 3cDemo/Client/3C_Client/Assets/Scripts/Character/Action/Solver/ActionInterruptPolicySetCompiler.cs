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
                definition.RequestType,
                definition.ResistanceRule);
        }

        public static IReadOnlyList<ActionInterruptPolicy> Compile(
            ActionTransitionPolicyMatrixDefinition matrix,
            ActionFactCompileContext factContext,
            out ActionInterruptPolicyValidationResult validation)
        {
            validation = ActionTransitionPolicyMatrixValidator.Validate(matrix, factContext);
            if (validation.HasErrors || matrix.Rows.Count == 0)
                return Array.Empty<ActionInterruptPolicy>();

            ActionInterruptPolicy[] result = new ActionInterruptPolicy[matrix.Rows.Count];
            for (int i = 0; i < matrix.Rows.Count; i++)
                result[i] = Compile(matrix.Rows[i]);

            return result;
        }

        public static ActionInterruptPolicy Compile(ActionTransitionPolicyRowDefinition row)
        {
            return new ActionInterruptPolicy(
                ToStateId(row.FromActionId),
                ToStateId(row.ToActionId),
                row.MinPriority,
                ActionInterruptTimingRule.Always,
                0f,
                0f,
                row.Force,
                string.Empty,
                row.RequiredFactId,
                row.RequestType,
                row.ResistanceRule);
        }

        static ActionStateId ToStateId(string value)
        {
            return new ActionStateId((value ?? string.Empty).Trim());
        }
    }
}
