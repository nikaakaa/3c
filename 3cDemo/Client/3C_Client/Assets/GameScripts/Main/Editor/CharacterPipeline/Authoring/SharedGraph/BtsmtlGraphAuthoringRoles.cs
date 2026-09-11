using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class BtsmtlGraphAuthoringRoles
    {
        public const string BaseTree = "BaseTree";
        public const string RunnableTree = "RunnableTree";
        public const string SubTree = "SubTree";
        public const string StateBehaviorSubTree = "StateBehaviorSubTree";
        public const string StateMachineGraph = "StateMachineGraph";
        public const string ConditionRuleGraph = "ConditionRuleGraph";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            BaseTree,
            RunnableTree,
            SubTree,
            StateBehaviorSubTree,
            StateMachineGraph,
            ConditionRuleGraph
        };

        public static bool IsKnown(string value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value);
    }
}
