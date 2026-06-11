using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonInput;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineDefinition
    {
        const float DefaultDodgeDuration = 0.35f;
        const float DefaultDirectionalDodgeDistance = 4f;
        const float DefaultBackstepDodgeDistance = 3f;
        const int DefaultDodgeRequestPriority = 30;

        readonly CharacterStateNodeDefinition[] nodes;
        readonly CharacterStateTransitionDefinition[] transitions;
        readonly Dictionary<CharacterStateId, CharacterStateNodeDefinition> nodeMap;

        public CharacterStateMachineDefinition(
            CharacterStateId initialState,
            CharacterStateNodeDefinition[] nodes,
            CharacterStateTransitionDefinition[] transitions)
        {
            InitialState = initialState.IsValid ? initialState : CharacterStateIds.Idle;
            this.nodes = nodes ?? Array.Empty<CharacterStateNodeDefinition>();
            this.transitions = transitions ?? Array.Empty<CharacterStateTransitionDefinition>();
            nodeMap = new Dictionary<CharacterStateId, CharacterStateNodeDefinition>();

            for (int i = 0; i < this.nodes.Length; i++)
            {
                CharacterStateNodeDefinition node = this.nodes[i];
                if (node != null && node.StateId.IsValid && !nodeMap.ContainsKey(node.StateId))
                    nodeMap.Add(node.StateId, node);
            }
        }

        public CharacterStateId InitialState { get; }
        public IReadOnlyList<CharacterStateNodeDefinition> Nodes => nodes;
        public IReadOnlyList<CharacterStateTransitionDefinition> Transitions => transitions;

        public bool TryGetNode(CharacterStateId id, out CharacterStateNodeDefinition node)
        {
            return nodeMap.TryGetValue(id, out node);
        }

        public CharacterStateMachineValidationResult Validate()
        {
            return CharacterStateMachineValidator.Validate(this);
        }

        public static CharacterStateMachineDefinition CreateDefault()
        {
            CharacterStateAnimationBinding dodgeDirectional =
                CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional");
            CharacterStateAnimationBinding dodgeBackstep =
                CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeBackstep.Value, "Dodge Backstep");

            CharacterStateNodeDefinition[] defaultNodes =
            {
                new CharacterStateNodeDefinition(
                    CharacterStateIds.FullBody,
                    default,
                    "FullBody",
                    new[] { CharacterStateTag.FullBody },
                    new CharacterStateOutputDefinition(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.Locomotion,
                    CharacterStateIds.FullBody,
                    "Locomotion",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement },
                    new CharacterStateOutputDefinition(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.Idle,
                    CharacterStateIds.Locomotion,
                    "Idle",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement },
                    CharacterStateOutputDefinition.IdleReset(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.MoveStart,
                    CharacterStateIds.Locomotion,
                    "MoveStart",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement },
                    CharacterStateOutputDefinition.InputDrivenMovement(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.MoveLoop,
                    CharacterStateIds.Locomotion,
                    "MoveLoop",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement },
                    CharacterStateOutputDefinition.InputDrivenMovement(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.MoveStop,
                    CharacterStateIds.Locomotion,
                    "MoveStop",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement },
                    CharacterStateOutputDefinition.InputDrivenMovement(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.Action,
                    CharacterStateIds.FullBody,
                    "Action",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Action },
                    new CharacterStateOutputDefinition(),
                    default),
                new CharacterStateNodeDefinition(
                    CharacterStateIds.Dodge,
                    CharacterStateIds.Action,
                    "Dodge",
                    new[] { CharacterStateTag.FullBody, CharacterStateTag.Action, CharacterStateTag.Dodge },
                    CharacterStateOutputDefinition.ActionMovement(
                        InputRequestKind.Dodge,
                        new CharacterActionMovementDefinition(
                            CharacterStateVariant.Directional,
                            DefaultDodgeDuration,
                            DefaultDirectionalDodgeDistance,
                            true,
                            true),
                        new CharacterActionMovementDefinition(
                            CharacterStateVariant.Backstep,
                            DefaultDodgeDuration,
                            DefaultBackstepDodgeDistance,
                            false,
                            false)),
                    default,
                    new[]
                    {
                        new CharacterStateVariantDefinition(CharacterStateVariant.Directional, dodgeDirectional),
                        new CharacterStateVariantDefinition(CharacterStateVariant.Backstep, dodgeBackstep)
                    })
            };

            CharacterStateTransitionCondition dodgeRequest = CharacterStateTransitionCondition.HasInputRequest(InputRequestKind.Dodge);
            CharacterStateTransitionCondition dodgePriority = CharacterStateTransitionCondition.RequestPriorityAtLeast(DefaultDodgeRequestPriority);

            CharacterStateTransitionDefinition[] defaultTransitions =
            {
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.Idle.Value,
                    CharacterStateIds.MoveStart,
                    100,
                    CharacterStateTransitionCondition.HasMoveIntent()),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.MoveStart.Value,
                    CharacterStateIds.MoveStop,
                    100,
                    CharacterStateTransitionCondition.NoMoveIntent()),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.MoveStart.Value,
                    CharacterStateIds.MoveLoop,
                    0,
                    CharacterStateTransitionCondition.HasMoveIntent(),
                    CharacterStateTransitionCondition.StateCanExit()),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.MoveLoop.Value,
                    CharacterStateIds.MoveStop,
                    100,
                    CharacterStateTransitionCondition.NoMoveIntent()),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.MoveStop.Value,
                    CharacterStateIds.MoveStart,
                    100,
                    CharacterStateTransitionCondition.HasMoveIntent()),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.MoveStop.Value,
                    CharacterStateIds.Idle,
                    0,
                    CharacterStateTransitionCondition.NoMoveIntent(),
                    CharacterStateTransitionCondition.StateCanExit()),
                new CharacterStateTransitionDefinition(
                    "FullBody/Locomotion/*",
                    CharacterStateIds.Dodge,
                    1000,
                    dodgeRequest,
                    dodgePriority),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.Dodge.Value,
                    CharacterStateIds.MoveLoop,
                    100,
                    CharacterStateTransitionCondition.HasMoveIntent(),
                    CharacterStateTransitionCondition.StateElapsedAtLeast(DefaultDodgeDuration)),
                new CharacterStateTransitionDefinition(
                    CharacterStateIds.Dodge.Value,
                    CharacterStateIds.Idle,
                    0,
                    CharacterStateTransitionCondition.NoMoveIntent(),
                    CharacterStateTransitionCondition.StateElapsedAtLeast(DefaultDodgeDuration))
            };

            return new CharacterStateMachineDefinition(CharacterStateIds.Idle, defaultNodes, defaultTransitions);
        }
    }
}
