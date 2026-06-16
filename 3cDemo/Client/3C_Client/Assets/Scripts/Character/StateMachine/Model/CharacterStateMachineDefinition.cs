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
        readonly StateGraphDefinition graph;
        readonly CharacterStateMetadataSet characterMetadata;

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
            graph = BuildGraph(InitialState, this.nodes, this.transitions);
            characterMetadata = BuildCharacterMetadata(this.nodes);

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
        public StateGraphDefinition Graph => graph;
        public CharacterStateMetadataSet CharacterMetadata => characterMetadata;

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

        public CharacterStateMachineValidationResult Validate(CharacterStateTransitionEvaluatorCollection transitionEvaluators)
        {
            return CharacterStateMachineValidator.Validate(this, transitionEvaluators);
        }

        static StateGraphDefinition BuildGraph(
            CharacterStateId initialState,
            CharacterStateNodeDefinition[] nodes,
            CharacterStateTransitionDefinition[] transitions)
        {
            Dictionary<StateGraphNodeId, List<StateGraphNodeId>> childrenByParent =
                new Dictionary<StateGraphNodeId, List<StateGraphNodeId>>();
            nodes = nodes ?? Array.Empty<CharacterStateNodeDefinition>();

            for (int i = 0; i < nodes.Length; i++)
            {
                CharacterStateNodeDefinition node = nodes[i];
                if (node == null)
                    continue;

                StateGraphNodeId parentId = new StateGraphNodeId(node.ParentStateId.Value);
                if (!parentId.IsValid)
                    continue;

                if (!childrenByParent.TryGetValue(parentId, out List<StateGraphNodeId> children))
                {
                    children = new List<StateGraphNodeId>();
                    childrenByParent.Add(parentId, children);
                }

                children.Add(new StateGraphNodeId(node.StateId.Value));
            }

            StateGraphNode[] graphNodes = new StateGraphNode[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                CharacterStateNodeDefinition node = nodes[i];
                if (node == null)
                    continue;

                StateGraphNodeId id = new StateGraphNodeId(node.StateId.Value);
                StateGraphNodeId parentId = new StateGraphNodeId(node.ParentStateId.Value);
                graphNodes[i] = new StateGraphNode(
                    id,
                    parentId,
                    node.PathSegment,
                    childrenByParent.TryGetValue(id, out List<StateGraphNodeId> children)
                        ? children.ToArray()
                        : Array.Empty<StateGraphNodeId>());
            }

            transitions = transitions ?? Array.Empty<CharacterStateTransitionDefinition>();
            StateGraphTransition[] graphTransitions = new StateGraphTransition[transitions.Length];
            for (int i = 0; i < transitions.Length; i++)
            {
                CharacterStateTransitionDefinition transition = transitions[i];
                if (transition == null)
                    continue;

                graphTransitions[i] = new StateGraphTransition(
                    transition.FromStateId,
                    new StateGraphNodeId(transition.ToStateId.Value),
                    transition.Priority,
                    BuildConditionReferences(transition.Conditions));
            }

            return new StateGraphDefinition(new StateGraphNodeId(initialState.Value), graphNodes, graphTransitions);
        }

        static StateGraphConditionReference[] BuildConditionReferences(
            IReadOnlyList<CharacterStateTransitionCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return Array.Empty<StateGraphConditionReference>();

            StateGraphConditionReference[] result = new StateGraphConditionReference[conditions.Count];
            for (int i = 0; i < conditions.Count; i++)
            {
                CharacterStateTransitionCondition condition = conditions[i];
                result[i] = new StateGraphConditionReference(
                    condition.Kind.ToString(),
                    condition.RequestKind.ToString(),
                    condition.MinSeconds,
                    condition.MinPriority,
                    condition.Tag.ToString());
            }

            return result;
        }

        static CharacterStateMetadataSet BuildCharacterMetadata(CharacterStateNodeDefinition[] nodes)
        {
            nodes = nodes ?? Array.Empty<CharacterStateNodeDefinition>();
            CharacterStateNodeMetadata[] metadata = new CharacterStateNodeMetadata[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                metadata[i] = CharacterStateNodeMetadata.FromNode(nodes[i]);
            return new CharacterStateMetadataSet(metadata);
        }
    }
}
