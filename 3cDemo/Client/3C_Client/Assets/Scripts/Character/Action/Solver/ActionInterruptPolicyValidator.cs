using System.Collections.Generic;

namespace ThirdPersonAction
{
    public static class ActionInterruptPolicyValidator
    {
        public static ActionInterruptPolicyValidationResult Validate(ActionInterruptPolicySet policySet)
        {
            ActionInterruptPolicyValidationResult result = new ActionInterruptPolicyValidationResult();
            if (policySet == null || policySet.Count == 0)
                return result;

            HashSet<string> keys = new HashSet<string>();
            for (int i = 0; i < policySet.Count; i++)
            {
                ActionInterruptPolicy policy = ActionInterruptPolicySetCompiler.Compile(policySet[i]);
                ValidatePolicy(policy, i, result);

                string key = $"{policy.FromState.Value}->{policy.TargetState.Value}:{policy.TimingRule}";
                if (!keys.Add(key))
                    result.AddWarning($"Policy {i} duplicates an earlier policy: {key}.");
            }

            return result;
        }

        public static ActionInterruptPolicyValidationResult Validate(IReadOnlyList<ActionInterruptPolicy> policies)
        {
            ActionInterruptPolicyValidationResult result = new ActionInterruptPolicyValidationResult();
            if (policies == null || policies.Count == 0)
                return result;

            HashSet<string> keys = new HashSet<string>();
            for (int i = 0; i < policies.Count; i++)
            {
                ActionInterruptPolicy policy = policies[i];
                ValidatePolicy(policy, i, result);

                string key = $"{policy.FromState.Value}->{policy.TargetState.Value}:{policy.TimingRule}";
                if (!keys.Add(key))
                    result.AddWarning($"Policy {i} duplicates an earlier policy: {key}.");
            }

            return result;
        }

        public static bool IsPolicyValid(ActionInterruptPolicy policy)
        {
            return ValidateSingle(policy).HasErrors == false;
        }

        static ActionInterruptPolicyValidationResult ValidateSingle(ActionInterruptPolicy policy)
        {
            ActionInterruptPolicyValidationResult result = new ActionInterruptPolicyValidationResult();
            ValidatePolicy(policy, 0, result);
            return result;
        }

        static void ValidatePolicy(ActionInterruptPolicy policy, int index, ActionInterruptPolicyValidationResult result)
        {
            if (!policy.FromState.IsValid)
                result.AddError($"Policy {index} from state is invalid.");

            if (!policy.TargetState.IsValid)
                result.AddError($"Policy {index} target state is invalid.");

            if (policy.MinPriority < 0)
                result.AddError($"Policy {index} min priority is invalid.");

            switch (policy.TimingRule)
            {
                case ActionInterruptTimingRule.Always:
                    break;
                case ActionInterruptTimingRule.AfterElapsedTime:
                    if (policy.WindowStart < 0f)
                        result.AddError($"Policy {index} elapsed time is invalid.");
                    break;
                case ActionInterruptTimingRule.DuringElapsedTimeWindow:
                    if (policy.WindowStart < 0f)
                        result.AddError($"Policy {index} window start is invalid.");
                    if (policy.WindowEnd < policy.WindowStart)
                        result.AddError($"Policy {index} window end is earlier than window start.");
                    break;
                default:
                    result.AddError($"Policy {index} timing rule is invalid.");
                    break;
            }
        }
    }
}
