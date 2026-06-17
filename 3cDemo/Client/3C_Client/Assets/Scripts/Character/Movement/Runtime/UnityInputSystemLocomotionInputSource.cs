using UnityEngine;
using UnityEngine.InputSystem;
using ThirdPersonCharacterConfig;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class UnityInputSystemLocomotionInputSource : MonoBehaviour, IBasicLocomotionInputSource, IFormalLocomotionInputConfigReceiver
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string actionMapName = "Player";
        [SerializeField] string moveActionName = "Move";
        [SerializeField] string lookActionName = "Look";
        [SerializeField] string runActionName = "";
        [SerializeField] bool enableInputOnEnable = true;

        InputAction moveAction;
        InputAction lookAction;
        InputAction runAction;

        public InputActionAsset InputActions { get => inputActions; set { inputActions = value; ClearCachedActions(); } }
        public string ActionMapName { get => actionMapName; set { actionMapName = value; ClearCachedActions(); } }
        public string MoveActionName { get => moveActionName; set { moveActionName = value; ClearCachedActions(); } }
        public string LookActionName { get => lookActionName; set { lookActionName = value; ClearCachedActions(); } }
        public string RunActionName { get => runActionName; set { runActionName = value; ClearCachedActions(); } }
        public InputAction MoveAction => ResolveMoveAction();
        public InputAction LookAction => ResolveLookAction();
        public InputAction RunAction => ResolveRunAction();

        public void ApplyFormalInputConfig(CharacterConfigSO config)
        {
            if (config == null)
                return;

            InputAction previousMoveAction = moveAction;
            InputAction previousLookAction = lookAction;
            InputAction previousRunAction = runAction;
            InputActionAsset formalInputActions = config.InputActions;
            string formalActionMapName = ResolveActionMapName(config.MoveAction, config.RunAction, config.LookAction);
            string formalMoveActionName = ResolveActionName(config.MoveAction);
            string formalRunActionName = ResolveActionName(config.RunAction);
            string formalLookActionName = ResolveActionName(config.LookAction);
            bool changed = inputActions != formalInputActions ||
                           actionMapName != formalActionMapName ||
                           moveActionName != formalMoveActionName ||
                           runActionName != formalRunActionName ||
                           lookActionName != formalLookActionName;

            if (changed)
            {
                if (enableInputOnEnable && isActiveAndEnabled)
                {
                    SetActionEnabled(previousMoveAction, false);
                    SetActionEnabled(previousLookAction, false);
                    SetActionEnabled(previousRunAction, false);
                }

                inputActions = formalInputActions;
                actionMapName = formalActionMapName;
                moveActionName = formalMoveActionName;
                runActionName = formalRunActionName;
                lookActionName = formalLookActionName;
                ClearCachedActions();
            }

            if (enableInputOnEnable && isActiveAndEnabled)
                SetInputEnabled(true);
        }

        void OnValidate()
        {
            ClearCachedActions();
        }

        void OnEnable()
        {
            if (enableInputOnEnable)
                SetInputEnabled(true);
        }

        void OnDisable()
        {
            if (enableInputOnEnable)
                SetInputEnabled(false);
        }

        public BasicLocomotionInputSnapshot ReadInput(float deltaTime)
        {
            InputAction activeMoveAction = ResolveMoveAction();
            InputAction activeLookAction = ResolveLookAction();
            InputAction activeRunAction = ResolveRunAction();
            Vector2 move = activeMoveAction != null ? activeMoveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 look = activeLookAction != null ? activeLookAction.ReadValue<Vector2>() : Vector2.zero;
            bool runHeld = activeRunAction != null && activeRunAction.IsPressed();
            return new BasicLocomotionInputSnapshot(deltaTime, move, look, runHeld);
        }

        public void SetInputEnabled(bool enabled)
        {
            SetActionEnabled(ResolveMoveAction(), enabled);
            SetActionEnabled(ResolveLookAction(), enabled);
            SetActionEnabled(ResolveRunAction(), enabled);
        }

        InputAction ResolveMoveAction()
        {
            return moveAction ?? (moveAction = ResolveAction(moveActionName));
        }

        InputAction ResolveLookAction()
        {
            return lookAction ?? (lookAction = ResolveAction(lookActionName));
        }

        InputAction ResolveRunAction()
        {
            return runAction ?? (runAction = ResolveAction(runActionName));
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

        void ClearCachedActions()
        {
            moveAction = null;
            lookAction = null;
            runAction = null;
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

        static string ResolveActionMapName(params InputActionReference[] references)
        {
            for (int i = 0; i < references.Length; i++)
            {
                InputAction action = references[i] != null ? references[i].action : null;
                if (action != null && action.actionMap != null)
                    return action.actionMap.name;
            }

            return string.Empty;
        }
    }
}
