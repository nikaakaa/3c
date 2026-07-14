using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeDesigner
{
    [DisallowMultipleComponent]
    public sealed class InputActionAssetValueSource : MonoBehaviour, IInputActionValueSource
    {
        [SerializeField]
        InputActionAsset m_Actions;

        public InputActionAsset Actions { get => m_Actions; set => m_Actions = value; }

        void OnEnable()
        {
            m_Actions?.Enable();
        }

        void OnDisable()
        {
            m_Actions?.Disable();
        }

        public bool TryReadButton(InputActionAsset sourceAsset, string actionId, out bool value)
        {
            value = false;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            value = action.IsPressed();
            return true;
        }

        public bool TryReadFloat(InputActionAsset sourceAsset, string actionId, out float value)
        {
            value = 0f;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            try
            {
                value = action.ReadValue<float>();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public bool TryReadVector2(InputActionAsset sourceAsset, string actionId, out Vector2 value)
        {
            value = Vector2.zero;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            try
            {
                value = action.ReadValue<Vector2>();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        bool TryFindAction(InputActionAsset sourceAsset, string actionId, out InputAction action)
        {
            action = null;
            if (!m_Actions || !sourceAsset || sourceAsset != m_Actions || string.IsNullOrEmpty(actionId) || !Guid.TryParse(actionId, out Guid guid))
                return false;

            action = sourceAsset.FindAction(guid);
            return action != null;
        }
    }
}
