using UnityEngine;
using UnityEngine.InputSystem;
using ThirdPersonCharacterConfig;
using ThirdPersonSimulation;

namespace ThirdPersonInput
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class UnityInputSystemRequestBufferAdapter : MonoBehaviour, IPredictionButtonFrameSource
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

        public void ApplyFormalInputConfig(CharacterConfigSO config)
        {
            if (config == null)
                return;

            InputAction previousAction = dodgeAction;
            InputActionAsset formalInputActions = config.InputActions;
            string formalActionMapName = ResolveActionMapName(config.DodgeInputAction);
            string formalDodgeActionName = ResolveActionName(config.DodgeInputAction);
            bool changed = inputActions != formalInputActions ||
                           actionMapName != formalActionMapName ||
                           dodgeActionName != formalDodgeActionName;

            if (changed)
            {
                if (enableInputOnEnable && isActiveAndEnabled)
                    SetActionEnabled(previousAction, false);

                inputActions = formalInputActions;
                actionMapName = formalActionMapName;
                dodgeActionName = formalDodgeActionName;
                dodgeAction = null;
            }

            if (enableInputOnEnable && isActiveAndEnabled)
                SetActionEnabled(ResolveDodgeAction(), true);
        }

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

            WriteCurrentButtonState();
        }

        public void Tick(int step)
        {
            ResolveBuffer();
            if (bufferComponent == null)
                return;

            bufferComponent.SetStep(step);
            WriteCurrentButtonState();
        }

        public bool TryReadPredictionButtons(
            out PredictionButtonFrame dodge,
            out PredictionButtonFrame attack,
            out PredictionButtonFrame jump,
            out PredictionButtonFrame interact)
        {
            InputAction action = ResolveDodgeAction();
            bool dodgeHeld = action != null && action.IsPressed();
            InputButtonState dodgeState = InputButtonState.FromHeld(previousDodgeHeld, dodgeHeld);
            dodge = new PredictionButtonFrame(dodgeState.Pressed, dodgeState.Held, dodgeState.Released);
            attack = PredictionButtonFrame.None;
            jump = PredictionButtonFrame.None;
            interact = PredictionButtonFrame.None;
            return true;
        }

        void WriteCurrentButtonState()
        {
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

        static string ResolveActionName(InputActionReference reference)
        {
            return reference != null && reference.action != null ? reference.action.name : string.Empty;
        }

        static string ResolveActionMapName(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.actionMap != null ? action.actionMap.name : string.Empty;
        }
    }
}
