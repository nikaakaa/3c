using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public enum CharacterActionRequestTimingClass : byte
    {
        Immediate = 1,
        Offensive = 2
    }

    public enum CharacterVector2ConflictPolicy : byte
    {
        UnityComposite = 0,
        LatestActuatedCardinal = 1
    }

    [CreateAssetMenu(fileName = "CharacterInputProfile", menuName = "3C/Character/Input Profile")]
    public sealed class CharacterInputProfile : ScriptableObject
    {
        [SerializeField] InputActionAsset m_SourceAsset;
        [SerializeField] string m_BindingGroup;
        [SerializeField] List<CharacterInputValueDefinition> m_InputValues = new List<CharacterInputValueDefinition>();
        [SerializeField] List<CharacterActionRequestDefinition> m_ActionRequests = new List<CharacterActionRequestDefinition>();

        public InputActionAsset SourceAsset => m_SourceAsset;
        public string BindingGroup => string.IsNullOrWhiteSpace(m_BindingGroup)
            ? string.Empty
            : m_BindingGroup.Trim();
        public IReadOnlyList<CharacterInputValueDefinition> InputValues => m_InputValues;
        public IReadOnlyList<CharacterActionRequestDefinition> ActionRequests => m_ActionRequests;

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            HashSet<string> inputValueIds = new HashSet<string>();
            HashSet<string> requestIds = new HashSet<string>();

            if (m_SourceAsset == null)
            {
                errors?.Add($"{name}: source InputActionAsset is missing.");
                valid = false;
            }
            else
            {
                string bindingGroup = BindingGroup;
                bool found = false;
                for (int i = 0; i < m_SourceAsset.controlSchemes.Count; i++)
                {
                    if (!string.Equals(
                            m_SourceAsset.controlSchemes[i].bindingGroup,
                            bindingGroup,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }
                    found = true;
                    break;
                }
                if (string.IsNullOrEmpty(bindingGroup) || !found)
                {
                    errors?.Add($"{name}: binding group '{bindingGroup}' is not declared by the source InputActionAsset.");
                    valid = false;
                }
            }

            for (int i = 0; i < m_InputValues.Count; i++)
            {
                CharacterInputValueDefinition inputValue = m_InputValues[i];
                if (inputValue == null)
                {
                    errors?.Add($"{name}: input value #{i} is missing.");
                    valid = false;
                    continue;
                }

                if (string.IsNullOrEmpty(inputValue.InputValueId))
                {
                    errors?.Add($"{name}: input value #{i} id is missing.");
                    valid = false;
                }
                else if (!inputValueIds.Add(inputValue.InputValueId))
                {
                    errors?.Add($"{name}: duplicate input value id '{inputValue.InputValueId}'.");
                    valid = false;
                }

                if (!Enum.IsDefined(typeof(CharacterVector2ConflictPolicy), inputValue.Vector2ConflictPolicy))
                {
                    errors?.Add($"{name}: input value '{inputValue.InputValueId}' Vector2 conflict policy is invalid.");
                    valid = false;
                    continue;
                }

                if (inputValue.ValueType != CharacterInputValueType.Vector2 &&
                    inputValue.Vector2ConflictPolicy != CharacterVector2ConflictPolicy.UnityComposite)
                {
                    errors?.Add($"{name}: input value '{inputValue.InputValueId}' cannot use a Vector2 conflict policy with value type '{inputValue.ValueType}'.");
                    valid = false;
                    continue;
                }

                if (!inputValue.TryResolveAction(m_SourceAsset, out InputAction action, out string error))
                {
                    errors?.Add($"{name}: input value '{inputValue.InputValueId}' {error}");
                    valid = false;
                    continue;
                }

                if (inputValue.Vector2ConflictPolicy == CharacterVector2ConflictPolicy.LatestActuatedCardinal &&
                    !CharacterDirectionalInputConflictResolver.TryValidateAction(action, out error))
                {
                    errors?.Add($"{name}: input value '{inputValue.InputValueId}' {error}");
                    valid = false;
                }
            }

            for (int i = 0; i < m_ActionRequests.Count; i++)
            {
                CharacterActionRequestDefinition request = m_ActionRequests[i];
                if (request == null)
                {
                    errors?.Add($"{name}: request #{i} is missing.");
                    valid = false;
                    continue;
                }

                if (string.IsNullOrEmpty(request.RequestId))
                {
                    errors?.Add($"{name}: action request #{i} id is missing.");
                    valid = false;
                }
                else if (!requestIds.Add(request.RequestId))
                {
                    errors?.Add($"{name}: duplicate action request id '{request.RequestId}'.");
                    valid = false;
                }

                if (!Enum.IsDefined(typeof(CharacterActionRequestTimingClass), request.TimingClass))
                {
                    errors?.Add($"{name}: action request '{request.RequestId}' timing class is invalid.");
                    valid = false;
                }

                if (!request.TryResolveAction(m_SourceAsset, out _, out string error))
                {
                    errors?.Add($"{name}: action request '{request.RequestId}' {error}");
                    valid = false;
                }
            }

            return valid;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            for (int i = 0; i < m_ActionRequests.Count; i++)
                m_ActionRequests[i]?.Clamp();
        }
#endif
    }

    [Serializable]
    public sealed class CharacterInputValueDefinition
    {
        [SerializeField] string m_InputValueId;
        [SerializeField] CharacterInputValueType m_ValueType = CharacterInputValueType.Vector2;
        [SerializeField] CharacterVector2ConflictPolicy m_Vector2ConflictPolicy;
        [SerializeField] InputActionReference m_SourceAction;

        public string InputValueId => m_InputValueId;
        public CharacterInputValueType ValueType => m_ValueType;
        public CharacterVector2ConflictPolicy Vector2ConflictPolicy => m_Vector2ConflictPolicy;
        public InputActionReference SourceAction => m_SourceAction;

        public bool TryResolveAction(InputActionAsset sourceAsset, out InputAction action, out string error)
        {
            return CharacterInputActionResolver.TryResolve(sourceAsset, m_SourceAction, out action, out error);
        }
    }

    [Serializable]
    public sealed class CharacterActionRequestDefinition
    {
        [SerializeField] string m_RequestId;
        [SerializeField] InputActionReference m_SourceAction;
        [SerializeField, Min(0f)] float m_BufferSeconds = 0.2f;
        [SerializeField] int m_Priority;
        [SerializeField] CharacterActionRequestTimingClass m_TimingClass = CharacterActionRequestTimingClass.Immediate;

        public string RequestId => m_RequestId;
        public InputActionReference SourceAction => m_SourceAction;
        public float BufferSeconds => Mathf.Max(0f, m_BufferSeconds);
        public int Priority => m_Priority;
        public CharacterActionRequestTimingClass TimingClass => m_TimingClass;

        public bool TryResolveAction(InputActionAsset sourceAsset, out InputAction action, out string error)
        {
            return CharacterInputActionResolver.TryResolve(sourceAsset, m_SourceAction, out action, out error);
        }

        public void Clamp()
        {
            m_BufferSeconds = Mathf.Max(0f, m_BufferSeconds);
        }
    }

    static class CharacterInputActionResolver
    {
        public static bool TryResolve(InputActionAsset sourceAsset, InputActionReference reference, out InputAction action, out string error)
        {
            action = null;
            error = string.Empty;

            if (sourceAsset == null)
            {
                error = "source asset is missing.";
                return false;
            }

            if (reference == null || reference.action == null)
            {
                error = "source action reference is missing.";
                return false;
            }

            InputAction referencedAction = reference.action;
            if (referencedAction.actionMap == null || referencedAction.actionMap.asset != sourceAsset)
            {
                error = "source action does not belong to profile source asset.";
                return false;
            }

            action = sourceAsset.FindAction(referencedAction.id);
            if (action == null)
            {
                error = $"source action id '{referencedAction.id}' was not found.";
                return false;
            }

            return true;
        }
    }
}
