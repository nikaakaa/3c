using System;
using UnityEngine;

namespace ThirdPersonCharacterBehavior
{
    [CreateAssetMenu(fileName = "CharacterBehaviorRuntimeDefinition", menuName = "3C/Behavior/Runtime Definition")]
    public sealed class CharacterBehaviorRuntimeDefinitionSO : ScriptableObject
    {
        [SerializeField] string rootId = "behavior.root";
        [SerializeField] CharacterBehaviorSourceKind[] leafOrder =
        {
            CharacterBehaviorSourceKind.Locomotion,
            CharacterBehaviorSourceKind.CommittedAction
        };

        public CharacterBehaviorRuntimeDefinition ToDefinition()
        {
            CharacterBehaviorSourceKind[] leaves = leafOrder != null
                ? (CharacterBehaviorSourceKind[])leafOrder.Clone()
                : Array.Empty<CharacterBehaviorSourceKind>();
            return new CharacterBehaviorRuntimeDefinition(new CharacterBehaviorSourceId(rootId), leaves);
        }

        public void SetDefinition(CharacterBehaviorRuntimeDefinition definition)
        {
            rootId = definition.RootId.Value;
            leafOrder = new CharacterBehaviorSourceKind[definition.LeafCount];
            for (int i = 0; i < definition.LeafCount; i++)
                leafOrder[i] = definition.GetLeafAt(i);
        }
    }
}
