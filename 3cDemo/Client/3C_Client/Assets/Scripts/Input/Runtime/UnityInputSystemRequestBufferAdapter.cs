using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonInput
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class UnityInputSystemRequestBufferAdapter : MonoBehaviour
    {
        [SerializeField] InputRequestBufferComponent bufferComponent;
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string actionMapName = "Player";
        [SerializeField] string dodgeActionName = "Dodge";
        [SerializeField] bool enableInputOnEnable = true;
        [SerializeField] bool advanceStepOnUpdate = true;

        InputAction dodgeAction;
        bool previousDodgeHeld;

        public InputRequestBufferComponent BufferComponent { get => bufferComponent; set => bufferComponent = value; }
        public InputActionAsset InputActions { get => inputActions; set { inputActions = value; dodgeAction = null; } }
        public string ActionMapName { get => actionMapName; set { actionMapName = value; dodgeAction = null; } }
        public string DodgeActionName { get => dodgeActionName; set { dodgeActionName = value; dodgeAction = null; } }
        public bool AdvanceStepOnUpdate { get => advanceStepOnUpdate; set => advanceStepOnUpdate = value; }

        void Reset()
        {
            ResolveBuffer();
        }

        void OnValidate()
        {
            dodgeAction = null;
        }

        void OnEnable()
        {
            ResolveBuffer();
            if (enableInputOnEnable)
                SetActionEnabled(ResolveDodgeAction(), true);
        }

        void OnDisable()
        {
            if (enableInputOnEnable)
                SetActionEnabled(ResolveDodgeAction(), false);

            previousDodgeHeld = false;
        }

        void Update()
        {
            Tick();
        }

        public void Tick()
        {
            ResolveBuffer();
            if (bufferComponent == null)
                return;

            if (advanceStepOnUpdate)
                bufferComponent.AdvanceStep();

            InputAction action = ResolveDodgeAction();
            bool dodgeHeld = action != null && action.IsPressed();
            bufferComponent.AddButtonState(InputButtonKind.Dodge, InputButtonState.FromHeld(previousDodgeHeld, dodgeHeld));
            previousDodgeHeld = dodgeHeld;
        }

        InputAction ResolveDodgeAction()
        {
            return dodgeAction ?? (dodgeAction = ResolveAction(dodgeActionName));
        }

        InputAction ResolveAction(string actionName)
        {
            if (inputActions == null || string.IsNullOrEmpty(actionName))
                return null;

            if (!string.IsNullOrEmpty(actionMapName))
            {
                InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
                if (actionMap != null)
                    return actionMap.FindAction(actionName, false);
            }

            return inputActions.FindAction(actionName, false);
        }

        void ResolveBuffer()
        {
            if (bufferComponent == null)
                bufferComponent = GetComponent<InputRequestBufferComponent>();
            if (bufferComponent == null)
                bufferComponent = GetComponentInParent<InputRequestBufferComponent>();
        }

        static void SetActionEnabled(InputAction action, bool enabled)
        {
            if (action == null)
                return;

            if (enabled)
                action.Enable();
            else
                action.Disable();
        }
    }
}
