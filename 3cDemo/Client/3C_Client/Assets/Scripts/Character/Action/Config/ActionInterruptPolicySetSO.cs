using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonAction
{
    [CreateAssetMenu(fileName = "ActionInterruptPolicySet", menuName = "3C/Action/ActionInterruptPolicySet")]
    public sealed class ActionInterruptPolicySetSO : ScriptableObject
    {
        [SerializeField] ActionInterruptPolicyDefinition[] policies = Array.Empty<ActionInterruptPolicyDefinition>();

        public IReadOnlyList<ActionInterruptPolicyDefinition> Policies => policies ?? Array.Empty<ActionInterruptPolicyDefinition>();

        public ActionInterruptPolicySet ToPolicySet()
        {
            return new ActionInterruptPolicySet(Policies);
        }

        public IReadOnlyList<ActionInterruptPolicy> CompilePolicies()
        {
            return ActionInterruptPolicySetCompiler.Compile(ToPolicySet());
        }

        public ActionInterruptPolicyValidationResult Validate()
        {
            return ActionInterruptPolicyValidator.Validate(ToPolicySet());
        }

        public void ResetToEmpty()
        {
            policies = Array.Empty<ActionInterruptPolicyDefinition>();
        }
    }
}
