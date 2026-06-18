using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;

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

                string key = $"{policy.FromState.Value}->{policy.TargetState.Value}:{policy.RequestType}:{policy.TimingRule}:{policy.WindowId}:{policy.RequiredFactId.Value}";
                if (!keys.Add(key))
                    result.AddWarning($"Policy {i} duplicates an earlier policy: {key}.");
            }

            return result;
        }

        public static ActionInterruptPolicyValidationResult Validate(
            ActionInterruptPolicySet policySet,
            IReadOnlyList<StateTimelinePolicyDefinition> timelinePolicies)
        {
            ActionInterruptPolicyValidationResult result = Validate(policySet);
            ValidateTimelineWindowReferences(result, Compile(policySet), timelinePolicies);
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

                string key = $"{policy.FromState.Value}->{policy.TargetState.Value}:{policy.RequestType}:{policy.TimingRule}:{policy.WindowId}:{policy.RequiredFactId.Value}";
                if (!keys.Add(key))
                    result.AddWarning($"Policy {i} duplicates an earlier policy: {key}.");
            }

            return result;
        }

        public static ActionInterruptPolicyValidationResult Validate(
            IReadOnlyList<ActionInterruptPolicy> policies,
            IReadOnlyList<StateTimelinePolicyDefinition> timelinePolicies)
        {
            ActionInterruptPolicyValidationResult result = Validate(policies);
            ValidateTimelineWindowReferences(result, policies, timelinePolicies);
            return result;
        }

        public static bool IsPolicyValid(ActionInterruptPolicy policy)
        {
            return ValidateSingle(policy).HasErrors == false;
        }

        static IReadOnlyList<ActionInterruptPolicy> Compile(ActionInterruptPolicySet policySet)
        {
            return policySet == null ? null : ActionInterruptPolicySetCompiler.Compile(policySet);
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

            if (!System.Enum.IsDefined(typeof(ActionRequestType), policy.RequestType))
                result.AddError($"Policy {index} request type is invalid.");

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

        static void ValidateTimelineWindowReferences(
            ActionInterruptPolicyValidationResult result,
            IReadOnlyList<ActionInterruptPolicy> policies,
            IReadOnlyList<StateTimelinePolicyDefinition> timelinePolicies)
        {
            if (policies == null || policies.Count == 0)
                return;

            for (int i = 0; i < policies.Count; i++)
            {
                ActionInterruptPolicy policy = policies[i];
                if (!policy.RequiresTimelineWindow)
                {
                    if (!policy.RequiresTimelineFact)
                        continue;
                }

                if (policy.RequiresTimelineFact)
                {
                    if (!TryFindTimelineFact(policy, timelinePolicies, out StateTimelineWindowDefinition factWindow))
                    {
                        result.AddError($"Policy {i} references missing timeline fact '{policy.RequiredFactId.Value}'.");
                        continue;
                    }

                    if (!factWindow.IsRequestWindow)
                        result.AddError($"Policy {i} references non-request timeline fact '{policy.RequiredFactId.Value}'.");
                }

                if (!policy.RequiresTimelineWindow)
                    continue;

                if (!TryFindTimelineWindow(policy, timelinePolicies, out StateTimelineWindowDefinition window))
                {
                    result.AddError($"Policy {i} references missing timeline window '{policy.WindowId}'.");
                    continue;
                }

                if (!window.IsRequestWindow)
                    result.AddError($"Policy {i} references non-request timeline window '{policy.WindowId}'.");
            }
        }

        static bool TryFindTimelineWindow(
            ActionInterruptPolicy interruptPolicy,
            IReadOnlyList<StateTimelinePolicyDefinition> timelinePolicies,
            out StateTimelineWindowDefinition window)
        {
            if (timelinePolicies != null)
            {
                for (int i = 0; i < timelinePolicies.Count; i++)
                {
                    StateTimelinePolicyDefinition policy = timelinePolicies[i];
                    if (!MatchesPolicyState(interruptPolicy, policy))
                        continue;

                    for (int j = 0; j < policy.Windows.Count; j++)
                    {
                        StateTimelineWindowDefinition candidate = policy.Windows[j];
                        if (string.Equals(candidate.WindowId, interruptPolicy.WindowId, System.StringComparison.Ordinal))
                        {
                            window = candidate;
                            return true;
                        }
                    }
                }
            }

            window = default;
            return false;
        }

        static bool TryFindTimelineFact(
            ActionInterruptPolicy interruptPolicy,
            IReadOnlyList<StateTimelinePolicyDefinition> timelinePolicies,
            out StateTimelineWindowDefinition window)
        {
            if (timelinePolicies != null)
            {
                for (int i = 0; i < timelinePolicies.Count; i++)
                {
                    StateTimelinePolicyDefinition policy = timelinePolicies[i];
                    if (!MatchesPolicyState(interruptPolicy, policy))
                        continue;

                    for (int j = 0; j < policy.Windows.Count; j++)
                    {
                        StateTimelineWindowDefinition candidate = policy.Windows[j];
                        if (candidate.FactId == interruptPolicy.RequiredFactId)
                        {
                            window = candidate;
                            return true;
                        }
                    }
                }
            }

            window = default;
            return false;
        }

        static bool MatchesPolicyState(ActionInterruptPolicy interruptPolicy, StateTimelinePolicyDefinition timelinePolicy)
        {
            return interruptPolicy.FromState.Matches(new ActionStateId(timelinePolicy.StateId.Value));
        }
    }

    public static class ActionTransitionPolicyMatrixValidator
    {
        public static ActionInterruptPolicyValidationResult Validate(
            ActionTransitionPolicyMatrixDefinition matrix,
            ActionFactCompileContext factContext)
        {
            ActionInterruptPolicyValidationResult result = new ActionInterruptPolicyValidationResult();
            if (matrix.Rows.Count == 0)
                return result;

            HashSet<string> keys = new HashSet<string>();
            Dictionary<string, ActionTransitionPolicyRowDefinition> firstByKey =
                new Dictionary<string, ActionTransitionPolicyRowDefinition>();
            for (int i = 0; i < matrix.Rows.Count; i++)
            {
                ActionTransitionPolicyRowDefinition row = matrix.Rows[i];
                ValidateRow(row, i, factContext, result);
                string key = $"{Normalize(row.FromActionId)}->{Normalize(row.ToActionId)}:{row.RequestType}:{Normalize(row.RequiredFactId)}";
                if (keys.Add(key))
                {
                    firstByKey.Add(key, row);
                    continue;
                }

                ActionTransitionPolicyRowDefinition first = firstByKey[key];
                if (first.MinPriority == row.MinPriority &&
                    first.Force == row.Force &&
                    first.ResistanceRule == row.ResistanceRule)
                {
                    result.AddWarning($"Matrix row {i} duplicates an earlier row: {key}.");
                }
                else
                {
                    result.AddError($"Matrix row {i} conflicts with an earlier row: {key}.");
                }
            }

            return result;
        }

        static void ValidateRow(
            ActionTransitionPolicyRowDefinition row,
            int index,
            ActionFactCompileContext factContext,
            ActionInterruptPolicyValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(row.FromActionId))
                result.AddError($"Matrix row {index} from action id is missing.");
            else if (!IsMatrixActionId(row.FromActionId))
                result.AddError($"Matrix row {index} from action id is outside Action scope:{row.FromActionId}.");

            if (string.IsNullOrWhiteSpace(row.ToActionId))
                result.AddError($"Matrix row {index} to action id is missing.");
            else if (!IsMatrixActionId(row.ToActionId))
                result.AddError($"Matrix row {index} to action id is outside Action scope:{row.ToActionId}.");

            if (!System.Enum.IsDefined(typeof(ActionRequestType), row.RequestType) ||
                row.RequestType == ActionRequestType.None)
            {
                result.AddError($"Matrix row {index} request kind is missing.");
            }

            if (row.MinPriority < 0)
                result.AddError($"Matrix row {index} min priority is invalid.");

            if (!System.Enum.IsDefined(typeof(ActionTransitionResistanceRule), row.ResistanceRule))
                result.AddError($"Matrix row {index} resistance rule is invalid.");

            TimelineFactId factId = new TimelineFactId(row.RequiredFactId);
            if (!factId.IsValid)
            {
                result.AddError($"Matrix row {index} required fact id is missing.");
                return;
            }

            if (!ActionFactIdResolver.IsValidFactId(factId.Value))
            {
                result.AddError($"Matrix row {index} required fact id is invalid:{factId.Value}.");
                return;
            }

            if (!ActionFactIdResolver.TryResolve(factContext, factId, out _))
                result.AddError($"Matrix row {index} required fact id is missing from action fact context:{factId.Value}.");
        }

        static bool IsMatrixActionId(string value)
        {
            string normalized = Normalize(value);
            if (!normalized.StartsWith("Action.", System.StringComparison.Ordinal))
                return false;

            string tail = normalized.Substring("Action.".Length);
            return !string.IsNullOrWhiteSpace(tail) &&
                   tail.IndexOf('.', System.StringComparison.Ordinal) < 0;
        }

        static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
