using System;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    [CreateAssetMenu(fileName = "CharacterStateMachineDefinition", menuName = "3C/StateMachine/CharacterStateMachineDefinition")]
    public sealed class CharacterStateMachineDefinitionSO : ScriptableObject
    {
        [SerializeField] string initialStateId = "FullBody/Locomotion/Idle";
        [SerializeField] CharacterStateNodeDefinition[] nodes = Array.Empty<CharacterStateNodeDefinition>();
        [SerializeField] CharacterStateTransitionDefinition[] transitions = Array.Empty<CharacterStateTransitionDefinition>();

        public IReadOnlyListWrapper<CharacterStateNodeDefinition> Nodes => new IReadOnlyListWrapper<CharacterStateNodeDefinition>(nodes);
        public IReadOnlyListWrapper<CharacterStateTransitionDefinition> Transitions => new IReadOnlyListWrapper<CharacterStateTransitionDefinition>(transitions);

        public CharacterStateMachineDefinition ToDefinition()
        {
            if ((nodes == null || nodes.Length == 0) && (transitions == null || transitions.Length == 0))
                return CharacterStateMachineDefinition.CreateDefault();

            return new CharacterStateMachineDefinition(new CharacterStateId(initialStateId), nodes, transitions);
        }

        public CharacterStateMachineValidationResult Validate()
        {
            return ToDefinition().Validate();
        }

        public void ResetToDefault()
        {
            CharacterStateMachineDefinition definition = CharacterStateMachineDefinition.CreateDefault();
            initialStateId = definition.InitialState.Value;
            nodes = CopyNodes(definition);
            transitions = CopyTransitions(definition);
        }

        public static CharacterStateMachineDefinition CreateDefaultDefinition()
        {
            return CharacterStateMachineDefinition.CreateDefault();
        }

        static CharacterStateNodeDefinition[] CopyNodes(CharacterStateMachineDefinition definition)
        {
            CharacterStateNodeDefinition[] copy = new CharacterStateNodeDefinition[definition.Nodes.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = definition.Nodes[i];
            return copy;
        }

        static CharacterStateTransitionDefinition[] CopyTransitions(CharacterStateMachineDefinition definition)
        {
            CharacterStateTransitionDefinition[] copy = new CharacterStateTransitionDefinition[definition.Transitions.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = definition.Transitions[i];
            return copy;
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
