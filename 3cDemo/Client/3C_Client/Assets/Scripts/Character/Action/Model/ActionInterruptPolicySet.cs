using System;
using System.Collections.Generic;

namespace ThirdPersonAction
{
    public sealed class ActionInterruptPolicySet
    {
        static readonly ActionInterruptPolicyDefinition[] EmptyPolicies = Array.Empty<ActionInterruptPolicyDefinition>();

        readonly ActionInterruptPolicyDefinition[] policies;

        public ActionInterruptPolicySet(IEnumerable<ActionInterruptPolicyDefinition> policies)
        {
            if (policies == null)
            {
                this.policies = EmptyPolicies;
                return;
            }

            List<ActionInterruptPolicyDefinition> copy = new List<ActionInterruptPolicyDefinition>(policies);
            this.policies = copy.Count == 0 ? EmptyPolicies : copy.ToArray();
        }

        public IReadOnlyList<ActionInterruptPolicyDefinition> Policies => policies;
        public int Count => policies.Length;

        public ActionInterruptPolicyDefinition this[int index] => policies[index];

        public ActionInterruptPolicyDefinition[] ToArray()
        {
            if (policies.Length == 0)
                return EmptyPolicies;

            ActionInterruptPolicyDefinition[] copy = new ActionInterruptPolicyDefinition[policies.Length];
            Array.Copy(policies, copy, policies.Length);
            return copy;
        }
    }
}
