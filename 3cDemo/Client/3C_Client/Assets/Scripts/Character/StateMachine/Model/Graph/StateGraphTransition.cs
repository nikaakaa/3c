using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class StateGraphTransition
    {
        readonly StateGraphConditionReference[] conditions;

        public StateGraphTransition(
            string fromNodeId,
            StateGraphNodeId toNodeId,
            int priority,
            StateGraphConditionReference[] conditions = null)
        {
            FromNodeId = StateGraphNodeId.Normalize(fromNodeId);
            ToNodeId = toNodeId;
            Priority = priority;
            this.conditions = conditions ?? Array.Empty<StateGraphConditionReference>();
        }

        public string FromNodeId { get; }
        public StateGraphNodeId ToNodeId { get; }
        public int Priority { get; }
        public IReadOnlyList<StateGraphConditionReference> Conditions => conditions;
        public string TransitionPath => $"{FromNodeId}->{ToNodeId.Value}";

        public bool MatchesSource(StateGraphNodeId currentNode)
        {
            string source = FromNodeId;
            if (source == "*")
                return true;

            if (source.EndsWith("/*", StringComparison.Ordinal))
            {
                string prefix = source.Substring(0, source.Length - 1);
                return currentNode.Value.StartsWith(prefix, StringComparison.Ordinal);
            }

            if (source.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = source.Substring(0, source.Length - 1);
                return currentNode.Value.StartsWith(prefix, StringComparison.Ordinal);
            }

            return string.Equals(source, currentNode.Value, StringComparison.Ordinal);
        }
    }
}
