using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateMachineValidator
    {
        public static CharacterStateMachineValidationResult Validate(CharacterStateMachineDefinition definition)
        {
            CharacterStateMachineValidationResult result = new CharacterStateMachineValidationResult();
            if (definition == null)
            {
                result.AddError("Character state machine definition is missing.");
                return result;
            }

            HashSet<CharacterStateId> ids = new HashSet<CharacterStateId>();
            IReadOnlyList<CharacterStateNodeDefinition> nodes = definition.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                CharacterStateNodeDefinition node = nodes[i];
                if (node == null)
                {
                    result.AddError($"Node[{i}] is missing.");
                    continue;
                }

                CharacterStateId id = node.StateId;
                if (!id.IsValid)
                {
                    result.AddError($"Node[{i}] state id is missing.");
                    continue;
                }

                if (!ids.Add(id))
                    result.AddError($"Node[{i}] duplicates state '{id.Value}'.");

                ValidateActionMovementNode(node, result);
            }

            if (!ids.Contains(definition.InitialState))
                result.AddError($"Initial state '{definition.InitialState.Value}' is not declared.");

            IReadOnlyList<CharacterStateTransitionDefinition> transitions = definition.Transitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                CharacterStateTransitionDefinition transition = transitions[i];
                if (transition == null)
                {
                    result.AddError($"Transition[{i}] is missing.");
                    continue;
                }

                if (!IsWildcardSource(transition.FromStateId) && !ids.Contains(new CharacterStateId(transition.FromStateId)))
                    result.AddError($"Transition[{i}] source '{transition.FromStateId}' is not declared.");

                if (!ids.Contains(transition.ToStateId))
                    result.AddError($"Transition[{i}] target '{transition.ToStateId.Value}' is not declared.");
            }

            return result;
        }

        static void ValidateActionMovementNode(CharacterStateNodeDefinition node, CharacterStateMachineValidationResult result)
        {
            if (node.Output.MotionOutput != CharacterStateMotionOutputKind.ConfiguredActionMovement)
                return;

            if (!node.HasTag(CharacterStateTag.Action))
                result.AddError($"State '{node.StateId.Value}' outputs action movement but is not tagged as Action.");

            if (node.Output.ActionMovements.Count == 0)
            {
                result.AddError($"State '{node.StateId.Value}' outputs action movement but has no movement definitions.");
                return;
            }

            for (int i = 0; i < node.Output.ActionMovements.Count; i++)
                ValidateActionMovementAnimation(node, node.Output.ActionMovements[i], result);
        }

        static void ValidateActionMovementAnimation(
            CharacterStateNodeDefinition node,
            CharacterActionMovementDefinition movement,
            CharacterStateMachineValidationResult result)
        {
            CharacterStateVariant variant = movement.Variant;
            if (variant == CharacterStateVariant.None)
            {
                if (!node.Animation.HasKey || !node.Animation.HasAnimationReference)
                    result.AddError($"State '{node.StateId.Value}' action movement animation binding is missing.");
                return;
            }

            if (!node.TryResolveVariant(variant, out CharacterStateVariantDefinition definition))
            {
                result.AddError($"State '{node.StateId.Value}' action movement variant '{variant}' is missing.");
                return;
            }

            if (!definition.Animation.HasKey || !definition.Animation.HasAnimationReference)
                result.AddError($"State '{node.StateId.Value}' action movement variant '{variant}' animation binding is missing.");
        }

        static bool IsWildcardSource(string source)
        {
            return source == "*" || source.EndsWith("/*", System.StringComparison.Ordinal);
        }
    }
}
