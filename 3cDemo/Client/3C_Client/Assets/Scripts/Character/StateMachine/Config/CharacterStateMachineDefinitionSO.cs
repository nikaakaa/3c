using System;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    [CreateAssetMenu(fileName = "CharacterStateMachineDefinition", menuName = "3C/StateMachine/CharacterStateMachineDefinition")]
    public sealed class CharacterStateMachineDefinitionSO : ScriptableObject
    {
        [SerializeField] string initialStateId = "Locomotion.Idle";
        [SerializeField] CharacterStateNodeDefinition[] nodes = Array.Empty<CharacterStateNodeDefinition>();
        [SerializeField] CharacterStateTransitionDefinition[] transitions = Array.Empty<CharacterStateTransitionDefinition>();
        [SerializeField] StateTimelinePolicyDefinition[] timelinePolicies = Array.Empty<StateTimelinePolicyDefinition>();

        public IReadOnlyListWrapper<CharacterStateNodeDefinition> Nodes => new IReadOnlyListWrapper<CharacterStateNodeDefinition>(nodes);
        public IReadOnlyListWrapper<CharacterStateTransitionDefinition> Transitions => new IReadOnlyListWrapper<CharacterStateTransitionDefinition>(transitions);
        public IReadOnlyListWrapper<StateTimelinePolicyDefinition> TimelinePolicies => new IReadOnlyListWrapper<StateTimelinePolicyDefinition>(timelinePolicies);

        public CharacterStateMachineDefinition ToDefinition()
        {
            if ((nodes == null || nodes.Length == 0) && (transitions == null || transitions.Length == 0))
                throw new InvalidOperationException("Character state machine asset has no configured nodes or transitions.");

            return new CharacterStateMachineDefinition(new CharacterStateId(initialStateId), nodes, transitions, timelinePolicies);
        }

        public CharacterStateMachineValidationResult Validate()
        {
            return ToDefinition().Validate();
        }

    }

    public readonly struct IReadOnlyListWrapper<T> : System.Collections.Generic.IReadOnlyList<T>
    {
        readonly T[] source;

        public IReadOnlyListWrapper(T[] source)
        {
            this.source = source ?? Array.Empty<T>();
        }

        public int Count => source.Length;

        public T this[int index] => source[index];

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < source.Length; i++)
                yield return source[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
