using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineDefinition
    {
        readonly CharacterStateNodeDefinition[] nodes;
        readonly CharacterStateTransitionDefinition[] transitions;
        readonly StateTimelinePolicyDefinition[] timelinePolicies;
        readonly Dictionary<CharacterStateId, CharacterStateNodeDefinition> nodeMap;
        readonly Dictionary<CharacterStateId, StateTimelinePolicyDefinition> timelinePolicyMap;

        public CharacterStateMachineDefinition(
            CharacterStateId initialState,
            CharacterStateNodeDefinition[] nodes,
            CharacterStateTransitionDefinition[] transitions)
            : this(initialState, nodes, transitions, Array.Empty<StateTimelinePolicyDefinition>())
        {
        }

        public CharacterStateMachineDefinition(
            CharacterStateId initialState,
            CharacterStateNodeDefinition[] nodes,
            CharacterStateTransitionDefinition[] transitions,
            StateTimelinePolicyDefinition[] timelinePolicies)
        {
            InitialState = initialState.IsValid ? initialState : CharacterStateIds.Idle;
            this.nodes = nodes ?? Array.Empty<CharacterStateNodeDefinition>();
            this.transitions = transitions ?? Array.Empty<CharacterStateTransitionDefinition>();
            this.timelinePolicies = timelinePolicies ?? Array.Empty<StateTimelinePolicyDefinition>();
            nodeMap = new Dictionary<CharacterStateId, CharacterStateNodeDefinition>();
            timelinePolicyMap = new Dictionary<CharacterStateId, StateTimelinePolicyDefinition>();

            for (int i = 0; i < this.nodes.Length; i++)
            {
                CharacterStateNodeDefinition node = this.nodes[i];
                if (node != null && node.StateId.IsValid && !nodeMap.ContainsKey(node.StateId))
                    nodeMap.Add(node.StateId, node);
            }

            for (int i = 0; i < this.timelinePolicies.Length; i++)
            {
                StateTimelinePolicyDefinition policy = this.timelinePolicies[i];
                if (policy.StateId.IsValid && !timelinePolicyMap.ContainsKey(policy.StateId))
                    timelinePolicyMap.Add(policy.StateId, policy);
            }
        }

        public CharacterStateId InitialState { get; }
        public IReadOnlyList<CharacterStateNodeDefinition> Nodes => nodes;
        public IReadOnlyList<CharacterStateTransitionDefinition> Transitions => transitions;
        public IReadOnlyList<StateTimelinePolicyDefinition> TimelinePolicies => timelinePolicies;

        public bool TryGetNode(CharacterStateId id, out CharacterStateNodeDefinition node)
        {
            return nodeMap.TryGetValue(id, out node);
        }

        public bool TryGetTimelinePolicy(CharacterStateId id, out StateTimelinePolicyDefinition policy)
        {
            return timelinePolicyMap.TryGetValue(id, out policy);
        }

        public CharacterStateMachineValidationResult Validate()
        {
            return CharacterStateMachineValidator.Validate(this);
        }
    }
}
